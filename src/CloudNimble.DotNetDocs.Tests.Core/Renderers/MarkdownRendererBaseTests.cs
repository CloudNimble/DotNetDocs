using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Renderers;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core.Renderers
{

    /// <summary>
    /// Tests for the <see cref="MarkdownRendererBase"/> class.
    /// </summary>
    [TestClass]
    public class MarkdownRendererBaseTests : DotNetDocsTestBase
    {

        #region Test Classes

        private sealed class TestMarkdownRenderer(ProjectContext? context = null) : MarkdownRendererBase(context)
        {
            public static string? HeaderText(string? content, string headerText) => GetHeaderText(content, headerText);
        }

        #endregion

        #region GetHeaderText Tests

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void GetHeaderText_ReturnsHeader_WhenContentIsMissing(string? content)
        {
            TestMarkdownRenderer.HeaderText(content, "## Summary").Should().Be("## Summary");
        }

        [TestMethod]
        public void GetHeaderText_ReturnsHeader_WhenContentDoesNotContainIt()
        {
            TestMarkdownRenderer.HeaderText("Explains what the type does.", "## Summary").Should().Be("## Summary");
        }

        [TestMethod]
        [DataRow("## Summary\n\nExplains what the type does.")]
        [DataRow("Intro text.\n\n## Summary\n\nMore text.")]
        public void GetHeaderText_ReturnsNull_WhenContentAlreadyContainsHeader(string content)
        {
            TestMarkdownRenderer.HeaderText(content, "## Summary").Should().BeNull();
        }

        [TestMethod]
        public void GetHeaderText_IsCaseSensitive()
        {
            TestMarkdownRenderer.HeaderText("## summary\n\nText.", "## Summary").Should().Be("## Summary");
        }

        #endregion

    }

}
