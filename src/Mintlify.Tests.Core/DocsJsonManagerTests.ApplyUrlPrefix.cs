using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {
        #region ApplyUrlPrefix Tests

        /// <summary>
        /// Tests that ApplyUrlPrefix throws when prefix is null or whitespace.
        /// </summary>
        [TestMethod]
        public void ApplyUrlPrefix_NullOrWhitespacePrefix_ThrowsArgumentException()
        {
            var manager = new DocsJsonManager();
            manager.Load("""{"name": "Test"}""");

            var act1 = () => manager.ApplyUrlPrefix(null!);
            var act2 = () => manager.ApplyUrlPrefix("");
            var act3 = () => manager.ApplyUrlPrefix("   ");

            act1.Should().Throw<ArgumentException>().WithParameterName("prefix");
            act2.Should().Throw<ArgumentException>().WithParameterName("prefix");
            act3.Should().Throw<ArgumentException>().WithParameterName("prefix");
        }

        /// <summary>
        /// Tests that ApplyUrlPrefix throws when no configuration is loaded.
        /// </summary>
        [TestMethod]
        public void ApplyUrlPrefix_NoConfiguration_ThrowsInvalidOperationException()
        {
            var manager = new DocsJsonManager();

            var act = () => manager.ApplyUrlPrefix("/docs");

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*No configuration is loaded*");
        }

        /// <summary>
        /// Tests that ApplyUrlPrefix handles null navigation gracefully.
        /// </summary>
        [TestMethod]
        public void ApplyUrlPrefix_NullNavigation_DoesNotThrow()
        {
            var manager = new DocsJsonManager();
            manager.Load("""{"name": "Test"}""");

            var act = () => manager.ApplyUrlPrefix("/docs");

            act.Should().NotThrow();
        }

        /// <summary>
        /// Tests that ApplyUrlPrefix correctly prefixes all URLs in navigation.
        /// </summary>
        [TestMethod]
        public void ApplyUrlPrefix_ValidNavigation_PrefixesAllUrls()
        {
            var manager = new DocsJsonManager();
            var config = new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        "quickstart",
                        new GroupConfig
                        {
                            Group = "API",
                            Root = "api",
                            Pages = ["api/overview", "api/reference"]
                        }
                    ],
                    Groups =
                    [
                        new GroupConfig
                        {
                            Group = "Guides",
                            Pages = ["guides/tutorial"]
                        }
                    ],
                    Tabs =
                    [
                        new TabConfig
                        {
                            Tab = "Examples",
                            Href = "examples",
                            Pages = ["examples/basic"]
                        }
                    ],
                    Anchors =
                    [
                        new AnchorConfig
                        {
                            Anchor = "Resources",
                            Href = "resources",
                            Pages = ["resources/links"]
                        }
                    ]
                }
            };
            manager.Load(JsonSerializer.Serialize(config, MintlifyConstants.JsonSerializerOptions));

            manager.ApplyUrlPrefix("/docs");

            // Check pages
            manager.Configuration!.Navigation!.Pages!.Should().Contain("/docs/quickstart");

            var apiGroup = manager.Configuration!.Navigation!.Pages?.OfType<GroupConfig>().First();
            apiGroup!.Root.Should().Be("/docs/api");
            apiGroup.Pages.Should().Contain("/docs/api/overview");
            apiGroup.Pages.Should().Contain("/docs/api/reference");

            // Check groups
            manager.Configuration!.Navigation!.Groups![0].Pages.Should().Contain("/docs/guides/tutorial");

            // Check tabs
            manager.Configuration!.Navigation!.Tabs![0].Href.Should().Be("/docs/examples");
            manager.Configuration!.Navigation!.Tabs![0].Pages.Should().Contain("/docs/examples/basic");

            // Check anchors
            manager.Configuration!.Navigation!.Anchors![0].Href.Should().Be("/docs/resources");
            manager.Configuration!.Navigation!.Anchors![0].Pages.Should().Contain("/docs/resources/links");
        }

        /// <summary>
        /// Tests that ApplyUrlPrefix handles trailing slashes correctly.
        /// </summary>
        [TestMethod]
        public void ApplyUrlPrefix_PrefixWithTrailingSlash_NormalizesUrls()
        {
            var manager = new DocsJsonManager();
            manager.Load("""{"name": "Test", "navigation": {"pages": ["quickstart"]}}""");

            manager.ApplyUrlPrefix("/docs/");

            manager.Configuration!.Navigation!.Pages!.Should().Contain("/docs/quickstart");
            manager.Configuration!.Navigation!.Pages!.Should().NotContain("/docs//quickstart");
        }

        #endregion

        #region ApplyUrlPrefixToPages Tests

        [TestMethod]
        public void ApplyUrlPrefixToPages_WithStringPages_AppliesPrefix()
        {
            var manager = new DocsJsonManager();
            var pages = new List<object> { "index", "quickstart", "installation" };

            manager.ApplyUrlPrefixToPages(pages, "services/auth");

            pages.Should().HaveCount(3);
            pages[0].Should().Be("services/auth/index");
            pages[1].Should().Be("services/auth/quickstart");
            pages[2].Should().Be("services/auth/installation");
        }

        [TestMethod]
        public void ApplyUrlPrefixToPages_WithNestedStringPages_AppliesPrefix()
        {
            var manager = new DocsJsonManager();
            var pages = new List<object> { "api/overview", "api/authentication", "api/endpoints" };

            manager.ApplyUrlPrefixToPages(pages, "services/auth");

            pages[0].Should().Be("services/auth/api/overview");
            pages[1].Should().Be("services/auth/api/authentication");
            pages[2].Should().Be("services/auth/api/endpoints");
        }

        [TestMethod]
        public void ApplyUrlPrefixToPages_WithGroupConfig_AppliesPrefixToGroupPages()
        {
            var manager = new DocsJsonManager();
            var group = new GroupConfig
            {
                Group = "Getting Started",
                Pages = ["index", "quickstart"]
            };
            var pages = new List<object> { group };

            manager.ApplyUrlPrefixToPages(pages, "services/auth");

            var resultGroup = pages[0] as GroupConfig;
            resultGroup.Should().NotBeNull();
            resultGroup!.Pages.Should().HaveCount(2);
            resultGroup.Pages![0].Should().Be("services/auth/index");
            resultGroup.Pages![1].Should().Be("services/auth/quickstart");
        }

        [TestMethod]
        public void ApplyUrlPrefixToPages_WithNestedGroups_AppliesPrefixRecursively()
        {
            var manager = new DocsJsonManager();
            var innerGroup = new GroupConfig
            {
                Group = "API Reference",
                Pages = ["api/overview", "api/methods"]
            };
            var outerGroup = new GroupConfig
            {
                Group = "Documentation",
                Pages = ["index", innerGroup]
            };
            var pages = new List<object> { outerGroup };

            manager.ApplyUrlPrefixToPages(pages, "services/auth");

            var resultGroup = pages[0] as GroupConfig;
            resultGroup.Should().NotBeNull();
            resultGroup!.Pages![0].Should().Be("services/auth/index");

            var nestedGroup = resultGroup.Pages![1] as GroupConfig;
            nestedGroup.Should().NotBeNull();
            nestedGroup!.Pages![0].Should().Be("services/auth/api/overview");
            nestedGroup.Pages![1].Should().Be("services/auth/api/methods");
        }

        [TestMethod]
        public void ApplyUrlPrefixToPages_WithMixedPagesAndGroups_AppliesPrefixCorrectly()
        {
            var manager = new DocsJsonManager();
            var group = new GroupConfig
            {
                Group = "Advanced",
                Pages = ["advanced/configuration"]
            };
            var pages = new List<object> { "index", group, "faq" };

            manager.ApplyUrlPrefixToPages(pages, "services/auth");

            pages.Should().HaveCount(3);
            pages[0].Should().Be("services/auth/index");
            pages[2].Should().Be("services/auth/faq");

            var resultGroup = pages[1] as GroupConfig;
            resultGroup!.Pages![0].Should().Be("services/auth/advanced/configuration");
        }

        [TestMethod]
        public void ApplyUrlPrefixToGroup_WithPages_AppliesPrefix()
        {
            var manager = new DocsJsonManager();
            var group = new GroupConfig
            {
                Group = "Getting Started",
                Pages = ["index", "quickstart", "installation"]
            };

            manager.ApplyUrlPrefixToGroup(group, "services/auth");

            group.Pages.Should().HaveCount(3);
            group.Pages![0].Should().Be("services/auth/index");
            group.Pages![1].Should().Be("services/auth/quickstart");
            group.Pages![2].Should().Be("services/auth/installation");
        }

        [TestMethod]
        public void ApplyUrlPrefixToTab_WithHrefAndPages_AppliesPrefixToBoth()
        {
            var manager = new DocsJsonManager();
            var tab = new TabConfig
            {
                Tab = "API",
                Href = "api",
                Pages = ["api/overview", "api/reference"]
            };

            manager.ApplyUrlPrefixToTab(tab, "services/auth");

            tab.Href.Should().Be("services/auth/api");
            tab.Pages.Should().HaveCount(2);
            tab.Pages![0].Should().Be("services/auth/api/overview");
            tab.Pages![1].Should().Be("services/auth/api/reference");
        }

        [TestMethod]
        public void ApplyUrlPrefixToTab_WithGroups_AppliesPrefixToGroupPages()
        {
            var manager = new DocsJsonManager();
            var tab = new TabConfig
            {
                Tab = "Documentation",
                Groups =
                [
                    new GroupConfig
                    {
                        Group = "Guides",
                        Pages = ["guides/intro", "guides/advanced"]
                    }
                ]
            };

            manager.ApplyUrlPrefixToTab(tab, "services/auth");

            tab.Groups![0].Pages![0].Should().Be("services/auth/guides/intro");
            tab.Groups![0].Pages![1].Should().Be("services/auth/guides/advanced");
        }

        [TestMethod]
        public void ApplyUrlPrefixToTab_WithAnchors_AppliesPrefixToAnchorPages()
        {
            var manager = new DocsJsonManager();
            var tab = new TabConfig
            {
                Tab = "Documentation",
                Anchors =
                [
                    new AnchorConfig
                    {
                        Anchor = "Resources",
                        Icon = "book",
                        Pages = ["resources/overview"]
                    }
                ]
            };

            manager.ApplyUrlPrefixToTab(tab, "services/auth");

            tab.Anchors![0].Pages![0].Should().Be("services/auth/resources/overview");
        }

        [TestMethod]
        public void ApplyUrlPrefixToAnchor_WithHrefAndPages_AppliesPrefixToBoth()
        {
            var manager = new DocsJsonManager();
            var anchor = new AnchorConfig
            {
                Anchor = "Resources",
                Icon = "book",
                Href = "resources",
                Pages = ["resources/docs", "resources/tutorials"]
            };

            manager.ApplyUrlPrefixToAnchor(anchor, "services/auth");

            anchor.Href.Should().Be("services/auth/resources");
            anchor.Pages![0].Should().Be("services/auth/resources/docs");
            anchor.Pages![1].Should().Be("services/auth/resources/tutorials");
        }

        [TestMethod]
        public void ApplyUrlPrefixToAnchor_WithGroupsAndTabs_AppliesPrefixRecursively()
        {
            var manager = new DocsJsonManager();
            var anchor = new AnchorConfig
            {
                Anchor = "Documentation",
                Icon = "book",
                Groups =
                [
                    new GroupConfig
                    {
                        Group = "Guides",
                        Pages = ["guides/intro"]
                    }
                ],
                Tabs =
                [
                    new TabConfig
                    {
                        Tab = "API",
                        Href = "api",
                        Pages = ["api/reference"]
                    }
                ]
            };

            manager.ApplyUrlPrefixToAnchor(anchor, "services/auth");

            anchor.Groups![0].Pages![0].Should().Be("services/auth/guides/intro");
            anchor.Tabs![0].Href.Should().Be("services/auth/api");
            anchor.Tabs![0].Pages![0].Should().Be("services/auth/api/reference");
        }

        [TestMethod]
        public void ApplyUrlPrefixToNavigation_WithComplexNestedStructure_AppliesPrefixEverywhere()
        {
            var docsJson = """
                {
                    "name": "Test",
                    "navigation": {
                        "pages": ["index"],
                        "groups": [
                            {
                                "group": "Guides",
                                "pages": ["guides/intro"]
                            }
                        ],
                        "tabs": [
                            {
                                "tab": "API",
                                "href": "api",
                                "pages": ["api/overview"],
                                "groups": [
                                    {
                                        "group": "Methods",
                                        "pages": ["api/methods"]
                                    }
                                ],
                                "anchors": [
                                    {
                                        "anchor": "Resources",
                                        "icon": "book",
                                        "pages": ["resources/docs"]
                                    }
                                ]
                            }
                        ]
                    }
                }
                """;

            var manager = new DocsJsonManager();
            manager.Load(docsJson);
            manager.ApplyUrlPrefix("services/auth");

            manager.Configuration!.Navigation!.Pages![0].Should().Be("services/auth/index");
            manager.Configuration.Navigation.Groups![0].Pages![0].Should().Be("services/auth/guides/intro");
            manager.Configuration.Navigation.Tabs![0].Href.Should().Be("services/auth/api");
            manager.Configuration.Navigation.Tabs![0].Pages![0].Should().Be("services/auth/api/overview");
            manager.Configuration.Navigation.Tabs![0].Groups![0].Pages![0].Should().Be("services/auth/api/methods");
            manager.Configuration.Navigation.Tabs![0].Anchors![0].Pages![0].Should().Be("services/auth/resources/docs");
        }

        [TestMethod]
        public void ApplyUrlPrefixToPages_WithEmptyList_DoesNothing()
        {
            var manager = new DocsJsonManager();
            var pages = new List<object>();

            manager.ApplyUrlPrefixToPages(pages, "services/auth");

            pages.Should().BeEmpty();
        }

        [TestMethod]
        public void ApplyUrlPrefixToGroup_WithNullPages_DoesNotThrow()
        {
            var manager = new DocsJsonManager();
            var group = new GroupConfig
            {
                Group = "Test",
                Pages = null
            };

            var act = () => manager.ApplyUrlPrefixToGroup(group, "services/auth");

            act.Should().NotThrow();
        }

        [TestMethod]
        public void ApplyUrlPrefixToTab_WithNullPages_DoesNotThrow()
        {
            var manager = new DocsJsonManager();
            var tab = new TabConfig
            {
                Tab = "Test",
                Pages = null
            };

            var act = () => manager.ApplyUrlPrefixToTab(tab, "services/auth");

            act.Should().NotThrow();
        }

        [TestMethod]
        public void ApplyUrlPrefixToAnchor_WithNullPages_DoesNotThrow()
        {
            var manager = new DocsJsonManager();
            var anchor = new AnchorConfig
            {
                Anchor = "Test",
                Icon = "book",
                Pages = null
            };

            var act = () => manager.ApplyUrlPrefixToAnchor(anchor, "services/auth");

            act.Should().NotThrow();
        }

        [TestMethod]
        public void ApplyUrlPrefixToPages_WithMultipleLevelsOfNesting_AppliesPrefixCorrectly()
        {
            var manager = new DocsJsonManager();
            var deeplyNestedGroup = new GroupConfig
            {
                Group = "Level 3",
                Pages = ["level3/page"]
            };
            var midGroup = new GroupConfig
            {
                Group = "Level 2",
                Pages = ["level2/page", deeplyNestedGroup]
            };
            var topGroup = new GroupConfig
            {
                Group = "Level 1",
                Pages = ["level1/page", midGroup]
            };
            var pages = new List<object> { topGroup };

            manager.ApplyUrlPrefixToPages(pages, "docs");

            var level1 = pages[0] as GroupConfig;
            level1!.Pages![0].Should().Be("docs/level1/page");

            var level2 = level1.Pages![1] as GroupConfig;
            level2!.Pages![0].Should().Be("docs/level2/page");

            var level3 = level2.Pages![1] as GroupConfig;
            level3!.Pages![0].Should().Be("docs/level3/page");
        }

        #endregion
    }

}