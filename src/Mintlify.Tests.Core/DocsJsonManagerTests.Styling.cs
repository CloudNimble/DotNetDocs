using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        /// <summary>
        /// Tests that styling.latex is loaded and survives save.
        /// </summary>
        [TestMethod]
        public void Load_StylingLatex_ParsesAndRoundTrips()
        {
            var manager = LoadDocument("""
                "styling": {
                    "latex": true,
                    "codeblocks": "dark",
                    "eyebrows": "breadcrumbs"
                }
                """);

            manager.Configuration!.Styling!.Latex.Should().BeTrue();
            manager.Configuration.Styling.Codeblocks.Should().Be("dark");
            manager.Configuration.Styling.Eyebrows.Should().Be("breadcrumbs");

            var reloaded = SaveAndReload(manager);
            reloaded.Styling!.Latex.Should().BeTrue();
            reloaded.Styling.Codeblocks.Should().Be("dark");
        }

    }

}
