﻿using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core
{

    /// <summary>
    /// Tests for the DocsJsonManager class that handles loading and managing Mintlify documentation configurations.
    /// </summary>
    [TestClass]
    public class DocsJsonManagerTests
    {

        #region Private Fields

        private readonly JsonSerializerOptions _jsonOptions = MintlifyConstants.JsonSerializerOptions;
        private readonly string _validDocsJson = """
            {
                "name": "Test Documentation",
                "theme": "mint",
                "navigation": [
                    "index",
                    {
                        "group": "Getting Started",
                        "pages": ["quickstart", "installation"]
                    }
                ]
            }
            """;

        #endregion

        #region Constructor Tests

        /// <summary>
        /// Tests that default constructor initializes properties correctly.
        /// </summary>
        [TestMethod]
        public void Constructor_Default_InitializesProperties()
        {
            var manager = new DocsJsonManager();

            manager.Configuration.Should().BeNull();
            manager.ConfigurationErrors.Should().NotBeNull().And.BeEmpty();
            manager.FilePath.Should().BeNull();
            manager.IsLoaded.Should().BeFalse();
        }

        /// <summary>
        /// Tests that constructor with valid file path sets FilePath property.
        /// </summary>
        [TestMethod]
        public void Constructor_ValidFilePath_SetsFilePath()
        {
            var tempFile = Path.GetTempFileName();
            File.Move(tempFile, Path.ChangeExtension(tempFile, ".json"));
            var jsonFile = Path.ChangeExtension(tempFile, ".json");

            try
            {
                File.WriteAllText(jsonFile, _validDocsJson);

                var manager = new DocsJsonManager(jsonFile);

                manager.FilePath.Should().Be(jsonFile);
                manager.Configuration.Should().BeNull();
                manager.ConfigurationErrors.Should().BeEmpty();
            }
            finally
            {
                if (File.Exists(jsonFile))
                    File.Delete(jsonFile);
            }
        }

        /// <summary>
        /// Tests that constructor throws exception for non-existent file.
        /// </summary>
        [TestMethod]
        public void Constructor_NonExistentFile_ThrowsArgumentException()
        {
            var nonExistentPath = @"C:\NonExistent\docs.json";

            var act = () => new DocsJsonManager(nonExistentPath);

            act.Should().Throw<ArgumentException>()
                .WithMessage("*does not exist*")
                .WithParameterName("filePath");
        }

        /// <summary>
        /// Tests that constructor throws exception for non-JSON file.
        /// </summary>
        [TestMethod]
        public void Constructor_NonJsonFile_ThrowsArgumentException()
        {
            var tempFile = Path.GetTempFileName(); // .tmp file

            try
            {
                var act = () => new DocsJsonManager(tempFile);

                act.Should().Throw<ArgumentException>()
                    .WithMessage("*does not point to a JSON file*")
                    .WithParameterName("filePath");
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Tests that constructor throws exception for null or whitespace file path.
        /// </summary>
        [TestMethod]
        public void Constructor_NullOrWhitespaceFilePath_ThrowsArgumentException()
        {
            var act1 = () => new DocsJsonManager(null!);
            var act2 = () => new DocsJsonManager("");
            var act3 = () => new DocsJsonManager("   ");

            act1.Should().Throw<ArgumentException>().WithParameterName("filePath");
            act2.Should().Throw<ArgumentException>().WithParameterName("filePath");
            act3.Should().Throw<ArgumentException>().WithParameterName("filePath");
        }

        #endregion

        #region Load Tests

        /// <summary>
        /// Tests that Load() method loads valid JSON configuration successfully.
        /// </summary>
        [TestMethod]
        public void Load_ValidJson_LoadsConfiguration()
        {
            var tempFile = Path.GetTempFileName();
            File.Move(tempFile, Path.ChangeExtension(tempFile, ".json"));
            var jsonFile = Path.ChangeExtension(tempFile, ".json");

            try
            {
                File.WriteAllText(jsonFile, _validDocsJson);
                var manager = new DocsJsonManager(jsonFile);

                manager.Load();

                manager.Configuration.Should().NotBeNull();
                manager.Configuration!.Name.Should().Be("Test Documentation");
                manager.Configuration!.Theme.Should().Be("mint");
                manager.Configuration!.Navigation.Should().NotBeNull();
                manager.ConfigurationErrors.Should().BeEmpty();
                manager.IsLoaded.Should().BeTrue();
            }
            finally
            {
                if (File.Exists(jsonFile))
                    File.Delete(jsonFile);
            }
        }

        /// <summary>
        /// Tests that Load() throws exception when no file path is specified.
        /// </summary>
        [TestMethod]
        public void Load_NoFilePath_ThrowsInvalidOperationException()
        {
            var manager = new DocsJsonManager();

            var act = () => manager.Load();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*No file path has been specified*");
        }

        /// <summary>
        /// Tests that Load(string) method loads valid JSON content successfully.
        /// </summary>
        [TestMethod]
        public void Load_ValidJsonString_LoadsConfiguration()
        {
            var manager = new DocsJsonManager();

            manager.Load(_validDocsJson);

            manager.Configuration.Should().NotBeNull();
            manager.Configuration!.Name.Should().Be("Test Documentation");
            manager.Configuration!.Theme.Should().Be("mint");
            manager.ConfigurationErrors.Should().BeEmpty();
            manager.IsLoaded.Should().BeTrue();
        }

        /// <summary>
        /// Tests that Load(string) throws exception for null or whitespace content.
        /// </summary>
        [TestMethod]
        public void Load_NullOrWhitespaceContent_ThrowsArgumentException()
        {
            var manager = new DocsJsonManager();

            var act1 = () => manager.Load(null!);
            var act2 = () => manager.Load("");
            var act3 = () => manager.Load("   ");

            act1.Should().Throw<ArgumentException>().WithParameterName("content");
            act2.Should().Throw<ArgumentException>().WithParameterName("content");
            act3.Should().Throw<ArgumentException>().WithParameterName("content");
        }

        /// <summary>
        /// Tests that Load(string) handles invalid JSON content gracefully.
        /// </summary>
        [TestMethod]
        public void Load_InvalidJson_AddsError()
        {
            var manager = new DocsJsonManager();
            var invalidJson = "{ invalid json content }";

            manager.Load(invalidJson);

            manager.Configuration.Should().BeNull();
            manager.ConfigurationErrors.Should().HaveCount(1);
            manager.ConfigurationErrors[0].ErrorNumber.Should().Be("JSON");
            manager.IsLoaded.Should().BeFalse();
        }

        #endregion

        #region Save Tests

        /// <summary>
        /// Tests that Save() method saves configuration to original file path.
        /// </summary>
        [TestMethod]
        public void Save_WithFilePath_SavesConfiguration()
        {
            var tempFile = Path.GetTempFileName();
            File.Move(tempFile, Path.ChangeExtension(tempFile, ".json"));
            var jsonFile = Path.ChangeExtension(tempFile, ".json");

            try
            {
                File.WriteAllText(jsonFile, _validDocsJson);
                var manager = new DocsJsonManager(jsonFile);
                manager.Load();
                manager.Configuration!.Description = "Updated description";

                manager.Save();

                var savedContent = File.ReadAllText(jsonFile);
                savedContent.Should().Contain("Updated description");
            }
            finally
            {
                if (File.Exists(jsonFile))
                    File.Delete(jsonFile);
            }
        }

        /// <summary>
        /// Tests that Save() throws exception when no configuration is loaded.
        /// </summary>
        [TestMethod]
        public void Save_NoConfiguration_ThrowsInvalidOperationException()
        {
            var manager = new DocsJsonManager();

            var act = () => manager.Save();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*No configuration is loaded*");
        }

        /// <summary>
        /// Tests that Save() throws exception when no file path is specified.
        /// </summary>
        [TestMethod]
        public void Save_NoFilePath_ThrowsInvalidOperationException()
        {
            var manager = new DocsJsonManager();
            manager.Load(_validDocsJson);

            var act = () => manager.Save();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*No file path is specified*");
        }

        /// <summary>
        /// Tests that Save(string) method saves configuration to specified file path.
        /// </summary>
        [TestMethod]
        public void Save_WithSpecifiedPath_SavesConfiguration()
        {
            var tempFile = Path.GetTempFileName();
            File.Move(tempFile, Path.ChangeExtension(tempFile, ".json"));
            var jsonFile = Path.ChangeExtension(tempFile, ".json");

            try
            {
                var manager = new DocsJsonManager();
                manager.Load(_validDocsJson);
                manager.Configuration!.Description = "Saved to specific path";

                manager.Save(jsonFile);

                var savedContent = File.ReadAllText(jsonFile);
                savedContent.Should().Contain("Saved to specific path");
                var reloaded = JsonSerializer.Deserialize<DocsJsonConfig>(savedContent, _jsonOptions);
                reloaded!.Name.Should().Be("Test Documentation");
            }
            finally
            {
                if (File.Exists(jsonFile))
                    File.Delete(jsonFile);
            }
        }

        #endregion

        #region CreateDefault Tests

        /// <summary>
        /// Tests that CreateDefault method creates configuration with required properties.
        /// </summary>
        [TestMethod]
        public void CreateDefault_WithName_CreatesValidConfiguration()
        {
            var config = DocsJsonManager.CreateDefault("My API Docs");

            config.Should().NotBeNull();
            config.Name.Should().Be("My API Docs");
            config.Theme.Should().Be("mint");
            config.Navigation.Should().NotBeNull();
            config.Navigation.Pages.Should().NotBeNull().And.HaveCountGreaterThan(0);
        }

        /// <summary>
        /// Tests that CreateDefault method creates configuration with custom theme.
        /// </summary>
        [TestMethod]
        public void CreateDefault_WithCustomTheme_CreatesConfigurationWithTheme()
        {
            var config = DocsJsonManager.CreateDefault("My API Docs", "prism");

            config.Name.Should().Be("My API Docs");
            config.Theme.Should().Be("prism");
        }

        /// <summary>
        /// Tests that CreateDefault throws exception for null or whitespace name.
        /// </summary>
        [TestMethod]
        public void CreateDefault_NullOrWhitespaceName_ThrowsArgumentException()
        {
            var act1 = () => DocsJsonManager.CreateDefault(null!);
            var act2 = () => DocsJsonManager.CreateDefault("");
            var act3 = () => DocsJsonManager.CreateDefault("   ");

            act1.Should().Throw<ArgumentException>().WithParameterName("name");
            act2.Should().Throw<ArgumentException>().WithParameterName("name");
            act3.Should().Throw<ArgumentException>().WithParameterName("name");
        }

        #endregion

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
            manager.Configuration!.Navigation!.Pages!.Should().BeEquivalentTo(new[] { "index", "quickstart", "api/overview", "api/reference" });
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
            apiGroup.Pages.Should().BeEquivalentTo(new[] { "api/overview", "api/authentication", "api/endpoints", "api/errors" });

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

            var authGroup = apiGroup!.Pages!.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Authentication");
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
            emptyGroups.Any(g => g.Pages?.Contains("page1") == true).Should().BeTrue();
            emptyGroups.Any(g => g.Pages?.Contains("page2") == true).Should().BeTrue();
            emptyGroups.Any(g => g.Pages?.Contains("page5") == true).Should().BeTrue();
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
            emptyGroups.Should().HaveCount(1); // Empty groups merged into one
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

            manager.Configuration!.Navigation!.Tabs!.Should().HaveCount(1);

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

        #region ApplyDefaults Tests

        /// <summary>
        /// Tests that ApplyDefaults method sets missing properties to default values.
        /// </summary>
        [TestMethod]
        public void ApplyDefaults_MissingProperties_SetsDefaults()
        {
            var manager = new DocsJsonManager();
            var minimalConfig = """
                {
                    "name": "Minimal Config"
                }
                """;

            manager.Load(minimalConfig);
            manager.ApplyDefaults();

            manager.Configuration!.Theme.Should().Be("mint");
            manager.Configuration!.Schema.Should().Be("https://mintlify.com/docs.json");
            manager.Configuration!.Navigation.Should().NotBeNull();
            manager.Configuration!.Navigation!.Pages!.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that ApplyDefaults does not override existing values.
        /// </summary>
        [TestMethod]
        public void ApplyDefaults_ExistingProperties_PreservesValues()
        {
            var manager = new DocsJsonManager();
            manager.Load(_validDocsJson);
            var originalTheme = manager.Configuration!.Theme;

            manager.ApplyDefaults();

            manager.Configuration!.Theme.Should().Be(originalTheme);
        }

        /// <summary>
        /// Tests that ApplyDefaults throws exception when no configuration is loaded.
        /// </summary>
        [TestMethod]
        public void ApplyDefaults_NoConfiguration_ThrowsInvalidOperationException()
        {
            var manager = new DocsJsonManager();

            var act = () => manager.ApplyDefaults();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*No configuration is loaded*");
        }

        #endregion

        #region Validation Tests

        /// <summary>
        /// Tests that validation adds warnings for missing required properties.
        /// </summary>
        [TestMethod]
        public void Load_MissingRequiredProperties_AddsWarnings()
        {
            var manager = new DocsJsonManager();
            var incompleteConfig = """
                {
                    "theme": "mint"
                }
                """;

            manager.Load(incompleteConfig);

            manager.Configuration.Should().NotBeNull();
            manager.ConfigurationErrors.Should().HaveCount(2); // Missing name and navigation
            manager.ConfigurationErrors.Should().OnlyContain(e => e.IsWarning);
        }

        /// <summary>
        /// Tests that complete configuration passes validation without warnings.
        /// </summary>
        [TestMethod]
        public void Load_CompleteConfiguration_NoWarnings()
        {
            var manager = new DocsJsonManager();

            manager.Load(_validDocsJson);

            manager.Configuration.Should().NotBeNull();
            manager.ConfigurationErrors.Should().BeEmpty();
            manager.IsLoaded.Should().BeTrue();
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Tests complete workflow of loading, modifying, and saving configuration.
        /// </summary>
        [TestMethod]
        public void CompleteWorkflow_LoadModifySave_WorksCorrectly()
        {
            var tempFile = Path.GetTempFileName();
            File.Move(tempFile, Path.ChangeExtension(tempFile, ".json"));
            var jsonFile = Path.ChangeExtension(tempFile, ".json");

            try
            {
                // Initial save
                File.WriteAllText(jsonFile, _validDocsJson);

                // Load, modify, and save
                var manager = new DocsJsonManager(jsonFile);
                manager.Load();
                manager.Configuration!.Description = "Modified during test";
                manager.Configuration!.Colors = new ColorsConfig { Primary = "#123456" };
                manager.Save();

                // Verify changes persisted
                var newManager = new DocsJsonManager(jsonFile);
                newManager.Load();

                newManager.Configuration!.Description.Should().Be("Modified during test");
                newManager.Configuration!.Colors!.Primary.Should().Be("#123456");
                newManager.Configuration!.Name.Should().Be("Test Documentation"); // Original value preserved
            }
            finally
            {
                if (File.Exists(jsonFile))
                    File.Delete(jsonFile);
            }
        }

        /// <summary>
        /// Tests that JSON serialization preserves complex navigation structures.
        /// </summary>
        [TestMethod]
        public void Navigation_ComplexStructure_SerializesCorrectly()
        {
            var manager = new DocsJsonManager();
            var complexNavJson = """
                {
                    "name": "Complex Nav Test",
                    "navigation": [
                        "index",
                        {
                            "group": "API Reference",
                            "icon": "code",
                            "pages": [
                                "api/overview",
                                {
                                    "group": "Endpoints",
                                    "pages": ["api/users", "api/orders"]
                                }
                            ]
                        }
                    ]
                }
                """;

            manager.Load(complexNavJson);

            manager.Configuration!.Navigation!.Pages!.Should().HaveCount(2);
            manager.Configuration!.Navigation!.Pages![0].Should().Be("index");
            manager.Configuration!.Navigation!.Pages![1].Should().BeOfType<GroupConfig>();

            var group = manager.Configuration!.Navigation!.Pages![1] as GroupConfig;
            group!.Group.Should().Be("API Reference");
            group!.Icon.Should().Be("code");
            group!.Pages.Should().HaveCount(2);
            group!.Pages![1].Should().BeOfType<GroupConfig>();
        }

        #endregion

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

                manager.PopulateNavigationFromPath(tempPath);

                manager.Configuration!.Navigation.Should().NotBeNull();
                manager.Configuration!.Navigation!.Pages!.Should().HaveCount(4); // index + changelog + 2 groups

                // Check index and changelog pages
                manager.Configuration!.Navigation!.Pages!.Should().Contain("index");
                manager.Configuration!.Navigation!.Pages!.Should().Contain("changelog");

                // Check groups
                var groups = manager.Configuration!.Navigation!.Pages!.OfType<GroupConfig>().ToList();
                groups.Should().HaveCount(2);

                var apiGroup = groups.FirstOrDefault(g => g.Group == "Api Reference");
                apiGroup.Should().NotBeNull();
                apiGroup!.Pages.Should().HaveCount(1);
                apiGroup!.Pages.Should().Contain("api-reference/overview");

                var gettingStartedGroup = groups.FirstOrDefault(g => g.Group == "Getting Started");
                gettingStartedGroup.Should().NotBeNull();
                gettingStartedGroup!.Pages.Should().HaveCount(2);
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

                manager.PopulateNavigationFromPath(tempPath, new[] { ".mdx" });

                manager.Configuration!.Navigation!.Pages!.Should().HaveCount(1);
                manager.Configuration!.Navigation!.Pages!.Should().Contain("doc2");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        #endregion

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

            var apiGroup = manager.Configuration!.Navigation!.Pages!.OfType<GroupConfig>().First();
            apiGroup.Root.Should().Be("/docs/api");
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

                manager.PopulateNavigationFromPath(tempPath);

                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().HaveCount(1);

                var docsGroup = pages.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Docs");
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

                var manager = new DocsJsonManager();
                manager.Configuration = DocsJsonManager.CreateDefault("Test");

                manager.PopulateNavigationFromPath(tempPath);

                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().HaveCount(1);

                var docsGroup = pages.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Docs");
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

                var manager = new DocsJsonManager();
                manager.Configuration = DocsJsonManager.CreateDefault("Test");

                manager.PopulateNavigationFromPath(tempPath);

                // Should include .mdx files
                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().HaveCount(2);
                pages.Should().Contain("index");
                pages.Should().Contain("guide");

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

                manager.PopulateNavigationFromPath(tempPath);

                // Root level: index should be first
                var pages = manager.Configuration!.Navigation!.Pages!;
                pages[0].Should().Be("index");
                pages[1].Should().Be("apple");
                pages[2].Should().Be("zebra");

                // Group level: index should be first
                var guidesGroup = pages.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Guides");
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

                manager.PopulateNavigationFromPath(tempPath);

                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().HaveCount(1);

                var cliGroup = pages.OfType<GroupConfig>().FirstOrDefault();
                cliGroup.Should().NotBeNull();
                cliGroup!.Group.Should().Be("CLI Tools");
                cliGroup!.Icon.Should().Be("terminal");
                cliGroup!.Pages.Should().HaveCount(2);

                var coreGroup = cliGroup!.Pages!.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Core Commands");
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

                manager.PopulateNavigationFromPath(tempPath);

                // Should have warning about malformed JSON
                var jsonWarnings = manager.ConfigurationErrors.Where(e => e.ErrorNumber == "NAVIGATION_JSON");
                jsonWarnings.Should().HaveCount(1);
                jsonWarnings.First().ErrorText.Should().Contain("Invalid navigation.json file");

                // Should fall back to auto-generation
                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().HaveCount(1);

                var docsGroup = pages.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Docs");
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

                manager.PopulateNavigationFromPath(tempPath);

                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().HaveCount(1);

                // CLI root should use navigation.json override completely (no auto-generated subdirectories)
                var cliGroup = pages.OfType<GroupConfig>().FirstOrDefault();
                cliGroup.Should().NotBeNull();
                cliGroup!.Group.Should().Be("CLI Tools");
                cliGroup!.Icon.Should().Be("terminal");
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
                manager.PopulateNavigationFromPath(tempPath);

                // Step 2: Apply URL prefix
                manager.ApplyUrlPrefix("/v2");

                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().HaveCount(3);

                // Auto-generated content should be prefixed
                pages.Should().Contain("/v2/index");

                var guidesGroup = pages.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Guides");
                guidesGroup.Should().NotBeNull();
                guidesGroup!.Pages.Should().Contain("/v2/guides/intro");
                guidesGroup!.Pages.Should().Contain("/v2/guides/advanced");

                // Override content should be prefixed (navigation.json takes complete control of api directory)
                var apiGroup = pages.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "API Reference");
                apiGroup.Should().NotBeNull();
                apiGroup!.Icon.Should().Be("code");
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

                manager.PopulateNavigationFromPath(tempPath);

                var pages = manager.Configuration!.Navigation!.Pages!;
                pages.Should().HaveCount(2); // index + docs group
                pages.Should().Contain("index");

                var docsGroup = pages.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Docs");
                docsGroup.Should().NotBeNull();
                docsGroup!.Pages.Should().HaveCount(3); // index + guides group + api group
                docsGroup!.Pages.Should().Contain("docs/index");

                // Guides should be auto-generated with proper nesting
                var guidesGroup = docsGroup!.Pages!.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Guides");
                guidesGroup.Should().NotBeNull();
                guidesGroup!.Pages![0].Should().Be("docs/guides/index"); // Index first
                guidesGroup!.Pages!.Should().Contain("docs/guides/basic");

                var advancedGroup = guidesGroup!.Pages!.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "Advanced");
                advancedGroup.Should().NotBeNull();
                advancedGroup!.Pages![0].Should().Be("docs/guides/advanced/index"); // Index first
                advancedGroup!.Pages!.Should().Contain("docs/guides/advanced/patterns");

                // API should use override (navigation.json takes complete control)
                var apiGroup = docsGroup!.Pages!.OfType<GroupConfig>().FirstOrDefault(g => g.Group == "API Documentation");
                apiGroup.Should().NotBeNull();
                apiGroup!.Icon.Should().Be("api");
                apiGroup!.Pages.Should().Contain("docs/api/overview");
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
