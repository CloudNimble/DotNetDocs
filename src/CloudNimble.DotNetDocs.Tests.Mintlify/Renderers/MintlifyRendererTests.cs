using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
using Mintlify.Core.Models;

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
                            ctx.ApiReferencePath = string.Empty;
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
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            var context = GetService<ProjectContext>();
            var renderer = GetMintlifyRenderer();

            // Get model to check namespaces (for assertion only)
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync(context);

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
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            var context = GetService<ProjectContext>();
            var renderer = GetMintlifyRenderer();

            // Get model to check namespaces (for assertion only)
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync(context);

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
            var context = new ProjectContext 
            { 
                DocumentationRootPath = nonExistentPath,
                ApiReferencePath = string.Empty
            };
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
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            var context = GetService<ProjectContext>();
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync(context);

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
                    var typePath = Path.Combine(_testOutputPath, typeFileName);
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
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert - Check that type files have appropriate icons
            var sampleClassFile = Directory.GetFiles(_testOutputPath, "*SampleClass.mdx").FirstOrDefault();
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
            var context = GetService<ProjectContext>();
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync(context);
            model.Usage = "This is assembly usage documentation";

            // Act
            await GetMintlifyRenderer().RenderAsync(model);

            // Assert
            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.mdx"), TestContext.CancellationTokenSource.Token);
            content.Should().Contain("## Usage");
            content.Should().Contain("This is assembly usage documentation");
        }

        [TestMethod]
        public async Task RenderAsync_IncludesAssemblyExamples_WhenProvided()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var context = GetService<ProjectContext>();
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync(context);
            model.Examples = "Example code here";

            // Act
            await GetMintlifyRenderer().RenderAsync(model);

            // Assert
            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.mdx"), TestContext.CancellationTokenSource.Token);
            content.Should().Contain("## Examples");
            content.Should().Contain("Example code here");
        }

        [TestMethod]
        public async Task RenderAsync_IncludesRelatedApis_WhenProvided()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var context = GetService<ProjectContext>();
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync(context);
            model.RelatedApis = new List<string> { "System.Object", "System.String" };

            // Act
            await GetMintlifyRenderer().RenderAsync(model);

            // Assert
            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.mdx"), TestContext.CancellationTokenSource.Token);
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
            var context = GetService<ProjectContext>();
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync(context);
            // Clear all optional documentation
            model.Usage = string.Empty;
            model.Examples = string.Empty;
            model.BestPractices = string.Empty;
            model.Patterns = string.Empty;
            model.Considerations = string.Empty;
            model.RelatedApis = new List<string>();

            // Act
            await GetMintlifyRenderer().RenderAsync(model);

            // Assert
            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.mdx"), TestContext.CancellationTokenSource.Token);
            content.Should().Contain("title: Overview");
            content.Should().Contain("## Namespaces");
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
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            var context = GetService<ProjectContext>();
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync(context);

            var ns = model.Namespaces.First(n => n.Types.Any(t => t.Symbol.Name == "ClassWithMethods"));
            var namespaceName = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var typeFileName = $"{namespaceName.Replace('.', '-')}.ClassWithMethods.mdx";
            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, typeFileName), TestContext.CancellationTokenSource.Token);

            content.Should().Contain("```csharp");
            content.Should().Contain("public");
        }

        #endregion

        #region FileNamingOptions Tests

        [TestMethod]
        public async Task RenderAsync_WithFileModeDefaultSeparator_CreatesFilesWithHyphen()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            var context = GetService<ProjectContext>();
            var originalFileNamingOptions = context.FileNamingOptions;
            var originalApiReferencePath = context.ApiReferencePath;

            context.FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '-');
            context.ApiReferencePath = string.Empty;

            try
            {
                // Act
                await documentationManager.ProcessAsync(assemblyPath, xmlPath);

                // Assert
                var files = Directory.GetFiles(_testOutputPath, "*.mdx");
                files.Should().Contain(f => Path.GetFileName(f).Contains("CloudNimble-DotNetDocs-Tests-Shared"));
            }
            finally
            {
                // Restore original settings
                context.FileNamingOptions = originalFileNamingOptions;
                context.ApiReferencePath = originalApiReferencePath;
            }
        }

        [TestMethod]
        public async Task RenderAsync_WithFileModeUnderscoreSeparator_CreatesFilesWithUnderscore()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            var context = GetService<ProjectContext>();
            var originalFileNamingOptions = context.FileNamingOptions;
            var originalApiReferencePath = context.ApiReferencePath;

            context.FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '_');
            context.ApiReferencePath = string.Empty;

            try
            {
                // Act
                await documentationManager.ProcessAsync(assemblyPath, xmlPath);

                // Assert
                var files = Directory.GetFiles(_testOutputPath, "*.mdx");
                files.Should().Contain(f => Path.GetFileName(f).Contains("CloudNimble_DotNetDocs_Tests_Shared"));
            }
            finally
            {
                // Restore original settings
                context.FileNamingOptions = originalFileNamingOptions;
                context.ApiReferencePath = originalApiReferencePath;
            }
        }

        [TestMethod]
        public async Task RenderAsync_WithFileModePeriodSeparator_CreatesFilesWithPeriod()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            var context = GetService<ProjectContext>();
            var originalFileNamingOptions = context.FileNamingOptions;
            var originalApiReferencePath = context.ApiReferencePath;

            context.FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '.');
            context.ApiReferencePath = string.Empty;

            try
            {
                // Act
                await documentationManager.ProcessAsync(assemblyPath, xmlPath);

                // Assert
                var files = Directory.GetFiles(_testOutputPath, "*.mdx");
                files.Should().Contain(f => Path.GetFileName(f).Contains("CloudNimble.DotNetDocs.Tests.Shared"));
            }
            finally
            {
                // Restore original settings
                context.FileNamingOptions = originalFileNamingOptions;
                context.ApiReferencePath = originalApiReferencePath;
            }
        }

        [TestMethod]
        public async Task RenderAsync_WithFolderMode_CreatesNamespaceFolderStructure()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            var context = GetService<ProjectContext>();
            var originalFileNamingOptions = context.FileNamingOptions;
            var originalApiReferencePath = context.ApiReferencePath;

            context.FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder);
            context.ApiReferencePath = string.Empty;

            try
            {
                // Act
                await documentationManager.ProcessAsync(assemblyPath, xmlPath);

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
            finally
            {
                // Restore original settings
                context.FileNamingOptions = originalFileNamingOptions;
                context.ApiReferencePath = originalApiReferencePath;
            }
        }

        [TestMethod]
        public async Task RenderAsync_WithFolderMode_IgnoresNamespaceSeparator()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            var context = GetService<ProjectContext>();
            var originalFileNamingOptions = context.FileNamingOptions;
            var originalApiReferencePath = context.ApiReferencePath;

            // Set a separator that should be ignored in Folder mode
            context.FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder, '_');
            context.ApiReferencePath = string.Empty;

            try
            {
                // Act
                await documentationManager.ProcessAsync(assemblyPath, xmlPath);

                // Assert
                // Verify folder structure uses path separators, not the configured separator
                var cloudNimbleDir = Path.Combine(_testOutputPath, "CloudNimble");
                Directory.Exists(cloudNimbleDir).Should().BeTrue();

                // Should NOT have a folder named "CloudNimble_DotNetDocs_Tests_Shared"
                var wrongDir = Path.Combine(_testOutputPath, "CloudNimble_DotNetDocs_Tests_Shared");
                Directory.Exists(wrongDir).Should().BeFalse();
            }
            finally
            {
                // Restore original settings
                context.FileNamingOptions = originalFileNamingOptions;
                context.ApiReferencePath = originalApiReferencePath;
            }
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
                var cloudNimbleDir = Path.Combine(testOutputPath, "CloudNimble");
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

            var indexPath = Path.Combine(_testOutputPath, "index.mdx");
            var content = await File.ReadAllTextAsync(indexPath, TestContext.CancellationTokenSource.Token);
            content.Should().Contain("title: Overview");
            content.Should().Contain("## Namespaces");
        }

        [TestMethod]
        public async Task RenderAssemblyAsync_Should_Include_Usage_When_Present()
        {
            var assembly = GetTestsDotSharedAssembly();
            assembly.Usage = "This is the assembly usage documentation.";

            await GetMintlifyRenderer().RenderAssemblyAsync(assembly, _testOutputPath);

            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.mdx"), TestContext.CancellationTokenSource.Token);
            content.Should().Contain("## Usage");
            content.Should().Contain(assembly.Usage);
        }

        [TestMethod]
        public async Task RenderAssemblyAsync_Should_List_Namespaces()
        {
            var assembly = GetTestsDotSharedAssembly();

            await GetMintlifyRenderer().RenderAssemblyAsync(assembly, _testOutputPath);

            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.mdx"), TestContext.CancellationTokenSource.Token);
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
                content.Should().Contain("### <Icon icon=\"file-brackets-curly\" iconType=\"duotone\" color=\"#0D9373\" size={24} style={{ paddingRight: '8px' }} /> Classes");
            }
            if (ns.Types.Any(t => t.Symbol.TypeKind == TypeKind.Interface))
            {
                content.Should().Contain("### <Icon icon=\"plug\" iconType=\"duotone\" color=\"#0D9373\" size={24} style={{ paddingRight: '8px' }} /> Interfaces\r\n");
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
            result.Should().Contain($$$"""### <Icon icon="function" iconType="duotone" color="#0D9373" size={24} style={{ paddingRight: '8px' }} />  {{{member!.Symbol.Name}}}""");
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

        #region Enum Rendering Tests

        [TestMethod]
        public void GetIconForType_DocEnum_Should_Return_ListOl()
        {
            // Arrange - use an actual enum from the test assembly
            var assembly = GetTestsDotSharedAssembly();
            var enumNamespace = assembly.Namespaces.FirstOrDefault(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.Enums");
            enumNamespace.Should().NotBeNull("Enums namespace should exist in test assembly");

            var simpleEnum = enumNamespace!.Types.FirstOrDefault(t => t.Name == "SimpleEnum");
            simpleEnum.Should().NotBeNull("SimpleEnum should exist in test assembly");
            simpleEnum.Should().BeOfType<DocEnum>("SimpleEnum should be DocEnum type when loaded from metadata");

            // Act
            var icon = MintlifyIcons.GetIconForType(simpleEnum!);

            // Assert
            icon.Should().Be("list-ol", "DocEnum should have list-ol icon");
            icon.Should().NotBe("lock", "DocEnum should not have lock icon");
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

        #region Additional MDX Content Discovery Tests

        [TestMethod]
        public async Task RenderAsync_WithAdditionalMdxFiles_IncludesThemInNavigation()
        {
            // Arrange - Create additional MDX files in the documentation root
            var testMdxFile = Path.Combine(_testOutputPath, "getting-started.mdx");
            await File.WriteAllTextAsync(testMdxFile, "---\ntitle: Getting Started\n---\n# Getting Started\n\nThis is additional content.", TestContext.CancellationTokenSource.Token);

            var subDir = Path.Combine(_testOutputPath, "guides");
            Directory.CreateDirectory(subDir);
            var guideMdxFile = Path.Combine(subDir, "tutorial.mdx");
            await File.WriteAllTextAsync(guideMdxFile, "---\ntitle: Tutorial\n---\n# Tutorial\n\nThis is a tutorial.", TestContext.CancellationTokenSource.Token);

            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            
            var renderer = GetMintlifyRenderer();
            
            // Act
            await renderer.RenderAsync(model);
            
            // Assert
            var docsJsonPath = Path.Combine(_testOutputPath, "docs.json");
            File.Exists(docsJsonPath).Should().BeTrue("docs.json should be created");
            
            var docsJsonContent = await File.ReadAllTextAsync(docsJsonPath, TestContext.CancellationTokenSource.Token);
            docsJsonContent.Should().Contain("getting-started", "Additional MDX file should be included in navigation");
            docsJsonContent.Should().Contain("guides", "Subdirectory with MDX files should be included in navigation");
            docsJsonContent.Should().Contain("tutorial", "Tutorial file should be included in navigation");
        }

        [TestMethod]
        public async Task RenderAsync_WithExistingIndexMdx_DoesNotDuplicateIndex()
        {
            // Arrange - Create an existing index.mdx file in the root
            var existingIndexFile = Path.Combine(_testOutputPath, "index.mdx");
            await File.WriteAllTextAsync(existingIndexFile, "---\ntitle: Custom Index\n---\n# Custom Index\n\nThis is a custom index page.", TestContext.CancellationTokenSource.Token);

            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            
            var renderer = GetMintlifyRenderer();
            
            // Act
            await renderer.RenderAsync(model);
            
            // Assert
            var docsJsonPath = Path.Combine(_testOutputPath, "docs.json");
            File.Exists(docsJsonPath).Should().BeTrue("docs.json should be created");
            
            var docsJsonContent = await File.ReadAllTextAsync(docsJsonPath, TestContext.CancellationTokenSource.Token);

            // Count occurrences of "index" in the navigation
            var indexCount = docsJsonContent.Split("\"index\"").Length - 1;
            indexCount.Should().Be(1, "Index should appear once in the navigation when existing index.mdx file is present");
        }

        [TestMethod]
        public async Task RenderAsync_WithApiReferenceFolder_ExcludesFromDiscovery()
        {
            // Arrange - Create an api-reference folder with MDX files that should be excluded
            var apiReferenceDir = Path.Combine(_testOutputPath, "api-reference");
            Directory.CreateDirectory(apiReferenceDir);
            var apiFile = Path.Combine(apiReferenceDir, "some-api.mdx");
            await File.WriteAllTextAsync(apiFile, "---\ntitle: Some API\n---\n# Some API\n\nThis should be excluded.", TestContext.CancellationTokenSource.Token);

            // Also add a legitimate additional file
            var guidesDir = Path.Combine(_testOutputPath, "guides");
            Directory.CreateDirectory(guidesDir);
            var guideFile = Path.Combine(guidesDir, "tutorial.mdx");
            await File.WriteAllTextAsync(guideFile, "---\ntitle: Tutorial\n---\n# Tutorial\n\nThis should be included.", TestContext.CancellationTokenSource.Token);

            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            
            var renderer = GetMintlifyRenderer();
            
            // Act
            await renderer.RenderAsync(model);
            
            // Assert
            var docsJsonPath = Path.Combine(_testOutputPath, "docs.json");
            File.Exists(docsJsonPath).Should().BeTrue("docs.json should be created");
            
            var docsJsonContent = await File.ReadAllTextAsync(docsJsonPath, TestContext.CancellationTokenSource.Token);
            
            // Should contain the guides content but NOT the api-reference content from discovery
            docsJsonContent.Should().Contain("guides", "Guides folder should be discovered");
            docsJsonContent.Should().Contain("tutorial", "Tutorial file should be discovered");
            
            // Should NOT contain duplicate api-reference content from discovery
            // (The generated API Reference should only appear once with the "code" icon)
            var apiReferenceCount = System.Text.RegularExpressions.Regex.Matches(docsJsonContent, "\"API Reference\"").Count;
            apiReferenceCount.Should().Be(1, "API Reference should only appear once (from generation, not discovery)");
            
            // Verify the single API Reference group has the code icon (meaning it's the generated one)
            docsJsonContent.Should().Contain("\"group\": \"API Reference\"", "API Reference group should exist");
            docsJsonContent.Should().Contain("\"icon\": \"code\"", "Generated API Reference should have code icon");
        }

        [TestMethod]
        public async Task RenderAsync_WithEmptyDocumentationRoot_WorksAsBeforeWithOnlyGeneratedIndex()
        {
            // Arrange - No additional MDX files, just the standard test scenario
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            
            var renderer = GetMintlifyRenderer();
            
            // Act
            await renderer.RenderAsync(model);
            
            // Assert
            var docsJsonPath = Path.Combine(_testOutputPath, "docs.json");
            File.Exists(docsJsonPath).Should().BeTrue("docs.json should be created");
            
            var docsJsonContent = await File.ReadAllTextAsync(docsJsonPath, TestContext.CancellationTokenSource.Token);
            
            // Should contain the generated index and API reference content, but no additional content
            docsJsonContent.Should().Contain("\"index\"", "Generated index should be present");
            docsJsonContent.Should().Contain("API Reference", "API Reference group should be present");
            
            // Should not contain any additional navigation items
            docsJsonContent.Should().NotContain("getting-started", "No additional content should be discovered");
            docsJsonContent.Should().NotContain("guides", "No additional content should be discovered");
        }

        #endregion

        #region Baseline Generation

        /// <summary>
        /// Generates baseline files for the MintlifyRenderer in both FileMode and FolderMode.
        /// This method is marked with [BreakdanceManifestGenerator] and is called by the Breakdance tool
        /// to generate baseline files for comparison in unit tests.
        /// </summary>
        /// <param name="projectPath">The root path of the test project where baselines will be stored.</param>
        /// <remarks>
        /// This method uses Dependency Injection to get the ProjectContext and MintlifyRenderer instances.
        /// It modifies the context properties to configure the output location and file naming options,
        /// then uses the renderer from DI (which ensures all dependencies are properly injected,
        /// including MintlifyRendererOptions and DocsJsonManager).
        ///
        /// The baseline generation intentionally does NOT use DocumentationManager.ProcessAsync because
        /// these are unit test baselines for the renderer itself, not integration test baselines.
        /// The renderer should be tested in isolation without transformers applied.
        /// </remarks>
        //[TestMethod]
        //[DataRow(projectPath)]
        [BreakdanceManifestGenerator]
        public async Task GenerateMintlifyBaselines(string projectPath)
        {
            await GenerateFileModeBaselines(projectPath);
            await GenerateFolderModeBaselines(projectPath);
        }

        /// <summary>
        /// Generates baseline files for MintlifyRenderer in FileMode configuration.
        /// </summary>
        /// <param name="projectPath">The root path of the test project where baselines will be stored.</param>
        /// <remarks>
        /// This method:
        /// 1. Gets the ProjectContext from DI to ensure consistent configuration
        /// 2. Modifies the context properties for FileMode output
        /// 3. Gets the MintlifyRenderer from DI with all dependencies properly injected
        /// 4. Uses AssemblyManager directly (not DocumentationManager) to generate documentation
        ///    without transformers, as these are unit test baselines for the renderer alone
        /// 5. Does not restore context values since this runs in an isolated Breakdance process
        /// </remarks>
        private async Task GenerateFileModeBaselines(string projectPath)
        {
            var baselinesDir = Path.Combine(projectPath, "Baselines", "MintlifyRenderer", "FileMode");
            if (Directory.Exists(baselinesDir))
            {
                Directory.Delete(baselinesDir, true);
            }
            Directory.CreateDirectory(baselinesDir);

            var context = GetService<ProjectContext>();
            context.FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '-');
            context.DocumentationRootPath = baselinesDir;

            var renderer = GetMintlifyRenderer();

            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync(context);

            await renderer.RenderAsync(assembly);
        }

        /// <summary>
        /// Generates baseline files for MintlifyRenderer in FolderMode configuration.
        /// </summary>
        /// <param name="projectPath">The root path of the test project where baselines will be stored.</param>
        /// <remarks>
        /// This method:
        /// 1. Gets the ProjectContext from DI to ensure consistent configuration
        /// 2. Modifies the context properties for FolderMode output
        /// 3. Gets the MintlifyRenderer from DI with all dependencies properly injected
        /// 4. Uses AssemblyManager directly (not DocumentationManager) to generate documentation
        ///    without transformers, as these are unit test baselines for the renderer alone
        /// 5. Does not restore context values since this runs in an isolated Breakdance process
        /// </remarks>
        private async Task GenerateFolderModeBaselines(string projectPath)
        {
            var baselinesDir = Path.Combine(projectPath, "Baselines", "MintlifyRenderer", "FolderMode");
            if (Directory.Exists(baselinesDir))
            {
                Directory.Delete(baselinesDir, true);
            }
            Directory.CreateDirectory(baselinesDir);

            // Get context from DI and modify it for baseline generation
            var context = GetService<ProjectContext>();
            context.FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder, '-');
            context.DocumentationRootPath = baselinesDir;

            // Get renderer from DI to ensure all dependencies are properly injected
            var renderer = GetMintlifyRenderer();

            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync(context);

            await renderer.RenderAsync(assembly);
        }

        //private static void CopyDirectoryRecursive(string sourceDir, string destDir)
        //{
        //    // Copy all files from the source directory
        //    foreach (var file in Directory.GetFiles(sourceDir))
        //    {
        //        var destFile = Path.Combine(destDir, Path.GetFileName(file));
        //        File.Copy(file, destFile, true);
        //    }

        //    // Recursively copy subdirectories
        //    foreach (var subDir in Directory.GetDirectories(sourceDir))
        //    {
        //        var dirName = Path.GetFileName(subDir);
        //        var destSubDir = Path.Combine(destDir, dirName);
        //        Directory.CreateDirectory(destSubDir);
        //        CopyDirectoryRecursive(subDir, destSubDir);
        //    }
        //}

        #endregion

        #region DocsJsonManager Template Loading Tests

        [TestMethod]
        public void EndToEnd_TemplateLoading_PreservesIconsAndStructure()
        {
            // Arrange - Create a manually constructed template config that simulates what the XML parsing would create
            // This represents the structure that would come from parsing the .docsproj MintlifyTemplate XML
            var templateConfig = new DocsJsonConfig
            {
                Name = "Test Template",
                Theme = "maple",
                Colors = new ColorsConfig { Primary = "#0D9373" },
                Navigation = new NavigationConfig
                {
                    Pages = new List<object>
                    {
                        new GroupConfig
                        {
                            Group = "Getting Started",
                            Icon = "stars",
                            Pages = new List<object> { "index", "quickstart" }
                        },
                        new GroupConfig
                        {
                            Group = "Guides",
                            Icon = "dog-leashed",
                            Pages = new List<object> { "guides/index", "guides/conceptual-docs" }
                        },
                        new GroupConfig
                        {
                            Group = "Providers",
                            Icon = "books",
                            Pages = new List<object>
                            {
                                "providers/index",
                                new GroupConfig
                                {
                                    Group = "Mintlify",
                                    Icon = "/mintlify.svg",
                                    Tag = "PARTNER",
                                    Pages = new List<object> { "providers/mintlify/index" }
                                }
                            }
                        },
                        new GroupConfig
                        {
                            Group = "Plugins",
                            Icon = "outlet",
                            Pages = new List<object> { "plugins/index" }
                        },
                        new GroupConfig
                        {
                            Group = "Learnings",
                            Icon = "", // Empty icon should be preserved
                            Pages = new List<object> { "learnings/bridge-assemblies" }
                        }
                    }
                }
            };

            // Act - Test the complete cycle that happens in MintlifyRenderer.cs lines 89-95
            // 1. Template is available (_options.Template is not null)
            // 2. docsConfig = _options.Template ?? DocsJsonManager.CreateDefault(...)
            // 3. var json = JsonSerializer.Serialize(docsConfig, MintlifyConstants.JsonSerializerOptions);
            // 4. _docsJsonManager.Load(json);

            var json = JsonSerializer.Serialize(templateConfig, MintlifyConstants.JsonSerializerOptions);
            var docsJsonManager = new DocsJsonManager();
            docsJsonManager.Load(json);
            var loadedConfig = docsJsonManager.Configuration;

            // Assert - Verify complete preservation through the serialization/loading cycle
            docsJsonManager.ConfigurationErrors.Should().BeEmpty($"Expected no configuration errors, but got: {string.Join(", ", docsJsonManager.ConfigurationErrors.Select(e => e.ErrorText))}");
            loadedConfig.Should().NotBeNull("Configuration should load successfully");
            loadedConfig!.Navigation.Should().NotBeNull();
            loadedConfig.Navigation!.Pages.Should().HaveCount(5, "Should have all 5 groups from the template");

            // Verify "Getting Started" group icon is preserved through the complete cycle
            var gettingStarted = loadedConfig.Navigation!.Pages![0] as GroupConfig;
            gettingStarted.Should().NotBeNull();
            gettingStarted!.Group.Should().Be("Getting Started");
            gettingStarted.Icon.Should().NotBeNull("Getting Started should preserve stars icon");
            gettingStarted.Icon!.Name.Should().Be("stars");

            // Verify "Guides" group icon is preserved
            var guides = loadedConfig.Navigation!.Pages![1] as GroupConfig;
            guides.Should().NotBeNull();
            guides!.Group.Should().Be("Guides");
            guides.Icon.Should().NotBeNull("Guides should preserve dog-leashed icon");
            guides.Icon!.Name.Should().Be("dog-leashed");

            // Verify "Providers" group and nested "Mintlify" group icons are preserved
            var providers = loadedConfig.Navigation!.Pages![2] as GroupConfig;
            providers.Should().NotBeNull();
            providers!.Group.Should().Be("Providers");
            providers.Icon.Should().NotBeNull("Providers should preserve books icon");
            providers.Icon!.Name.Should().Be("books");
            providers.Pages.Should().HaveCount(2, "Providers should have providers/index page plus nested Mintlify group");

            var mintlify = providers!.Pages![1] as GroupConfig;
            mintlify.Should().NotBeNull();
            mintlify!.Group.Should().Be("Mintlify");
            mintlify.Icon.Should().NotBeNull("Mintlify should preserve /mintlify.svg icon");
            mintlify.Icon!.Name.Should().Be("/mintlify.svg");
            mintlify.Tag.Should().Be("PARTNER");

            // Verify "Plugins" group icon is preserved
            var plugins = loadedConfig.Navigation!.Pages![3] as GroupConfig;
            plugins.Should().NotBeNull();
            plugins!.Group.Should().Be("Plugins");
            plugins.Icon.Should().NotBeNull("Plugins should preserve outlet icon");
            plugins.Icon!.Name.Should().Be("outlet");

            // Verify "Learnings" group with empty icon is preserved (critical edge case)
            var learnings = loadedConfig.Navigation!.Pages![4] as GroupConfig;
            learnings.Should().NotBeNull();
            learnings!.Group.Should().Be("Learnings");
            learnings.Icon.Should().NotBeNull("Learnings should preserve empty icon");
            learnings.Icon!.Name.Should().Be("", "Empty icon should be preserved as empty string");

            // Verify order is preserved through the entire serialization/loading cycle
            var groups = loadedConfig.Navigation!.Pages!.OfType<GroupConfig>().ToList();
            groups[0].Group.Should().Be("Getting Started");
            groups[1].Group.Should().Be("Guides");
            groups[2].Group.Should().Be("Providers");
            groups[3].Group.Should().Be("Plugins");
            groups[4].Group.Should().Be("Learnings");
        }

        #endregion

    }

}