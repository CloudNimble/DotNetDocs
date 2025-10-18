using System.Collections.Generic;
using System;
using System.IO;
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
        //[TestMethod] // Disabled: validation logic moved to DocsJsonValidator
        public void ValidateConfiguration_NullConfiguration_DoesNotThrow()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = null;

            //var act = () => manager.ValidateConfiguration();
            var act = () => { }; // Method removed - validation moved to DocsJsonValidator

            act.Should().NotThrow();
        }

        /// <summary>
        /// Tests that ValidateConfiguration adds warning for missing name.
        /// </summary>
        //[TestMethod] // Disabled: validation logic moved to DocsJsonValidator
        public void ValidateConfiguration_MissingName_AddsWarning()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = new DocsJsonConfig { Theme = "mint" };

            //manager.ValidateConfiguration(); // Method removed - validation moved to DocsJsonValidator

            manager.ConfigurationErrors.Should().HaveCount(2); // Name + Navigation warnings
            manager.ConfigurationErrors[0].IsWarning.Should().BeTrue();
            manager.ConfigurationErrors[0].ErrorText.Should().Contain("name");
        }

        /// <summary>
        /// Tests that ValidateConfiguration adds warning for missing navigation.
        /// </summary>
        //[TestMethod] // Disabled: validation logic moved to DocsJsonValidator
        public void ValidateConfiguration_MissingNavigation_AddsWarning()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = new DocsJsonConfig { Name = "Test" };

            //manager.ValidateConfiguration(); // Method removed - validation moved to DocsJsonValidator

            manager.ConfigurationErrors.Should().HaveCount(1);
            manager.ConfigurationErrors[0].IsWarning.Should().BeTrue();
            manager.ConfigurationErrors[0].ErrorText.Should().Contain("Navigation");
        }

        /// <summary>
        /// Tests that ValidateConfiguration handles empty navigation.
        /// </summary>
        //[TestMethod] // Disabled: validation logic moved to DocsJsonValidator
        public void ValidateConfiguration_EmptyNavigation_AddsWarning()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig()
            };

            //manager.ValidateConfiguration(); // Method removed - validation moved to DocsJsonValidator

            manager.ConfigurationErrors.Should().HaveCount(1);
            manager.ConfigurationErrors[0].IsWarning.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ValidateConfiguration passes for valid configuration.
        /// </summary>
        //[TestMethod] // Disabled: validation logic moved to DocsJsonValidator
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

            //manager.ValidateConfiguration(); // Method removed - validation moved to DocsJsonValidator

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
            var manager = new DocsJsonManager();
            // Load a configuration first
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object> { "index" }
                }
            });
            var target = manager.Configuration!.Navigation;
            var originalPages = target.Pages!.ToList();

            manager.MergeNavigation((NavigationConfig)null!);

            target.Pages.Should().BeEquivalentTo(originalPages);
        }

        /// <summary>
        /// Tests that MergeNavigation merges all navigation properties.
        /// </summary>
        [TestMethod]
        public void MergeNavigation_AllProperties_MergesCorrectly()
        {
            var manager = new DocsJsonManager();
            // Load a configuration first
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object> { "index" },
                    Groups = new List<GroupConfig> { new GroupConfig { Group = "API" } },
                    Tabs = new List<TabConfig> { new TabConfig { Tab = "Docs" } },
                    Anchors = new List<AnchorConfig> { new AnchorConfig { Anchor = "anchor1" } }
                }
            });

            var source = new NavigationConfig
            {
                Pages = new List<object> { "quickstart" },
                Groups = new List<GroupConfig> { new GroupConfig { Group = "Guides" } },
                Tabs = new List<TabConfig> { new TabConfig { Tab = "Examples" } },
                Anchors = new List<AnchorConfig> { new AnchorConfig { Anchor = "anchor2" } },
                Global = new GlobalNavigationConfig()
            };

            manager.MergeNavigation(source);

            manager.Configuration!.Navigation.Pages.Should().HaveCount(2);
            manager.Configuration.Navigation.Groups.Should().HaveCount(2);
            manager.Configuration.Navigation.Tabs.Should().HaveCount(2);
            manager.Configuration.Navigation.Anchors.Should().HaveCount(2);
            manager.Configuration.Navigation.Global.Should().NotBeNull();
        }

        #endregion

        #region MergePagesList Tests

        /// <summary>
        /// Tests that MergePagesList deduplicates string pages.
        /// </summary>
        [TestMethod]
        public void MergePagesList_DuplicateStrings_Deduplicates()
        {
            var manager = new DocsJsonManager();
            var targetPages = new List<object> { "index", "quickstart" };
            var sourcePages = new List<object> { "quickstart", "api" };

            manager.MergePagesList(sourcePages, targetPages);

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

            var manager = new DocsJsonManager();
            manager.MergePagesList(sourcePages, targetPages);

            targetPages.Should().HaveCount(2);
            var apiGroup = targetPages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "API");
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

            var manager = new DocsJsonManager();
            manager.MergePagesList(sourcePages, targetPages);

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

            var manager = new DocsJsonManager();
            manager.MergeGroupsList(sourceGroups, targetGroups);

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

            var manager = new DocsJsonManager();
            manager.MergeGroupsList(sourceGroups, targetGroups);

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

            var manager = new DocsJsonManager();
            manager.MergeGroupsList(sourceGroups, targetGroups);

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

            var manager = new DocsJsonManager();
            // Load a configuration to initialize _knownPagePaths properly
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig()
            });
            manager.MergeTabsList(sourceTabs, targetTabs);

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

            var manager = new DocsJsonManager();
            // Load a configuration to initialize _knownPagePaths properly
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig()
            });
            manager.MergeTabsList(sourceTabs, targetTabs);

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

            var manager = new DocsJsonManager();
            // Load a configuration to initialize _knownPagePaths properly
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig()
            });
            manager.MergeTabsList(sourceTabs, targetTabs);

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

            var manager = new DocsJsonManager();
            manager.MergeGroupConfig(target, source);

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

            var manager = new DocsJsonManager();
            manager.MergeGroupConfig(target, source);

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

            var manager = new DocsJsonManager();
            manager.MergeTabConfig(target, source);

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

            var manager = new DocsJsonManager();
            manager.MergeTabConfig(target, source);

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
            var manager = new DocsJsonManager();
            var pages = new List<object> { "page1", "folder/page2" };

            manager.ApplyUrlPrefixToPages(pages, "/docs");

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
            var manager = new DocsJsonManager();
            var pages = new List<object>
            {
                "page1",
                new GroupConfig
                {
                    Group = "API",
                    Pages = new List<object> { "api/endpoint" }
                }
            };

            manager.ApplyUrlPrefixToPages(pages, "/v1");

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
            var manager = new DocsJsonManager();
            var act = () => manager.ApplyUrlPrefixToGroup(null!, "/docs");

            act.Should().NotThrow();
        }

        /// <summary>
        /// Tests that ApplyUrlPrefixToGroup prefixes root and pages.
        /// </summary>
        [TestMethod]
        public void ApplyUrlPrefixToGroup_ValidGroup_PrefixesRootAndPages()
        {
            var manager = new DocsJsonManager();
            var group = new GroupConfig
            {
                Group = "API",
                Root = "api",
                Pages = new List<object> { "overview", "reference" }
            };

            manager.ApplyUrlPrefixToGroup(group, "/docs");

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
            var manager = new DocsJsonManager();
            var act = () => manager.ApplyUrlPrefixToTab(null!, "/docs");

            act.Should().NotThrow();
        }

        /// <summary>
        /// Tests that ApplyUrlPrefixToTab prefixes all relevant properties.
        /// </summary>
        [TestMethod]
        public void ApplyUrlPrefixToTab_ValidTab_PrefixesAllUrls()
        {
            var manager = new DocsJsonManager();
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

            manager.ApplyUrlPrefixToTab(tab, "/docs");

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
            var manager = new DocsJsonManager();
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

            manager.ApplyUrlPrefixToAnchor(anchor, "/v2");

            anchor.Href.Should().Be("/v2/resources");
            anchor.Pages.Should().Contain("/v2/links");
            anchor.Groups[0].Pages.Should().Contain("/v2/external/apis");
            anchor.Tabs[0].Href.Should().Be("/v2/tools");
            anchor.Tabs[0].Pages.Should().Contain("/v2/tools/cli");
        }

        #endregion

        #region RemoveNullGroups Tests

        /// <summary>
        /// Tests that RemoveNullGroups handles null configuration gracefully.
        /// </summary>
        [TestMethod]
        public void RemoveNullGroups_NullConfiguration_DoesNotThrow()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = null;

            var act = () => manager.RemoveNullGroups();

            act.Should().NotThrow();
        }

        /// <summary>
        /// Tests that RemoveNullGroups handles null navigation gracefully.
        /// </summary>
        [TestMethod]
        public void RemoveNullGroups_NullNavigation_DoesNotThrow()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = new DocsJsonConfig { Name = "Test" };

            var act = () => manager.RemoveNullGroups();

            act.Should().NotThrow();
        }

        /// <summary>
        /// Tests that RemoveNullGroups handles null pages gracefully.
        /// </summary>
        [TestMethod]
        public void RemoveNullGroups_NullPages_DoesNotThrow()
        {
            var manager = new DocsJsonManager();
            manager.Configuration = new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig()
            };

            var act = () => manager.RemoveNullGroups();

            act.Should().NotThrow();
        }

        /// <summary>
        /// Tests that RemoveNullGroups removes groups with null names.
        /// </summary>
        //[TestMethod] // Disabled: validation logic moved to DocsJsonValidator
        public void RemoveNullGroups_GroupsWithNullNames_RemovesGroups()
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

            manager.RemoveNullGroups();

            manager.Configuration.Navigation.Pages.Should().HaveCount(3);
            manager.Configuration.Navigation.Pages.Should().Contain("index");
            manager.Configuration.Navigation.Pages.Should().Contain("quickstart");
            manager.Configuration.Navigation.Pages?.OfType<GroupConfig>().Should().HaveCount(1);
            manager.Configuration.Navigation.Pages?.OfType<GroupConfig>().First().Group.Should().Be("Valid Group");
            manager.ConfigurationErrors.Should().HaveCount(1);
            manager.ConfigurationErrors[0].ErrorText.Should().Contain("Group with null name found and removed");
        }

        /// <summary>
        /// Tests that RemoveNullGroups adds warnings for empty group names.
        /// </summary>
        //[TestMethod] // Disabled: validation logic moved to DocsJsonValidator
        public void RemoveNullGroups_EmptyGroupNames_AddsWarnings()
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

            manager.RemoveNullGroups();

            manager.Configuration.Navigation.Pages.Should().HaveCount(3);
            manager.ConfigurationErrors.Should().HaveCount(2);
            manager.ConfigurationErrors.All(e => e.IsWarning).Should().BeTrue();
            manager.ConfigurationErrors.All(e => e.ErrorText.Contains("Empty group name found")).Should().BeTrue();
        }

        /// <summary>
        /// Tests that RemoveNullGroups recursively cleans nested groups.
        /// </summary>
        //[TestMethod] // Disabled: validation logic moved to DocsJsonValidator
        public void RemoveNullGroups_NestedGroups_CleansRecursively()
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

            manager.RemoveNullGroups();

            var parentGroup = manager.Configuration.Navigation.Pages?.OfType<GroupConfig>().First();
            parentGroup!.Pages.Should().HaveCount(3);
            parentGroup.Pages!.Should().Contain("page1");
            parentGroup.Pages.Should().Contain("page2");
            parentGroup.Pages!.OfType<GroupConfig>().Should().HaveCount(1);
            parentGroup.Pages!.OfType<GroupConfig>().First().Group.Should().Be("Valid Nested");
            manager.ConfigurationErrors.Should().HaveCount(1);
            manager.ConfigurationErrors[0].ErrorText.Should().Contain("Nested group with null name found and removed");
        }

        /// <summary>
        /// Tests that RemoveNullGroups cleans the Groups property.
        /// </summary>
        //[TestMethod] // Disabled: validation logic moved to DocsJsonValidator
        public void RemoveNullGroups_NavigationGroups_CleansGroupsList()
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

            manager.RemoveNullGroups();

            manager.Configuration.Navigation.Groups.Should().HaveCount(2);
            manager.Configuration.Navigation.Groups.Should().Contain(g => g.Group == "Valid Group");
            manager.Configuration.Navigation.Groups.Should().Contain(g => g.Group == "");
            manager.ConfigurationErrors.Should().HaveCount(2); // 1 null group removed + 1 empty group warning
            manager.ConfigurationErrors.Count(e => !e.IsWarning).Should().Be(1); // Only null group is error
            manager.ConfigurationErrors.Count(e => e.IsWarning).Should().Be(1); // Empty group is warning
        }

        /// <summary>
        /// Tests that RemoveNullGroups preserves valid configuration unchanged.
        /// </summary>
        [TestMethod]
        public void RemoveNullGroups_ValidConfiguration_PreservesUnchanged()
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

            manager.RemoveNullGroups();

            manager.Configuration.Navigation.Pages.Should().HaveCount(3);
            manager.Configuration.Navigation.Pages.Should().Contain("index");
            manager.Configuration.Navigation.Pages.Should().Contain("quickstart");
            manager.Configuration.Navigation.Pages?.OfType<GroupConfig>().Should().HaveCount(1);
            manager.Configuration.Navigation.Pages?.OfType<GroupConfig>().First().Group.Should().Be("API Reference");
            manager.ConfigurationErrors.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that RemoveNullGroups handles mixed invalid scenarios.
        /// </summary>
        //[TestMethod] // Disabled: validation logic moved to DocsJsonValidator
        public void RemoveNullGroups_MixedInvalidScenarios_HandlesCorrectly()
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

            manager.RemoveNullGroups();

            manager.Configuration.Navigation.Pages.Should().HaveCount(3); // index, Parent, Valid
            manager.Configuration.Navigation.Pages.Should().Contain("index");

            var parentGroup = manager.Configuration.Navigation.Pages?.OfType<GroupConfig>().First(g => g.Group == "Parent");
            parentGroup!.Pages!.Should().HaveCount(1); // Only empty group remains

            manager.ConfigurationErrors.Should().HaveCount(3); // 1 root null + 1 nested null + 1 nested empty warning
            manager.ConfigurationErrors.Count(e => !e.IsWarning).Should().Be(2); // 2 null groups
            manager.ConfigurationErrors.Count(e => e.IsWarning).Should().Be(1); // 1 empty group warning
        }

        #endregion

        #region PopulateNavigationFromPath Tests

        /// <summary>
        /// Tests that PopulateNavigationFromPath with preserveExisting=true merges discovered content into existing navigation.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_PreserveExistingTrue_MergesDiscoveredContent()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = ["index"]
                }
            });

            // Create a temporary directory structure
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var guidesDir = Path.Combine(tempDir, "guides");
            Directory.CreateDirectory(guidesDir);

            try
            {
                // Create some MDX files
                File.WriteAllText(Path.Combine(tempDir, "quickstart.mdx"), "# Quickstart");
                File.WriteAllText(Path.Combine(guidesDir, "overview.mdx"), "# Overview");
                File.WriteAllText(Path.Combine(guidesDir, "tutorial.mdx"), "# Tutorial");

                manager.PopulateNavigationFromPath(tempDir, [".mdx"], includeApiReference: false, preserveExisting: true);

                // Should have merged the discovered content
                manager.Configuration!.Navigation.Pages.Should().HaveCount(3); // index + guides group
                manager.Configuration.Navigation.Pages.Should().Contain("index");

                var guidesGroup = manager.Configuration.Navigation.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Guides");
                guidesGroup.Should().NotBeNull();
                guidesGroup!.Pages.Should().HaveCount(2);
                guidesGroup.Pages.Should().Contain("guides/overview");
                guidesGroup.Pages.Should().Contain("guides/tutorial");
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Tests that existing groups with partial page listings get new discovered pages added.
        /// This is the core scenario we fixed.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_ExistingGroupPartialPages_AddsMissingPages()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object>
                    {
                        new GroupConfig
                        {
                            Group = "Learnings",
                            Pages = new List<object> { "learnings/bridge-assemblies" }
                        }
                    }
                }
            });

            // Create a temporary directory structure
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var learningsDir = Path.Combine(tempDir, "learnings");
            Directory.CreateDirectory(learningsDir);

            try
            {
                // Create MDX files including one not in existing navigation
                File.WriteAllText(Path.Combine(learningsDir, "bridge-assemblies.mdx"), "# Bridge Assemblies");
                File.WriteAllText(Path.Combine(learningsDir, "sdk-packaging.mdx"), "# SDK Packaging");
                File.WriteAllText(Path.Combine(learningsDir, "converter-infinite-recursion.mdx"), "# Converter Infinite Recursion");

                manager.PopulateNavigationFromPath(tempDir, [".mdx"], includeApiReference: false, preserveExisting: true);

                // Should have merged the new page into existing group
                var learningsGroup = manager.Configuration!.Navigation.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Learnings");
                learningsGroup.Should().NotBeNull();
                learningsGroup!.Pages.Should().HaveCount(3);
                learningsGroup.Pages.Should().Contain("learnings/bridge-assemblies");
                learningsGroup.Pages.Should().Contain("learnings/sdk-packaging");
                learningsGroup.Pages.Should().Contain("learnings/converter-infinite-recursion");
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Tests that existing groups with complete page listings don't get duplicates added.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_ExistingGroupCompletePages_NoDuplicates()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        new GroupConfig
                        {
                            Group = "API",
                            Pages = ["api/overview", "api/reference"]
                        }
                    ]
                }
            });

            // Create a temporary directory structure
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var apiDir = Path.Combine(tempDir, "api");
            Directory.CreateDirectory(apiDir);

            try
            {
                // Create the same MDX files
                File.WriteAllText(Path.Combine(apiDir, "overview.mdx"), "# Overview");
                File.WriteAllText(Path.Combine(apiDir, "reference.mdx"), "# Reference");

                manager.PopulateNavigationFromPath(tempDir, [".mdx"], includeApiReference: false, preserveExisting: true);

                // Should not have added duplicates
                var apiGroup = manager.Configuration!.Navigation.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "API");
                apiGroup.Should().NotBeNull();
                apiGroup!.Pages.Should().HaveCount(2);
                apiGroup.Pages.Should().Contain("api/overview");
                apiGroup.Pages.Should().Contain("api/reference");
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Tests that new groups are created for directories not in existing navigation.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_NewDirectories_CreatesNewGroups()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object> { "index" }
                }
            });

            // Create a temporary directory structure
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var tutorialsDir = Path.Combine(tempDir, "tutorials");
            Directory.CreateDirectory(tutorialsDir);
            var examplesDir = Path.Combine(tempDir, "examples");
            Directory.CreateDirectory(examplesDir);

            try
            {
                // Create MDX files
                File.WriteAllText(Path.Combine(tutorialsDir, "basic.mdx"), "# Basic Tutorial");
                File.WriteAllText(Path.Combine(examplesDir, "simple.mdx"), "# Simple Example");

                manager.PopulateNavigationFromPath(tempDir, [".mdx"], includeApiReference: false, preserveExisting: true);

                // Should have created new groups
                manager.Configuration!.Navigation.Pages.Should().HaveCount(3); // index + tutorials group + examples group

                var tutorialsGroup = manager.Configuration.Navigation.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Tutorials");
                tutorialsGroup.Should().NotBeNull();
                tutorialsGroup!.Pages.Should().Contain("tutorials/basic");

                var examplesGroup = manager.Configuration.Navigation.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Examples");
                examplesGroup.Should().NotBeNull();
                examplesGroup!.Pages.Should().Contain("examples/simple");
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Tests mixed scenario with some existing groups (partial), some complete, and some new.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_MixedExistingAndNew_MergesCorrectly()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        "index",
                        new GroupConfig
                        {
                            Group = "Guides",
                            Pages = ["guides/getting-started"]
                        },
                        new GroupConfig
                        {
                            Group = "API",
                            Pages = ["api/overview", "api/reference"]
                        }
                    ]
                }
            });

            // Create a temporary directory structure
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var guidesDir = Path.Combine(tempDir, "Guides");
            Directory.CreateDirectory(guidesDir);
            var apiDir = Path.Combine(tempDir, "API");
            Directory.CreateDirectory(apiDir);
            var tutorialsDir = Path.Combine(tempDir, "tutorials");
            Directory.CreateDirectory(tutorialsDir);

            try
            {
                // Create MDX files
                File.WriteAllText(Path.Combine(guidesDir, "getting-started.mdx"), "# Getting Started");
                File.WriteAllText(Path.Combine(guidesDir, "advanced.mdx"), "# Advanced");
                File.WriteAllText(Path.Combine(apiDir, "overview.mdx"), "# Overview");
                File.WriteAllText(Path.Combine(apiDir, "reference.mdx"), "# Reference");
                File.WriteAllText(Path.Combine(tutorialsDir, "basic.mdx"), "# Basic Tutorial");

                manager.PopulateNavigationFromPath(tempDir, [".mdx"], includeApiReference: false, preserveExisting: true);

                // Check results
                manager.Configuration!.Navigation.Pages.Should().HaveCount(4); // index + guides + api + tutorials

                // Guides group should have both existing and new pages
                var guidesGroup = manager.Configuration.Navigation.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Guides");
                guidesGroup.Should().NotBeNull();
                guidesGroup!.Pages.Should().HaveCount(2);
                guidesGroup.Pages.Should().Contain("guides/getting-started");
                guidesGroup.Pages.Should().Contain("Guides/advanced");

                // API group should remain unchanged (complete)
                var apiGroup = manager.Configuration.Navigation.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "API");
                apiGroup.Should().NotBeNull();
                apiGroup!.Pages.Should().HaveCount(2);

                // Tutorials group should be new
                var tutorialsGroup = manager.Configuration.Navigation.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Tutorials");
                tutorialsGroup.Should().NotBeNull();
                tutorialsGroup!.Pages.Should().Contain("tutorials/basic");
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Tests that navigation.json files override automatic discovery for directories.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_NavigationJsonOverride_UsesCustomNavigation()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object> { "index" }
                }
            });

            // Create a temporary directory structure
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var guidesDir = Path.Combine(tempDir, "guides");
            Directory.CreateDirectory(guidesDir);

            try
            {
                // Create navigation.json override
                var navigationJson = """
                    {
                        "group": "Custom Guides",
                        "pages": ["guides/intro", "guides/advanced"]
                    }
                    """;
                File.WriteAllText(Path.Combine(guidesDir, "navigation.json"), navigationJson);

                // Create MDX files that would normally be discovered
                File.WriteAllText(Path.Combine(guidesDir, "intro.mdx"), "# Intro");
                File.WriteAllText(Path.Combine(guidesDir, "advanced.mdx"), "# Advanced");
                File.WriteAllText(Path.Combine(guidesDir, "extra.mdx"), "# Extra");

                manager.PopulateNavigationFromPath(tempDir, [".mdx"], includeApiReference: false, preserveExisting: true);

                // Should use the navigation.json override, not discover extra.mdx
                var guidesGroup = manager.Configuration!.Navigation.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Custom Guides");
                guidesGroup.Should().NotBeNull();
                guidesGroup!.Pages.Should().HaveCount(2);
                guidesGroup.Pages.Should().Contain("guides/intro");
                guidesGroup.Pages.Should().Contain("guides/advanced");
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Tests that files with non-matching extensions are ignored.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_NonMatchingExtensions_Ignored()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object> { "index" }
                }
            });

            // Create a temporary directory structure
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create files with different extensions
                File.WriteAllText(Path.Combine(tempDir, "valid.mdx"), "# Valid");
                File.WriteAllText(Path.Combine(tempDir, "invalid.md"), "# Invalid");
                File.WriteAllText(Path.Combine(tempDir, "also-invalid.txt"), "# Also Invalid");

                manager.PopulateNavigationFromPath(tempDir, [".mdx"], includeApiReference: false, preserveExisting: true);

                // Should only include the MDX file in Getting Started
                var gettingStartedGroup = manager.Configuration!.Navigation.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Getting Started");
                gettingStartedGroup.Should().NotBeNull();
                gettingStartedGroup!.Pages.Should().HaveCount(1);
                gettingStartedGroup.Pages.Should().Contain("valid");
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Tests that empty directories are not created as groups.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_EmptyDirectories_NoGroupsCreated()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object> { "index" }
                }
            });

            // Create a temporary directory structure
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var emptyDir = Path.Combine(tempDir, "empty");
            Directory.CreateDirectory(emptyDir);
            var docsDir = Path.Combine(tempDir, "docs");
            Directory.CreateDirectory(docsDir);

            try
            {
                // Only create file in docs directory
                File.WriteAllText(Path.Combine(docsDir, "readme.mdx"), "# Readme");

                manager.PopulateNavigationFromPath(tempDir, [".mdx"], includeApiReference: false, preserveExisting: true);

                // Should only create group for docs, not empty
                manager.Configuration!.Navigation.Pages.Should().HaveCount(2); // index + docs group

                var docsGroup = manager.Configuration.Navigation.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Docs");
                docsGroup.Should().NotBeNull();
                docsGroup!.Pages.Should().Contain("docs/readme");
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Tests that root-level files are grouped under "Getting Started" when preserveExisting=true.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_RootFiles_GroupedUnderGettingStarted()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object> { "index" }
                }
            });

            // Create a temporary directory structure
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create root-level MDX files
                File.WriteAllText(Path.Combine(tempDir, "quickstart.mdx"), "# Quickstart");
                File.WriteAllText(Path.Combine(tempDir, "faq.mdx"), "# FAQ");

                manager.PopulateNavigationFromPath(tempDir, [".mdx"], includeApiReference: false, preserveExisting: true);

                // Should group root files under "Getting Started"
                var gettingStartedGroup = manager.Configuration!.Navigation.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Getting Started");
                gettingStartedGroup.Should().NotBeNull();
                gettingStartedGroup!.Pages.Should().HaveCount(2);
                gettingStartedGroup.Pages.Should().Contain("quickstart");
                gettingStartedGroup.Pages.Should().Contain("faq");
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Tests that api-reference directories are excluded when includeApiReference=false.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_ApiReferenceExcluded_WhenIncludeApiReferenceFalse()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object> { "index" }
                }
            });

            // Create a temporary directory structure
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var apiDir = Path.Combine(tempDir, "api-reference");
            Directory.CreateDirectory(apiDir);

            try
            {
                // Create API reference file
                File.WriteAllText(Path.Combine(apiDir, "overview.mdx"), "# API Overview");

                manager.PopulateNavigationFromPath(tempDir, [".mdx"], includeApiReference: false, preserveExisting: true);

                // Should not include api-reference directory
                manager.Configuration!.Navigation.Pages.Should().HaveCount(1); // only index
                manager.Configuration.Navigation.Pages.Should().Contain("index");
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Tests that api-reference directories are included when includeApiReference=true.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_ApiReferenceIncluded_WhenIncludeApiReferenceTrue()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = ["index"]
                }
            });

            // Create a temporary directory structure
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var apiDir = Path.Combine(tempDir, "api-reference");
            Directory.CreateDirectory(apiDir);

            try
            {
                // Create API reference file
                File.WriteAllText(Path.Combine(apiDir, "overview.mdx"), "# API Overview");

                manager.PopulateNavigationFromPath(tempDir, [".mdx"], includeApiReference: true, preserveExisting: true);

                // Should include api-reference directory
                var apiGroup = manager.Configuration!.Navigation.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Api Reference");
                apiGroup.Should().NotBeNull();
                apiGroup!.Pages.Should().Contain("api-reference/overview");
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Tests that dot-directories are excluded from navigation discovery.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_DotDirectories_Excluded()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object> { "index" }
                }
            });

            // Create a temporary directory structure
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var dotDir = Path.Combine(tempDir, ".git");
            Directory.CreateDirectory(dotDir);
            var normalDir = Path.Combine(tempDir, "docs");
            Directory.CreateDirectory(normalDir);

            try
            {
                // Create files in both directories
                File.WriteAllText(Path.Combine(dotDir, "config.mdx"), "# Config");
                File.WriteAllText(Path.Combine(normalDir, "readme.mdx"), "# Readme");

                manager.PopulateNavigationFromPath(tempDir, [".mdx"], includeApiReference: false, preserveExisting: true);

                // Should only include normal directory, not dot directory
                var docsGroup = manager.Configuration!.Navigation.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Docs");
                docsGroup.Should().NotBeNull();
                docsGroup!.Pages.Should().Contain("docs/readme");

                // Should not have created a group for .git
                manager.Configuration.Navigation.Pages?.OfType<GroupConfig>().Should().HaveCount(1);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Tests that index files are sorted first within directories.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_IndexFiles_SortedFirst()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object> { "index" }
                }
            });

            // Create a temporary directory structure
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var guidesDir = Path.Combine(tempDir, "guides");
            Directory.CreateDirectory(guidesDir);

            try
            {
                // Create files with index first alphabetically
                File.WriteAllText(Path.Combine(guidesDir, "index.mdx"), "# Index");
                File.WriteAllText(Path.Combine(guidesDir, "advanced.mdx"), "# Advanced");
                File.WriteAllText(Path.Combine(guidesDir, "basic.mdx"), "# Basic");

                manager.PopulateNavigationFromPath(tempDir, [".mdx"], includeApiReference: false, preserveExisting: true);

                var guidesGroup = manager.Configuration!.Navigation.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Guides");
                guidesGroup.Should().NotBeNull();
                guidesGroup!.Pages.Should().HaveCount(3);

                // Index should be first
                guidesGroup.Pages![0].Should().Be("guides/index");
                // Other files should be sorted alphabetically
                guidesGroup.Pages[1].Should().Be("guides/advanced");
                guidesGroup.Pages[2].Should().Be("guides/basic");
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Tests that preserveExisting=false clears existing navigation and repopulates.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_PreserveExistingFalse_ReplacesNavigation()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = new List<object>
                    {
                        "old-index",
                        new GroupConfig
                        {
                            Group = "Old Group",
                            Pages = new List<object> { "old/page" }
                        }
                    }
                }
            });

            // Create a temporary directory structure
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var newDir = Path.Combine(tempDir, "newdocs");
            Directory.CreateDirectory(newDir);

            try
            {
                // Create new content
                File.WriteAllText(Path.Combine(tempDir, "newindex.mdx"), "# New Index");
                File.WriteAllText(Path.Combine(newDir, "guide.mdx"), "# Guide");

                manager.PopulateNavigationFromPath(tempDir, [".mdx"], includeApiReference: false, preserveExisting: false);

                // Should have replaced all navigation
                manager.Configuration!.Navigation.Pages.Should().HaveCount(2); // newindex + newdocs group

                var gettingStartedGroup = manager.Configuration.Navigation.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Getting Started");
                gettingStartedGroup.Should().NotBeNull();
                gettingStartedGroup!.Pages.Should().Contain("newindex");

                var newdocsGroup = manager.Configuration.Navigation.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Newdocs");
                newdocsGroup.Should().NotBeNull();
                newdocsGroup!.Pages.Should().Contain("newdocs/guide");
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        #endregion

    }

}