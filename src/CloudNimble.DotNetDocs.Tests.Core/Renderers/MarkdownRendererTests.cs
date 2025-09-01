using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Assemblies;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Core.Renderers;
using CloudNimble.DotNetDocs.Tests.Shared;
using CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core.Renderers
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
        private ProjectContext _context = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            _testOutputPath = Path.Combine(Path.GetTempPath(), $"MDRendererTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testOutputPath);
            _context = new ProjectContext
            {
                OutputPath = _testOutputPath
            };
            _renderer = new MarkdownRenderer(_context);
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
            var baselinePath = Path.Combine(projectPath, "Baselines", "MarkdownRenderer", "FileMode", "index.md");
            
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
                var namespaceName = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
                var nsFileName = $"{namespaceName.Replace('.', context.FileNamingOptions.NamespaceSeparator)}.md";
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
                    var namespaceName = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
                    var safeTypeName = type!.Symbol.Name
                        .Replace('<', '_')
                        .Replace('>', '_')
                        .Replace('`', '_');
                    var typeFileName = $"{namespaceName.Replace('.', context.FileNamingOptions.NamespaceSeparator)}.{safeTypeName}.md";
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
            content.Should().Contain("## Usage");
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
            var namespaceName = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var typeFileName = $"{namespaceName.Replace('.', context.FileNamingOptions.NamespaceSeparator)}.ClassWithMethods.md";
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
            // Generate baselines for both FileMode and FolderMode
            await GenerateFileModeBaselines(projectPath);
            await GenerateFolderModeBaselines(projectPath);
        }

        private async Task GenerateFileModeBaselines(string projectPath)
        {
            // Setup with FileMode context
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '-')
            };
            var renderer = new MarkdownRenderer(context);
            var tempOutputPath = Path.Combine(Path.GetTempPath(), $"MDBaseline_FileMode_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempOutputPath);

            try
            {
                // Generate baseline for SimpleClass documentation
                var assemblyPath = typeof(SimpleClass).Assembly.Location;
                var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
                using var manager = new AssemblyManager(assemblyPath, xmlPath);
                var model = await manager.DocumentAsync();

                await renderer.RenderAsync(model, tempOutputPath, context);

                // Create baselines directory
                var baselinesDir = Path.Combine(projectPath, "Baselines", "MarkdownRenderer", "FileMode");
                if (Directory.Exists(baselinesDir))
                {
                    Directory.Delete(baselinesDir, true);
                }
                Directory.CreateDirectory(baselinesDir);

                // Copy all rendered files to baselines
                foreach (var file in Directory.GetFiles(tempOutputPath, "*.md", SearchOption.AllDirectories))
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

        private async Task GenerateFolderModeBaselines(string projectPath)
        {
            // Setup with FolderMode context
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder, '-')
            };
            var renderer = new MarkdownRenderer(context);
            var tempOutputPath = Path.Combine(Path.GetTempPath(), $"MDBaseline_FolderMode_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempOutputPath);

            try
            {
                // Generate baseline for SimpleClass documentation
                var assemblyPath = typeof(SimpleClass).Assembly.Location;
                var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
                using var manager = new AssemblyManager(assemblyPath, xmlPath);
                var model = await manager.DocumentAsync();

                await renderer.RenderAsync(model, tempOutputPath, context);

                // Create baselines directory
                var baselinesDir = Path.Combine(projectPath, "Baselines", "MarkdownRenderer", "FolderMode");
                if (Directory.Exists(baselinesDir))
                {
                    Directory.Delete(baselinesDir, true);
                }
                Directory.CreateDirectory(baselinesDir);

                // Copy entire folder structure preserving hierarchy
                CopyDirectoryRecursive(tempOutputPath, baselinesDir);
            }
            finally
            {
                if (Directory.Exists(tempOutputPath))
                {
                    Directory.Delete(tempOutputPath, true);
                }
            }
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destDir)
        {
            // Copy all files from the source directory
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            // Recursively copy subdirectories
            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(subDir);
                var destSubDir = Path.Combine(destDir, dirName);
                Directory.CreateDirectory(destSubDir);
                CopyDirectoryRecursive(subDir, destSubDir);
            }
        }

        #endregion

        #region FileNamingOptions Tests

        [TestMethod]
        public async Task RenderAsync_WithFileModeDefaultSeparator_CreatesFilesWithHyphen()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '-')
            };
            var renderer = new MarkdownRenderer(context);

            // Act
            await renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var files = Directory.GetFiles(_testOutputPath, "*.md");
            files.Should().Contain(f => Path.GetFileName(f).Contains("CloudNimble-DotNetDocs-Tests-Shared"));
        }

        [TestMethod]
        public async Task RenderAsync_WithFileModeUnderscoreSeparator_CreatesFilesWithUnderscore()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '_')
            };
            var renderer = new MarkdownRenderer(context);

            // Act
            await renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var files = Directory.GetFiles(_testOutputPath, "*.md");
            files.Should().Contain(f => Path.GetFileName(f).Contains("CloudNimble_DotNetDocs_Tests_Shared"));
        }

        [TestMethod]
        public async Task RenderAsync_WithFileModePeriodSeparator_CreatesFilesWithPeriod()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '.')
            };
            var renderer = new MarkdownRenderer(context);

            // Act
            await renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var files = Directory.GetFiles(_testOutputPath, "*.md");
            files.Should().Contain(f => Path.GetFileName(f).Contains("CloudNimble.DotNetDocs.Tests.Shared"));
        }

        [TestMethod]
        public async Task RenderAsync_WithFolderMode_CreatesNamespaceFolderStructure()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder)
            };
            var renderer = new MarkdownRenderer(context);

            // Act
            await renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            // Check folder structure exists
            var cloudNimbleDir = Path.Combine(_testOutputPath, "CloudNimble");
            Directory.Exists(cloudNimbleDir).Should().BeTrue();
            
            var dotNetDocsDir = Path.Combine(cloudNimbleDir, "DotNetDocs");
            Directory.Exists(dotNetDocsDir).Should().BeTrue();
            
            var testsDir = Path.Combine(dotNetDocsDir, "Tests");
            Directory.Exists(testsDir).Should().BeTrue();
            
            var sharedDir = Path.Combine(testsDir, "Shared");
            Directory.Exists(sharedDir).Should().BeTrue();
            
            // Check that index.md exists in namespace folder
            var indexFile = Path.Combine(sharedDir, "index.md");
            File.Exists(indexFile).Should().BeTrue();
            
            // Check that type files exist in namespace folder
            var typeFiles = Directory.GetFiles(sharedDir, "*.md");
            typeFiles.Should().Contain(f => Path.GetFileName(f) != "index.md", "Type files should exist alongside index.md");
        }

        [TestMethod]
        public async Task RenderAsync_WithFolderMode_IgnoresNamespaceSeparator()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            
            // Set a separator that should be ignored in Folder mode
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder, '_')
            };
            var renderer = new MarkdownRenderer(context);

            // Act
            await renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            // Verify folder structure uses path separators, not the configured separator
            var cloudNimbleDir = Path.Combine(_testOutputPath, "CloudNimble");
            Directory.Exists(cloudNimbleDir).Should().BeTrue();
            
            // Should NOT have a folder named "CloudNimble_DotNetDocs_Tests_Shared"
            var wrongDir = Path.Combine(_testOutputPath, "CloudNimble_DotNetDocs_Tests_Shared");
            Directory.Exists(wrongDir).Should().BeFalse();
        }

        #endregion

        #region Folder Structure Baseline Tests

        [TestMethod]
        public async Task RenderAsync_WithFolderMode_MatchesFolderStructureBaseline()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder)
            };
            var renderer = new MarkdownRenderer(context);
            var testOutputPath = Path.Combine(Path.GetTempPath(), $"MDFolderBaseline_{Guid.NewGuid()}");

            try
            {
                // Get the test model
                var assemblyPath = typeof(SampleClass).Assembly.Location;
                var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
                using var manager = new AssemblyManager(assemblyPath, xmlPath);
                var projectContext = new ProjectContext([Accessibility.Public, Accessibility.Internal]) 
                { 
                    ShowPlaceholders = false,
                    FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder)
                };
                var model = await manager.DocumentAsync(projectContext);

                // Act
                await renderer.RenderAsync(model, testOutputPath, projectContext);

                // Assert - Verify folder structure
                var cloudNimbleDir = Path.Combine(testOutputPath, "CloudNimble");
                Directory.Exists(cloudNimbleDir).Should().BeTrue();
                
                var dotNetDocsDir = Path.Combine(cloudNimbleDir, "DotNetDocs");
                Directory.Exists(dotNetDocsDir).Should().BeTrue();
                
                var testsDir = Path.Combine(dotNetDocsDir, "Tests");
                Directory.Exists(testsDir).Should().BeTrue();
                
                var sharedDir = Path.Combine(testsDir, "Shared");
                Directory.Exists(sharedDir).Should().BeTrue();

                // Verify namespace index files exist 
                File.Exists(Path.Combine(sharedDir, "index.md")).Should().BeTrue();
                
                // Verify type files exist in namespace folders
                File.Exists(Path.Combine(sharedDir, "SampleClass.md")).Should().BeTrue();
                
                // Verify sub-namespace folders
                var basicScenariosDir = Path.Combine(sharedDir, "BasicScenarios");
                Directory.Exists(basicScenariosDir).Should().BeTrue();
                File.Exists(Path.Combine(basicScenariosDir, "index.md")).Should().BeTrue();
                File.Exists(Path.Combine(basicScenariosDir, "SimpleClass.md")).Should().BeTrue();

                // Compare specific files with baselines
                await CompareWithFolderBaseline(
                    Path.Combine(sharedDir, "index.md"),
                    "CloudNimble/DotNetDocs/Tests/Shared/index.md");

                await CompareWithFolderBaseline(
                    Path.Combine(basicScenariosDir, "SimpleClass.md"),
                    "CloudNimble/DotNetDocs/Tests/Shared/BasicScenarios/SimpleClass.md");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testOutputPath))
                {
                    Directory.Delete(testOutputPath, true);
                }
            }
        }

        private async Task CompareWithFolderBaseline(string actualFilePath, string baselineRelativePath)
        {
            // Read actual file
            var actualContent = await File.ReadAllTextAsync(actualFilePath);
            
            // Construct baseline path
            var baselineDir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Baselines",
                "MarkdownRenderer",
                "FolderMode");
            var baselinePath = Path.Combine(baselineDir, baselineRelativePath);
            
            // If baseline doesn't exist, create it for first run
            if (!File.Exists(baselinePath))
            {
                var directory = Path.GetDirectoryName(baselinePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                await File.WriteAllTextAsync(baselinePath, actualContent);
                Assert.Inconclusive($"Baseline created at: {baselinePath}. Re-run test to verify.");
            }
            
            // Compare with baseline
            var baselineContent = await File.ReadAllTextAsync(baselinePath);
            actualContent.Should().Be(baselineContent, 
                $"Output should match baseline at {baselinePath}");
        }

        #endregion

        #region RenderAssemblyAsync Tests

        [TestMethod]
        public async Task RenderAssemblyAsync_Should_Create_Index_File()
        {
            var assembly = GetTestsDotSharedAssembly();

            await _renderer.RenderAssemblyAsync(assembly, _testOutputPath);

            var indexPath = Path.Combine(_testOutputPath, "index.md");
            File.Exists(indexPath).Should().BeTrue();
        }

        [TestMethod]
        public async Task RenderAssemblyAsync_Should_Include_Assembly_Name()
        {
            var assembly = GetTestsDotSharedAssembly();

            await _renderer.RenderAssemblyAsync(assembly, _testOutputPath);

            var indexPath = Path.Combine(_testOutputPath, "index.md");
            var content = await File.ReadAllTextAsync(indexPath);
            content.Should().Contain($"# {assembly.AssemblyName}");
        }

        [TestMethod]
        public async Task RenderAssemblyAsync_Should_Include_Usage_When_Present()
        {
            var assembly = GetTestsDotSharedAssembly();
            assembly.Usage = "This is the assembly usage documentation.";

            await _renderer.RenderAssemblyAsync(assembly, _testOutputPath);

            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.md"));
            content.Should().Contain("## Usage");
            content.Should().Contain(assembly.Usage);
        }

        [TestMethod]
        public async Task RenderAssemblyAsync_Should_List_Namespaces()
        {
            var assembly = GetTestsDotSharedAssembly();

            await _renderer.RenderAssemblyAsync(assembly, _testOutputPath);

            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.md"));
            content.Should().Contain("## Namespaces");
            foreach (var ns in assembly.Namespaces)
            {
                var namespaceName = ns.Symbol.IsGlobalNamespace ? "global" : ns.Symbol.ToDisplayString();
                content.Should().Contain($"- [{namespaceName}]");
            }
            
            // Global namespace should never be in the list
            assembly.Namespaces.Should().NotContain(ns => ns.Symbol.IsGlobalNamespace);
        }

        #endregion

        #region RenderNamespaceAsync Tests

        [TestMethod]
        public async Task RenderNamespaceAsync_Should_Create_Namespace_File()
        {
            var assembly = GetTestsDotSharedAssembly();
            
            assembly.Namespaces.Should().NotBeEmpty("Test assembly should contain namespaces");
            
            var ns = assembly.Namespaces.First();

            await _renderer.RenderNamespaceAsync(ns, _testOutputPath);

            var namespaceName = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var nsPath = Path.Combine(_testOutputPath, $"{namespaceName.Replace('.', _context.FileNamingOptions.NamespaceSeparator)}.md");
            File.Exists(nsPath).Should().BeTrue();
        }

        [TestMethod]
        public async Task RenderNamespaceAsync_Should_List_Types_By_Category()
        {
            var assembly = GetTestsDotSharedAssembly();
            
            assembly.Namespaces.Should().NotBeEmpty("Test assembly should contain namespaces");
            
            var ns = assembly.Namespaces.First();

            await _renderer.RenderNamespaceAsync(ns, _testOutputPath);

            var namespaceName = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var nsPath = Path.Combine(_testOutputPath, $"{namespaceName.Replace('.', _context.FileNamingOptions.NamespaceSeparator)}.md");
            var content = await File.ReadAllTextAsync(nsPath);

            content.Should().Contain("## Types");
            if (ns.Types.Any(t => t.Symbol.TypeKind == TypeKind.Class))
            {
                content.Should().Contain("### Classes");
            }
            if (ns.Types.Any(t => t.Symbol.TypeKind == TypeKind.Interface))
            {
                content.Should().Contain("### Interfaces");
            }
        }

        #endregion

        #region RenderTypeAsync Tests

        [TestMethod]
        public async Task RenderTypeAsync_Should_Create_Type_File()
        {
            var assembly = GetTestsDotSharedAssembly();
            var ns = assembly.Namespaces.FirstOrDefault(n => n.Types.Any());
            
            ns.Should().NotBeNull("Test assembly should contain a namespace with types");
            
            var type = ns!.Types.First();

            await _renderer.RenderTypeAsync(type, ns, _testOutputPath);

            var safeNamespace = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var safeTypeName = type!.Symbol.Name
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_');
            var separator = _context.FileNamingOptions.NamespaceSeparator;
            var fileName = $"{safeNamespace.Replace('.', separator)}.{safeTypeName}.md";
            var typePath = Path.Combine(_testOutputPath, fileName);
            File.Exists(typePath).Should().BeTrue();
        }

        [TestMethod]
        public async Task RenderTypeAsync_Should_Include_Type_Metadata()
        {
            var assembly = GetTestsDotSharedAssembly();
            var ns = assembly.Namespaces.FirstOrDefault(n => n.Types.Any());
            
            ns.Should().NotBeNull("Test assembly should contain a namespace with types");
            
            var type = ns!.Types.First();

            await _renderer.RenderTypeAsync(type, ns, _testOutputPath);

            var safeNamespace = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var safeTypeName = type!.Symbol.Name
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_');
            var separator = _context.FileNamingOptions.NamespaceSeparator;
            var fileName = $"{safeNamespace.Replace('.', separator)}.{safeTypeName}.md";
            var typePath = Path.Combine(_testOutputPath, fileName);
            var content = await File.ReadAllTextAsync(typePath);

            content.Should().Contain($"# {type.Symbol.Name}");
            content.Should().Contain("## Definition");
            var expectedNamespace = ns.Symbol.IsGlobalNamespace ? "global" : ns.Symbol.ToDisplayString();
            content.Should().Contain($"**Namespace:** {expectedNamespace}");
            content.Should().Contain($"**Assembly:** {type.Symbol.ContainingAssembly?.Name}");
        }

        [TestMethod]
        public async Task RenderTypeAsync_Should_Include_Type_Signature()
        {
            var assembly = GetTestsDotSharedAssembly();
            var ns = assembly.Namespaces.FirstOrDefault(n => n.Types.Any());
            
            ns.Should().NotBeNull("Test assembly should contain a namespace with types");
            
            var type = ns!.Types.First();

            await _renderer.RenderTypeAsync(type, ns, _testOutputPath);

            var safeNamespace = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var safeTypeName = type!.Symbol.Name
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_');
            var separator = _context.FileNamingOptions.NamespaceSeparator;
            var fileName = $"{safeNamespace.Replace('.', separator)}.{safeTypeName}.md";
            var typePath = Path.Combine(_testOutputPath, fileName);
            var content = await File.ReadAllTextAsync(typePath);

            content.Should().Contain("## Syntax");
            content.Should().Contain("```csharp");
        }

        [TestMethod]
        public async Task RenderTypeAsync_Should_Include_Members_By_Category()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithMethods");
            
            type.Should().NotBeNull("ClassWithMethods should exist in test assembly");
            
            var ns = assembly.Namespaces.First(n => n.Types.Contains(type!));

            await _renderer.RenderTypeAsync(type!, ns, _testOutputPath);

            var safeNamespace = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var safeTypeName = type!.Symbol.Name
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_');
            var separator = _context.FileNamingOptions.NamespaceSeparator;
            var fileName = $"{safeNamespace.Replace('.', separator)}.{safeTypeName}.md";
            var typePath = Path.Combine(_testOutputPath, fileName);
            var content = await File.ReadAllTextAsync(typePath);

            content.Should().Contain("## Constructors");
            content.Should().Contain("## Methods");
        }

        #endregion

        #region RenderMember Tests

        [TestMethod]
        public void RenderMember_Should_Include_Member_Name()
        {
            var assembly = GetTestsDotSharedAssembly();
            
            assembly.Namespaces.Should().NotBeEmpty("Test assembly should contain namespaces");
            assembly.Namespaces.First().Types.Should().NotBeEmpty("First namespace should contain types");
            
            var type = assembly.Namespaces.First().Types.First();
            var member = type.Members.FirstOrDefault(m => m.Symbol.Kind == SymbolKind.Method);
            
            member.Should().NotBeNull("Test assembly should contain a method");
            var sb = new StringBuilder();

            _renderer.RenderMember(sb, member!);

            var result = sb.ToString();
            result.Should().Contain($"### {member!.Symbol.Name}");
        }

        [TestMethod]
        public void RenderMember_Should_Include_Syntax_Section()
        {
            var assembly = GetTestsDotSharedAssembly();
            
            assembly.Namespaces.Should().NotBeEmpty("Test assembly should contain namespaces");
            assembly.Namespaces.First().Types.Should().NotBeEmpty("First namespace should contain types");
            
            var type = assembly.Namespaces.First().Types.First();
            var member = type.Members.FirstOrDefault(m => m.Symbol.Kind == SymbolKind.Method);
            
            member.Should().NotBeNull("Test assembly should contain a method");
            var sb = new StringBuilder();

            _renderer.RenderMember(sb, member!);

            var result = sb.ToString();
            result.Should().Contain("#### Syntax");
            result.Should().Contain("```csharp");
        }

        [TestMethod]
        public void RenderMember_Should_Include_Parameters_Table_When_Present()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithMethods");
            
            type.Should().NotBeNull("ClassWithMethods should exist in test assembly");
            var member = type!.Members.FirstOrDefault(m => m.Symbol.Name == "Calculate");
            member.Should().NotBeNull("Calculate method should exist in ClassWithMethods");
            var sb = new StringBuilder();

            _renderer.RenderMember(sb, member!);

            var result = sb.ToString();
            result.Should().Contain("#### Parameters");
            result.Should().Contain("| Name | Type | Description |");
            result.Should().Contain("|------|------|-------------|");
        }

        [TestMethod]
        public void RenderMember_Should_Include_Returns_Section_For_NonVoid_Methods()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithMethods");
            
            type.Should().NotBeNull("ClassWithMethods should exist in test assembly");
            var member = type!.Members.FirstOrDefault(m => m.Symbol.Name == "Calculate");
            member.Should().NotBeNull("Calculate method should exist in ClassWithMethods");
            var sb = new StringBuilder();

            _renderer.RenderMember(sb, member!);

            var result = sb.ToString();
            result.Should().Contain("#### Returns");
            result.Should().Contain("Type: `int`");
        }

        [TestMethod]
        public void RenderMember_Should_Include_Property_Value_Section_For_Properties()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithProperties");
            
            type.Should().NotBeNull("ClassWithProperties should exist in test assembly");
            
            var member = type!.Members.FirstOrDefault(m => m.Symbol.Kind == SymbolKind.Property);
            member.Should().NotBeNull("ClassWithProperties should contain a property");
            var sb = new StringBuilder();

            _renderer.RenderMember(sb, member!);

            var result = sb.ToString();
            result.Should().Contain("#### Property Value");
            result.Should().Contain("Type: `");
        }

        [TestMethod]
        public void RenderMember_Should_Include_Usage_When_Present()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithMethods");
            
            type.Should().NotBeNull("ClassWithMethods should exist in test assembly");
            var member = type!.Members.FirstOrDefault(m => m.Symbol.Name == "Calculate");
            member.Should().NotBeNull("Calculate method should exist in ClassWithMethods");
            var sb = new StringBuilder();

            _renderer.RenderMember(sb, member!);

            var result = sb.ToString();
            if (!string.IsNullOrWhiteSpace(member!.Usage))
            {
                result.Should().Contain(member.Usage);
            }
        }

        #endregion

    }

}