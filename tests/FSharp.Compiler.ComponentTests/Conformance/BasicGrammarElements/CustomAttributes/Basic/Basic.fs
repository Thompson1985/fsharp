// Copyright (c) Microsoft Corporation.  All Rights Reserved.  See License.txt in the project root for license information.

namespace Conformance.BasicGrammarElements

open Xunit
open FSharp.Test
open FSharp.Test.Compiler

module CustomAttributes_Basic =

    let verifyCompile compilation =
        compilation
        |> asExe
        |> withOptions ["--nowarn:988"]
        |> compile

    let verifyCompileAndRun compilation =
        compilation
        |> asExe
        |> withOptions ["--nowarn:988"]
        |> compileAndRun

    // SOURCE=ArrayParam.fs							# ArrayParam.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"ArrayParam.fs"|])>]
    let ``ArrayParam_fs`` compilation =
        compilation
        |> verifyCompileAndRun
        |> shouldSucceed

    // SOURCE=AttribWithEnumFlags01.fs						# AttribWithEnumFlags01.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"AttribWithEnumFlags01.fs"|])>]
    let ``AttribWithEnumFlags01_fs`` compilation =
        compilation
        |> verifyCompileAndRun
        |> shouldSucceed

    // SOURCE=E_AttributeApplication01.fs					# E_AttributeApplication01.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"E_AttributeApplication01.fs"|])>]
    let ``E_AttributeApplication01_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldFail
        |> withDiagnostics [
            (Error 841, Line 7, Col 3, Line 7, Col 111, "This attribute is not valid for use on this language element. Assembly attributes should be attached to a 'do ()' declaration, if necessary within an F# module.")
        ]

    // SOURCE=E_AttributeApplication02.fs     SCFLAGS="--test:ErrorRanges"	# E_AttributeApplication02.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"E_AttributeApplication02.fs"|])>]
    let ``E_AttributeApplication02_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldFail
        |> withDiagnostics [
            (Error 429, Line 6, Col 15, Line 6, Col 22, "The attribute type 'MeasureAttribute' has 'AllowMultiple=false'. Multiple instances of this attribute cannot be attached to a single language element.")
        ]

    // SOURCE=E_AttributeApplication03.fs     SCFLAGS="--test:ErrorRanges"	# E_AttributeApplication03.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"E_AttributeApplication03.fs"|])>]
    let ``E_AttributeApplication03_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldFail
        |> withDiagnostics [
            (Warning 842, Line 15, Col 7, Line 15, Col 17, "This attribute cannot be applied to property, event, return value. Valid targets are: parameter")
        ]

    // SOURCE=E_AttributeApplication04.fs     SCFLAGS="--test:ErrorRanges"	# E_AttributeApplication04.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"E_AttributeApplication04.fs"|])>]
    let ``E_AttributeApplication04_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldFail
        |> withDiagnostics [
            (Warning 842, Line 14, Col 3, Line 14, Col 13, "This attribute cannot be applied to class, struct, enum, interface, delegate. Valid targets are: parameter")
            (Warning 842, Line 14, Col 3, Line 14, Col 13, "This attribute cannot be applied to class. Valid targets are: parameter")
        ]

    // SOURCE=E_AttributeApplication05.fs     SCFLAGS="--test:ErrorRanges"	# E_AttributeApplication05.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"E_AttributeApplication05.fs"|])>]
    let ``E_AttributeApplication05_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldFail
        |> withDiagnostics [
            (Warning 842, Line 8, Col 7, Line 8, Col 8, "This attribute is not valid for use on this language element")
            (Error 824, Line 8, Col 7, Line 8, Col 8, "Attributes are not permitted on 'let' bindings in expressions")
            (Warning 20, Line 8, Col 1, Line 8, Col 31, "The result of this expression has type 'int' and is implicitly ignored. Consider using 'ignore' to discard this value explicitly, e.g. 'expr |> ignore', or 'let' to bind the result to a name, e.g. 'let result = expr'.")
        ]

    // SOURCE=E_AttributeApplication06.fs     SCFLAGS="--test:ErrorRanges"	# E_AttributeApplication06.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"E_AttributeApplication06.fs"|])>]
    let ``E_AttributeApplication06_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldFail
        |> withDiagnostics [
            (Error 824, Line 8, Col 13, Line 8, Col 14, "Attributes are not permitted on 'let' bindings in expressions")
            (Warning 20, Line 8, Col 28, Line 8, Col 41, "The result of this expression has type 'int' and is implicitly ignored. Consider using 'ignore' to discard this value explicitly, e.g. 'expr |> ignore', or 'let' to bind the result to a name, e.g. 'let result = expr'.")
            (Error 824, Line 10, Col 14, Line 10, Col 15, "Attributes are not permitted on 'let' bindings in expressions")
            (Error 824, Line 10, Col 26, Line 10, Col 27, "Attributes are not permitted on 'let' bindings in expressions")
            (Warning 20, Line 8, Col 1, Line 10, Col 60, "The result of this expression has type 'int' and is implicitly ignored. Consider using 'ignore' to discard this value explicitly, e.g. 'expr |> ignore', or 'let' to bind the result to a name, e.g. 'let result = expr'.")
        ]

    // SOURCE=E_AttributeApplication07.fs					# E_AttributeApplication07.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"E_AttributeApplication07.fs"|])>]
    let ``E_AttributeApplication07_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldFail
        |> withDiagnostics [
            (Error 1, Line 10, Col 3, Line 10, Col 59, "This expression was expected to have type\n    'int array'    \nbut here has type\n    'unit'    ")
            (Error 267, Line 10, Col 3, Line 10, Col 59, "This is not a valid constant expression or custom attribute value")
            (Error 850, Line 10, Col 3, Line 10, Col 59, "This attribute cannot be used in this version of F#")
            (Error 850, Line 13, Col 3, Line 13, Col 101, "This attribute cannot be used in this version of F#")
            (Error 850, Line 16, Col 3, Line 16, Col 50, "This attribute cannot be used in this version of F#")
        ]

    // SOURCE=E_AttributeTargetSpecifications.fs						# E_AttributeTargetSpecifications.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"E_AttributeTargetSpecifications.fs"|])>]
    let ``E_AttributeTargetSpecifications_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldFail
        |> withDiagnostics [
            (Error 840, Line 9, Col 3, Line 9, Col 34, "Unrecognized attribute target. Valid attribute targets are 'assembly', 'module', 'type', 'method', 'property', 'return', 'param', 'field', 'event', 'constructor'.")
        ]

    // SOURCE=E_StructLayout.fs SCFLAGS="-a --test:ErrorRanges --flaterrors  --nowarn:9"	# E_StructLayout.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"E_StructLayout.fs"|])>]
    let ``E_StructLayout_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldFail
        |> withDiagnostics [
            (Warning 9, Line 14, Col 6, Line 14, Col 21, "Uses of this construct may result in the generation of unverifiable .NET IL code. This warning can be disabled using '--nowarn:9' or '#nowarn \"9\"'.")
            (Error 1206, Line 12, Col 1, Line 13, Col 1, "The type 'SExplicitBroken' has been marked as having an Explicit layout, but the field 'v2' has not been marked with the 'FieldOffset' attribute")
            (Error 1211, Line 22, Col 1, Line 23, Col 1, "The FieldOffset attribute can only be placed on members of types marked with the StructLayout(LayoutKind.Explicit)")
        ]

    // SOURCE=E_StructLayoutSequentialNeg_AbstractClass.fs SCFLAGS="--test:ErrorRanges"	# E_StructLayoutSequentialNeg_AbstractClass.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"E_StructLayoutSequentialNeg_AbstractClass.fs"|])>]
    let ``E_StructLayoutSequentialNeg_AbstractClass_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldFail
        |> withDiagnostics [
            (Error 937, Line 9, Col 10, Line 9, Col 12, "Only structs and classes without primary constructors may be given the 'StructLayout' attribute")
        ]

    // SOURCE=E_StructLayoutSequentialNeg_DU1.fs SCFLAGS="--test:ErrorRanges"			# E_StructLayoutSequentialNeg_DU1.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"E_StructLayoutSequentialNeg_DU1.fs"|])>]
    let ``E_StructLayoutSequentialNeg_DU1_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldFail
        |> withDiagnostics [
            (Error 937, Line 9, Col 10, Line 9, Col 12, "Only structs and classes without primary constructors may be given the 'StructLayout' attribute")
        ]

    // SOURCE=E_StructLayoutSequentialNeg_DU2.fs SCFLAGS="--test:ErrorRanges"			# E_StructLayoutSequentialNeg_DU2.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"E_StructLayoutSequentialNeg_DU2.fs"|])>]
    let ``E_StructLayoutSequentialNeg_DU2_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldFail
        |> withDiagnostics [
            (Error 937, Line 9, Col 10, Line 9, Col 12, "Only structs and classes without primary constructors may be given the 'StructLayout' attribute")
        ]

    // SOURCE=E_StructLayoutSequentialNeg_Delegate.fs SCFLAGS="--test:ErrorRanges"		# E_StructLayoutSequentialNeg_Delegate.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"E_StructLayoutSequentialNeg_Delegate.fs"|])>]
    let ``E_StructLayoutSequentialNeg_Delegate_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldFail
        |> withDiagnostics [
            (Error 937, Line 9, Col 10, Line 9, Col 12, "Only structs and classes without primary constructors may be given the 'StructLayout' attribute")
        ]

    // SOURCE=E_StructLayoutSequentialNeg_Interface.fs SCFLAGS="--test:ErrorRanges"		# E_StructLayoutSequentialNeg_Interface.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"E_StructLayoutSequentialNeg_Interface.fs"|])>]
    let ``E_StructLayoutSequentialNeg_Interface_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldFail
        |> withDiagnostics [
            (Error 937, Line 8, Col 10, Line 8, Col 12, "Only structs and classes without primary constructors may be given the 'StructLayout' attribute")
        ]

    // SOURCE=E_UseNullAsTrueValue01.fs     SCFLAGS="--test:ErrorRanges"	# E_UseNullAsTrueValue01.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"E_UseNullAsTrueValue01.fs"|])>]
    let ``E_UseNullAsTrueValue01_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldFail
        |> withDiagnostics [
            (Error 1196, Line 15, Col 6, Line 15, Col 13, "The 'UseNullAsTrueValue' attribute flag may only be used with union types that have one nullary case and at least one non-nullary case")
            (Error 1196, Line 22, Col 6, Line 22, Col 14, "The 'UseNullAsTrueValue' attribute flag may only be used with union types that have one nullary case and at least one non-nullary case")
            (Error 1196, Line 28, Col 6, Line 28, Col 14, "The 'UseNullAsTrueValue' attribute flag may only be used with union types that have one nullary case and at least one non-nullary case")
            (Error 1196, Line 33, Col 6, Line 33, Col 15, "The 'UseNullAsTrueValue' attribute flag may only be used with union types that have one nullary case and at least one non-nullary case")
            (Error 1196, Line 37, Col 6, Line 37, Col 14, "The 'UseNullAsTrueValue' attribute flag may only be used with union types that have one nullary case and at least one non-nullary case")
            (Error 1196, Line 44, Col 6, Line 44, Col 18, "The 'UseNullAsTrueValue' attribute flag may only be used with union types that have one nullary case and at least one non-nullary case")
            (Error 1196, Line 51, Col 6, Line 51, Col 15, "The 'UseNullAsTrueValue' attribute flag may only be used with union types that have one nullary case and at least one non-nullary case")
        ]

    // SOURCE=FreeTypeVariable01.fs						# FreeTypeVariable01.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"FreeTypeVariable01.fs"|])>]
    let ``FreeTypeVariable01_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldFail
        |> withDiagnostics [
            (Warning 64, Line 14, Col 81, Line 14, Col 83, "This construct causes code to be less generic than indicated by the type annotations. The type variable 'a has been constrained to be type 'obj'.")
        ]

    // SOURCE=Function01.fs							# Function01.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"Function01.fs"|])>]
    let ``Function01_fs`` compilation =
        compilation
        |> verifyCompileAndRun
        |> shouldSucceed

    // SOURCE=FunctionArg01.fs							# FunctionArg01.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"FunctionArg01.fs"|])>]
    let ``FunctionArg01_fs`` compilation =
        compilation
        |> verifyCompileAndRun
        |> shouldSucceed

    // SOURCE=InExternDecl.fs SCFLAGS="-a --warnaserror+"			# InExternDecl.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"InExternDecl.fs"|])>]
    let ``InExternDecl_fs`` compilation =
        compilation
        |> verifyCompileAndRun
        |> shouldSucceed

    // SOURCE=ParamArrayAttrUsage.fs   							# ParamArrayAttrUsage.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"ParamArrayAttrUsage.fs"|])>]
    let ``ParamArrayAttrUsage_fs`` compilation =
        compilation
        |> asFsx
        |> verifyCompileAndRun
        |> shouldSucceed

    // SOURCE=ReturnType01.fs							# ReturnType01.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"ReturnType01.fs"|])>]
    let ``ReturnType01_fs`` compilation =
        compilation
        |> verifyCompileAndRun
        |> shouldSucceed

    // SOURCE=ReturnType02.fs							# ReturnType02.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"ReturnType02.fs"|])>]
    let ``ReturnType02_fs`` compilation =
        compilation
        |> verifyCompileAndRun
        |> shouldSucceed

    // SOURCE=ReturnType03.fs							# ReturnType03.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"ReturnType03.fs"|])>]
    let ``ReturnType03_fs`` compilation =
        compilation
        |> verifyCompileAndRun
        |> shouldSucceed

    // SOURCE=SanityCheck01.fs							# SanityCheck01.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"SanityCheck01.fs"|])>]
    let ``SanityCheck01_fs`` compilation =
        compilation
        |> verifyCompileAndRun
        |> shouldSucceed

    // SOURCE=StructLayout.fs   SCFLAGS="-a --nowarn:9 --warnaserror"				# StructLayout.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"StructLayout.fs"|])>]
    let ``StructLayout_fs`` compilation =
        compilation
        |> asLibrary
        |> withOptions ["--nowarn:988"]
        |> compile
        |> shouldFail
        |> withDiagnostics [
            (Warning 9, Line 19, Col 6, Line 19, Col 15, "Uses of this construct may result in the generation of unverifiable .NET IL code. This warning can be disabled using '--nowarn:9' or '#nowarn \"9\"'.")
        ]

    // SOURCE=StructLayoutSequentialPos_Exception.fs						# StructLayoutSequentialPos_Exception.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"StructLayoutSequentialPos_Exception.fs"|])>]
    let ``StructLayoutSequentialPos_Exception_fs`` compilation =
        compilation
        |> verifyCompileAndRun
        |> shouldSucceed

    // SOURCE=TypeofTypedefofInAttribute.fs					# TypeofTypedefofInAttribute.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"TypeofTypedefofInAttribute.fs"|])>]
    let ``TypeofTypedefofInAttribute_fs`` compilation =
        compilation
        |> verifyCompileAndRun
        |> shouldSucceed

    // SOURCE=TypesAsAttrArgs01.fs						# TypesAsAttrArgs01
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"TypesAsAttrArgs01.fs"|])>]
    let ``TypesAsAttrArgs01_fs`` compilation =
        compilation
        |> verifyCompileAndRun
        |> shouldSucceed

    // SOURCE=W_ReturnType03b.fs          SCFLAGS="--test:ErrorRanges"		# W_ReturnType03b.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"W_ReturnType03b.fs"|])>]
    let ``W_ReturnType03b_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldFail
        |> withDiagnostics [
            (Error 429, Line 16, Col 28, Line 16, Col 37, "The attribute type 'CA1' has 'AllowMultiple=false'. Multiple instances of this attribute cannot be attached to a single language element.")
        ]

    // SOURCE=W_StructLayoutExplicit01.fs   SCFLAGS="--test:ErrorRanges" PEVER="/Exp_Fail"	# W_StructLayoutExplicit01.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"W_StructLayoutExplicit01.fs"|])>]
    let ``W_StructLayoutExplicit01_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldFail
        |> withDiagnostics [
            (Warning 9, Line 12, Col 6, Line 12, Col 7, "Uses of this construct may result in the generation of unverifiable .NET IL code. This warning can be disabled using '--nowarn:9' or '#nowarn \"9\"'.")
        ]

    // SOURCE=W_StructLayoutSequentialPos_AbstractClass.fs SCFLAGS="--test:ErrorRanges --warnaserror+"	# W_StructLayoutSequentialPos_AbstractClass.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"W_StructLayoutSequentialPos_AbstractClass.fs"|])>]
    let ``W_StructLayoutSequentialPos_AbstractClass_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldSucceed

    // SOURCE=W_StructLayoutSequentialPos_ClassnoCtr.fs SCFLAGS="--test:ErrorRanges --warnaserror+"		# W_StructLayoutSequentialPos_ClassnoCtr.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"W_StructLayoutSequentialPos_ClassnoCtr.fs"|])>]
    let ``W_StructLayoutSequentialPos_ClassnoCtr_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldSucceed

    // SOURCE=W_StructLayoutSequentialPos_Record.fs SCFLAGS="--test:ErrorRanges --warnaserror+"		# W_StructLayoutSequentialPos_Record.fs
    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"W_StructLayoutSequentialPos_Record.fs"|])>]
    let ``W_StructLayoutSequentialPos_Record_fs`` compilation =
        compilation
        |> verifyCompile
        |> shouldSucceed

    [<Theory; Directory(__SOURCE_DIRECTORY__, Includes=[|"EnsureValidCustomAttributeBlob.fs"|])>]
    let ``EnsureValidCustomAttributeBlob_fs`` compilation =
        compilation
        |> verifyCompileAndRun
        |> shouldSucceed

    [<Fact>]
    let ``StructLayoutAttribute doesn't have size=1 for struct DUs with instance fields`` () =
        Fsx """
        [<Struct>] type Option<'T> = None | Some of 'T
        """
        |> compile
        |> shouldSucceed
        |> verifyIL [
        """
        .class sequential autochar serializable sealed nested public beforefieldinit Option`1<T>
        extends [runtime]System.ValueType
        implements class [runtime]System.IEquatable`1<valuetype Test/Option`1<!T>>,
                   [runtime]System.Collections.IStructuralEquatable,
                   class [runtime]System.IComparable`1<valuetype Test/Option`1<!T>>,
                   [runtime]System.IComparable,
                   [runtime]System.Collections.IStructuralComparable
  {
    .custom instance void [FSharp.Core]Microsoft.FSharp.Core.StructAttribute::.ctor() = ( 01 00 00 00 ) 
    .custom instance void [runtime]System.Diagnostics.DebuggerDisplayAttribute::.ctor(string) = ( 01 00 15 7B 5F 5F 44 65 62 75 67 44 69 73 70 6C   
                                                                                                  61 79 28 29 2C 6E 71 7D 00 00 )                   
    .custom instance void [FSharp.Core]Microsoft.FSharp.Core.CompilationMappingAttribute::.ctor(valuetype [FSharp.Core]Microsoft.FSharp.Core.SourceConstructFlags) = ( 01 00 01 00 00 00 00 00 ) 
    .class abstract auto ansi sealed nested public Tags<T>
          extends [runtime]System.Object
    {
      .field public static literal int32 None = int32(0x00000000)
      .field public static literal int32 Some = int32(0x00000001)
    } 
        """
        ]

    [<Fact>]
    let ``StructLayoutAttribute has size=1 for struct DUs with no instance fields`` () =
        Fsx """
        [<Struct>] type Option<'T> = None | Some
        """
        |> compile
        |> shouldSucceed
        |> verifyIL [
        """
        .class sequential autochar serializable sealed nested public beforefieldinit Option`1<T>
        extends [runtime]System.ValueType
        implements class [runtime]System.IEquatable`1<valuetype Test/Option`1<!T>>,
                   [runtime]System.Collections.IStructuralEquatable,
                   class [runtime]System.IComparable`1<valuetype Test/Option`1<!T>>,
                   [runtime]System.IComparable,
                   [runtime]System.Collections.IStructuralComparable
  {
    .pack 0
    .size 1
    .custom instance void [FSharp.Core]Microsoft.FSharp.Core.StructAttribute::.ctor() = ( 01 00 00 00 ) 
    .custom instance void [runtime]System.Diagnostics.DebuggerDisplayAttribute::.ctor(string) = ( 01 00 15 7B 5F 5F 44 65 62 75 67 44 69 73 70 6C   
                                                                                                  61 79 28 29 2C 6E 71 7D 00 00 )                   
    .custom instance void [FSharp.Core]Microsoft.FSharp.Core.CompilationMappingAttribute::.ctor(valuetype [FSharp.Core]Microsoft.FSharp.Core.SourceConstructFlags) = ( 01 00 01 00 00 00 00 00 ) 
    .class abstract auto ansi sealed nested public Tags<T>
          extends [runtime]System.Object
    {
      .field public static literal int32 None = int32(0x00000000)
      .field public static literal int32 Some = int32(0x00000001)
    } 
        """
        ]