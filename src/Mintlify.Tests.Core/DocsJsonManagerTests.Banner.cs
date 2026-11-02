using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        /// <summary>
        /// Tests that banner type and color are loaded and survive save.
        /// </summary>
        [TestMethod]
        public void Load_Banner_ParsesTypeAndColor()
        {
            var manager = LoadDocument("""
                "banner": {
                    "content": "Scheduled maintenance tonight.",
                    "dismissible": true,
                    "type": "warning",
                    "color": {
                        "light": "#B45309",
                        "dark": "#F59E0B"
                    }
                }
                """);

            var banner = manager.Configuration!.Banner;
            banner.Should().NotBeNull();
            banner!.Content.Should().Be("Scheduled maintenance tonight.");
            banner.Dismissible.Should().BeTrue();
            banner.Type.Should().Be("warning");
            banner.Color!.Light.Should().Be("#B45309");
            banner.Color.Dark.Should().Be("#F59E0B");

            var reloaded = SaveAndReload(manager);
            reloaded.Banner!.Type.Should().Be("warning");
            reloaded.Banner.Color!.Light.Should().Be("#B45309");
        }

    }

}
