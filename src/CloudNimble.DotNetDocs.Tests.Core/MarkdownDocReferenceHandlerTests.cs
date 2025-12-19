using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Tests for <see cref="MarkdownDocReferenceHandler"/>, covering the base Markdown
    /// content rewriting functionality.
    /// </summary>
    [TestClass]
    public class MarkdownDocReferenceHandlerTests : DotNetDocsTestBase
    {

        #region DocumentationType Tests

        [TestMethod]
        public void DocumentationType_ReturnsGeneric()
        {
            var handler = new TestMarkdownHandler();

            handler.DocumentationType.Should().Be(SupportedDocumentationType.Generic);
        }

        #endregion

        #region RewriteMarkdownContent Tests - Image Rewriting

        [TestMethod]
        public void RewriteMarkdownContent_MarkdownImage_RewritesPath()
        {
            var handler = new TestMarkdownHandler();

            var content = "![Logo](/images/logo.png)";
            var result = handler.TestRewriteMarkdownContent(content, "my-project");

            result.Should().Be("![Logo](/images/my-project/logo.png)");
        }

        [TestMethod]
        public void RewriteMarkdownContent_MarkdownImageWithAltText_PreservesAltText()
        {
            var handler = new TestMarkdownHandler();

            var content = "![Company Logo](/images/company/logo.png)";
            var result = handler.TestRewriteMarkdownContent(content, "my-project");

            result.Should().Be("![Company Logo](/images/my-project/company/logo.png)");
        }

        [TestMethod]
        public void RewriteMarkdownContent_MarkdownImageWithTitle_PreservesTitle()
        {
            var handler = new TestMarkdownHandler();

            var content = "![Logo](/images/logo.png \"Company Logo\")";
            var result = handler.TestRewriteMarkdownContent(content, "my-project");

            result.Should().Be("![Logo](/images/my-project/logo.png \"Company Logo\")");
        }

        [TestMethod]
        public void RewriteMarkdownContent_MultipleImages_RewritesAll()
        {
            var handler = new TestMarkdownHandler();

            var content = "![Logo](/images/logo.png) and ![Banner](/images/banner.jpg)";
            var result = handler.TestRewriteMarkdownContent(content, "my-project");

            result.Should().Be("![Logo](/images/my-project/logo.png) and ![Banner](/images/my-project/banner.jpg)");
        }

        #endregion

        #region RewriteMarkdownContent Tests - Link Rewriting

        [TestMethod]
        public void RewriteMarkdownContent_MarkdownLink_RewritesPath()
        {
            var handler = new TestMarkdownHandler();

            var content = "[Getting Started](/guides/quickstart)";
            var result = handler.TestRewriteMarkdownContent(content, "my-project");

            result.Should().Be("[Getting Started](/my-project/guides/quickstart)");
        }

        [TestMethod]
        public void RewriteMarkdownContent_MarkdownLinkWithAnchor_PreservesAnchor()
        {
            var handler = new TestMarkdownHandler();

            var content = "[Installation](/guides/install#prerequisites)";
            var result = handler.TestRewriteMarkdownContent(content, "my-project");

            result.Should().Be("[Installation](/my-project/guides/install#prerequisites)");
        }

        [TestMethod]
        public void RewriteMarkdownContent_MarkdownLinkWithQueryString_PreservesQueryString()
        {
            var handler = new TestMarkdownHandler();

            var content = "[API Reference](/api/overview?version=2)";
            var result = handler.TestRewriteMarkdownContent(content, "my-project");

            result.Should().Be("[API Reference](/my-project/api/overview?version=2)");
        }

        [TestMethod]
        public void RewriteMarkdownContent_MultipleLinks_RewritesAll()
        {
            var handler = new TestMarkdownHandler();

            var content = "[Home](/index) and [About](/about)";
            var result = handler.TestRewriteMarkdownContent(content, "my-project");

            result.Should().Be("[Home](/my-project/index) and [About](/my-project/about)");
        }

        #endregion

        #region RewriteMarkdownContent Tests - Code Block Preservation

        [TestMethod]
        public void RewriteMarkdownContent_PathInCodeBlock_PreservesOriginalPath()
        {
            var handler = new TestMarkdownHandler();

            var content = """
                ```bash
                curl /api/endpoint
                ```
                """;

            var result = handler.TestRewriteMarkdownContent(content, "my-project");

            result.Should().Be(content, "paths inside code blocks should not be rewritten");
        }

        [TestMethod]
        public void RewriteMarkdownContent_PathInTildeCodeBlock_PreservesOriginalPath()
        {
            var handler = new TestMarkdownHandler();

            var content = """
                ~~~javascript
                fetch('/api/data');
                ~~~
                """;

            var result = handler.TestRewriteMarkdownContent(content, "my-project");

            result.Should().Be(content, "paths inside tilde code blocks should not be rewritten");
        }

        [TestMethod]
        public void RewriteMarkdownContent_MixedContentWithCodeBlock_OnlyRewritesOutsideCodeBlock()
        {
            var handler = new TestMarkdownHandler();

            var content = """
                [Documentation](/docs/intro)

                ```javascript
                import { api } from '/api/client';
                ```

                [More Info](/docs/advanced)
                """;

            var result = handler.TestRewriteMarkdownContent(content, "my-project");

            result.Should().Contain("[Documentation](/my-project/docs/intro)");
            result.Should().Contain("import { api } from '/api/client';");
            result.Should().Contain("[More Info](/my-project/docs/advanced)");
        }

        #endregion

        #region RewriteMarkdownContent Tests - Path Preservation

        [TestMethod]
        public void RewriteMarkdownContent_RelativePath_PreservesOriginalPath()
        {
            var handler = new TestMarkdownHandler();

            var content = "[Link](./relative/path)";
            var result = handler.TestRewriteMarkdownContent(content, "my-project");

            result.Should().Be(content, "relative paths should not be rewritten");
        }

        [TestMethod]
        public void RewriteMarkdownContent_AlreadyPrefixedPath_PreservesOriginalPath()
        {
            var handler = new TestMarkdownHandler();

            var content = "[Link](/my-project/guides/intro)";
            var result = handler.TestRewriteMarkdownContent(content, "my-project");

            result.Should().Be(content, "already-prefixed paths should not be rewritten again");
        }

        [TestMethod]
        public void RewriteMarkdownContent_ExternalProtocolRelativeUrl_PreservesOriginalPath()
        {
            var handler = new TestMarkdownHandler();

            var content = "[External](//example.com/page)";
            var result = handler.TestRewriteMarkdownContent(content, "my-project");

            result.Should().Be(content, "protocol-relative URLs should not be rewritten");
        }

        [TestMethod]
        public void RewriteMarkdownContent_EmptyContent_ReturnsEmpty()
        {
            var handler = new TestMarkdownHandler();

            var result = handler.TestRewriteMarkdownContent("", "my-project");

            result.Should().Be("");
        }

        [TestMethod]
        public void RewriteMarkdownContent_NullOrWhitespaceDestination_ReturnsOriginalContent()
        {
            var handler = new TestMarkdownHandler();
            var content = "[Link](/guides/intro)";

            handler.TestRewriteMarkdownContent(content, "").Should().Be(content);
            handler.TestRewriteMarkdownContent(content, "   ").Should().Be(content);
        }

        #endregion

        #region RewritePath Tests

        [TestMethod]
        public void RewritePath_ImagesPath_InsertsDestinationAfterImages()
        {
            var handler = new TestMarkdownHandler();

            var result = handler.TestRewritePath("/images/logo.png", "my-project");

            result.Should().Be("/images/my-project/logo.png");
        }

        [TestMethod]
        public void RewritePath_SnippetsPath_InsertsDestinationAfterSnippets()
        {
            var handler = new TestMarkdownHandler();

            var result = handler.TestRewritePath("/snippets/component.jsx", "my-project");

            result.Should().Be("/snippets/my-project/component.jsx");
        }

        [TestMethod]
        public void RewritePath_PagePath_InsertsDestinationAtRoot()
        {
            var handler = new TestMarkdownHandler();

            var result = handler.TestRewritePath("/guides/overview", "my-project");

            result.Should().Be("/my-project/guides/overview");
        }

        [TestMethod]
        public void RewritePath_AlreadyPrefixed_ReturnsOriginal()
        {
            var handler = new TestMarkdownHandler();

            var result = handler.TestRewritePath("/my-project/guides/overview", "my-project");

            result.Should().Be("/my-project/guides/overview");
        }

        [TestMethod]
        public void RewritePath_ProtocolRelativeUrl_ReturnsOriginal()
        {
            var handler = new TestMarkdownHandler();

            var result = handler.TestRewritePath("//cdn.example.com/image.png", "my-project");

            result.Should().Be("//cdn.example.com/image.png");
        }

        [TestMethod]
        public void RewritePath_RelativePath_ReturnsOriginal()
        {
            var handler = new TestMarkdownHandler();

            var result = handler.TestRewritePath("./local/file.md", "my-project");

            result.Should().Be("./local/file.md");
        }

        [TestMethod]
        public void RewritePath_EmptyOrNull_ReturnsOriginal()
        {
            var handler = new TestMarkdownHandler();

            handler.TestRewritePath("", "my-project").Should().Be("");
            handler.TestRewritePath("   ", "my-project").Should().Be("   ");
        }

        #endregion

        #region IsInsideCodeBlock Tests

        [TestMethod]
        public void IsInsideCodeBlock_OutsideCodeBlock_ReturnsFalse()
        {
            var handler = new TestMarkdownHandler();

            var content = """
                Regular content here.

                ```js
                code here
                ```

                More content.
                """;

            // Position at "Regular" - outside code block
            handler.TestIsInsideCodeBlock(content, 0).Should().BeFalse();
            // Position at "More content" - outside code block
            handler.TestIsInsideCodeBlock(content, content.LastIndexOf("More")).Should().BeFalse();
        }

        [TestMethod]
        public void IsInsideCodeBlock_InsideCodeBlock_ReturnsTrue()
        {
            var handler = new TestMarkdownHandler();

            var content = """
                Before code.

                ```js
                code here
                ```

                After code.
                """;

            // Position at "code here" - inside code block
            var codePosition = content.IndexOf("code here");
            handler.TestIsInsideCodeBlock(content, codePosition).Should().BeTrue();
        }

        #endregion

        #region ProcessAsync Integration Tests

        [TestMethod]
        public async Task ProcessAsync_CopiesAndRewritesMarkdownFiles()
        {
            // Arrange
            var testDir = Path.Combine(Path.GetTempPath(), $"MarkdownHandlerTest_{System.Guid.NewGuid():N}");
            var sourceDir = Path.Combine(testDir, "source");
            var destDir = Path.Combine(testDir, "dest");

            try
            {
                // Create source structure
                Directory.CreateDirectory(Path.Combine(sourceDir, "guides"));
                Directory.CreateDirectory(Path.Combine(sourceDir, "images"));

                // Create a markdown file with paths to rewrite
                var mdContent = """
                    # Guide

                    ![Logo](/images/logo.png)

                    [Home](/index)
                    """;
                File.WriteAllText(Path.Combine(sourceDir, "guides", "intro.md"), mdContent);

                // Create an image file
                File.WriteAllBytes(Path.Combine(sourceDir, "images", "logo.png"), new byte[] { 0x89, 0x50, 0x4E, 0x47 });

                var reference = new DocumentationReference
                {
                    DocumentationRoot = sourceDir,
                    DestinationPath = "my-lib",
                    DocumentationType = SupportedDocumentationType.Generic
                };

                var handler = new MarkdownDocReferenceHandler();

                // Act
                await handler.ProcessAsync(reference, destDir);

                // Assert - Content file should be copied with rewritten paths
                var copiedMd = Path.Combine(destDir, "my-lib", "guides", "intro.md");
                File.Exists(copiedMd).Should().BeTrue("markdown file should be copied");

                var rewrittenContent = File.ReadAllText(copiedMd);
                rewrittenContent.Should().Contain("/images/my-lib/logo.png", "image path should be rewritten");
                rewrittenContent.Should().Contain("/my-lib/index", "link path should be rewritten");

                // Assert - Image should be relocated to central location
                var relocatedImage = Path.Combine(destDir, "images", "my-lib", "logo.png");
                File.Exists(relocatedImage).Should().BeTrue("image should be relocated to /images/{dest}/");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, recursive: true);
                }
            }
        }

        [TestMethod]
        public async Task ProcessAsync_RelocatesSnippetsDirectory()
        {
            // Arrange
            var testDir = Path.Combine(Path.GetTempPath(), $"MarkdownHandlerTest_{System.Guid.NewGuid():N}");
            var sourceDir = Path.Combine(testDir, "source");
            var destDir = Path.Combine(testDir, "dest");

            try
            {
                // Create source structure with snippets
                Directory.CreateDirectory(Path.Combine(sourceDir, "snippets"));

                File.WriteAllText(Path.Combine(sourceDir, "snippets", "example.txt"), "snippet content");

                var reference = new DocumentationReference
                {
                    DocumentationRoot = sourceDir,
                    DestinationPath = "my-lib",
                    DocumentationType = SupportedDocumentationType.Generic
                };

                var handler = new MarkdownDocReferenceHandler();

                // Act
                await handler.ProcessAsync(reference, destDir);

                // Assert - Snippet should be relocated to central location
                var relocatedSnippet = Path.Combine(destDir, "snippets", "my-lib", "example.txt");
                File.Exists(relocatedSnippet).Should().BeTrue("snippet should be relocated to /snippets/{dest}/");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, recursive: true);
                }
            }
        }

        [TestMethod]
        public async Task ProcessAsync_OnlyRewritesMdFiles()
        {
            // Arrange
            var testDir = Path.Combine(Path.GetTempPath(), $"MarkdownHandlerTest_{System.Guid.NewGuid():N}");
            var sourceDir = Path.Combine(testDir, "source");
            var destDir = Path.Combine(testDir, "dest");

            try
            {
                Directory.CreateDirectory(sourceDir);

                // Create markdown file (should be rewritten)
                File.WriteAllText(Path.Combine(sourceDir, "guide.md"), "[Link](/index)");

                // Create non-markdown file (should NOT be rewritten)
                File.WriteAllText(Path.Combine(sourceDir, "data.json"), "{\"link\": \"/index\"}");

                var reference = new DocumentationReference
                {
                    DocumentationRoot = sourceDir,
                    DestinationPath = "my-lib",
                    DocumentationType = SupportedDocumentationType.Generic
                };

                var handler = new MarkdownDocReferenceHandler();

                // Act
                await handler.ProcessAsync(reference, destDir);

                // Assert - MD file should have rewritten paths
                var mdContent = File.ReadAllText(Path.Combine(destDir, "my-lib", "guide.md"));
                mdContent.Should().Contain("/my-lib/index");

                // Assert - JSON file should NOT have rewritten paths
                var jsonContent = File.ReadAllText(Path.Combine(destDir, "my-lib", "data.json"));
                jsonContent.Should().Contain("\"/index\"", "non-markdown files should not be rewritten");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, recursive: true);
                }
            }
        }

        #endregion

        #region Test Helper Class

        /// <summary>
        /// Test helper that exposes protected methods of <see cref="MarkdownDocReferenceHandler"/> for testing.
        /// </summary>
        private class TestMarkdownHandler : MarkdownDocReferenceHandler
        {

            public string TestRewriteMarkdownContent(string content, string destinationPath)
            {
                return RewriteMarkdownContent(content, destinationPath);
            }

            public string TestRewritePath(string originalPath, string destinationPath)
            {
                return RewritePath(originalPath, destinationPath);
            }

            public bool TestIsInsideCodeBlock(string content, int position)
            {
                return IsInsideCodeBlock(content, position);
            }

        }

        #endregion

    }

}
