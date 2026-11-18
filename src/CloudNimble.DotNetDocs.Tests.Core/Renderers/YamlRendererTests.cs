#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Assemblies;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Core.Renderers;
using CloudNimble.DotNetDocs.Tests.Core.Helpers;
using CloudNimble.DotNetDocs.Tests.Shared;
using CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios;
using CloudNimble.DotNetDocs.Tests.Shared.Parameters;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YamlDotNet.Serialization;

namespace CloudNimble.DotNetDocs.Tests.Core.Renderers
{

    /// <summary>
    /// Tests for the YamlRenderer class.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class YamlRendererTests : DotNetDocsTestBase
    {

        #region Fields

        private string _testOutputPath = null!;
        private IDeserializer _yamlDeserializer = null!;

        #endregion

        #region Helper Methods

        private YamlRenderer GetYamlRenderer()
        {
            var renderer = GetServices<IDocRenderer>()
                .OfType<YamlRenderer>()
                .FirstOrDefault();
            renderer.Should().NotBeNull("YamlRenderer should be registered in DI");
            return renderer!;
        }

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            _testOutputPath = Path.Combine(Path.GetTempPath(), $"YamlRendererTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testOutputPath);
            _yamlDeserializer = new DeserializerBuilder()
                .EnablePrivateConstructors()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            // Configure services for DI
            TestHostBuilder.ConfigureServices((context, services) =>
            {
                services.AddDotNetDocsPipeline(pipeline =>
                {
                    pipeline.UseYamlRenderer()
                        .ConfigureContext(ctx =>
                        {
                            ctx.DocumentationRootPath = _testOutputPath;
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
        [DataRow(false, DisplayName = "Public members")]
        [DataRow(true, DisplayName = "Public and internal members")]
        public async Task RenderAsync_ProducesConsistentBaseline(bool includeInternals)
        {
            // Arrange
            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var context = GetService<ProjectContext>();

            // The FileMode baselines are generated with an empty ApiReferencePath, so render the same way
            context.ApiReferencePath = string.Empty;
            context.IncludedMembers = BaselinePermutations.GetIncludedMembers(includeInternals);
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync(context);
            var renderer = GetYamlRenderer();

            // Act
            await renderer.RenderAsync(assembly);

            // Assert - Compare every rendered file with the FileMode baselines, reporting all differences at once
            BaselineAssert.DirectoryMatches(Path.Combine(projectPath, "Baselines", framework, "YamlRenderer", BaselinePermutations.GetName("FileMode", includeInternals)), _testOutputPath);
        }

        [TestMethod]
        public async Task RenderAsync_CreatesTocYamlFile()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            var context = GetService<ProjectContext>();
            var tocPath = Path.Combine(_testOutputPath, context.ApiReferencePath, "toc.yaml");
            File.Exists(tocPath).Should().BeTrue();
        }

        [TestMethod]
        public async Task RenderAsync_ProducesValidYaml()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            var context = GetService<ProjectContext>();
            var yamlPath = Path.Combine(_testOutputPath, context.ApiReferencePath, "documentation.yaml");
            var yaml = await File.ReadAllTextAsync(yamlPath, TestContext.CancellationToken);

            Action act = () => _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
            act.Should().NotThrow();
        }

        [TestMethod]
        public async Task RenderAsync_IncludesAssemblyMetadata()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var context = GetService<ProjectContext>();
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync(context);
            model.Usage = "Test usage";
            model.Examples = "Test examples";
            model.BestPractices = "Test best practices";

            // Act
            await GetYamlRenderer().RenderAsync(model);

            // Assert
            var yaml = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, context.ApiReferencePath, "documentation.yaml"), TestContext.CancellationToken);
            var document = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);

            // DocAssembly is serialized directly at root level
            document.Should().ContainKey("assemblyName");
            document["assemblyName"].Should().Be(model.AssemblyName);
            document.Should().ContainKey("version");
            document["version"].Should().NotBeNull();
            document["usage"].Should().Be("Test usage");
            document["examples"].Should().Be("Test examples");
            document["bestPractices"].Should().Be("Test best practices");
        }

        [TestMethod]
        public async Task RenderAsync_CreatesNamespaceFiles()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var context = GetService<ProjectContext>();
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync(context);

            // Act
            await GetYamlRenderer().RenderAsync(model);

            // Assert
            var renderer = GetYamlRenderer();
            foreach (var ns in model.Namespaces)
            {
                var nsFileName = renderer.GetNamespaceFileName(ns, "yaml");
                var nsPath = Path.Combine(_testOutputPath, context.ApiReferencePath, nsFileName);
                File.Exists(nsPath).Should().BeTrue($"Namespace file {nsFileName} should exist");

                var yaml = await File.ReadAllTextAsync(nsPath, TestContext.CancellationToken);
                Action act = () => _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
                act.Should().NotThrow();
            }
        }

        [TestMethod]
        public async Task RenderAsync_WithNullModel_ReturnsWithoutError()
        {
            // Arrange
            var renderer = GetYamlRenderer();
            var context = GetService<ProjectContext>();

            // Act - Documentation-only mode passes null model to renderers
            Func<Task> act = async () => await renderer.RenderAsync(null);

            // Assert - Should not throw, should return gracefully
            await act.Should().NotThrowAsync();

            // Verify no documentation.yaml was created (nothing to render)
            var docYamlPath = Path.Combine(_testOutputPath, context.ApiReferencePath, "documentation.yaml");
            File.Exists(docYamlPath).Should().BeFalse("No documentation file should be created for null model");
        }

        #endregion

        #region Content Structure Tests

        [TestMethod]
        public async Task RenderAsync_TocContainsNamespaceHierarchy()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            var context = GetService<ProjectContext>();
            var tocPath = Path.Combine(_testOutputPath, context.ApiReferencePath, "toc.yaml");
            var yaml = await File.ReadAllTextAsync(tocPath, TestContext.CancellationToken);
            var toc = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);

