using System;
using System.Text.Json;

#if NET10_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;
#endif

namespace CloudNimble.DotNetDocs.WasmRunner
{

    /// <summary>
    /// Entry point for the .NET WASM runner application.
    /// </summary>
    /// <remarks>
    /// Exposes C# compilation and execution capabilities to JavaScript via <c>[JSExport]</c> interop.
    /// The exported methods are called from the <c>dotnet-runner.jsx</c> Mintlify React component.
    /// On runtimes that do not support <c>[JSExport]</c> (e.g., desktop .NET), the methods are still
    /// available for direct invocation.
    /// </remarks>
    public partial class Program
    {

        #region Fields

        private static readonly CSharpCompiler _compiler = new();

        #endregion

        #region Public Methods

        /// <summary>
        /// Compiles and executes C# code, returning JSON with the result.
        /// </summary>
        /// <param name="code">The C# code to compile and execute.</param>
        /// <returns>A JSON string containing the <see cref="CompilationResult"/>.</returns>
#if NET10_0_OR_GREATER
#pragma warning disable CA1416 // Validate platform compatibility — JSExport is used when deployed to browser-wasm
        [JSExport]
#pragma warning restore CA1416
#endif
        public static string CompileAndRun(string code)
        {
            var result = _compiler.CompileAndRun(code);
            return JsonSerializer.Serialize(result, CompilationResultContext.Default.CompilationResult);
        }

        /// <summary>
        /// Application entry point. Required for WASM runtime initialization.
        /// </summary>
        public static void Main()
        {
            Console.WriteLine("DotNetDocs WASM Runner initialized.");
        }

        #endregion

    }

}
