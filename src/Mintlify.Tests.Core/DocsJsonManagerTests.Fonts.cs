using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        /// <summary>
        /// Tests that fonts source and format are loaded and survive save.
        /// </summary>
        [TestMethod]
        public void Load_Fonts_ParsesSourceAndFormat()
        {
            var manager = LoadDocument("""
                "fonts": {
                    "family": "Inter",
                    "weight": 400,
                    "source": "https://fonts.example.com/inter.woff2",
                    "format": "woff2"
                }
                """);

            manager.Configuration!.Fonts!.Family.Should().Be("Inter");
            manager.Configuration.Fonts.Weight.Should().Be(400);
            manager.Configuration.Fonts.Source.Should().Be("https://fonts.example.com/inter.woff2");
            manager.Configuration.Fonts.Format.Should().Be("woff2");

            var reloaded = SaveAndReload(manager);
            reloaded.Fonts!.Format.Should().Be("woff2");
        }

    }

}