            toc.Should().ContainKey("title");
            toc.Should().ContainKey("items");
            var items = toc["items"] as List<object>;
            items.Should().NotBeNull();
            items.Should().HaveCountGreaterThan(0);

            foreach (var item in items.Cast<Dictionary<object, object>>())
            {
                item.Should().ContainKey("name");
                item.Should().ContainKey("href");
                item.Should().ContainKey("types");
            }
        }

        [TestMethod]
        public async Task RenderAsync_IncludesTypeInformation()
        {
            // Arrange
            var assemblyPath = typeof(ClassWithMethods).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            var context = GetService<ProjectContext>();
            var yaml = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, context.ApiReferencePath, "documentation.yaml"), TestContext.CancellationToken);
            var document = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);

            // DocAssembly is serialized directly at root level
            var namespaces = document["namespaces"] as List<object>;
            namespaces.Should().NotBeNull();

            var hasTypes = false;
            foreach (var ns in namespaces.Cast<Dictionary<object, object>>())
            {
                if (ns.ContainsKey("types"))
                {
                    var types = ns["types"] as List<object>;
                    if (types is not null && types.Count > 0)
                    {
                        hasTypes = true;
                        foreach (var type in types.Cast<Dictionary<object, object>>())
                        {
                            type.Should().ContainKey("name");
                            type.Should().ContainKey("fullName");
                            type.Should().ContainKey("typeKind");
                        }
                    }
                }
            }

            hasTypes.Should().BeTrue("At least one type should be present");
        }

        [TestMethod]
        public async Task RenderAsync_IncludesMemberModifiers()
        {
            // Arrange
            var assemblyPath = typeof(ClassWithProperties).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            var context = GetService<ProjectContext>();
            var yaml = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, context.ApiReferencePath, "documentation.yaml"), TestContext.CancellationToken);
            var document = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);

            // DocAssembly is serialized directly at root level
            var namespaces = document["namespaces"] as List<object>;

            // Instead of looking for a "modifiers" field, verify that member metadata is properly serialized
            // Modifiers are encoded in other properties like accessibility, methodKind, etc.
            var hasMembers = false;
            foreach (var ns in namespaces.Cast<Dictionary<object, object>>())
            {
                if (ns.ContainsKey("types"))
                {
                    var types = ns["types"] as List<object>;
                    foreach (var type in types.Cast<Dictionary<object, object>>())
                    {
                        if (type["name"].ToString() == "ClassWithProperties" && type.ContainsKey("members"))
                        {
                            var members = type["members"] as List<object>;
                            hasMembers = members.Count > 0;
                            foreach (var member in members.Cast<Dictionary<object, object>>())
                            {
                                // Verify members have appropriate metadata fields
                                member.Should().ContainKey("accessibility");
                                member.Should().ContainKey("memberKind");
                                // Methods should have methodKind
                                if (member["memberKind"].ToString() == "Method")
                                {
                                    member.Should().ContainKey("methodKind");
                                }
                            }
                        }
                    }
                }
            }

            hasMembers.Should().BeTrue("ClassWithProperties should have members");
        }

        [TestMethod]
        public async Task RenderAsync_IncludesParameterDetails()
        {
            // Arrange
            var assemblyPath = typeof(ParameterVariations).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            var context = GetService<ProjectContext>();
            var yaml = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, context.ApiReferencePath, "documentation.yaml"), TestContext.CancellationToken);
            var document = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);

            // DocAssembly is serialized directly at root level
            var namespaces = document["namespaces"] as List<object>;

            var hasParameters = false;
            foreach (var ns in namespaces.Cast<Dictionary<object, object>>())
            {
                if (ns.ContainsKey("types"))
                {
                    var types = ns["types"] as List<object>;
                    foreach (var type in types.Cast<Dictionary<object, object>>())
                    {
                        if (type.ContainsKey("members"))
                        {
                            var members = type["members"] as List<object>;
                            foreach (var member in members.Cast<Dictionary<object, object>>())
                            {
                                if (member.ContainsKey("parameters"))
                                {
                                    var parameters = member["parameters"] as List<object>;
                                    if (parameters is not null && parameters.Count > 0)
                                    {
                                        hasParameters = true;
                                        foreach (var param in parameters.Cast<Dictionary<object, object>>())
                                        {
                                            param.Should().ContainKey("name");
                                            // parameterType or typeName should exist
                                            (param.ContainsKey("parameterType") || param.ContainsKey("typeName")).Should().BeTrue("parameterType or typeName should exist");

                                            // These properties are only present when true or when hasDefaultValue is true
                                            // We just need to verify the structure is correct when they exist
                                            if (param.ContainsKey("isOptional"))
                                            {
                                                var value = param["isOptional"];
                                                (value is bool || (value is string str && (str == "true" || str == "false")))
                                                    .Should().BeTrue($"isOptional should be a boolean");
                                            }
                                            if (param.ContainsKey("isParams"))
                                            {
                                                var value = param["isParams"];
                                                (value is bool || (value is string str && (str == "true" || str == "false")))
                                                    .Should().BeTrue($"isParams should be a boolean");
                                            }
                                            if (param.ContainsKey("hasDefaultValue"))
                                            {
                                                var value = param["hasDefaultValue"];
                                                (value is bool || (value is string str && (str == "true" || str == "false")))
                                                    .Should().BeTrue($"hasDefaultValue should be a boolean");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            hasParameters.Should().BeTrue("At least one method with parameters should be present");
        }

        [TestMethod]
        public async Task RenderAsync_UsesProperYamlNamingConvention()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var context = GetService<ProjectContext>();
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync(context);
            model.BestPractices = "Test best practices";
            model.RelatedApis = ["System.Object"];

            // Act
            await GetYamlRenderer().RenderAsync(model);

            // Assert
            var yaml = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, context.ApiReferencePath, "documentation.yaml"), TestContext.CancellationToken);
            var document = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);

            // DocAssembly is serialized directly at root level
            // Properties should be camelCase
            document.Should().ContainKey("bestPractices");
            document.Should().ContainKey("relatedApis");
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
                var files = Directory.GetFiles(_testOutputPath, "*.yaml");
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
                var files = Directory.GetFiles(_testOutputPath, "*.yaml");
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
                var files = Directory.GetFiles(_testOutputPath, "*.yaml");
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

                // Check that index.yaml exists in namespace folder
                var indexFile = Path.Combine(sharedDir, "index.yaml");
                File.Exists(indexFile).Should().BeTrue();

                // YAML renderer includes types within namespace files, not as separate files
                // So we just check that the namespace file exists
                var content = await File.ReadAllTextAsync(indexFile, TestContext.CancellationToken);
                content.Should().NotBeNullOrWhiteSpace();
                content.Should().Contain("types", "Namespace file should include types");
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

        [TestMethod]
        public async Task RenderAsync_WithWebSafeSeparators_CreatesValidFileNames()
        {
            // Test various web-safe separator characters
            var separators = new[] { '-', '_', '.' };

            var context = GetService<ProjectContext>();
            var originalFileNamingOptions = context.FileNamingOptions;
            var originalApiReferencePath = context.ApiReferencePath;
            var originalPath = context.DocumentationRootPath;

            foreach (var separator in separators)
            {
                // Arrange
                var assemblyPath = typeof(SampleClass).Assembly.Location;
                var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
                var documentationManager = GetService<DocumentationManager>();

                var testPath = Path.Combine(Path.GetTempPath(), $"YamlTest_{separator}_{Guid.NewGuid()}");
                Directory.CreateDirectory(testPath);

                context.FileNamingOptions = new FileNamingOptions(NamespaceMode.File, separator);
                context.DocumentationRootPath = testPath;
                context.ApiReferencePath = string.Empty;

                try
                {
                    // Act
                    await documentationManager.ProcessAsync(assemblyPath, xmlPath);

                    // Assert
                    var files = Directory.GetFiles(testPath, "*.yaml");
                    files.Should().NotBeEmpty($"Files should be created with separator '{separator}'");

                    // Verify all files have valid web-safe names
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        // Check that file names don't contain invalid web characters
                        fileName.Should().NotContainAny(["<", ">", ":", "\"", "|", "?", "*", "\\"],
                            $"File names with separator '{separator}' should be web-safe");
                    }
                }
                finally
                {
                    if (Directory.Exists(testPath))
                    {
                        Directory.Delete(testPath, true);
                    }
                }
            }

            // Restore original settings
            context.FileNamingOptions = originalFileNamingOptions;
            context.DocumentationRootPath = originalPath;
            context.ApiReferencePath = originalApiReferencePath;
        }

        #endregion

        #region Folder Structure Baseline Tests

        [TestMethod]
        [DataRow(false, DisplayName = "Public members")]
        [DataRow(true, DisplayName = "Public and internal members")]
        public async Task RenderAsync_WithFolderMode_MatchesFolderStructureBaseline(bool includeInternals)
        {
            var testOutputPath = Path.Combine(Path.GetTempPath(), $"YamlFolderBaseline_{Guid.NewGuid()}");

            // Use the test's DI container and update the context
            var context = GetService<ProjectContext>();
            var originalFileNamingOptions = context.FileNamingOptions;
            var originalPath = context.DocumentationRootPath;
            var originalApiReferencePath = context.ApiReferencePath;
            var originalIncludedMembers = context.IncludedMembers;

            context.FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder);
            context.DocumentationRootPath = testOutputPath;
            context.ApiReferencePath = string.Empty;
            context.IncludedMembers = BaselinePermutations.GetIncludedMembers(includeInternals);

            try
            {
                // Generate documentation using AssemblyManager and renderer directly
                var assemblyPath = typeof(SampleClass).Assembly.Location;
                var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

                using var manager = new AssemblyManager(assemblyPath, xmlPath);
                var assembly = await manager.DocumentAsync(context);
                var renderer = GetYamlRenderer();
                await renderer.RenderAsync(assembly);

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
                File.Exists(Path.Combine(sharedDir, "index.yaml")).Should().BeTrue();

                // Verify sub-namespace folders
                var basicScenariosDir = Path.Combine(sharedDir, "BasicScenarios");
                Directory.Exists(basicScenariosDir).Should().BeTrue();
                File.Exists(Path.Combine(basicScenariosDir, "index.yaml")).Should().BeTrue();

                // Compare every rendered file with the FolderMode baselines, reporting all differences at once
                BaselineAssert.DirectoryMatches(Path.Combine(projectPath, "Baselines", framework, "YamlRenderer", BaselinePermutations.GetName("FolderMode", includeInternals)), testOutputPath);

                // Verify TOC structure
                var tocPath = Path.Combine(testOutputPath, "toc.yaml");
                File.Exists(tocPath).Should().BeTrue();
            }
            finally
            {
                // Restore original settings
                context.FileNamingOptions = originalFileNamingOptions;
                context.DocumentationRootPath = originalPath;
                context.ApiReferencePath = originalApiReferencePath;
                context.IncludedMembers = originalIncludedMembers;

                // Cleanup
                if (Directory.Exists(testOutputPath))
                {
                    Directory.Delete(testOutputPath, true);
                }
            }
        }

        #endregion

        #region Baseline Generation

        /// <summary>
        /// Generates baseline files for the YamlRenderer in FileMode and FolderMode, each for public members and for public
        /// and internal members.
        /// This method is marked with [BreakdanceManifestGenerator] and is called by the Breakdance tool
        /// to generate baseline files for comparison in unit tests.
        /// </summary>
        /// <param name="projectPath">The root path of the test project where baselines will be stored.</param>
        /// <remarks>
        /// This method uses Dependency Injection to get the ProjectContext and YamlRenderer instances.
        /// It modifies the context properties to configure the output location, file naming options, and documented members,
        /// then uses the renderer from DI (which ensures all dependencies are properly injected).
        ///
        /// The baseline generation intentionally does NOT use DocumentationManager.ProcessAsync because
        /// these are unit test baselines for the renderer itself, not integration test baselines.
        /// The renderer should be tested in isolation without transformers applied.
        /// </remarks>
        //[TestMethod]
        //[DataRow(projectPath)]
        [BreakdanceManifestGenerator]
        public async Task GenerateYamlBaselines(string projectPath)
        {
            foreach (var namespaceMode in new[] { NamespaceMode.File, NamespaceMode.Folder })
            {
                foreach (var includeInternals in new[] { false, true })
                {
                    await GenerateBaselinesAsync(projectPath, namespaceMode, includeInternals);
                }
            }
        }

        /// <summary>
        /// Generates the YamlRenderer baseline files for one permutation.
        /// </summary>
        /// <param name="projectPath">The root path of the test project where baselines will be stored.</param>
        /// <param name="namespaceMode">Whether namespaces render as files or folders.</param>
        /// <param name="includeInternals">Whether internal members are documented along with public ones.</param>
        /// <remarks>
        /// This method:
        /// 1. Clears the permutation's baseline folder, such as "FolderMode" or "FolderModeWithInternals"
        /// 2. Creates a DI scope so no renderer state carries over from another permutation
        /// 3. Sets every ProjectContext property the permutation depends on
        /// 4. Uses AssemblyManager directly (not DocumentationManager) to generate documentation
        ///    without transformers, as these are unit test baselines for the renderer alone
        /// </remarks>
        private async Task GenerateBaselinesAsync(string projectPath, NamespaceMode namespaceMode, bool includeInternals)
        {
            var baselinesDir = Path.Combine(projectPath, "Baselines", framework, "YamlRenderer", BaselinePermutations.GetName($"{namespaceMode}Mode", includeInternals));
            if (Directory.Exists(baselinesDir))
            {
                Directory.Delete(baselinesDir, true);
            }
            Directory.CreateDirectory(baselinesDir);

            using var scope = TestHost.Services.CreateScope();

            // Get context from DI and modify it for baseline generation
            var context = scope.ServiceProvider.GetRequiredService<ProjectContext>();
            context.FileNamingOptions = new FileNamingOptions(namespaceMode, '-');
            context.DocumentationRootPath = baselinesDir;
            context.ApiReferencePath = string.Empty;
            context.IncludedMembers = BaselinePermutations.GetIncludedMembers(includeInternals);

            // Get renderer from DI to ensure all dependencies are properly injected
            var renderer = scope.ServiceProvider.GetServices<IDocRenderer>().OfType<YamlRenderer>().Single();

            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync(context);

            await renderer.RenderAsync(assembly);
        }

        #endregion

    }

}
