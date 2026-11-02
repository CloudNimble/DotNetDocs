using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        /// <summary>
        /// Tests that navigation graphql and mcp spec refs are loaded and survive save.
        /// </summary>
        [TestMethod]
        public void Load_Navigation_ParsesGraphqlAndMcp()
        {
            var json = """
                {
                    "name": "Test Documentation",
                    "theme": "mint",
                    "colors": { "primary": "#419AC5" },
                    "navigation": {
                        "graphql": "schema.graphql",
                        "mcp": {
                            "source": "mcp.json",
                            "directory": "mcp-reference"
                        },
                        "pages": [
                            {
                                "group": "GraphQL API",
                                "graphql": {
                                    "source": "schema.graphql",
                                    "directory": "api/graphql"
                                },
                                "pages": ["graphql/overview"]
                            }
                        ]
                    }
                }
                """;

            var manager = new DocsJsonManager();
            manager.Load(json);

            var navigation = manager.Configuration!.Navigation;
            navigation.Graphql.Should().NotBeNull();
            navigation.Graphql!.Source.Should().Be("schema.graphql");
            navigation.Mcp.Should().NotBeNull();
            navigation.Mcp!.Source.Should().Be("mcp.json");
            navigation.Mcp.Directory.Should().Be("mcp-reference");

            var group = navigation.Pages.Should().ContainSingle().Which.Should().BeOfType<GroupConfig>().Subject;
            group.Graphql!.Source.Should().Be("schema.graphql");
            group.Graphql.Directory.Should().Be("api/graphql");

            var reloaded = SaveAndReload(manager);
            reloaded.Navigation.Graphql!.Source.Should().Be("schema.graphql");
            reloaded.Navigation.Mcp!.Directory.Should().Be("mcp-reference");
        }

        /// <summary>
        /// Tests that navigation pages accept link objects and sourceRef entries.
        /// </summary>
        [TestMethod]
        public void Load_Navigation_ParsesLinkAndSourceRefPages()
        {
            var json = """
                {
                    "name": "Test Documentation",
                    "theme": "mint",
                    "colors": { "primary": "#419AC5" },
                    "navigation": {
                        "pages": [
                            "index",
                            {
                                "type": "link",
                                "href": "https://example.com",
                                "title": "Example"
                            },
                            {
                                "sourceRef": "mintlify-source"
                            }
                        ]
                    }
                }
                """;

            var manager = new DocsJsonManager();
            manager.Load(json);

            manager.Configuration!.Navigation.Pages.Should().HaveCount(3);
            manager.Configuration.Navigation.Pages![0].Should().Be("index");
            var link = manager.Configuration.Navigation.Pages[1].Should().BeOfType<NavigationLinkConfig>().Subject;
            link.Href.Should().Be("https://example.com");
            link.Title.Should().Be("Example");
            manager.Configuration.Navigation.Pages[2].Should().BeOfType<SourceRefConfig>()
                .Which.SourceRef.Should().Be("mintlify-source");

            var reloaded = SaveAndReload(manager);
            reloaded.Navigation.Pages.Should().HaveCount(3);
            reloaded.Navigation.Pages![1].Should().BeOfType<NavigationLinkConfig>().Which.Href.Should().Be("https://example.com");
            reloaded.Navigation.Pages[2].Should().BeOfType<SourceRefConfig>().Which.SourceRef.Should().Be("mintlify-source");
        }

        /// <summary>
        /// Tests that sdk, directory, searchable, tab align, and product display name round-trip.
        /// </summary>
        [TestMethod]
        public void Load_Navigation_ParsesSdkDirectorySearchableAlignAndProductName()
        {
            var json = """
                {
                    "name": "Test Documentation",
                    "theme": "mint",
                    "colors": { "primary": "#419AC5" },
                    "navigation": {
                        "directory": "accordion",
                        "sdk": {
                            "format": "docfx",
                            "source": "api.zip",
                            "directory": "reference"
                        },
                        "tabs": [
                            {
                                "tab": "API",
                                "align": "end",
                                "hidden": true,
                                "searchable": true,
                                "directory": "card",
                                "thumbnail": {
                                    "preset": "minimal",
                                    "eyebrow": "API"
                                },
                                "pages": ["api/index"]
                            }
                        ],
                        "products": [
                            {
                                "product": "core",
                                "name": "DotNetDocs Core",
                                "pages": ["index"]
                            }
                        ]
                    }
                }
                """;

            var manager = new DocsJsonManager();
            manager.Load(json);

            manager.ConfigurationErrors.Should().BeEmpty();
            var navigation = manager.Configuration!.Navigation;
            navigation.Directory.Should().Be("accordion");
            navigation.Sdk!.Format.Should().Be("docfx");
            navigation.Sdk.Source.Should().Be("api.zip");
            var tab = navigation.Tabs.Should().ContainSingle().Subject;
            tab.Align.Should().Be("end");
            tab.Searchable.Should().BeTrue();
            tab.Directory.Should().Be("card");
            tab.Thumbnail!.Eyebrow.Should().Be("API");
            navigation.Products.Should().ContainSingle().Which.Name.Should().Be("DotNetDocs Core");

            var reloaded = SaveAndReload(manager);
            reloaded.Navigation.Sdk!.Format.Should().Be("docfx");
            reloaded.Navigation.Tabs![0].Align.Should().Be("end");
            reloaded.Navigation.Products![0].Name.Should().Be("DotNetDocs Core");
        }

        /// <summary>
        /// Tests that version tags and per-language banner, footer, navbar, and thumbnail round-trip.
        /// </summary>
        [TestMethod]
        public void Load_Navigation_ParsesVersionTagAndLanguageChrome()
        {
            var json = """
                {
                    "name": "Test Documentation",
                    "theme": "mint",
                    "colors": { "primary": "#419AC5" },
                    "navigation": {
                        "versions": [
                            {
                                "version": "v2",
                                "tag": "Latest",
                                "directory": "accordion",
                                "thumbnail": { "preset": "minimal" },
                                "pages": ["v2/index"]
                            }
                        ],
                        "languages": [
                            {
                                "language": "es",
                                "directory": "card",
                                "banner": { "content": "Documentación en español" },
                                "footer": { "socials": { "x": "https://x.com/docs" } },
                                "navbar": { "links": [{ "label": "GitHub", "href": "https://github.com" }] },
                                "thumbnail": { "eyebrow": "ES" },
                                "pages": ["es/index"]
                            }
                        ]
                    }
                }
                """;

            var manager = new DocsJsonManager();
            manager.Load(json);

            manager.ConfigurationErrors.Should().BeEmpty();
            var version = manager.Configuration!.Navigation.Versions.Should().ContainSingle().Subject;
            version.Tag.Should().Be("Latest");
            version.Directory.Should().Be("accordion");
            version.Thumbnail!.Preset.Should().Be("minimal");
            var language = manager.Configuration.Navigation.Languages.Should().ContainSingle().Subject;
            language.Directory.Should().Be("card");
            language.Banner!.Content.Should().Be("Documentación en español");
            language.Thumbnail!.Eyebrow.Should().Be("ES");

            var reloaded = SaveAndReload(manager);
            reloaded.Navigation.Versions![0].Tag.Should().Be("Latest");
            reloaded.Navigation.Languages![0].Banner!.Content.Should().Be("Documentación en español");
            reloaded.Navigation.Languages[0].Thumbnail!.Eyebrow.Should().Be("ES");
        }

        /// <summary>
        /// Tests that menu items round-trip directory, thumbnail, and sdk.
        /// </summary>
        [TestMethod]
        public void Load_Navigation_ParsesMenuDirectoryThumbnailAndSdk()
        {
            var json = """
                {
                    "name": "Test Documentation",
                    "theme": "mint",
                    "colors": { "primary": "#419AC5" },
                    "navigation": {
                        "tabs": [
                            {
                                "tab": "Platform",
                                "menu": [
                                    {
                                        "item": "SDKs",
                                        "directory": "card",
                                        "thumbnail": { "preset": "headline" },
                                        "sdk": {
                                            "format": "typedoc",
                                            "source": "typedoc.json",
                                            "directory": "sdk/ts"
                                        },
                                        "pages": ["sdk/index"]
                                    }
                                ]
                            }
                        ]
                    }
                }
                """;

            var manager = new DocsJsonManager();
            manager.Load(json);

            manager.ConfigurationErrors.Should().BeEmpty();
            var menu = manager.Configuration!.Navigation.Tabs.Should().ContainSingle().Subject.Menu.Should().ContainSingle().Subject;
            menu.Directory.Should().Be("card");
            menu.Thumbnail!.Preset.Should().Be("headline");
            menu.Sdk!.Format.Should().Be("typedoc");
            menu.Sdk.Source.Should().Be("typedoc.json");

            var reloaded = SaveAndReload(manager);
            reloaded.Navigation.Tabs![0].Menu![0].Sdk!.Directory.Should().Be("sdk/ts");
        }

        /// <summary>
        /// Tests that invalid directory, sdk format, and tab align values are reported.
        /// </summary>
        [TestMethod]
        public void Load_Navigation_InvalidDirectorySdkAndAlign_AddsValidationWarnings()
        {
            var json = """
                {
                    "name": "Test Documentation",
                    "theme": "mint",
                    "colors": { "primary": "#419AC5" },
                    "navigation": {
                        "directory": "grid",
                        "sdk": { "format": "swagger", "source": "api.zip" },
                        "tabs": [
                            {
                                "tab": "API",
                                "align": "center",
                                "pages": ["api/index"]
                            }
                        ],
                        "languages": [
                            {
                                "language": "es",
                                "directory": "masonry",
                                "pages": ["es/index"]
                            }
                        ]
                    }
                }
                """;

            var manager = new DocsJsonManager();
            manager.Load(json);

            manager.ConfigurationErrors.Should().Contain(e => e.ErrorText.Contains("directory"));
            manager.ConfigurationErrors.Should().Contain(e => e.ErrorText.Contains("sdk"));
            manager.ConfigurationErrors.Should().Contain(e => e.ErrorText.Contains("align"));
            manager.ConfigurationErrors.Should().Contain(e => e.ErrorText.Contains("languages"));
        }

    }

}
