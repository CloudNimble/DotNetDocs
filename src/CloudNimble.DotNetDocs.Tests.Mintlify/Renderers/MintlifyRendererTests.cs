using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Assemblies;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Mintlify;
using CloudNimble.DotNetDocs.Tests.Shared;
using CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;

namespace CloudNimble.DotNetDocs.Tests.Mintlify.Renderers
{

    /// <summary>
    /// Tests for the MintlifyRenderer class.
    /// </summary>
    [TestClass]
    public class MintlifyRendererTests : DotNetDocsTestBase
    {

        #region Fields

        private string _testOutputPath = null!;

        #endregion

        #region Helper Methods

        private MintlifyRenderer GetMintlifyRenderer()
        {
            var renderer = GetServices<IDocRenderer>()
                .OfType<MintlifyRenderer>()
                .FirstOrDefault();
            renderer.Should().NotBeNull("MintlifyRenderer should be registered in DI");
            return renderer!;
        }

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            _testOutputPath = Path.Combine(Path.GetTempPath(), $"MintlifyRendererTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testOutputPath);
            
            // Configure services for DI
            TestHostBuilder.ConfigureServices((context, services) =>
            {
                services.AddDotNetDocsPipeline(pipeline =>
                {
                    pipeline.UseMintlifyRenderer()
                        .ConfigureContext(ctx =>
                        {
                            ctx.DocumentationRootPath = _testOutputPath;
                            ctx.ApiReferencePath = "api-reference";
                        });
                });
            });
            
            TestSetup();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestTearDown();
            
            if (Directory.Exists(_testOutputPath))
            {
                Directory.Delete(_testOutputPath, true);
            }
        }

        #endregion

        #region RenderAsync Tests

        [TestMethod]
        public async Task RenderAsync_CreatesNamespaceFiles()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();

            // Act
            await GetMintlifyRenderer().RenderAsync(model);

            // Assert
            var context = GetService<ProjectContext>();
            var renderer = GetMintlifyRenderer();
            foreach (var ns in model.Namespaces)
            {
                var nsFileName = renderer.GetNamespaceFileName(ns, "mdx");
                var nsPath = Path.Combine(_testOutputPath, context.ApiReferencePath, nsFileName);
                File.Exists(nsPath).Should().BeTrue($"Namespace file {nsFileName} should exist");
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

            // Act
            await GetMintlifyRenderer().RenderAsync(model);

            // Assert
            var context = GetService<ProjectContext>();
            var renderer = GetMintlifyRenderer();
            foreach (var ns in model.Namespaces)
            {
                var nsFileName = renderer.GetNamespaceFileName(ns, "mdx");
                var nsPath = Path.Combine(_testOutputPath, context.ApiReferencePath, nsFileName);
                File.Exists(nsPath).Should().BeTrue($"Namespace file {nsFileName} should exist");
            }
        }

        [TestMethod]
        public async Task RenderAsync_WithNullModel_ThrowsArgumentNullException()
        {
            // Act
            Func<Task> act = async () => await GetMintlifyRenderer().RenderAsync(null!);

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

            // Create a new renderer with custom context for this test
            var context = new ProjectContext { DocumentationRootPath = nonExistentPath };
            var renderer = new MintlifyRenderer(
                context,
                GetServices<IOptions<MintlifyRendererOptions>>().FirstOrDefault()!,
                GetServices<DocsJsonManager>().FirstOrDefault()!
            );

            try
            {
                // Act
                Directory.Exists(nonExistentPath).Should().BeFalse();
                await renderer.RenderAsync(model);

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

        #region Frontmatter Tests

        [TestMethod]
        public async Task RenderAsync_WithTypes_CreatesTypeFiles()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();

            // Act
            await GetMintlifyRenderer().RenderAsync(model);

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
                    var typeFileName = $"{namespaceName.Replace('.', '-')}.{safeTypeName}.mdx";
                    var typePath = Path.Combine(_testOutputPath, "api-reference", typeFileName);
                    File.Exists(typePath).Should().BeTrue($"Type file {typeFileName} should exist");
                }
            }
        }

