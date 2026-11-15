using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        #region Known Paths Tests

        /// <summary>
        /// Tests that Load(DocsJsonConfig) tracks pages from every navigation container, not just the top-level pages list.
        /// </summary>
        [TestMethod]
        public void Load_DocsJsonConfig_TracksPagesInEveryNavigationContainer()
        {
            var manager = new DocsJsonManager();
            manager.Load(CreateConfigWithEveryNavigationContainer());

            foreach (var page in EveryNavigationContainerPages)
            {
                manager.IsPathKnown(page).Should().BeTrue($"'{page}' is listed in the navigation");
            }
        }

        /// <summary>
        /// Tests that Load(string) tracks the navigation pages in the JSON content.
        /// </summary>
        [TestMethod]
        public void Load_String_TracksNavigationPages()
        {
            var manager = new DocsJsonManager();
            manager.Load("""
                {
                  "name": "Test",
                  "navigation": {
                    "pages": ["index"],
                    "groups": [{ "group": "Guides", "root": "guides/overview", "pages": ["guides/setup"] }],
                    "tabs": [{ "tab": "API", "pages": ["api/intro"] }]
                  }
                }
                """);

            manager.IsPathKnown("index").Should().BeTrue();
            manager.IsPathKnown("guides/overview").Should().BeTrue();
            manager.IsPathKnown("guides/setup").Should().BeTrue();
            manager.IsPathKnown("api/intro").Should().BeTrue();
        }

        /// <summary>
        /// Tests that loading a new configuration forgets the pages from the previous configuration.
        /// </summary>
        [TestMethod]
        public void Load_NewConfiguration_ForgetsPagesFromPreviousConfiguration()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig { Name = "First", Navigation = new NavigationConfig { Pages = ["first-only"] } });

            manager.Load(new DocsJsonConfig { Name = "Second", Navigation = new NavigationConfig { Pages = ["second-only"] } });

            manager.IsPathKnown("first-only").Should().BeFalse();
            manager.IsPathKnown("second-only").Should().BeTrue();
        }

        /// <summary>
        /// Tests that MergeNavigation tracks pages from merged tabs and groups.
        /// </summary>
        [TestMethod]
        public void MergeNavigation_TracksPagesFromMergedTabsAndGroups()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig { Name = "Test", Navigation = new NavigationConfig { Pages = ["index"] } });

            manager.MergeNavigation(new NavigationConfig
            {
                Groups = [new GroupConfig { Group = "Guides", Root = "guides/overview", Pages = ["guides/setup"] }],
                Tabs = [new TabConfig { Tab = "API", Pages = ["api/intro"] }]
            });

            manager.IsPathKnown("guides/overview").Should().BeTrue();
            manager.IsPathKnown("guides/setup").Should().BeTrue();
            manager.IsPathKnown("api/intro").Should().BeTrue();
        }

        /// <summary>
        /// Tests that combining the loaded configuration's own empty groups keeps every page, even though those pages are already known.
        /// </summary>
        [TestMethod]
        public void Merge_WithCombineEmptyGroupsOption_KeepsPagesFromLoadedEmptyGroups()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        new GroupConfig { Group = "", Pages = ["page1"] },
                        new GroupConfig { Group = "", Pages = ["page2"] }
                    ]
                }
            });

            manager.Merge(new DocsJsonConfig { Navigation = new NavigationConfig { Pages = [new GroupConfig { Group = "", Pages = ["page3"] }] } },
                options: new MergeOptions { CombineEmptyGroups = true });

            var emptyGroup = manager.Configuration!.Navigation!.Pages!.OfType<GroupConfig>().Should().ContainSingle().Subject;
            emptyGroup.Pages.Should().BeEquivalentTo(["page1", "page2", "page3"]);
            manager.IsPathKnown("page2").Should().BeTrue();
        }

        /// <summary>
        /// Tests that merging into a configuration with an empty navigation tracks every page from the merged navigation.
        /// </summary>
        [TestMethod]
        public void Merge_IntoEmptyNavigation_TracksMergedPages()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig { Name = "Test" });

            manager.Merge(new DocsJsonConfig
            {
                Navigation = new NavigationConfig
                {
                    Pages = ["index"],
                    Tabs = [new TabConfig { Tab = "Guides", Groups = [new GroupConfig { Group = "Basics", Root = "guides/overview", Pages = ["guides/setup"] }] }]
                }
            });

            manager.IsPathKnown("index").Should().BeTrue();
            manager.IsPathKnown("guides/overview").Should().BeTrue();
            manager.IsPathKnown("guides/setup").Should().BeTrue();
        }

        /// <summary>
        /// Tests that a page the template lists in more than one section stays in every section after discovery, and is not added again.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_PageListedInMultipleSections_KeepsEveryReference()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = [new GroupConfig { Group = "Guides", Pages = ["shared", "guides/setup"] }],
                    Groups = [new GroupConfig { Group = "Reference", Root = "shared", Pages = ["reference/intro"] }],
                    Versions = [new VersionConfig { Version = "v1", Pages = ["shared"] }]
                }
            });

            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                Directory.CreateDirectory(tempPath);
                File.WriteAllText(Path.Combine(tempPath, "shared.mdx"), "# Shared");

                manager.PopulateNavigationFromPath(tempPath, preserveExisting: true);

                var navigation = manager.Configuration!.Navigation!;
                var pages = new List<string>();
                CollectRootsAndPages(navigation.Pages!, pages);
                pages.Count(p => p == "shared").Should().Be(1, "the Guides reference stays and discovery does not add another");
                navigation.Groups![0].Root.Should().Be("shared");
                navigation.Versions![0].Pages.Should().ContainSingle().Which.Should().Be("shared");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        /// <summary>
        /// Tests that merging a tab that lists a page another tab already lists keeps the page in both tabs.
        /// </summary>
        [TestMethod]
        public void Merge_TabListingPageFromAnotherTab_KeepsPageInBothTabs()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Tabs =
                    [
                        new TabConfig { Tab = "Guides", Pages = ["shared"] },
                        new TabConfig { Tab = "API", Pages = ["api/intro"] }
                    ]
                }
            });

            manager.Merge(new DocsJsonConfig
            {
                Navigation = new NavigationConfig { Tabs = [new TabConfig { Tab = "API", Pages = ["shared"] }] }
            });

            var tabs = manager.Configuration!.Navigation!.Tabs!;
            tabs.Single(t => t.Tab == "Guides").Pages.Should().BeEquivalentTo(["shared"]);
            tabs.Single(t => t.Tab == "API").Pages.Should().BeEquivalentTo(["api/intro", "shared"]);
        }

        /// <summary>
        /// Tests that merging pages that list a page a tab already lists keeps the page in both places.
        /// </summary>
        [TestMethod]
        public void Merge_PagesListingPageFromTab_KeepsPageInBothPlaces()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = ["index"],
                    Tabs = [new TabConfig { Tab = "Guides", Pages = ["shared"] }]
                }
            });

            manager.Merge(new DocsJsonConfig { Navigation = new NavigationConfig { Pages = ["shared"] } });

            var navigation = manager.Configuration!.Navigation!;
            navigation.Pages.Should().BeEquivalentTo(["index", "shared"]);
            navigation.Tabs![0].Pages.Should().BeEquivalentTo(["shared"]);
        }

        /// <summary>
        /// Tests that PopulateNavigationFromPath does not add a group's root page again, which is the shape the SDK's MintlifyTemplate produces.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_GroupRootInPages_IsNotAddedAgain()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Pages = [new GroupConfig { Group = "Episodes", Root = "overview", Pages = ["episode-1"] }]
                }
            });

            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                Directory.CreateDirectory(tempPath);
                File.WriteAllText(Path.Combine(tempPath, "overview.mdx"), "# Overview");
                File.WriteAllText(Path.Combine(tempPath, "episode-1.mdx"), "# Episode 1");

                manager.PopulateNavigationFromPath(tempPath, preserveExisting: true);

                var flat = new List<string>();
                CollectRootsAndPages(manager.Configuration!.Navigation!.Pages!, flat);
                flat.Count(p => p == "overview").Should().Be(1);
            }
            finally
            {
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        /// <summary>
        /// Tests that PopulateNavigationFromPath does not add pages the template already lists outside the top-level pages list.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_PagesListedInGroupsOrRoots_AreNotAddedAgain()
        {
            var manager = new DocsJsonManager();
            manager.Load(new DocsJsonConfig
            {
                Name = "Test",
                Navigation = new NavigationConfig
                {
                    Groups =
                    [
                        new GroupConfig { Group = "Getting Started", Root = "overview", Pages = ["index"] }
                    ]
                }
            });

            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                Directory.CreateDirectory(tempPath);
                File.WriteAllText(Path.Combine(tempPath, "index.mdx"), "# Index");
                File.WriteAllText(Path.Combine(tempPath, "overview.mdx"), "# Overview");
                File.WriteAllText(Path.Combine(tempPath, "new-page.mdx"), "# New Page");

                manager.PopulateNavigationFromPath(tempPath, preserveExisting: true);

                var topLevelPages = GetAllPagesFlat(manager.Configuration!.Navigation!.Pages!);
                topLevelPages.Should().Contain("new-page");
                topLevelPages.Should().NotContain("index");
                topLevelPages.Should().NotContain("overview");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Flattens a pages list into page paths, including each group's root page.
        /// </summary>
        private static void CollectRootsAndPages(List<object> pages, List<string> paths)
        {
            foreach (var page in pages)
            {
                switch (page)
                {
                    case string stringPage:
                        paths.Add(stringPage);
                        break;
                    case GroupConfig group:
                        if (!string.IsNullOrWhiteSpace(group.Root))
                        {
                            paths.Add(group.Root!);
                        }
                        if (group.Pages is not null)
                        {
                            CollectRootsAndPages(group.Pages, paths);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Gets every page path that <see cref="CreateConfigWithEveryNavigationContainer"/> places in its navigation.
        /// </summary>
        private static IEnumerable<string> EveryNavigationContainerPages =>
        [
            "top/page",
            "top/group-root",
            "top/group-page",
            "top/nested-group-page",
            "groups/root",
            "groups/page",
            "tab/page",
            "tab/group-page",
            "tab/menu-page",
            "tab/menu-group-page",
            "tab/anchor-page",
            "anchor/page",
            "anchor/tab-page",
            "dropdown/page",
            "dropdown/anchor-page",
            "dropdown/tab-page",
            "product/page",
            "product/group-page",
            "version/page",
            "version/group-page",
            "version/tab-page",
            "version/anchor-page",
            "version/dropdown-page",
            "version/language-page",
            "language/page",
            "language/group-page",
            "language/tab-page",
            "language/anchor-page",
            "language/dropdown-page",
            "language/version-page",
            "section/dropdown-page",
            "section/language-page",
            "section/version-page"
        ];

        /// <summary>
        /// Creates a configuration that lists a page in every kind of navigation container Mintlify supports.
        /// </summary>
        private static DocsJsonConfig CreateConfigWithEveryNavigationContainer() => new()
        {
            Name = "Test",
            Navigation = new NavigationConfig
            {
                Pages =
                [
                    "top/page",
                    new GroupConfig
                    {
                        Group = "Top Group",
                        Root = "top/group-root",
                        Pages =
                        [
                            "top/group-page",
                            new GroupConfig { Group = "Nested", Pages = ["top/nested-group-page"] }
                        ]
                    },
                    new NavigationLinkConfig { Title = "External", Href = "https://example.com" },
                    new SourceRefConfig { SourceRef = "mounted/source" }
                ],
                Groups = [new GroupConfig { Group = "Groups", Root = "groups/root", Pages = ["groups/page"] }],
                Tabs =
                [
                    new TabConfig
                    {
                        Tab = "Tab",
                        Pages = ["tab/page"],
                        Groups = [new GroupConfig { Group = "Tab Group", Pages = ["tab/group-page"] }],
                        Menu =
                        [
                            new MenuConfig
                            {
                                Item = "Menu",
                                Pages = ["tab/menu-page"],
                                Groups = [new GroupConfig { Group = "Menu Group", Pages = ["tab/menu-group-page"] }]
                            }
                        ],
                        Anchors = [new AnchorConfig { Anchor = "Tab Anchor", Pages = ["tab/anchor-page"] }],
                        Dropdowns = [new DropdownConfig { Dropdown = "Section Dropdown", Pages = ["section/dropdown-page"] }],
                        Languages = [new LanguageConfig { Language = "fr", Pages = ["section/language-page"] }],
                        Versions = [new VersionConfig { Version = "v1", Pages = ["section/version-page"] }]
                    }
                ],
                Anchors =
                [
                    new AnchorConfig
                    {
                        Anchor = "Anchor",
                        Pages = ["anchor/page"],
                        Tabs = [new TabConfig { Tab = "Anchor Tab", Pages = ["anchor/tab-page"] }]
                    }
                ],
                Dropdowns =
                [
                    new DropdownConfig
                    {
                        Dropdown = "Dropdown",
                        Pages = ["dropdown/page"],
                        Anchors = [new AnchorConfig { Anchor = "Dropdown Anchor", Pages = ["dropdown/anchor-page"] }],
                        Tabs = [new TabConfig { Tab = "Dropdown Tab", Pages = ["dropdown/tab-page"] }]
                    }
                ],
                Products =
                [
                    new ProductConfig
                    {
                        Product = "Product",
                        Pages = ["product/page"],
                        Groups = [new GroupConfig { Group = "Product Group", Pages = ["product/group-page"] }]
                    }
                ],
                Versions =
                [
                    new VersionConfig
                    {
                        Version = "v2",
                        Pages = ["version/page"],
                        Groups = [new GroupConfig { Group = "Version Group", Pages = ["version/group-page"] }],
                        Tabs = [new TabConfig { Tab = "Version Tab", Pages = ["version/tab-page"] }],
                        Anchors = [new AnchorConfig { Anchor = "Version Anchor", Pages = ["version/anchor-page"] }],
                        Dropdowns = [new DropdownConfig { Dropdown = "Version Dropdown", Pages = ["version/dropdown-page"] }],
                        Languages = [new LanguageConfig { Language = "de", Pages = ["version/language-page"] }]
                    }
                ],
                Languages =
                [
                    new LanguageConfig
                    {
                        Language = "es",
                        Pages = ["language/page"],
                        Groups = [new GroupConfig { Group = "Language Group", Pages = ["language/group-page"] }],
                        Tabs = [new TabConfig { Tab = "Language Tab", Pages = ["language/tab-page"] }],
                        Anchors = [new AnchorConfig { Anchor = "Language Anchor", Pages = ["language/anchor-page"] }],
                        Dropdowns = [new DropdownConfig { Dropdown = "Language Dropdown", Pages = ["language/dropdown-page"] }],
                        Versions = [new VersionConfig { Version = "v3", Pages = ["language/version-page"] }]
                    }
                ]
            }
        };

        #endregion

    }

}
