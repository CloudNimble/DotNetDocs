using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core
{

    /// <summary>
    /// Tests for internal methods of DocsJsonManager to ensure proper code coverage.
    /// </summary>
    [TestClass]
    public class DocsJsonManagerInternalTests
    {

        #region LoadInternal Tests

        /// <summary>
        /// Tests that LoadInternal successfully loads valid JSON content.
        /// </summary>
        [TestMethod]
        public void LoadInternal_ValidJson_LoadsSuccessfully()
        {
            var manager = new DocsJsonManager();
            var validJson = """
                {
                    "name": "Test API",
                    "theme": "mint",
                    "navigation": {
                        "pages": ["index", "quickstart"]
                    }
                }
                """;

            manager.LoadInternal(validJson);

            manager.Configuration.Should().NotBeNull();
            manager.Configuration!.Name.Should().Be("Test API");
            manager.ConfigurationErrors.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that LoadInternal handles invalid JSON gracefully.
        /// </summary>
        [TestMethod]
        public void LoadInternal_InvalidJson_AddsJsonError()
        {
            var manager = new DocsJsonManager();
            var invalidJson = "{ invalid json }";

            manager.LoadInternal(invalidJson);

            manager.Configuration.Should().BeNull();
            manager.ConfigurationErrors.Should().HaveCount(1);
            manager.ConfigurationErrors[0].ErrorNumber.Should().Be("JSON");
        }

        /// <summary>
        /// Tests that LoadInternal handles null JSON response.
        /// </summary>
        [TestMethod]
        public void LoadInternal_NullDeserialization_AddsError()
        {
            var manager = new DocsJsonManager();
            var nullJson = "null";

            manager.LoadInternal(nullJson);

            manager.Configuration.Should().BeNull();
            manager.ConfigurationErrors.Should().HaveCount(1);
            manager.ConfigurationErrors[0].ErrorNumber.Should().Be("JSON");
            manager.ConfigurationErrors[0].ErrorText.Should().Contain("Failed to deserialize");
        }

        /// <summary>
        /// Tests that LoadInternal clears previous errors.
        /// </summary>
        [TestMethod]
        public void LoadInternal_ClearsPreviousErrors()
        {
            var manager = new DocsJsonManager();

            // First load with invalid JSON
            manager.LoadInternal("{ invalid }");
            manager.ConfigurationErrors.Should().HaveCount(1);

            // Second load with valid JSON
            var validJson = """{"name": "Test", "navigation": {"pages": ["index"]}}""";
            manager.LoadInternal(validJson);

            manager.ConfigurationErrors.Should().BeEmpty();
        }

        #endregion

        #region ValidateConfiguration Tests

        /// <summary>
        /// Tests that ValidateConfiguration handles null configuration gracefully.
        /// </summary>
        [TestMethod]
        public void ValidateConfiguration_NullConfiguration_DoesNotThrow()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = null;

            var act = () => manager.ValidateConfiguration();

            act.Should().NotThrow();
        }

        /// <summary>
        /// Tests that ValidateConfiguration adds warning for missing name.
        /// </summary>
        [TestMethod]
        public void ValidateConfiguration_MissingName_AddsWarning()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = new DocsJsonConfig { Theme = "mint" };

            manager.ValidateConfiguration();

            manager.ConfigurationErrors.Should().HaveCount(2); // Name + Navigation warnings
            manager.ConfigurationErrors[0].IsWarning.Should().BeTrue();
            manager.ConfigurationErrors[0].ErrorText.Should().Contain("name");
        }

        /// <summary>
        /// Tests that ValidateConfiguration adds warning for missing navigation.
        /// </summary>
        [TestMethod]
        public void ValidateConfiguration_MissingNavigation_AddsWarning()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = new DocsJsonConfig { Name = "Test" };

            manager.ValidateConfiguration();

            manager.ConfigurationErrors.Should().HaveCount(1);
            manager.ConfigurationErrors[0].IsWarning.Should().BeTrue();
            manager.ConfigurationErrors[0].ErrorText.Should().Contain("Navigation");
        }

        /// <summary>
        /// Tests that ValidateConfiguration handles empty navigation.
        /// </summary>
        [TestMethod]
        public void ValidateConfiguration_EmptyNavigation_AddsWarning()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig()
            };

            manager.ValidateConfiguration();

            manager.ConfigurationErrors.Should().HaveCount(1);
            manager.ConfigurationErrors[0].IsWarning.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ValidateConfiguration passes for valid configuration.
        /// </summary>
        [TestMethod]
        public void ValidateConfiguration_ValidConfiguration_NoErrors()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = new DocsJsonConfig
            {
                Name = "Test API",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object> { "index" }
                }
            };

            manager.ValidateConfiguration();

            manager.ConfigurationErrors.Should().BeEmpty();
        }

        #endregion

        #region MergeNavigation Tests

        /// <summary>
        /// Tests that MergeNavigation handles null source navigation.
        /// </summary>
        [TestMethod]
        public void MergeNavigation_NullSource_DoesNotModifyTarget()
        {
            var target = new NavigationConfig
            {
                Pages = new List<object> { "index" }
            };
            var originalPages = target.Pages.ToList();

            DocsJsonManager.MergeNavigation(target, null!);

            target.Pages.Should().BeEquivalentTo(originalPages);
        }

        /// <summary>
        /// Tests that MergeNavigation merges all navigation properties.
        /// </summary>
        [TestMethod]
        public void MergeNavigation_AllProperties_MergesCorrectly()
        {
            var target = new NavigationConfig
            {
                Pages = new List<object> { "index" },
                Groups = new List<GroupConfig> { new GroupConfig { Group = "API" } },
                Tabs = new List<TabConfig> { new TabConfig { Tab = "Docs" } },
                Anchors = new List<AnchorConfig> { new AnchorConfig { Anchor = "anchor1" } }
            };

            var source = new NavigationConfig
            {
                Pages = new List<object> { "quickstart" },
                Groups = new List<GroupConfig> { new GroupConfig { Group = "Guides" } },
                Tabs = new List<TabConfig> { new TabConfig { Tab = "Examples" } },
                Anchors = new List<AnchorConfig> { new AnchorConfig { Anchor = "anchor2" } },
                Global = new GlobalNavigationConfig()
            };

            DocsJsonManager.MergeNavigation(target, source);

            target.Pages.Should().HaveCount(2);
            target.Groups.Should().HaveCount(2);
            target.Tabs.Should().HaveCount(2);
            target.Anchors.Should().HaveCount(2);
            target.Global.Should().NotBeNull();
        }

        #endregion

        #region MergePagesList Tests

        /// <summary>
        /// Tests that MergePagesList deduplicates string pages.
        /// </summary>
        [TestMethod]
        public void MergePagesList_DuplicateStrings_Deduplicates()
        {
            var targetPages = new List<object> { "index", "quickstart" };
            var sourcePages = new List<object> { "quickstart", "api" };

            DocsJsonManager.MergePagesList(targetPages, sourcePages);

            targetPages.Should().HaveCount(3);
            targetPages.Should().BeEquivalentTo(new[] { "api", "index", "quickstart" });
        }

        /// <summary>
        /// Tests that MergePagesList merges GroupConfig objects with same names.
        /// </summary>
        [TestMethod]
        public void MergePagesList_SameGroupNames_MergesGroups()
        {
            var targetPages = new List<object>
            {
                "index",
                new GroupConfig
                {
                    Group = "API",
                    Pages = new List<object> { "api/overview" }
                }
            };

            var sourcePages = new List<object>
            {
                new GroupConfig
                {
                    Group = "API",
                    Pages = new List<object> { "api/reference" }
                }
            };

            DocsJsonManager.MergePagesList(targetPages, sourcePages);

            targetPages.Should().HaveCount(2);
            var apiGroup = targetPages.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "API");
            apiGroup.Should().NotBeNull();
            apiGroup!.Pages.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that MergePagesList handles mixed content types.
        /// </summary>
        [TestMethod]
        public void MergePagesList_MixedTypes_HandlesCorrectly()
        {
            var targetPages = new List<object>
            {
                "index",
                new GroupConfig { Group = "API" },
                42 // Non-standard object
            };

            var sourcePages = new List<object>
            {
                "quickstart",
                new GroupConfig { Group = "Guides" },
                "test" // Some other object
            };

            DocsJsonManager.MergePagesList(targetPages, sourcePages);

            targetPages.Should().HaveCount(6);
            targetPages.Should().Contain("index");
            targetPages.Should().Contain("quickstart");
            targetPages.Should().Contain("test");
            targetPages.Should().Contain(42);
        }

        #endregion

        #region MergeGroupsList Tests

        /// <summary>
        /// Tests that MergeGroupsList combines groups with same names.
        /// </summary>
        [TestMethod]
        public void MergeGroupsList_SameNames_CombinesGroups()
        {
            var targetGroups = new List<GroupConfig>
            {
                new GroupConfig
                {
                    Group = "API",
                    Pages = new List<object> { "api/overview" }
                }
            };

            var sourceGroups = new List<GroupConfig>
            {
                new GroupConfig
                {
                    Group = "API",
                    Pages = new List<object> { "api/reference" }
                }
            };

            DocsJsonManager.MergeGroupsList(targetGroups, sourceGroups);

            targetGroups.Should().HaveCount(1);
            var apiGroup = targetGroups.FirstOrDefault(g => g.Group == "API");
            apiGroup.Should().NotBeNull();
            apiGroup!.Pages.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that MergeGroupsList keeps groups with different names separate.
        /// </summary>
        [TestMethod]
        public void MergeGroupsList_DifferentNames_KeepsSeparate()
        {
            var targetGroups = new List<GroupConfig>
            {
                new GroupConfig { Group = "API" }
            };

            var sourceGroups = new List<GroupConfig>
            {
                new GroupConfig { Group = "Guides" }
            };

            DocsJsonManager.MergeGroupsList(targetGroups, sourceGroups);

            targetGroups.Should().HaveCount(2);
            targetGroups.Should().Contain(g => g.Group == "API");
            targetGroups.Should().Contain(g => g.Group == "Guides");
        }

        /// <summary>
        /// Tests that MergeGroupsList handles groups without names.
        /// </summary>
        [TestMethod]
        public void MergeGroupsList_GroupsWithoutNames_HandlesCorrectly()
        {
            var targetGroups = new List<GroupConfig>
            {
                new GroupConfig { Group = null! },
                new GroupConfig { Group = "API" }
            };

            var sourceGroups = new List<GroupConfig>
            {
                new GroupConfig { Group = null! },
                new GroupConfig { Group = "" }
            };

            DocsJsonManager.MergeGroupsList(targetGroups, sourceGroups);

            targetGroups.Should().HaveCount(4); // All groups without names kept separate
        }

        #endregion

        #region MergeTabsList Tests

        /// <summary>
        /// Tests that MergeTabsList combines tabs with same names.
        /// </summary>
        [TestMethod]
        public void MergeTabsList_SameNames_CombinesTabs()
        {
            var targetTabs = new List<TabConfig>
            {
                new TabConfig
                {
                    Tab = "API",
                    Pages = new List<object> { "api/overview" }
                }
            };

            var sourceTabs = new List<TabConfig>
            {
                new TabConfig
                {
                    Tab = "API",
                    Pages = new List<object> { "api/reference" }
                }
            };

            DocsJsonManager.MergeTabsList(targetTabs, sourceTabs);

            targetTabs.Should().HaveCount(1);
            var apiTab = targetTabs.FirstOrDefault(t => t.Tab == "API");
            apiTab.Should().NotBeNull();
            apiTab!.Pages.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that MergeTabsList combines tabs with same href.
        /// </summary>
        [TestMethod]
        public void MergeTabsList_SameHref_CombinesTabs()
        {
            var targetTabs = new List<TabConfig>
            {
                new TabConfig
                {
                    Tab = "Documentation",
                    Href = "/docs",
                    Pages = new List<object> { "intro" }
                }
            };

            var sourceTabs = new List<TabConfig>
            {
                new TabConfig
                {
                    Tab = "Docs",
                    Href = "/docs",
                    Pages = new List<object> { "quickstart" }
                }
            };

            DocsJsonManager.MergeTabsList(targetTabs, sourceTabs);

            targetTabs.Should().HaveCount(1);
            var tab = targetTabs[0];
            tab.Tab.Should().Be("Docs"); // Source takes precedence
            tab.Href.Should().Be("/docs");
            tab.Pages.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that MergeTabsList preserves order.
        /// </summary>
        [TestMethod]
        public void MergeTabsList_PreservesOrder()
        {
            var targetTabs = new List<TabConfig>
            {
                new TabConfig { Tab = "First" },
                new TabConfig { Tab = "Second" }
            };

            var sourceTabs = new List<TabConfig>
            {
                new TabConfig { Tab = "Third" }
            };

            DocsJsonManager.MergeTabsList(targetTabs, sourceTabs);

            targetTabs.Should().HaveCount(3);
            targetTabs[0].Tab.Should().Be("First");
            targetTabs[1].Tab.Should().Be("Second");
            targetTabs[2].Tab.Should().Be("Third");
        }

        #endregion

        #region MergeGroupConfig Tests

        /// <summary>
        /// Tests that MergeGroupConfig merges all properties correctly.
        /// </summary>
        [TestMethod]
        public void MergeGroupConfig_AllProperties_MergesCorrectly()
        {
            var target = new GroupConfig
            {
                Group = "API",
                Pages = new List<object> { "api/overview" }
            };

            var source = new GroupConfig
            {
                Tag = "v1",
                Root = "/api",
                Hidden = true,
                Icon = "api-icon",
                AsyncApi = "async-config",
                OpenApi = "openapi-config",
                Pages = new List<object> { "api/reference" }
            };

            DocsJsonManager.MergeGroupConfig(target, source);

            target.Tag.Should().Be("v1");
            target.Root.Should().Be("/api");
            target.Hidden.Should().BeTrue();
            ((string?)target.Icon).Should().Be("api-icon");
            ((string?)target.AsyncApi).Should().Be("async-config");
            ((string?)target.OpenApi).Should().Be("openapi-config");
            target.Pages.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that MergeGroupConfig handles null pages in target.
        /// </summary>
        [TestMethod]
        public void MergeGroupConfig_NullTargetPages_CopiesSourcePages()
        {
            var target = new GroupConfig { Group = "API" };
            var source = new GroupConfig
            {
                Pages = new List<object> { "api/reference" }
            };

            DocsJsonManager.MergeGroupConfig(target, source);

            target.Pages.Should().HaveCount(1);
            target.Pages.Should().Contain("api/reference");
        }

        #endregion

        #region MergeTabConfig Tests

        /// <summary>
        /// Tests that MergeTabConfig merges all properties correctly.
        /// </summary>
        [TestMethod]
        public void MergeTabConfig_AllProperties_MergesCorrectly()
        {
            var target = new TabConfig
            {
                Tab = "API",
                Pages = new List<object> { "api/overview" }
            };

            var source = new TabConfig
            {
                Href = "/api",
                Hidden = true,
                Icon = "api-icon",
                AsyncApi = "async-config",
                OpenApi = "openapi-config",
                Pages = new List<object> { "api/reference" },
                Groups = new List<GroupConfig> { new GroupConfig { Group = "Endpoints" } },
                Anchors = new List<AnchorConfig> { new AnchorConfig { Anchor = "test" } },
                Dropdowns = new List<DropdownConfig> { new DropdownConfig { Dropdown = "dropdown" } },
                Languages = new List<LanguageConfig> { new LanguageConfig { Language = "en" } },
                Versions = new List<VersionConfig> { new VersionConfig { Version = "v1" } },
                Global = new GlobalNavigationConfig()
            };

            DocsJsonManager.MergeTabConfig(target, source);

            target.Href.Should().Be("/api");
            target.Hidden.Should().BeTrue();
            ((string?)target.Icon).Should().Be("api-icon");
            ((string?)target.AsyncApi).Should().Be("async-config");
            ((string?)target.OpenApi).Should().Be("openapi-config");
            target.Pages.Should().HaveCount(2);
            target.Groups.Should().HaveCount(1);
            target.Anchors.Should().HaveCount(1);
            target.Dropdowns.Should().HaveCount(1);
            target.Languages.Should().HaveCount(1);
            target.Versions.Should().HaveCount(1);
            target.Global.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that MergeTabConfig handles null collections in target.
        /// </summary>
        [TestMethod]
        public void MergeTabConfig_NullCollections_InitializesCollections()
        {
            var target = new TabConfig { Tab = "API" };
            var source = new TabConfig
            {
                Pages = new List<object> { "page1" },
                Groups = new List<GroupConfig> { new GroupConfig { Group = "group1" } },
                Anchors = new List<AnchorConfig> { new AnchorConfig { Anchor = "anchor1" } }
            };

            DocsJsonManager.MergeTabConfig(target, source);

            target.Pages.Should().HaveCount(1);
            target.Groups.Should().HaveCount(1);
            target.Anchors.Should().HaveCount(1);
        }

        #endregion

        #region FormatGroupName Tests

        /// <summary>
        /// Tests that FormatGroupName correctly formats directory names.
        /// </summary>
        [TestMethod]
        public void FormatGroupName_VariousFormats_FormatsCorrectly()
        {
            DocsJsonManager.FormatGroupName("getting-started").Should().Be("Getting Started");
            DocsJsonManager.FormatGroupName("api_reference").Should().Be("Api Reference");
            DocsJsonManager.FormatGroupName("user-guide_v2").Should().Be("User Guide V2");
            DocsJsonManager.FormatGroupName("FAQ").Should().Be("Faq");
            DocsJsonManager.FormatGroupName("simple").Should().Be("Simple");
        }

        #endregion

        #region ApplyUrlPrefix Helper Tests

        /// <summary>
        /// Tests that ApplyUrlPrefixToPages correctly prefixes string pages.
        /// </summary>
        [TestMethod]
        public void ApplyUrlPrefixToPages_StringPages_AppliesPrefix()
        {
            var pages = new List<object> { "page1", "folder/page2" };

            DocsJsonManager.ApplyUrlPrefixToPages(pages, "/docs");

            pages.Should().HaveCount(2);
            pages[0].Should().Be("/docs/page1");
            pages[1].Should().Be("/docs/folder/page2");
        }

        /// <summary>
        /// Tests that ApplyUrlPrefixToPages correctly handles mixed content.
        /// </summary>
        [TestMethod]
        public void ApplyUrlPrefixToPages_MixedContent_AppliesPrefixToAll()
        {
            var pages = new List<object>
            {
                "page1",
                new GroupConfig
                {
                    Group = "API",
                    Pages = new List<object> { "api/endpoint" }
                }
            };

            DocsJsonManager.ApplyUrlPrefixToPages(pages, "/v1");

            pages[0].Should().Be("/v1/page1");
            var group = pages[1] as GroupConfig;
            group!.Pages![0].Should().Be("/v1/api/endpoint");
        }

        /// <summary>
        /// Tests that ApplyUrlPrefixToGroup handles null group.
        /// </summary>
        [TestMethod]
        public void ApplyUrlPrefixToGroup_NullGroup_DoesNotThrow()
        {
            var act = () => DocsJsonManager.ApplyUrlPrefixToGroup(null!, "/docs");

            act.Should().NotThrow();
        }

        /// <summary>
        /// Tests that ApplyUrlPrefixToGroup prefixes root and pages.
        /// </summary>
        [TestMethod]
        public void ApplyUrlPrefixToGroup_ValidGroup_PrefixesRootAndPages()
        {
            var group = new GroupConfig
            {
                Group = "API",
                Root = "api",
                Pages = new List<object> { "overview", "reference" }
            };

            DocsJsonManager.ApplyUrlPrefixToGroup(group, "/docs");

            group.Root.Should().Be("/docs/api");
            group.Pages.Should().Contain("/docs/overview");
            group.Pages.Should().Contain("/docs/reference");
        }

        /// <summary>
        /// Tests that ApplyUrlPrefixToTab handles null tab.
        /// </summary>
        [TestMethod]
        public void ApplyUrlPrefixToTab_NullTab_DoesNotThrow()
        {
            var act = () => DocsJsonManager.ApplyUrlPrefixToTab(null!, "/docs");

            act.Should().NotThrow();
        }

        /// <summary>
        /// Tests that ApplyUrlPrefixToTab prefixes all relevant properties.
        /// </summary>
        [TestMethod]
        public void ApplyUrlPrefixToTab_ValidTab_PrefixesAllUrls()
        {
            var tab = new TabConfig
            {
                Tab = "Examples",
                Href = "examples",
                Pages = new List<object> { "basic" },
                Groups = new List<GroupConfig>
                {
                    new GroupConfig
                    {
                        Group = "Advanced",
                        Pages = new List<object> { "advanced/topic" }
                    }
                }
            };

            DocsJsonManager.ApplyUrlPrefixToTab(tab, "/docs");

            tab.Href.Should().Be("/docs/examples");
            tab.Pages.Should().Contain("/docs/basic");
            tab.Groups[0].Pages.Should().Contain("/docs/advanced/topic");
        }

        /// <summary>
        /// Tests that ApplyUrlPrefixToAnchor handles nested structures.
        /// </summary>
        [TestMethod]
        public void ApplyUrlPrefixToAnchor_NestedStructure_PrefixesAllLevels()
        {
            var anchor = new AnchorConfig
            {
                Anchor = "Resources",
                Href = "resources",
                Pages = new List<object> { "links" },
                Groups = new List<GroupConfig>
                {
                    new GroupConfig
                    {
                        Group = "External",
                        Pages = new List<object> { "external/apis" }
                    }
                },
                Tabs = new List<TabConfig>
                {
                    new TabConfig
                    {
                        Tab = "Tools",
                        Href = "tools",
                        Pages = new List<object> { "tools/cli" }
                    }
                }
            };

            DocsJsonManager.ApplyUrlPrefixToAnchor(anchor, "/v2");

            anchor.Href.Should().Be("/v2/resources");
            anchor.Pages.Should().Contain("/v2/links");
            anchor.Groups[0].Pages.Should().Contain("/v2/external/apis");
            anchor.Tabs[0].Href.Should().Be("/v2/tools");
            anchor.Tabs[0].Pages.Should().Contain("/v2/tools/cli");
        }

        #endregion

        #region CleanNavigationGroups Tests

        /// <summary>
        /// Tests that CleanNavigationGroups handles null configuration gracefully.
        /// </summary>
        [TestMethod]
        public void CleanNavigationGroups_NullConfiguration_DoesNotThrow()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = null;

            var act = () => manager.CleanNavigationGroups();

            act.Should().NotThrow();
        }

        /// <summary>
        /// Tests that CleanNavigationGroups handles null navigation gracefully.
        /// </summary>
        [TestMethod]
        public void CleanNavigationGroups_NullNavigation_DoesNotThrow()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = new DocsJsonConfig { Name = "Test" };

            var act = () => manager.CleanNavigationGroups();

            act.Should().NotThrow();
        }

        /// <summary>
        /// Tests that CleanNavigationGroups handles null pages gracefully.
        /// </summary>
        [TestMethod]
        public void CleanNavigationGroups_NullPages_DoesNotThrow()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig()
            };

            var act = () => manager.CleanNavigationGroups();

            act.Should().NotThrow();
        }

        /// <summary>
        /// Tests that CleanNavigationGroups removes groups with null names.
        /// </summary>
        [TestMethod]
        public void CleanNavigationGroups_GroupsWithNullNames_RemovesGroups()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object>
                    {
                        "index",
                        new GroupConfig { Group = null! },
                        new GroupConfig { Group = "Valid Group" },
                        "quickstart"
                    }
                }
            };

            manager.CleanNavigationGroups();

            manager.Configuration.Navigation.Pages.Should().HaveCount(3);
            manager.Configuration.Navigation.Pages.Should().Contain("index");
            manager.Configuration.Navigation.Pages.Should().Contain("quickstart");
            manager.Configuration.Navigation.Pages.OfType<GroupConfig>().Should().HaveCount(1);
            manager.Configuration.Navigation.Pages.OfType<GroupConfig>().First().Group.Should().Be("Valid Group");
            manager.ConfigurationErrors.Should().HaveCount(1);
            manager.ConfigurationErrors[0].ErrorText.Should().Contain("Group with null name found and removed");
        }

        /// <summary>
        /// Tests that CleanNavigationGroups adds warnings for empty group names.
        /// </summary>
        [TestMethod]
        public void CleanNavigationGroups_EmptyGroupNames_AddsWarnings()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object>
                    {
                        new GroupConfig { Group = "" },
                        new GroupConfig { Group = "   " },
                        new GroupConfig { Group = "Valid Group" }
                    }
                }
            };

            manager.CleanNavigationGroups();

            manager.Configuration.Navigation.Pages.Should().HaveCount(3);
            manager.ConfigurationErrors.Should().HaveCount(2);
            manager.ConfigurationErrors.All(e => e.IsWarning).Should().BeTrue();
            manager.ConfigurationErrors.All(e => e.ErrorText.Contains("Empty group name found")).Should().BeTrue();
        }

        /// <summary>
        /// Tests that CleanNavigationGroups recursively cleans nested groups.
        /// </summary>
        [TestMethod]
        public void CleanNavigationGroups_NestedGroups_CleansRecursively()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object>
                    {
                        new GroupConfig
                        {
                            Group = "Parent Group",
                            Pages = new List<object>
                            {
                                "page1",
                                new GroupConfig { Group = null! },
                                new GroupConfig { Group = "Valid Nested" },
                                "page2"
                            }
                        }
                    }
                }
            };

            manager.CleanNavigationGroups();

            var parentGroup = manager.Configuration.Navigation.Pages.OfType<GroupConfig>().First();
            parentGroup.Pages.Should().HaveCount(3);
            parentGroup.Pages.Should().Contain("page1");
            parentGroup.Pages.Should().Contain("page2");
            parentGroup.Pages!.OfType<GroupConfig>().Should().HaveCount(1);
            parentGroup.Pages!.OfType<GroupConfig>().First().Group.Should().Be("Valid Nested");
            manager.ConfigurationErrors.Should().HaveCount(1);
            manager.ConfigurationErrors[0].ErrorText.Should().Contain("Nested group with null name found and removed");
        }

        /// <summary>
        /// Tests that CleanNavigationGroups cleans the Groups property.
        /// </summary>
        [TestMethod]
        public void CleanNavigationGroups_NavigationGroups_CleansGroupsList()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object> { "index" },
                    Groups = new List<GroupConfig>
                    {
                        new GroupConfig { Group = "Valid Group" },
                        new GroupConfig { Group = null! },
                        new GroupConfig { Group = "" }
                    }
                }
            };

            manager.CleanNavigationGroups();

            manager.Configuration.Navigation.Groups.Should().HaveCount(2);
            manager.Configuration.Navigation.Groups.Should().Contain(g => g.Group == "Valid Group");
            manager.Configuration.Navigation.Groups.Should().Contain(g => g.Group == "");
            manager.ConfigurationErrors.Should().HaveCount(2); // 1 null group removed + 1 empty group warning
            manager.ConfigurationErrors.Count(e => !e.IsWarning).Should().Be(1); // Only null group is error
            manager.ConfigurationErrors.Count(e => e.IsWarning).Should().Be(1); // Empty group is warning
        }

        /// <summary>
        /// Tests that CleanNavigationGroups preserves valid configuration unchanged.
        /// </summary>
        [TestMethod]
        public void CleanNavigationGroups_ValidConfiguration_PreservesUnchanged()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object>
                    {
                        "index",
                        new GroupConfig
                        {
                            Group = "API Reference",
                            Pages = new List<object> { "api/overview" }
                        },
                        "quickstart"
                    }
                }
            };

            manager.CleanNavigationGroups();

            manager.Configuration.Navigation.Pages.Should().HaveCount(3);
            manager.Configuration.Navigation.Pages.Should().Contain("index");
            manager.Configuration.Navigation.Pages.Should().Contain("quickstart");
            manager.Configuration.Navigation.Pages.OfType<GroupConfig>().Should().HaveCount(1);
            manager.Configuration.Navigation.Pages.OfType<GroupConfig>().First().Group.Should().Be("API Reference");
            manager.ConfigurationErrors.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that CleanNavigationGroups handles mixed invalid scenarios.
        /// </summary>
        [TestMethod]
        public void CleanNavigationGroups_MixedInvalidScenarios_HandlesCorrectly()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object>
                    {
                        "index",
                        new GroupConfig { Group = null! },
                        new GroupConfig
                        {
                            Group = "Parent",
                            Pages = new List<object>
                            {
                                new GroupConfig { Group = "" },
                                new GroupConfig { Group = null! }
                            }
                        },
                        new GroupConfig { Group = "Valid" }
                    }
                }
            };

            manager.CleanNavigationGroups();

            manager.Configuration.Navigation.Pages.Should().HaveCount(3); // index, Parent, Valid
            manager.Configuration.Navigation.Pages.Should().Contain("index");

            var parentGroup = manager.Configuration.Navigation.Pages.OfType<GroupConfig>().First(g => g.Group == "Parent");
            parentGroup.Pages!.Should().HaveCount(1); // Only empty group remains

            manager.ConfigurationErrors.Should().HaveCount(3); // 1 root null + 1 nested null + 1 nested empty warning
            manager.ConfigurationErrors.Count(e => !e.IsWarning).Should().Be(2); // 2 null groups
            manager.ConfigurationErrors.Count(e => e.IsWarning).Should().Be(1); // 1 empty group warning
        }

        #endregion

    }

}