        [TestMethod]
        public async Task RenderAsync_UsesMintlifyIcons_ForTypes()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

            // Act
            await GetMintlifyRenderer().RenderAsync(model);

            // Assert - Check that type files have appropriate icons
            var sampleClassFile = Directory.GetFiles(Path.Combine(_testOutputPath, "api-reference"), "*SampleClass.mdx").FirstOrDefault();
            sampleClassFile.Should().NotBeNull();
            
            var content = await File.ReadAllTextAsync(sampleClassFile!, TestContext.CancellationTokenSource.Token);
            content.Should().Contain("icon: file-brackets-curly", "Class should use file-code icon");
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
            await GetMintlifyRenderer().RenderAsync(model);

            // Assert
            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "api-reference", "index.mdx"), TestContext.CancellationTokenSource.Token);
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
            await GetMintlifyRenderer().RenderAsync(model);

            // Assert
            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "api-reference", "index.mdx"), TestContext.CancellationTokenSource.Token);
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
            await GetMintlifyRenderer().RenderAsync(model);

            // Assert
            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "api-reference", "index.mdx"), TestContext.CancellationTokenSource.Token);
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
            await GetMintlifyRenderer().RenderAsync(model);

            // Assert
            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "api-reference", "index.mdx"), TestContext.CancellationTokenSource.Token);
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
            await GetMintlifyRenderer().RenderAsync(model);

            // Assert
            var ns = model.Namespaces.First(n => n.Types.Any(t => t.Symbol.Name == "ClassWithMethods"));
            var namespaceName = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var typeFileName = $"{namespaceName.Replace('.', '-')}.ClassWithMethods.mdx";
            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "api-reference", typeFileName), TestContext.CancellationTokenSource.Token);
            
            content.Should().Contain("```csharp");
            content.Should().Contain("public");
        }

        #endregion

        #region Baseline Generation

        //[TestMethod]
        //[DataRow(projectPath)]
        [BreakdanceManifestGenerator]
        public async Task GenerateMintlifyBaselines(string projectPath)
        {
            // Generate baselines for both FileMode and FolderMode
            await GenerateFileModeBaselines(projectPath);
            await GenerateFolderModeBaselines(projectPath);
        }

        private async Task GenerateFileModeBaselines(string projectPath)
        {
            // Setup with FileMode context
            var tempOutputPath = Path.Combine(Path.GetTempPath(), $"MintlifyBaseline_FileMode_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempOutputPath);

            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '-'),
                DocumentationRootPath = tempOutputPath,
                ApiReferencePath = string.Empty
            };
            var options = Options.Create(new MintlifyRendererOptions());
            var docsJsonManager = new DocsJsonManager();
            var renderer = new MintlifyRenderer(context, options, docsJsonManager);

            try
            {
                // Generate baseline for SimpleClass documentation
                var assemblyPath = typeof(SimpleClass).Assembly.Location;
                var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
                using var manager = new AssemblyManager(assemblyPath, xmlPath);
                var model = await manager.DocumentAsync();

                await renderer.RenderAsync(model);

                // Create baselines directory
                var baselinesDir = Path.Combine(projectPath, "Baselines", "MintlifyRenderer", "FileMode");
                if (Directory.Exists(baselinesDir))
                {
                    Directory.Delete(baselinesDir, true);
                }
                Directory.CreateDirectory(baselinesDir);

                // Copy all rendered files to baselines
                foreach (var file in Directory.GetFiles(tempOutputPath, "*.mdx", SearchOption.AllDirectories))
                {
                    var fileName = Path.GetFileName(file);
                    var content = await File.ReadAllTextAsync(file, TestContext.CancellationTokenSource.Token);
                    var baselinePath = Path.Combine(baselinesDir, fileName);
                    await File.WriteAllTextAsync(baselinePath, content, TestContext.CancellationTokenSource.Token);
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
            var tempOutputPath = Path.Combine(Path.GetTempPath(), $"MintlifyBaseline_FolderMode_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempOutputPath);

            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder, '-'),
                DocumentationRootPath = tempOutputPath,
                ApiReferencePath = string.Empty
            };
            var options = Options.Create(new MintlifyRendererOptions());
            var docsJsonManager = new DocsJsonManager();
            var renderer = new MintlifyRenderer(context, options, docsJsonManager);

            try
            {
                // Generate baseline for SimpleClass documentation
                var assemblyPath = typeof(SimpleClass).Assembly.Location;
                var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
                using var manager = new AssemblyManager(assemblyPath, xmlPath);
                var model = await manager.DocumentAsync();

                await renderer.RenderAsync(model);

                // Create baselines directory
                var baselinesDir = Path.Combine(projectPath, "Baselines", "MintlifyRenderer", "FolderMode");
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
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '-'),
                DocumentationRootPath = _testOutputPath,
                ApiReferencePath = string.Empty
            };
            var options = Options.Create(new MintlifyRendererOptions());
            var docsJsonManager = new DocsJsonManager();
            var renderer = new MintlifyRenderer(context, options, docsJsonManager);

            // Act
            await renderer.RenderAsync(model);

            // Assert
            var files = Directory.GetFiles(_testOutputPath, "*.mdx");
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
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '_'),
                DocumentationRootPath = _testOutputPath,
                ApiReferencePath = string.Empty
            };
            var options = Options.Create(new MintlifyRendererOptions());
            var docsJsonManager = new DocsJsonManager();
            var renderer = new MintlifyRenderer(context, options, docsJsonManager);

            // Act
            await renderer.RenderAsync(model);

            // Assert
            var files = Directory.GetFiles(_testOutputPath, "*.mdx");
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
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '.'),
                DocumentationRootPath = _testOutputPath,
                ApiReferencePath = string.Empty
            };
            var options = Options.Create(new MintlifyRendererOptions());
            var docsJsonManager = new DocsJsonManager();
            var renderer = new MintlifyRenderer(context, options, docsJsonManager);

            // Act
            await renderer.RenderAsync(model);

            // Assert
            var files = Directory.GetFiles(_testOutputPath, "*.mdx");
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
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder),
                DocumentationRootPath = _testOutputPath,
                ApiReferencePath = string.Empty
            };
            var options = Options.Create(new MintlifyRendererOptions());
            var docsJsonManager = new DocsJsonManager();
            var renderer = new MintlifyRenderer(context, options, docsJsonManager);

            // Act
            await renderer.RenderAsync(model);

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
            
            // Check that index.mdx exists in namespace folder
            var indexFile = Path.Combine(sharedDir, "index.mdx");
            File.Exists(indexFile).Should().BeTrue();
            
            // Check that type files exist in namespace folder
            var typeFiles = Directory.GetFiles(sharedDir, "*.mdx");
            typeFiles.Should().Contain(f => Path.GetFileName(f) != "index.mdx", "Type files should exist alongside index.mdx");
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
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder, '_'),
                DocumentationRootPath = _testOutputPath,
                ApiReferencePath = string.Empty
            };
            var options = Options.Create(new MintlifyRendererOptions());
            var docsJsonManager = new DocsJsonManager();
            var renderer = new MintlifyRenderer(context, options, docsJsonManager);

            // Act
            await renderer.RenderAsync(model);

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
            var testOutputPath = Path.Combine(Path.GetTempPath(), $"MintlifyFolderBaseline_{Guid.NewGuid()}");

            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder),
                DocumentationRootPath = testOutputPath,
                ApiReferencePath = string.Empty
            };
            var options = Options.Create(new MintlifyRendererOptions());
            var docsJsonManager = new DocsJsonManager();
            var renderer = new MintlifyRenderer(context, options, docsJsonManager);

            try
            {
                // Get the test model
                var assemblyPath = typeof(SampleClass).Assembly.Location;
                var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
                using var manager = new AssemblyManager(assemblyPath, xmlPath);
                var projectContext = new ProjectContext([Accessibility.Public, Accessibility.Internal])
                {
                    ShowPlaceholders = false,
                    FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder),
                    ApiReferencePath = string.Empty
                };
                var model = await manager.DocumentAsync(projectContext);

                // Act
                await renderer.RenderAsync(model);

                // Assert - Verify folder structure
                var apiRefDir = Path.Combine(testOutputPath, "api-reference");
                var cloudNimbleDir = Path.Combine(apiRefDir, "CloudNimble");
                Directory.Exists(cloudNimbleDir).Should().BeTrue();

                var dotNetDocsDir = Path.Combine(cloudNimbleDir, "DotNetDocs");
                Directory.Exists(dotNetDocsDir).Should().BeTrue();

                var testsDir = Path.Combine(dotNetDocsDir, "Tests");
                Directory.Exists(testsDir).Should().BeTrue();

                var sharedDir = Path.Combine(testsDir, "Shared");
                Directory.Exists(sharedDir).Should().BeTrue();

                // Verify namespace index files exist
                File.Exists(Path.Combine(sharedDir, "index.mdx")).Should().BeTrue();

                // Verify type files exist in namespace folders
                File.Exists(Path.Combine(sharedDir, "SampleClass.mdx")).Should().BeTrue();

                // Verify sub-namespace folders
                var basicScenariosDir = Path.Combine(sharedDir, "BasicScenarios");
                Directory.Exists(basicScenariosDir).Should().BeTrue();
                File.Exists(Path.Combine(basicScenariosDir, "index.mdx")).Should().BeTrue();
                File.Exists(Path.Combine(basicScenariosDir, "SimpleClass.mdx")).Should().BeTrue();

                // Compare specific files with baselines
                await CompareWithFolderBaseline(
                    Path.Combine(sharedDir, "index.mdx"),
                    "CloudNimble/DotNetDocs/Tests/Shared/index.mdx");

                await CompareWithFolderBaseline(
                    Path.Combine(basicScenariosDir, "SimpleClass.mdx"),
                    "CloudNimble/DotNetDocs/Tests/Shared/BasicScenarios/SimpleClass.mdx");
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
            var actualContent = await File.ReadAllTextAsync(actualFilePath, TestContext.CancellationTokenSource.Token);
            
            // Construct baseline path
            var baselineDir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Baselines",
                "MintlifyRenderer",
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
                await File.WriteAllTextAsync(baselinePath, actualContent, TestContext.CancellationTokenSource.Token);
                Assert.Inconclusive($"Baseline created at: {baselinePath}. Re-run test to verify.");
            }
            
            // Compare with baseline - normalize line endings for cross-platform compatibility
            var baselineContent = await File.ReadAllTextAsync(baselinePath, TestContext.CancellationTokenSource.Token);
            
            // Normalize both to Environment.NewLine to handle any line ending differences
            var normalizedActual = actualContent.ReplaceLineEndings(Environment.NewLine);
            var normalizedBaseline = baselineContent.ReplaceLineEndings(Environment.NewLine);
            
            normalizedActual.Should().Be(normalizedBaseline, 
                $"Output should match baseline at {baselinePath}");
        }

        #endregion

        #region RenderAssemblyAsync Tests

        [TestMethod]
        public async Task RenderAssemblyAsync_Should_Create_Index_File_With_Frontmatter()
        {
            var assembly = GetTestsDotSharedAssembly();

            await GetMintlifyRenderer().RenderAssemblyAsync(assembly, _testOutputPath);

            var context = GetService<ProjectContext>();
            var indexPath = Path.Combine(_testOutputPath, context.ApiReferencePath, "index.mdx");
            File.Exists(indexPath).Should().BeTrue();
            
            // Check frontmatter
            var content = await File.ReadAllTextAsync(indexPath, TestContext.CancellationTokenSource.Token);
            content.Should().StartWith("---");
            content.Should().Contain("title:");
            content.Should().Contain("icon: cube");
        }

        [TestMethod]
        public async Task RenderAssemblyAsync_Should_Include_Assembly_Name()
        {
            var assembly = GetTestsDotSharedAssembly();

            await GetMintlifyRenderer().RenderAssemblyAsync(assembly, _testOutputPath);

            var context = GetService<ProjectContext>();
            var indexPath = Path.Combine(_testOutputPath, context.ApiReferencePath, "index.mdx");
            var content = await File.ReadAllTextAsync(indexPath, TestContext.CancellationTokenSource.Token);
            content.Should().Contain($"# {assembly.AssemblyName}");
        }

        [TestMethod]
        public async Task RenderAssemblyAsync_Should_Include_Usage_When_Present()
        {
            var assembly = GetTestsDotSharedAssembly();
            assembly.Usage = "This is the assembly usage documentation.";

            await GetMintlifyRenderer().RenderAssemblyAsync(assembly, _testOutputPath);

            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "api-reference", "index.mdx"), TestContext.CancellationTokenSource.Token);
            content.Should().Contain("## Usage");
            content.Should().Contain(assembly.Usage);
        }

        [TestMethod]
        public async Task RenderAssemblyAsync_Should_List_Namespaces()
        {
            var assembly = GetTestsDotSharedAssembly();

            await GetMintlifyRenderer().RenderAssemblyAsync(assembly, _testOutputPath);

            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "api-reference", "index.mdx"), TestContext.CancellationTokenSource.Token);
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
        public async Task RenderNamespaceAsync_Should_Create_Namespace_File_With_Frontmatter()
        {
            var assembly = GetTestsDotSharedAssembly();
            
            assembly.Namespaces.Should().NotBeEmpty("Test assembly should contain namespaces");
            
            var ns = assembly.Namespaces.First();

            await GetMintlifyRenderer().RenderNamespaceAsync(ns, _testOutputPath);

            var namespaceName = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var nsPath = Path.Combine(_testOutputPath, $"{namespaceName.Replace('.', GetService<ProjectContext>().FileNamingOptions.NamespaceSeparator)}.mdx");
            File.Exists(nsPath).Should().BeTrue();
            
            // Check frontmatter
            var content = await File.ReadAllTextAsync(nsPath, TestContext.CancellationTokenSource.Token);
            content.Should().StartWith("---");
            content.Should().Contain("title:");
            content.Should().Contain("icon:");
        }

        [TestMethod]
        public async Task RenderNamespaceAsync_Should_List_Types_By_Category()
        {
            var assembly = GetTestsDotSharedAssembly();
            
            assembly.Namespaces.Should().NotBeEmpty("Test assembly should contain namespaces");
            
            var ns = assembly.Namespaces.First();

            await GetMintlifyRenderer().RenderNamespaceAsync(ns, _testOutputPath);

            var namespaceName = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var nsPath = Path.Combine(_testOutputPath, $"{namespaceName.Replace('.', GetService<ProjectContext>().FileNamingOptions.NamespaceSeparator)}.mdx");
            var content = await File.ReadAllTextAsync(nsPath, TestContext.CancellationTokenSource.Token);

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
        public async Task RenderTypeAsync_Should_Create_Type_File_With_Frontmatter()
        {
            var assembly = GetTestsDotSharedAssembly();
            var ns = assembly.Namespaces.FirstOrDefault(n => n.Types.Any());
            
            ns.Should().NotBeNull("Test assembly should contain a namespace with types");
            
            var type = ns!.Types.First();

            await GetMintlifyRenderer().RenderTypeAsync(type, ns, _testOutputPath);

            var safeNamespace = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var safeTypeName = type!.Symbol.Name
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_');
            var separator = GetService<ProjectContext>().FileNamingOptions.NamespaceSeparator;
            var fileName = $"{safeNamespace.Replace('.', separator)}.{safeTypeName}.mdx";
            var typePath = Path.Combine(_testOutputPath, fileName);
            File.Exists(typePath).Should().BeTrue();
            
            // Check frontmatter
            var content = await File.ReadAllTextAsync(typePath, TestContext.CancellationTokenSource.Token);
            content.Should().StartWith("---");
            content.Should().Contain("title:");
            content.Should().Contain("icon:");
        }

        [TestMethod]
        public async Task RenderTypeAsync_Should_Include_Type_Metadata()
        {
            var assembly = GetTestsDotSharedAssembly();
            var ns = assembly.Namespaces.FirstOrDefault(n => n.Types.Any());
            
            ns.Should().NotBeNull("Test assembly should contain a namespace with types");
            
            var type = ns!.Types.First();

            await GetMintlifyRenderer().RenderTypeAsync(type, ns, _testOutputPath);

            var safeNamespace = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var safeTypeName = type!.Symbol.Name
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_');
            var separator = GetService<ProjectContext>().FileNamingOptions.NamespaceSeparator;
            var fileName = $"{safeNamespace.Replace('.', separator)}.{safeTypeName}.mdx";
            var typePath = Path.Combine(_testOutputPath, fileName);
            var content = await File.ReadAllTextAsync(typePath, TestContext.CancellationTokenSource.Token);

            //content.Should().Contain($"# {type.Symbol.Name}");
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

            await GetMintlifyRenderer().RenderTypeAsync(type, ns, _testOutputPath);

            var safeNamespace = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var safeTypeName = type!.Symbol.Name
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_');
            var separator = GetService<ProjectContext>().FileNamingOptions.NamespaceSeparator;
            var fileName = $"{safeNamespace.Replace('.', separator)}.{safeTypeName}.mdx";
            var typePath = Path.Combine(_testOutputPath, fileName);
            var content = await File.ReadAllTextAsync(typePath, TestContext.CancellationTokenSource.Token);

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

            await GetMintlifyRenderer().RenderTypeAsync(type!, ns, _testOutputPath);

            var safeNamespace = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var safeTypeName = type!.Symbol.Name
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_');
            var separator = GetService<ProjectContext>().FileNamingOptions.NamespaceSeparator;
            var fileName = $"{safeNamespace.Replace('.', separator)}.{safeTypeName}.mdx";
            var typePath = Path.Combine(_testOutputPath, fileName);
            var content = await File.ReadAllTextAsync(typePath, TestContext.CancellationTokenSource.Token);

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

            GetMintlifyRenderer().RenderMember(sb, member!);

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

            GetMintlifyRenderer().RenderMember(sb, member!);

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

            GetMintlifyRenderer().RenderMember(sb, member!);

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

            GetMintlifyRenderer().RenderMember(sb, member!);

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

            GetMintlifyRenderer().RenderMember(sb, member!);

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

            GetMintlifyRenderer().RenderMember(sb, member!);

            var result = sb.ToString();
            if (!string.IsNullOrWhiteSpace(member!.Usage))
            {
                result.Should().Contain(member.Usage);
            }
        }

        #endregion

        #region DocsJson Generation Tests

        [TestMethod]
        public async Task RenderAsync_WithGenerateDocsJson_CreatesDocsJsonFile()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            
            var context = GetService<ProjectContext>();
            var renderer = GetMintlifyRenderer();
            
            // Act
            await renderer.RenderAsync(model);
            
            // Assert
            var docsJsonPath = Path.Combine(_testOutputPath, "docs.json");
            File.Exists(docsJsonPath).Should().BeTrue("docs.json should be created when GenerateDocsJson is true");
        }


        #endregion

    }

}