// Copyright (c) Microsoft Corporation.  All Rights Reserved.  See License.txt in the project root for license information.

// Extension typing, validation of extension types, etc.

module internal rec FSharp.Compiler.TypeProviders

#if !NO_TYPEPROVIDERS

open System
open System.Collections.Concurrent
open System.Collections.Generic
open Internal.Utilities.Library
open FSharp.Core.CompilerServices
open FSharp.Compiler.AbstractIL.IL
open FSharp.Compiler.Text

type TypeProviderDesignation = TypeProviderDesignation of string
type 'a ProvidedArray = ('a[]) MaybeNull

/// Raised when a type provider has thrown an exception.
exception ProvidedTypeResolution of range * exn

/// Raised when an type provider has thrown an exception.
exception ProvidedTypeResolutionNoRange of exn

/// Get the list of relative paths searched for type provider design-time components
val toolingCompatiblePaths: unit -> string list

/// Carries information about the type provider resolution environment.
type ResolutionEnvironment =
    {
        /// The folder from which an extension provider is resolving from. This is typically the project folder.
        ResolutionFolder: string

        /// Output file name
        OutputFile: string option

        /// Whether or not the --showextensionresolution flag was supplied to the compiler.
        ShowResolutionMessages: bool

        /// All referenced assemblies, including the type provider itself, and possibly other type providers.
        GetReferencedAssemblies: unit -> string[]

        /// The folder for temporary files
        TemporaryFolder: string
    }

/// Find and instantiate the set of ITypeProvider components for the given assembly reference
val GetTypeProvidersOfAssembly:
    runtimeAssemblyFilename: string *
    ilScopeRefOfRuntimeAssembly: ILScopeRef *
    designTimeName: string *
    resolutionEnvironment: ResolutionEnvironment *
    isInvalidationSupported: bool *
    isInteractive: bool *
    systemRuntimeContainsType: (string -> bool) *
    systemRuntimeAssemblyVersion: Version *
    compilerToolPaths: string list *
    m: range ->
        Tainted<ITypeProvider> list

/// Given an extension type resolver, supply a human-readable name suitable for error messages.
val DisplayNameOfTypeProvider: Tainted<ITypeProvider> * range -> string

/// The context used to interpret information in the closure of System.Type, System.MethodInfo and other
/// info objects coming from the type provider.
///
/// At the moment this is the "Type --> ILTypeRef" and "Type --> Tycon" remapping
/// context for generated types (it is empty for erased types). This is computed from
/// while processing the [<Generate>] declaration related to the type.
///
/// Immutable (after type generation for a [<Generate>] declaration populates the dictionaries).
///
/// The 'obj' values are all TyconRef, but obj is used due to a forward reference being required. Not particularly
/// pleasant, but better than intertwining the whole "ProvidedType" with the TAST structure.
[<Sealed>]
type ProvidedTypeContext =

    member TryGetILTypeRef: ProvidedType -> ILTypeRef option

    member TryGetTyconRef: ProvidedType -> obj option

    static member Empty: ProvidedTypeContext

    static member Create:
        ConcurrentDictionary<ProvidedType, ILTypeRef> * ConcurrentDictionary<ProvidedType, obj (* TyconRef *) > ->
            ProvidedTypeContext

    member GetDictionaries:
        unit -> ConcurrentDictionary<ProvidedType, ILTypeRef> * ConcurrentDictionary<ProvidedType, obj (* TyconRef *) >

    /// Map the TyconRef objects, if any
    member RemapTyconRefs: (obj -> obj) -> ProvidedTypeContext

