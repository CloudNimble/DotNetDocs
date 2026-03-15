using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CloudNimble.DotNetDocs.WasmRunner
{

    /// <summary>
    /// Compiles and executes C# code using the Roslyn compiler.
    /// </summary>
    /// <remarks>
    /// This class accepts top-level C# statements (or full programs), compiles them in-memory
    /// using Roslyn, performs security validation via <see cref="SecuritySyntaxWalker"/>,
    /// captures console output via <see cref="OutputCapture"/>, and returns structured
    /// <see cref="CompilationResult"/> objects.
    /// </remarks>
    public class CSharpCompiler
    {

        #region Fields

        private static readonly string[] _defaultUsings =
        [
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text"
        ];

        private readonly MetadataReference[] _references;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpCompiler"/> class.
        /// </summary>
        public CSharpCompiler()
        {
            _references = BuildReferences();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Compiles and executes the specified C# code, returning the result.
        /// </summary>
        /// <param name="code">The C# code to compile and execute. Supports top-level statements.</param>
        /// <returns>
        /// A <see cref="CompilationResult"/> containing either the captured console output
        /// on success, or compiler diagnostics on failure.
        /// </returns>
        public CompilationResult CompileAndRun(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return CompilationResult.Success(string.Empty);
            }

            // Security check: scan for blocked namespaces
            var tree = CSharpSyntaxTree.ParseText(code);
            var walker = new SecuritySyntaxWalker();
            walker.Visit(tree.GetRoot());

            if (walker.HasSecurityIssues)
            {
                var blocked = string.Join(", ", walker.BlockedUsings);
                return CompilationResult.Failure(
                    $"Blocked namespace(s): {blocked}. " +
                    "These namespaces are not allowed in the sandbox for security reasons.");
            }

            // Compile
            var compilation = CSharpCompilation.Create(
                $"UserCode_{Guid.NewGuid():N}",
                [tree],
                _references,
                new CSharpCompilationOptions(OutputKind.ConsoleApplication)
                    .WithUsings(_defaultUsings));

            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);

            if (!emitResult.Success)
            {
                var errors = emitResult.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.ToString());
                return CompilationResult.Failure(string.Join(Environment.NewLine, errors));
            }

            // Execute
            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());
            var entryPoint = assembly.EntryPoint;

            if (entryPoint is null)
            {
                return CompilationResult.Success(string.Empty);
            }

            using var capture = new OutputCapture();
            capture.StartCapture();

            try
            {
                var parameters = entryPoint.GetParameters();
                var args = parameters.Length > 0 ? new object?[] { Array.Empty<string>() } : null;
                entryPoint.Invoke(null, args);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                var output = capture.StopCapture();
                return CompilationResult.Failure(
                    $"Runtime error: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}" +
                    (output.Length > 0 ? $"\n\nPartial output:\n{output}" : ""));
            }
            catch (Exception ex)
            {
                var output = capture.StopCapture();
                return CompilationResult.Failure(
                    $"Execution error: {ex.Message}" +
                    (output.Length > 0 ? $"\n\nPartial output:\n{output}" : ""));
            }

            return CompilationResult.Success(capture.StopCapture());
        }

        #endregion

        #region Private Methods

        private static MetadataReference[] BuildReferences()
        {
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

            string[] assemblyNames =
            [
                "System.Runtime.dll",
                "System.Console.dll",
                "System.Collections.dll",
                "System.Linq.dll",
                "System.Text.RegularExpressions.dll",
                "System.Private.CoreLib.dll",
                "netstandard.dll"
            ];

            return assemblyNames
                .Select(name => Path.Combine(assemblyPath, name))
                .Where(File.Exists)
                .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
                .ToArray();
        }

        #endregion

    }

}
