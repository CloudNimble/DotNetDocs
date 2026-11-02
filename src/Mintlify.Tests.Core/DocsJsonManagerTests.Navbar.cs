using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        /// <summary>
        /// Tests that navbar links including type are loaded and survive save.
        /// </summary>
        [TestMethod]
        public void Load_Navbar_ParsesLinkType()
        {
            var manager = LoadDocument("""
                "navbar": {
                    "links": [
                        {
                            "label": "GitHub",
                            "href": "https://github.com/example",
                            "type": "github"
                        }
                    ]
                }
                """);

            var link = manager.Configuration!.Navbar!.Links.Should().ContainSingle().Subject;
            link.Label.Should().Be("GitHub");
            link.Href.Should().Be("https://github.com/example");
            link.Type.Should().Be("github");

            var reloaded = SaveAndReload(manager);
            reloaded.Navbar!.Links.Should().ContainSingle().Which.Type.Should().Be("github");
        }

    }

}
