using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Renderers;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Unit tests for internal methods of MarkdownRenderer.
    /// </summary>
    [TestClass]
    public class MarkdownRendererInternalTests : TestBase
    {

        #region Fields

        private MarkdownRenderer _renderer = null!;
        private ProjectContext _context = null!;

        #endregion

        #region Test Initialization

        [TestInitialize]
        public void Initialize()
        {
            _context = new ProjectContext
            {
                OutputPath = Path.Combine(Path.GetTempPath(), "MarkdownRendererTests", Guid.NewGuid().ToString())
            };
            _renderer = new MarkdownRenderer(_context);
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

        #region RenderAssemblyAsync Tests

        [TestMethod]
        public async Task RenderAssemblyAsync_Should_Create_Index_File()
        {
            var assembly = CreateTestAssembly();

            await _renderer.RenderAssemblyAsync(assembly, _context.OutputPath);

            var indexPath = Path.Combine(_context.OutputPath, "index.md");
            File.Exists(indexPath).Should().BeTrue();
        }

        [TestMethod]
        public async Task RenderAssemblyAsync_Should_Include_Assembly_Name()
        {
            var assembly = CreateTestAssembly();

            await _renderer.RenderAssemblyAsync(assembly, _context.OutputPath);

            var indexPath = Path.Combine(_context.OutputPath, "index.md");
            var content = await File.ReadAllTextAsync(indexPath);
            content.Should().Contain($"# {assembly.AssemblyName}");
        }

        [TestMethod]
        public async Task RenderAssemblyAsync_Should_Include_Usage_When_Present()
        {
            var assembly = CreateTestAssembly();
            assembly.Usage = "This is the assembly usage documentation.";

            await _renderer.RenderAssemblyAsync(assembly, _context.OutputPath);

            var content = await File.ReadAllTextAsync(Path.Combine(_context.OutputPath, "index.md"));
            content.Should().Contain("## Overview");
            content.Should().Contain(assembly.Usage);
        }

        [TestMethod]
        public async Task RenderAssemblyAsync_Should_List_Namespaces()
        {
            var assembly = CreateTestAssembly();

            await _renderer.RenderAssemblyAsync(assembly, _context.OutputPath);

            var content = await File.ReadAllTextAsync(Path.Combine(_context.OutputPath, "index.md"));
            content.Should().Contain("## Namespaces");
            foreach (var ns in assembly.Namespaces)
            {
                var namespaceName = ns.Symbol.IsGlobalNamespace ? "global" : ns.Symbol.ToDisplayString();
                content.Should().Contain($"- [{namespaceName}]");
            }
        }

        #endregion

        #region RenderNamespaceAsync Tests

        [TestMethod]
        public async Task RenderNamespaceAsync_Should_Create_Namespace_File()
        {
            var assembly = CreateTestAssembly();
            var ns = assembly.Namespaces.First();

            await _renderer.RenderNamespaceAsync(ns, _context.OutputPath);

            var namespaceName = ns.Symbol.IsGlobalNamespace ? "global" : ns.Symbol.ToDisplayString();
            var nsPath = Path.Combine(_context.OutputPath, $"{namespaceName}.md");
            File.Exists(nsPath).Should().BeTrue();
        }

        [TestMethod]
        public async Task RenderNamespaceAsync_Should_List_Types_By_Category()
        {
            var assembly = CreateTestAssembly();
            var ns = assembly.Namespaces.First();

            await _renderer.RenderNamespaceAsync(ns, _context.OutputPath);

            var namespaceName = ns.Symbol.IsGlobalNamespace ? "global" : ns.Symbol.ToDisplayString();
            var nsPath = Path.Combine(_context.OutputPath, $"{namespaceName}.md");
            var content = await File.ReadAllTextAsync(nsPath);

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
            var assembly = CreateTestAssembly();
            var ns = assembly.Namespaces.FirstOrDefault(n => n.Types.Any());
            
            if (ns == null)
            {
                Assert.Inconclusive("No namespace with types found in test assembly");
                return;
            }
            
            var type = ns.Types.First();

            await _renderer.RenderTypeAsync(type, ns, _context.OutputPath);

            var safeNamespace = ns.Symbol.IsGlobalNamespace ? "global" : ns.Symbol.ToDisplayString();
            var safeTypeName = type.Symbol.Name
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_');
            var separator = _context.FileNamingOptions.NamespaceSeparator;
            var fileName = $"{safeNamespace.Replace('.', separator)}.{safeTypeName}.md";
            var typePath = Path.Combine(_context.OutputPath, fileName);
            File.Exists(typePath).Should().BeTrue();
        }

        [TestMethod]
        public async Task RenderTypeAsync_Should_Include_Type_Metadata()
        {
            var assembly = CreateTestAssembly();
            var ns = assembly.Namespaces.FirstOrDefault(n => n.Types.Any());
            
            if (ns == null)
            {
                Assert.Inconclusive("No namespace with types found in test assembly");
                return;
            }
            
            var type = ns.Types.First();

            await _renderer.RenderTypeAsync(type, ns, _context.OutputPath);

            var safeNamespace = ns.Symbol.IsGlobalNamespace ? "global" : ns.Symbol.ToDisplayString();
            var safeTypeName = type.Symbol.Name
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_');
            var separator = _context.FileNamingOptions.NamespaceSeparator;
            var fileName = $"{safeNamespace.Replace('.', separator)}.{safeTypeName}.md";
            var typePath = Path.Combine(_context.OutputPath, fileName);
            var content = await File.ReadAllTextAsync(typePath);

            content.Should().Contain($"# {type.Symbol.Name}");
            content.Should().Contain("## Definition");
            var expectedNamespace = ns.Symbol.IsGlobalNamespace ? "global" : ns.Symbol.ToDisplayString();
            content.Should().Contain($"**Namespace:** {expectedNamespace}");
            content.Should().Contain($"**Assembly:** {type.Symbol.ContainingAssembly?.Name}");
        }

        [TestMethod]
        public async Task RenderTypeAsync_Should_Include_Type_Signature()
        {
            var assembly = CreateTestAssembly();
            var ns = assembly.Namespaces.FirstOrDefault(n => n.Types.Any());
            
            if (ns == null)
            {
                Assert.Inconclusive("No namespace with types found in test assembly");
                return;
            }
            
            var type = ns.Types.First();

            await _renderer.RenderTypeAsync(type, ns, _context.OutputPath);

            var safeNamespace = ns.Symbol.IsGlobalNamespace ? "global" : ns.Symbol.ToDisplayString();
            var safeTypeName = type.Symbol.Name
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_');
            var separator = _context.FileNamingOptions.NamespaceSeparator;
            var fileName = $"{safeNamespace.Replace('.', separator)}.{safeTypeName}.md";
            var typePath = Path.Combine(_context.OutputPath, fileName);
            var content = await File.ReadAllTextAsync(typePath);

            content.Should().Contain("## Syntax");
            content.Should().Contain("```csharp");
        }

        [TestMethod]
        public async Task RenderTypeAsync_Should_Include_Members_By_Category()
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
            
            var ns = assembly.Namespaces.First(n => n.Types.Contains(type));

            await _renderer.RenderTypeAsync(type, ns, _context.OutputPath);

            var safeNamespace = ns.Symbol.IsGlobalNamespace ? "global" : ns.Symbol.ToDisplayString();
            var safeTypeName = type.Symbol.Name
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_');
            var separator = _context.FileNamingOptions.NamespaceSeparator;
            var fileName = $"{safeNamespace.Replace('.', separator)}.{safeTypeName}.md";
            var typePath = Path.Combine(_context.OutputPath, fileName);
            var content = await File.ReadAllTextAsync(typePath);

            content.Should().Contain("## Constructors");
            content.Should().Contain("## Methods");
        }

        #endregion

        #region RenderMember Tests

        [TestMethod]
        public void RenderMember_Should_Include_Member_Name()
        {
            var assembly = CreateTestAssembly();
            var type = assembly.Namespaces.First().Types.First();
            var member = type.Members.FirstOrDefault(m => m.Symbol.Kind == SymbolKind.Method);
            
            if (member == null)
            {
                Assert.Inconclusive("No method found in test assembly");
                return;
            }
            var sb = new StringBuilder();

            _renderer.RenderMember(sb, member);

            var result = sb.ToString();
            result.Should().Contain($"### {member.Symbol.Name}");
        }

        [TestMethod]
        public void RenderMember_Should_Include_Syntax_Section()
        {
            var assembly = CreateTestAssembly();
            var type = assembly.Namespaces.First().Types.First();
            var member = type.Members.FirstOrDefault(m => m.Symbol.Kind == SymbolKind.Method);
            
            if (member == null)
            {
                Assert.Inconclusive("No method found in test assembly");
                return;
            }
            var sb = new StringBuilder();

            _renderer.RenderMember(sb, member);

            var result = sb.ToString();
            result.Should().Contain("#### Syntax");
            result.Should().Contain("```csharp");
        }

        [TestMethod]
        public void RenderMember_Should_Include_Parameters_Table_When_Present()
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
            var member = type.Members.FirstOrDefault(m => m.Symbol.Name == "Calculate");
            if (member == null)
            {
                Assert.Inconclusive("Calculate method not found");
                return;
            }
            var sb = new StringBuilder();

            _renderer.RenderMember(sb, member);

            var result = sb.ToString();
            result.Should().Contain("#### Parameters");
            result.Should().Contain("| Name | Type | Description |");
            result.Should().Contain("|------|------|-------------|");
        }

        [TestMethod]
        public void RenderMember_Should_Include_Returns_Section_For_NonVoid_Methods()
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
            var member = type.Members.FirstOrDefault(m => m.Symbol.Name == "Calculate");
            if (member == null)
            {
                Assert.Inconclusive("Calculate method not found");
                return;
            }
            var sb = new StringBuilder();

            _renderer.RenderMember(sb, member);

            var result = sb.ToString();
            result.Should().Contain("#### Returns");
            result.Should().Contain("Type: `int`");
        }

        [TestMethod]
        public void RenderMember_Should_Include_Property_Value_Section_For_Properties()
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
            
            var member = type.Members.FirstOrDefault(m => m.Symbol.Kind == SymbolKind.Property);
            if (member == null)
            {
                Assert.Inconclusive("No property found");
                return;
            }
            var sb = new StringBuilder();

            _renderer.RenderMember(sb, member);

            var result = sb.ToString();
            result.Should().Contain("#### Property Value");
            result.Should().Contain("Type: `");
        }

        [TestMethod]
        public void RenderMember_Should_Include_Usage_When_Present()
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
            var member = type.Members.FirstOrDefault(m => m.Symbol.Name == "Calculate");
            if (member == null)
            {
                Assert.Inconclusive("Calculate method not found");
                return;
            }
            var sb = new StringBuilder();

            _renderer.RenderMember(sb, member);

            var result = sb.ToString();
            if (!string.IsNullOrWhiteSpace(member.Usage))
            {
                result.Should().Contain(member.Usage);
            }
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