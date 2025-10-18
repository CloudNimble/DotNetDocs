using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Core.Transformers;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core.Transformers
{

    /// <summary>
    /// Tests for the MarkdownXmlTransformer class.
    /// </summary>
    [TestClass]
    public class MarkdownXmlTransformerTests : DotNetDocsTestBase
    {

        #region Fields

        private MarkdownXmlTransformer _transformer = null!;
        private ProjectContext _context = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            TestHostBuilder.ConfigureServices((context, services) =>
            {
                services.AddDotNetDocs(ctx =>
                {
                    ctx.FileNamingOptions.NamespaceMode = NamespaceMode.Folder;
                    ctx.FileNamingOptions.NamespaceSeparator = '-';
                });
            });

            TestSetup();
            _context = GetService<ProjectContext>();
            _transformer = new MarkdownXmlTransformer(_context);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestTearDown();
        }

        #endregion

        #region Code Tag Tests

        [TestMethod]
        public async Task TransformAsync_RemovesCodeTagsFromExamples()
        {
            // Arrange
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            // Get a real type to work with
            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            // Set up test content with code tags
            testType.Examples = @"<code>
public class TestClass
{
    public void Method() { }
}
</code>";

            // Act
            await _transformer.TransformAsync(testType);

            // Assert
            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().NotContain("<code>");
            testType.Examples.Should().NotContain("</code>");
            testType.Examples.Should().Contain("```csharp");
            testType.Examples.Should().Contain("public class TestClass");
        }

        [TestMethod]
        public async Task TransformAsync_RemovesCodeTagsWithCDATA()
        {
            // Arrange
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = @"<code>
<![CDATA[
public class TestClass
{
    public void Method() { }
}
]]>
</code>";

            // Act
            await _transformer.TransformAsync(testType);

            // Assert
            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().NotContain("<code>");
            testType.Examples.Should().NotContain("</code>");
            testType.Examples.Should().NotContain("<![CDATA[");
            testType.Examples.Should().NotContain("]]>");
            testType.Examples.Should().Contain("```csharp");
            testType.Examples.Should().Contain("public class TestClass");
        }

        [TestMethod]
        public async Task TransformAsync_EscapesTripleBackticksInCodeBlocks()
        {
            // Arrange
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            // This simulates the DocEntity example that has triple backticks in the code
            testType.Examples = @"<code>
<![CDATA[
var docType = new DocType(symbol)
{
    Summary = ""Represents a logging service."",
    Examples = ""```csharp\nLogger.LogInfo(\""Message\"");\n```""
};
]]>
</code>";

            // Act
            await _transformer.TransformAsync(testType);

            // Assert
            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().NotContain("<code>");
            testType.Examples.Should().NotContain("</code>");
            testType.Examples.Should().NotContain("<![CDATA[");
            testType.Examples.Should().NotContain("]]>");
            testType.Examples.Should().Contain("```csharp");
            // The triple backticks in the string literal should be escaped
            testType.Examples.Should().Contain("\\`\\`\\`csharp");
            testType.Examples.Should().Contain("var docType = new DocType(symbol)");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesMultipleCodeBlocks()
        {
            // Arrange
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = @"First example:
<code>
var x = 1;
</code>

Second example:
<code language=""json"">
{
  ""key"": ""value""
}
</code>";

            // Act
            await _transformer.TransformAsync(testType);

            // Assert
            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().NotContain("<code");
            testType.Examples.Should().NotContain("</code>");
            testType.Examples.Should().Contain("```csharp");
            testType.Examples.Should().Contain("```json");
            testType.Examples.Should().Contain("var x = 1;");
            testType.Examples.Should().Contain("\"key\": \"value\"");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesInlineCodeTags()
        {
            // Arrange
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = "Use <c>TestClass</c> to perform operations.";

            // Act
            await _transformer.TransformAsync(testType);

            // Assert
            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<c>");
            testType.Summary.Should().NotContain("</c>");
            testType.Summary.Should().Contain("`TestClass`");
        }

        [TestMethod]
        public async Task TransformAsync_PreservesCodeInRemarks()
        {
            // Arrange
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Remarks = @"Example usage:
<code>
var instance = new TestClass();
instance.DoWork();
</code>";

            // Act
            await _transformer.TransformAsync(testType);

            // Assert
            testType.Remarks.Should().NotBeNull();
            testType.Remarks.Should().NotContain("<code>");
            testType.Remarks.Should().NotContain("</code>");
            testType.Remarks.Should().Contain("```csharp");
            testType.Remarks.Should().Contain("var instance = new TestClass();");
        }

        #endregion

        #region See Also Tests

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeAlsoReferences()
        {
            // Arrange
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            // Find the NamespaceMode type specifically
            var namespaceMode = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Name == "NamespaceMode");

            namespaceMode.Should().NotBeNull("NamespaceMode type should exist in the assembly");

            namespaceMode!.SeeAlso = new List<DocReference>
            {
                new DocReference("T:CloudNimble.DotNetDocs.Core.Configuration.NamespaceMode"),
                new DocReference("T:System.String")
            };

            // Act - Transform the entire assembly to build reference map
            await _transformer.TransformAsync(assembly);

            // Assert
            namespaceMode.SeeAlso.Should().NotBeNull();
            namespaceMode.SeeAlso.Should().HaveCount(2);

            // First reference should resolve to the NamespaceMode type itself
            var firstRef = namespaceMode.SeeAlso!.First();
            firstRef.IsResolved.Should().BeTrue();
            firstRef.ReferenceType.Should().Be(ReferenceType.Type);

            // Second reference should resolve to System.String (framework type)
            var secondRef = namespaceMode.SeeAlso!.Last();
            secondRef.IsResolved.Should().BeTrue();
            secondRef.ReferenceType.Should().Be(ReferenceType.Framework);
        }

        #endregion

    }

}