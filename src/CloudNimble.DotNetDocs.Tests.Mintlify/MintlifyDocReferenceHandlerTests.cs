using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Mintlify;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CloudNimble.DotNetDocs.Tests.Mintlify
{

    /// <summary>
    /// Tests for <see cref="MintlifyDocReferenceHandler"/>, covering Mintlify-specific
    /// content rewriting patterns including ES imports, JSX attributes, and CSS url().
    /// </summary>
    [TestClass]
    public class MintlifyDocReferenceHandlerTests : DotNetDocsTestBase
    {

        #region Private Fields

        private string? _tempDirectory;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"MintlifyDocRefTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDirectory);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (_tempDirectory is not null && Directory.Exists(_tempDirectory))
            {
                try
                {
                    Directory.Delete(_tempDirectory, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors in tests
                }
            }
        }

        #endregion

        #region DocumentationType Tests

        [TestMethod]
        public void DocumentationType_ReturnsMintlify()
        {
            var handler = new MintlifyDocReferenceHandler();

            handler.DocumentationType.Should().Be(SupportedDocumentationType.Mintlify);
        }

        #endregion

        #region RewriteMintlifyContent Tests - ES Import Rewriting

        [TestMethod]
        public void RewriteMintlifyContent_EsImportDefault_RewritesPath()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = "import Component from '/snippets/Component.jsx'";
            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Be("import Component from '/snippets/my-project/Component.jsx'");
        }

        [TestMethod]
        public void RewriteMintlifyContent_EsImportNamed_RewritesPath()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = "import { Button, Card } from '/snippets/components.jsx'";
            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Be("import { Button, Card } from '/snippets/my-project/components.jsx'");
        }

        [TestMethod]
        public void RewriteMintlifyContent_EsImportNamespace_RewritesPath()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = "import * as Utils from '/snippets/utils.jsx'";
            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Be("import * as Utils from '/snippets/my-project/utils.jsx'");
        }

        [TestMethod]
        public void RewriteMintlifyContent_EsImportWithSingleQuotes_RewritesPath()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = "import Component from '/snippets/Component.jsx'";
            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Contain("/snippets/my-project/Component.jsx");
        }

        [TestMethod]
        public void RewriteMintlifyContent_MultipleImports_RewritesAll()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = """
                import Header from '/snippets/Header.jsx'
                import Footer from '/snippets/Footer.jsx'
                """;

            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Contain("/snippets/my-project/Header.jsx");
            result.Should().Contain("/snippets/my-project/Footer.jsx");
        }

        #endregion

        #region RewriteMintlifyContent Tests - JSX src Rewriting

        [TestMethod]
        public void RewriteMintlifyContent_JsxSrcAttribute_RewritesPath()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = "<img src=\"/images/logo.png\" />";
            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Be("<img src=\"/images/my-project/logo.png\" />");
        }

        [TestMethod]
        public void RewriteMintlifyContent_JsxSrcWithSingleQuotes_RewritesPath()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = "<img src='/images/banner.jpg' />";
            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Contain("/images/my-project/banner.jpg");
        }

        [TestMethod]
        public void RewriteMintlifyContent_JsxSrcWithSpaces_RewritesPath()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = "<img src = \"/images/icon.svg\" />";
            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Contain("/images/my-project/icon.svg");
        }

        #endregion

        #region RewriteMintlifyContent Tests - JSX href Rewriting

        [TestMethod]
        public void RewriteMintlifyContent_JsxHrefAttribute_RewritesPath()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = "<a href=\"/guides/quickstart\">Get Started</a>";
            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Be("<a href=\"/my-project/guides/quickstart\">Get Started</a>");
        }

        [TestMethod]
        public void RewriteMintlifyContent_JsxHrefWithAnchor_PreservesAnchor()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = "<a href=\"/guides/install#prerequisites\">Install</a>";
            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Be("<a href=\"/my-project/guides/install#prerequisites\">Install</a>");
        }

        [TestMethod]
        public void RewriteMintlifyContent_JsxHrefWithQueryString_PreservesQueryString()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = "<a href=\"/api/reference?version=2\">API</a>";
            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Be("<a href=\"/my-project/api/reference?version=2\">API</a>");
        }

        #endregion

        #region RewriteMintlifyContent Tests - CSS url() Rewriting

        [TestMethod]
        public void RewriteMintlifyContent_CssUrl_RewritesPath()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = "background: url(/images/bg.svg);";
            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Be("background: url(/images/my-project/bg.svg);");
        }

        [TestMethod]
        public void RewriteMintlifyContent_CssUrlWithSingleQuotes_RewritesPath()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = "background: url('/images/pattern.png');";
            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Contain("/images/my-project/pattern.png");
        }

        [TestMethod]
        public void RewriteMintlifyContent_CssUrlWithDoubleQuotes_RewritesPath()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = "background: url(\"/images/texture.jpg\");";
            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Contain("/images/my-project/texture.jpg");
        }

        #endregion

        #region RewriteMintlifyContent Tests - Code Block Preservation

        [TestMethod]
        public void RewriteMintlifyContent_PathInCodeBlock_PreservesOriginalPath()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = """
                ```jsx
                import Component from '/snippets/Component.jsx'
                ```
                """;

            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Contain("from '/snippets/Component.jsx'", "paths inside code blocks should not be rewritten");
        }

        [TestMethod]
        public void RewriteMintlifyContent_MixedContentWithCodeBlock_OnlyRewritesOutsideCodeBlock()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = """
                import Header from '/snippets/Header.jsx'

                ```jsx
                import Example from '/snippets/Example.jsx'
                ```

                import Footer from '/snippets/Footer.jsx'
                """;

            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Contain("from '/snippets/my-project/Header.jsx'");
            result.Should().Contain("from '/snippets/Example.jsx'"); // Inside code block - unchanged
            result.Should().Contain("from '/snippets/my-project/Footer.jsx'");
        }

        #endregion

        #region RewriteMintlifyContent Tests - Combined Markdown and Mintlify Patterns

        [TestMethod]
        public void RewriteMintlifyContent_MarkdownImage_RewritesPath()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = "![Screenshot](/images/screenshot.png)";
            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Be("![Screenshot](/images/my-project/screenshot.png)");
        }

        [TestMethod]
        public void RewriteMintlifyContent_MarkdownLink_RewritesPath()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = "[Learn More](/docs/introduction)";
            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Be("[Learn More](/my-project/docs/introduction)");
        }

        [TestMethod]
        public void RewriteMintlifyContent_MixedMarkdownAndJsx_RewritesAll()
        {
            var handler = new MintlifyDocReferenceHandler();

            var content = """
                import Logo from '/snippets/Logo.jsx'

                ![Banner](/images/banner.png)

                [Read the docs](/docs/intro)

                <Card href="/guides/tutorial" />
                """;

            var result = handler.RewriteMintlifyContent(content, "my-project");

            result.Should().Contain("from '/snippets/my-project/Logo.jsx'");
            result.Should().Contain("](/images/my-project/banner.png)");
            result.Should().Contain("](/my-project/docs/intro)");
            result.Should().Contain("href=\"/my-project/guides/tutorial\"");
        }

        #endregion

        #region ProcessAsync Integration Tests

        [TestMethod]
        public async Task ProcessAsync_CopiesAndRewritesContent()
        {
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destRootDir = Path.Combine(_tempDirectory!, "docs");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(Path.Combine(sourceDir, "images"));

            File.WriteAllText(Path.Combine(sourceDir, "index.mdx"), """
                import Component from '/snippets/Component.jsx'

                ![Logo](/images/logo.png)

                [Getting Started](/guides/quickstart)
                """);
            File.WriteAllText(Path.Combine(sourceDir, "images", "logo.png"), "PNG_DATA");

            var handler = new MintlifyDocReferenceHandler();
            var reference = new DocumentationReference
            {
                DocumentationRoot = sourceDir,
                DestinationPath = "my-lib",
                DocumentationType = SupportedDocumentationType.Mintlify
            };

            await handler.ProcessAsync(reference, destRootDir);

            // Content should be copied to destRootDir/my-lib/
            var destFile = Path.Combine(destRootDir, "my-lib", "index.mdx");
            File.Exists(destFile).Should().BeTrue("content file should be copied");

            var content = await File.ReadAllTextAsync(destFile);
            content.Should().Contain("/snippets/my-lib/Component.jsx");
            content.Should().Contain("/images/my-lib/logo.png");
            content.Should().Contain("/my-lib/guides/quickstart");

            // Images should be relocated to destRootDir/images/my-lib/
            File.Exists(Path.Combine(destRootDir, "images", "my-lib", "logo.png")).Should().BeTrue("image should be relocated");
        }

        [TestMethod]
        public async Task ProcessAsync_RelocatesImagesDirectory()
        {
            var sourceDir = Path.Combine(_tempDirectory!, "source2");
            var destRootDir = Path.Combine(_tempDirectory!, "docs2");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(Path.Combine(sourceDir, "images"));
            Directory.CreateDirectory(Path.Combine(sourceDir, "images", "screenshots"));

            File.WriteAllText(Path.Combine(sourceDir, "images", "logo.png"), "PNG1");
            File.WriteAllText(Path.Combine(sourceDir, "images", "screenshots", "app.png"), "PNG2");

            var handler = new MintlifyDocReferenceHandler();
            var reference = new DocumentationReference
            {
                DocumentationRoot = sourceDir,
                DestinationPath = "my-lib",
                DocumentationType = SupportedDocumentationType.Mintlify
            };

            await handler.ProcessAsync(reference, destRootDir);

            File.Exists(Path.Combine(destRootDir, "images", "my-lib", "logo.png")).Should().BeTrue();
            File.Exists(Path.Combine(destRootDir, "images", "my-lib", "screenshots", "app.png")).Should().BeTrue();
        }

        [TestMethod]
        public async Task ProcessAsync_RelocatesSnippetsDirectory()
        {
            var sourceDir = Path.Combine(_tempDirectory!, "source3");
            var destRootDir = Path.Combine(_tempDirectory!, "docs3");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(Path.Combine(sourceDir, "snippets"));

            File.WriteAllText(Path.Combine(sourceDir, "snippets", "Component.jsx"), "export default function Component() {}");

            var handler = new MintlifyDocReferenceHandler();
            var reference = new DocumentationReference
            {
                DocumentationRoot = sourceDir,
                DestinationPath = "my-lib",
                DocumentationType = SupportedDocumentationType.Mintlify
            };

            await handler.ProcessAsync(reference, destRootDir);

            File.Exists(Path.Combine(destRootDir, "snippets", "my-lib", "Component.jsx")).Should().BeTrue();
        }

        [TestMethod]
        public async Task ProcessAsync_ExcludesMintlifyConfigFiles()
        {
            var sourceDir = Path.Combine(_tempDirectory!, "source4");
            var destRootDir = Path.Combine(_tempDirectory!, "docs4");
            Directory.CreateDirectory(sourceDir);

            File.WriteAllText(Path.Combine(sourceDir, "index.mdx"), "# Welcome");
            File.WriteAllText(Path.Combine(sourceDir, "docs.json"), "{}");
            File.WriteAllText(Path.Combine(sourceDir, "style.css"), "body {}");
            File.WriteAllText(Path.Combine(sourceDir, "test.mdz"), "zone");
            File.WriteAllText(Path.Combine(sourceDir, "Project.docsproj"), "<Project />");

            var handler = new MintlifyDocReferenceHandler();
            var reference = new DocumentationReference
            {
                DocumentationRoot = sourceDir,
                DestinationPath = "my-lib",
                DocumentationType = SupportedDocumentationType.Mintlify
            };

            await handler.ProcessAsync(reference, destRootDir);

            File.Exists(Path.Combine(destRootDir, "my-lib", "index.mdx")).Should().BeTrue("content should be copied");
            File.Exists(Path.Combine(destRootDir, "my-lib", "docs.json")).Should().BeFalse("docs.json should be excluded");
            File.Exists(Path.Combine(destRootDir, "my-lib", "style.css")).Should().BeFalse("CSS should be excluded");
            File.Exists(Path.Combine(destRootDir, "my-lib", "test.mdz")).Should().BeFalse("mdz should be excluded");
            File.Exists(Path.Combine(destRootDir, "my-lib", "Project.docsproj")).Should().BeFalse("docsproj should be excluded");
        }

        #endregion

    }

}
