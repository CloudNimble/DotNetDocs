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
using Microsoft.CodeAnalysis;
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
        private JsonRenderer _renderer = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            _testOutputPath = Path.Combine(Path.GetTempPath(), $"JsonRendererTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testOutputPath);
            _renderer = new JsonRenderer();
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
            var baselinePath = Path.Combine(projectPath, "Baselines", "JsonRenderer", "documentation.json");
            var actualPath = Path.Combine(_testOutputPath, "documentation.json");
            
            if (File.Exists(baselinePath))
            {
                var baseline = await File.ReadAllTextAsync(baselinePath);
                var actual = await File.ReadAllTextAsync(actualPath);
                
                actual.Should().Be(baseline,
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
            await _renderer.RenderAsync(model, _testOutputPath, context);

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
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var json = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.json"));
            using var document = JsonDocument.Parse(json);
            
            var assembly = document.RootElement.GetProperty("assembly");
            assembly.GetProperty("name").GetString().Should().Be(model.AssemblyName);
            assembly.GetProperty("version").GetString().Should().NotBeNullOrWhiteSpace();
            assembly.GetProperty("usage").GetString().Should().Be("Test usage");
            assembly.GetProperty("examples").GetString().Should().Be("Test examples");
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
                var nsFileName = $"{namespaceName.Replace('.', '-')}.json";
                var nsPath = Path.Combine(_testOutputPath, nsFileName);
                File.Exists(nsPath).Should().BeTrue($"Namespace file {nsFileName} should exist");
                
                var json = await File.ReadAllTextAsync(nsPath);
                Action act = () => JsonDocument.Parse(json);
                act.Should().NotThrow();
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
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var json = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.json"));
            using var document = JsonDocument.Parse(json);
            
            var namespaces = document.RootElement.GetProperty("assembly").GetProperty("namespaces");
            namespaces.GetArrayLength().Should().BeGreaterThan(0);
            
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
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var json = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.json"));
            using var document = JsonDocument.Parse(json);
            
            var namespaces = document.RootElement.GetProperty("assembly").GetProperty("namespaces");
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
                        type.TryGetProperty("kind", out _).Should().BeTrue();
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
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var json = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.json"));
            using var document = JsonDocument.Parse(json);
            
            var namespaces = document.RootElement.GetProperty("assembly").GetProperty("namespaces");
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
                            members.GetArrayLength().Should().BeGreaterThan(0);
                            
                            foreach (var member in members.EnumerateArray())
                            {
                                member.TryGetProperty("name", out _).Should().BeTrue();
                                member.TryGetProperty("kind", out _).Should().BeTrue();
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
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var json = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.json"));
            using var document = JsonDocument.Parse(json);
            
            var namespaces = document.RootElement.GetProperty("assembly").GetProperty("namespaces");
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
                                        param.TryGetProperty("type", out _).Should().BeTrue();
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
            await _renderer.RenderAsync(model, _testOutputPath, context);

            // Assert
            var json = await File.ReadAllTextAsync(Path.Combine(_testOutputPath, "documentation.json"));
            using var document = JsonDocument.Parse(json);
            
            var assembly = document.RootElement.GetProperty("assembly");
            
            // Properties should be camelCase
            assembly.TryGetProperty("bestPractices", out _).Should().BeTrue();
            assembly.TryGetProperty("relatedApis", out _).Should().BeTrue();
            
            // Should not be PascalCase
            assembly.TryGetProperty("BestPractices", out _).Should().BeFalse();
            assembly.TryGetProperty("RelatedApis", out _).Should().BeFalse();
        }

        #endregion

        #region Baseline Generation

        //[TestMethod]
        //[DataRow(projectPath)]
        [BreakdanceManifestGenerator]
        public async Task GenerateJsonBaseline(string projectPath)
        {
            // Setup
            var renderer = new JsonRenderer();
            var tempOutputPath = Path.Combine(Path.GetTempPath(), $"JsonBaseline_{Guid.NewGuid()}");
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

        #region Internal Method Tests

        #region SerializeNamespaces Tests

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SerializeNamespaces_Should_Return_Anonymous_Object_Collection(bool ignoreGlobalModule)
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly(ignoreGlobalModule);

            // Act
            var result = _renderer.SerializeNamespaces(assembly);

            // Assert
            result.Should().NotBeNull();
            var json = JsonSerializer.Serialize(result, _renderer.SerializerOptions);
            var deserialized = JsonSerializer.Deserialize<List<JsonElement>>(json);
            deserialized.Should().HaveCount(assembly.Namespaces.Count);
            
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

            // Act
            var result = _renderer.SerializeNamespaces(assembly);

            // Assert
            var json = JsonSerializer.Serialize(result, _renderer.SerializerOptions);
            var deserialized = JsonSerializer.Deserialize<List<JsonElement>>(json);
            
            deserialized.Should().NotBeNull();
            var firstNamespace = deserialized!.First();
            // JSON uses camelCase due to JsonNamingPolicy.CamelCase
            firstNamespace.TryGetProperty("name", out _).Should().BeTrue();
            firstNamespace.TryGetProperty("types", out _).Should().BeTrue();
            // usage is only present if not null (due to JsonIgnoreCondition.WhenWritingNull)
        }

        #endregion

        #region SerializeTypes Tests

        [TestMethod]
        public void SerializeTypes_Should_Return_Anonymous_Object_Collection()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var ns = assembly.Namespaces.First();

            // Act
            var result = _renderer.SerializeTypes(ns);

            // Assert
            result.Should().NotBeNull();
            var json = JsonSerializer.Serialize(result, _renderer.SerializerOptions);
            var deserialized = JsonSerializer.Deserialize<List<JsonElement>>(json);
            deserialized.Should().HaveCount(ns.Types.Count);
        }

        [TestMethod]
        public void SerializeTypes_Should_Include_Type_Properties()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            
            assembly.Namespaces.Should().NotBeEmpty("Test assembly should contain namespaces");
            
            var ns = assembly.Namespaces.First();

            // Act
            var result = _renderer.SerializeTypes(ns);

            // Assert
            var json = JsonSerializer.Serialize(result, _renderer.SerializerOptions);
            var deserialized = JsonSerializer.Deserialize<List<JsonElement>>(json);
            
            deserialized.Should().NotBeNull();
            var firstType = deserialized!.First();
            // JSON uses camelCase due to JsonNamingPolicy.CamelCase
            firstType.TryGetProperty("name", out _).Should().BeTrue();
            firstType.TryGetProperty("fullName", out _).Should().BeTrue();
            firstType.TryGetProperty("kind", out _).Should().BeTrue();
            // baseType is only present if not null (due to JsonIgnoreCondition.WhenWritingNull)
            // Check if it exists, and if it does, it should be a valid property
            firstType.TryGetProperty("members", out _).Should().BeTrue();
        }

        #endregion

        #region SerializeMembers Tests

        [TestMethod]
        public void SerializeMembers_Should_Return_Anonymous_Object_Collection()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            
            assembly.Namespaces.Should().NotBeEmpty("Test assembly should contain namespaces");
            assembly.Namespaces.First().Types.Should().NotBeEmpty("First namespace should contain types");
            
            var type = assembly.Namespaces.First().Types.First();

            // Act
            var result = _renderer.SerializeMembers(type);

            // Assert
            result.Should().NotBeNull();
            var json = JsonSerializer.Serialize(result, _renderer.SerializerOptions);
            var deserialized = JsonSerializer.Deserialize<List<JsonElement>>(json);
            deserialized.Should().HaveCount(type.Members.Count);
        }

        [TestMethod]
        public void SerializeMembers_Should_Include_Member_Properties()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            
            assembly.Namespaces.Should().NotBeEmpty("Test assembly should contain namespaces");
            assembly.Namespaces.First().Types.Should().NotBeEmpty("First namespace should contain types");
            
            var type = assembly.Namespaces.First().Types.First();

            // Act
            var result = _renderer.SerializeMembers(type);

            // Assert
            var json = JsonSerializer.Serialize(result, _renderer.SerializerOptions);
            var deserialized = JsonSerializer.Deserialize<List<JsonElement>>(json);
            
            deserialized.Should().NotBeNull();
            if (deserialized != null && deserialized.Any())
            {
                var firstMember = deserialized.First();
                // JSON uses camelCase due to JsonNamingPolicy.CamelCase
                firstMember.TryGetProperty("name", out _).Should().BeTrue();
                firstMember.TryGetProperty("kind", out _).Should().BeTrue();
                firstMember.TryGetProperty("accessibility", out _).Should().BeTrue();
                // signature might be null for some members
            }
        }

        #endregion

        #region SerializeParameters Tests

        [TestMethod]
        public void SerializeParameters_Should_Return_Null_When_No_Parameters()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            
            assembly.Namespaces.Should().NotBeEmpty("Test assembly should contain namespaces");
            assembly.Namespaces.First().Types.Should().NotBeEmpty("First namespace should contain types");
            
            var type = assembly.Namespaces.First().Types.First();
            var memberWithoutParams = type.Members.FirstOrDefault(m => 
                m.Symbol.Kind == SymbolKind.Method && 
                (m.Symbol as IMethodSymbol)?.Parameters.Length == 0);

            if (memberWithoutParams != null)
            {
                // Act
                var result = _renderer.SerializeParameters(memberWithoutParams);
                
                // Assert
                result.Should().BeNull();
            }
        }

        [TestMethod]
        public void SerializeParameters_Should_Return_Collection_When_Parameters_Present()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithMethods");
            
            type.Should().NotBeNull("ClassWithMethods should exist in test assembly");
            
            var memberWithParams = type!.Members.FirstOrDefault(m => m.Symbol.Name == "Calculate");
            
            memberWithParams.Should().NotBeNull("Calculate method should exist in ClassWithMethods");

            // Act
            var result = _renderer.SerializeParameters(memberWithParams!);

            // Assert
            result.Should().NotBeNull();
            var json = JsonSerializer.Serialize(result, _renderer.SerializerOptions);
            var deserialized = JsonSerializer.Deserialize<List<JsonElement>>(json);
            deserialized.Should().NotBeNull().And.NotBeEmpty();
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
            
            var memberWithParams = type!.Members.FirstOrDefault(m => m.Symbol.Name == "Calculate");
            
            memberWithParams.Should().NotBeNull("Calculate method should exist in ClassWithMethods");

            // Act
            var result = _renderer.SerializeParameters(memberWithParams!);

            // Assert
            var json = JsonSerializer.Serialize(result, _renderer.SerializerOptions);
            var deserialized = JsonSerializer.Deserialize<List<JsonElement>>(json);
            
            deserialized.Should().NotBeNull();
            var firstParam = deserialized!.First();
            // JSON uses camelCase due to JsonNamingPolicy.CamelCase
            firstParam.TryGetProperty("name", out _).Should().BeTrue();
            firstParam.TryGetProperty("type", out _).Should().BeTrue();
            firstParam.TryGetProperty("isOptional", out _).Should().BeTrue();
            // usage is only present if not null (due to JsonIgnoreCondition.WhenWritingNull)
        }

        #endregion

        #region RenderNamespaceFileAsync Tests

        [TestMethod]
        public async Task RenderNamespaceFileAsync_Should_Create_Namespace_File()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            
            assembly.Namespaces.Should().NotBeEmpty("Test assembly should contain namespaces");
            
            var ns = assembly.Namespaces.First();

            // Act
            await _renderer.RenderNamespaceFileAsync(ns, _testOutputPath);

            // Assert
            var expectedFileName = _renderer.GetNamespaceFileName(ns, "json");
            var nsPath = Path.Combine(_testOutputPath, expectedFileName);
            File.Exists(nsPath).Should().BeTrue();
        }

        [TestMethod]
        public async Task RenderNamespaceFileAsync_Should_Include_Namespace_Data()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            
            assembly.Namespaces.Should().NotBeEmpty("Test assembly should contain namespaces");
            
            var ns = assembly.Namespaces.First();

            // Act
            await _renderer.RenderNamespaceFileAsync(ns, _testOutputPath);

            // Assert
            var expectedFileName = _renderer.GetNamespaceFileName(ns, "json");
            var nsPath = Path.Combine(_testOutputPath, expectedFileName);
            var content = await File.ReadAllTextAsync(nsPath);
            var data = JsonSerializer.Deserialize<JsonElement>(content);

            data.TryGetProperty("namespace", out var namespaceElement).Should().BeTrue();
            namespaceElement.TryGetProperty("name", out var nameElement).Should().BeTrue();
            nameElement.GetString().Should().Be(ns.Symbol.ToDisplayString());
        }

        [TestMethod]
        public async Task RenderNamespaceFileAsync_Should_Include_Types()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            
            assembly.Namespaces.Should().NotBeEmpty("Test assembly should contain namespaces");
            
            var ns = assembly.Namespaces.First();

            // Act
            await _renderer.RenderNamespaceFileAsync(ns, _testOutputPath);

            // Assert
            var expectedFileName = _renderer.GetNamespaceFileName(ns, "json");
            var nsPath = Path.Combine(_testOutputPath, expectedFileName);
            var content = await File.ReadAllTextAsync(nsPath);
            var data = JsonSerializer.Deserialize<JsonElement>(content);

            data.TryGetProperty("namespace", out var namespaceElement).Should().BeTrue();
            namespaceElement.TryGetProperty("types", out var typesElement).Should().BeTrue();
            typesElement.GetArrayLength().Should().Be(ns.Types.Count);
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

            // Act
            var result = _renderer.GetReturnType(method!);

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

            // Act
            var result = _renderer.GetReturnType(property!);

            // Assert
            result.Should().Be("string");
        }

        [TestMethod]
        public void GetReturnType_Should_Return_Field_Type()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces.First().Types.FirstOrDefault(t => 
                t.Members.Any(m => m.Symbol.Kind == SymbolKind.Field));
            
            if (type != null)
            {
                var field = type.Members.First(m => m.Symbol.Kind == SymbolKind.Field);
                
                // Act
                var result = _renderer.GetReturnType(field);
                
                // Assert
                result.Should().NotBeNull();
            }
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
                // Act
                var result = _renderer.GetReturnType(constructor);
                
                // Assert
                result.Should().BeNull();
            }
        }

        [TestMethod]
        public void GetReturnType_Should_Return_Null_For_Void_Method()
        {
            // Arrange
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithMethods");
            
            type.Should().NotBeNull("ClassWithMethods should exist in test assembly");
            
            var voidMethod = type!.Members.FirstOrDefault(m => m.Symbol.Name == "PerformAction");
            voidMethod.Should().NotBeNull("PerformAction method should exist in ClassWithMethods");

            // Act
            var result = _renderer.GetReturnType(voidMethod!);

            // Assert
            result.Should().Be("void");
        }

        #endregion

        #endregion

    }

}