using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Assemblies;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Renderers;
using CloudNimble.DotNetDocs.Tests.Shared;
using CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Tests for the MarkdownRenderer class.
    /// </summary>
    [TestClass]
    public class MarkdownRendererTests : DotNetDocsTestBase
    {

        #region Fields

        private string _testOutputPath = null!;
        private MarkdownRenderer _renderer = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            _testOutputPath = Path.Combine(Path.GetTempPath(), $"MDRendererTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testOutputPath);
            _renderer = new MarkdownRenderer();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (Directory.Exists(_testOutputPath))
            {
                Directory.Delete(_testOutputPath, true);
            }
        }

        #endregion

        #region RenderAsync Tests

        [TestMethod]
        public async Task RenderAsync_ProducesConsistentBaseline()
        {
            // Arrange
            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert - Compare against baseline
            var baselinePath = Path.Combine(projectPath, "Baselines", "MarkdownRenderer", "index.md");
            
            if (File.Exists(baselinePath))
            {
                var baseline = await File.ReadAllTextAsync(baselinePath);
                var actual = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.md"));
                
                actual.Should().Be(baseline,
                    "Markdown output has changed. If this is intentional, regenerate baselines using 'dotnet breakdance generate'");
            }
            else
            {
                Assert.Inconclusive($"Baseline not found at {baselinePath}. Run 'dotnet breakdance generate' to create baselines.");
            }
        }

        [TestMethod]
        public async Task RenderAsync_WithNamespaces_CreatesNamespaceFiles()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            foreach (var ns in model.Namespaces)
            {
                var namespaceName = ns.Symbol.IsGlobalNamespace ? "global" : ns.Symbol.ToDisplayString();
                var nsFileName = $"{namespaceName.Replace('.', '-')}.md";
                var nsPath = Path.Combine(_testOutputPath, nsFileName);
                File.Exists(nsPath).Should().BeTrue($"Namespace file {nsFileName} should exist");
            }
        }

        [TestMethod]
        public async Task RenderAsync_WithTypes_CreatesTypeFiles()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            foreach (var ns in model.Namespaces)
            {
                foreach (var type in ns.Types)
                {
                    var namespaceName = ns.Symbol.IsGlobalNamespace ? "global" : ns.Symbol.ToDisplayString();
                    var safeTypeName = type.Symbol.Name
                        .Replace('<', '_')
                        .Replace('>', '_')
                        .Replace('`', '_');
                    var typeFileName = $"{namespaceName.Replace('.', '-')}.{safeTypeName}.md";
                    var typePath = Path.Combine(_testOutputPath, typeFileName);
                    File.Exists(typePath).Should().BeTrue($"Type file {typeFileName} should exist");
                }
            }
        }

        [TestMethod]
        public async Task RenderAsync_WithNullModel_ThrowsArgumentNullException()
        {
            // Arrange
            var context = new ProjectContext();

            // Act
            Func<Task> act = async () => await _renderer.RenderAsync(null!, _testOutputPath, context);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task RenderAsync_WithNullOutputPath_ThrowsArgumentNullException()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

            // Act
            Func<Task> act = async () => await _renderer.RenderAsync(model, null!, context);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task RenderAsync_WithNullContext_ThrowsArgumentNullException()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();

            // Act
            Func<Task> act = async () => await _renderer.RenderAsync(model, _testOutputPath, null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task RenderAsync_CreatesOutputDirectory_WhenNotExists()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), $"NewDir_{Guid.NewGuid()}");
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

            try
            {
                // Act
                Directory.Exists(nonExistentPath).Should().BeFalse();
                await _renderer.RenderAsync(model, nonExistentPath, context);

                // Assert
                Directory.Exists(nonExistentPath).Should().BeTrue();
            }
            finally
            {
                if (Directory.Exists(nonExistentPath))
                {
                    Directory.Delete(nonExistentPath, true);
                }
            }
        }

        #endregion

        #region Content Tests

        [TestMethod]
        public async Task RenderAsync_IncludesAssemblyUsage_WhenProvided()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            model.Usage = "This is assembly usage documentation";
            var context = new ProjectContext();

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.md"));
            content.Should().Contain("## Overview");
            content.Should().Contain("This is assembly usage documentation");
        }

        [TestMethod]
        public async Task RenderAsync_IncludesAssemblyExamples_WhenProvided()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            model.Examples = "Example code here";
            var context = new ProjectContext();

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.md"));
            content.Should().Contain("## Examples");
            content.Should().Contain("Example code here");
        }

        [TestMethod]
        public async Task RenderAsync_IncludesRelatedApis_WhenProvided()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            model.RelatedApis = new List<string> { "System.Object", "System.String" };
            var context = new ProjectContext();

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.md"));
            content.Should().Contain("## Related APIs");
            content.Should().Contain("- System.Object");
            content.Should().Contain("- System.String");
        }

        [TestMethod]
        public async Task RenderAsync_HandlesEmptyDocumentation_Gracefully()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            // Clear all optional documentation
            model.Usage = string.Empty;
            model.Examples = string.Empty;
            model.BestPractices = string.Empty;
            model.Patterns = string.Empty;
            model.Considerations = string.Empty;
            model.RelatedApis = new List<string>();
            var context = new ProjectContext();

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.md"));
            content.Should().Contain($"# {model.AssemblyName}");
            content.Should().NotContain("## Overview");
            content.Should().NotContain("## Examples");
            content.Should().NotContain("## Best Practices");
            content.Should().NotContain("## Patterns");
            content.Should().NotContain("## Considerations");
            content.Should().NotContain("## Related APIs");
        }

        [TestMethod]
        public async Task RenderAsync_IncludesMemberSignatures_InTypeFiles()
        {
            // Arrange
            var assemblyPath = typeof(ClassWithMethods).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var ns = model.Namespaces.First(n => n.Types.Any(t => t.Symbol.Name == "ClassWithMethods"));
            var namespaceName = ns.Symbol.IsGlobalNamespace ? "global" : ns.Symbol.ToDisplayString();
            var typeFileName = $"{namespaceName.Replace('.', '-')}.ClassWithMethods.md";
            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, typeFileName));
            
            content.Should().Contain("```csharp");
            content.Should().Contain("public");
        }

        #endregion

        #region Baseline Generation

        //[TestMethod]
        //[DataRow(projectPath)]
        [BreakdanceManifestGenerator]
        public async Task GenerateMarkdownBaselines(string projectPath)
        {
            // Setup
            var renderer = new MarkdownRenderer();
            var tempOutputPath = Path.Combine(Path.GetTempPath(), $"MDBaseline_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempOutputPath);

            try
            {
                // Generate baseline for SimpleClass documentation
                var assemblyPath = typeof(SimpleClass).Assembly.Location;
                var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
                using var manager = new AssemblyManager(assemblyPath, xmlPath);
                var model = await manager.DocumentAsync();
                var context = new ProjectContext();

                await renderer.RenderAsync(model, tempOutputPath, context);

                // Create baselines directory
                var baselinesDir = Path.Combine(projectPath, "Baselines", "MarkdownRenderer");
                if (!Directory.Exists(baselinesDir))
                {
                    Directory.CreateDirectory(baselinesDir);
                }

                // Copy rendered files to baselines
                foreach (var file in Directory.GetFiles(tempOutputPath, "*.md"))
                {
                    var fileName = Path.GetFileName(file);
                    var content = await File.ReadAllTextAsync(file);
                    var baselinePath = Path.Combine(baselinesDir, fileName);
                    await File.WriteAllTextAsync(baselinePath, content);
                }
            }
            finally
            {
                if (Directory.Exists(tempOutputPath))
                {
                    Directory.Delete(tempOutputPath, true);
                }
            }
        }

        #endregion

    }

}