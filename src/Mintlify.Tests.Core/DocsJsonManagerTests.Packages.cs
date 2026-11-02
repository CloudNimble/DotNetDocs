using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        /// <summary>
        /// Tests that packages configuration is loaded and survives save.
        /// </summary>
        [TestMethod]
        public void Load_Packages_ParsesAndRoundTrips()
        {
            var manager = LoadDocument("""
                "packages": {
                    "css": ["@acme/ui/dist/styles.css"],
                    "provider": "/snippets/provider.jsx",
                    "registries": {
                        "@acme": "https://npm.acme.com/"
                    },
                    "ssr": false
                }
                """);

            var packages = manager.Configuration!.Packages;
            packages.Should().NotBeNull();
            packages!.Css.Should().Contain("@acme/ui/dist/styles.css");
            packages.Provider.Should().Be("/snippets/provider.jsx");
            packages.Registries.Should().ContainKey("@acme").WhoseValue.Should().Be("https://npm.acme.com/");
            packages.Ssr.Should().BeFalse();

            var reloaded = SaveAndReload(manager);
            reloaded.Packages!.Provider.Should().Be("/snippets/provider.jsx");
            reloaded.Packages.Ssr.Should().BeFalse();
        }

    }

}
