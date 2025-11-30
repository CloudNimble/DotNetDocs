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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core.Renderers
{

    /// <summary>
    /// Tests for the MarkdownRenderer class.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarkdownRendererTests : DotNetDocsTestBase
    {

        #region Fields

        private string _testOutputPath = null!;

        #endregion

        #region Helper Methods

        private MarkdownRenderer GetMarkdownRenderer()
        {
            var renderer = GetServices<IDocRenderer>()
                .OfType<MarkdownRenderer>()
                .FirstOrDefault();
            renderer.Should().NotBeNull("MarkdownRenderer should be registered in DI");
            return renderer!;
        }

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            _testOutputPath = Path.Combine(Path.GetTempPath(), $"MDRendererTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testOutputPath);

            // Configure services for DI
            TestHostBuilder.ConfigureServices((context, services) =>
            {
                services.AddDotNetDocsPipeline(pipeline =>
                {
                    pipeline.UseMarkdownRenderer()
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
        public async Task RenderAsync_ProducesConsistentBaseline()
        {
            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            var baselinePath = Path.Combine(projectPath, "Baselines", "MarkdownRenderer", "FileMode", "index.md");
            
            if (File.Exists(baselinePath))
            {
                var baseline = await File.ReadAllTextAsync(baselinePath, TestContext.CancellationToken);
                var actual = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.md"), TestContext.CancellationToken);
                
                // Normalize line endings for cross-platform compatibility
                var normalizedActual = actual.ReplaceLineEndings(Environment.NewLine);
                var normalizedBaseline = baseline.ReplaceLineEndings(Environment.NewLine);
                
                normalizedActual.Should().Be(normalizedBaseline,
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
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            var context = GetService<ProjectContext>();

            // Get the model separately to verify namespaces
            var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync(context);

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
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            var context = GetService<ProjectContext>();

            // Get the model separately to verify types
            var manager = new AssemblyManager(assemblyPath, xmlPath);
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
                    var typeFileName = $"{namespaceName.Replace('.', context.FileNamingOptions.NamespaceSeparator)}.{safeTypeName}.md";
                    var typePath = Path.Combine(_testOutputPath, typeFileName);
                    File.Exists(typePath).Should().BeTrue($"Type file {typeFileName} should exist");
                }
            }
        }

        [TestMethod]
        public async Task RenderAsync_WithNullModel_ThrowsArgumentNullException()
        {
            var renderer = GetMarkdownRenderer();

            Func<Task> act = async () => await renderer.RenderAsync(null!);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task RenderAsync_CreatesOutputDirectory_WhenNotExists()
        {
            var nonExistentPath = Path.Combine(Path.GetTempPath(), $"NewDir_{Guid.NewGuid()}");
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            // Use the test's DI container and update the context
            var context = GetService<ProjectContext>();
            var originalPath = context.DocumentationRootPath;
            context.DocumentationRootPath = nonExistentPath;
            context.ApiReferencePath = string.Empty;

            var documentationManager = GetService<DocumentationManager>();

            try
            {
                Directory.Exists(nonExistentPath).Should().BeFalse();
                await documentationManager.ProcessAsync(assemblyPath, xmlPath);

                Directory.Exists(nonExistentPath).Should().BeTrue();
            }
            finally
            {
                // Restore original path
                context.DocumentationRootPath = originalPath;

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
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            // Get the model and modify it
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            model.Usage = "This is assembly usage documentation";

            var renderer = GetMarkdownRenderer();
            await renderer.RenderAsync(model);

            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.md"), TestContext.CancellationToken);
            content.Should().Contain("## Usage");
            content.Should().Contain("This is assembly usage documentation");
        }

        [TestMethod]
        public async Task RenderAsync_IncludesAssemblyExamples_WhenProvided()
        {
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            // Get the model and modify it
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            model.Examples = "Example code here";

            var renderer = GetMarkdownRenderer();
            await renderer.RenderAsync(model);

            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.md"), TestContext.CancellationToken);
            content.Should().Contain("## Examples");
            content.Should().Contain("Example code here");
        }

        [TestMethod]
        public async Task RenderAsync_IncludesRelatedApis_WhenProvided()
        {
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            // Get the model and modify it
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            model.RelatedApis = ["System.Object", "System.String"];

            var renderer = GetMarkdownRenderer();
            await renderer.RenderAsync(model);

            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.md"), TestContext.CancellationToken);
            content.Should().Contain("## Related APIs");
            content.Should().Contain("- System.Object");
            content.Should().Contain("- System.String");
        }

        [TestMethod]
        public async Task RenderAsync_HandlesEmptyDocumentation_Gracefully()
        {
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            // Get the model and clear documentation
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();

            // Clear all optional documentation
            model.Usage = string.Empty;
            model.Examples = string.Empty;
            model.BestPractices = string.Empty;
            model.Patterns = string.Empty;
            model.Considerations = string.Empty;
            model.RelatedApis = [];

            var renderer = GetMarkdownRenderer();
            await renderer.RenderAsync(model);

            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.md"), TestContext.CancellationToken);
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
            var assemblyPath = typeof(ClassWithMethods).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            var context = GetService<ProjectContext>();

            // Get the model separately to find the correct file
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();

            var ns = model.Namespaces.First(n => n.Types.Any(t => t.Symbol.Name == "ClassWithMethods"));
            var namespaceName = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var typeFileName = $"{namespaceName.Replace('.', context.FileNamingOptions.NamespaceSeparator)}.ClassWithMethods.md";
            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, typeFileName), TestContext.CancellationToken);

            content.Should().Contain("```csharp");
            content.Should().Contain("public");
        }

        #endregion

        #region FileNamingOptions Tests

        [TestMethod]
        public async Task RenderAsync_WithFileModeDefaultSeparator_CreatesFilesWithHyphen()
        {
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            // Use the test's DI container and update the context
            var context = GetService<ProjectContext>();
            var originalFileNamingOptions = context.FileNamingOptions;
            var originalApiReferencePath = context.ApiReferencePath;

            context.FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '-');
            context.DocumentationRootPath = _testOutputPath;
            context.ApiReferencePath = string.Empty;

            var documentationManager = GetService<DocumentationManager>();

            try
            {
                await documentationManager.ProcessAsync(assemblyPath, xmlPath);

                var files = Directory.GetFiles(_testOutputPath, "*.md");
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
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            // Use the test's DI container and update the context
            var context = GetService<ProjectContext>();
            var originalFileNamingOptions = context.FileNamingOptions;
            var originalApiReferencePath = context.ApiReferencePath;

            context.FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '_');
            context.DocumentationRootPath = _testOutputPath;
            context.ApiReferencePath = string.Empty;

            var documentationManager = GetService<DocumentationManager>();

            try
            {
                await documentationManager.ProcessAsync(assemblyPath, xmlPath);

                var files = Directory.GetFiles(_testOutputPath, "*.md");
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
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            // Use the test's DI container and update the context
            var context = GetService<ProjectContext>();
            var originalFileNamingOptions = context.FileNamingOptions;
            var originalApiReferencePath = context.ApiReferencePath;

            context.FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '.');
            context.DocumentationRootPath = _testOutputPath;
            context.ApiReferencePath = string.Empty;

            var documentationManager = GetService<DocumentationManager>();

            try
            {
                await documentationManager.ProcessAsync(assemblyPath, xmlPath);

                var files = Directory.GetFiles(_testOutputPath, "*.md");
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
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            // Use the test's DI container and update the context
            var context = GetService<ProjectContext>();
            var originalFileNamingOptions = context.FileNamingOptions;
            var originalApiReferencePath = context.ApiReferencePath;

            context.FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder);
            context.DocumentationRootPath = _testOutputPath;
            context.ApiReferencePath = string.Empty;

            var documentationManager = GetService<DocumentationManager>();

            try
            {
                await documentationManager.ProcessAsync(assemblyPath, xmlPath);

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
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            // Use the test's DI container and update the context
            var context = GetService<ProjectContext>();
            var originalFileNamingOptions = context.FileNamingOptions;
            var originalApiReferencePath = context.ApiReferencePath;

            context.FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder, '_');
            context.DocumentationRootPath = _testOutputPath;
            context.ApiReferencePath = string.Empty;

            var documentationManager = GetService<DocumentationManager>();

            try
            {
                await documentationManager.ProcessAsync(assemblyPath, xmlPath);

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
            var testOutputPath = Path.Combine(Path.GetTempPath(), $"MDFolderBaseline_{Guid.NewGuid()}");

            // Use the test's DI container and update the context
            var context = GetService<ProjectContext>();
            var originalFileNamingOptions = context.FileNamingOptions;
            var originalPath = context.DocumentationRootPath;
            var originalApiReferencePath = context.ApiReferencePath;

            context.FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder);
            context.DocumentationRootPath = testOutputPath;
            context.ApiReferencePath = string.Empty;

            var renderer = GetMarkdownRenderer();

            try
            {
                // Process the assembly through DocumentationManager to apply transformations
                var assemblyPath = typeof(SampleClass).Assembly.Location;
                var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

                var manager = new AssemblyManager(assemblyPath, xmlPath);
                var assembly = await manager.DocumentAsync(context);

                await renderer.RenderAsync(assembly); 

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
                // Restore original settings
                context.FileNamingOptions = originalFileNamingOptions;
                context.DocumentationRootPath = originalPath;
                context.ApiReferencePath = originalApiReferencePath;

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
                framework,
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
            
            // Compare with baseline - normalize line endings for cross-platform compatibility
            var baselineContent = await File.ReadAllTextAsync(baselinePath);

            // Normalize both to Environment.NewLine to handle any line ending differences
            var normalizedActual = actualContent.ReplaceLineEndings(Environment.NewLine);
            var normalizedBaseline = baselineContent.ReplaceLineEndings(Environment.NewLine);

            normalizedActual.Should().Be(normalizedBaseline,
                $"Output should match baseline at {baselinePath}");
        }

        #endregion

        #region RenderAssemblyAsync Tests

        [TestMethod]
        public async Task RenderAssemblyAsync_Should_Create_Index_File()
        {
            var assembly = GetTestsDotSharedAssembly();
            var renderer = GetMarkdownRenderer();

            await renderer.RenderAssemblyAsync(assembly, _testOutputPath);

            var indexPath = Path.Combine(_testOutputPath, "index.md");
            File.Exists(indexPath).Should().BeTrue();
        }

        [TestMethod]
        public async Task RenderAssemblyAsync_Should_Include_Assembly_Name()
        {
            var assembly = GetTestsDotSharedAssembly();
            var renderer = GetMarkdownRenderer();

            await renderer.RenderAssemblyAsync(assembly, _testOutputPath);

            var indexPath = Path.Combine(_testOutputPath, "index.md");
            var content = await File.ReadAllTextAsync(indexPath, TestContext.CancellationToken);
            content.Should().Contain($"# {assembly.AssemblyName}");
        }

        [TestMethod]
        public async Task RenderAssemblyAsync_Should_Include_Usage_When_Present()
        {
            var assembly = GetTestsDotSharedAssembly();
            assembly.Usage = "This is the assembly usage documentation.";
            var renderer = GetMarkdownRenderer();

            await renderer.RenderAssemblyAsync(assembly, _testOutputPath);

            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.md"), TestContext.CancellationToken);
            content.Should().Contain("## Usage");
            content.Should().Contain(assembly.Usage);
        }

        [TestMethod]
        public async Task RenderAssemblyAsync_Should_List_Namespaces()
        {
            var assembly = GetTestsDotSharedAssembly();
            var renderer = GetMarkdownRenderer();

            await renderer.RenderAssemblyAsync(assembly, _testOutputPath);

            var content = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "index.md"), TestContext.CancellationToken);
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
            var renderer = GetMarkdownRenderer();
            var context = GetService<ProjectContext>();

            await renderer.RenderNamespaceAsync(ns, _testOutputPath);

            var namespaceName = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var nsPath = Path.Combine(_testOutputPath, $"{namespaceName.Replace('.', context.FileNamingOptions.NamespaceSeparator)}.md");
            File.Exists(nsPath).Should().BeTrue();
        }

        [TestMethod]
        public async Task RenderNamespaceAsync_Should_List_Types_By_Category()
        {
            var assembly = GetTestsDotSharedAssembly();

            assembly.Namespaces.Should().NotBeEmpty("Test assembly should contain namespaces");

            var ns = assembly.Namespaces.First();
            var renderer = GetMarkdownRenderer();
            var context = GetService<ProjectContext>();

            await renderer.RenderNamespaceAsync(ns, _testOutputPath);

            var namespaceName = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var nsPath = Path.Combine(_testOutputPath, $"{namespaceName.Replace('.', context.FileNamingOptions.NamespaceSeparator)}.md");
            var content = await File.ReadAllTextAsync(nsPath, TestContext.CancellationToken);

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
            var renderer = GetMarkdownRenderer();
            var context = GetService<ProjectContext>();

            await renderer.RenderTypeAsync(type, ns, _testOutputPath);

            var safeNamespace = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var safeTypeName = type!.Symbol.Name
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_');
            var separator = context.FileNamingOptions.NamespaceSeparator;
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
            var renderer = GetMarkdownRenderer();
            var context = GetService<ProjectContext>();

            await renderer.RenderTypeAsync(type, ns, _testOutputPath);

            var safeNamespace = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var safeTypeName = type!.Symbol.Name
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_');
            var separator = context.FileNamingOptions.NamespaceSeparator;
            var fileName = $"{safeNamespace.Replace('.', separator)}.{safeTypeName}.md";
            var typePath = Path.Combine(_testOutputPath, fileName);
            var content = await File.ReadAllTextAsync(typePath, TestContext.CancellationToken);

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
            var renderer = GetMarkdownRenderer();
            var context = GetService<ProjectContext>();

            await renderer.RenderTypeAsync(type, ns, _testOutputPath);

            var safeNamespace = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var safeTypeName = type!.Symbol.Name
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_');
            var separator = context.FileNamingOptions.NamespaceSeparator;
            var fileName = $"{safeNamespace.Replace('.', separator)}.{safeTypeName}.md";
            var typePath = Path.Combine(_testOutputPath, fileName);
            var content = await File.ReadAllTextAsync(typePath, TestContext.CancellationToken);

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
            var renderer = GetMarkdownRenderer();
            var context = GetService<ProjectContext>();

            await renderer.RenderTypeAsync(type!, ns, _testOutputPath);

            var safeNamespace = string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
            var safeTypeName = type!.Symbol.Name
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_');
            var separator = context.FileNamingOptions.NamespaceSeparator;
            var fileName = $"{safeNamespace.Replace('.', separator)}.{safeTypeName}.md";
            var typePath = Path.Combine(_testOutputPath, fileName);
            var content = await File.ReadAllTextAsync(typePath, TestContext.CancellationToken);

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
            var renderer = GetMarkdownRenderer();

            renderer.RenderMember(sb, member!);

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
            var renderer = GetMarkdownRenderer();

            renderer.RenderMember(sb, member!);

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
            var renderer = GetMarkdownRenderer();

            renderer.RenderMember(sb, member!);

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
            var renderer = GetMarkdownRenderer();

            renderer.RenderMember(sb, member!);

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
            var renderer = GetMarkdownRenderer();

            renderer.RenderMember(sb, member!);

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
            var renderer = GetMarkdownRenderer();

            renderer.RenderMember(sb, member!);

            var result = sb.ToString();
            if (!string.IsNullOrWhiteSpace(member!.Usage))
            {
                result.Should().Contain(member.Usage);
            }
        }

        #endregion

        #region Enum Support Tests

        /// <summary>
        /// Generates baseline files for the MarkdownRenderer in both FileMode and FolderMode.
        /// This method is marked with [BreakdanceManifestGenerator] and is called by the Breakdance tool
        /// to generate baseline files for comparison in unit tests.
        /// </summary>
        /// <param name="projectPath">The root path of the test project where baselines will be stored.</param>
        /// <remarks>
        /// This method uses Dependency Injection to get the ProjectContext and MarkdownRenderer instances.
        /// It modifies the context properties to configure the output location and file naming options,
        /// then uses the renderer from DI (which ensures all dependencies are properly injected).
        ///
        /// The baseline generation intentionally does NOT use DocumentationManager.ProcessAsync because
        /// these are unit test baselines for the renderer itself, not integration test baselines.
        /// The renderer should be tested in isolation without transformers applied.
        /// </remarks>
        [TestMethod]
        public async Task RenderTypeAsync_Should_Include_Enum_Values()
        {
            var assembly = GetTestsDotSharedAssembly();

            // Find the SimpleEnum type
            var enumType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Name == "SimpleEnum");

            enumType.Should().NotBeNull("SimpleEnum should exist in test assembly");
            enumType.Should().BeOfType<DocEnum>("SimpleEnum should be a DocEnum instance");

            var docEnum = enumType as DocEnum;
            docEnum!.Values.Should().NotBeEmpty("Enum should have values");
            docEnum.Values.Should().HaveCount(5, "SimpleEnum should have 5 values");

            // Verify enum values
            var noneValue = docEnum.Values.FirstOrDefault(v => v.Name == "None");
            noneValue.Should().NotBeNull("None value should exist");
            noneValue!.NumericValue.Should().Be("0");

            var thirdValue = docEnum.Values.FirstOrDefault(v => v.Name == "Third");
            thirdValue.Should().NotBeNull("Third value should exist");
            thirdValue!.NumericValue.Should().Be("10");

            // Render the enum type
            var renderer = GetMarkdownRenderer();
            var enumNamespace = assembly.Namespaces.First(n => n.Types.Contains(enumType!));
            await renderer.RenderTypeAsync(enumType!, enumNamespace, _testOutputPath);

            // Debug: List all files created
            var createdFiles = Directory.GetFiles(_testOutputPath, "*.md");
            TestContext.WriteLine($"Created files in {_testOutputPath}:");
            foreach (var file in createdFiles)
            {
                TestContext.WriteLine($"  - {Path.GetFileName(file)}");
            }

            var outputFile = Path.Combine(_testOutputPath, "CloudNimble-DotNetDocs-Tests-Shared-Enums.SimpleEnum.md");
            File.Exists(outputFile).Should().BeTrue("Output file should exist");

            var content = await File.ReadAllTextAsync(outputFile, TestContext.CancellationToken);

            // Check that enum values section is present
            content.Should().Contain("## Values", "Should have Values section");
            content.Should().Contain("| Name | Value | Description |", "Should have enum values table header");
            content.Should().Contain("| `None` | 0 |", "Should include None value");
            content.Should().Contain("| `First` | 1 |", "Should include First value");
            content.Should().Contain("| `Second` | 2 |", "Should include Second value");
            content.Should().Contain("| `Third` | 10 |", "Should include Third value");
            content.Should().Contain("| `Fourth` | 11 |", "Should include Fourth value");
        }

        [TestMethod]
        public async Task RenderTypeAsync_Should_Include_Flags_Attribute()
        {
            var assembly = GetTestsDotSharedAssembly();

            // Find the FlagsEnum type
            var flagsEnum = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Name == "FlagsEnum");

            flagsEnum.Should().NotBeNull("FlagsEnum should exist in test assembly");
            flagsEnum.Should().BeOfType<DocEnum>("FlagsEnum should be a DocEnum instance");

            var docEnum = flagsEnum as DocEnum;
            docEnum!.IsFlags.Should().BeTrue("FlagsEnum should have IsFlags set to true");

            // Render the enum type
            var renderer = GetMarkdownRenderer();
            await renderer.RenderTypeAsync(flagsEnum!, assembly.Namespaces.First(n => n.Types.Contains(flagsEnum!)), _testOutputPath);

            var outputFile = Path.Combine(_testOutputPath, "CloudNimble-DotNetDocs-Tests-Shared-Enums.FlagsEnum.md");
            var content = await File.ReadAllTextAsync(outputFile, TestContext.CancellationToken);

            // Check that Flags attribute is mentioned
            content.Should().Contain("[Flags]", "Should indicate Flags attribute");
            content.Should().Contain("| `All` | 15 |", "Should include All value (Read | Write | Execute | Delete = 15)");
        }

        [TestMethod]
        public async Task RenderTypeAsync_Should_Show_Correct_Underlying_Type()
        {
            var assembly = GetTestsDotSharedAssembly();

            // Find the ByteEnum type
            var byteEnum = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Name == "ByteEnum");

            byteEnum.Should().NotBeNull("ByteEnum should exist in test assembly");
            byteEnum.Should().BeOfType<DocEnum>("ByteEnum should be a DocEnum instance");

            var docEnum = byteEnum as DocEnum;
            docEnum!.UnderlyingType.Should().NotBeNull("ByteEnum should have an underlying type");
            docEnum.UnderlyingType.DisplayName.Should().Be("byte", "ByteEnum should have byte as underlying type");

            // Render the enum type
            var renderer = GetMarkdownRenderer();
            await renderer.RenderTypeAsync(byteEnum!, assembly.Namespaces.First(n => n.Types.Contains(byteEnum!)), _testOutputPath);

            var outputFile = Path.Combine(_testOutputPath, "CloudNimble-DotNetDocs-Tests-Shared-Enums.ByteEnum.md");
            var content = await File.ReadAllTextAsync(outputFile, TestContext.CancellationToken);

            // Check that underlying type is shown
            content.Should().Contain("**Underlying Type:** byte", "Should show byte as underlying type");
        }

        #endregion

        #region Baseline Generation

        //[TestMethod]
        //[DataRow(projectPath)]
        [BreakdanceManifestGenerator]
        public async Task GenerateMarkdownBaselines(string projectPath)
        {
            await GenerateFileModeBaselines(projectPath);
            await GenerateFolderModeBaselines(projectPath);
        }

        /// <summary>
        /// Generates baseline files for MarkdownRenderer in FileMode configuration.
        /// </summary>
        /// <param name="projectPath">The root path of the test project where baselines will be stored.</param>
        /// <remarks>
        /// This method:
        /// 1. Gets the ProjectContext from DI to ensure consistent configuration
        /// 2. Modifies the context properties for FileMode output
        /// 3. Gets the MarkdownRenderer from DI with all dependencies properly injected
        /// 4. Uses AssemblyManager directly (not DocumentationManager) to generate documentation
        ///    without transformers, as these are unit test baselines for the renderer alone
        /// 5. Does not restore context values since this runs in an isolated Breakdance process
        /// </remarks>
        private async Task GenerateFileModeBaselines(string projectPath)
        {
            var baselinesDir = Path.Combine(projectPath, "Baselines", framework, "MarkdownRenderer", "FileMode");
            if (Directory.Exists(baselinesDir))
            {
                Directory.Delete(baselinesDir, true);
            }
            Directory.CreateDirectory(baselinesDir);

            // Get context from DI and modify it for baseline generation
            var context = GetService<ProjectContext>();
            context.Should().NotBeNull("ProjectContext should be registered in DI");

            context.FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '-');
            context.DocumentationRootPath = baselinesDir;
            context.ApiReferencePath = string.Empty;

            // Get renderer from DI to ensure all dependencies are properly injected
            var renderer = GetMarkdownRenderer();

            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync(context);

            await renderer.RenderAsync(assembly);
        }

        /// <summary>
        /// Generates baseline files for MarkdownRenderer in FolderMode configuration.
        /// </summary>
        /// <param name="projectPath">The root path of the test project where baselines will be stored.</param>
        /// <remarks>
        /// This method:
        /// 1. Gets the ProjectContext from DI to ensure consistent configuration
        /// 2. Modifies the context properties for FolderMode output
        /// 3. Gets the MarkdownRenderer from DI with all dependencies properly injected
        /// 4. Uses AssemblyManager directly (not DocumentationManager) to generate documentation
        ///    without transformers, as these are unit test baselines for the renderer alone
        /// 5. Does not restore context values since this runs in an isolated Breakdance process
        /// </remarks>
        private async Task GenerateFolderModeBaselines(string projectPath)
        {
            var baselinesDir = Path.Combine(projectPath, "Baselines", framework, "MarkdownRenderer", "FolderMode");
            if (Directory.Exists(baselinesDir))
            {
                Directory.Delete(baselinesDir, true);
            }
            Directory.CreateDirectory(baselinesDir);

            // Get context from DI and modify it for baseline generation
            var context = GetService<ProjectContext>();
            context.FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder, '-');
            context.DocumentationRootPath = baselinesDir;
            context.ApiReferencePath = string.Empty;

            // Get renderer from DI to ensure all dependencies are properly injected
            var renderer = GetMarkdownRenderer();

            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync(context);

            await renderer.RenderAsync(assembly);
        }

        #endregion

    }

}