[<Sealed; Class>]
type ProvidedType =
    inherit ProvidedMemberInfo

    member IsSuppressRelocate: bool

    member IsErased: bool

    member IsGenericType: bool

    member Namespace: string MaybeNull

    member FullName: string MaybeNull

    member IsArray: bool

    member GetInterfaces: unit -> ProvidedType ProvidedArray

    member Assembly: ProvidedAssembly MaybeNull

    member BaseType: ProvidedType MaybeNull

    member GetNestedType: string -> ProvidedType MaybeNull

    member GetNestedTypes: unit -> ProvidedType ProvidedArray

    member GetAllNestedTypes: unit -> ProvidedType ProvidedArray

    member GetMethods: unit -> ProvidedMethodInfo ProvidedArray

    member GetFields: unit -> ProvidedFieldInfo ProvidedArray

    member GetField: string -> ProvidedFieldInfo MaybeNull

    member GetProperties: unit -> ProvidedPropertyInfo ProvidedArray

    member GetProperty: string -> ProvidedPropertyInfo MaybeNull

    member GetEvents: unit -> ProvidedEventInfo ProvidedArray

    member GetEvent: string -> ProvidedEventInfo MaybeNull

    member GetConstructors: unit -> ProvidedConstructorInfo ProvidedArray

    member GetStaticParameters: ITypeProvider -> ProvidedParameterInfo ProvidedArray

    member GetGenericTypeDefinition: unit -> ProvidedType

    member IsVoid: bool

    member IsGenericParameter: bool

    member IsValueType: bool

    member IsByRef: bool

    member IsPointer: bool

    member IsEnum: bool

    member IsInterface: bool

    member IsClass: bool

    member IsMeasure: bool

    member IsSealed: bool

    member IsAbstract: bool

    member IsPublic: bool

    member IsNestedPublic: bool

    member GenericParameterPosition: int

    member GetElementType: unit -> ProvidedType MaybeNull

    member GetGenericArguments: unit -> ProvidedType ProvidedArray

    member GetArrayRank: unit -> int

    member RawSystemType: Type

    member GetEnumUnderlyingType: unit -> ProvidedType

    member MakePointerType: unit -> ProvidedType

    member MakeByRefType: unit -> ProvidedType

    member MakeArrayType: unit -> ProvidedType

    member MakeArrayType: rank: int -> ProvidedType

    member MakeGenericType: args: ProvidedType[] -> ProvidedType

    member AsProvidedVar: name: string -> ProvidedVar

    static member Void: ProvidedType

    static member CreateNoContext: Type -> ProvidedType

    member TryGetILTypeRef: unit -> ILTypeRef option

    member TryGetTyconRef: unit -> obj option

    static member ApplyContext: ProvidedType * ProvidedTypeContext -> ProvidedType

    member Context: ProvidedTypeContext

    interface IProvidedCustomAttributeProvider

    static member TaintedEquals: Tainted<ProvidedType> * Tainted<ProvidedType> -> bool

type IProvidedCustomAttributeProvider =
    abstract GetHasTypeProviderEditorHideMethodsAttribute: provider: ITypeProvider -> bool

    abstract GetDefinitionLocationAttribute: provider: ITypeProvider -> (string MaybeNull * int * int) option

    abstract GetXmlDocAttributes: provider: ITypeProvider -> string[]

    abstract GetAttributeConstructorArgs:
        provider: ITypeProvider * attribName: string -> (obj option list * (string * obj option) list) option

[<Sealed; Class>]
type ProvidedAssembly =
    member GetName: unit -> System.Reflection.AssemblyName

    member FullName: string

    member GetManifestModuleContents: ITypeProvider -> byte[]

    member Handle: System.Reflection.Assembly

[<AbstractClass>]
type ProvidedMemberInfo =

    member Name: string

    member DeclaringType: ProvidedType MaybeNull

    interface IProvidedCustomAttributeProvider

[<AbstractClass>]
type ProvidedMethodBase =
    inherit ProvidedMemberInfo

    member IsGenericMethod: bool

    member IsStatic: bool

    member IsFamily: bool

    member IsFamilyAndAssembly: bool

    member IsFamilyOrAssembly: bool

    member IsVirtual: bool

    member IsFinal: bool

    member IsPublic: bool

    member IsAbstract: bool

    member IsHideBySig: bool

    member IsConstructor: bool

    member GetParameters: unit -> ProvidedParameterInfo ProvidedArray

    member GetGenericArguments: unit -> ProvidedType ProvidedArray

    member GetStaticParametersForMethod: ITypeProvider -> ProvidedParameterInfo ProvidedArray

    static member TaintedGetHashCode: Tainted<ProvidedMethodBase> -> int

    static member TaintedEquals: Tainted<ProvidedMethodBase> * Tainted<ProvidedMethodBase> -> bool

