using System;
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

        [TestMethod]
        public async Task TransformAsync_HandlesEmptyInlineCodeTag()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = "This is <c></c> empty code.";

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<c>");
            testType.Summary.Should().Be("This is  empty code.");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesWhitespaceOnlyInlineCodeTag()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = "This is <c>   </c> whitespace code.";

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<c>");
            testType.Summary.Should().Be("This is  whitespace code.");
        }

        [TestMethod]
        public async Task TransformAsync_TrimsInlineCodeContent()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = "Use <c>  TestClass  </c> to perform operations.";

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().Contain("`TestClass`");
            testType.Summary.Should().NotContain("  TestClass  ");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesMultipleInlineCodeTags()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = "Use <c>TestClass</c> with <c>DoWork()</c> method.";

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<c>");
            testType.Summary.Should().Contain("`TestClass`");
            testType.Summary.Should().Contain("`DoWork()`");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesEmptyCodeBlock()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = "Example:<code></code>End.";

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().NotContain("<code>");
            testType.Examples.Should().Be("Example:End.");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesWhitespaceOnlyCodeBlock()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                Example:
                <code>

                </code>
                End.
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().NotContain("<code>");
            testType.Examples.Should().Contain("```");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesCodeBlockWithExplicitLanguage()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                <code language="python">
                def hello():
                    print("Hello")
                </code>
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().NotContain("<code");
            testType.Examples.Should().Contain("```python");
            testType.Examples.Should().Contain("def hello():");
        }

        [TestMethod]
        public async Task TransformAsync_DefaultsToCSharplanguageWhenNotSpecified()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                <code>
                var x = 1;
                </code>
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("```csharp");
        }

        [TestMethod]
        public async Task TransformAsync_RemovesCDATAFromCodeBlock()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                <code language="xml">
                <![CDATA[
                <root>
                    <element>value</element>
                </root>
                ]]>
                </code>
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().NotContain("<![CDATA[");
            testType.Examples.Should().NotContain("]]>");
            testType.Examples.Should().Contain("<root>");
            testType.Examples.Should().Contain("<element>value</element>");
        }

        [TestMethod]
        public async Task TransformAsync_RemovesCommonIndentationFromCodeBlock()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                <code>
                    public class Test
                    {
                        public void Method()
                        {
                            var x = 1;
                        }
                    }
                </code>
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("```csharp");
            testType.Examples.Should().Contain("public class Test");
            testType.Examples.Should().NotStartWith("    public class Test");
            var lines = testType.Examples!.Split('\n');
            var classLine = lines.First(l => l.Contains("public class Test"));
            classLine.Should().NotStartWith("    ");
        }

        [TestMethod]
        public async Task TransformAsync_EscapesTripleBackticksInCodeBlockContent()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                <code>
                var markdown = "```csharp\nvar x = 1;\n```";
                </code>
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("\\`\\`\\`csharp");
            testType.Examples.Should().NotContain("```csharp\nvar x = 1;");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesCodeBlockWithVariousLanguages()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                <code language="javascript">
                console.log("Hello");
                </code>
                <code language="typescript">
                const x: string = "test";
                </code>
                <code language="sql">
                SELECT * FROM Users;
                </code>
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("```javascript");
            testType.Examples.Should().Contain("```typescript");
            testType.Examples.Should().Contain("```sql");
            testType.Examples.Should().Contain("console.log(\"Hello\");");
            testType.Examples.Should().Contain("const x: string = \"test\";");
            testType.Examples.Should().Contain("SELECT * FROM Users;");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesMixedInlineAndBlockCode()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                Use <c>TestClass</c> like this:
                <code>
                var test = new TestClass();
                </code>
                Call <c>DoWork()</c> method.
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().NotContain("<c>");
            testType.Examples.Should().NotContain("<code>");
            testType.Examples.Should().Contain("`TestClass`");
            testType.Examples.Should().Contain("`DoWork()`");
            testType.Examples.Should().Contain("```csharp");
            testType.Examples.Should().Contain("var test = new TestClass();");
        }

        #endregion

        #region Formatting Tag Tests

        [TestMethod]
        public async Task TransformAsync_ConvertsParaToMarkdownParagraph()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Remarks = """
                First paragraph.
                <para>Second paragraph.</para>
                Third paragraph.
                """;

            await _transformer.TransformAsync(testType);

            testType.Remarks.Should().NotBeNull();
            testType.Remarks.Should().NotContain("<para>");
            testType.Remarks.Should().NotContain("</para>");
            testType.Remarks.Should().Contain("Second paragraph.");
            testType.Remarks.Should().Contain("\n\n");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesMultipleParaTags()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Remarks = """
                <para>First paragraph.</para>
                <para>Second paragraph.</para>
                <para>Third paragraph.</para>
                """;

            await _transformer.TransformAsync(testType);

            testType.Remarks.Should().NotBeNull();
            testType.Remarks.Should().NotContain("<para>");
            testType.Remarks.Should().Contain("First paragraph.");
            testType.Remarks.Should().Contain("Second paragraph.");
            testType.Remarks.Should().Contain("Third paragraph.");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesEmptyParaTag()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Remarks = """
                Before<para></para>After
                """;

            await _transformer.TransformAsync(testType);

            testType.Remarks.Should().NotBeNull();
            testType.Remarks.Should().NotContain("<para>");
            testType.Remarks.Should().Contain("\n\n");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesParaWithMultilineContent()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Remarks = """
                <para>This is a paragraph
                that spans multiple
                lines of text.</para>
                """;

            await _transformer.TransformAsync(testType);

            testType.Remarks.Should().NotBeNull();
            testType.Remarks.Should().NotContain("<para>");
            testType.Remarks.Should().Contain("This is a paragraph");
            testType.Remarks.Should().Contain("that spans multiple");
            testType.Remarks.Should().Contain("lines of text.");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsBoldToMarkdown()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                This is <b>bold text</b> in a sentence.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<b>");
            testType.Summary.Should().NotContain("</b>");
            testType.Summary.Should().Contain("**bold text**");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesMultipleBoldTags()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                This is <b>first bold</b> and <b>second bold</b> text.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<b>");
            testType.Summary.Should().Contain("**first bold**");
            testType.Summary.Should().Contain("**second bold**");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesEmptyBoldTag()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                This is <b></b> empty bold.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<b>");
            testType.Summary.Should().Contain("****");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesBoldWithMultilineContent()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                <b>This is bold
                across multiple lines.</b>
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<b>");
            testType.Summary.Should().Contain("**This is bold");
            testType.Summary.Should().Contain("across multiple lines.**");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsItalicToMarkdown()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                This is <i>italic text</i> in a sentence.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<i>");
            testType.Summary.Should().NotContain("</i>");
            testType.Summary.Should().Contain("*italic text*");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesMultipleItalicTags()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                This is <i>first italic</i> and <i>second italic</i> text.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<i>");
            testType.Summary.Should().Contain("*first italic*");
            testType.Summary.Should().Contain("*second italic*");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesEmptyItalicTag()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                This is <i></i> empty italic.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<i>");
            testType.Summary.Should().Contain("**");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesItalicWithMultilineContent()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                <i>This is italic
                across multiple lines.</i>
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<i>");
            testType.Summary.Should().Contain("*This is italic");
            testType.Summary.Should().Contain("across multiple lines.*");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsBrToMarkdownLineBreak()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Line one<br/>Line two
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<br");
            testType.Summary.Should().Contain("  \n");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesBrWithoutSlash()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Line one<br>Line two
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<br");
            testType.Summary.Should().Contain("  \n");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesBrWithSpaceAndSlash()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Line one<br />Line two
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<br");
            testType.Summary.Should().Contain("  \n");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesMultipleBrTags()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Line one<br/>Line two<br/>Line three<br />Line four
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<br");
            var lineBreakCount = testType.Summary!.Split("  \n").Length - 1;
            lineBreakCount.Should().Be(3);
        }

        [TestMethod]
        public async Task TransformAsync_HandlesBrCaseInsensitive()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Line one<BR/>Line two<Br />Line three<bR>Line four
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<BR");
            testType.Summary.Should().NotContain("<Br");
            testType.Summary.Should().NotContain("<bR");
            testType.Summary.Should().NotContain("<br");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesFormattingTagsCaseInsensitive()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                <PARA>Uppercase para</PARA>
                <B>Uppercase bold</B>
                <I>Uppercase italic</I>
                <Para>Mixed case para</Para>
                <b>Lowercase bold</b>
                <i>Lowercase italic</i>
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<PARA>");
            testType.Summary.Should().NotContain("<Para>");
            testType.Summary.Should().NotContain("<para>");
            testType.Summary.Should().NotContain("<B>");
            testType.Summary.Should().NotContain("<b>");
            testType.Summary.Should().NotContain("<I>");
            testType.Summary.Should().NotContain("<i>");
            testType.Summary.Should().Contain("**Uppercase bold**");
            testType.Summary.Should().Contain("*Uppercase italic*");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesMixedFormattingTags()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                <para>This is a paragraph with <b>bold</b> and <i>italic</i> text.</para>
                Another line<br/>with a break.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<para>");
            testType.Summary.Should().NotContain("<b>");
            testType.Summary.Should().NotContain("<i>");
            testType.Summary.Should().NotContain("<br");
            testType.Summary.Should().Contain("**bold**");
            testType.Summary.Should().Contain("*italic*");
            testType.Summary.Should().Contain("  \n");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesNestedBoldAndItalic()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                This is <b>bold with <i>italic inside</i></b> text.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<b>");
            testType.Summary.Should().NotContain("<i>");
            testType.Summary.Should().Contain("**bold with *italic inside***");
        }

        #endregion

        #region Escape Remaining XML Tags Tests

        [TestMethod]
        public async Task TransformAsync_EscapesXmlTagsOutsideCodeBlocks()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = "This has <generic> and <other> tags.";

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().Contain("&lt;generic&gt;");
            testType.Summary.Should().Contain("&lt;other&gt;");
        }

        [TestMethod]
        public async Task TransformAsync_PreservesInlineCodeWithoutEscaping()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = "Use `List<T>` for collections.";

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().Contain("`List<T>`");
            testType.Summary.Should().NotContain("&lt;");
            testType.Summary.Should().NotContain("&gt;");
        }

        [TestMethod]
        public async Task TransformAsync_EscapesXmlBeforeInlineCode()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = "Escape <this> but not `<that>`.";

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().Contain("&lt;this&gt;");
            testType.Summary.Should().Contain("`<that>`");
        }

        [TestMethod]
        public async Task TransformAsync_EscapesXmlAfterInlineCode()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = "Use `List<T>` and escape <generic>.";

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().Contain("`List<T>`");
            testType.Summary.Should().Contain("&lt;generic&gt;");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesMultipleInlineCodeBlocks()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = "Use `List<T>` or `Dictionary<K,V>` and escape <other>.";

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().Contain("`List<T>`");
            testType.Summary.Should().Contain("`Dictionary<K,V>`");
            testType.Summary.Should().Contain("&lt;other&gt;");
        }

        [TestMethod]
        public async Task TransformAsync_PreservesCodeFenceWithoutEscaping()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                ```csharp
                var list = new List<int>();
                var dict = new Dictionary<string, object>();
                ```
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("```csharp");
            testType.Examples.Should().Contain("List<int>");
            testType.Examples.Should().Contain("Dictionary<string, object>");
            testType.Examples.Should().NotContain("&lt;");
            testType.Examples.Should().NotContain("&gt;");
        }

        [TestMethod]
        public async Task TransformAsync_EscapesXmlBeforeCodeFence()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                Escape <this> tag.
                ```csharp
                var list = new List<int>();
                ```
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("&lt;this&gt;");
            testType.Examples.Should().Contain("List<int>");
        }

        [TestMethod]
        public async Task TransformAsync_EscapesXmlAfterCodeFence()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                ```csharp
                var list = new List<int>();
                ```
                Escape <this> tag.
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("List<int>");
            testType.Examples.Should().Contain("&lt;this&gt;");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesMultipleCodeFences()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                ```csharp
                var list = new List<int>();
                ```
                Escape <between> tags.
                ```typescript
                const map: Map<string, number>;
                ```
                Escape <after> too.
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("List<int>");
            testType.Examples.Should().Contain("Map<string, number>");
            testType.Examples.Should().Contain("&lt;between&gt;");
            testType.Examples.Should().Contain("&lt;after&gt;");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesNoEscapeBlock()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                ```noescape
                <root>
                    <element>Value</element>
                </root>
                ```
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().NotContain("```noescape");
            testType.Examples.Should().NotContain("```");
            testType.Examples.Should().Contain("<root>");
            testType.Examples.Should().Contain("<element>Value</element>");
            testType.Examples.Should().NotContain("&lt;");
            testType.Examples.Should().NotContain("&gt;");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesNoEscapeBlockWithCarriageReturn()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = "```noescape\r\n<content>Test</content>\r\n```";

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("<content>Test</content>");
            testType.Examples.Should().NotContain("```");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesNoEscapeBlockWithNewlineOnly()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = "```noescape\n<content>Test</content>\n```";

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("<content>Test</content>");
            testType.Examples.Should().NotContain("```");
        }

        [TestMethod]
        public async Task TransformAsync_EscapesXmlBeforeNoEscapeBlock()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                Escape <this> tag.
                ```noescape
                <root>Keep</root>
                ```
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("&lt;this&gt;");
            testType.Examples.Should().Contain("<root>Keep</root>");
        }

        [TestMethod]
        public async Task TransformAsync_EscapesXmlAfterNoEscapeBlock()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                ```noescape
                <root>Keep</root>
                ```
                Escape <this> tag.
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("<root>Keep</root>");
            testType.Examples.Should().Contain("&lt;this&gt;");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesUnmatchedSingleBacktick()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = "This has ` single backtick and <tag>.";

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().Contain("&lt;tag&gt;");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesUnmatchedTripleBacktick()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                ```csharp
                var list = new List<int>();
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("&lt;int&gt;");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesTextWithNoBackticks()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = "This has <generic> and <other> tags.";

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().Contain("&lt;generic&gt;");
            testType.Summary.Should().Contain("&lt;other&gt;");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesMixedBackticksAndXmlTags()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                Use `List<T>` for collections.
                ```csharp
                var dict = new Dictionary<K, V>();
                ```
                Escape <generic> tags.
                Another `IEnumerable<T>` example.
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("`List<T>`");
            testType.Examples.Should().Contain("Dictionary<K, V>");
            testType.Examples.Should().Contain("&lt;generic&gt;");
            testType.Examples.Should().Contain("`IEnumerable<T>`");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesGenericTypeNames()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Use `List<T>`, `Dictionary<TKey, TValue>`, `IEnumerable<T>`, and `Task<T>`.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().Contain("`List<T>`");
            testType.Summary.Should().Contain("`Dictionary<TKey, TValue>`");
            testType.Summary.Should().Contain("`IEnumerable<T>`");
            testType.Summary.Should().Contain("`Task<T>`");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesComplexGenericTypes()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                ```csharp
                var nested = new Dictionary<string, List<Tuple<int, string>>>();
                var func = new Func<Task<IEnumerable<T>>, CancellationToken, Task<Result<T>>>();
                ```
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("Dictionary<string, List<Tuple<int, string>>>");
            testType.Examples.Should().Contain("Func<Task<IEnumerable<T>>, CancellationToken, Task<Result<T>>>");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesXmlInCodeFenceLanguageSpecifier()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                ```xml
                <configuration>
                    <appSettings>
                        <add key="test" value="123" />
                    </appSettings>
                </configuration>
                ```
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("<configuration>");
            testType.Examples.Should().Contain("<appSettings>");
            testType.Examples.Should().NotContain("&lt;");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesEmptyCodeFence()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                ```
                ```
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("```");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesNoEscapeBlockWithOnlyXmlTags()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                <note>This will be escaped</note>
                ```noescape
                ```
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("&lt;note&gt;");
            testType.Examples.Should().NotContain("```noescape");
            testType.Examples.Should().NotContain("```");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesNoEscapeBlockInlineSyntax()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = "<test/>```noescape\n```";

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("&lt;test/&gt;");
            testType.Examples.Should().NotContain("```");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesNoEscapeBlockWithMultipleBlankLines()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                <test/>
                ```noescape


                ```
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("&lt;test/&gt;");
            testType.Examples.Should().NotContain("```noescape");
            testType.Examples.Should().NotContain("```");
            testType.Examples.Count(c => c == '\n').Should().Be(3);
        }

        [TestMethod]
        public async Task TransformAsync_PreservesLessThanGreaterThanInCode()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Examples = """
                ```csharp
                if (x < 5 && y > 10)
                {
                    var result = x <= y ? x : y;
                }
                ```
                """;

            await _transformer.TransformAsync(testType);

            testType.Examples.Should().NotBeNull();
            testType.Examples.Should().Contain("x < 5");
            testType.Examples.Should().Contain("y > 10");
            testType.Examples.Should().Contain("x <= y");
            testType.Examples.Should().NotContain("&lt;");
            testType.Examples.Should().NotContain("&gt;");
        }

        #endregion

        #region List Conversion Tests

        [TestMethod]
        public async Task TransformAsync_ConvertsBulletListWithDescriptionOnly()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Remarks = """
                <para>This method registers:</para>
                <list type="bullet">
                <item><description>MintlifyRenderer for generating MDX documentation</description></item>
                <item><description>DocsJsonManager for manipulating docs.json files</description></item>
                <item><description>DocsJsonValidator to ensure correct structures</description></item>
                </list>
                """;

            await _transformer.TransformAsync(testType);

            testType.Remarks.Should().NotBeNull();
            testType.Remarks.Should().NotContain("<list");
            testType.Remarks.Should().NotContain("<item>");
            testType.Remarks.Should().NotContain("<description>");
            testType.Remarks.Should().Contain("- MintlifyRenderer for generating MDX documentation");
            testType.Remarks.Should().Contain("- DocsJsonManager for manipulating docs.json files");
            testType.Remarks.Should().Contain("- DocsJsonValidator to ensure correct structures");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsNumberedList()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Remarks = """
                <list type="number">
                <item><description>First step</description></item>
                <item><description>Second step</description></item>
                <item><description>Third step</description></item>
                </list>
                """;

            await _transformer.TransformAsync(testType);

            testType.Remarks.Should().NotBeNull();
            testType.Remarks.Should().NotContain("<list");
            testType.Remarks.Should().NotContain("<item>");
            testType.Remarks.Should().NotContain("<description>");
            testType.Remarks.Should().Contain("1. First step");
            testType.Remarks.Should().Contain("2. Second step");
            testType.Remarks.Should().Contain("3. Third step");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsTableList()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Remarks = """
                <list type="table">
                <listheader><term>Parameter</term><description>Description</description></listheader>
                <item><term>name</term><description>The name value</description></item>
                <item><term>age</term><description>The age value</description></item>
                </list>
                """;

            await _transformer.TransformAsync(testType);

            testType.Remarks.Should().NotBeNull();
            testType.Remarks.Should().NotContain("<list");
            testType.Remarks.Should().NotContain("<term>");
            testType.Remarks.Should().Contain("| Parameter | Description |");
            testType.Remarks.Should().Contain("|----------|----------|");
            testType.Remarks.Should().Contain("| name | The name value |");
            testType.Remarks.Should().Contain("| age | The age value |");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsBulletListWithTermAndDescription()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Remarks = """
                <list type="bullet">
                <item><term>Option A</term><description>First option description</description></item>
                <item><term>Option B</term><description>Second option description</description></item>
                </list>
                """;

            await _transformer.TransformAsync(testType);

            testType.Remarks.Should().NotBeNull();
            testType.Remarks.Should().NotContain("<list");
            testType.Remarks.Should().Contain("- First option description");
            testType.Remarks.Should().Contain("- Second option description");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesListWithPara()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Remarks = """
                <para>Important items:</para>
                <list type="bullet">
                <item><description>Item one</description></item>
                <item><description>Item two</description></item>
                </list>
                """;

            await _transformer.TransformAsync(testType);

            testType.Remarks.Should().NotBeNull();
            testType.Remarks.Should().NotContain("<para>");
            testType.Remarks.Should().NotContain("<list");
            testType.Remarks.Should().Contain("Important items:");
            testType.Remarks.Should().Contain("- Item one");
            testType.Remarks.Should().Contain("- Item two");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesEmptyListItems()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Remarks = """
                <list type="bullet">
                <item><description>Valid item</description></item>
                <item><description></description></item>
                <item><description>Another valid item</description></item>
                </list>
                """;

            await _transformer.TransformAsync(testType);

            testType.Remarks.Should().NotBeNull();
            testType.Remarks.Should().NotContain("<list");
            testType.Remarks.Should().Contain("- Valid item");
            testType.Remarks.Should().Contain("- Another valid item");
            var bulletCount = testType.Remarks!.Split('-').Length - 1;
            bulletCount.Should().Be(2, "empty items should be skipped");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsTableWithoutHeader()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Remarks = """
                <list type="table">
                <item><term>First Term</term><description>First description</description></item>
                <item><term>Second Term</term><description>Second description</description></item>
                </list>
                """;

            await _transformer.TransformAsync(testType);

            testType.Remarks.Should().NotBeNull();
            testType.Remarks.Should().NotContain("<list");
            testType.Remarks.Should().NotContain("<item>");
            testType.Remarks.Should().NotContain("<term>");
            testType.Remarks.Should().NotContain("<description>");
            testType.Remarks.Should().Contain("**First Term**");
            testType.Remarks.Should().Contain("First description");
            testType.Remarks.Should().Contain("**Second Term**");
            testType.Remarks.Should().Contain("Second description");
            testType.Remarks.Should().NotContain("| ");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsTableWithoutHeaderAndOnlyTerms()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Remarks = """
                <list type="table">
                <item><term>Only Term</term></item>
                <item><term>Another Term</term></item>
                </list>
                """;

            await _transformer.TransformAsync(testType);

            testType.Remarks.Should().NotBeNull();
            testType.Remarks.Should().NotContain("<list");
            testType.Remarks.Should().Contain("**Only Term**");
            testType.Remarks.Should().Contain("**Another Term**");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsTableWithoutHeaderAndOnlyDescriptions()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Remarks = """
                <list type="table">
                <item><description>Only description</description></item>
                <item><description>Another description</description></item>
                </list>
                """;

            await _transformer.TransformAsync(testType);

            testType.Remarks.Should().NotBeNull();
            testType.Remarks.Should().NotContain("<list");
            testType.Remarks.Should().Contain("Only description");
            testType.Remarks.Should().Contain("Another description");
            testType.Remarks.Should().NotContain("**");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesBulletListWithOnlyTerms()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Remarks = """
                <list type="bullet">
                <item><term>Term only item</term></item>
                <item><term>Another term only</term></item>
                </list>
                """;

            await _transformer.TransformAsync(testType);

            testType.Remarks.Should().NotBeNull();
            testType.Remarks.Should().NotContain("<list");
            testType.Remarks.Should().Contain("- Term only item");
            testType.Remarks.Should().Contain("- Another term only");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesNumberedListWithEmptyItems()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Remarks = """
                <list type="number">
                <item><description>First step</description></item>
                <item><description></description></item>
                <item><description>Third step</description></item>
                <item><description></description></item>
                <item><description>Fifth step</description></item>
                </list>
                """;

            await _transformer.TransformAsync(testType);

            testType.Remarks.Should().NotBeNull();
            testType.Remarks.Should().NotContain("<list");
            testType.Remarks.Should().Contain("1. First step");
            testType.Remarks.Should().Contain("2. Third step");
            testType.Remarks.Should().Contain("3. Fifth step");
            testType.Remarks.Should().NotContain("4.");
            testType.Remarks.Should().NotContain("5.");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesListTypeCaseInsensitivity()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Remarks = """
                <list type="Bullet">
                <item><description>Item one</description></item>
                </list>
                <list type="Number">
                <item><description>Item two</description></item>
                </list>
                <list type="TABLE">
                <listheader><term>Col1</term><description>Col2</description></listheader>
                <item><term>val1</term><description>val2</description></item>
                </list>
                """;

            await _transformer.TransformAsync(testType);

            testType.Remarks.Should().NotBeNull();
            testType.Remarks.Should().NotContain("<list");
            testType.Remarks.Should().Contain("- Item one");
            testType.Remarks.Should().Contain("1. Item two");
            testType.Remarks.Should().Contain("| Col1 | Col2 |");
            testType.Remarks.Should().Contain("| val1 | val2 |");
        }

        #endregion

        #region See Reference Tests

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeCrefToMarkdownLink()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                See <see cref="T:CloudNimble.DotNetDocs.Core.Configuration.NamespaceMode"/> for details.
                """;

            await _transformer.TransformAsync(assembly);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see cref=");
            testType.Summary.Should().Contain("[");
            testType.Summary.Should().Contain("]");
            testType.Summary.Should().Contain("(");
            testType.Summary.Should().Contain(")");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeCrefSelfClosing()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                See <see cref="T:System.String" /> for details.
                """;

            await _transformer.TransformAsync(assembly);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see cref=");
            testType.Summary.Should().Contain("[");
            testType.Summary.Should().Contain("]");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesMultipleSeeCrefTags()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                See <see cref="T:System.String"/> and <see cref="T:System.Int32"/> for details.
                """;

            await _transformer.TransformAsync(assembly);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see cref=");
            var linkCount = testType.Summary!.Split('[').Length - 1;
            linkCount.Should().BeGreaterOrEqualTo(2, "should have at least two markdown links");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeHrefWithLinkText()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                See <see href="https://example.com">example website</see> for details.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see href=");
            testType.Summary.Should().Contain("[example website](https://example.com)");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeHrefSelfClosingWithDefaultLinkText()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                See <see href="https://example.com"/> for details.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see href=");
            testType.Summary.Should().Contain("[link](https://example.com)");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeHrefSelfClosingWithSlash()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                See <see href="https://example.com" /> for details.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see href=");
            testType.Summary.Should().Contain("[link](https://example.com)");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesMultipleSeeHrefTags()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                See <see href="https://example.com">first</see> and <see href="https://example.org">second</see>.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see href=");
            testType.Summary.Should().Contain("[first](https://example.com)");
            testType.Summary.Should().Contain("[second](https://example.org)");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeLangwordNull()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Returns <see langword="null"/> if not found.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see langword=");
            testType.Summary.Should().Contain("[`null`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null)");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeLangwordTrue()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Returns <see langword="true"/> if successful.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see langword=");
            testType.Summary.Should().Contain("[`true`](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool)");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeLangwordFalse()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Returns <see langword="false"/> if failed.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see langword=");
            testType.Summary.Should().Contain("[`false`](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool)");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeLangwordVoid()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Returns <see langword="void"/>.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see langword=");
            testType.Summary.Should().Contain("[`void`](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/void)");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeLangwordAsync()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Use <see langword="async"/> methods.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see langword=");
            testType.Summary.Should().Contain("[`async`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/async)");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeLangwordAwait()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Use <see langword="await"/> keyword.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see langword=");
            testType.Summary.Should().Contain("[`await`](https://learn.microsoft.com/dotnet/csharp/language-reference/operators/await)");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeLangwordStatic()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Use <see langword="static"/> modifier.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see langword=");
            testType.Summary.Should().Contain("[`static`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/static)");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeLangwordAbstract()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Use <see langword="abstract"/> modifier.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see langword=");
            testType.Summary.Should().Contain("[`abstract`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/abstract)");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeLangwordVirtual()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Use <see langword="virtual"/> modifier.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see langword=");
            testType.Summary.Should().Contain("[`virtual`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/virtual)");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeLangwordOverride()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Use <see langword="override"/> modifier.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see langword=");
            testType.Summary.Should().Contain("[`override`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/override)");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeLangwordSealed()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Use <see langword="sealed"/> modifier.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see langword=");
            testType.Summary.Should().Contain("[`sealed`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/sealed)");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeLangwordUnknownKeywordWithoutUrl()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Use <see langword="customkeyword"/> modifier.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see langword=");
            testType.Summary.Should().Contain("`customkeyword`");
            testType.Summary.Should().NotContain("](");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeLangwordCaseInsensitive()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                Use <see langword="NULL"/> and <see langword="True"/> and <see langword="ASYNC"/>.
                """;

            await _transformer.TransformAsync(testType);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see langword=");
            testType.Summary.Should().Contain("[`null`]");
            testType.Summary.Should().Contain("[`true`]");
            testType.Summary.Should().Contain("[`async`]");
        }

        [TestMethod]
        public async Task TransformAsync_HandlesMixedSeeTagTypes()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var testType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .First();

            testType.Summary = """
                See <see cref="T:System.String"/> and <see href="https://example.com">example</see> or <see langword="null"/>.
                """;

            await _transformer.TransformAsync(assembly);

            testType.Summary.Should().NotBeNull();
            testType.Summary.Should().NotContain("<see ");
            testType.Summary.Should().Contain("[example](https://example.com)");
            testType.Summary.Should().Contain("[`null`]");
        }

        [TestMethod]
        public async Task TransformAsync_ConvertsSeeAlsoReferences()
        {
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            var namespaceMode = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Name == "NamespaceMode");

            namespaceMode.Should().NotBeNull("NamespaceMode type should exist in the assembly");

            namespaceMode!.SeeAlso = new List<DocReference>
            {
                new DocReference("T:CloudNimble.DotNetDocs.Core.Configuration.NamespaceMode"),
                new DocReference("T:System.String")
            };

            await _transformer.TransformAsync(assembly);

            namespaceMode.SeeAlso.Should().NotBeNull();
            namespaceMode.SeeAlso.Should().HaveCount(2);

            var firstRef = namespaceMode.SeeAlso!.First();
            firstRef.IsResolved.Should().BeTrue();
            firstRef.ReferenceType.Should().Be(ReferenceType.Type);

            var secondRef = namespaceMode.SeeAlso!.Last();
            secondRef.IsResolved.Should().BeTrue();
            secondRef.ReferenceType.Should().Be(ReferenceType.Framework);
        }

        #endregion

    }

}