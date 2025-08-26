using System.IO;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CloudNimble.DotNetDocs.Tests.Shared
{

    /// <summary>
    /// Base class for DotNetDocs tests that need Roslyn compilation support.
    /// </summary>
    public abstract class TestBase : BreakdanceTestBase
    {

        #region Protected Methods

        /// <summary>
        /// Creates a Roslyn compilation with the Tests.Shared assembly referenced.
        /// </summary>
        /// <returns>A compilation with Tests.Shared assembly referenced, which includes SampleLib types.</returns>
        protected async Task<Compilation> CreateCompilationAsync()
        {
            // Use the Tests.Shared assembly itself as the reference since it contains SampleLib
            var assemblyPath = typeof(TestBase).Assembly.Location;

            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException($"Tests.Shared assembly not found at: {assemblyPath}");
            }

            var metadataReference = MetadataReference.CreateFromFile(assemblyPath);
            var compilation = CSharpCompilation.Create("TestAssembly")
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(metadataReference);

            return await Task.FromResult(compilation);
        }

        #endregion

    }

}