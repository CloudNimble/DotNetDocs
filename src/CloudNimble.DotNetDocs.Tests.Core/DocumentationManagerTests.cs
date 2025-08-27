using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Assemblies;
using CloudNimble.DotNetDocs.Core;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Tests for DocumentationManager, which orchestrates the documentation pipeline.
    /// </summary>
    [TestClass]
    public class DocumentationManagerTests : BreakdanceTestBase
    {

        #region Private Fields

        private string? _tempDirectory;
        private string? _testAssemblyPath;
        private string? _testXmlPath;

        #endregion

        #region Test Methods

        [TestMethod]
        public async Task ProcessAsync_WithConceptualPath_LoadsConceptualContent()
        {
            // Arrange
            await CreateTestAssemblyAsync();
            await CreateConceptualContentAsync();

            var manager = new DocumentationManager([], [], []);
            var context = new ProjectContext
            {
                ConceptualPath = Path.Combine(_tempDirectory!, "conceptual"),
                OutputPath = Path.Combine(_tempDirectory!, "output")
            };

            // Since ProcessAsync runs the full pipeline, we'll test LoadConceptual directly
            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading directly (using reflection to test private async method)
            var loadConceptualMethod = typeof(DocumentationManager).GetMethod("LoadConceptualAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var task = loadConceptualMethod?.Invoke(manager, [model, context.ConceptualPath]) as Task;
            await task!;

            // Assert
            var testClass = model.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "TestClass");

            testClass.Should().NotBeNull();
            testClass!.Usage.Should().Be("This is conceptual usage documentation");
            testClass.Examples.Should().Be("This is conceptual examples documentation");
            testClass.BestPractices.Should().Be("This is conceptual best practices");
            testClass.Patterns.Should().Be("This is conceptual patterns");
            testClass.Considerations.Should().Be("This is conceptual considerations");
        }

        [TestMethod]
        public async Task ProcessAsync_WithMemberConceptual_LoadsMemberContent()
        {
            // Arrange
            await CreateTestAssemblyAsync();
            await CreateConceptualContentAsync();

            var manager = new DocumentationManager([], [], []);
            var context = new ProjectContext
            {
                ConceptualPath = Path.Combine(_tempDirectory!, "conceptual"),
                OutputPath = Path.Combine(_tempDirectory!, "output")
            };

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading directly
            var loadConceptualMethod = typeof(DocumentationManager).GetMethod("LoadConceptualAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var task = loadConceptualMethod?.Invoke(manager, [model, context.ConceptualPath]) as Task;
            await task!;

            // Assert
            var method = model.Namespaces
                .SelectMany(ns => ns.Types)
                .SelectMany(t => t.Members)
                .FirstOrDefault(m => m.Symbol.Name == "DoSomething");

            method.Should().NotBeNull();
            method!.Usage.Should().Be("This is conceptual member usage");
            method.Examples.Should().Be("This is conceptual member examples");
        }

        [TestMethod]
        public async Task ProcessAsync_WithRelatedApis_LoadsFromMarkdownFile()
        {
            // Arrange
            await CreateTestAssemblyAsync();
            await CreateConceptualContentAsync();

            var manager = new DocumentationManager([], [], []);
            var context = new ProjectContext
            {
                ConceptualPath = Path.Combine(_tempDirectory!, "conceptual"),
                OutputPath = Path.Combine(_tempDirectory!, "output")
            };

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading directly
            var loadConceptualMethod = typeof(DocumentationManager).GetMethod("LoadConceptualAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var task = loadConceptualMethod?.Invoke(manager, [model, context.ConceptualPath]) as Task;
            await task!;

            // Assert
            var testClass = model.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "TestClass");

            testClass.Should().NotBeNull();
            testClass!.RelatedApis.Should().NotBeEmpty();
            testClass.RelatedApis.Should().Contain("System.Object");
            testClass.RelatedApis.Should().Contain("System.Collections.Generic.List<T>");
            testClass.RelatedApis.Should().Contain("System.Linq.Enumerable");
        }

        [TestMethod]
        public async Task ProcessAsync_WithNamespaceBasedPath_LoadsFromCorrectFolder()
        {
            // Arrange
            await CreateTestAssemblyAsync();

            // Create namespace-based conceptual structure
            var conceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            var namespacePath = Path.Combine(conceptualPath, "TestNamespace");
            var typePath = Path.Combine(namespacePath, "TestClass");
            Directory.CreateDirectory(typePath);

            await File.WriteAllTextAsync(Path.Combine(typePath, DotNetDocsConstants.UsageFileName),
                "Documentation from namespace-based path");

            var manager = new DocumentationManager([], [], []);
            var context = new ProjectContext
            {
                ConceptualPath = conceptualPath,
                OutputPath = Path.Combine(_tempDirectory!, "output")
            };

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading directly
            var loadConceptualMethod = typeof(DocumentationManager).GetMethod("LoadConceptualAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var task = loadConceptualMethod?.Invoke(manager, [model, context.ConceptualPath]) as Task;
            await task!;

            // Assert
            var testClass = model.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "TestClass");

            testClass.Should().NotBeNull();
            testClass!.Usage.Should().Be("Documentation from namespace-based path");
        }

        [TestMethod]
        public async Task ProcessAsync_WithParameterDocumentation_LoadsParameterContent()
        {
            // Arrange
            await CreateTestAssemblyAsync();

            // Create parameter documentation
            var conceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            var namespacePath = Path.Combine(conceptualPath, "TestNamespace");
            var typePath = Path.Combine(namespacePath, "TestClass");
            var memberPath = Path.Combine(typePath, "DoSomething");
            Directory.CreateDirectory(memberPath);

            await File.WriteAllTextAsync(Path.Combine(memberPath, $"{DotNetDocsConstants.ParameterFilePrefix}input{DotNetDocsConstants.ParameterFileExtension}"),
                "Custom parameter documentation");

            var manager = new DocumentationManager([], [], []);
            var context = new ProjectContext
            {
                ConceptualPath = conceptualPath,
                OutputPath = Path.Combine(_tempDirectory!, "output")
            };

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading directly
            var loadConceptualMethod = typeof(DocumentationManager).GetMethod("LoadConceptualAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var task = loadConceptualMethod?.Invoke(manager, [model, context.ConceptualPath]) as Task;
            await task!;

            // Assert
            var method = model.Namespaces
                .SelectMany(ns => ns.Types)
                .SelectMany(t => t.Members)
                .FirstOrDefault(m => m.Symbol.Name == "DoSomething");

            var parameter = method?.Parameters.FirstOrDefault(p => p.Symbol.Name == "input");

            parameter.Should().NotBeNull();
            parameter!.Usage.Should().Be("Custom parameter documentation");
        }

        #endregion

        #region Setup and Cleanup

        [TestInitialize]
        public void Setup()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"DocManagerTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDirectory);

            _testAssemblyPath = Path.Combine(_tempDirectory, "TestAssembly.dll");
            _testXmlPath = Path.Combine(_tempDirectory, "TestAssembly.xml");
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (!string.IsNullOrWhiteSpace(_tempDirectory) && Directory.Exists(_tempDirectory))
            {
                try
                {
                    Directory.Delete(_tempDirectory, recursive: true);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }

        #endregion

        #region Private Methods

        private async Task CreateTestAssemblyAsync()
        {
            // Create a simple test assembly
            var code = @"
                namespace TestNamespace
                {
                    /// <summary>
                    /// This is a test class.
                    /// </summary>
                    /// <example>var test = new TestClass();</example>
                    /// <remarks>These are remarks about the class.</remarks>
                    public class TestClass : System.IDisposable
                    {
                        /// <summary>
                        /// Gets or sets the test property.
                        /// </summary>
                        public string TestProperty { get; set; }

                        /// <summary>
                        /// Does something important.
                        /// </summary>
                        /// <param name=""input"">The input value.</param>
                        /// <example>var result = DoSomething(""test"");</example>
                        public string DoSomething(string input)
                        {
                            return input.ToUpper();
                        }

                        /// <summary>
                        /// Disposes the object.
                        /// </summary>
                        public void Dispose() { }
                    }

                    /// <summary>
                    /// A derived class.
                    /// </summary>
                    public class DerivedClass : TestClass
                    {
                    }
                }";

            var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            var references = new[]
            {
                Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location),
                Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
                Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(System.IDisposable).Assembly.Location),
            };

            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
                "TestAssembly",
                syntaxTrees: [syntaxTree],
                references: references,
                options: new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(
                    Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary,
                    xmlReferenceResolver: null,
                    sourceReferenceResolver: null));

            using var peStream = File.Create(_testAssemblyPath!);
            var emitResult = compilation.Emit(peStream);
            emitResult.Success.Should().BeTrue("Assembly compilation should succeed");

            // Create XML documentation
            var xml = @"<?xml version=""1.0""?>
                <doc>
                    <assembly>
                        <name>TestAssembly</name>
                    </assembly>
                    <members>
                        <member name=""T:TestNamespace.TestClass"">
                            <summary>
                            This is a test class.
                            </summary>
                            <example>var test = new TestClass();</example>
                            <remarks>These are remarks about the class.</remarks>
                        </member>
                        <member name=""P:TestNamespace.TestClass.TestProperty"">
                            <summary>
                            Gets or sets the test property.
                            </summary>
                        </member>
                        <member name=""M:TestNamespace.TestClass.DoSomething(System.String)"">
                            <summary>
                            Does something important.
                            </summary>
                            <param name=""input"">The input value.</param>
                            <example>var result = DoSomething(""test"");</example>
                        </member>
                        <member name=""M:TestNamespace.TestClass.Dispose"">
                            <summary>
                            Disposes the object.
                            </summary>
                        </member>
                        <member name=""T:TestNamespace.DerivedClass"">
                            <summary>
                            A derived class.
                            </summary>
                        </member>
                    </members>
                </doc>";

            await File.WriteAllTextAsync(_testXmlPath!, xml);
        }

        private async Task CreateConceptualContentAsync()
        {
            var conceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            Directory.CreateDirectory(conceptualPath);

            // Create namespace-based folder structure
            var namespacePath = Path.Combine(conceptualPath, "TestNamespace");
            Directory.CreateDirectory(namespacePath);

            // TestClass conceptual content
            var testClassPath = Path.Combine(namespacePath, "TestClass");
            Directory.CreateDirectory(testClassPath);
            await File.WriteAllTextAsync(Path.Combine(testClassPath, DotNetDocsConstants.UsageFileName), "This is conceptual usage documentation");
            await File.WriteAllTextAsync(Path.Combine(testClassPath, DotNetDocsConstants.ExamplesFileName), "This is conceptual examples documentation");
            await File.WriteAllTextAsync(Path.Combine(testClassPath, DotNetDocsConstants.BestPracticesFileName), "This is conceptual best practices");
            await File.WriteAllTextAsync(Path.Combine(testClassPath, DotNetDocsConstants.PatternsFileName), "This is conceptual patterns");
            await File.WriteAllTextAsync(Path.Combine(testClassPath, DotNetDocsConstants.ConsiderationsFileName), "This is conceptual considerations");

            // Related APIs as markdown file
            var relatedApis = @"System.Object
System.Collections.Generic.List<T>
System.Linq.Enumerable";
            await File.WriteAllTextAsync(Path.Combine(testClassPath, DotNetDocsConstants.RelatedApisFileName), relatedApis);

            // Member-specific content
            var doSomethingPath = Path.Combine(testClassPath, "DoSomething");
            Directory.CreateDirectory(doSomethingPath);
            await File.WriteAllTextAsync(Path.Combine(doSomethingPath, DotNetDocsConstants.UsageFileName), "This is conceptual member usage");
            await File.WriteAllTextAsync(Path.Combine(doSomethingPath, DotNetDocsConstants.ExamplesFileName), "This is conceptual member examples");
        }

        #endregion

    }

}