[<Sealed; Class>]
type ProvidedMethodInfo =

    inherit ProvidedMethodBase

    member ReturnType: ProvidedType

    member MetadataToken: int

[<Sealed; Class>]
type ProvidedParameterInfo =

    member Name: string

    member ParameterType: ProvidedType

    member IsIn: bool

    member IsOut: bool

    member IsOptional: bool

    member RawDefaultValue: objnull

    member HasDefaultValue: bool

    interface IProvidedCustomAttributeProvider

[<Sealed; Class>]
type ProvidedFieldInfo =

    inherit ProvidedMemberInfo

    member IsInitOnly: bool

    member IsStatic: bool

    member IsSpecialName: bool

    member IsLiteral: bool

    member GetRawConstantValue: unit -> objnull

    member FieldType: ProvidedType

    member IsPublic: bool

    member IsFamily: bool

    member IsFamilyAndAssembly: bool

    member IsFamilyOrAssembly: bool

    member IsPrivate: bool

    static member TaintedEquals: Tainted<ProvidedFieldInfo> * Tainted<ProvidedFieldInfo> -> bool

[<Sealed; Class>]
type ProvidedPropertyInfo =

    inherit ProvidedMemberInfo

    member GetGetMethod: unit -> ProvidedMethodInfo MaybeNull

    member GetSetMethod: unit -> ProvidedMethodInfo MaybeNull

    member GetIndexParameters: unit -> ProvidedParameterInfo ProvidedArray

    member CanRead: bool

    member CanWrite: bool

    member PropertyType: ProvidedType

    static member TaintedGetHashCode: Tainted<ProvidedPropertyInfo> -> int

    static member TaintedEquals: Tainted<ProvidedPropertyInfo> * Tainted<ProvidedPropertyInfo> -> bool

[<Sealed; Class>]
type ProvidedEventInfo =

    inherit ProvidedMemberInfo

    member GetAddMethod: unit -> ProvidedMethodInfo MaybeNull

    member GetRemoveMethod: unit -> ProvidedMethodInfo MaybeNull

    member EventHandlerType: ProvidedType

    static member TaintedGetHashCode: Tainted<ProvidedEventInfo> -> int

    static member TaintedEquals: Tainted<ProvidedEventInfo> * Tainted<ProvidedEventInfo> -> bool

[<Sealed; Class>]
type ProvidedConstructorInfo =
    inherit ProvidedMethodBase

type ProvidedExprType =

    | ProvidedNewArrayExpr of ProvidedType * ProvidedExpr ProvidedArray

    | ProvidedNewObjectExpr of ProvidedConstructorInfo * ProvidedExpr ProvidedArray

    | ProvidedWhileLoopExpr of ProvidedExpr * ProvidedExpr

    | ProvidedNewDelegateExpr of ProvidedType * ProvidedVar ProvidedArray * ProvidedExpr

    | ProvidedForIntegerRangeLoopExpr of ProvidedVar * ProvidedExpr * ProvidedExpr * ProvidedExpr

    | ProvidedSequentialExpr of ProvidedExpr * ProvidedExpr

    | ProvidedTryWithExpr of ProvidedExpr * ProvidedVar * ProvidedExpr * ProvidedVar * ProvidedExpr

    | ProvidedTryFinallyExpr of ProvidedExpr * ProvidedExpr

    | ProvidedLambdaExpr of ProvidedVar * ProvidedExpr

    | ProvidedCallExpr of ProvidedExpr option * ProvidedMethodInfo * ProvidedExpr ProvidedArray

    | ProvidedConstantExpr of objnull * ProvidedType

    | ProvidedDefaultExpr of ProvidedType

    | ProvidedNewTupleExpr of ProvidedExpr ProvidedArray

    | ProvidedTupleGetExpr of ProvidedExpr * int

    | ProvidedTypeAsExpr of ProvidedExpr * ProvidedType

    | ProvidedTypeTestExpr of ProvidedExpr * ProvidedType

    | ProvidedLetExpr of ProvidedVar * ProvidedExpr * ProvidedExpr

    | ProvidedVarSetExpr of ProvidedVar * ProvidedExpr

    | ProvidedIfThenElseExpr of ProvidedExpr * ProvidedExpr * ProvidedExpr

    | ProvidedVarExpr of ProvidedVar

