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

    /// <summary>
    /// Tests for the DocsJsonManager class that handles loading and managing Mintlify documentation configurations.
    /// </summary>
    [TestClass]
    public partial class DocsJsonManagerTests
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
            var act1 = () => new DocsJsonManager((string)null!, (DocsJsonValidator?)null);
            var act2 = () => new DocsJsonManager("", (DocsJsonValidator?)null);
            var act3 = () => new DocsJsonManager("   ", (DocsJsonValidator?)null);

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

            var act1 = () => manager.Load((string)null!);
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
            manager.ConfigurationErrors.Should().ContainSingle();
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
            ((string?)group!.Icon).Should().Be("code");
            group!.Pages.Should().HaveCount(2);
            group!.Pages![1].Should().BeOfType<GroupConfig>();
        }

        #endregion

        #region Load DocsJsonConfig Tests

        /// <summary>
        /// Tests that Load(DocsJsonConfig) populates _knownPagePaths with navigation pages.
        /// </summary>
        [TestMethod]
        public void Load_DocsJsonConfig_PopulatesKnownPagePaths()
        {
            var config = new DocsJsonConfig
            {
                Name = "Test Documentation",
                Theme = "mint",
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        "index",
                        "quickstart",
                        new GroupConfig
                        {
                            Group = "API Reference",
                            Pages = ["api-reference/index", "api-reference/classes"]
                        }
                    ]
                }
            };

            var manager = new DocsJsonManager();
            manager.Load(config);

            // Verify configuration is loaded
            manager.Configuration.Should().Be(config);
            manager.IsLoaded.Should().BeTrue();

            // Verify _knownPagePaths is populated (access via PopulateNavigationFromPath behavior)
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempPath);

                // Create files that should be skipped (already in template)
                File.WriteAllText(Path.Combine(tempPath, "index.mdx"), "# Index");
                File.WriteAllText(Path.Combine(tempPath, "quickstart.mdx"), "# Quickstart");

                // Create file that should be added (not in template)
                File.WriteAllText(Path.Combine(tempPath, "new-page.mdx"), "# New Page");

                var initialPageCount = config.Navigation.Pages.Count;
                manager.PopulateNavigationFromPath(tempPath, preserveExisting: true);

                // Should have one more page (new-page), but not duplicates of existing ones
                var currentPageCount = manager.Configuration!.Navigation!.Pages!.Count;
                currentPageCount.Should().BeGreaterThan(initialPageCount);

                // Verify the new page was added to Getting Started
                var gettingStartedGroup = manager.Configuration.Navigation.Pages?
                    .OfType<GroupConfig>()
                    .FirstOrDefault(g => g.Group == "Getting Started");
                gettingStartedGroup?.Pages?.Should().Contain("new-page");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        /// <summary>
        /// Tests that Load(DocsJsonConfig) with nested groups populates _knownPagePaths correctly.
        /// </summary>
        [TestMethod]
        public void Load_DocsJsonConfig_WithNestedGroups_PopulatesKnownPagePaths()
        {
            var config = new DocsJsonConfig
            {
                Name = "Test Documentation",
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        new GroupConfig
                        {
                            Group = "Getting Started",
                            Pages =
                            [
                                "index",
                                new GroupConfig
                                {
                                    Group = "Installation",
                                    Pages = ["install/windows", "install/linux"]
                                }
                            ]
                        }
                    ]
                }
            };

            var manager = new DocsJsonManager();
            manager.Load(config);

            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempPath);
                Directory.CreateDirectory(Path.Combine(tempPath, "install"));

                // Create files that match nested navigation paths
                File.WriteAllText(Path.Combine(tempPath, "index.mdx"), "# Index");
                File.WriteAllText(Path.Combine(tempPath, "install", "windows.mdx"), "# Windows Install");
                File.WriteAllText(Path.Combine(tempPath, "install", "linux.mdx"), "# Linux Install");

                // Create a new file that should be discovered
                File.WriteAllText(Path.Combine(tempPath, "install", "macos.mdx"), "# macOS Install");

                var initialNavigationJson = JsonSerializer.Serialize(config.Navigation, _jsonOptions);
                manager.PopulateNavigationFromPath(tempPath, preserveExisting: true);
                var finalNavigationJson = JsonSerializer.Serialize(manager.Configuration!.Navigation, _jsonOptions);

                // The navigation structure should have changed (new file added)
                finalNavigationJson.Should().NotBe(initialNavigationJson);

                // But existing paths should not be duplicated
                var allPagesFlat = GetAllPagesFlat(manager.Configuration.Navigation.Pages!);
                allPagesFlat.Count(p => p == "index").Should().Be(1);
                allPagesFlat.Count(p => p == "install/windows").Should().Be(1);
                allPagesFlat.Count(p => p == "install/linux").Should().Be(1);

                // New page should be added
                allPagesFlat.Should().Contain("install/macos");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        /// <summary>
        /// Tests that ApplyDefaults with parameters uses CreateDefault output.
        /// </summary>
        [TestMethod]
        public void ApplyDefaults_WithParameters_UsesCreateDefaultOutput()
        {
            var config = new DocsJsonConfig(); // Empty config
            var manager = new DocsJsonManager();
            manager.Load(config);

            manager.ApplyDefaults("Custom Name", "custom-theme");

            manager.Configuration!.Name.Should().Be("Custom Name");
            manager.Configuration.Theme.Should().Be("custom-theme");
            manager.Configuration.Schema.Should().Be("https://mintlify.com/docs.json");

            // Should have default navigation structure from CreateDefault
            manager.Configuration.Navigation.Should().NotBeNull();
            manager.Configuration.Navigation!.Pages.Should().HaveCount(2);

            var groups = manager.Configuration.Navigation.Pages?.OfType<GroupConfig>().ToList();
            groups.Should().HaveCount(2);
            groups.Should().Contain(g => g.Group == "Getting Started");
            groups.Should().Contain(g => g.Group == "API Reference");

            // Should have default colors
            manager.Configuration.Colors.Should().NotBeNull();
            manager.Configuration.Colors!.Primary.Should().Be("#0D9373");
        }

        /// <summary>
        /// Tests that PopulateNavigationFromPath skips pages already in _knownPagePaths.
        /// </summary>
        [TestMethod]
        public void PopulateNavigationFromPath_SkipsKnownPages()
        {
            var manager = new DocsJsonManager();
            var config = DocsJsonManager.CreateDefault("Test");
            manager.Load(config);

            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempPath);

                // Create files that match default navigation
                File.WriteAllText(Path.Combine(tempPath, "index.mdx"), "# Index");
                File.WriteAllText(Path.Combine(tempPath, "quickstart.mdx"), "# Quickstart");

                // Also create the api-reference directory structure that would match defaults
                Directory.CreateDirectory(Path.Combine(tempPath, "api-reference"));
                File.WriteAllText(Path.Combine(tempPath, "api-reference", "index.mdx"), "# API Reference");

                var initialNavigationJson = JsonSerializer.Serialize(config.Navigation, _jsonOptions);
                manager.PopulateNavigationFromPath(tempPath, preserveExisting: true);
                var finalNavigationJson = JsonSerializer.Serialize(manager.Configuration!.Navigation, _jsonOptions);

                // Navigation should be unchanged because all discovered files already exist in template
                finalNavigationJson.Should().Be(initialNavigationJson);
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        #endregion

        #region Helper Methods

        private static DocsJsonManager LoadDocument(string extraJsonMembers)
        {
            var json = $$"""
                {
                    "name": "Test Documentation",
                    "theme": "mint",
                    "colors": { "primary": "#419AC5" },
                    "navigation": ["index"]
                    {{(string.IsNullOrWhiteSpace(extraJsonMembers) ? string.Empty : "," + extraJsonMembers)}}
                }
                """;

            var manager = new DocsJsonManager();
            manager.Load(json);
            return manager;
        }

        private static DocsJsonConfig SaveAndReload(DocsJsonManager manager)
        {
            var jsonFile = Path.ChangeExtension(Path.GetTempFileName(), ".json");

            try
            {
                manager.Save(jsonFile);
                var reloaded = new DocsJsonManager();
                reloaded.Load(File.ReadAllText(jsonFile));
                reloaded.Configuration.Should().NotBeNull();
                return reloaded.Configuration!;
            }
            finally
            {
                if (File.Exists(jsonFile))
                {
                    File.Delete(jsonFile);
                }
            }
        }

        /// <summary>
        /// Helper method to flatten all pages from a navigation structure.
        /// </summary>
        private static List<string> GetAllPagesFlat(List<object> pages)
        {
            var result = new List<string>();

            foreach (var page in pages)
            {
                switch (page)
                {
                    case string stringPage:
                        result.Add(stringPage);
                        break;
                    case GroupConfig group when group.Pages is not null:
                        result.AddRange(GetAllPagesFlat(group.Pages));
                        break;
                }
            }

            return result;
        }

        #endregion

    }

}
