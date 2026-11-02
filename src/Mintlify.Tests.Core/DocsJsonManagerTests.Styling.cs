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
            manager.Configuration.Styling.Codeblocks!.Theme.Should().Be("dark");
            manager.Configuration.Styling.Eyebrows.Should().Be("breadcrumbs");

            var reloaded = SaveAndReload(manager);
            reloaded.Styling!.Latex.Should().BeTrue();
            reloaded.Styling.Codeblocks!.Theme.Should().Be("dark");
        }

        /// <summary>
        /// Tests that Shiki theme objects and custom languages load and save as objects.
        /// </summary>
        [TestMethod]
        public void Load_Styling_ParsesShikiThemeObject()
        {
            var manager = LoadDocument("""
                "styling": {
                    "codeblocks": {
                        "theme": {
                            "light": "github-light",
                            "dark": "github-dark"
                        },
                        "languages": {
                            "custom": ["grammars/bicep.json"]
                        }
                    }
                }
                """);

            var codeblocks = manager.Configuration!.Styling!.Codeblocks;
            codeblocks.Should().NotBeNull();
            codeblocks!.ThemePair!.Light.Should().Be("github-light");
            codeblocks.ThemePair.Dark.Should().Be("github-dark");
            codeblocks.Languages!.Custom.Should().Equal("grammars/bicep.json");

            var reloaded = SaveAndReload(manager);
            reloaded.Styling!.Codeblocks!.ThemePair!.Dark.Should().Be("github-dark");
            reloaded.Styling.Codeblocks.Languages!.Custom.Should().Equal("grammars/bicep.json");
        }

    }

}
