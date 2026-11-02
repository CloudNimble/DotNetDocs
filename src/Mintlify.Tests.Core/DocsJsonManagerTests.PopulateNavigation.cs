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
        #region PopulateNavigationFromPath Tests

        /// <summary>
        /// Tests that PopulateNavigationFromPath throws when path is null or whitespace.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_NullOrWhitespacePath_ThrowsArgumentException()
        {
            var manager = new DocsJsonManager();
            manager.Load("""{"name": "Test"}""");

            var act1 = () => manager.PopulateNavigationFromPath(null!);
            var act2 = () => manager.PopulateNavigationFromPath("");
            var act3 = () => manager.PopulateNavigationFromPath("   ");

            act1.Should().Throw<ArgumentException>().WithParameterName("path");
            act2.Should().Throw<ArgumentException>().WithParameterName("path");
            act3.Should().Throw<ArgumentException>().WithParameterName("path");
        }

        /// <summary>
        /// Tests that PopulateNavigationFromPath throws when no configuration is loaded.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_NoConfiguration_ThrowsInvalidOperationException()
        {
            var manager = new DocsJsonManager();

            var act = () => manager.PopulateNavigationFromPath("C:\\test");

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*No configuration is loaded*");
        }

        /// <summary>
        /// Tests that PopulateNavigationFromPath throws when directory doesn't exist.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_NonExistentDirectory_ThrowsDirectoryNotFoundException()
        {
            var manager = new DocsJsonManager();
            manager.Load("""{"name": "Test"}""");
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            var act = () => manager.PopulateNavigationFromPath(nonExistentPath);

            act.Should().Throw<DirectoryNotFoundException>()
                .WithMessage($"*{nonExistentPath}*");
        }

        /// <summary>
        /// Tests that PopulateNavigationFromPath populates navigation from directory structure.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_ValidDirectory_PopulatesNavigation()
        {
            // Create a temporary directory structure
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);

            try
            {
                // Create structure:
                // /getting-started/
                //   quickstart.md
                //   installation.md
                // /api-reference/
                //   overview.md
                // changelog.md
                var gettingStartedDir = Path.Combine(tempPath, "getting-started");
                var apiDir = Path.Combine(tempPath, "api-reference");
                Directory.CreateDirectory(gettingStartedDir);
                Directory.CreateDirectory(apiDir);

                File.WriteAllText(Path.Combine(gettingStartedDir, "quickstart.mdx"), "# Quickstart");
                File.WriteAllText(Path.Combine(gettingStartedDir, "installation.mdx"), "# Installation");
                File.WriteAllText(Path.Combine(apiDir, "overview.mdx"), "# API Overview");
                File.WriteAllText(Path.Combine(tempPath, "changelog.mdx"), "# Changelog");
                File.WriteAllText(Path.Combine(tempPath, "index.mdx"), "# Home");

                var manager = new DocsJsonManager();
                manager.Load("""{"name": "Test"}""");

                manager.PopulateNavigationFromPath(tempPath, preserveExisting: false);

                manager.Configuration!.Navigation.Should().NotBeNull();
                manager.Configuration!.Navigation!.Pages!.Should().HaveCount(2); // Getting Started group + Api Reference group

                // Check groups
                var groups = manager.Configuration!.Navigation!.Pages?.OfType<GroupConfig>().ToList();
                groups.Should().HaveCount(2);

                // Check Getting Started group (for root files)
                var gettingStartedGroup = groups?.FirstOrDefault(g => g.Group == "Getting Started");
                gettingStartedGroup.Should().NotBeNull();
                gettingStartedGroup!.Pages.Should().HaveCount(2); // index + changelog
                gettingStartedGroup!.Pages.Should().Contain("index");
                gettingStartedGroup!.Pages.Should().Contain("changelog");

                // Check Api Reference group (for api-reference directory, but should be excluded by default)
                // Since api-reference is excluded by default, we should not have it
                var apiGroup = groups?.FirstOrDefault(g => g.Group == "Api Reference");
                apiGroup.Should().BeNull();

                // The other group should be the getting-started directory (renamed to avoid conflict)
                var gettingStartedDirGroup = groups?.FirstOrDefault(g => g.Group == "Getting Started (getting-started)");
                if (gettingStartedDirGroup is null)
                {
                    // If we only have one "Getting Started" group, it should contain both root files and directory files
                    gettingStartedGroup!.Pages.Should().HaveCount(4); // index + changelog + installation + quickstart
                }
                else
                {
                    gettingStartedDirGroup.Should().NotBeNull();
                    gettingStartedDirGroup!.Pages.Should().HaveCount(2); // installation + quickstart
                }
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        /// <summary>
        /// Tests that PopulateNavigationFromPath respects custom file extensions.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_CustomExtensions_OnlyIncludesSpecifiedFiles()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);

            try
            {
                File.WriteAllText(Path.Combine(tempPath, "doc1.md"), "# Doc1");
                File.WriteAllText(Path.Combine(tempPath, "doc2.mdx"), "# Doc2");
                File.WriteAllText(Path.Combine(tempPath, "doc3.txt"), "# Doc3");

                var manager = new DocsJsonManager();
                manager.Load("""{"name": "Test"}""");

                manager.PopulateNavigationFromPath(tempPath, [".mdx"]);

                manager.Configuration!.Navigation!.Pages!.Should().ContainSingle();
                // Root files get grouped into "Getting Started" group
                var groups = manager.Configuration!.Navigation!.Pages?.OfType<GroupConfig>().ToList();
                groups.Should().ContainSingle();
                var gettingStartedGroup = groups?.FirstOrDefault(g => g.Group == "Getting Started");
                gettingStartedGroup.Should().NotBeNull();
                gettingStartedGroup!.Pages.Should().Contain("doc2");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        #endregion

        #region Enhanced Directory Navigation Tests

        /// <summary>
        /// Tests that PopulateNavigationFromPath excludes dot directories.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_DotDirectories_ExcludesFromNavigation()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempPath);
                Directory.CreateDirectory(Path.Combine(tempPath, ".git"));
                Directory.CreateDirectory(Path.Combine(tempPath, ".vs"));
                Directory.CreateDirectory(Path.Combine(tempPath, ".vscode"));
                Directory.CreateDirectory(Path.Combine(tempPath, "docs"));

                File.WriteAllText(Path.Combine(tempPath, ".git", "config"), "git config");
                File.WriteAllText(Path.Combine(tempPath, "docs", "index.mdx"), "# Index");

                var manager = new DocsJsonManager();
                manager.Configuration = DocsJsonManager.CreateDefault("Test");

                manager.PopulateNavigationFromPath(tempPath, preserveExisting: false);

                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().ContainSingle();

                var docsGroup = pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Docs");
                docsGroup.Should().NotBeNull();
                docsGroup!.Pages.Should().Contain("docs/index");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        /// <summary>
        /// Tests that PopulateNavigationFromPath excludes specific directories.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_ExcludedDirectories_ExcludesFromNavigation()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempPath);
                Directory.CreateDirectory(Path.Combine(tempPath, "node_modules"));
                Directory.CreateDirectory(Path.Combine(tempPath, "conceptual"));
                Directory.CreateDirectory(Path.Combine(tempPath, "overrides"));
                Directory.CreateDirectory(Path.Combine(tempPath, "CONCEPTUAL")); // Test case sensitivity
                Directory.CreateDirectory(Path.Combine(tempPath, "docs"));

                File.WriteAllText(Path.Combine(tempPath, "node_modules", "package.json"), "{}");
                File.WriteAllText(Path.Combine(tempPath, "conceptual", "concept.mdx"), "# Concept");
                File.WriteAllText(Path.Combine(tempPath, "overrides", "override.mdx"), "# Override");
                File.WriteAllText(Path.Combine(tempPath, "CONCEPTUAL", "concept2.mdx"), "# Concept2");
                File.WriteAllText(Path.Combine(tempPath, "docs", "index.mdx"), "# Index");

                var manager = new DocsJsonManager
                {
                    Configuration = DocsJsonManager.CreateDefault("Test")
                };

                manager.PopulateNavigationFromPath(tempPath, preserveExisting: false);

                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().ContainSingle();

                var docsGroup = pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Docs");
                docsGroup.Should().NotBeNull();
                docsGroup!.Pages.Should().Contain("docs/index");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        /// <summary>
        /// Tests that PopulateNavigationFromPath includes .mdx files and warns about .md files.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_MdxOnlySupport_IncludesMdxWarnsAboutMd()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempPath);
                File.WriteAllText(Path.Combine(tempPath, "index.mdx"), "# Index MDX");
                File.WriteAllText(Path.Combine(tempPath, "guide.mdx"), "# Guide MDX");
                File.WriteAllText(Path.Combine(tempPath, "readme.md"), "# Readme MD");
                File.WriteAllText(Path.Combine(tempPath, "doc.md"), "# Doc MD");

                var manager = new DocsJsonManager
                {
                    Configuration = DocsJsonManager.CreateDefault("Test")
                };

                manager.PopulateNavigationFromPath(tempPath, preserveExisting: false);

                // Should include .mdx files
                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().ContainSingle(); // Root files get grouped into "Getting Started"

                var gettingStartedGroup = pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Getting Started");
                gettingStartedGroup.Should().NotBeNull();
                gettingStartedGroup!.Pages.Should().Contain("index");
                gettingStartedGroup!.Pages.Should().Contain("guide");

                // Should have warnings for .md files
                var mdWarnings = manager.ConfigurationErrors.Where(e => e.ErrorNumber == "MD_FILE_WARNING");
                mdWarnings.Should().HaveCount(2);
                mdWarnings.Should().Contain(w => w.ErrorText.Contains("readme.md"));
                mdWarnings.Should().Contain(w => w.ErrorText.Contains("doc.md"));
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        /// <summary>
        /// Tests that PopulateNavigationFromPath prioritizes index files first.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_IndexFiles_AppearsFirst()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempPath);
                Directory.CreateDirectory(Path.Combine(tempPath, "guides"));

                File.WriteAllText(Path.Combine(tempPath, "zebra.mdx"), "# Zebra");
                File.WriteAllText(Path.Combine(tempPath, "index.mdx"), "# Index");
                File.WriteAllText(Path.Combine(tempPath, "apple.mdx"), "# Apple");

                File.WriteAllText(Path.Combine(tempPath, "guides", "zebra.mdx"), "# Zebra Guide");
                File.WriteAllText(Path.Combine(tempPath, "guides", "index.mdx"), "# Guide Index");
                File.WriteAllText(Path.Combine(tempPath, "guides", "apple.mdx"), "# Apple Guide");

                var manager = new DocsJsonManager();
                manager.Configuration = DocsJsonManager.CreateDefault("Test");

                manager.PopulateNavigationFromPath(tempPath, preserveExisting: false);

                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().HaveCount(2); // "Getting Started" group + "Guides" group

                // Root level files get grouped into "Getting Started" - index should be first
                var gettingStartedGroup = pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Getting Started");
                gettingStartedGroup.Should().NotBeNull();
                gettingStartedGroup!.Pages![0].Should().Be("index");
                gettingStartedGroup!.Pages![1].Should().Be("apple");
                gettingStartedGroup!.Pages![2].Should().Be("zebra");

                // Group level: index should be first
                var guidesGroup = pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Guides");
                guidesGroup.Should().NotBeNull();
                guidesGroup!.Pages![0].Should().Be("guides/index");
                guidesGroup!.Pages![1].Should().Be("guides/apple");
                guidesGroup!.Pages![2].Should().Be("guides/zebra");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        /// <summary>
        /// Tests navigation.json override functionality.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_NavigationJsonOverride_ReplacesAutoGeneration()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempPath);
                Directory.CreateDirectory(Path.Combine(tempPath, "cli"));
                Directory.CreateDirectory(Path.Combine(tempPath, "cli", "code"));

                // Create files that would normally be auto-processed
                File.WriteAllText(Path.Combine(tempPath, "cli", "index.mdx"), "# CLI Index");
                File.WriteAllText(Path.Combine(tempPath, "cli", "setup.mdx"), "# CLI Setup");
                File.WriteAllText(Path.Combine(tempPath, "cli", "code", "generate.mdx"), "# Code Generate");

                // Create navigation.json override
                var navigationJson = """
                {
                  "group": "CLI Tools",
                  "icon": "terminal",
                  "pages": [
                    {
                      "group": "Core Commands",
                      "pages": ["cli/index", "cli/setup"]
                    },
                    {
                      "group": "Code Generation",
                      "pages": ["cli/code/generate"]
                    }
                  ]
                }
                """;
                File.WriteAllText(Path.Combine(tempPath, "cli", "navigation.json"), navigationJson);

                var manager = new DocsJsonManager();
                manager.Configuration = DocsJsonManager.CreateDefault("Test");

                manager.PopulateNavigationFromPath(tempPath, preserveExisting: false);

                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().ContainSingle();

                var cliGroup = pages?.OfType<GroupConfig>().FirstOrDefault();
                cliGroup.Should().NotBeNull();
                cliGroup!.Group.Should().Be("CLI Tools");
                ((string?)cliGroup!.Icon).Should().Be("terminal");
                cliGroup!.Pages.Should().HaveCount(2);

                var coreGroup = cliGroup!.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Core Commands");
                coreGroup.Should().NotBeNull();
                coreGroup!.Pages.Should().Contain("cli/index");
                coreGroup!.Pages.Should().Contain("cli/setup");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        /// <summary>
        /// Tests malformed navigation.json falls back to auto-generation.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_MalformedNavigationJson_FallsBackToAutoGeneration()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempPath);
                Directory.CreateDirectory(Path.Combine(tempPath, "docs"));

                File.WriteAllText(Path.Combine(tempPath, "docs", "index.mdx"), "# Index");
                File.WriteAllText(Path.Combine(tempPath, "docs", "guide.mdx"), "# Guide");

                // Create malformed navigation.json
                File.WriteAllText(Path.Combine(tempPath, "docs", "navigation.json"), "{ invalid json }");

                var manager = new DocsJsonManager();
                manager.Configuration = DocsJsonManager.CreateDefault("Test");

                manager.PopulateNavigationFromPath(tempPath, preserveExisting: false);

                // Should have warning about malformed JSON
                var jsonWarnings = manager.ConfigurationErrors.Where(e => e.ErrorNumber == "NAVIGATION_JSON");
                jsonWarnings.Should().ContainSingle();
                jsonWarnings.First().ErrorText.Should().Contain("Invalid navigation.json file");

                // Should fall back to auto-generation
                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().ContainSingle();

                var docsGroup = pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Docs");
                docsGroup.Should().NotBeNull();
                docsGroup!.Pages.Should().HaveCount(2);
                docsGroup!.Pages.Should().Contain("docs/index");
                docsGroup!.Pages.Should().Contain("docs/guide");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        /// <summary>
        /// Tests CLI folder structure replication with navigation.json.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_CliStructure_ReplicatesActualStructure()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempPath);
                Directory.CreateDirectory(Path.Combine(tempPath, "cli"));
                Directory.CreateDirectory(Path.Combine(tempPath, "cli", "code"));
                Directory.CreateDirectory(Path.Combine(tempPath, "cli", "database"));
                Directory.CreateDirectory(Path.Combine(tempPath, "cli", "mintlify"));

                // Create CLI files
                File.WriteAllText(Path.Combine(tempPath, "cli", "index.mdx"), "# CLI");
                File.WriteAllText(Path.Combine(tempPath, "cli", "cleanup.mdx"), "# Cleanup");
                File.WriteAllText(Path.Combine(tempPath, "cli", "setup.mdx"), "# Setup");

                // Create subdirectory files
                File.WriteAllText(Path.Combine(tempPath, "cli", "code", "index.mdx"), "# Code");
                File.WriteAllText(Path.Combine(tempPath, "cli", "code", "generate.mdx"), "# Generate Code");

                File.WriteAllText(Path.Combine(tempPath, "cli", "database", "index.mdx"), "# Database");
                File.WriteAllText(Path.Combine(tempPath, "cli", "database", "generate.mdx"), "# Generate DB");
                File.WriteAllText(Path.Combine(tempPath, "cli", "database", "refresh.mdx"), "# Refresh DB");

                File.WriteAllText(Path.Combine(tempPath, "cli", "mintlify", "index.mdx"), "# Mintlify");
                File.WriteAllText(Path.Combine(tempPath, "cli", "mintlify", "init.mdx"), "# Init");
                File.WriteAllText(Path.Combine(tempPath, "cli", "mintlify", "generate.mdx"), "# Generate");

                // Create navigation.json override for CLI root
                var navigationJson = """
                {
                  "group": "CLI Tools",
                  "icon": "terminal",
                  "pages": [
                    "cli/index",
                    "cli/cleanup", 
                    "cli/setup"
                  ]
                }
                """;
                File.WriteAllText(Path.Combine(tempPath, "cli", "navigation.json"), navigationJson);

                var manager = new DocsJsonManager();
                manager.Configuration = DocsJsonManager.CreateDefault("Test");

                manager.PopulateNavigationFromPath(tempPath, preserveExisting: false);

                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().ContainSingle();

                // CLI root should use navigation.json override completely (no auto-generated subdirectories)
                var cliGroup = pages?.OfType<GroupConfig>().FirstOrDefault();
                cliGroup.Should().NotBeNull();
                cliGroup!.Group.Should().Be("CLI Tools");
                ((string?)cliGroup!.Icon).Should().Be("terminal");
                // Should only have override pages (3) - user takes complete control
                cliGroup!.Pages.Should().HaveCount(3);
                cliGroup!.Pages.Should().Contain("cli/index");
                cliGroup!.Pages.Should().Contain("cli/cleanup");
                cliGroup!.Pages.Should().Contain("cli/setup");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        /// <summary>
        /// Tests complete workflow: populate with overrides, then apply URL prefix.
        /// </summary>
        [TestMethod]
        public void CompleteWorkflow_PopulateWithOverrides_ThenApplyPrefix_WorksCorrectly()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempPath);
                Directory.CreateDirectory(Path.Combine(tempPath, "api"));
                Directory.CreateDirectory(Path.Combine(tempPath, "guides"));

                // Create auto-generated content
                File.WriteAllText(Path.Combine(tempPath, "index.mdx"), "# Index");
                File.WriteAllText(Path.Combine(tempPath, "guides", "intro.mdx"), "# Intro");
                File.WriteAllText(Path.Combine(tempPath, "guides", "advanced.mdx"), "# Advanced");

                // Create override content
                File.WriteAllText(Path.Combine(tempPath, "api", "endpoints.mdx"), "# Endpoints");
                var apiNavigationJson = """
                {
                  "group": "API Reference",
                  "icon": "code",
                  "pages": [
                    "api/endpoints"
                  ]
                }
                """;
                File.WriteAllText(Path.Combine(tempPath, "api", "navigation.json"), apiNavigationJson);

                var manager = new DocsJsonManager();
                manager.Configuration = DocsJsonManager.CreateDefault("Test");

                // Step 1: Populate navigation
                manager.PopulateNavigationFromPath(tempPath, preserveExisting: false);

                // Step 2: Apply URL prefix
                manager.ApplyUrlPrefix("/v2");

                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().HaveCount(3); // Getting Started group + Guides group + API Reference group

                // Auto-generated root files get grouped into "Getting Started" and should be prefixed
                var gettingStartedGroup = pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Getting Started");
                gettingStartedGroup.Should().NotBeNull();
                gettingStartedGroup!.Pages.Should().Contain("/v2/index");

                var guidesGroup = pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Guides");
                guidesGroup.Should().NotBeNull();
                guidesGroup!.Pages.Should().Contain("/v2/guides/intro");
                guidesGroup!.Pages.Should().Contain("/v2/guides/advanced");

                // Override content should be prefixed (navigation.json takes complete control of api directory)
                var apiGroup = pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "API Reference");
                apiGroup.Should().NotBeNull();
                ((string?)apiGroup!.Icon).Should().Be("code");
                apiGroup!.Pages.Should().Contain("/v2/api/endpoints");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        /// <summary>
        /// Tests deep nesting with mixed auto-generation and overrides.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_DeepNestingWithMixedOverrides_HandlesCorrectly()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempPath);
                Directory.CreateDirectory(Path.Combine(tempPath, "docs"));
                Directory.CreateDirectory(Path.Combine(tempPath, "docs", "guides"));
                Directory.CreateDirectory(Path.Combine(tempPath, "docs", "guides", "advanced"));
                Directory.CreateDirectory(Path.Combine(tempPath, "docs", "api"));

                // Create nested structure
                File.WriteAllText(Path.Combine(tempPath, "index.mdx"), "# Root Index");
                File.WriteAllText(Path.Combine(tempPath, "docs", "index.mdx"), "# Docs Index");
                File.WriteAllText(Path.Combine(tempPath, "docs", "guides", "index.mdx"), "# Guides Index");
                File.WriteAllText(Path.Combine(tempPath, "docs", "guides", "basic.mdx"), "# Basic Guide");
                File.WriteAllText(Path.Combine(tempPath, "docs", "guides", "advanced", "index.mdx"), "# Advanced Index");
                File.WriteAllText(Path.Combine(tempPath, "docs", "guides", "advanced", "patterns.mdx"), "# Patterns");
                File.WriteAllText(Path.Combine(tempPath, "docs", "api", "overview.mdx"), "# API Overview");

                // Add override at api level
                var apiNavigationJson = """
                {
                  "group": "API Documentation",
                  "icon": "api",
                  "pages": [
                    "docs/api/overview"
                  ]
                }
                """;
                File.WriteAllText(Path.Combine(tempPath, "docs", "api", "navigation.json"), apiNavigationJson);

                var manager = new DocsJsonManager();
                manager.Configuration = DocsJsonManager.CreateDefault("Test");

                manager.PopulateNavigationFromPath(tempPath, preserveExisting: false);

                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().HaveCount(2); // Getting Started group + Docs group

                // Root files get grouped into "Getting Started"
                var gettingStartedGroup = pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Getting Started");
                gettingStartedGroup.Should().NotBeNull();
                gettingStartedGroup!.Pages.Should().Contain("index");

                var docsGroup = pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Docs");
                docsGroup.Should().NotBeNull();
                docsGroup!.Pages.Should().HaveCount(3); // index + guides group + api group
                docsGroup!.Pages.Should().Contain("docs/index");

                // Guides should be auto-generated with proper nesting
                var guidesGroup = docsGroup!.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Guides");
                guidesGroup.Should().NotBeNull();
                guidesGroup!.Pages![0].Should().Be("docs/guides/index"); // Index first
                guidesGroup!.Pages!.Should().Contain("docs/guides/basic");

                var advancedGroup = guidesGroup!.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Advanced");
                advancedGroup.Should().NotBeNull();
                advancedGroup!.Pages![0].Should().Be("docs/guides/advanced/index"); // Index first
                advancedGroup!.Pages!.Should().Contain("docs/guides/advanced/patterns");

                // API should use override (navigation.json takes complete control)
                var apiGroup = docsGroup!.Pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "API Documentation");
                apiGroup.Should().NotBeNull();
                ((string?)apiGroup!.Icon).Should().Be("api");
                apiGroup!.Pages.Should().Contain("docs/api/overview");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        /// <summary>
        /// Tests that PopulateNavigationFromPath groups root files under "Getting Started".
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_RootFiles_GroupsUnderGettingStarted()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempPath);
                Directory.CreateDirectory(Path.Combine(tempPath, "guides"));

                // Create root files
                File.WriteAllText(Path.Combine(tempPath, "index.mdx"), "# Index");
                File.WriteAllText(Path.Combine(tempPath, "quickstart.mdx"), "# Quickstart");
                File.WriteAllText(Path.Combine(tempPath, "installation.mdx"), "# Installation");

                // Create subdirectory files
                File.WriteAllText(Path.Combine(tempPath, "guides", "tutorial.mdx"), "# Tutorial");

                var manager = new DocsJsonManager();
                manager.Configuration = DocsJsonManager.CreateDefault("Test");

                manager.PopulateNavigationFromPath(tempPath, preserveExisting: false);

                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().HaveCount(2); // Getting Started group + Guides group

                // Check Getting Started group
                var gettingStartedGroup = pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Getting Started");
                gettingStartedGroup.Should().NotBeNull();
                gettingStartedGroup!.Pages.Should().HaveCount(3);
                gettingStartedGroup!.Pages![0].Should().Be("index"); // Index should be first
                gettingStartedGroup!.Pages.Should().Contain("quickstart");
                gettingStartedGroup!.Pages.Should().Contain("installation");

                // Check Guides group
                var guidesGroup = pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Guides");
                guidesGroup.Should().NotBeNull();
                guidesGroup!.Pages.Should().Contain("guides/tutorial");

                // Verify Getting Started appears first
                pages![0].Should().BeSameAs(gettingStartedGroup);
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        /// <summary>
        /// Tests that PopulateNavigationFromPath handles empty root directory correctly.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_NoRootFiles_DoesNotCreateGettingStartedGroup()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempPath);
                Directory.CreateDirectory(Path.Combine(tempPath, "guides"));

                // Create only subdirectory files, no root files
                File.WriteAllText(Path.Combine(tempPath, "guides", "tutorial.mdx"), "# Tutorial");

                var manager = new DocsJsonManager();
                manager.Configuration = DocsJsonManager.CreateDefault("Test");

                manager.PopulateNavigationFromPath(tempPath, preserveExisting: false);

                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().ContainSingle(); // Only Guides group

                // Should not have Getting Started group
                var gettingStartedGroup = pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Getting Started");
                gettingStartedGroup.Should().BeNull();

                // Should have Guides group
                var guidesGroup = pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Guides");
                guidesGroup.Should().NotBeNull();
                guidesGroup!.Pages.Should().Contain("guides/tutorial");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        /// <summary>
        /// Tests that PopulateNavigationFromPath maintains index priority in Getting Started group.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_RootFilesWithIndex_IndexAppearsFirstInGettingStarted()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempPath);

                // Create root files in non-alphabetical order
                File.WriteAllText(Path.Combine(tempPath, "zebra.mdx"), "# Zebra");
                File.WriteAllText(Path.Combine(tempPath, "apple.mdx"), "# Apple");
                File.WriteAllText(Path.Combine(tempPath, "index.mdx"), "# Index");
                File.WriteAllText(Path.Combine(tempPath, "banana.mdx"), "# Banana");

                var manager = new DocsJsonManager();
                manager.Configuration = DocsJsonManager.CreateDefault("Test");

                manager.PopulateNavigationFromPath(tempPath, preserveExisting: false);

                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().ContainSingle(); // Getting Started group only

                var gettingStartedGroup = pages?.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Getting Started");
                gettingStartedGroup.Should().NotBeNull();
                gettingStartedGroup!.Pages.Should().HaveCount(4);

                // Verify sorting: index first, then alphabetical
                gettingStartedGroup!.Pages![0].Should().Be("index");
                gettingStartedGroup!.Pages![1].Should().Be("apple");
                gettingStartedGroup!.Pages![2].Should().Be("banana");
                gettingStartedGroup!.Pages![3].Should().Be("zebra");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        #endregion
    }

}