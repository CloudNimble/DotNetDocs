using CloudNimble.Breakdance.Assemblies;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

            // Apply conceptual loading directly
            await manager.LoadConceptualAsync(model, context.ConceptualPath!, context);

            // Assert
            var testClass = model.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SimpleClass");

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
            await manager.LoadConceptualAsync(model, context.ConceptualPath!, context);

            // Assert
            var method = model.Namespaces
                .SelectMany(ns => ns.Types)
                .SelectMany(t => t.Members)
                .FirstOrDefault(m => m.Name == "DoWork");

            method.Should().NotBeNull();
            method!.Usage.Should().Be("This is conceptual member usage");
            method.Examples.Should().Be("This is conceptual member examples");
        }

        [TestMethod]
        public async Task ProcessAsync_WithPipeline_ExecutesAllSteps()
        {
            // Arrange
            await CreateConceptualContentAsync();

            var enricher = new TestEnricher("Enricher");
            var transformer = new TestTransformer("Transformer");
            var renderer = new TestRenderer("Renderer");

            var manager = new DocumentationManager(
                [enricher],
                [transformer],
                [renderer]
            );

            var context = new ProjectContext
            {
                ConceptualPath = Path.Combine(_tempDirectory!, "conceptual"),
                OutputPath = Path.Combine(_tempDirectory!, "output")
            };

            // Act
            await manager.ProcessAsync(_testAssemblyPath!, _testXmlPath!, context);

            // Assert
            enricher.Executed.Should().BeTrue();
            transformer.Executed.Should().BeTrue();
            renderer.Executed.Should().BeTrue();
        }

        [TestMethod]
        public async Task ProcessAsync_WithShowPlaceholdersFalse_SkipsPlaceholderContent()
        {
            // Arrange
            await CreateConceptualContentWithPlaceholdersAsync();

            var manager = new DocumentationManager([], [], []);
            var context = new ProjectContext
            {
                ConceptualPath = Path.Combine(_tempDirectory!, "conceptual"),
                OutputPath = Path.Combine(_tempDirectory!, "output"),
                ShowPlaceholders = false // Hide placeholders
            };

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading
            await manager.LoadConceptualAsync(model, context.ConceptualPath!, context);

            // Assert - properties that had placeholder content should not contain the placeholder text
            var testClass = model.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SimpleClass");

            testClass.Should().NotBeNull();
            // The Usage property should remain unchanged from XML docs since placeholder was skipped
            testClass!.Usage.Should().NotContain("TODO: REMOVE THIS COMMENT");
            testClass.Usage.Should().NotContain("This is placeholder usage");
            
            // BestPractices should be empty since it had placeholder content
            testClass.BestPractices.Should().BeEmpty();

            // Examples was set to real content, should have that content
            testClass.Examples.Should().Be("This is real conceptual examples");
        }

        [TestMethod]
        public async Task ProcessAsync_WithShowPlaceholdersTrue_IncludesPlaceholderContent()
        {
            // Arrange
            await CreateConceptualContentWithPlaceholdersAsync();

            var manager = new DocumentationManager([], [], []);
            var context = new ProjectContext
            {
                ConceptualPath = Path.Combine(_tempDirectory!, "conceptual"),
                OutputPath = Path.Combine(_tempDirectory!, "output"),
                ShowPlaceholders = true // Show placeholders
            };

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading
            await manager.LoadConceptualAsync(model, context.ConceptualPath!, context);

            // Assert - placeholder content should be included
            var testClass = model.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SimpleClass");

            testClass.Should().NotBeNull();
            // Usage should have the placeholder content
            testClass!.Usage.Should().Be("<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nThis is placeholder usage");
            
            // BestPractices should have the placeholder content
            testClass.BestPractices.Should().Be("<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nThis is placeholder best practices");

            // Examples was set to real content, should have that content
            testClass.Examples.Should().Be("This is real conceptual examples");
        }

        [TestMethod]
        public async Task ProcessAsync_WithMixedPlaceholderContent_HandlesCorrectly()
        {
            // Arrange
            await CreateMixedPlaceholderContentAsync();

            var manager = new DocumentationManager([], [], []);
            var context = new ProjectContext
            {
                ConceptualPath = Path.Combine(_tempDirectory!, "conceptual"),
                OutputPath = Path.Combine(_tempDirectory!, "output"),
                ShowPlaceholders = false // Hide placeholders
            };

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading
            await manager.LoadConceptualAsync(model, context.ConceptualPath!, context);

            // Assert
            var testClass = model.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SimpleClass");

            testClass.Should().NotBeNull();
            // Usage had placeholder, should not contain placeholder text
            testClass!.Usage.Should().NotContain("TODO: REMOVE THIS COMMENT");
            
            // BestPractices had real content after placeholder, should contain it
            testClass.BestPractices.Should().Be("This is real best practices after placeholder");
            
            // Patterns had no placeholder, should have full content
            testClass.Patterns.Should().Be("This is real patterns content");
        }

        [TestMethod]
        public async Task ProcessAsync_WithNestedNamespacePlaceholders_HandlesCorrectly()
        {
            // Arrange
            await CreateNestedNamespacePlaceholderContentAsync();

            var manager = new DocumentationManager([], [], []);
            var context = new ProjectContext
            {
                ConceptualPath = Path.Combine(_tempDirectory!, "conceptual"),
                OutputPath = Path.Combine(_tempDirectory!, "output"),
                ShowPlaceholders = false // Hide placeholders
            };

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading
            await manager.LoadConceptualAsync(model, context.ConceptualPath!, context);

            // Assert - namespace level documentation
            var ns = model.Namespaces.FirstOrDefault(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");
            
            ns.Should().NotBeNull();
            // Namespace usage had placeholder, should not contain placeholder text
            ns!.Usage.Should().NotContain("TODO: REMOVE THIS COMMENT");
            ns.Usage.Should().NotContain("placeholder namespace usage");
            
            // The Usage property should remain unchanged from XML docs since placeholder was skipped
            ns.Examples.Should().Be("This is real namespace examples");
        }

        [TestMethod]
        public async Task ProcessAsync_WithMemberPlaceholders_HandlesCorrectly()
        {
            // Arrange
            await CreateMemberPlaceholderContentAsync();

            var manager = new DocumentationManager([], [], []);
            var context = new ProjectContext
            {
                ConceptualPath = Path.Combine(_tempDirectory!, "conceptual"),
                OutputPath = Path.Combine(_tempDirectory!, "output"),
                ShowPlaceholders = false // Hide placeholders
            };

            // Get the model first from AssemblyManager
            using var assemblyComponent = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyComponent.DocumentAsync(context);

            // Apply conceptual loading
            await manager.LoadConceptualAsync(model, context.ConceptualPath!, context);

            // Assert - member level documentation
            var method = model.Namespaces
                .SelectMany(ns => ns.Types)
                .SelectMany(t => t.Members)
                .FirstOrDefault(m => m.Name == "DoWork");

            method.Should().NotBeNull();
            // The Usage property should remain unchanged from XML docs since placeholder was skipped
            method!.Usage.Should().NotContain("TODO: REMOVE THIS COMMENT");
            method.Usage.Should().NotContain("placeholder member usage");
            
            // Examples had real content, should have it
            method.Examples.Should().Be("This is real member examples");
        }

        [TestMethod]
        public async Task ProcessAsync_WithParameterPlaceholders_HandlesCorrectly()
        {
            // Arrange
            await CreateParameterPlaceholderContentAsync();

            var manager = new DocumentationManager([], [], []);
            var context = new ProjectContext
            {
                ConceptualPath = Path.Combine(_tempDirectory!, "conceptual"),
                OutputPath = Path.Combine(_tempDirectory!, "output"),
                ShowPlaceholders = false // Hide placeholders
            };

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading
            await manager.LoadConceptualAsync(model, context.ConceptualPath!, context);

            // Assert - parameter level documentation
            var method = model.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithMethods")
                ?.Members
                .FirstOrDefault(m => m.Symbol.Name == "Calculate");

            method.Should().NotBeNull();

            var parameter = method?.Parameters.FirstOrDefault(p => p.Symbol.Name == "a");

            parameter.Should().NotBeNull();
            // The Usage property should remain unchanged from XML docs since placeholder was skipped
            // The XML doc for the parameter is "The first number."
            parameter!.Usage.Should().Be("The first number.");
        }

        #endregion

        #region Setup and Cleanup

        [TestInitialize]
        public void Setup()
        {
            // Use the real Tests.Shared assembly and its XML documentation
            _testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            _testXmlPath = Path.ChangeExtension(_testAssemblyPath, ".xml");
            
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"DocManagerTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDirectory);
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

        private async Task CreateConceptualContentAsync()
        {
            var conceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            
            // Create namespace folder structure
            var namespacePath = Path.Combine(conceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios");
            Directory.CreateDirectory(namespacePath);

            // Create conceptual content for SimpleClass
            var classPath = Path.Combine(namespacePath, "SimpleClass");
            Directory.CreateDirectory(classPath);

            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.UsageFileName),
                "This is conceptual usage documentation");
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.ExamplesFileName),
                "This is conceptual examples documentation");
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.BestPracticesFileName),
                "This is conceptual best practices");
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.PatternsFileName),
                "This is conceptual patterns");
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.ConsiderationsFileName),
                "This is conceptual considerations");

            // Create member documentation
            var memberPath = Path.Combine(classPath, "DoWork");
            Directory.CreateDirectory(memberPath);

            await File.WriteAllTextAsync(Path.Combine(memberPath, DocConstants.UsageFileName),
                "This is conceptual member usage");
            await File.WriteAllTextAsync(Path.Combine(memberPath, DocConstants.ExamplesFileName),
                "This is conceptual member examples");
        }

        private async Task CreateConceptualContentWithPlaceholdersAsync()
        {
            var conceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            
            // Create namespace folder structure
            var namespacePath = Path.Combine(conceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios");
            Directory.CreateDirectory(namespacePath);

            // Create conceptual content for SimpleClass with placeholders
            var classPath = Path.Combine(namespacePath, "SimpleClass");
            Directory.CreateDirectory(classPath);

            // Usage has placeholder
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.UsageFileName),
                "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nThis is placeholder usage");
            
            // Examples has real content
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.ExamplesFileName),
                "This is real conceptual examples");
            
            // BestPractices has placeholder
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.BestPracticesFileName),
                "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nThis is placeholder best practices");
        }

        private async Task CreateMixedPlaceholderContentAsync()
        {
            var conceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            
            // Create namespace folder structure
            var namespacePath = Path.Combine(conceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios");
            Directory.CreateDirectory(namespacePath);

            // Create conceptual content for SimpleClass with mixed content
            var classPath = Path.Combine(namespacePath, "SimpleClass");
            Directory.CreateDirectory(classPath);

            // Usage has placeholder
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.UsageFileName),
                "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nThis is placeholder usage");
            
            // BestPractices has placeholder followed by real content
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.BestPracticesFileName),
                "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nThis is placeholder best practices\n\nThis is real best practices after placeholder");
            
            // Patterns has only real content
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.PatternsFileName),
                "This is real patterns content");
        }

        private async Task CreateNestedNamespacePlaceholderContentAsync()
        {
            var conceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            
            // Create namespace folder structure
            var namespacePath = Path.Combine(conceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios");
            Directory.CreateDirectory(namespacePath);

            // Create namespace-level documentation
            await File.WriteAllTextAsync(Path.Combine(namespacePath, DocConstants.UsageFileName),
                "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nThis is placeholder namespace usage");
            
            await File.WriteAllTextAsync(Path.Combine(namespacePath, DocConstants.ExamplesFileName),
                "This is real namespace examples");
        }

        private async Task CreateMemberPlaceholderContentAsync()
        {
            var conceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            
            // Create namespace folder structure
            var namespacePath = Path.Combine(conceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios");
            Directory.CreateDirectory(namespacePath);

            // Create conceptual content for SimpleClass
            var classPath = Path.Combine(namespacePath, "SimpleClass");
            Directory.CreateDirectory(classPath);

            // Create member documentation with placeholders
            var memberPath = Path.Combine(classPath, "DoWork");
            Directory.CreateDirectory(memberPath);

            await File.WriteAllTextAsync(Path.Combine(memberPath, DocConstants.UsageFileName),
                "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nThis is placeholder member usage");
            
            await File.WriteAllTextAsync(Path.Combine(memberPath, DocConstants.ExamplesFileName),
                "This is real member examples");
        }

        private async Task CreateParameterPlaceholderContentAsync()
        {
            var conceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            
            // Create namespace folder structure
            var namespacePath = Path.Combine(conceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios");
            Directory.CreateDirectory(namespacePath);

            // Create conceptual content for ClassWithMethods
            var classPath = Path.Combine(namespacePath, "ClassWithMethods");
            Directory.CreateDirectory(classPath);

            // Create member documentation
            var memberPath = Path.Combine(classPath, "Calculate");
            Directory.CreateDirectory(memberPath);

            // Create parameter documentation with placeholder
            var paramPath = Path.Combine(memberPath, "a");
            Directory.CreateDirectory(paramPath);

            await File.WriteAllTextAsync(Path.Combine(paramPath, DocConstants.UsageFileName),
                "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nThis is placeholder parameter usage");
        }

        #endregion

        #region Test Support Classes

        private class TestEnricher : IDocEnricher
        {
            public string Name { get; }
            public bool Executed { get; private set; }

            public TestEnricher(string name)
            {
                Name = name;
            }

            public Task EnrichAsync(DocEntity entity, ProjectContext context)
            {
                Executed = true;
                return Task.CompletedTask;
            }
        }

        private class TestTransformer : IDocTransformer
        {
            public string Name { get; }
            public bool Executed { get; private set; }

            public TestTransformer(string name)
            {
                Name = name;
            }

            public Task TransformAsync(DocEntity entity, ProjectContext context)
            {
                Executed = true;
                return Task.CompletedTask;
            }
        }

        private class TestRenderer : IDocRenderer
        {
            public string Name { get; }
            public bool Executed { get; private set; }

            public TestRenderer(string name)
            {
                Name = name;
            }

            public Task RenderAsync(DocAssembly model, string outputPath, ProjectContext context)
            {
                Executed = true;
                return Task.CompletedTask;
            }
        }

        #endregion

    }

}