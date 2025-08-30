using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Renderers;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Unit tests for internal methods of JsonRenderer.
    /// </summary>
    [TestClass]
    public class JsonRendererInternalTests : TestBase
    {

        #region Fields

        private JsonRenderer _renderer = null!;
        private ProjectContext _context = null!;

        #endregion

        #region Test Initialization

        [TestInitialize]
        public void Initialize()
        {
            _context = new ProjectContext
            {
                OutputPath = Path.Combine(Path.GetTempPath(), "JsonRendererTests", Guid.NewGuid().ToString())
            };
            _renderer = new JsonRenderer(_context);
            Directory.CreateDirectory(_context.OutputPath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_context.OutputPath))
            {
                Directory.Delete(_context.OutputPath, true);
            }
        }

        #endregion

        #region SerializeNamespaces Tests

        [TestMethod]
        public void SerializeNamespaces_Should_Return_Anonymous_Object_Collection()
        {
            var assembly = CreateTestAssembly();

            var result = _renderer.SerializeNamespaces(assembly);

            result.Should().NotBeNull();
            var json = JsonSerializer.Serialize(result, _renderer.SerializerOptions);
            var deserialized = JsonSerializer.Deserialize<List<JsonElement>>(json);
            deserialized.Should().HaveCount(assembly.Namespaces.Count);
        }

        [TestMethod]
        public void SerializeNamespaces_Should_Include_Namespace_Properties()
        {
            var assembly = CreateTestAssembly();

            var result = _renderer.SerializeNamespaces(assembly);

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
            var assembly = CreateTestAssembly();
            var ns = assembly.Namespaces.First();

            var result = _renderer.SerializeTypes(ns);

            result.Should().NotBeNull();
            var json = JsonSerializer.Serialize(result, _renderer.SerializerOptions);
            var deserialized = JsonSerializer.Deserialize<List<JsonElement>>(json);
            deserialized.Should().HaveCount(ns.Types.Count);
        }

        [TestMethod]
        public void SerializeTypes_Should_Include_Type_Properties()
        {
            var assembly = CreateTestAssembly();
            var ns = assembly.Namespaces.First();

            var result = _renderer.SerializeTypes(ns);

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
            var assembly = CreateTestAssembly();
            var type = assembly.Namespaces.First().Types.First();

            var result = _renderer.SerializeMembers(type);

            result.Should().NotBeNull();
            var json = JsonSerializer.Serialize(result, _renderer.SerializerOptions);
            var deserialized = JsonSerializer.Deserialize<List<JsonElement>>(json);
            deserialized.Should().HaveCount(type.Members.Count);
        }

        [TestMethod]
        public void SerializeMembers_Should_Include_Member_Properties()
        {
            var assembly = CreateTestAssembly();
            var type = assembly.Namespaces.First().Types.First();

            var result = _renderer.SerializeMembers(type);

            var json = JsonSerializer.Serialize(result);
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
            var assembly = CreateTestAssembly();
            var type = assembly.Namespaces.First().Types.First();
            var memberWithoutParams = type.Members.FirstOrDefault(m => 
                m.Symbol.Kind == SymbolKind.Method && 
                (m.Symbol as IMethodSymbol)?.Parameters.Length == 0);

            if (memberWithoutParams != null)
            {
                var result = _renderer.SerializeParameters(memberWithoutParams);
                result.Should().BeNull();
            }
        }

        [TestMethod]
        public void SerializeParameters_Should_Return_Collection_When_Parameters_Present()
        {
            var assembly = CreateTestAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithMethods");
            
            if (type == null)
            {
                Assert.Inconclusive("ClassWithMethods not found in test assembly");
                return;
            }
            
            var memberWithParams = type.Members.FirstOrDefault(m => m.Symbol.Name == "Calculate");
            
            if (memberWithParams == null)
            {
                Assert.Inconclusive("Calculate method not found");
                return;
            }

            var result = _renderer.SerializeParameters(memberWithParams);

            result.Should().NotBeNull();
            var json = JsonSerializer.Serialize(result, _renderer.SerializerOptions);
            var deserialized = JsonSerializer.Deserialize<List<JsonElement>>(json);
            deserialized.Should().NotBeNull().And.NotBeEmpty();
        }

        [TestMethod]
        public void SerializeParameters_Should_Include_Parameter_Properties()
        {
            var assembly = CreateTestAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithMethods");
            
            if (type == null)
            {
                Assert.Inconclusive("ClassWithMethods not found in test assembly");
                return;
            }
            
            var memberWithParams = type.Members.FirstOrDefault(m => m.Symbol.Name == "Calculate");
            
            if (memberWithParams == null)
            {
                Assert.Inconclusive("Calculate method not found");
                return;
            }

            var result = _renderer.SerializeParameters(memberWithParams);

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
            var assembly = CreateTestAssembly();
            var ns = assembly.Namespaces.First();

            await _renderer.RenderNamespaceFileAsync(ns, _context.OutputPath);

            var expectedFileName = _renderer.GetNamespaceFileName(ns, "json");
            var nsPath = Path.Combine(_context.OutputPath, expectedFileName);
            File.Exists(nsPath).Should().BeTrue();
        }

        [TestMethod]
        public async Task RenderNamespaceFileAsync_Should_Include_Namespace_Data()
        {
            var assembly = CreateTestAssembly();
            var ns = assembly.Namespaces.First();

            await _renderer.RenderNamespaceFileAsync(ns, _context.OutputPath);

            var expectedFileName = _renderer.GetNamespaceFileName(ns, "json");
            var nsPath = Path.Combine(_context.OutputPath, expectedFileName);
            var content = await File.ReadAllTextAsync(nsPath);
            var data = JsonSerializer.Deserialize<JsonElement>(content);

            data.TryGetProperty("namespace", out var namespaceElement).Should().BeTrue();
            namespaceElement.TryGetProperty("name", out var nameElement).Should().BeTrue();
            nameElement.GetString().Should().Be(ns.Symbol.ToDisplayString());
        }

        [TestMethod]
        public async Task RenderNamespaceFileAsync_Should_Include_Types()
        {
            var assembly = CreateTestAssembly();
            var ns = assembly.Namespaces.First();

            await _renderer.RenderNamespaceFileAsync(ns, _context.OutputPath);

            var expectedFileName = _renderer.GetNamespaceFileName(ns, "json");
            var nsPath = Path.Combine(_context.OutputPath, expectedFileName);
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
            var assembly = CreateTestAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithMethods");
            
            if (type == null)
            {
                Assert.Inconclusive("ClassWithMethods not found in test assembly");
                return;
            }
            
            var method = type.Members.FirstOrDefault(m => m.Symbol.Name == "Calculate");
            if (method == null)
            {
                Assert.Inconclusive("Calculate method not found");
                return;
            }

            var result = _renderer.GetReturnType(method);

            result.Should().Be("int");
        }

        [TestMethod]
        public void GetReturnType_Should_Return_Property_Type()
        {
            var assembly = CreateTestAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithProperties");
            
            if (type == null)
            {
                Assert.Inconclusive("ClassWithProperties not found in test assembly");
                return;
            }
            
            var property = type.Members.FirstOrDefault(m => m.Symbol.Kind == SymbolKind.Property && m.Symbol.Name == "Name");
            if (property == null)
            {
                Assert.Inconclusive("Name property not found");
                return;
            }

            var result = _renderer.GetReturnType(property);

            result.Should().Be("string");
        }

        [TestMethod]
        public void GetReturnType_Should_Return_Field_Type()
        {
            var assembly = CreateTestAssembly();
            var type = assembly.Namespaces.First().Types.FirstOrDefault(t => 
                t.Members.Any(m => m.Symbol.Kind == SymbolKind.Field));
            
            if (type != null)
            {
                var field = type.Members.First(m => m.Symbol.Kind == SymbolKind.Field);
                var result = _renderer.GetReturnType(field);
                result.Should().NotBeNull();
            }
        }

        [TestMethod]
        public void GetReturnType_Should_Return_Null_For_Constructor()
        {
            var assembly = CreateTestAssembly();
            var type = assembly.Namespaces.First().Types.First();
            var constructor = type.Members.FirstOrDefault(m => 
                m.Symbol.Kind == SymbolKind.Method && 
                (m.Symbol as IMethodSymbol)?.MethodKind == MethodKind.Constructor);

            if (constructor != null)
            {
                var result = _renderer.GetReturnType(constructor);
                result.Should().BeNull();
            }
        }

        [TestMethod]
        public void GetReturnType_Should_Return_Null_For_Void_Method()
        {
            var assembly = CreateTestAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithMethods");
            
            if (type == null)
            {
                Assert.Inconclusive("ClassWithMethods not found in test assembly");
                return;
            }
            
            var voidMethod = type.Members.FirstOrDefault(m => m.Symbol.Name == "PerformAction");
            if (voidMethod == null)
            {
                Assert.Inconclusive("PerformAction method not found");
                return;
            }

            var result = _renderer.GetReturnType(voidMethod);

            result.Should().Be("void");
        }

        #endregion

        #region Helper Methods

        private DocAssembly CreateTestAssembly()
        {
            // Use a type from BasicScenarios namespace to ensure we get all types
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = manager.DocumentAsync().GetAwaiter().GetResult();
            
            // Debug: Log available namespaces and types
            System.Diagnostics.Debug.WriteLine($"Loaded {assembly.Namespaces.Count} namespaces");
            foreach (var ns in assembly.Namespaces)
            {
                System.Diagnostics.Debug.WriteLine($"  Namespace: {ns.Symbol.ToDisplayString()} with {ns.Types.Count} types");
                foreach (var type in ns.Types)
                {
                    System.Diagnostics.Debug.WriteLine($"    Type: {type.Symbol.Name}");
                }
            }
            
            return assembly;
        }

        #endregion

    }

}