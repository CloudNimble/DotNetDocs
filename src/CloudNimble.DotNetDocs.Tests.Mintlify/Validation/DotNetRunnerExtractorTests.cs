using System.Collections.Generic;
using CloudNimble.DotNetDocs.Mintlify.Validation;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Mintlify.Validation
{

    /// <summary>
    /// Tests for <see cref="DotNetRunnerExtractor"/>, verifying that <c>&lt;DotNetRunner&gt;</c>
    /// component <c>initialCode</c> props are correctly extracted from MDX content.
    /// </summary>
    [TestClass]
    public class DotNetRunnerExtractorTests
    {

        #region Extract Tests

        [TestMethod]
        public void Extract_MultilineCode_ExtractsCorrectly()
        {
            var mdx = """
                <DotNetRunner initialCode={`var names = new List<string> { "Alice", "Bob" };
                foreach (var name in names)
                {
                    Console.WriteLine(name);
                }`} />
                """;

            var results = DotNetRunnerExtractor.Extract(mdx, "multiline.mdx");

            results.Should().HaveCount(1);
            results[0].Code.Should().Contain("var names = new List<string>");
            results[0].Code.Should().Contain("Console.WriteLine(name);");
            results[0].SourceFile.Should().Be("multiline.mdx");
        }

        [TestMethod]
        public void Extract_MultipleRunners_ReturnsAll()
        {
            var mdx = """
                # Examples

                <DotNetRunner initialCode={`Console.WriteLine("First");`} />

                Some text between runners.

                <DotNetRunner initialCode={`Console.WriteLine("Second");`} />
                """;

            var results = DotNetRunnerExtractor.Extract(mdx, "multiple.mdx");

            results.Should().HaveCount(2);
            results[0].Code.Should().Be("Console.WriteLine(\"First\");");
            results[1].Code.Should().Be("Console.WriteLine(\"Second\");");
        }

        [TestMethod]
        public void Extract_NoRunners_ReturnsEmpty()
        {
            var mdx = """
                # Getting Started

                This is a regular MDX page with no DotNetRunner components.

                ```csharp
                Console.WriteLine("This is a code block, not a runner.");
                ```
                """;

            var results = DotNetRunnerExtractor.Extract(mdx, "plain.mdx");

            results.Should().BeEmpty();
        }

        [TestMethod]
        public void Extract_SingleRunner_ReturnsOneExample()
        {
            var mdx = """
                # Hello World

                <DotNetRunner initialCode={`Console.WriteLine("Hello, World!");`} />
                """;

            var results = DotNetRunnerExtractor.Extract(mdx, "hello.mdx");

            results.Should().HaveCount(1);
            results[0].Code.Should().Be("Console.WriteLine(\"Hello, World!\");");
            results[0].SourceFile.Should().Be("hello.mdx");
        }

        #endregion

    }

}
