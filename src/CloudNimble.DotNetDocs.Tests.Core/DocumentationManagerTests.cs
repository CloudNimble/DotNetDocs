using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Tests for DocumentationManager, which orchestrates the documentation pipeline.
    /// </summary>
    [TestClass]
    public class DocumentationManagerTests : DotNetDocsTestBase
    {

        #region Private Fields

        private string? _tempDirectory;
        private string? _testAssemblyPath;
        private string? _testXmlPath;

        [TestMethod]
        public void GetFilePatternsForDocumentationType_Mintlify_ReturnsCorrectPatterns()
        {
            // Arrange
            var manager = GetDocumentationManager();

            // Act
            var patterns = manager.GetFilePatternsForDocumentationType("Mintlify");

            // Assert
            patterns.Should().NotBeNull();
            patterns.Should().Contain("*.md");
            patterns.Should().Contain("*.mdx");
            patterns.Should().Contain("*.mdz");
            patterns.Should().Contain("docs.json");
            patterns.Should().Contain("images/**/*");
            patterns.Should().Contain("favicon.*");
            patterns.Should().Contain("snippets/**/*");
        }

        [TestMethod]
        public void GetFilePatternsForDocumentationType_DocFX_ReturnsCorrectPatterns()
        {
            // Arrange
            var manager = GetDocumentationManager();

            // Act
            var patterns = manager.GetFilePatternsForDocumentationType("DocFX");

            // Assert
            patterns.Should().NotBeNull();
            patterns.Should().Contain("*.md");
            patterns.Should().Contain("*.yml");
            patterns.Should().Contain("toc.yml");
            patterns.Should().Contain("docfx.json");
            patterns.Should().Contain("images/**/*");
        }

        [TestMethod]
        public void GetFilePatternsForDocumentationType_MkDocs_ReturnsCorrectPatterns()
        {
            // Arrange
            var manager = GetDocumentationManager();

            // Act
            var patterns = manager.GetFilePatternsForDocumentationType("MkDocs");

            // Assert
            patterns.Should().NotBeNull();
            patterns.Should().Contain("*.md");
            patterns.Should().Contain("mkdocs.yml");
            patterns.Should().Contain("docs/**/*");
        }

        [TestMethod]
        public void GetFilePatternsForDocumentationType_Unknown_ReturnsDefaultPatterns()
        {
            // Arrange
            var manager = GetDocumentationManager();

            // Act
            var patterns = manager.GetFilePatternsForDocumentationType("Unknown");

            // Assert
            patterns.Should().NotBeNull();
            patterns.Should().Contain("*.md");
            patterns.Should().Contain("*.html");
            patterns.Should().Contain("images/**/*");
            patterns.Should().Contain("assets/**/*");
        }

        [TestMethod]
        public async Task CopyFilesAsync_SimplePattern_CopiesMatchingFiles()
        {
            // Arrange
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(sourceDir);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "test1.md"), "Test content 1");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "test2.md"), "Test content 2");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "test.txt"), "Not copied");

            // Act
            await manager.CopyFilesAsync(sourceDir, destDir, "*.md", skipExisting: true);

            // Assert
            File.Exists(Path.Combine(destDir, "test1.md")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "test2.md")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "test.txt")).Should().BeFalse();
        }

        [TestMethod]
        public async Task CopyFilesAsync_SkipExisting_PreservesExistingFiles()
        {
            // Arrange
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destDir);

            await File.WriteAllTextAsync(Path.Combine(sourceDir, "test.md"), "New content");
            await File.WriteAllTextAsync(Path.Combine(destDir, "test.md"), "Original content");

            // Act
            await manager.CopyFilesAsync(sourceDir, destDir, "*.md", skipExisting: true);

            // Assert
            var content = await File.ReadAllTextAsync(Path.Combine(destDir, "test.md"));
            content.Should().Be("Original content", "existing files should not be overwritten when skipExisting is true");
        }

        [TestMethod]
        public async Task CopyDirectoryRecursiveAsync_PreservesStructure()
        {
            // Arrange
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(Path.Combine(sourceDir, "subdir1"));
            Directory.CreateDirectory(Path.Combine(sourceDir, "subdir2"));
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "root.md"), "Root file");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "subdir1", "file1.md"), "File 1");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "subdir2", "file2.md"), "File 2");

            // Act
            await manager.CopyDirectoryRecursiveAsync(sourceDir, destDir, "*", skipExisting: true);

            // Assert
            File.Exists(Path.Combine(destDir, "root.md")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "subdir1", "file1.md")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "subdir2", "file2.md")).Should().BeTrue();
        }

        [TestMethod]
        public async Task CopyReferencedDocumentationAsync_ProcessesAllReferences()
        {
            // Arrange
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();

            var sourceDir1 = Path.Combine(_tempDirectory!, "ref1");
            var sourceDir2 = Path.Combine(_tempDirectory!, "ref2");
            Directory.CreateDirectory(sourceDir1);
            Directory.CreateDirectory(sourceDir2);

            await File.WriteAllTextAsync(Path.Combine(sourceDir1, "test1.md"), "Content 1");
            await File.WriteAllTextAsync(Path.Combine(sourceDir2, "test2.md"), "Content 2");

            context.DocumentationReferences.Add(new DocumentationReference
            {
                DocumentationRoot = sourceDir1,
                DestinationPath = "ref1",
                DocumentationType = "Mintlify"
            });

            context.DocumentationReferences.Add(new DocumentationReference
            {
                DocumentationRoot = sourceDir2,
                DestinationPath = "ref2",
                DocumentationType = "Mintlify"
            });

            // Act
            await manager.CopyReferencedDocumentationAsync();

            // Assert
            File.Exists(Path.Combine(context.DocumentationRootPath, "ref1", "test1.md")).Should().BeTrue();
            File.Exists(Path.Combine(context.DocumentationRootPath, "ref2", "test2.md")).Should().BeTrue();
        }

        #endregion

        #region Helper Methods

        private DocumentationManager GetDocumentationManager()
        {
            var manager = GetService<DocumentationManager>();
            manager.Should().NotBeNull("DocumentationManager should be registered in DI");
            return manager!;
        }

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            Setup();

            // Configure services for DI
            TestHostBuilder.ConfigureServices((context, services) =>
            {
                services.AddDotNetDocsCore(ctx =>
                {
                    ctx.DocumentationRootPath = Path.Combine(_tempDirectory ?? Path.GetTempPath(), "output");
                });
            });

            TestSetup();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestTearDown();
        }

        #endregion

        #region Test Methods

        [TestMethod]
        public async Task ProcessAsync_WithConceptualPath_LoadsConceptualContent()
        {
            // Arrange
            await CreateConceptualContentAsync();

            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");

            // Since ProcessAsync runs the full pipeline, we'll test LoadConceptual directly
            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading directly
            await manager.LoadConceptualAsync(model);

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

            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading directly
            await manager.LoadConceptualAsync(model);

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

            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");

            var manager = new DocumentationManager(
                context,
                [enricher],
                [transformer],
                [renderer]
            );

            // Act
            await manager.ProcessAsync(_testAssemblyPath!, _testXmlPath!);

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

            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            context.ShowPlaceholders = false; // Hide placeholders

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading
            await manager.LoadConceptualAsync(model);

            // Assert - properties that had placeholder content should not contain the placeholder text
            var testClass = model.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SimpleClass");

            testClass.Should().NotBeNull();
            // The Usage property should remain unchanged from XML docs since placeholder was skipped
            testClass!.Usage.Should().NotContain("TODO: REMOVE THIS COMMENT");
            testClass.Usage.Should().NotContain("This is placeholder usage");
            
            // BestPractices should be null since it had placeholder content and was skipped
            testClass.BestPractices.Should().BeNull();

            // Examples was set to real content, should have that content
            testClass.Examples.Should().Be("This is real conceptual examples");
        }

        [TestMethod]
        public async Task ProcessAsync_WithShowPlaceholdersTrue_IncludesPlaceholderContent()
        {
            // Arrange
            await CreateConceptualContentWithPlaceholdersAsync();

            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            context.ShowPlaceholders = true; // Show placeholders

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading
            await manager.LoadConceptualAsync(model);

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

            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            context.ShowPlaceholders = false; // Hide placeholders

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading
            await manager.LoadConceptualAsync(model);

            // Assert
            var testClass = model.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SimpleClass");

            testClass.Should().NotBeNull();
            // Usage had placeholder, should not contain placeholder text
            testClass!.Usage.Should().NotContain("TODO: REMOVE THIS COMMENT");
            
            // BestPractices had placeholder marker, should be skipped entirely
            testClass.BestPractices.Should().BeNull();
            
            // Patterns had no placeholder, should have full content
            testClass.Patterns.Should().Be("This is real patterns content");
        }

        [TestMethod]
        public async Task ProcessAsync_WithNestedNamespacePlaceholders_HandlesCorrectly()
        {
            // Arrange
            await CreateNestedNamespacePlaceholderContentAsync();

            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            context.ShowPlaceholders = false; // Hide placeholders

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading
            await manager.LoadConceptualAsync(model);

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

            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            context.ShowPlaceholders = false; // Hide placeholders

            // Get the model first from AssemblyManager
            using var assemblyComponent = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyComponent.DocumentAsync(context);

            // Apply conceptual loading
            await manager.LoadConceptualAsync(model);

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

            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            context.ShowPlaceholders = false; // Hide placeholders

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading
            await manager.LoadConceptualAsync(model);

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

        // TestInitialize is already defined in the Test Lifecycle region above
        // This method is now called from TestInitialize
        private void Setup()
        {
            // Use the real Tests.Shared assembly and its XML documentation
            _testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            _testXmlPath = Path.ChangeExtension(_testAssemblyPath, ".xml");

            _tempDirectory = Path.Combine(Path.GetTempPath(), $"DocManagerTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDirectory);
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

            public Task EnrichAsync(DocEntity entity)
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

            public Task TransformAsync(DocEntity entity)
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

            public Task RenderAsync(DocAssembly model)
            {
                Executed = true;
                return Task.CompletedTask;
            }

            public Task RenderPlaceholdersAsync(DocAssembly model)
            {
                return Task.CompletedTask;
            }

            public Task CombineReferencedNavigationAsync(List<DocumentationReference> references)
            {
                return Task.CompletedTask;
            }
        }

        #endregion

    }

}