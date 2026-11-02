using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        /// <summary>
        /// Tests that contextual options and display are loaded and survive save.
        /// </summary>
        [TestMethod]
        public void Load_Contextual_ParsesOptionsAndDisplay()
        {
            var manager = LoadDocument("""
                "contextual": {
                    "options": ["copy", "claude"],
                    "display": "toc"
                }
                """);

            manager.Configuration!.Contextual!.Display.Should().Be("toc");
            manager.Configuration.Contextual.Options.Should().Contain("copy");
            manager.Configuration.Contextual.Options.Should().Contain("claude");

            var reloaded = SaveAndReload(manager);
            reloaded.Contextual!.Display.Should().Be("toc");
        }

        /// <summary>
        /// Tests that custom contextual option objects with string and query hrefs load and save.
        /// </summary>
        [TestMethod]
        public void Load_Contextual_ParsesCustomOptionObjects()
        {
            var manager = LoadDocument("""
                "contextual": {
                    "options": [
                        "copy",
                        {
                            "title": "Ask Grok",
                            "description": "Open in Grok",
                            "href": "https://grok.com"
                        },
                        {
                            "title": "Search",
                            "href": {
                                "base": "https://x.com/search",
                                "query": [
                                    { "key": "q", "value": "docs" }
                                ]
                            }
                        }
                    ]
                }
                """);

            var options = manager.Configuration!.Contextual!.Options;
            options.Should().HaveCount(3);
            options![0].Should().Be("copy");
            var grok = options[1].Should().BeOfType<ContextualOptionConfig>().Subject;
            grok.Title.Should().Be("Ask Grok");
            grok.Href!.Url.Should().Be("https://grok.com");
            var search = options[2].Should().BeOfType<ContextualOptionConfig>().Subject;
            search.Href!.Base.Should().Be("https://x.com/search");
            search.Href.Query.Should().ContainSingle().Which.Key.Should().Be("q");

            var reloaded = SaveAndReload(manager);
            reloaded.Contextual!.Options.Should().HaveCount(3);
            reloaded.Contextual.Options![1].Should().BeOfType<ContextualOptionConfig>().Which.Href!.Url.Should().Be("https://grok.com");
        }

    }

}