[<RequireQualifiedAccess; Sealed; Class>]
type ProvidedExpr =

    member Type: ProvidedType

    /// Convert the expression to a string for diagnostics
    member UnderlyingExpressionString: string

    member GetExprType: unit -> ProvidedExprType option

[<RequireQualifiedAccess; Sealed; Class>]
type ProvidedVar =

    member Type: ProvidedType

    member Name: string

    member IsMutable: bool

    override GetHashCode: unit -> int

/// Get the provided expression for a particular use of a method.
val GetInvokerExpression: ITypeProvider * ProvidedMethodBase * ProvidedVar[] -> ProvidedExpr MaybeNull

/// Validate that the given provided type meets some of the rules for F# provided types
val ValidateProvidedTypeAfterStaticInstantiation:
    m: range * st: Tainted<ProvidedType> * expectedPath: string[] * expectedName: string -> unit

/// Try to apply a provided type to the given static arguments. If successful also return a function
/// to check the type name is as expected (this function is called by the caller of TryApplyProvidedType
/// after other checks are made).
val TryApplyProvidedType:
    typeBeforeArguments: Tainted<ProvidedType> *
    optGeneratedTypePath: string list option *
    staticArgs: objnull[] *
    range ->
        (Tainted<ProvidedType> * (unit -> unit)) option

/// Try to apply a provided method to the given static arguments.
val TryApplyProvidedMethod:
    methBeforeArgs: Tainted<ProvidedMethodBase> * staticArgs: objnull[] * range -> Tainted<ProvidedMethodBase> option

/// Try to resolve a type in the given extension type resolver
val TryResolveProvidedType: Tainted<ITypeProvider> * range * string[] * typeName: string -> Tainted<ProvidedType> option

/// Try to resolve a type in the given extension type resolver
val TryLinkProvidedType:
    Tainted<ITypeProvider> * string[] * typeLogicalName: string * range: range -> Tainted<ProvidedType> option

/// Get the parts of a .NET namespace. Special rules: null means global, empty is not allowed.
val GetProvidedNamespaceAsPath: range * Tainted<ITypeProvider> * string MaybeNull -> string list

/// Decompose the enclosing name of a type (including any class nestings) into a list of parts.
/// e.g. System.Object -> ["System"; "Object"]
val GetFSharpPathToProvidedType: Tainted<ProvidedType> * range: range -> string list

/// Get the ILTypeRef for the provided type (including for nested types). Take into account
/// any type relocations or static linking for generated types.
val GetILTypeRefOfProvidedType: Tainted<ProvidedType> * range: range -> ILTypeRef

/// Get the ILTypeRef for the provided type (including for nested types). Do not take into account
/// any type relocations or static linking for generated types.
val GetOriginalILTypeRefOfProvidedType: Tainted<ProvidedType> * range: range -> ILTypeRef

/// Represents the remapping information for a generated provided type and its nested types.
///
/// There is one overall tree for each root 'type X = ... type generation expr...' specification.
type ProviderGeneratedType =
    | ProviderGeneratedType of ilOrigTyRef: ILTypeRef * ilRenamedTyRef: ILTypeRef * ProviderGeneratedType list

/// The table of information recording remappings from type names in the provided assembly to type
/// names in the statically linked, embedded assembly, plus what types are nested in side what types.
type ProvidedAssemblyStaticLinkingMap =
    {
        /// The table of remappings from type names in the provided assembly to type
        /// names in the statically linked, embedded assembly.
        ILTypeMap: Dictionary<ILTypeRef, ILTypeRef>
    }

    /// Create a new static linking map, ready to populate with data.
    static member CreateNew: unit -> ProvidedAssemblyStaticLinkingMap

/// Check if this is a direct reference to a non-embedded generated type. This is not permitted at any name resolution.
/// We check by seeing if the type is absent from the remapping context.
val IsGeneratedTypeDirectReference: Tainted<ProvidedType> * range -> bool

#endif
