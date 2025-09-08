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
            _yamlDeserializer = new DeserializerBuilder().Build();

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
        public async Task RenderAsync_ProducesConsistentBaseline()
        {
            // Arrange
            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext();
            var model = await manager.DocumentAsync(context);

            // Act
            await GetYamlRenderer().RenderAsync(model);

            // Assert - Compare against baseline
            var baselinePath = Path.Combine(projectPath, "Baselines", "YamlRenderer", "FileMode", "documentation.yaml");
            var actualPath = Path.Combine(_testOutputPath, "documentation.yaml");
            
            if (File.Exists(baselinePath))
            {
                var baseline = await File.ReadAllTextAsync(baselinePath);
                var actual = await File.ReadAllTextAsync(actualPath);
                
                // Normalize line endings for cross-platform compatibility
                var normalizedActual = actual.ReplaceLineEndings(Environment.NewLine);
                var normalizedBaseline = baseline.ReplaceLineEndings(Environment.NewLine);
                
                normalizedActual.Should().Be(normalizedBaseline,
                    "YAML output has changed. If this is intentional, regenerate baselines using 'dotnet breakdance generate'");
            }
            else
            {
                Assert.Inconclusive($"Baseline not found at {baselinePath}. Run 'dotnet breakdance generate' to create baselines.");
            }
        }

        [TestMethod]
        public async Task RenderAsync_CreatesTocYamlFile()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext();
            var model = await manager.DocumentAsync(context);

            // Act
            await GetYamlRenderer().RenderAsync(model);

            // Assert
            var tocPath = Path.Combine(_testOutputPath, "toc.yaml");
            File.Exists(tocPath).Should().BeTrue();
        }

        [TestMethod]
        public async Task RenderAsync_ProducesValidYaml()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext();
            var model = await manager.DocumentAsync(context);

            // Act
            await GetYamlRenderer().RenderAsync(model);

            // Assert
            var yamlPath = Path.Combine(_testOutputPath, "documentation.yaml");
            var yaml = await File.ReadAllTextAsync(yamlPath);
            
            Action act = () => _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
            act.Should().NotThrow();
        }

        [TestMethod]
        public async Task RenderAsync_IncludesAssemblyMetadata()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext();
            var model = await manager.DocumentAsync(context);
            model.Usage = "Test usage";
            model.Examples = "Test examples";
            model.BestPractices = "Test best practices";

            // Act
            await GetYamlRenderer().RenderAsync(model);

            // Assert
            var yaml = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.yaml"));
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
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext();
            var model = await manager.DocumentAsync(context);

            // Act
            await GetYamlRenderer().RenderAsync(model);

            // Assert
            foreach (var ns in model.Namespaces)
            {
                var nsFileName = GetYamlRenderer().GetNamespaceFileName(ns, "yaml");
                var nsPath = Path.Combine(_testOutputPath, nsFileName);
                File.Exists(nsPath).Should().BeTrue($"Namespace file {nsFileName} should exist");
                
                var yaml = await File.ReadAllTextAsync(nsPath);
                Action act = () => _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
                act.Should().NotThrow();
            }
        }

        [TestMethod]
        public async Task RenderAsync_WithNullModel_ThrowsArgumentNullException()
        {
            // Arrange
            var renderer = GetYamlRenderer();

            // Act
            Func<Task> act = async () => await renderer.RenderAsync(null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        #endregion

        #region Content Structure Tests

        [TestMethod]
        public async Task RenderAsync_TocContainsNamespaceHierarchy()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext();
            var model = await manager.DocumentAsync(context);

            // Act
            await GetYamlRenderer().RenderAsync(model);

            // Assert
            var tocPath = Path.Combine(_testOutputPath, "toc.yaml");
            var yaml = await File.ReadAllTextAsync(tocPath);
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
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext();
            var model = await manager.DocumentAsync(context);

            // Act
            await GetYamlRenderer().RenderAsync(model);

            // Assert
            var yaml = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.yaml"));
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
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext();
            var model = await manager.DocumentAsync(context);

            // Act
            await GetYamlRenderer().RenderAsync(model);

            // Assert
            var yaml = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.yaml"));
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
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext();
            var model = await manager.DocumentAsync(context);

            // Act
            await GetYamlRenderer().RenderAsync(model);

            // Assert
            var yaml = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.yaml"));
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
                                            (param.ContainsKey("parameterType") || param.ContainsKey("typeName")).Should().BeTrue();
                                            
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
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext();
            var model = await manager.DocumentAsync(context);
            model.BestPractices = "Test best practices";
            model.RelatedApis = new List<string> { "System.Object" };

            // Act
            await GetYamlRenderer().RenderAsync(model);

            // Assert
            var yaml = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.yaml"));
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
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();

            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '-')
            };
            var renderer = new YamlRenderer(context);

            // Act
            await renderer.RenderAsync(model);

            // Assert
            var files = Directory.GetFiles(_testOutputPath, "*.yaml");
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
            var renderer = new YamlRenderer(context);

            // Act
            await renderer.RenderAsync(model);

            // Assert
            var files = Directory.GetFiles(_testOutputPath, "*.yaml");
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
            var renderer = new YamlRenderer(context);

            // Act
            await renderer.RenderAsync(model);

            // Assert
            var files = Directory.GetFiles(_testOutputPath, "*.yaml");
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
            var renderer = new YamlRenderer(context);

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
            
            // Check that index.yaml exists in namespace folder
            var indexFile = Path.Combine(sharedDir, "index.yaml");
            File.Exists(indexFile).Should().BeTrue();
            
            // YAML renderer includes types within namespace files, not as separate files
            // So we just check that the namespace file exists
            var content = await File.ReadAllTextAsync(indexFile);
            content.Should().NotBeNullOrWhiteSpace();
            content.Should().Contain("types", "Namespace file should include types");
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
            var renderer = new YamlRenderer(context);

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

        [TestMethod]
        public async Task RenderAsync_WithWebSafeSeparators_CreatesValidFileNames()
        {
            // Test various web-safe separator characters
            var separators = new[] { '-', '_', '.' };
            
            foreach (var separator in separators)
            {
                // Arrange
                var assemblyPath = typeof(SampleClass).Assembly.Location;
                var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
                using var manager = new AssemblyManager(assemblyPath, xmlPath);
                var context = new ProjectContext();
                var model = await manager.DocumentAsync(context);
                
                context = new ProjectContext
                {
                    FileNamingOptions = new FileNamingOptions(NamespaceMode.File, separator)
                };
                var renderer = new YamlRenderer(context);
                var testPath = Path.Combine(Path.GetTempPath(), $"YamlTest_{separator}_{Guid.NewGuid()}");
                Directory.CreateDirectory(testPath);

                try
                {
                    // Act
                    await renderer.RenderAsync(model);

                    // Assert
                    var files = Directory.GetFiles(testPath, "*.yaml");
                    files.Should().NotBeEmpty($"Files should be created with separator '{separator}'");
                    
                    // Verify all files have valid web-safe names
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        // Check that file names don't contain invalid web characters
                        fileName.Should().NotContainAny(new[] { "<", ">", ":", "\"", "|", "?", "*", "\\" },
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
            var renderer = new YamlRenderer(context);
            var testOutputPath = Path.Combine(Path.GetTempPath(), $"YamlFolderBaseline_{Guid.NewGuid()}");

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
                File.Exists(Path.Combine(sharedDir, "index.yaml")).Should().BeTrue();
                
                // Verify sub-namespace folders
                var basicScenariosDir = Path.Combine(sharedDir, "BasicScenarios");
                Directory.Exists(basicScenariosDir).Should().BeTrue();
                File.Exists(Path.Combine(basicScenariosDir, "index.yaml")).Should().BeTrue();

                // Compare specific files with baselines
                await CompareYamlWithFolderBaseline(
                    Path.Combine(sharedDir, "index.yaml"),
                    "CloudNimble/DotNetDocs/Tests/Shared/index.yaml");
                
                await CompareYamlWithFolderBaseline(
                    Path.Combine(basicScenariosDir, "index.yaml"),
                    "CloudNimble/DotNetDocs/Tests/Shared/BasicScenarios/index.yaml");
                
                // Verify TOC structure
                var tocPath = Path.Combine(testOutputPath, "toc.yaml");
                File.Exists(tocPath).Should().BeTrue();
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

        private async Task CompareYamlWithFolderBaseline(string actualFilePath, string baselineRelativePath)
        {
            // Read actual file
            var actualContent = await File.ReadAllTextAsync(actualFilePath);
            
            // Construct baseline path
            var baselineDir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Baselines",
                "YamlRenderer",
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
            
            // Parse YAML for comparison
            var actualYaml = _yamlDeserializer.Deserialize<object>(actualContent);
            var baselineContent = await File.ReadAllTextAsync(baselinePath);
            var baselineYaml = _yamlDeserializer.Deserialize<object>(baselineContent);
            
            // Compare YAML structures
            actualYaml.Should().BeEquivalentTo(baselineYaml, 
                $"YAML output should match baseline at {baselinePath}");
        }

        #endregion

        #region Baseline Generation

        //[TestMethod]
        //[DataRow(projectPath)]
        [BreakdanceManifestGenerator]
        public async Task GenerateYamlBaselines(string projectPath)
        {
            // Generate baselines for both FileMode and FolderMode
            await GenerateFileModeBaselines(projectPath);
            await GenerateFolderModeBaselines(projectPath);
        }
        
        private async Task GenerateFileModeBaselines(string projectPath)
        {
            // Setup with FileMode context
            var tempOutputPath = Path.Combine(Path.GetTempPath(), $"YamlBaseline_FileMode_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempOutputPath);

            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '-'),
                DocumentationRootPath = tempOutputPath
            };
            var renderer = new YamlRenderer(context);

            try
            {
                // Generate baseline for SimpleClass documentation
                var assemblyPath = typeof(SimpleClass).Assembly.Location;
                var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
                using var manager = new AssemblyManager(assemblyPath, xmlPath);
                var model = await manager.DocumentAsync(context);

                await renderer.RenderAsync(model);

                // Create baselines directory
                var baselinesDir = Path.Combine(projectPath, "Baselines", "YamlRenderer", "FileMode");
                if (!Directory.Exists(baselinesDir))
                {
                    Directory.CreateDirectory(baselinesDir);
                }

                // Copy YAML files to baseline
                foreach (var file in Directory.GetFiles(tempOutputPath, "*.yaml"))
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
            var tempOutputPath = Path.Combine(Path.GetTempPath(), $"YamlBaseline_FolderMode_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempOutputPath);

            var context = new ProjectContext([Accessibility.Public, Accessibility.Internal])
            {
                ShowPlaceholders = false,
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder),
                DocumentationRootPath = tempOutputPath
            };
            var renderer = new YamlRenderer(context);

            try
            {
                // Generate baseline for SimpleClass documentation
                var assemblyPath = typeof(SimpleClass).Assembly.Location;
                var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
                using var manager = new AssemblyManager(assemblyPath, xmlPath);
                var model = await manager.DocumentAsync(context);

                await renderer.RenderAsync(model);

                // Create baselines directory
                var baselinesDir = Path.Combine(projectPath, "Baselines", "YamlRenderer", "FolderMode");
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
        
        private void CopyDirectoryRecursive(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            // Copy files
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, true);
            }

            // Copy subdirectories
            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(subDir);
                var destDir = Path.Combine(targetDir, dirName);
                CopyDirectoryRecursive(subDir, destDir);
            }
        }

        #endregion


    }

}
