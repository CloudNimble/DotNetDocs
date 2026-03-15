using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CloudNimble.DotNetDocs.Mintlify.Validation
{

    /// <summary>
    /// Represents a validation error found when compiling a code example extracted from an MDX file.
    /// </summary>
    public class ValidationError
    {

        #region Properties

        /// <summary>
        /// Gets or sets the original C# code that failed compilation.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of compiler diagnostic messages for this code example.
        /// </summary>
        public List<string> Diagnostics { get; set; } = [];

        /// <summary>
        /// Gets or sets the path to the MDX file that contained the failing code example.
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

        #endregion

    }

    /// <summary>
    /// Validates extracted <c>&lt;DotNetRunner&gt;</c> code examples by compiling them with Roslyn
    /// to detect compilation errors at build time.
    /// </summary>
    /// <remarks>
    /// Each <see cref="CodeExample"/> is compiled as a standalone console application with common
    /// default usings (<c>System</c>, <c>System.Collections.Generic</c>, <c>System.Linq</c>, and
    /// <c>System.Text</c>). Only diagnostics with <see cref="DiagnosticSeverity.Error"/> severity
    /// are reported. This enables build-time validation of embedded code examples to catch issues
    /// such as typos, missing references, or syntax errors before documentation is published.
    /// </remarks>
    public static class MdxCodeExampleValidator
    {

        #region Fields

        /// <summary>
        /// The default using directives prepended to each code example before compilation.
        /// </summary>
        private static readonly string[] DefaultUsings =
        [
            "using System;",
            "using System.Collections.Generic;",
            "using System.Linq;",
            "using System.Text;"
        ];

        #endregion

        #region Public Methods

        /// <summary>
        /// Validates a list of code examples by compiling each one with Roslyn and returning any errors.
        /// </summary>
        /// <param name="examples">The list of <see cref="CodeExample"/> instances to validate.</param>
        /// <returns>
        /// A list of <see cref="ValidationError"/> instances, one for each code example that produced
        /// compilation errors. Returns an empty list if all examples compile successfully or if
        /// <paramref name="examples"/> is null or empty.
        /// </returns>
        /// <example>
        /// <code>
        /// var examples = new List&lt;CodeExample&gt;
        /// {
        ///     new() { Code = "Console.WriteLine(\"Hello\");", SourceFile = "intro.mdx" }
        /// };
        /// var errors = MdxCodeExampleValidator.Validate(examples);
        /// // errors.Count == 0 for valid code
        /// </code>
        /// </example>
        public static List<ValidationError> Validate(List<CodeExample> examples)
        {
            var errors = new List<ValidationError>();

            if (examples is null || examples.Count == 0)
            {
                return errors;
            }

            var references = GetMetadataReferences();

            foreach (var example in examples)
            {
                var diagnostics = CompileExample(example.Code, references);

                if (diagnostics.Count > 0)
                {
                    errors.Add(new ValidationError
                    {
                        SourceFile = example.SourceFile,
                        Code = example.Code,
                        Diagnostics = diagnostics
                    });
                }
            }

            return errors;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Compiles a single code example and returns any error-level diagnostic messages.
        /// </summary>
        /// <param name="code">The C# code to compile.</param>
        /// <param name="references">The metadata references to use for compilation.</param>
        /// <returns>A list of error diagnostic message strings.</returns>
        private static List<string> CompileExample(string code, List<MetadataReference> references)
        {
            var fullSource = string.Join(Environment.NewLine, DefaultUsings) + Environment.NewLine + code;

            var syntaxTree = CSharpSyntaxTree.ParseText(fullSource);

            var compilation = CSharpCompilation.Create(
                assemblyName: "CodeExampleValidation",
                syntaxTrees: [syntaxTree],
                references: references,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

            var emitResult = compilation.GetDiagnostics();

            return emitResult
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.GetMessage())
                .ToList();
        }

        /// <summary>
        /// Builds the list of metadata references from the runtime assembly directory,
        /// including core runtime assemblies needed for most C# code examples.
        /// </summary>
        /// <returns>A list of <see cref="MetadataReference"/> instances for Roslyn compilation.</returns>
        private static List<MetadataReference> GetMetadataReferences()
        {
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

            var assemblyNames = new[]
            {
                "mscorlib.dll",
                "System.dll",
                "System.Console.dll",
                "System.Collections.dll",
                "System.Linq.dll",
                "System.Runtime.dll",
                "System.Private.CoreLib.dll"
            };

            var references = new List<MetadataReference>();

            foreach (var assemblyName in assemblyNames)
            {
                var fullPath = Path.Combine(assemblyPath, assemblyName);

                if (File.Exists(fullPath))
                {
                    references.Add(MetadataReference.CreateFromFile(fullPath));
                }
            }

            return references;
        }

        #endregion

    }

}
