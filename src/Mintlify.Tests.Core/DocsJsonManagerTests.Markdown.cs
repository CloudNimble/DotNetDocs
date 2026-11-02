using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        /// <summary>
        /// Tests that markdown.instructions as a string is loaded and survives save.
        /// </summary>
        [TestMethod]
        public void Load_MarkdownInstructionsString_ParsesAndRoundTrips()
        {
            var manager = LoadDocument("""
                "markdown": {
                    "schema": false,
                    "instructions": "Always cite the API version."
                }
                """);

            manager.Configuration!.Markdown.Should().NotBeNull();
            manager.Configuration.Markdown!.Schema.Should().BeFalse();
            manager.Configuration.Markdown.Instructions.Should().Equal("Always cite the API version.");

            var reloaded = SaveAndReload(manager);
            reloaded.Markdown!.Schema.Should().BeFalse();
            reloaded.Markdown.Instructions.Should().Equal("Always cite the API version.");
        }

        /// <summary>
        /// Tests that markdown.instructions as an array is loaded.
        /// </summary>
        [TestMethod]
        public void Load_MarkdownInstructionsArray_ParsesAllItems()
        {
            var manager = LoadDocument("""
                "markdown": {
                    "instructions": [
                        "Always cite the API version.",
                        "Prefer the TypeScript SDK in examples."
                    ]
                }
                """);

            manager.Configuration!.Markdown!.Instructions.Should().Equal(
                "Always cite the API version.",
                "Prefer the TypeScript SDK in examples.");
        }

    }

}
