// Copyright (c) Microsoft Corporation.  All Rights Reserved.  See License.txt in the project root for license information.

module TestFramework

open System
open System.IO
open System.Diagnostics
open System.Reflection
open Scripting
open Xunit
open FSharp.Compiler.IO

let getShortId() = Guid.NewGuid().ToString().[..7]

// Temporary directory is TempPath + "/FSharp.Test.Utilities/xxxxxxx/"
let tempDirectoryOfThisTestRun =
    let temp = DirectoryInfo(Path.Combine(__SOURCE_DIRECTORY__, @"../../artifacts/Temp/FSharp.Test.Utilities", $"{getShortId()}"))
    lazy (temp.Create(); temp)

let cleanUpTemporaryDirectoryOfThisTestRun () =
    if tempDirectoryOfThisTestRun.IsValueCreated then
        ()//try tempDirectoryOfThisTestRun.Value.Delete(true) with _ -> ()

let createTemporaryDirectory () =
    tempDirectoryOfThisTestRun.Value
        .CreateSubdirectory($"{getShortId()}")

let getTemporaryFileName () =
    createTemporaryDirectory().FullName ++ getShortId()

let changeExtension path extension = Path.ChangeExtension(path, extension)

let getTemporaryFileNameInDirectory (directory: DirectoryInfo) =
    directory.FullName ++ getShortId()

// Well, this function is AI generated.
let rec copyDirectory (sourceDir: string) (destinationDir: string) (recursive: bool) =
    // Get information about the source directory
    let dir = DirectoryInfo(sourceDir)

    // Check if the source directory exists
    if not dir.Exists then
        raise (DirectoryNotFoundException($"Source directory not found: {dir.FullName}"))

    // Create the destination directory
    Directory.CreateDirectory(destinationDir) |> ignore

    // Get the files in the source directory and copy to the destination directory
    for file in dir.EnumerateFiles() do
        let targetFilePath = Path.Combine(destinationDir, file.Name)
        file.CopyTo(targetFilePath) |> ignore

    // If recursive and copying subdirectories, recursively call this method
    if recursive then
        for subDir in dir.EnumerateDirectories() do
            let newDestinationDir = Path.Combine(destinationDir, subDir.Name)
            copyDirectory subDir.FullName newDestinationDir true

