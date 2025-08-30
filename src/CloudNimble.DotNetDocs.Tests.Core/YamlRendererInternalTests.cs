using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Renderers;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Unit tests for internal methods of YamlRenderer.
    /// </summary>
    [TestClass]
    public class YamlRendererInternalTests : TestBase
    {

        #region Fields

        private YamlRenderer _renderer = null!;
        private ProjectContext _context = null!;
        private IDeserializer _yamlDeserializer = null!;

        #endregion

        #region Test Initialization

        [TestInitialize]
        public void Initialize()
        {
            _context = new ProjectContext
            {
                OutputPath = Path.Combine(Path.GetTempPath(), "YamlRendererTests", Guid.NewGuid().ToString())
            };
            _renderer = new YamlRenderer(_context);
            _yamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
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
        public void SerializeNamespaces_Should_Return_List_Of_Namespace_Dictionaries()
        {
            var assembly = CreateTestAssembly();

            var result = _renderer.SerializeNamespaces(assembly);

            result.Should().NotBeNull();
            result.Should().BeOfType<List<Dictionary<string, object>>>();
            var namespaces = result as List<Dictionary<string, object>>;
            namespaces.Should().HaveCount(assembly.Namespaces.Count);
        }

        [TestMethod]
        public void SerializeNamespaces_Should_Include_Namespace_Properties()
        {
            var assembly = CreateTestAssembly();

            var result = _renderer.SerializeNamespaces(assembly) as List<Dictionary<string, object>>;

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
            var assembly = CreateTestAssembly();
            var ns = assembly.Namespaces.First();

            var result = _renderer.SerializeTypes(ns);

            result.Should().NotBeNull();
            result.Should().BeOfType<List<Dictionary<string, object>>>();
            var types = result as List<Dictionary<string, object>>;
            types.Should().HaveCount(ns.Types.Count);
        }

        [TestMethod]
        public void SerializeTypes_Should_Include_Type_Properties()
        {
            var assembly = CreateTestAssembly();
            var ns = assembly.Namespaces.First();

            var result = _renderer.SerializeTypes(ns) as List<Dictionary<string, object>>;

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
            var assembly = CreateTestAssembly();
            var type = assembly.Namespaces.First().Types.First();

            var result = _renderer.SerializeMembers(type);

            result.Should().NotBeNull();
            result.Should().BeOfType<List<Dictionary<string, object>>>();
            var members = result as List<Dictionary<string, object>>;
            members.Should().HaveCount(type.Members.Count);
        }

        [TestMethod]
        public void SerializeMembers_Should_Include_Member_Properties()
        {
            var assembly = CreateTestAssembly();
            var type = assembly.Namespaces.First().Types.First();

            var result = _renderer.SerializeMembers(type) as List<Dictionary<string, object>>;

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
        public void SerializeParameters_Should_Return_List_When_Parameters_Present()
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
            result.Should().BeOfType<List<Dictionary<string, object>>>();
            var parameters = result as List<Dictionary<string, object>>;
            parameters!.Should().HaveCount(2); // Calculate method has 2 parameters
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

            var result = _renderer.SerializeParameters(memberWithParams) as List<Dictionary<string, object>>;

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
            var assembly = CreateTestAssembly();
            var ns = assembly.Namespaces.First();

            await _renderer.RenderNamespaceFileAsync(ns, _context.OutputPath);

            var expectedFileName = _renderer.GetNamespaceFileName(ns, "yaml");
            var nsPath = Path.Combine(_context.OutputPath, expectedFileName);
            File.Exists(nsPath).Should().BeTrue();
        }

        [TestMethod]
        public async Task RenderNamespaceFileAsync_Should_Include_Namespace_Data()
        {
            var assembly = CreateTestAssembly();
            var ns = assembly.Namespaces.First();

            await _renderer.RenderNamespaceFileAsync(ns, _context.OutputPath);

            var expectedFileName = _renderer.GetNamespaceFileName(ns, "yaml");
            var nsPath = Path.Combine(_context.OutputPath, expectedFileName);
            var content = await File.ReadAllTextAsync(nsPath);
            var data = _yamlDeserializer.Deserialize<Dictionary<string, object>>(content);

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
            var assembly = CreateTestAssembly();

            await _renderer.RenderTableOfContentsAsync(assembly, _context.OutputPath);

            var tocPath = Path.Combine(_context.OutputPath, "toc.yaml");
            File.Exists(tocPath).Should().BeTrue();
        }

        [TestMethod]
        public async Task RenderTableOfContentsAsync_Should_Include_All_Namespaces()
        {
            var assembly = CreateTestAssembly();

            await _renderer.RenderTableOfContentsAsync(assembly, _context.OutputPath);

            var tocPath = Path.Combine(_context.OutputPath, "toc.yaml");
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

        #endregion

        #region GetModifiers Tests

        [TestMethod]
        public void GetModifiers_Should_Return_Method_Modifiers()
        {
            var assembly = CreateTestAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "BaseClass");
            
            if (type == null)
            {
                Assert.Inconclusive("BaseClass not found in test assembly");
                return;
            }
            
            var method = type.Members.FirstOrDefault(m => m.Symbol.Name == "VirtualMethod");
            if (method == null)
            {
                Assert.Inconclusive("VirtualMethod not found");
                return;
            }

            var result = _renderer.GetModifiers(method);

            result.Should().NotBeNull();
            result.Should().Contain("virtual");
        }

        [TestMethod]
        public void GetModifiers_Should_Return_Empty_List_For_Non_Modified_Members()
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

            var result = _renderer.GetModifiers(method);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region Helper Methods

        private DocAssembly CreateTestAssembly()
        {
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var manager = new AssemblyManager(assemblyPath, xmlPath);
            return manager.DocumentAsync().GetAwaiter().GetResult();
        }

        #endregion

    }

}