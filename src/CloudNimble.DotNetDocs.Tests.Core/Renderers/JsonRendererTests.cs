using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Assemblies;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Renderers;
using CloudNimble.DotNetDocs.Tests.Shared;
using CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios;
using CloudNimble.DotNetDocs.Tests.Shared.Parameters;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core.Renderers
{

    /// <summary>
    /// Tests for the JsonRenderer class.
    /// </summary>
    [TestClass]
    public class JsonRendererTests : DotNetDocsTestBase
    {

        #region Fields

        private string _testOutputPath = null!;

        #endregion

        #region Helper Methods

        private JsonRenderer GetJsonRenderer()
        {
            var renderer = GetServices<IDocRenderer>()
                .OfType<JsonRenderer>()
                .FirstOrDefault();
            renderer.Should().NotBeNull("JsonRenderer should be registered in DI");
            return renderer!;
        }

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            _testOutputPath = Path.Combine(Path.GetTempPath(), $"JsonRendererTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testOutputPath);

            // Configure services for DI
            TestHostBuilder.ConfigureServices((context, services) =>
            {
                services.AddDotNetDocsCore();
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
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

            // Act
            await GetJsonRenderer().RenderAsync(model);

            // Assert - Compare against baseline
            var baselinePath = Path.Combine(projectPath, "Baselines", "JsonRenderer", "documentation.json");
            var actualPath = Path.Combine(_testOutputPath, "documentation.json");
            
            if (File.Exists(baselinePath))
            {
                var baseline = await File.ReadAllTextAsync(baselinePath, TestContext.CancellationTokenSource.Token);
                var actual = await File.ReadAllTextAsync(actualPath, TestContext.CancellationTokenSource.Token);
                
                // Normalize line endings for cross-platform compatibility
                var normalizedActual = actual.ReplaceLineEndings(Environment.NewLine);
                var normalizedBaseline = baseline.ReplaceLineEndings(Environment.NewLine);
                
                normalizedActual.Should().Be(normalizedBaseline,
                    "JSON output has changed. If this is intentional, regenerate baselines using 'dotnet breakdance generate'");
            }
            else
            {
                Assert.Inconclusive($"Baseline not found at {baselinePath}. Run 'dotnet breakdance generate' to create baselines.");
            }
        }

        [TestMethod]
        public async Task RenderAsync_ProducesValidJson()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

            // Act
            await GetJsonRenderer().RenderAsync(model);

            // Assert
            var jsonPath = Path.Combine(_testOutputPath, "documentation.json");
            var json = await File.ReadAllTextAsync(jsonPath);
            
            Action act = () => JsonDocument.Parse(json);
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
            var context = new ProjectContext();

            // Act
            await GetJsonRenderer().RenderAsync(model);

            // Assert
            var json = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.json"), TestContext.CancellationTokenSource.Token);
            using var document = JsonDocument.Parse(json);
            
            // DocAssembly is serialized directly, so root element is the assembly
            var root = document.RootElement;
            root.GetProperty("assemblyName").GetString().Should().Be(model.AssemblyName);
            root.GetProperty("version").GetString().Should().NotBeNullOrWhiteSpace();
            root.GetProperty("usage").GetString().Should().Be("Test usage");
            root.GetProperty("examples").GetString().Should().Be("Test examples");
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
            await GetJsonRenderer().RenderAsync(model);

            // Assert
            foreach (var ns in model.Namespaces)
            {
                var fileName = GetJsonRenderer().GetNamespaceFileName(ns, "json");
                var nsPath = Path.Combine(_testOutputPath, fileName);
                File.Exists(nsPath).Should().BeTrue($"Namespace file {fileName} should exist");
                
                var json = await File.ReadAllTextAsync(nsPath, TestContext.CancellationTokenSource.Token);
                Action act = () => JsonDocument.Parse(json);
                act.Should().NotThrow();
            }
        }

        [TestMethod]
        public async Task RenderAsync_WithNullModel_ThrowsArgumentNullException()
        {
            // Arrange
            var renderer = GetJsonRenderer();

            // Act
            Func<Task> act = async () => await renderer.RenderAsync(null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        #endregion

        #region Content Structure Tests

        [TestMethod]
        public async Task RenderAsync_IncludesNamespaceHierarchy()
        {
            // Arrange
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

            // Act
            await GetJsonRenderer().RenderAsync(model);

            // Assert
            var json = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.json"));
            using var document = JsonDocument.Parse(json);
            
            var namespaces = document.RootElement.GetProperty("namespaces");
            namespaces.GetArrayLength().Should().BePositive();
            
            foreach (var ns in namespaces.EnumerateArray())
            {
                ns.TryGetProperty("name", out _).Should().BeTrue();
                ns.TryGetProperty("types", out _).Should().BeTrue();
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
            await GetJsonRenderer().RenderAsync(model);

            // Assert
            var json = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.json"), TestContext.CancellationTokenSource.Token);
            using var document = JsonDocument.Parse(json);
            
            var namespaces = document.RootElement.GetProperty("namespaces");
            var hasTypes = false;
            
            foreach (var ns in namespaces.EnumerateArray())
            {
                if (ns.TryGetProperty("types", out var types))
                {
                    foreach (var type in types.EnumerateArray())
                    {
                        hasTypes = true;
                        type.TryGetProperty("name", out _).Should().BeTrue();
                        type.TryGetProperty("fullName", out _).Should().BeTrue();
                        type.TryGetProperty("typeKind", out _).Should().BeTrue();
                    }
                }
            }
            
            hasTypes.Should().BeTrue("At least one type should be present");
        }

        [TestMethod]
        public async Task RenderAsync_IncludesMemberInformation()
        {
            // Arrange
            var assemblyPath = typeof(ClassWithProperties).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

            // Act
            await GetJsonRenderer().RenderAsync(model);

            // Assert
            var json = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.json"), TestContext.CancellationTokenSource.Token);
            using var document = JsonDocument.Parse(json);
            
            var namespaces = document.RootElement.GetProperty("namespaces");
            var hasMembers = false;
            
            foreach (var ns in namespaces.EnumerateArray())
            {
                if (ns.TryGetProperty("types", out var types))
                {
                    foreach (var type in types.EnumerateArray())
                    {
                        if (type.GetProperty("name").GetString() == "ClassWithProperties" && 
                            type.TryGetProperty("members", out var members))
                        {
                            hasMembers = true;
                            members.GetArrayLength().Should().BePositive();
                            
                            foreach (var member in members.EnumerateArray())
                            {
                                member.TryGetProperty("name", out _).Should().BeTrue();
                                member.TryGetProperty("memberKind", out _).Should().BeTrue();
                                member.TryGetProperty("accessibility", out _).Should().BeTrue();
                                member.TryGetProperty("signature", out _).Should().BeTrue();
                            }
                        }
                    }
                }
            }
            
            hasMembers.Should().BeTrue("ClassWithProperties should have members");
        }

        [TestMethod]
        public async Task RenderAsync_IncludesParameterInformation()
        {
            // Arrange
            var assemblyPath = typeof(ParameterVariations).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync();
            var context = new ProjectContext();

            // Act
            await GetJsonRenderer().RenderAsync(model);

            // Assert
            var json = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.json"));
            using var document = JsonDocument.Parse(json);
            
            var namespaces = document.RootElement.GetProperty("namespaces");
            var hasParameters = false;
            
            foreach (var ns in namespaces.EnumerateArray())
            {
                if (ns.TryGetProperty("types", out var types))
                {
                    foreach (var type in types.EnumerateArray())
                    {
                        if (type.TryGetProperty("members", out var members))
                        {
                            foreach (var member in members.EnumerateArray())
                            {
                                if (member.TryGetProperty("parameters", out var parameters) && 
                                    parameters.ValueKind == JsonValueKind.Array &&
                                    parameters.GetArrayLength() > 0)
                                {
                                    hasParameters = true;
                                    
                                    foreach (var param in parameters.EnumerateArray())
                                    {
                                        param.TryGetProperty("name", out _).Should().BeTrue();
                                        param.TryGetProperty("typeName", out _).Should().BeTrue();
                                        param.TryGetProperty("isOptional", out _).Should().BeTrue();
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
        public async Task RenderAsync_UsesProperJsonNamingConvention()
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
            await GetJsonRenderer().RenderAsync(model);

            // Assert
            var json = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.json"), TestContext.CancellationTokenSource.Token);
            using var document = JsonDocument.Parse(json);
            
            // DocAssembly is serialized directly at root level
            var root = document.RootElement;
            
            // Properties should be camelCase
            root.TryGetProperty("bestPractices", out _).Should().BeTrue();
            root.TryGetProperty("relatedApis", out _).Should().BeTrue();
            
            // Should not be PascalCase
            root.TryGetProperty("BestPractices", out _).Should().BeFalse();
            root.TryGetProperty("RelatedApis", out _).Should().BeFalse();
        }

        #endregion

        #region Baseline Generation

        [TestMethod]
        [DataRow(projectPath)]
        [BreakdanceManifestGenerator]
        public async Task GenerateJsonBaseline(string projectPath)
        {
            // Setup
            var renderer = GetJsonRenderer();
            var tempOutputPath = Path.Combine(Path.GetTempPath(), $"JsonBaseline_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempOutputPath);

            try
            {
                // Generate baseline for SimpleClass documentation
                var assemblyPath = typeof(SimpleClass).Assembly.Location;
                var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
                using var manager = new AssemblyManager(assemblyPath, xmlPath);
                var model = await manager.DocumentAsync();

                await renderer.RenderAsync(model);

                // Create baselines directory
                var baselinesDir = Path.Combine(projectPath, "Baselines", "JsonRenderer");
                if (!Directory.Exists(baselinesDir))
                {
                    Directory.CreateDirectory(baselinesDir);
                }

                // Copy JSON file to baseline
                var jsonFile = Path.Combine(tempOutputPath, "documentation.json");
                if (File.Exists(jsonFile))
                {
                    var content = await File.ReadAllTextAsync(jsonFile);
                    var baselinePath = Path.Combine(baselinesDir, "documentation.json");
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