#nullable disable
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
using CloudNimble.DotNetDocs.Tests.Shared.Parameters;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YamlDotNet.Serialization;

namespace CloudNimble.DotNetDocs.Tests.Core
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
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

            // Act
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert - Compare against baseline
            var baselinePath = Path.Combine(projectPath, "Baselines", "YamlRenderer", "documentation.yaml");
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
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

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
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

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
            var model = await manager.DocumentAsync();
            model.Usage = "Test usage";
            model.Examples = "Test examples";
            model.BestPractices = "Test best practices";
            var context = new ProjectContext();

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
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

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
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

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
            var model = await manager.DocumentAsync();

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
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

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
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

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
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

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
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

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
            var model = await manager.DocumentAsync();
            model.BestPractices = "Test best practices";
            model.RelatedApis = new List<string> { "System.Object" };
            var context = new ProjectContext();

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
                var model = await manager.DocumentAsync();
                var context = new ProjectContext();

                await renderer.RenderAsync(model, tempOutputPath, context);

                // Create baselines directory
                var baselinesDir = Path.Combine(projectPath, "Baselines", "YamlRenderer");
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

    }

}