[<RequireQualifiedAccess>]
module Commands =

    let gate = obj()

    // Execute the process pathToExe passing the arguments: arguments with the working directory: workingDir timeout after timeout milliseconds -1 = wait forever
    // returns exit code, stdio and stderr as string arrays
    let executeProcess pathToExe arguments workingDir =
        let commandLine = ResizeArray()
        let errorsList = ResizeArray()
        let outputList = ResizeArray()
        let errorslock = obj()
        let outputlock = obj()
        let outputDataReceived (message: string) =
            if not (isNull message) then
                lock outputlock (fun () -> outputList.Add(message))

        let errorDataReceived (message: string) =
            if not (isNull message) then
                lock errorslock (fun () -> errorsList.Add(message))

        commandLine.Add $"cd {workingDir}"
        commandLine.Add $"{pathToExe} {arguments} /bl"

        let psi = ProcessStartInfo()
        psi.FileName <- pathToExe
        psi.WorkingDirectory <- workingDir
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        psi.Arguments <- arguments
        psi.CreateNoWindow <- true

        // When running tests, we want to roll forward to minor versions (including previews).
        psi.EnvironmentVariables["DOTNET_ROLL_FORWARD"] <- "LatestMajor"
        psi.EnvironmentVariables["DOTNET_ROLL_FORWARD_TO_PRERELEASE"] <- "1"

        // Host can sometimes add this, and it can break things
        psi.EnvironmentVariables.Remove("MSBuildSDKsPath")
        psi.UseShellExecute <- false

        use p = new Process()
        p.StartInfo <- psi

        p.OutputDataReceived.Add(fun a -> outputDataReceived a.Data)
        p.ErrorDataReceived.Add(fun a ->  errorDataReceived a.Data)

        if p.Start() then
            p.BeginOutputReadLine()
            p.BeginErrorReadLine()
            p.WaitForExit()

        let workingDir' =
            if workingDir = ""
            then
                // Assign working dir to prevent default to C:\Windows\System32
                let executionLocation = Assembly.GetExecutingAssembly().Location
                Path.GetDirectoryName executionLocation
            else
                workingDir

        lock gate (fun () ->
            File.WriteAllLines(Path.Combine(workingDir', "commandline.txt"), commandLine)
            File.WriteAllLines(Path.Combine(workingDir', "StandardOutput.txt"), outputList)
            File.WriteAllLines(Path.Combine(workingDir', "StandardError.txt"), errorsList)
        )
        p.ExitCode, outputList.ToArray(), errorsList.ToArray()

    let getfullpath workDir (path:string) =
        let rooted =
            if Path.IsPathRooted(path) then path
            else Path.Combine(workDir, path)
        rooted |> Path.GetFullPath

    let fileExists workDir path =
        if path |> getfullpath workDir |> FileSystem.FileExistsShim then Some path else None

    let directoryExists workDir path =
        if path |> getfullpath workDir |> Directory.Exists then Some path else None

    let copy workDir source dest =
        log "copy /y %s %s" source dest
        File.Copy( source |> getfullpath workDir, dest |> getfullpath workDir, true)
        CmdResult.Success ""

    let mkdir_p workDir dir =
        log "mkdir %s" dir
        Directory.CreateDirectory ( Path.Combine(workDir, dir) ) |> ignore

    let rm dir path =
        let p = path |> getfullpath dir
        if FileSystem.FileExistsShim(p) then
            (log "rm %s" p) |> ignore
            File.Delete(p)
        else
            (log "not found: %s p") |> ignore

    let rmdir dir path =
        let p = path |> getfullpath dir
        if Directory.Exists(p) then
            (log "rmdir /sy %s" p) |> ignore
            Directory.Delete(p, true)
        else
            (log "not found: %s p") |> ignore

    let pathAddBackslash (p: FilePath) =
        if String.IsNullOrWhiteSpace (p) then p
        else
            p.TrimEnd ([| Path.DirectorySeparatorChar; Path.AltDirectorySeparatorChar |])
            + Path.DirectorySeparatorChar.ToString()

    let echoAppendToFile workDir text p =
        log "echo %s> %s" text p
        let dest = p |> getfullpath workDir in File.AppendAllText(dest, text + Environment.NewLine)

    let appendToFile workDir source p =
        log "type %s >> %s" source p
        let from = source |> getfullpath workDir
        let dest = p |> getfullpath workDir
        let contents = File.ReadAllText(from)
        File.AppendAllText(dest, contents)

    let fsc workDir exec (dotNetExe: FilePath) (fscExe: FilePath) flags srcFiles =
        let args = (sprintf "%s %s" flags (srcFiles |> Seq.ofList |> String.concat " "))

        ignore workDir
#if NETCOREAPP
        exec dotNetExe (fscExe + " " + args)
#else
        ignore dotNetExe
        printfn "fscExe: %A" fscExe
        printfn "args: %A" args
        exec fscExe args
#endif

    let csc exec cscExe flags srcFiles =
        exec cscExe (sprintf "%s %s  /reference:netstandard.dll" flags (srcFiles |> Seq.ofList |> String.concat " "))

    let vbc exec vbcExe flags srcFiles =
        exec vbcExe (sprintf "%s %s  /reference:netstandard.dll" flags (srcFiles |> Seq.ofList |> String.concat " "))

    let fsi exec fsiExe flags sources =
        exec fsiExe (sprintf "%s %s"  flags (sources |> Seq.ofList |> String.concat " "))

    let internal quotepath (p: FilePath) =
        let quote = '"'.ToString()
        if p.Contains(" ") then (sprintf "%s%s%s" quote p quote) else p

    let ildasm exec ildasmExe flags assembly =
        exec ildasmExe (sprintf "%s %s" flags (quotepath assembly))

    let ilasm exec ilasmExe flags assembly =
        exec ilasmExe (sprintf "%s %s" flags (quotepath assembly))

    let sn exec snExe flags assembly =
        exec snExe (sprintf "%s %s" flags (quotepath assembly))

    let peverify exec peverifyExe flags path =
        exec peverifyExe (sprintf "%s %s" (quotepath path) flags)

type TestConfig =
    { EnvironmentVariables : Map<string, string>
      CSC : string
      csc_flags : string
      VBC : string
      vbc_flags : string
      BUILD_CONFIG : string
      FSC : string
      fsc_flags : string
      FSCOREDLLPATH : string
      FSI : string
#if !NETCOREAPP
      FSIANYCPU : string
      FSCANYCPU : string
      SN: string
#endif
      DOTNETFSCCOMPILERPATH : string
      FSI_FOR_SCRIPTS : string
      FSharpBuild : string
      FSharpCompilerInteractiveSettings : string
      fsi_flags : string
      ILDASM : string
      ILASM : string
      PEVERIFY : string
      Directory: string
      DotNetExe: string
      DotNetMultiLevelLookup: string
      DotNetRoot: string
      DefaultPlatform: string}

#if NETCOREAPP
open System.Runtime.InteropServices
#endif

let getOperatingSystem () =
#if NETCOREAPP
    let isPlatform p = RuntimeInformation.IsOSPlatform(p)
    if   isPlatform OSPlatform.Windows then "win"
    elif isPlatform OSPlatform.Linux   then "linux"
    elif isPlatform OSPlatform.OSX     then "osx"
    else                                    "unknown"
#else
    "win"
#endif

module DotnetPlatform =
    let Is64BitOperatingSystem envVars =
        match getOperatingSystem () with
        | "win" ->
            // On Windows PROCESSOR_ARCHITECTURE has the value AMD64 on 64 bit Intel Machines
            let value =
                let find s = envVars |> Map.tryFind s
                [| "PROCESSOR_ARCHITECTURE" |] |> Seq.tryPick (fun s -> find s) |> function None -> "" | Some x -> x
            value = "AMD64"
        | _ -> System.Environment.Is64BitOperatingSystem // As an alternative for netstandard1.4+: System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture

type FSLibPaths =
    { FSCOREDLLPATH : string }

let getPackagesDir () =
    match Environment.GetEnvironmentVariable("NUGET_PACKAGES") with
    | null ->
        let path = match  Environment.GetEnvironmentVariable("USERPROFILE") with
                   | null -> Environment.GetEnvironmentVariable("HOME")
                   | p -> p
        path ++ ".nuget" ++ "packages"
    | path -> path

let requireFile dir path =
    // Linux filesystems are (in most cases) case-sensitive.
    // However when nuget packages are installed to $HOME/.nuget/packages, it seems they are lowercased
    let fullPath = (dir ++ path)
    match Commands.fileExists __SOURCE_DIRECTORY__ fullPath with
    | Some _ -> fullPath
    | None ->
        let fullPathLower = (dir ++ path.ToLower())
        match Commands.fileExists __SOURCE_DIRECTORY__ fullPathLower with
        | Some _ -> fullPathLower
        | None -> failwith (sprintf "Couldn't find \"%s\" on the following paths: \"%s\", \"%s\". Running 'build test' once might solve this issue" path fullPath fullPathLower)

let config configurationName envVars =
    let SCRIPT_ROOT = __SOURCE_DIRECTORY__
    let fsharpCoreArchitecture = "netstandard2.0"
    let fsharpBuildArchitecture = "netstandard2.0"
    let fsharpCompilerInteractiveSettingsArchitecture = "netstandard2.0"
    let dotnetArchitecture = "net9.0"
#if NET472
    let fscArchitecture = "net472"
    let fsiArchitecture = "net472"
    //let peverifyArchitecture = "net472"
#else
    let fscArchitecture = dotnetArchitecture
    let fsiArchitecture = dotnetArchitecture
    //let peverifyArchitecture = dotnetArchitecture
#endif
    let repoRoot = SCRIPT_ROOT ++ ".." ++ ".."
    let artifactsPath = repoRoot ++ "artifacts"
    let artifactsBinPath = artifactsPath ++ "bin"
    let coreClrRuntimePackageVersion = "5.0.0-preview.7.20364.11"
    let csc_flags = "/nologo"
    let vbc_flags = "/nologo"
    let fsc_flags = "-r:System.Core.dll --nowarn:20 --define:COMPILED --preferreduilang:en-US" 
    let fsi_flags = "-r:System.Core.dll --nowarn:20 --define:INTERACTIVE --maxerrors:1 --abortonerror --preferreduilang:en-US"
    let operatingSystem = getOperatingSystem ()
    let Is64BitOperatingSystem = DotnetPlatform.Is64BitOperatingSystem envVars
    let architectureMoniker = if Is64BitOperatingSystem then "x64" else "x86"
    let packagesDir = getPackagesDir ()
    let requirePackage = requireFile packagesDir
    let requireArtifact = requireFile artifactsBinPath
    let CSC = requirePackage ("Microsoft.Net.Compilers" ++ "4.3.0-1.22220.8" ++ "tools" ++ "csc.exe")
    let VBC = requirePackage ("Microsoft.Net.Compilers" ++ "4.3.0-1.22220.8" ++ "tools" ++ "vbc.exe")
    let ILDASM_EXE = if operatingSystem = "win" then "ildasm.exe" else "ildasm"
    let ILDASM = requirePackage (("runtime." + operatingSystem + "-" + architectureMoniker + ".Microsoft.NETCore.ILDAsm") ++ coreClrRuntimePackageVersion ++ "runtimes" ++ (operatingSystem + "-" + architectureMoniker) ++ "native" ++ ILDASM_EXE)
    let ILASM_EXE = if operatingSystem = "win" then "ilasm.exe" else "ilasm"
    let ILASM = requirePackage (("runtime." + operatingSystem + "-" + architectureMoniker + ".Microsoft.NETCore.ILAsm") ++ coreClrRuntimePackageVersion ++ "runtimes" ++ (operatingSystem + "-" + architectureMoniker) ++ "native" ++ ILASM_EXE)
    //let PEVERIFY_EXE = if operatingSystem = "win" then "PEVerify.exe" elif operatingSystem = "osx" then "PEVerify.dll" else "PEVerify"
    let PEVERIFY = "ilverify" //requireArtifact ("PEVerify" ++ configurationName ++ peverifyArchitecture ++ PEVERIFY_EXE)
//    let FSI_FOR_SCRIPTS = artifactsBinPath ++ "fsi" ++ configurationName ++ fsiArchitecture ++ "fsi.exe"
    let FSharpBuild = requireArtifact ("FSharp.Build" ++ configurationName ++ fsharpBuildArchitecture ++ "FSharp.Build.dll")
    let FSharpCompilerInteractiveSettings = requireArtifact ("FSharp.Compiler.Interactive.Settings" ++ configurationName ++ fsharpCompilerInteractiveSettingsArchitecture ++ "FSharp.Compiler.Interactive.Settings.dll")

    let dotNetExe =
        // first look for {repoRoot}\.dotnet\dotnet.exe, otherwise fallback to %PATH%
        let DOTNET_EXE = if operatingSystem = "win" then "dotnet.exe" else "dotnet"
        let repoLocalDotnetPath = repoRoot ++ ".dotnet" ++ DOTNET_EXE
        if FileSystem.FileExistsShim(repoLocalDotnetPath) then repoLocalDotnetPath
        else DOTNET_EXE

#if !NETCOREAPP
    let FSI_PATH = ("fsi" ++ configurationName ++ fsiArchitecture ++ "fsi.exe")
#else
    let FSI_PATH = ("fsi" ++ configurationName ++ fsiArchitecture ++ "fsi.dll")
#endif
    let FSI_FOR_SCRIPTS = requireArtifact FSI_PATH
    let FSI = requireArtifact FSI_PATH
#if !NETCOREAPP
    let FSC = requireArtifact ("fsc" ++ configurationName ++ fscArchitecture ++ "fsc.exe")
    let FSIANYCPU = requireArtifact ("fsiAnyCpu" ++ configurationName ++ "net472" ++ "fsiAnyCpu.exe")
    let FSCANYCPU = requireArtifact ("fscAnyCpu" ++ configurationName ++ fscArchitecture ++ "fscAnyCpu.exe")
#else
    let FSC = requireArtifact ("fsc" ++ configurationName ++ fscArchitecture ++ "fsc.dll")
#endif
#if !NETCOREAPP
    let SN = requirePackage ("sn" ++ "1.0.0" ++ "sn.exe")
#endif
    let FSCOREDLLPATH = requireArtifact ("FSharp.Core" ++ configurationName ++ fsharpCoreArchitecture ++ "FSharp.Core.dll")
    let DOTNETFSCCOMPILERPATH = requireArtifact ("fsc" ++ configurationName ++ dotnetArchitecture ++ "fsc.dll")
    let defaultPlatform =
        match Is64BitOperatingSystem with
//        | PlatformID.MacOSX, true -> "osx.10.10-x64"
//        | PlatformID.Unix,true -> "ubuntu.14.04-x64"
        | true -> "win7-x64"
        | false -> "win7-x86"

    { EnvironmentVariables = envVars
      FSCOREDLLPATH = FSCOREDLLPATH
      ILDASM = ILDASM
      ILASM = ILASM
      PEVERIFY = PEVERIFY
      VBC = VBC
      CSC = CSC
      BUILD_CONFIG = configurationName
      FSC = FSC
      FSI = FSI
#if !NETCOREAPP
      FSCANYCPU = FSCANYCPU
      FSIANYCPU = FSIANYCPU
      SN = SN
#endif
      DOTNETFSCCOMPILERPATH = DOTNETFSCCOMPILERPATH
      FSI_FOR_SCRIPTS = FSI_FOR_SCRIPTS
      FSharpBuild = FSharpBuild
      FSharpCompilerInteractiveSettings = FSharpCompilerInteractiveSettings
      csc_flags = csc_flags
      fsc_flags = fsc_flags
      fsi_flags = fsi_flags
      vbc_flags = vbc_flags
      Directory=""
      DotNetExe = dotNetExe
      DotNetMultiLevelLookup = System.Environment.GetEnvironmentVariable "DOTNET_MULTILEVEL_LOOKUP"
      DotNetRoot = System.Environment.GetEnvironmentVariable "DOTNET_ROOT"
      DefaultPlatform = defaultPlatform }

let logConfig (cfg: TestConfig) =
    log "---------------------------------------------------------------"
    log "Executables"
    log ""
    log "CSC                      = %s" cfg.CSC
    log "BUILD_CONFIG             = %s" cfg.BUILD_CONFIG
    log "csc_flags                = %s" cfg.csc_flags
    log "FSC                      = %s" cfg.FSC
    log "fsc_flags                = %s" cfg.fsc_flags
    log "FSCOREDLLPATH            = %s" cfg.FSCOREDLLPATH
    log "FSI                      = %s" cfg.FSI
#if NETCOREAPP
    log "DotNetExe                = %s" cfg.DotNetExe
    log "DOTNET_MULTILEVEL_LOOKUP = %s" cfg.DotNetMultiLevelLookup
    log "DOTNET_ROOT              = %s" cfg.DotNetRoot
#else
    log "FSIANYCPU                = %s" cfg.FSIANYCPU
    log "FSCANYCPU                = %s" cfg.FSCANYCPU
    log "SN                       = %s" cfg.SN
#endif
    log "FSI_FOR_SCRIPTS          = %s" cfg.FSI_FOR_SCRIPTS
    log "fsi_flags                = %s" cfg.fsi_flags
    log "ILDASM                   = %s" cfg.ILDASM
    log "PEVERIFY                 = %s" cfg.PEVERIFY
    log "---------------------------------------------------------------"

let outputPassed (output: string) = output.Contains "TEST PASSED OK"

let checkResultPassed result =
    match result with
    | CmdResult.ErrorLevel (msg1, err) -> Assert.Fail (sprintf "%s. ERRORLEVEL %d" msg1 err)
    | CmdResult.Success output -> Assert.True(outputPassed output, "Output does not contain 'TEST PASSED OK'")

let checkResult result =
    match result with
    | CmdResult.ErrorLevel (msg1, err) -> Assert.Fail (sprintf "%s. ERRORLEVEL %d" msg1 err)
    | CmdResult.Success _ -> ()

let checkErrorLevel1 result =
    match result with
    | CmdResult.ErrorLevel (_,1) -> ()
    | CmdResult.Success _ | CmdResult.ErrorLevel _ -> Assert.Fail (sprintf "Command passed unexpectedly")

let envVars () =
    System.Environment.GetEnvironmentVariables ()
    |> Seq.cast<System.Collections.DictionaryEntry>
    |> Seq.map (fun d -> d.Key :?> string, d.Value :?> string)
    |> Map.ofSeq

let initialConfig =

#if DEBUG
    let configurationName = "Debug"
#else
    let configurationName = "Release"
#endif
    let env = envVars ()

    let cfg =
        let c = config configurationName env
        let usedEnvVars = c.EnvironmentVariables  |> Map.add "FSC" c.FSC
        { c with EnvironmentVariables = usedEnvVars }

    cfg

let testConfig sourceDir (relativePathToTestFixture: string) =
    let testFixtureFullPath = Path.GetFullPath(sourceDir ++ relativePathToTestFixture)

    let tempTestDir =
        createTemporaryDirectory()
            .CreateSubdirectory(relativePathToTestFixture)
            .FullName
    copyDirectory testFixtureFullPath tempTestDir true

    { initialConfig with Directory = tempTestDir }

let createConfigWithEmptyDirectory() =
    { initialConfig with Directory = createTemporaryDirectory().FullName }

type RedirectToType =
    | Overwrite of FilePath
    | Append of FilePath

type RedirectTo =
    | Ignore
    | Collect
    | Output of RedirectToType
    | OutputAndError of RedirectToType * RedirectToType
    | OutputAndErrorToSameFile of RedirectToType
    | Error of RedirectToType

type RedirectFrom =
    | RedirectInput of FilePath

type RedirectInfo =
    { Output : RedirectTo
      Input : RedirectFrom option }


module Command =

    let logExec _dir path args redirect =
        let inF =
            function
            | None -> ""
            | Some(RedirectInput l) -> sprintf " <%s" l
        let redirectType = function Overwrite x -> sprintf ">%s" x | Append x -> sprintf ">>%s" x
        let outF =
            function
            | Ignore | Collect -> ""
            | Output r -> sprintf " 1%s" (redirectType r)
            | OutputAndError (r1, r2) -> sprintf " 1%s 2%s" (redirectType r1)  (redirectType r2)
            | OutputAndErrorToSameFile r -> sprintf " 1%s 2>1" (redirectType r)
            | Error r -> sprintf " 2%s" (redirectType r)
        sprintf "%s%s%s%s" path (match args with "" -> "" | x -> " " + x) (inF redirect.Input) (outF redirect.Output)

    let exec dir envVars (redirect:RedirectInfo) path args =

        let inputWriter sources (writer: StreamWriter) =
            let pipeFile name = async {
                let path = Commands.getfullpath dir name
                use reader = File.OpenRead (path)
                use ms = new MemoryStream()
                do! reader.CopyToAsync (ms) |> (Async.AwaitIAsyncResult >> Async.Ignore)
                ms.Position <- 0L
                try
                    do! ms.CopyToAsync(writer.BaseStream) |> (Async.AwaitIAsyncResult >> Async.Ignore)
                    do! writer.FlushAsync() |> (Async.AwaitIAsyncResult >> Async.Ignore)
                with
                | :? System.IO.IOException -> //input closed is ok if process is closed
                    ()
                }
            sources |> pipeFile |> Async.RunSynchronously

        let inF fCont cmdArgs =
            match redirect.Input with
            | None -> fCont cmdArgs
            | Some(RedirectInput l) -> fCont { cmdArgs with RedirectInput = Some (inputWriter l) }

        let openWrite rt =
            let fullpath = Commands.getfullpath dir
            match rt with
            | Append p -> File.AppendText( p |> fullpath)
            | Overwrite p -> new StreamWriter(new FileStream(p |> fullpath, FileMode.Create))

        let outF fCont cmdArgs =
            match redirect.Output with
            | Ignore ->
                fCont { cmdArgs with RedirectOutput = Some ignore; RedirectError = Some ignore }
            | Collect ->
                use out = redirectTo (new StringWriter())
                use error = redirectTo (new StringWriter())
                fCont { cmdArgs with RedirectOutput = Some out.Post; RedirectError = Some error.Post }
            | Output r ->
                use writer = openWrite r
                use outFile = redirectTo writer
                fCont { cmdArgs with RedirectOutput = Some (outFile.Post); RedirectError = Some ignore }
            | OutputAndError (r1,r2) ->
                use writer1 = openWrite r1
                use writer2 = openWrite r2
                use outFile1 = redirectTo writer1
                use outFile2 = redirectTo writer2
                fCont { cmdArgs with RedirectOutput = Some (outFile1.Post); RedirectError = Some (outFile2.Post) }
            | OutputAndErrorToSameFile r ->
                use writer = openWrite r
                use outFile = redirectTo writer
                fCont { cmdArgs with RedirectOutput = Some (outFile.Post); RedirectError = Some (outFile.Post) }
            | Error r ->
                use writer = openWrite r
                use outFile = redirectTo writer
                fCont { cmdArgs with RedirectOutput = Some ignore; RedirectError = Some (outFile.Post) }

        let exec cmdArgs =
            log "%s" (logExec dir path args redirect)
            Process.exec cmdArgs dir envVars path args

        { RedirectOutput = None; RedirectError = None; RedirectInput = None }
        |> (outF (inF exec))

let alwaysSuccess _ = ()

let execArgs = { Output = Ignore; Input = None; }
let execAppend cfg stdoutPath stderrPath p = Command.exec cfg.Directory cfg.EnvironmentVariables { execArgs with Output = OutputAndError(Append(stdoutPath), Append(stderrPath)) } p >> checkResult
let execAppendIgnoreExitCode cfg stdoutPath stderrPath p = Command.exec cfg.Directory cfg.EnvironmentVariables { execArgs with Output = OutputAndError(Append(stdoutPath), Append(stderrPath)) } p >> alwaysSuccess
let exec cfg p = Command.exec cfg.Directory cfg.EnvironmentVariables execArgs p >> checkResult
let execAndCheckPassed cfg p = Command.exec cfg.Directory cfg.EnvironmentVariables { execArgs with Output = Collect } p >> checkResultPassed
let execExpectFail cfg p = Command.exec cfg.Directory cfg.EnvironmentVariables execArgs p >> checkErrorLevel1
let execIn cfg workDir p = Command.exec workDir cfg.EnvironmentVariables execArgs p >> checkResult
let execBothToOutNoCheck cfg workDir outFile p = Command.exec workDir cfg.EnvironmentVariables { execArgs with Output = OutputAndErrorToSameFile(Overwrite(outFile)) } p
let execBothToOut cfg workDir outFile p = execBothToOutNoCheck cfg workDir outFile p >> checkResult
let execBothToOutCheckPassed cfg workDir outFile p = execBothToOutNoCheck cfg workDir outFile p >> checkResultPassed
let execBothToOutExpectFail cfg workDir outFile p = execBothToOutNoCheck cfg workDir outFile p >> checkErrorLevel1
let execAppendOutIgnoreExitCode cfg workDir outFile p = Command.exec workDir  cfg.EnvironmentVariables { execArgs with Output = Output(Append(outFile)) } p >> alwaysSuccess
let execAppendErrExpectFail cfg errPath p = Command.exec cfg.Directory cfg.EnvironmentVariables { execArgs with Output = Error(Overwrite(errPath)) } p >> checkErrorLevel1
let execStdin cfg l p = Command.exec cfg.Directory cfg.EnvironmentVariables { Output = Ignore; Input = Some(RedirectInput(l)) } p >> checkResult
let execStdinCheckPassed cfg l p = Command.exec cfg.Directory cfg.EnvironmentVariables { Output = Collect; Input = Some(RedirectInput(l)) } p >> checkResultPassed
let execStdinAppendBothIgnoreExitCode cfg stdoutPath stderrPath stdinPath p = Command.exec cfg.Directory cfg.EnvironmentVariables { Output = OutputAndError(Append(stdoutPath), Append(stderrPath)); Input = Some(RedirectInput(stdinPath)) } p >> alwaysSuccess
let fsc cfg arg = Printf.ksprintf (Commands.fsc cfg.Directory (exec cfg) cfg.DotNetExe cfg.FSC) arg
let fscIn cfg workDir arg = Printf.ksprintf (Commands.fsc workDir (execIn cfg workDir) cfg.DotNetExe  cfg.FSC) arg
let fscAppend cfg stdoutPath stderrPath arg = Printf.ksprintf (Commands.fsc cfg.Directory (execAppend cfg stdoutPath stderrPath) cfg.DotNetExe  cfg.FSC) arg
let fscAppendIgnoreExitCode cfg stdoutPath stderrPath arg = Printf.ksprintf (Commands.fsc cfg.Directory (execAppendIgnoreExitCode cfg stdoutPath stderrPath) cfg.DotNetExe  cfg.FSC) arg
let fscBothToOut cfg out arg = Printf.ksprintf (Commands.fsc cfg.Directory (execBothToOut cfg cfg.Directory out) cfg.DotNetExe  cfg.FSC) arg
let fscBothToOutExpectFail cfg out arg = Printf.ksprintf (Commands.fsc cfg.Directory (execBothToOutExpectFail cfg cfg.Directory out) cfg.DotNetExe  cfg.FSC) arg
let fscAppendErrExpectFail cfg errPath arg = Printf.ksprintf (Commands.fsc cfg.Directory (execAppendErrExpectFail cfg errPath) cfg.DotNetExe  cfg.FSC) arg
let csc cfg arg = Printf.ksprintf (Commands.csc (exec cfg) cfg.CSC) arg
let vbc cfg arg = Printf.ksprintf (Commands.vbc (exec cfg) cfg.VBC) arg
let ildasm cfg arg = Printf.ksprintf (Commands.ildasm (exec cfg) cfg.ILDASM) arg
let ilasm cfg arg = Printf.ksprintf (Commands.ilasm (exec cfg) cfg.ILASM) arg
let peverify _cfg _test = printfn "PEVerify is disabled, need to migrate to ILVerify instead, see https://github.com/dotnet/fsharp/issues/13854" //Commands.peverify (exec cfg) cfg.PEVERIFY "/nologo"
let peverifyWithArgs _cfg _args _test = printfn "PEVerify is disabled, need to migrate to ILVerify instead, see https://github.com/dotnet/fsharp/issues/13854" //Commands.peverify (exec cfg) cfg.PEVERIFY args
let fsi cfg = Printf.ksprintf (Commands.fsi (exec cfg) cfg.FSI)
let fsiCheckPassed cfg = Printf.ksprintf (Commands.fsi (execAndCheckPassed cfg) cfg.FSI)
#if !NETCOREAPP
let fsiAnyCpu cfg = Printf.ksprintf (Commands.fsi (exec cfg) cfg.FSIANYCPU)
let sn cfg = Printf.ksprintf (Commands.sn (exec cfg) cfg.SN)
#endif
let fsi_script cfg = Printf.ksprintf (Commands.fsi (exec cfg) cfg.FSI_FOR_SCRIPTS)
let fsiExpectFail cfg = Printf.ksprintf (Commands.fsi (execExpectFail cfg) cfg.FSI)
let fsiAppendIgnoreExitCode cfg stdoutPath stderrPath = Printf.ksprintf (Commands.fsi (execAppendIgnoreExitCode cfg stdoutPath stderrPath) cfg.FSI)
let getfullpath cfg = Commands.getfullpath cfg.Directory
let fileExists cfg fileName = Commands.fileExists cfg.Directory fileName |> Option.isSome
let fsiStdin cfg stdinPath = Printf.ksprintf (Commands.fsi (execStdin cfg stdinPath) cfg.FSI)
let fsiStdinCheckPassed cfg stdinPath = Printf.ksprintf (Commands.fsi (execStdinCheckPassed cfg stdinPath) cfg.FSI)
let fsiStdinAppendBothIgnoreExitCode cfg stdoutPath stderrPath stdinPath = Printf.ksprintf (Commands.fsi (execStdinAppendBothIgnoreExitCode cfg stdoutPath stderrPath stdinPath) cfg.FSI)
let rm cfg x = Commands.rm cfg.Directory x
let rmdir cfg x = Commands.rmdir cfg.Directory x
let mkdir cfg = Commands.mkdir_p cfg.Directory
let copy cfg fromFile toFile = Commands.copy cfg.Directory fromFile toFile |> checkResult
let copySystemValueTuple cfg = copy cfg (getDirectoryName(cfg.FSC) ++ "System.ValueTuple.dll") ("." ++ "System.ValueTuple.dll")

let diff normalize path1 path2 =
    let result = System.Text.StringBuilder()
    let append (s:string) = result.AppendLine s |> ignore
    let cwd = Directory.GetCurrentDirectory()

    if not <| FileSystem.FileExistsShim(path1) then
        // creating empty baseline file as this is likely someone initializing a new test
        File.WriteAllText(path1, String.Empty)
    if not <| FileSystem.FileExistsShim(path2) then failwithf "Invalid path %s" path2

    let lines1 = File.ReadAllLines(path1)
    let lines2 = File.ReadAllLines(path2)

    let minLines = min lines1.Length lines2.Length

    for i = 0 to (minLines - 1) do
        let normalizePath (line:string) =
            if normalize then
                let x = line.IndexOf(cwd, StringComparison.OrdinalIgnoreCase)
                if x >= 0 then line.Substring(x+cwd.Length) else line
            else line

        let line1 = lines1[i] |> normalizePath
        let line2 = lines2[i] |> normalizePath

        if line1 <> line2 then
            append <| sprintf "diff between [%s] and [%s]" path1 path2
            append <| sprintf "line %d" (i+1)
            append <| sprintf " - %s" line1
            append <| sprintf " + %s" line2

    if lines1.Length <> lines2.Length then
        append <| sprintf "diff between [%s] and [%s]" path1 path2
        append <| sprintf "diff at line %d" minLines
        lines1[minLines .. (lines1.Length - 1)] |> Array.iter (append << sprintf "- %s")
        lines2[minLines .. (lines2.Length - 1)] |> Array.iter (append << sprintf "+ %s")

    result.ToString()

let fsdiff cfg a b =
    let actualFile = System.IO.Path.Combine(cfg.Directory, a)
    let expectedFile = System.IO.Path.Combine(cfg.Directory, b)
    let errorText = System.IO.File.ReadAllText (System.IO.Path.Combine(cfg.Directory, a))

    let result = diff false expectedFile actualFile
    if result <> "" then
        log "%s" result
        log "New error file:"
        log "%s" errorText

    result

let requireENCulture () =
    match System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName with
    | "en" -> true
    | _ -> false
