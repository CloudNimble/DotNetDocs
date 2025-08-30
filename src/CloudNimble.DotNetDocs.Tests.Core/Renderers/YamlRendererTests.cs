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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
        private YamlRenderer _renderer = null!;
        private IDeserializer _yamlDeserializer = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            _testOutputPath = Path.Combine(Path.GetTempPath(), $"YamlRendererTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testOutputPath);
            _renderer = new YamlRenderer();
            _yamlDeserializer = new DeserializerBuilder().Build();
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
            var context = new ProjectContext { IgnoreGlobalModule = false };
            var model = await manager.DocumentAsync(context);

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert - Compare against baseline
            var baselinePath = Path.Combine(projectPath, "Baselines", "YamlRenderer", "FileMode", "documentation.yaml");
            var actualPath = Path.Combine(_testOutputPath, "documentation.yaml");
            
            if (File.Exists(baselinePath))
            {
                var baseline = await File.ReadAllTextAsync(baselinePath);
                var actual = await File.ReadAllTextAsync(actualPath);
                
                actual.Should().Be(baseline,
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
            var context = new ProjectContext { IgnoreGlobalModule = false };
            var model = await manager.DocumentAsync(context);

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

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
            var context = new ProjectContext { IgnoreGlobalModule = false };
            var model = await manager.DocumentAsync(context);

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

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
            var context = new ProjectContext { IgnoreGlobalModule = false };
            var model = await manager.DocumentAsync(context);
            model.Usage = "Test usage";
            model.Examples = "Test examples";
            model.BestPractices = "Test best practices";

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var yaml = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.yaml"));
            var document = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
            
            document.Should().ContainKey("assembly");
            var assembly = document["assembly"] as Dictionary<object, object>;
            assembly.Should().NotBeNull();
            
            assembly["name"].Should().Be(model.AssemblyName);
            assembly["version"].Should().NotBeNull();
            assembly["usage"].Should().Be("Test usage");
            assembly["examples"].Should().Be("Test examples");
            assembly["bestPractices"].Should().Be("Test best practices");
        }

        [TestMethod]
        public async Task RenderAsync_CreatesNamespaceFiles()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext { IgnoreGlobalModule = false };
            var model = await manager.DocumentAsync(context);

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            foreach (var ns in model.Namespaces)
            {
                var namespaceName = ns.Symbol.IsGlobalNamespace ? "global" : ns.Symbol.ToDisplayString();
                var nsFileName = $"{namespaceName.Replace('.', '-')}.yaml";
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
            var context = new ProjectContext();

            // Act
            Func<Task> act = async () => await _renderer.RenderAsync(null, _testOutputPath, context);

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
            var context = new ProjectContext { IgnoreGlobalModule = false };
            var model = await manager.DocumentAsync(context);

            // Act
            Func<Task> act = async () => await _renderer.RenderAsync(model, null, context);

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
            var context = new ProjectContext { IgnoreGlobalModule = false };
            var model = await manager.DocumentAsync(context);

            // Act
            Func<Task> act = async () => await _renderer.RenderAsync(model, _testOutputPath, null);

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
            var context = new ProjectContext { IgnoreGlobalModule = false };
            var model = await manager.DocumentAsync(context);

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var tocPath = Path.Combine(_testOutputPath, "toc.yaml");
            var yaml = await File.ReadAllTextAsync(tocPath);
            var toc = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
            
            toc.Should().ContainKey("items");
            var items = toc["items"] as List<object>;
            items.Should().NotBeNull();
            items.Should().HaveCountGreaterThan(0);
            
            foreach (var item in items.Cast<Dictionary<object, object>>())
            {
                item.Should().ContainKey("name");
                item.Should().ContainKey("href");
                item.Should().ContainKey("items");
            }
        }

        [TestMethod]
        public async Task RenderAsync_IncludesTypeInformation()
        {
            // Arrange
            var assemblyPath = typeof(ClassWithMethods).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext { IgnoreGlobalModule = false };
            var model = await manager.DocumentAsync(context);

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var yaml = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.yaml"));
            var document = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
            
            var assembly = document["assembly"] as Dictionary<object, object>;
            var namespaces = assembly["namespaces"] as List<object>;
            namespaces.Should().NotBeNull();
            
            var hasTypes = false;
            foreach (var ns in namespaces.Cast<Dictionary<object, object>>())
            {
                if (ns.ContainsKey("types"))
                {
                    var types = ns["types"] as List<object>;
                    if (types != null && types.Count > 0)
                    {
                        hasTypes = true;
                        foreach (var type in types.Cast<Dictionary<object, object>>())
                        {
                            type.Should().ContainKey("name");
                            type.Should().ContainKey("fullName");
                            type.Should().ContainKey("kind");
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
            var context = new ProjectContext { IgnoreGlobalModule = false };
            var model = await manager.DocumentAsync(context);

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var yaml = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.yaml"));
            var document = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
            
            var assembly = document["assembly"] as Dictionary<object, object>;
            var namespaces = assembly["namespaces"] as List<object>;
            
            var hasModifiers = false;
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
                            foreach (var member in members.Cast<Dictionary<object, object>>())
                            {
                                if (member.ContainsKey("modifiers"))
                                {
                                    hasModifiers = true;
                                    var modifiers = member["modifiers"] as List<object>;
                                    modifiers.Should().NotBeNull();
                                }
                            }
                        }
                    }
                }
            }
            
            hasModifiers.Should().BeTrue("Members should have modifiers");
        }

        [TestMethod]
        public async Task RenderAsync_IncludesParameterDetails()
        {
            // Arrange
            var assemblyPath = typeof(ParameterVariations).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext { IgnoreGlobalModule = false };
            var model = await manager.DocumentAsync(context);

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var yaml = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.yaml"));
            var document = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
            
            var assembly = document["assembly"] as Dictionary<object, object>;
            var namespaces = assembly["namespaces"] as List<object>;
            
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
                                    if (parameters != null && parameters.Count > 0)
                                    {
                                        hasParameters = true;
                                        foreach (var param in parameters.Cast<Dictionary<object, object>>())
                                        {
                                            param.Should().ContainKey("name");
                                            param.Should().ContainKey("type");
                                            param.Should().ContainKey("isOptional");
                                            param.Should().ContainKey("isParams");
                                            param.Should().ContainKey("isRef");
                                            param.Should().ContainKey("isOut");
                                            param.Should().ContainKey("isIn");
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
            var context = new ProjectContext { IgnoreGlobalModule = false };
            var model = await manager.DocumentAsync(context);
            model.BestPractices = "Test best practices";
            model.RelatedApis = new List<string> { "System.Object" };

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var yaml = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.yaml"));
            var document = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
            
            var assembly = document["assembly"] as Dictionary<object, object>;
            
            // Properties should be camelCase
            assembly.Should().ContainKey("bestPractices");
            assembly.Should().ContainKey("relatedApis");
        }

        #endregion

        #region Baseline Generation

        //[TestMethod]
        //[DataRow(projectPath)]
        [BreakdanceManifestGenerator]
        public async Task GenerateYamlBaselines(string projectPath)
        {
            // Setup
            var renderer = new YamlRenderer();
            var yamlDeserializer = new DeserializerBuilder().Build();
            var tempOutputPath = Path.Combine(Path.GetTempPath(), $"YamlBaseline_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempOutputPath);

            try
            {
                // Generate baseline for SimpleClass documentation
                var assemblyPath = typeof(SimpleClass).Assembly.Location;
                var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
                using var manager = new AssemblyManager(assemblyPath, xmlPath);
                var context = new ProjectContext { IgnoreGlobalModule = false };
                var model = await manager.DocumentAsync(context);

                await renderer.RenderAsync(model, tempOutputPath, context);

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

        #endregion

        #region FileNamingOptions Tests

        [TestMethod]
        public async Task RenderAsync_WithFileModeDefaultSeparator_CreatesFilesWithHyphen()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext { IgnoreGlobalModule = false };
            var model = await manager.DocumentAsync(context);
            
            context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '-')
            };
            var renderer = new YamlRenderer(context);

            // Act
            await renderer.RenderAsync(model, _testOutputPath, context);

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
            var context = new ProjectContext { IgnoreGlobalModule = false };
            var model = await manager.DocumentAsync(context);
            
            context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '_')
            };
            var renderer = new YamlRenderer(context);

            // Act
            await renderer.RenderAsync(model, _testOutputPath, context);

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
            var context = new ProjectContext { IgnoreGlobalModule = false };
            var model = await manager.DocumentAsync(context);
            
            context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '.')
            };
            var renderer = new YamlRenderer(context);

            // Act
            await renderer.RenderAsync(model, _testOutputPath, context);

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
            var context = new ProjectContext { IgnoreGlobalModule = false };
            var model = await manager.DocumentAsync(context);
            
            context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder)
            };
            var renderer = new YamlRenderer(context);

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
            var context = new ProjectContext { IgnoreGlobalModule = false };
            var model = await manager.DocumentAsync(context);
            
            // Set a separator that should be ignored in Folder mode
            context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder, '_')
            };
            var renderer = new YamlRenderer(context);

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
                var context = new ProjectContext { IgnoreGlobalModule = false };
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
                    await renderer.RenderAsync(model, testPath, context);

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

        #region Internal Method Tests

        #region SerializeNamespaces Tests

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SerializeNamespaces_Should_Return_List_Of_Namespace_Dictionaries(bool ignoreGlobalModule)
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly(ignoreGlobalModule);
            var context = new ProjectContext { OutputPath = _testOutputPath };
            var renderer = new YamlRenderer(context);

            // Act
            var result = renderer.SerializeNamespaces(assembly);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<List<Dictionary<string, object>>>();
            var namespaces = result as List<Dictionary<string, object>>;
            namespaces.Should().HaveCount(assembly.Namespaces.Count);
            
            // When ignoreGlobalModule is true, global namespace should not be present
            if (ignoreGlobalModule)
            {
                assembly.Namespaces.Should().NotContain(ns => ns.Symbol.IsGlobalNamespace);
            }
        }

        [TestMethod]
        public void SerializeNamespaces_Should_Include_Namespace_Properties()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var context = new ProjectContext { OutputPath = _testOutputPath };
            var renderer = new YamlRenderer(context);

            // Act
            var result = renderer.SerializeNamespaces(assembly) as List<Dictionary<string, object>>;

            // Assert
            result.Should().NotBeNull();
            var firstNamespace = result!.First();
            firstNamespace.Should().ContainKey("name");
            firstNamespace.Should().ContainKey("types");
            firstNamespace.Should().ContainKey("usage");
        }

        #endregion

        #region SerializeTypes Tests

        [TestMethod]
        public void SerializeTypes_Should_Return_List_Of_Type_Dictionaries()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var ns = assembly.Namespaces.First();
            var context = new ProjectContext { OutputPath = _testOutputPath };
            var renderer = new YamlRenderer(context);

            // Act
            var result = renderer.SerializeTypes(ns);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<List<Dictionary<string, object>>>();
            var types = result as List<Dictionary<string, object>>;
            types.Should().HaveCount(ns.Types.Count);
        }

        [TestMethod]
        public void SerializeTypes_Should_Include_Type_Properties()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var ns = assembly.Namespaces.First();
            var context = new ProjectContext { OutputPath = _testOutputPath };
            var renderer = new YamlRenderer(context);

            // Act
            var result = renderer.SerializeTypes(ns) as List<Dictionary<string, object>>;

            // Assert
            result.Should().NotBeNull();
            var firstType = result!.First();
            firstType.Should().ContainKey("name");
            firstType.Should().ContainKey("fullName");
            firstType.Should().ContainKey("kind");
            firstType.Should().ContainKey("baseType");
            firstType.Should().ContainKey("members");
        }

        #endregion

        #region SerializeMembers Tests

        [TestMethod]
        public void SerializeMembers_Should_Return_List_Of_Member_Dictionaries()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces.First().Types.First();
            var context = new ProjectContext { OutputPath = _testOutputPath };
            var renderer = new YamlRenderer(context);

            // Act
            var result = renderer.SerializeMembers(type);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<List<Dictionary<string, object>>>();
            var members = result as List<Dictionary<string, object>>;
            members.Should().HaveCount(type.Members.Count);
        }

        [TestMethod]
        public void SerializeMembers_Should_Include_Member_Properties()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces.First().Types.First();
            var context = new ProjectContext { OutputPath = _testOutputPath };
            var renderer = new YamlRenderer(context);

            // Act
            var result = renderer.SerializeMembers(type) as List<Dictionary<string, object>>;

            // Assert
            result.Should().NotBeNull();
            if (result != null && result.Any())
            {
                var firstMember = result.First();
                firstMember.Should().ContainKey("name");
                firstMember.Should().ContainKey("kind");
                firstMember.Should().ContainKey("accessibility");
                firstMember.Should().ContainKey("signature");
            }
        }

        #endregion

        #region SerializeParameters Tests

        [TestMethod]
        public void SerializeParameters_Should_Return_Null_When_No_Parameters()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces.First().Types.First();
            var memberWithoutParams = type.Members.FirstOrDefault(m => 
                m.Symbol.Kind == SymbolKind.Method && 
                (m.Symbol as IMethodSymbol)?.Parameters.Length == 0);
            var context = new ProjectContext { OutputPath = _testOutputPath };
            var renderer = new YamlRenderer(context);

            if (memberWithoutParams != null)
            {
                // Act
                var result = renderer.SerializeParameters(memberWithoutParams);
                
                // Assert
                result.Should().BeNull();
            }
        }

        [TestMethod]
        public void SerializeParameters_Should_Return_List_When_Parameters_Present()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithMethods");
            
            type.Should().NotBeNull("ClassWithMethods should exist in test assembly");
            
            var memberWithParams = type!.Members.FirstOrDefault(m => m.Symbol.Name == "Calculate");
            memberWithParams.Should().NotBeNull("Calculate method should exist in ClassWithMethods");

            var context = new ProjectContext { OutputPath = _testOutputPath };
            var renderer = new YamlRenderer(context);

            // Act
            var result = renderer.SerializeParameters(memberWithParams!);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<List<Dictionary<string, object>>>();
            var parameters = result as List<Dictionary<string, object>>;
            parameters!.Should().HaveCount(2); // Calculate method has 2 parameters
        }

        [TestMethod]
        public void SerializeParameters_Should_Include_Parameter_Properties()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithMethods");
            
            type.Should().NotBeNull("ClassWithMethods should exist in test assembly");
            
            var memberWithParams = type.Members.FirstOrDefault(m => m.Symbol.Name == "Calculate");
            memberWithParams.Should().NotBeNull("Calculate method should exist in ClassWithMethods");

            var context = new ProjectContext { OutputPath = _testOutputPath };
            var renderer = new YamlRenderer(context);

            // Act
            var result = renderer.SerializeParameters(memberWithParams) as List<Dictionary<string, object>>;

            // Assert
            result.Should().NotBeNull();
            var firstParam = result!.First();
            firstParam.Should().ContainKey("name");
            firstParam.Should().ContainKey("type");
            firstParam.Should().ContainKey("isOptional");
            firstParam.Should().ContainKey("usage");
        }

        #endregion

        #region RenderNamespaceFileAsync Tests

        [TestMethod]
        public async Task RenderNamespaceFileAsync_Should_Create_Namespace_File()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var ns = assembly.Namespaces.First();
            var context = new ProjectContext { OutputPath = _testOutputPath };
            var renderer = new YamlRenderer(context);

            // Act
            await renderer.RenderNamespaceFileAsync(ns, _testOutputPath);

            // Assert
            var expectedFileName = renderer.GetNamespaceFileName(ns, "yaml");
            var nsPath = Path.Combine(_testOutputPath, expectedFileName);
            File.Exists(nsPath).Should().BeTrue();
        }

        [TestMethod]
        public async Task RenderNamespaceFileAsync_Should_Include_Namespace_Data()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var ns = assembly.Namespaces.First();
            var context = new ProjectContext { OutputPath = _testOutputPath };
            var renderer = new YamlRenderer(context);
            var camelCaseDeserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            // Act
            await renderer.RenderNamespaceFileAsync(ns, _testOutputPath);

            // Assert
            var expectedFileName = renderer.GetNamespaceFileName(ns, "yaml");
            var nsPath = Path.Combine(_testOutputPath, expectedFileName);
            var content = await File.ReadAllTextAsync(nsPath);
            var data = camelCaseDeserializer.Deserialize<Dictionary<string, object>>(content);

            data.Should().ContainKey("namespace");
            var namespaceData = data["namespace"] as Dictionary<object, object>;
            namespaceData.Should().NotBeNull();
            namespaceData!.Should().ContainKey("name");
            var expectedName = ns.Symbol.IsGlobalNamespace ? "global" : ns.Symbol.ToDisplayString();
            namespaceData!["name"].Should().Be(expectedName);
        }

        #endregion

        #region RenderTableOfContentsAsync Tests

        [TestMethod]
        public async Task RenderTableOfContentsAsync_Should_Create_TOC_File()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var context = new ProjectContext { OutputPath = _testOutputPath };
            var renderer = new YamlRenderer(context);

            // Act
            await renderer.RenderTableOfContentsAsync(assembly, _testOutputPath);

            // Assert
            var tocPath = Path.Combine(_testOutputPath, "toc.yaml");
            File.Exists(tocPath).Should().BeTrue();
        }

        [TestMethod]
        public async Task RenderTableOfContentsAsync_Should_Include_All_Namespaces()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var context = new ProjectContext { OutputPath = _testOutputPath };
            var renderer = new YamlRenderer(context);

            // Act
            await renderer.RenderTableOfContentsAsync(assembly, _testOutputPath);

            // Assert
            var tocPath = Path.Combine(_testOutputPath, "toc.yaml");
            var content = await File.ReadAllTextAsync(tocPath);
            var data = _yamlDeserializer.Deserialize<Dictionary<string, object>>(content);

            data.Should().ContainKey("items");
            var items = data["items"] as List<object>;
            items.Should().NotBeNull();
            items!.Should().HaveCount(assembly.Namespaces.Count);
        }

        #endregion

        #region GetReturnType Tests

        [TestMethod]
        public void GetReturnType_Should_Return_Method_ReturnType()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithMethods");
            
            type.Should().NotBeNull("ClassWithMethods should exist in test assembly");
            
            var method = type!.Members.FirstOrDefault(m => m.Symbol.Name == "Calculate");
            method.Should().NotBeNull("Calculate method should exist in ClassWithMethods");

            var context = new ProjectContext { OutputPath = _testOutputPath };
            var renderer = new YamlRenderer(context);

            // Act
            var result = renderer.GetReturnType(method!);

            // Assert
            result.Should().Be("int");
        }

        [TestMethod]
        public void GetReturnType_Should_Return_Property_Type()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithProperties");
            
            type.Should().NotBeNull("ClassWithProperties should exist in test assembly");
            
            var property = type!.Members.FirstOrDefault(m => m.Symbol.Kind == SymbolKind.Property && m.Symbol.Name == "Name");
            property.Should().NotBeNull("Name property should exist in ClassWithProperties");

            var context = new ProjectContext { OutputPath = _testOutputPath };
            var renderer = new YamlRenderer(context);

            // Act
            var result = renderer.GetReturnType(property!);

            // Assert
            result.Should().Be("string");
        }

        [TestMethod]
        public void GetReturnType_Should_Return_Null_For_Constructor()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces.First().Types.First();
            var constructor = type.Members.FirstOrDefault(m => 
                m.Symbol.Kind == SymbolKind.Method && 
                (m.Symbol as IMethodSymbol)?.MethodKind == MethodKind.Constructor);

            if (constructor != null)
            {
                var context = new ProjectContext { OutputPath = _testOutputPath };
                var renderer = new YamlRenderer(context);

                // Act
                var result = renderer.GetReturnType(constructor);
                
                // Assert
                result.Should().BeNull();
            }
        }

        #endregion

        #region GetModifiers Tests

        [TestMethod]
        public void GetModifiers_Should_Return_Method_Modifiers()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "BaseClass");
            
            type.Should().NotBeNull("BaseClass should exist in test assembly");
            
            var method = type!.Members.FirstOrDefault(m => m.Symbol.Name == "VirtualMethod");
            method.Should().NotBeNull("VirtualMethod should exist in BaseClass");

            var context = new ProjectContext { OutputPath = _testOutputPath };
            var renderer = new YamlRenderer(context);

            // Act
            var result = renderer.GetModifiers(method!);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("virtual");
        }

        [TestMethod]
        public void GetModifiers_Should_Return_Empty_List_For_Non_Modified_Members()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithMethods");
            
            type.Should().NotBeNull("ClassWithMethods should exist in test assembly");
            
            var method = type!.Members.FirstOrDefault(m => m.Symbol.Name == "Calculate");
            method.Should().NotBeNull("Calculate method should exist in ClassWithMethods");

            var context = new ProjectContext { OutputPath = _testOutputPath };
            var renderer = new YamlRenderer(context);

            // Act
            var result = renderer.GetModifiers(method!);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #endregion

    }

}