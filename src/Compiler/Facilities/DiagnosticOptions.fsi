// Copyright (c) Microsoft Corporation.  All Rights Reserved.  See License.txt in the project root for license information.

// This file contains simple types related to diagnostics that are made public in the
// FSharp.Compiler.Service API but which are also used throughout the
// F# compiler.

namespace FSharp.Compiler.Diagnostics

[<RequireQualifiedAccess>]
type FSharpDiagnosticSeverity =
    | Hidden
    | Info
    | Warning
    | Error

type FSharpDiagnosticOptions =
    { WarnLevel: int
      GlobalWarnAsError: bool
      WarnOff: int list
      WarnOn: int list
      WarnAsError: int list
      WarnAsWarn: int list
      mutable WarnScopeData: obj option }

    static member Default: FSharpDiagnosticOptions

    member CheckXmlDocs: bool
