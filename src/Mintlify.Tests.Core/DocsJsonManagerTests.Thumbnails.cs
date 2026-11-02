using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        /// <summary>
        /// Tests that thumbnail preset, surface, eyebrow, and fonts are loaded and survive save.
        /// </summary>
        [TestMethod]
        public void Load_Thumbnails_ParsesPresetSurfaceEyebrowAndFonts()
        {
            var manager = LoadDocument("""
                "thumbnails": {
                    "appearance": "dark",
                    "preset": "headline",
                    "surface": "brand",
                    "eyebrow": {
                        "source": "tab",
                        "style": "pill",
                        "color": "#419AC5"
                    },
                    "fonts": {
                        "family": "Inter",
                        "source": "https://example.com/inter.woff",
                        "format": "woff"
                    }
                }
                """);

            var thumbnails = manager.Configuration!.Thumbnails;
            thumbnails.Should().NotBeNull();
            thumbnails!.Appearance.Should().Be("dark");
            thumbnails.Preset.Should().Be("headline");
            thumbnails.Surface.Should().Be("brand");
            thumbnails.Eyebrow!.Source.Should().Be("tab");
            thumbnails.Eyebrow.Style.Should().Be("pill");
            thumbnails.Fonts!.Family.Should().Be("Inter");
            thumbnails.Fonts.Format.Should().Be("woff");

            var reloaded = SaveAndReload(manager);
            reloaded.Thumbnails!.Preset.Should().Be("headline");
            reloaded.Thumbnails.Fonts!.Format.Should().Be("woff");
        }

    }

}
