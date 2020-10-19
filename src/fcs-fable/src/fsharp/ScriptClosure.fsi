// Copyright (c) Microsoft Corporation.  All Rights Reserved.  See License.txt in the project root for license information.

/// Compute the load closure of a set of script files
module internal FSharp.Compiler.ScriptClosure

open FSharp.Compiler
open FSharp.Compiler.AbstractIL.ILBinaryReader
open FSharp.Compiler.AbstractIL.Internal.Library
open FSharp.Compiler.CompilerConfig
open FSharp.Compiler.CompilerImports
open FSharp.Compiler.ErrorLogger
open FSharp.Compiler.Range
open FSharp.Compiler.SyntaxTree
open FSharp.Compiler.Text
#if !FABLE_COMPILER
open Microsoft.DotNet.DependencyManager
#endif

[<RequireQualifiedAccess>]
type CodeContext =
    | CompilationAndEvaluation
    | Compilation
    | Editing

[<RequireQualifiedAccess>]
type LoadClosureInput = 
    { FileName: string
      SyntaxTree: ParsedInput option
      ParseDiagnostics: (PhasedDiagnostic * bool) list 
      MetaCommandDiagnostics: (PhasedDiagnostic * bool) list  }

[<RequireQualifiedAccess>]
type LoadClosure = 
    { /// The source files along with the ranges of the #load positions in each file.
      SourceFiles: (string * range list) list

      /// The resolved references along with the ranges of the #r positions in each file.
      References: (string * AssemblyResolution list) list

      /// The resolved pacakge references along with the ranges of the #r positions in each file.
      PackageReferences: (range * string list)[]

      /// The list of references that were not resolved during load closure.
      UnresolvedReferences: UnresolvedAssemblyReference list

      /// The list of all sources in the closure with inputs when available, with associated parse errors and warnings
      Inputs: LoadClosureInput list

      /// The original #load references, including those that didn't resolve
      OriginalLoadReferences: (range * string * string) list

      /// The #nowarns
      NoWarns: (string * range list) list

      /// Diagnostics seen while processing resolutions
      ResolutionDiagnostics: (PhasedDiagnostic * bool)  list

      /// Diagnostics to show for root of closure (used by fsc.fs)
      AllRootFileDiagnostics: (PhasedDiagnostic * bool) list

      /// Diagnostics seen while processing the compiler options implied root of closure
      LoadClosureRootFileDiagnostics: (PhasedDiagnostic * bool) list }   

#if !FABLE_COMPILER

    /// Analyze a script text and find the closure of its references. 
    /// Used from FCS, when editing a script file.  
    //
    /// A temporary TcConfig is created along the way, is why this routine takes so many arguments. We want to be sure to use exactly the
    /// same arguments as the rest of the application.
    static member ComputeClosureOfScriptText:
        CompilationThreadToken * 
        legacyReferenceResolver: ReferenceResolver.Resolver * 
        defaultFSharpBinariesDir: string * 
        filename: string * 
        sourceText: ISourceText * 
        implicitDefines:CodeContext * 
        useSimpleResolution: bool * 
        useFsiAuxLib: bool * 
        useSdkRefs: bool * 
        lexResourceManager: Lexhelp.LexResourceManager * 
        applyCompilerOptions: (TcConfigBuilder -> unit) * 
        assumeDotNetFramework: bool * 
        tryGetMetadataSnapshot: ILReaderTryGetMetadataSnapshot *
        reduceMemoryUsage: ReduceMemoryFlag *
        dependencyProvider: DependencyProvider
          -> LoadClosure

    /// Analyze a set of script files and find the closure of their references. The resulting references are then added to the given TcConfig.
    /// Used from fsi.fs and fsc.fs, for #load and command line. 
    static member ComputeClosureOfScriptFiles: 
        CompilationThreadToken * 
        tcConfig:TcConfig * 
        (string * range) list * 
        implicitDefines:CodeContext * 
        lexResourceManager: Lexhelp.LexResourceManager *
        dependencyProvider: DependencyProvider
            -> LoadClosure

#endif //!FABLE_COMPILER