using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        /// <summary>
        /// Tests that sequoia, luma, and tabler are accepted when loading a docs.json.
        /// </summary>
        [TestMethod]
        public void Load_SequoiaThemeAndTablerIcons_DoesNotWarn()
        {
            var json = """
                {
                    "name": "Test Documentation",
                    "theme": "sequoia",
                    "colors": { "primary": "#419AC5" },
                    "navigation": ["index"],
                    "icons": { "library": "tabler" }
                }
                """;

            var manager = new DocsJsonManager();
            manager.Load(json);

            manager.Configuration!.Theme.Should().Be("sequoia");
            manager.Configuration.Icons!.Library.Should().Be("tabler");
            manager.ConfigurationErrors.Should().NotContain(e => e.ErrorText.Contains("theme") || e.ErrorText.Contains("library"));
        }

        /// <summary>
        /// Tests that an invalid themeVersion is reported when loading.
        /// </summary>
        [TestMethod]
        public void Load_InvalidThemeVersion_AddsValidationWarning()
        {
            var manager = LoadDocument("\"themeVersion\": 3");

            manager.ConfigurationErrors.Should().Contain(e => e.ErrorText.Contains("themeVersion"));
        }

        /// <summary>
        /// Tests that themeVersion is rejected on a non-mint theme.
        /// </summary>
        [TestMethod]
        public void Load_ThemeVersionOnMaple_AddsValidationWarning()
        {
            var json = """
                {
                    "name": "Test Documentation",
                    "theme": "maple",
                    "themeVersion": 2,
                    "colors": { "primary": "#419AC5" },
                    "navigation": ["index"]
                }
                """;

            var manager = new DocsJsonManager();
            manager.Load(json);

            manager.ConfigurationErrors.Should().Contain(e => e.ErrorText.Contains("themeVersion"));
        }

        /// <summary>
        /// Tests that public and themeVersion survive save.
        /// </summary>
        [TestMethod]
        public void Load_PublicAndThemeVersion_RoundTrips()
        {
            var manager = LoadDocument("""
                "public": true,
                "themeVersion": 2,
                "variables": {
                    "apiVersion": "v2"
                }
                """);

            manager.Configuration!.Public.Should().BeTrue();
            manager.Configuration.ThemeVersion.Should().Be(2);
            manager.Configuration.Variables.Should().ContainKey("apiVersion").WhoseValue.Should().Be("v2");

            var reloaded = SaveAndReload(manager);
            reloaded.Public.Should().BeTrue();
            reloaded.ThemeVersion.Should().Be(2);
            reloaded.Variables.Should().ContainKey("apiVersion").WhoseValue.Should().Be("v2");
        }

        /// <summary>
        /// Tests that an invalid banner type is reported when loading.
        /// </summary>
        [TestMethod]
        public void Load_InvalidBannerType_AddsValidationWarning()
        {
            var manager = LoadDocument("""
                "banner": {
                    "content": "Hello",
                    "type": "urgent"
                }
                """);

            manager.ConfigurationErrors.Should().Contain(e => e.ErrorText.Contains("banner"));
        }

        /// <summary>
        /// Tests that an invalid thumbnail preset is reported when loading.
        /// </summary>
        [TestMethod]
        public void Load_InvalidThumbnailPreset_AddsValidationWarning()
        {
            var manager = LoadDocument("""
                "thumbnails": {
                    "preset": "cinematic"
                }
                """);

            manager.ConfigurationErrors.Should().Contain(e => e.ErrorText.Contains("preset"));
        }

        /// <summary>
        /// Tests that an invalid Mixpanel region is reported when loading.
        /// </summary>
        [TestMethod]
        public void Load_InvalidMixpanelRegion_AddsValidationWarning()
        {
            var manager = LoadDocument("""
                "integrations": {
                    "mixpanel": {
                        "projectToken": "token",
                        "region": "ap"
                    }
                }
                """);

            manager.ConfigurationErrors.Should().Contain(e => e.ErrorText.Contains("region"));
        }

        /// <summary>
        /// Tests that Transcend without a bundleId is reported when loading.
        /// </summary>
        [TestMethod]
        public void Load_TranscendWithoutBundleId_AddsValidationWarning()
        {
            var manager = LoadDocument("""
                "integrations": {
                    "transcend": {
                        "analyticsPurpose": "Analytics"
                    }
                }
                """);

            manager.ConfigurationErrors.Should().Contain(e => e.ErrorText.Contains("bundleId"));
        }

        /// <summary>
        /// Tests that a GraphQL spec ref without a source is reported when loading.
        /// </summary>
        [TestMethod]
        public void Load_GraphqlWithoutSource_AddsValidationWarning()
        {
            var json = """
                {
                    "name": "Test Documentation",
                    "theme": "mint",
                    "colors": { "primary": "#419AC5" },
                    "navigation": {
                        "graphql": { "directory": "api/graphql" },
                        "pages": ["index"]
                    }
                }
                """;

            var manager = new DocsJsonManager();
            manager.Load(json);

            manager.ConfigurationErrors.Should().Contain(e => e.ErrorText.Contains("graphql"));
        }

    }

}
