using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;
using Mintlify.Core.Models.Integrations;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {
        #region Merge Tests

        /// <summary>
        /// Tests that Merge method combines configurations correctly.
        /// </summary>
        [TestMethod]
        public void Merge_ValidConfigurations_CombinesProperties()
        {
            var manager = new DocsJsonManager();
            manager.Load(_validDocsJson);

            var otherConfig = new DocsJsonConfig
            {
                Name = "Updated Name",
                Description = "New description",
                Colors = new ColorsConfig { Primary = "#FF0000" }
            };

            manager.Merge(otherConfig);

            manager.Configuration!.Name.Should().Be("Updated Name");
            manager.Configuration!.Description.Should().Be("New description");
            manager.Configuration!.Colors.Should().NotBeNull();
            manager.Configuration!.Colors.Primary.Should().Be("#FF0000");
            manager.Configuration!.Theme.Should().Be("mint"); // Original value preserved
        }

        /// <summary>
        /// Tests that Merge copies additive root properties from the incoming configuration.
        /// </summary>
        [TestMethod]
        public void Merge_CopiesNewRootProperties()
        {
            var manager = new DocsJsonManager();
            manager.Load(_validDocsJson);

            var otherConfig = new DocsJsonConfig
            {
                Public = false,
                ThemeVersion = 2,
                Variables = new Dictionary<string, string> { ["product"] = "DotNetDocs" },
                Markdown = new MarkdownConfig { Schema = false },
                Packages = new PackagesConfig { Ssr = false }
            };

            manager.Merge(otherConfig);

            manager.Configuration!.Public.Should().BeFalse();
            manager.Configuration.ThemeVersion.Should().Be(2);
            manager.Configuration.Variables.Should().ContainKey("product").WhoseValue.Should().Be("DotNetDocs");
            manager.Configuration.Markdown.Should().NotBeNull();
            manager.Configuration.Markdown!.Schema.Should().BeFalse();
            manager.Configuration.Packages.Should().NotBeNull();
            manager.Configuration.Packages!.Ssr.Should().BeFalse();
        }

        /// <summary>
        /// Tests that Merge copies remaining site-chrome objects from the incoming configuration.
        /// </summary>
        [TestMethod]
        public void Merge_CopiesSiteChromeProperties()
        {
            var manager = new DocsJsonManager();
            manager.Load(_validDocsJson);

            var otherConfig = new DocsJsonConfig
            {
                Api = new ApiConfig { Url = "full" },
                Appearance = new AppearanceConfig { Default = "dark" },
                Banner = new BannerConfig { Type = "warning" },
                Contextual = new ContextualConfig { Display = "toc" },
                Fonts = new FontsConfig { Family = "Inter" },
                Icons = new IconsConfig { Library = "tabler" },
                Integrations = new IntegrationsConfig
                {
                    Mixpanel = new MixpanelConfig { ProjectToken = "token", Region = "eu" }
                },
                Seo = new SeoConfig { TrailingSlash = true },
                Styling = new StylingConfig { Latex = true },
                Thumbnails = new ThumbnailsConfig { Preset = "headline" },
                Metadata = new MetadataConfig { Timestamp = true }
            };

            manager.Merge(otherConfig);

            manager.Configuration!.Api!.Url.Should().Be("full");
            manager.Configuration.Appearance!.Default.Should().Be("dark");
            manager.Configuration.Banner!.Type.Should().Be("warning");
            manager.Configuration.Contextual!.Display.Should().Be("toc");
            manager.Configuration.Fonts!.Family.Should().Be("Inter");
            manager.Configuration.Icons!.Library.Should().Be("tabler");
            manager.Configuration.Integrations!.Mixpanel!.Region.Should().Be("eu");
            manager.Configuration.Seo!.TrailingSlash.Should().BeTrue();
            manager.Configuration.Styling!.Latex.Should().BeTrue();
            manager.Configuration.Thumbnails!.Preset.Should().Be("headline");
            manager.Configuration.Metadata!.Timestamp.Should().BeTrue();
        }

        /// <summary>
        /// Tests that Merge copies GraphQL and MCP spec refs on navigation and matching groups.
        /// </summary>
        [TestMethod]
        public void Merge_CopiesGraphqlAndMcpOnNavigationAndGroups()
        {
            var manager = new DocsJsonManager();
            manager.Load(_validDocsJson);

            var otherConfig = new DocsJsonConfig
            {
                Navigation = new NavigationConfig
                {
                    Graphql = new SpecRefConfig { Source = "schema.graphql" },
                    Mcp = new SpecRefConfig { Source = "mcp.json", Directory = "mcp-reference" },
                    Pages =
                    [
                        new GroupConfig
                        {
                            Group = "Getting Started",
                            Graphql = new SpecRefConfig { Source = "guides.graphql", Directory = "api/graphql" },
                            Mcp = new SpecRefConfig { Source = "guides-mcp.json" },
                            Pages = ["quickstart"]
                        }
                    ]
                }
            };

            manager.Merge(otherConfig);

            var navigation = manager.Configuration!.Navigation;
            navigation.Graphql!.Source.Should().Be("schema.graphql");
            navigation.Mcp!.Source.Should().Be("mcp.json");
            navigation.Mcp.Directory.Should().Be("mcp-reference");

            var group = navigation.Pages!.OfType<GroupConfig>().First(g => g.Group == "Getting Started");
            group.Graphql!.Source.Should().Be("guides.graphql");
            group.Graphql.Directory.Should().Be("api/graphql");
            group.Mcp!.Source.Should().Be("guides-mcp.json");
        }

        /// <summary>
        /// Tests that Merge copies sdk, directory, searchable, and thumbnail on matching groups.
        /// </summary>
        [TestMethod]
        public void Merge_CopiesSdkDirectorySearchableAndThumbnail()
        {
            var manager = new DocsJsonManager();
            manager.Load(_validDocsJson);

            var otherConfig = new DocsJsonConfig
            {
                Navigation = new NavigationConfig
                {
                    Directory = "accordion",
                    Sdk = new SdkRefConfig { Format = "docfx", Source = "api.zip", Directory = "reference" },
                    Pages =
                    [
                        new GroupConfig
                        {
                            Group = "Getting Started",
                            Directory = "card",
                            Searchable = true,
                            Sdk = new SdkRefConfig { Format = "typedoc", Source = "dist", Directory = "js" },
                            Thumbnail = new NavThumbnailConfig { Preset = "headline", Eyebrow = "Start" },
                            Pages = ["quickstart"]
                        }
                    ],
                    Tabs =
                    [
                        new TabConfig
                        {
                            Tab = "API",
                            Align = "end",
                            Pages = ["api/index"]
                        }
                    ]
                }
            };

            manager.Merge(otherConfig);

            var navigation = manager.Configuration!.Navigation;
            navigation.Directory.Should().Be("accordion");
            navigation.Sdk!.Format.Should().Be("docfx");
            var group = navigation.Pages!.OfType<GroupConfig>().First(g => g.Group == "Getting Started");
            group.Directory.Should().Be("card");
            group.Searchable.Should().BeTrue();
            group.Sdk!.Directory.Should().Be("js");
            group.Thumbnail!.Eyebrow.Should().Be("Start");
            navigation.Tabs.Should().ContainSingle(t => t.Tab == "API").Which.Align.Should().Be("end");
        }

        /// <summary>
        /// Tests that Merge throws exception when no configuration is loaded.
        /// </summary>
        [TestMethod]
        public void Merge_NoConfiguration_ThrowsInvalidOperationException()
        {
            var manager = new DocsJsonManager();
            var otherConfig = new DocsJsonConfig { Name = "Test" };

            var act = () => manager.Merge(otherConfig);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*No configuration is loaded*");
        }

        /// <summary>
        /// Tests that Merge throws exception for null parameter.
        /// </summary>
        [TestMethod]
        public void Merge_NullOtherConfig_ThrowsArgumentNullException()
        {
            var manager = new DocsJsonManager();
            manager.Load(_validDocsJson);

            var act = () => manager.Merge(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("other");
        }

        /// <summary>
        /// Tests that Merge correctly handles navigation with duplicate string pages.
        /// </summary>
        [TestMethod]
        public void Merge_NavigationWithDuplicateStringPages_DeduplicatesPages()
        {
            var manager = new DocsJsonManager();
            var config1 = new DocsJsonConfig
            {
                Name = "Config 1",
                Navigation = new NavigationConfig
                {
                    Pages = ["index", "quickstart", "api/overview"]
                }
            };
            manager.Load(JsonSerializer.Serialize(config1, MintlifyConstants.JsonSerializerOptions));

            var config2 = new DocsJsonConfig
            {
                Navigation = new NavigationConfig
                {
                    Pages = ["quickstart", "api/overview", "api/reference"]
                }
            };

            manager.Merge(config2);

            manager.Configuration!.Navigation!.Pages!.Should().NotBeNull();
            manager.Configuration!.Navigation!.Pages!.Should().HaveCount(4);
            manager.Configuration!.Navigation!.Pages!.Should().BeEquivalentTo(["index", "quickstart", "api/overview", "api/reference"]);
        }

        /// <summary>
        /// Tests that Merge correctly handles navigation with duplicate GroupConfig objects.
        /// </summary>
        [TestMethod]
        public void Merge_NavigationWithDuplicateGroups_MergesGroupsIntelligently()
        {
            var manager = new DocsJsonManager();
            var config1 = new DocsJsonConfig
            {
                Name = "Config 1",
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        "index",
                        new GroupConfig
                        {
                            Group = "API Reference",
                            Pages = ["api/overview", "api/authentication"]
                        }
                    ]
                }
            };
            manager.Load(JsonSerializer.Serialize(config1, MintlifyConstants.JsonSerializerOptions));

            var config2 = new DocsJsonConfig
            {
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        new GroupConfig
                        {
                            Group = "API Reference",
                            Pages = ["api/endpoints", "api/errors"]
                        },
                        new GroupConfig
                        {
                            Group = "Guides",
                            Pages = ["guides/quickstart"]
                        }
                    ]
                }
            };

            manager.Merge(config2);

            manager.Configuration!.Navigation!.Pages!.Should().HaveCount(3);
            manager.Configuration!.Navigation!.Pages![0].Should().Be("index");

            var apiGroup = manager.Configuration!.Navigation!.Pages![1] as GroupConfig;
            apiGroup.Should().NotBeNull();
            apiGroup!.Group.Should().Be("API Reference");
            apiGroup!.Pages.Should().HaveCount(4);
            apiGroup.Pages.Should().BeEquivalentTo(["api/overview", "api/authentication", "api/endpoints", "api/errors"]);

            var guidesGroup = manager.Configuration!.Navigation!.Pages![2] as GroupConfig;
            guidesGroup.Should().NotBeNull();
            guidesGroup!.Group.Should().Be("Guides");
        }

        /// <summary>
        /// Tests that Merge correctly handles mixed navigation content (strings and groups).
        /// </summary>
        [TestMethod]
        public void Merge_MixedNavigationContent_PreservesStructure()
        {
            var manager = new DocsJsonManager();
            var config1 = new DocsJsonConfig
            {
                Name = "Config 1",
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        "index",
                        new GroupConfig
                        {
                            Group = "Getting Started",
                            Pages = ["quickstart", "installation"]
                        },
                        "changelog"
                    ]
                }
            };
            manager.Load(JsonSerializer.Serialize(config1, MintlifyConstants.JsonSerializerOptions));

            var config2 = new DocsJsonConfig
            {
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        "support",
                        new GroupConfig
                        {
                            Group = "Getting Started",
                            Pages = ["configuration", "deployment"]
                        }
                    ]
                }
            };

            manager.Merge(config2);

            manager.Configuration!.Navigation!.Pages!.Should().HaveCount(4);
            manager.Configuration!.Navigation!.Pages!.Should().Contain("index");
            manager.Configuration!.Navigation!.Pages!.Should().Contain("changelog");
            manager.Configuration!.Navigation!.Pages!.Should().Contain("support");

            var gettingStartedGroup = manager.Configuration!.Navigation!.Pages!
                .OfType<GroupConfig>()
                .FirstOrDefault(g => g.Group == "Getting Started");
            gettingStartedGroup.Should().NotBeNull();
            gettingStartedGroup!.Pages.Should().HaveCount(4);
        }

        /// <summary>
        /// Tests that Merge correctly handles null navigation scenarios.
        /// </summary>
        [TestMethod]
        public void Merge_NullNavigationScenarios_HandlesGracefully()
        {
            // Scenario 1: Target has null navigation
            var manager1 = new DocsJsonManager();
            var config1 = new DocsJsonConfig { Name = "Config 1" };
            manager1.Load(JsonSerializer.Serialize(config1, MintlifyConstants.JsonSerializerOptions));

            var config2 = new DocsJsonConfig
            {
                Navigation = new NavigationConfig
                {
                    Pages = ["index", "quickstart"]
                }
            };

            manager1.Merge(config2);
            manager1.Configuration!.Navigation.Should().NotBeNull();
            manager1.Configuration!.Navigation!.Pages.Should().HaveCount(2);

            // Scenario 2: Source has null navigation
            var manager2 = new DocsJsonManager();
            manager2.Load(_validDocsJson);
            var originalNavigation = manager2.Configuration!.Navigation;

            var config3 = new DocsJsonConfig { Name = "Updated" };
            manager2.Merge(config3);

            manager2.Configuration.Navigation.Should().BeSameAs(originalNavigation);
        }

        /// <summary>
        /// Tests that Merge correctly handles nested GroupConfig structures.
        /// </summary>
        [TestMethod]
        public void Merge_NestedGroupStructures_MergesDeepHierarchy()
        {
            var manager = new DocsJsonManager();
            var config1 = new DocsJsonConfig
            {
                Name = "Config 1",
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        new GroupConfig
                        {
                            Group = "API",
                            Pages =
                            [
                                "api/overview",
                                new GroupConfig
                                {
                                    Group = "Authentication",
                                    Pages = ["api/auth/oauth", "api/auth/tokens"]
                                }
                            ]
                        }
                    ]
                }
            };
            manager.Load(JsonSerializer.Serialize(config1, MintlifyConstants.JsonSerializerOptions));

            var config2 = new DocsJsonConfig
            {
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        new GroupConfig
                        {
                            Group = "API",
                            Pages =
                            [
                                new GroupConfig
                                {
                                    Group = "Authentication",
                                    Pages = ["api/auth/jwt", "api/auth/api-keys"]
                                },
                                new GroupConfig
                                {
                                    Group = "Endpoints",
                                    Pages = ["api/endpoints/users", "api/endpoints/posts"]
                                }
                            ]
                        }
                    ]
                }
            };

            manager.Merge(config2);

            var apiGroup = manager.Configuration!.Navigation!.Pages![0] as GroupConfig;
            apiGroup.Should().NotBeNull();
            apiGroup!.Group.Should().Be("API");
            apiGroup!.Pages.Should().HaveCount(3); // overview + merged auth group + new endpoints group

            var authGroup = apiGroup!.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Authentication");
            authGroup.Should().NotBeNull();
            authGroup!.Pages.Should().HaveCount(4); // All auth pages merged
        }

        /// <summary>
        /// Tests that Merge preserves original values when source has null values.
        /// </summary>
        [TestMethod]
        public void Merge_SourceWithNullValues_PreservesOriginalValues()
        {
            var manager = new DocsJsonManager();
            manager.Load(_validDocsJson);
            var originalName = manager.Configuration!.Name;
            var originalTheme = manager.Configuration!.Theme;

            var config2 = new DocsJsonConfig
            {
                Name = null!,
                Theme = null!,
                Description = "Updated description"
            };

            manager.Merge(config2);

            manager.Configuration!.Name.Should().Be(originalName);
            manager.Configuration!.Theme.Should().Be(originalTheme);
            manager.Configuration!.Description.Should().Be("Updated description");
        }

        /// <summary>
        /// Tests that Merge handles empty navigation pages correctly.
        /// </summary>
        [TestMethod]
        public void Merge_EmptyNavigationPages_HandlesCorrectly()
        {
            var manager = new DocsJsonManager();
            var config1 = new DocsJsonConfig
            {
                Name = "Config 1",
                Navigation = new NavigationConfig
                {
                    Pages = []
                }
            };
            manager.Load(JsonSerializer.Serialize(config1, MintlifyConstants.JsonSerializerOptions));

            var config2 = new DocsJsonConfig
            {
                Navigation = new NavigationConfig
                {
                    Pages = ["index", "quickstart"]
                }
            };

            manager.Merge(config2);

            manager.Configuration!.Navigation!.Pages!.Should().HaveCount(2);
            manager.Configuration!.Navigation!.Pages!.Should().BeEquivalentTo(["index", "quickstart"]);
        }

        /// <summary>
        /// Tests that Merge handles groups with null or empty group names using default behavior.
        /// </summary>
        [TestMethod]
        public void Merge_GroupsWithNullOrEmptyNames_DefaultBehaviorKeepsSeparate()
        {
            var manager = new DocsJsonManager();
            var config1 = new DocsJsonConfig
            {
                Name = "Config 1",
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        new GroupConfig { Group = null!, Pages = ["page1"] },
                        new GroupConfig { Group = "", Pages = ["page2"] },
                        new GroupConfig { Group = "Valid Group", Pages = ["page3"] }
                    ]
                }
            };
            manager.Load(JsonSerializer.Serialize(config1, MintlifyConstants.JsonSerializerOptions));

            var config2 = new DocsJsonConfig
            {
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        new GroupConfig { Group = null!, Pages = ["page4"] },
                        new GroupConfig { Group = "", Pages = ["page5"] },
                        new GroupConfig { Group = "Valid Group", Pages = ["page6"] }
                    ]
                }
            };

            // Merge with default behavior (no options = empty groups stay separate, matching Mintlify)
            manager.Merge(config2);

            // The test shows all groups have "" (empty string), not null
            // This means JSON serialization converts null! to empty string
            // Empty groups remain separate (default Mintlify behavior)
            // We should have: 3 empty groups (page1, page2, page5), 1 "Valid Group" (page3, page6)
            manager.Configuration!.Navigation!.Pages!.Should().HaveCount(4);

            var validGroup = manager.Configuration!.Navigation!.Pages!
                .OfType<GroupConfig>()
                .FirstOrDefault(g => g.Group == "Valid Group");
            validGroup.Should().NotBeNull();
            validGroup!.Pages.Should().HaveCount(2); // page3 and page6 merged

            // Verify empty groups remain separate (default behavior)
            var emptyGroups = manager.Configuration!.Navigation!.Pages!
                .OfType<GroupConfig>()
                .Where(g => string.IsNullOrWhiteSpace(g.Group))
                .ToList();
            emptyGroups.Should().HaveCount(3); // Empty groups NOT merged (page1, page2, page5)
            // The group with page1 still exists but with empty string (null was cleaned to empty)
            emptyGroups.Should().Contain(g => g.Pages != null && g.Pages.Contains("page1"));
            emptyGroups.Should().Contain(g => g.Pages != null && g.Pages.Contains("page2"));
            emptyGroups.Should().Contain(g => g.Pages != null && g.Pages.Contains("page5"));
        }

        /// <summary>
        /// Tests that Merge with CombineEmptyGroups option merges empty groups together.
        /// </summary>
        [TestMethod]
        public void Merge_WithCombineEmptyGroupsOption_MergesEmptyGroups()
        {
            var manager = new DocsJsonManager();
            var config1 = new DocsJsonConfig
            {
                Name = "Config 1",
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        new GroupConfig { Group = null!, Pages = ["page1"] },
                        new GroupConfig { Group = "", Pages = ["page2"] },
                        new GroupConfig { Group = "Valid Group", Pages = ["page3"] }
                    ]
                }
            };
            manager.Load(JsonSerializer.Serialize(config1, MintlifyConstants.JsonSerializerOptions));

            var config2 = new DocsJsonConfig
            {
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        new GroupConfig { Group = null!, Pages = ["page4"] },
                        new GroupConfig { Group = "", Pages = ["page5"] },
                        new GroupConfig { Group = "Valid Group", Pages = ["page6"] }
                    ]
                }
            };

            // Merge with CombineEmptyGroups option enabled
            var mergeOptions = new MergeOptions { CombineEmptyGroups = true };
            manager.Merge(config2, options: mergeOptions);

            // Groups with null names become empty strings during JSON serialization
            // Empty groups should be merged together when option is enabled
            // We should have: 1 merged empty group (page1, page2, page5), 1 "Valid Group" (page3, page6)
            manager.Configuration!.Navigation!.Pages!.Should().HaveCount(2);

            var validGroup = manager.Configuration!.Navigation!.Pages!
                .OfType<GroupConfig>()
                .FirstOrDefault(g => g.Group == "Valid Group");
            validGroup.Should().NotBeNull();
            validGroup!.Pages.Should().HaveCount(2); // page3 and page6 merged

            // Verify empty groups are merged when option is enabled
            var emptyGroups = manager.Configuration!.Navigation!.Pages!
                .OfType<GroupConfig>()
                .Where(g => string.IsNullOrWhiteSpace(g.Group))
                .ToList();
            emptyGroups.Should().ContainSingle(); // Empty groups merged into one
            emptyGroups[0].Pages.Should().HaveCount(3); // page1, page2 and page5 merged
        }

        /// <summary>
        /// Tests that Merge with null options uses default behavior.
        /// </summary>
        [TestMethod]
        public void Merge_WithNullOptions_UsesDefaultBehavior()
        {
            var manager = new DocsJsonManager();
            var config1 = new DocsJsonConfig
            {
                Name = "Config 1",
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        new GroupConfig { Group = "", Pages = ["page1"] },
                        new GroupConfig { Group = "Valid Group", Pages = ["page2"] }
                    ]
                }
            };
            manager.Load(JsonSerializer.Serialize(config1, MintlifyConstants.JsonSerializerOptions));

            var config2 = new DocsJsonConfig
            {
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        new GroupConfig { Group = "", Pages = ["page3"] },
                        new GroupConfig { Group = "Valid Group", Pages = ["page4"] }
                    ]
                }
            };

            // Explicitly pass null options
            manager.Merge(config2, options: null);

            // Should behave the same as default (empty groups stay separate)
            manager.Configuration!.Navigation!.Pages!.Should().HaveCount(3);

            var emptyGroups = manager.Configuration!.Navigation!.Pages!
                .OfType<GroupConfig>()
                .Where(g => string.IsNullOrWhiteSpace(g.Group))
                .ToList();
            emptyGroups.Should().HaveCount(2); // Empty groups NOT merged with null options
        }

        /// <summary>
        /// Tests that Merge with combineBaseProperties=false only merges navigation.
        /// </summary>
        [TestMethod]
        public void Merge_CombineBasePropertiesFalse_OnlyMergesNavigation()
        {
            var manager = new DocsJsonManager();
            manager.Load(_validDocsJson);
            var originalName = manager.Configuration!.Name;
            var originalTheme = manager.Configuration!.Theme;

            var config2 = new DocsJsonConfig
            {
                Name = "Should Not Be Applied",
                Theme = "dark",
                Description = "Should Not Be Applied",
                Navigation = new NavigationConfig
                {
                    Pages = ["new-page"]
                }
            };

            manager.Merge(config2, combineBaseProperties: false);

            // Base properties should not change
            manager.Configuration!.Name.Should().Be(originalName);
            manager.Configuration!.Theme.Should().Be(originalTheme);
            manager.Configuration!.Description.Should().NotBe("Should Not Be Applied");

            // Navigation should be merged
            manager.Configuration!.Navigation!.Pages!.Should().Contain("new-page");
        }

        /// <summary>
        /// Tests that Merge intelligently combines Groups with the same name.
        /// </summary>
        [TestMethod]
        public void Merge_GroupsWithSameName_CombinesIntelligently()
        {
            var manager = new DocsJsonManager();
            var config1 = new DocsJsonConfig
            {
                Name = "Config 1",
                Navigation = new NavigationConfig
                {
                    Groups =
                    [
                        new GroupConfig
                        {
                            Group = "API Reference",
                            Pages = ["api/overview", "api/auth"]
                        },
                        new GroupConfig
                        {
                            Group = "Guides",
                            Pages = ["guides/quickstart"]
                        }
                    ]
                }
            };
            manager.Load(JsonSerializer.Serialize(config1, MintlifyConstants.JsonSerializerOptions));

            var config2 = new DocsJsonConfig
            {
                Navigation = new NavigationConfig
                {
                    Groups =
                    [
                        new GroupConfig
                        {
                            Group = "API Reference",
                            Pages = ["api/endpoints", "api/errors"]
                        },
                        new GroupConfig
                        {
                            Group = "Examples",
                            Pages = ["examples/basic"]
                        }
                    ]
                }
            };

            manager.Merge(config2);

            manager.Configuration!.Navigation!.Groups!.Should().HaveCount(3);

            var apiGroup = manager.Configuration!.Navigation!.Groups!.FirstOrDefault(g => g.Group == "API Reference");
            apiGroup.Should().NotBeNull();
            apiGroup!.Pages.Should().HaveCount(4); // Combined pages
            apiGroup!.Pages.Should().Contain("api/overview");
            apiGroup!.Pages.Should().Contain("api/endpoints");

            var guidesGroup = manager.Configuration!.Navigation!.Groups!.FirstOrDefault(g => g.Group == "Guides");
            guidesGroup.Should().NotBeNull();

            var examplesGroup = manager.Configuration!.Navigation!.Groups!.FirstOrDefault(g => g.Group == "Examples");
            examplesGroup.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that Merge intelligently combines Tabs with the same name.
        /// </summary>
        [TestMethod]
        public void Merge_TabsWithSameName_CombinesIntelligently()
        {
            var manager = new DocsJsonManager();
            var config1 = new DocsJsonConfig
            {
                Name = "Config 1",
                Navigation = new NavigationConfig
                {
                    Tabs =
                    [
                        new TabConfig
                        {
                            Tab = "API",
                            Pages = ["api/overview"]
                        },
                        new TabConfig
                        {
                            Tab = "Guides",
                            Href = "/guides",
                            Pages = ["guides/intro"]
                        }
                    ]
                }
            };
            manager.Load(JsonSerializer.Serialize(config1, MintlifyConstants.JsonSerializerOptions));

            var config2 = new DocsJsonConfig
            {
                Navigation = new NavigationConfig
                {
                    Tabs =
                    [
                        new TabConfig
                        {
                            Tab = "API",
                            Pages = ["api/reference"]
                        },
                        new TabConfig
                        {
                            Tab = "SDK",
                            Pages = ["sdk/installation"]
                        }
                    ]
                }
            };

            manager.Merge(config2);

            manager.Configuration!.Navigation!.Tabs!.Should().HaveCount(3);

            var apiTab = manager.Configuration!.Navigation!.Tabs!.FirstOrDefault(t => t.Tab == "API");
            apiTab.Should().NotBeNull();
            apiTab!.Pages.Should().HaveCount(2); // Combined pages
            apiTab!.Pages.Should().Contain("api/overview");
            apiTab!.Pages.Should().Contain("api/reference");

            var guidesTab = manager.Configuration!.Navigation!.Tabs!.FirstOrDefault(t => t.Tab == "Guides");
            guidesTab.Should().NotBeNull();

            var sdkTab = manager.Configuration!.Navigation!.Tabs!.FirstOrDefault(t => t.Tab == "SDK");
            sdkTab.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that Merge combines Tabs with the same href when names are different.
        /// </summary>
        [TestMethod]
        public void Merge_TabsWithSameHref_CombinesIntelligently()
        {
            var manager = new DocsJsonManager();
            var config1 = new DocsJsonConfig
            {
                Name = "Config 1",
                Navigation = new NavigationConfig
                {
                    Tabs =
                    [
                        new TabConfig
                        {
                            Tab = "Documentation",
                            Href = "/docs",
                            Pages = ["intro"]
                        }
                    ]
                }
            };
            manager.Load(JsonSerializer.Serialize(config1, MintlifyConstants.JsonSerializerOptions));

            var config2 = new DocsJsonConfig
            {
                Navigation = new NavigationConfig
                {
                    Tabs =
                    [
                        new TabConfig
                        {
                            Tab = "Docs", // Different name, same href
                            Href = "/docs",
                            Pages = ["quickstart"]
                        }
                    ]
                }
            };

            manager.Merge(config2);

            manager.Configuration!.Navigation!.Tabs!.Should().ContainSingle();

            var mergedTab = manager.Configuration!.Navigation!.Tabs![0];
            mergedTab.Tab.Should().Be("Docs"); // Source takes precedence
            mergedTab.Href.Should().Be("/docs");
            mergedTab.Pages.Should().HaveCount(2); // Combined pages
            mergedTab.Pages.Should().Contain("intro");
            mergedTab.Pages.Should().Contain("quickstart");
        }

        /// <summary>
        /// Tests that Merge handles nested Groups within Tabs correctly.
        /// </summary>
        [TestMethod]
        public void Merge_NestedGroupsInTabs_MergesCorrectly()
        {
            var manager = new DocsJsonManager();
            var config1 = new DocsJsonConfig
            {
                Name = "Config 1",
                Navigation = new NavigationConfig
                {
                    Tabs =
                    [
                        new TabConfig
                        {
                            Tab = "API",
                            Groups =
                            [
                                new GroupConfig
                                {
                                    Group = "Authentication",
                                    Pages = ["auth/oauth"]
                                }
                            ]
                        }
                    ]
                }
            };
            manager.Load(JsonSerializer.Serialize(config1, MintlifyConstants.JsonSerializerOptions));

            var config2 = new DocsJsonConfig
            {
                Navigation = new NavigationConfig
                {
                    Tabs =
                    [
                        new TabConfig
                        {
                            Tab = "API",
                            Groups =
                            [
                                new GroupConfig
                                {
                                    Group = "Authentication",
                                    Pages = ["auth/tokens"]
                                },
                                new GroupConfig
                                {
                                    Group = "Endpoints",
                                    Pages = ["endpoints/users"]
                                }
                            ]
                        }
                    ]
                }
            };

            manager.Merge(config2);

            var apiTab = manager.Configuration!.Navigation!.Tabs!.FirstOrDefault(t => t.Tab == "API");
            apiTab.Should().NotBeNull();
            apiTab!.Groups.Should().HaveCount(2);

            var authGroup = apiTab!.Groups!.FirstOrDefault(g => g.Group == "Authentication");
            authGroup.Should().NotBeNull();
            authGroup!.Pages.Should().HaveCount(2); // Combined auth pages
            authGroup!.Pages.Should().Contain("auth/oauth");
            authGroup!.Pages.Should().Contain("auth/tokens");

            var endpointsGroup = apiTab!.Groups!.FirstOrDefault(g => g.Group == "Endpoints");
            endpointsGroup.Should().NotBeNull();
        }

        #endregion
    }

}