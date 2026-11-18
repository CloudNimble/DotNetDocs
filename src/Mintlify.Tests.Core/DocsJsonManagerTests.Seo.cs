using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        /// <summary>
        /// Tests that SEO paths, organization, and trailingSlash are loaded and survive save.
        /// </summary>
        [TestMethod]
        public void Load_Seo_ParsesPathsOrganizationAndTrailingSlash()
        {
            var manager = LoadDocument("""
                "seo": {
                    "indexing": "all",
                    "trailingSlash": true,
                    "paths": [
                        {
                            "path": "guides/v2",
                            "metatags": {
                                "robots": "noindex"
                            }
                        }
                    ],
                    "organization": {
                        "id": "https://example.com/#organization",
                        "name": "Example Co.",
                        "legalName": "Example Co. Inc.",
                        "url": "https://example.com",
                        "logo": "https://example.com/logo.png",
                        "sameAs": ["https://github.com/example"]
                    }
                }
                """);

            var seo = manager.Configuration!.Seo;
            seo.Should().NotBeNull();
            seo!.Indexing.Should().Be("all");
            seo.TrailingSlash.Should().BeTrue();
            seo.Paths.Should().ContainSingle();
            seo.Paths![0].Path.Should().Be("guides/v2");
            seo.Paths[0].Metatags.Should().ContainKey("robots").WhoseValue.Should().Be("noindex");
            seo.Organization!.LegalName.Should().Be("Example Co. Inc.");
            seo.Organization.SameAs.Should().Contain("https://github.com/example");

            var reloaded = SaveAndReload(manager);
            reloaded.Seo!.TrailingSlash.Should().BeTrue();
            reloaded.Seo.Organization!.LegalName.Should().Be("Example Co. Inc.");
        }

    }

}
