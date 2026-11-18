using System.IO;
using System.Linq;
using System.Text;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Core.Renderers;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core.Renderers
{

    /// <summary>
    /// Tests for the RendererBase class.
    /// </summary>
    [TestClass]
    public class RendererBaseTests : DotNetDocsTestBase
    {

        #region Test Classes

        private class TestRenderer : RendererBase
        {
            public TestRenderer(ProjectContext? context = null) : base(context)
            {
            }

            // Expose protected methods for testing
            public new string GetSafeNamespaceName(DocNamespace ns) => base.GetSafeNamespaceName(ns);
            public new string GetSafeTypeName(DocType type) => base.GetSafeTypeName(type);
            public new string GetNamespaceFilePath(DocNamespace ns, string outputPath, string extension) => base.GetNamespaceFilePath(ns, outputPath, extension);
            public new string GetTypeFilePath(DocType type, DocNamespace ns, string outputPath, string extension) => base.GetTypeFilePath(type, ns, outputPath, extension);
            public new string GetNamespaceFileName(DocNamespace ns, string extension) => base.GetNamespaceFileName(ns, extension);
            public new ProjectContext Context => base.Context;
            public new FileNamingOptions FileNamingOptions => base.FileNamingOptions;
            public static string EscapeXmlTags(string? value) => EscapeXmlTagsInString(value);
            public new string GetNamespaceLink(DocNamespace ns, string extension, bool omitIndexFile = false) => base.GetNamespaceLink(ns, extension, omitIndexFile);
        }

        #endregion

        #region Fields

        private TestRenderer _renderer = null!;
        private DocAssembly _testAssembly = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            _renderer = new TestRenderer();
            
            // Use the shared test assembly
            _testAssembly = GetTestsDotSharedAssembly();
        }

        #endregion

        #region Constructor Tests

        [TestMethod]
        public void Constructor_WithNullContext_CreatesDefaultContext()
        {
            // Act
            var renderer = new TestRenderer(null);

            // Assert
            renderer.Context.Should().NotBeNull();
            renderer.FileNamingOptions.Should().NotBeNull();
            renderer.FileNamingOptions.NamespaceMode.Should().Be(NamespaceMode.File);
            renderer.FileNamingOptions.NamespaceSeparator.Should().Be('-');
        }

        [TestMethod]
        public void Constructor_WithCustomContext_UsesProvidedContext()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder, '_')
            };

            // Act
            var renderer = new TestRenderer(context);

            // Assert
            renderer.Context.Should().BeSameAs(context);
            renderer.FileNamingOptions.NamespaceMode.Should().Be(NamespaceMode.Folder);
            renderer.FileNamingOptions.NamespaceSeparator.Should().Be('_');
        }

        #endregion

        #region GetSafeNamespaceName Tests

        [TestMethod]
        public void GetSafeNamespaceName_WithNormalNamespace_ReturnsFullName()
        {
            // Arrange
            var ns = _testAssembly.Namespaces.First();

            // Act
            var result = _renderer.GetSafeNamespaceName(ns);

            // Assert
            result.Should().Be(ns.Name);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        public void GetSafeNamespaceName_WithGlobalNamespace_ReturnsGlobal(string name)
        {
            var globalNamespace = new DocNamespace(Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Empty").GlobalNamespace) { Name = name };

            _renderer.GetSafeNamespaceName(globalNamespace).Should().Be("global");
        }

        #endregion

        #region EscapeXmlTagsInString Tests

        [TestMethod]
        [DataRow(null, "")]
        [DataRow("", "")]
        [DataRow("   ", "   ")]
        [DataRow("List<int>", "List&lt;int&gt;")]
        [DataRow("Dictionary<string, List<int>>", "Dictionary&lt;string, List&lt;int&gt;&gt;")]
        [DataRow("<see cref=\"Widget\"/>", "&lt;see cref=\"Widget\"/&gt;")]
        [DataRow("a > b && c < d", "a &gt; b && c &lt; d")]
        [DataRow("No brackets here", "No brackets here")]
        public void EscapeXmlTagsInString_EscapesAngleBracketsOnly(string? input, string expected)
        {
            TestRenderer.EscapeXmlTags(input).Should().Be(expected);
        }

        #endregion

        #region GetNamespaceLink Tests

        [TestMethod]
        [DataRow(NamespaceMode.File, false, "CloudNimble-DotNetDocs-Tests-Shared.md")]
        [DataRow(NamespaceMode.File, true, "CloudNimble-DotNetDocs-Tests-Shared.md")]
        [DataRow(NamespaceMode.Folder, false, "CloudNimble/DotNetDocs/Tests/Shared/index.md")]
        [DataRow(NamespaceMode.Folder, true, "CloudNimble/DotNetDocs/Tests/Shared")]
        public void GetNamespaceLink_ReturnsForwardSlashLinkForEachMode(NamespaceMode mode, bool omitIndexFile, string expected)
        {
            var renderer = new TestRenderer(new ProjectContext { FileNamingOptions = new FileNamingOptions(mode) });
            var ns = _testAssembly.Namespaces.First(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared");

            renderer.GetNamespaceLink(ns, "md", omitIndexFile).Should().Be(expected);
        }

        #endregion

        #region GetSafeTypeName Tests

        [TestMethod]
        public void GetSafeTypeName_WithGenericType_ReplacesInvalidCharacters()
        {
            // Arrange
            // Find a type in the test assembly - SimpleClass is a good candidate
            var ns = _testAssembly.Namespaces.FirstOrDefault(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");
            ns.Should().NotBeNull("BasicScenarios namespace should exist");
            var type = ns!.Types.FirstOrDefault(t => t.Name == "SimpleClass");
            type.Should().NotBeNull("SimpleClass should exist in BasicScenarios");

            // Act
            var result = _renderer.GetSafeTypeName(type!);

            // Assert
            result.Should().Be("SimpleClass");
        }

        [TestMethod]
        public void GetSafeTypeName_WithSpecialCharacters_ReplacesAllInvalidCharacters()
        {
            // Arrange
            // Use any type from test assembly
            var type = _testAssembly.Namespaces.SelectMany(n => n.Types).First();
            
            // Act
            var result = _renderer.GetSafeTypeName(type);
            
            // Assert - verify no invalid characters remain
            result.Should().NotContain("<");
            result.Should().NotContain(">");
            result.Should().NotContain("`");
            result.Should().NotContain("/");
            result.Should().NotContain("\\");
            result.Should().NotContain(":");
            result.Should().NotContain("*");
            result.Should().NotContain("?");
            result.Should().NotContain("\"");
            result.Should().NotContain("|");
        }

        #endregion

        #region GetNamespaceFilePath Tests

        [TestMethod]
        public void GetNamespaceFilePath_WithFileMode_ReturnsFlatFileName()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '-')
            };
            var renderer = new TestRenderer(context);
            var ns = _testAssembly.Namespaces.First(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared");

            // Act
            var result = renderer.GetNamespaceFilePath(ns, Path.Combine("output"), "md");

            // Assert
            result.Should().Be(Path.Combine("output", "CloudNimble-DotNetDocs-Tests-Shared.md"));
        }

        [TestMethod]
        public void GetNamespaceFilePath_WithFolderMode_ReturnsFolderWithIndex()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder)
            };
            var renderer = new TestRenderer(context);
            var ns = _testAssembly.Namespaces.First(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared");

            // Act
            var result = renderer.GetNamespaceFilePath(ns, "output", "md");

            // Assert
            var expected = Path.Combine("output", "CloudNimble", "DotNetDocs", "Tests", "Shared", "index.md");
            result.Should().Be(expected);
        }

        #endregion

        #region GetTypeFilePath Tests

        [TestMethod]
        public void GetTypeFilePath_WithFileMode_ReturnsFlatFileName()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '_')
            };
            var renderer = new TestRenderer(context);
            var ns = _testAssembly.Namespaces.First(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");
            var type = ns.Types.First(t => t.Name == "SimpleClass");

            // Act
            var result = renderer.GetTypeFilePath(type, ns, "output", "md");

            // Assert
            result.Should().Be(Path.Combine("output", "CloudNimble_DotNetDocs_Tests_Shared_BasicScenarios.SimpleClass.md"));
        }

        [TestMethod]
        public void GetTypeFilePath_WithFolderMode_ReturnsFolderWithTypeFile()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder)
            };
            var renderer = new TestRenderer(context);
            var ns = _testAssembly.Namespaces.First(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");
            var type = ns.Types.First(t => t.Name == "SimpleClass");

            // Act
            var result = renderer.GetTypeFilePath(type, ns, "output", "md");

            // Assert
            result.Should().Be(Path.Combine("output", "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios", "SimpleClass.md"));
        }

        #endregion

    }

}