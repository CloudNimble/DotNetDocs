using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CloudNimble.EasyAF.Core;
using Mintlify.Core.Models;

namespace Mintlify.Core
{

    /// <summary>
    /// Loads and manages Mintlify docs.json configuration files with navigation manipulation capabilities.
    /// </summary>
    /// <remarks>
    /// This class provides comprehensive support for loading, validating, and modifying Mintlify
    /// documentation configuration files. It handles serialization using the standard Mintlify
    /// JSON options and provides APIs for navigation manipulation.
    /// </remarks>
    public class DocsJsonManager
    {

        #region Fields

        /// <summary>
        /// Directories to exclude during navigation population.
        /// </summary>
        /// <remarks>
        /// These directories are excluded in addition to any directory that starts with a dot (.).
        /// The comparison is case-insensitive to handle different filesystem conventions.
        /// Common exclusions include package directories, internal documentation, and build artifacts.
        /// </remarks>
        private static readonly string[] ExcludedDirectories = { "node_modules", "conceptual", "overrides" };

        #endregion

        #region Properties

        /// <summary>
        /// Gets the loaded Mintlify documentation configuration.
        /// </summary>
        /// <value>
        /// The <see cref="DocsJsonConfig"/> instance loaded from the file system or string content.
        /// Returns null if no configuration has been loaded or if loading failed.
        /// </value>
        public DocsJsonConfig? Configuration { get; internal set; }

        /// <summary>
        /// Gets the collection of configuration loading errors encountered during processing.
        /// </summary>
        /// <value>
        /// A collection of <see cref="CompilerError"/> instances representing validation errors,
        /// parsing failures, or other issues encountered during configuration loading.
        /// </value>
        public List<CompilerError> ConfigurationErrors { get; private set; }

        /// <summary>
        /// Gets the file path of the loaded docs.json configuration file.
        /// </summary>
        /// <value>
        /// The full path to the docs.json file that was loaded. Returns null if the configuration
        /// was loaded from string content rather than a file.
        /// </value>
        public string? FilePath { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the configuration has been successfully loaded.
        /// </summary>
        /// <value>
        /// True if the configuration was loaded without errors; otherwise, false.
        /// </value>
        public bool IsLoaded => Configuration is not null && ConfigurationErrors.Count == 0;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DocsJsonManager"/> class.
        /// </summary>
        public DocsJsonManager()
        {
            ConfigurationErrors = [];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocsJsonManager"/> class with the specified file path.
        /// </summary>
        /// <param name="filePath">The path to the docs.json file to load.</param>
        /// <exception cref="ArgumentException">Thrown when the file path does not exist or is not a JSON file.</exception>
        public DocsJsonManager(string filePath) : this()
        {
            Ensure.ArgumentNotNullOrWhiteSpace(filePath, nameof(filePath));

            filePath = Path.GetFullPath(filePath);
            if (!File.Exists(filePath))
            {
                throw new ArgumentException("The filePath specified does not exist.", nameof(filePath));
            }

            if (!Path.GetExtension(filePath).Equals(".json", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException("The filePath specified does not point to a JSON file.", nameof(filePath));
            }

            FilePath = filePath;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads and parses the docs.json file from the file path specified in the constructor.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when no file path has been specified.</exception>
        public void Load()
        {
            if (string.IsNullOrWhiteSpace(FilePath))
            {
                throw new InvalidOperationException("No file path has been specified. Use the constructor overload or Load(string) method.");
            }

            var content = File.ReadAllText(FilePath);
            LoadInternal(content);
        }

        /// <summary>
        /// Loads and parses the docs.json configuration from the specified string content.
        /// </summary>
        /// <param name="content">The JSON content to parse as a docs.json configuration.</param>
        /// <exception cref="ArgumentException">Thrown when content is null or whitespace.</exception>
        public void Load(string content)
        {
            Ensure.ArgumentNotNullOrWhiteSpace(content, nameof(content));
            LoadInternal(content);
        }

        /// <summary>
        /// Saves the current configuration to the file system using the original file path.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when no configuration is loaded or no file path is specified.</exception>
        public void Save()
        {
            if (Configuration is null)
            {
                throw new InvalidOperationException("No configuration is loaded. Load a configuration before saving.");
            }

            if (string.IsNullOrWhiteSpace(FilePath))
            {
                throw new InvalidOperationException("No file path is specified. Use Save(string) to specify a path.");
            }

            Save(FilePath!);
        }

        /// <summary>
        /// Saves the current configuration to the specified file path.
        /// </summary>
        /// <param name="filePath">The file path where the configuration should be saved.</param>
        /// <exception cref="ArgumentException">Thrown when filePath is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no configuration is loaded.</exception>
        public void Save(string filePath)
        {
            Ensure.ArgumentNotNullOrWhiteSpace(filePath, nameof(filePath));

            if (Configuration is null)
            {
                throw new InvalidOperationException("No configuration is loaded. Load a configuration before saving.");
            }

            var json = JsonSerializer.Serialize(Configuration, MintlifyConstants.JsonSerializerOptions);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Creates a default Mintlify documentation configuration with basic structure.
        /// </summary>
        /// <param name="name">The name of the documentation site.</param>
        /// <param name="theme">The theme to use for the documentation site.</param>
        /// <returns>A new <see cref="DocsJsonConfig"/> instance with default values.</returns>
        /// <exception cref="ArgumentException">Thrown when name is null or whitespace.</exception>
        public static DocsJsonConfig CreateDefault(string name, string theme = "mint")
        {
            Ensure.ArgumentNotNullOrWhiteSpace(name, nameof(name));

            return new DocsJsonConfig
            {
                Name = name,
                Theme = theme,
                Colors = new ColorsConfig
                {
                    Primary = "#0D9373"
                },
                Navigation = new NavigationConfig
                {
                    Pages =
                    [
                        "index",
                        new GroupConfig
                        {
                            Group = "Getting Started",
                            Pages = ["quickstart"]
                        },
                        new GroupConfig
                        {
                            Group = "API Reference",
                            Pages = ["api-reference/index"]
                        }
                    ]
                }
            };
        }

        /// <summary>
        /// Merges another docs.json configuration into the current configuration.
        /// </summary>
        /// <param name="other">The configuration to merge into the current one.</param>
        /// <param name="combineBaseProperties">If true, merges base properties; if false, only merges navigation.</param>
        /// <param name="options">Optional merge options to control merge behavior. When null, default behavior is used.</param>
        /// <exception cref="ArgumentNullException">Thrown when other is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no configuration is loaded.</exception>
        /// <remarks>
        /// This method performs a shallow merge, with the other configuration taking precedence
        /// for non-null values. Navigation structures are combined intelligently.
        /// </remarks>
        public void Merge(DocsJsonConfig other, bool combineBaseProperties = true, MergeOptions? options = null)
        {
            Ensure.ArgumentNotNull(other, nameof(other));

            if (Configuration is null)
            {
                throw new InvalidOperationException("No configuration is loaded. Load a configuration before merging.");
            }

            if (combineBaseProperties)
            {
                // Merge primitive properties
                if (!string.IsNullOrWhiteSpace(other.Name))
                    Configuration.Name = other.Name;
                if (!string.IsNullOrWhiteSpace(other.Description))
                    Configuration.Description = other.Description;
                if (!string.IsNullOrWhiteSpace(other.Theme))
                    Configuration.Theme = other.Theme;

                // Merge complex objects
                if (other.Colors is not null)
                    Configuration.Colors = other.Colors;
                if (other.Logo is not null)
                    Configuration.Logo = other.Logo;
                if (other.Footer is not null)
                    Configuration.Footer = other.Footer;
            }

            // Always merge navigation
            if (other.Navigation is not null)
            {
                if (Configuration.Navigation is null)
                {
                    Configuration.Navigation = other.Navigation;
                }
                else
                {
                    MergeNavigation(Configuration.Navigation, other.Navigation, options);
                }
            }
        }

        /// <summary>
        /// Applies default values to missing configuration properties.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when no configuration is loaded.</exception>
        public void ApplyDefaults()
        {
            if (Configuration is null)
            {
                throw new InvalidOperationException("No configuration is loaded. Load a configuration before applying defaults.");
            }

            Configuration.Theme ??= "mint";
            Configuration.Schema ??= "https://mintlify.com/docs.json";

            if (Configuration.Navigation is null)
            {
                Configuration.Navigation = new NavigationConfig
                {
                    Pages = new List<object>
                    {
                        "index",
                        new GroupConfig
                        {
                            Group = "API Reference",
                            Pages = new List<object> { "api-reference/index" }
                        }
                    }
                };
            }
            else if (Configuration.Navigation.Pages is null)
            {
                Configuration.Navigation.Pages = new List<object>();
            }
        }

        /// <summary>
        /// Populates the navigation structure from a directory path by scanning for MDX files.
        /// </summary>
        /// <param name="path">The directory path to scan for documentation files.</param>
        /// <param name="fileExtensions">The file extensions to include (defaults to .mdx only).</param>
        /// <exception cref="ArgumentException">Thrown when path is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no configuration is loaded.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the specified path does not exist.</exception>
        /// <remarks>
        /// <para>This method scans the specified directory recursively and builds a navigation structure
        /// based on the folder hierarchy and MDX files found. The method includes several advanced features:</para>
        /// <list type="bullet">
        /// <item><description><strong>Directory Exclusions:</strong> Automatically excludes 'node_modules', 'conceptual', 
        /// 'overrides', and any directories starting with '.' (dot directories).</description></item>
        /// <item><description><strong>File Processing:</strong> Only includes .mdx files by default. .md files 
        /// generate warnings and are excluded from navigation.</description></item>
        /// <item><description><strong>Index File Priority:</strong> Files named 'index' appear first in each directory's navigation.</description></item>
        /// <item><description><strong>Enhanced Sorting:</strong> Multi-level sorting with index files first, 
        /// then regular files, then directories, all sorted alphabetically within their category.</description></item>
        /// <item><description><strong>Navigation Override:</strong> If a directory contains 'navigation.json', 
        /// the user assumes complete control over that directory and all subdirectories. The JSON file should 
        /// contain a complete GroupConfig object.</description></item>
        /// </list>
        /// <para>The method preserves the directory structure in the navigation while applying these intelligent 
        /// processing rules to create clean, organized documentation.</para>
        /// </remarks>
        public void PopulateNavigationFromPath(string path, string[]? fileExtensions = null)
        {
            Ensure.ArgumentNotNullOrWhiteSpace(path, nameof(path));

            if (Configuration is null)
            {
                throw new InvalidOperationException("No configuration is loaded. Load a configuration before populating navigation.");
            }

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"The specified path does not exist: {path}");
            }

            fileExtensions ??= [/*".md", */".mdx"];

            Configuration.Navigation ??= new NavigationConfig();
            Configuration.Navigation.Pages ??= [];

            // Clear existing pages to repopulate
            Configuration.Navigation.Pages.Clear();

            // Populate from directory structure
            PopulateNavigationFromDirectory(path, Configuration.Navigation.Pages, path, fileExtensions);
        }

        /// <summary>
        /// Applies a URL prefix to all page references in the navigation structure.
        /// </summary>
        /// <param name="prefix">The prefix to apply to all URLs.</param>
        /// <exception cref="ArgumentException">Thrown when prefix is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no configuration is loaded.</exception>
        /// <remarks>
        /// This method recursively traverses the entire navigation structure and prepends the
        /// specified prefix to all page URLs, href attributes, and root paths. The prefix is
        /// normalized to ensure proper URL formatting (e.g., trailing slashes are handled).
        /// </remarks>
        public void ApplyUrlPrefix(string prefix)
        {
            Ensure.ArgumentNotNullOrWhiteSpace(prefix, nameof(prefix));

            if (Configuration is null)
            {
                throw new InvalidOperationException("No configuration is loaded. Load a configuration before applying URL prefix.");
            }

            if (Configuration.Navigation is null)
            {
                return; // Nothing to prefix
            }

            // Normalize prefix - ensure it doesn't end with /
            prefix = prefix.TrimEnd('/');

            // Apply prefix to navigation pages
            if (Configuration.Navigation.Pages is not null)
            {
                ApplyUrlPrefixToPages(Configuration.Navigation.Pages, prefix);
            }

            // Apply prefix to groups
            if (Configuration.Navigation.Groups is not null)
            {
                foreach (var group in Configuration.Navigation.Groups)
                {
                    ApplyUrlPrefixToGroup(group, prefix);
                }
            }

            // Apply prefix to tabs
            if (Configuration.Navigation.Tabs is not null)
            {
                foreach (var tab in Configuration.Navigation.Tabs)
                {
                    ApplyUrlPrefixToTab(tab, prefix);
                }
            }

            // Apply prefix to anchors
            if (Configuration.Navigation.Anchors is not null)
            {
                foreach (var anchor in Configuration.Navigation.Anchors)
                {
                    ApplyUrlPrefixToAnchor(anchor, prefix);
                }
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Internal method to load and parse docs.json content from a string.
        /// </summary>
        /// <param name="content">The JSON content to parse.</param>
        internal void LoadInternal(string content)
        {
            ConfigurationErrors.Clear();

            try
            {
                Configuration = JsonSerializer.Deserialize<DocsJsonConfig>(content, MintlifyConstants.JsonSerializerOptions);

                if (Configuration is null)
                {
                    ConfigurationErrors.Add(new CompilerError(FilePath ?? "string", 0, 0, "JSON", "Failed to deserialize configuration from JSON content."));
                    return;
                }

                // Apply defensive validation and cleaning
                CleanNavigationGroups();

                // Apply basic validation
                ValidateConfiguration();
            }
            catch (JsonException ex)
            {
                ConfigurationErrors.Add(new CompilerError(FilePath ?? "string", 0, 0, "JSON", $"JSON parsing error: {ex.Message}"));
                Configuration = null;
            }
            catch (Exception ex)
            {
                ConfigurationErrors.Add(new CompilerError(FilePath ?? "string", 0, 0, "LOAD", $"Configuration loading error: {ex.Message}"));
                Configuration = null;
            }
        }

        /// <summary>
        /// Cleans navigation groups by removing invalid entries and logging warnings.
        /// </summary>
        internal void CleanNavigationGroups()
        {
            if (Configuration?.Navigation?.Pages is null)
                return;

            var cleanedPages = new List<object>();
            var nullGroupsRemoved = 0;
            var emptyGroupsFound = 0;

            foreach (var page in Configuration.Navigation.Pages)
            {
                if (page is GroupConfig group)
                {
                    if (group.Group is null)
                    {
                        // Skip groups with null names - these are invalid
                        nullGroupsRemoved++;
                        ConfigurationErrors.Add(new CompilerError(FilePath ?? "string", 0, 0, "VALIDATION",
                            "Group with null name found and removed. Mintlify will reject configurations with null group names."));
                        continue;
                    }
                    else if (string.IsNullOrWhiteSpace(group.Group))
                    {
                        emptyGroupsFound++;
                        // Keep empty groups but add warning
                        ConfigurationErrors.Add(new CompilerError(FilePath ?? "string", 0, 0, "VALIDATION",
                            "Empty group name found. Mintlify treats empty groups as separate ungrouped sections.")
                        {
                            IsWarning = true
                        });
                    }

                    // Recursively clean nested groups if present
                    if (group.Pages is not null)
                    {
                        CleanNestedGroups(group.Pages);
                    }
                }

                // Add valid pages/groups to cleaned list
                if (!(page is GroupConfig g && g.Group is null))
                {
                    cleanedPages.Add(page);
                }
            }

            // Replace pages with cleaned list
            Configuration.Navigation.Pages = cleanedPages;

            // Also clean Groups property if present
            if (Configuration.Navigation.Groups is not null)
            {
                var cleanedGroups = new List<GroupConfig>();
                foreach (var group in Configuration.Navigation.Groups)
                {
                    if (group.Group is null)
                    {
                        nullGroupsRemoved++;
                        ConfigurationErrors.Add(new CompilerError(FilePath ?? "string", 0, 0, "VALIDATION",
                            "Group with null name found in Groups list and removed."));
                        continue;
                    }
                    else if (string.IsNullOrWhiteSpace(group.Group))
                    {
                        emptyGroupsFound++;
                        ConfigurationErrors.Add(new CompilerError(FilePath ?? "string", 0, 0, "VALIDATION",
                            "Empty group name found in Groups list.")
                        {
                            IsWarning = true
                        });
                    }
                    cleanedGroups.Add(group);
                }
                Configuration.Navigation.Groups = cleanedGroups;
            }
        }

        /// <summary>
        /// Recursively cleans nested groups in a pages list.
        /// </summary>
        /// <param name="pages">The pages list to clean.</param>
        private void CleanNestedGroups(List<object> pages)
        {
            var cleanedPages = new List<object>();
            foreach (var page in pages)
            {
                if (page is GroupConfig group)
                {
                    if (group.Group is null)
                    {
                        ConfigurationErrors.Add(new CompilerError(FilePath ?? "string", 0, 0, "VALIDATION",
                            "Nested group with null name found and removed."));
                        continue;
                    }
                    else if (string.IsNullOrWhiteSpace(group.Group))
                    {
                        ConfigurationErrors.Add(new CompilerError(FilePath ?? "string", 0, 0, "VALIDATION",
                            "Empty nested group name found.")
                        {
                            IsWarning = true
                        });
                    }

                    // Recursively clean further nested groups
                    if (group.Pages is not null)
                    {
                        CleanNestedGroups(group.Pages);
                    }
                }

                // Add valid pages/groups to cleaned list
                if (!(page is GroupConfig g && g.Group is null))
                {
                    cleanedPages.Add(page);
                }
            }
            pages.Clear();
            pages.AddRange(cleanedPages);
        }

        /// <summary>
        /// Validates the loaded configuration for common issues.
        /// </summary>
        internal void ValidateConfiguration()
        {
            if (Configuration is null)
                return;

            if (string.IsNullOrWhiteSpace(Configuration.Name))
            {
                ConfigurationErrors.Add(new CompilerError(FilePath ?? "string", 0, 0, "VALIDATION", "Configuration name is required but was not provided.")
                {
                    IsWarning = true
                });
            }

            if (Configuration.Navigation is null ||
                (Configuration.Navigation.Pages is null && Configuration.Navigation.Groups is null && Configuration.Navigation.Tabs is null))
            {
                ConfigurationErrors.Add(new CompilerError(FilePath ?? "string", 0, 0, "VALIDATION", "Navigation configuration is missing or empty.")
                {
                    IsWarning = true
                });
            }
        }

        /// <summary>
        /// Merges navigation configurations intelligently.
        /// </summary>
        /// <param name="target">The target navigation configuration to merge into.</param>
        /// <param name="source">The source navigation configuration to merge from.</param>
        /// <param name="options">Optional merge options to control merge behavior. When null, default behavior is used.</param>
        internal static void MergeNavigation(NavigationConfig target, NavigationConfig source, MergeOptions? options = null)
        {
            if (source is null)
                return;

            // Merge pages with intelligent deduplication and group merging
            if (source.Pages is not null)
            {
                if (target.Pages is null)
                {
                    target.Pages = [.. source.Pages];
                }
                else
                {
                    MergePagesList(target.Pages, source.Pages, options);
                }
            }

            // Merge groups intelligently
            if (source.Groups is not null)
            {
                if (target.Groups is null)
                {
                    target.Groups = [.. source.Groups];
                }
                else
                {
                    MergeGroupsList(target.Groups, source.Groups, options);
                }
            }

            // Merge tabs intelligently
            if (source.Tabs is not null)
            {
                if (target.Tabs is null)
                {
                    target.Tabs = [.. source.Tabs];
                }
                else
                {
                    MergeTabsList(target.Tabs, source.Tabs, options);
                }
            }

            // Merge other navigation properties
            if (source.Anchors is not null)
            {
                target.Anchors ??= [];
                target.Anchors.AddRange(source.Anchors);
            }

            if (source.Global is not null)
                target.Global = source.Global;
        }

        /// <summary>
        /// Intelligently merges two pages lists, handling both strings and GroupConfig objects.
        /// </summary>
        /// <param name="targetPages">The target pages list to merge into.</param>
        /// <param name="sourcePages">The source pages list to merge from.</param>
        /// <param name="options">Optional merge options to control merge behavior. When null, default behavior is used.</param>
        internal static void MergePagesList(List<object> targetPages, List<object> sourcePages, MergeOptions? options = null)
        {
            var processedPages = new List<object>();
            var seenStringPages = new HashSet<string>();
            var groupsByName = new Dictionary<string, GroupConfig>();

            // Determine if we should combine empty groups based on options
            // When options?.CombineEmptyGroups == true, empty groups are merged together
            // When options?.CombineEmptyGroups != true (default), empty groups remain separate (Mintlify behavior)
            var combineEmptyGroups = options?.CombineEmptyGroups ?? false;

            // First pass: process target pages, keeping order and deduplicating
            foreach (var page in targetPages)
            {
                switch (page)
                {
                    case string stringPage:
                        if (seenStringPages.Add(stringPage))
                        {
                            processedPages.Add(stringPage);
                        }
                        break;
                    case GroupConfig groupConfig when groupConfig.Group is null:
                        // Skip groups with null names - these are invalid and will be caught by validation
                        break;
                    case GroupConfig groupConfig when !string.IsNullOrWhiteSpace(groupConfig.Group):
                        if (!groupsByName.ContainsKey(groupConfig.Group))
                        {
                            groupsByName[groupConfig.Group] = groupConfig;
                            processedPages.Add(groupConfig);
                        }
                        break;
                    case GroupConfig groupConfig when string.IsNullOrWhiteSpace(groupConfig.Group):
                        // Handle empty group names based on options
                        if (combineEmptyGroups)
                        {
                            // Combine empty groups: use empty string as key
                            if (!groupsByName.ContainsKey(string.Empty))
                            {
                                groupsByName[string.Empty] = groupConfig;
                                processedPages.Add(groupConfig);
                            }
                            else
                            {
                                // Merge into existing empty group
                                MergeGroupConfig(groupsByName[string.Empty], groupConfig, options);
                            }
                        }
                        else
                        {
                            // Keep empty groups separate (default Mintlify behavior)
                            processedPages.Add(groupConfig);
                        }
                        break;
                    default:
                        processedPages.Add(page);
                        break;
                }
            }

            // Second pass: process source pages, merging groups and adding new items
            foreach (var page in sourcePages)
            {
                switch (page)
                {
                    case string stringPage:
                        if (seenStringPages.Add(stringPage))
                        {
                            processedPages.Add(stringPage);
                        }
                        break;
                    case GroupConfig sourceGroup when sourceGroup.Group is null:
                        // Skip groups with null names - these are invalid and will be caught by validation
                        break;
                    case GroupConfig sourceGroup when !string.IsNullOrWhiteSpace(sourceGroup.Group):
                        if (groupsByName.TryGetValue(sourceGroup.Group, out var existingGroup))
                        {
                            // Merge the groups
                            MergeGroupConfig(existingGroup, sourceGroup, options);
                        }
                        else
                        {
                            groupsByName[sourceGroup.Group] = sourceGroup;
                            processedPages.Add(sourceGroup);
                        }
                        break;
                    case GroupConfig sourceGroup when string.IsNullOrWhiteSpace(sourceGroup.Group):
                        // Handle empty group names based on options
                        if (combineEmptyGroups)
                        {
                            // Combine empty groups: use empty string as key
                            if (!groupsByName.ContainsKey(string.Empty))
                            {
                                groupsByName[string.Empty] = sourceGroup;
                                processedPages.Add(sourceGroup);
                            }
                            else
                            {
                                // Merge into existing empty group
                                MergeGroupConfig(groupsByName[string.Empty], sourceGroup, options);
                            }
                        }
                        else
                        {
                            // Keep empty groups separate (default Mintlify behavior)
                            processedPages.Add(sourceGroup);
                        }
                        break;
                    default:
                        processedPages.Add(page);
                        break;
                }
            }

            // Replace target pages with processed result
            targetPages.Clear();
            targetPages.AddRange(processedPages);
        }

        /// <summary>
        /// Merges two GroupConfig objects with the same group name.
        /// </summary>
        /// <param name="target">The target group to merge into.</param>
        /// <param name="source">The source group to merge from.</param>
        /// <param name="options">Optional merge options to control merge behavior. When null, default behavior is used.</param>
        internal static void MergeGroupConfig(GroupConfig target, GroupConfig source, MergeOptions? options = null)
        {
            // Merge basic properties
            if (!string.IsNullOrWhiteSpace(source.Tag))
                target.Tag = source.Tag;
            if (!string.IsNullOrWhiteSpace(source.Root))
                target.Root = source.Root;
            if (source.Hidden.HasValue)
                target.Hidden = source.Hidden;
            if (source.Icon is not null)
                target.Icon = source.Icon;
            if (source.AsyncApi is not null)
                target.AsyncApi = source.AsyncApi;
            if (source.OpenApi is not null)
                target.OpenApi = source.OpenApi;

            // Merge pages recursively
            if (source.Pages is not null)
            {
                if (target.Pages is null)
                {
                    target.Pages = [.. source.Pages];
                }
                else
                {
                    MergePagesList(target.Pages, source.Pages, options);
                }
            }
        }

        /// <summary>
        /// Intelligently merges two groups lists, combining groups with the same name.
        /// </summary>
        /// <param name="targetGroups">The target groups list to merge into.</param>
        /// <param name="sourceGroups">The source groups list to merge from.</param>
        /// <param name="options">Optional merge options to control merge behavior. When null, default behavior is used.</param>
        internal static void MergeGroupsList(List<GroupConfig> targetGroups, List<GroupConfig> sourceGroups, MergeOptions? options = null)
        {
            var groupsByName = new Dictionary<string, GroupConfig>();
            var groupsWithoutNames = new List<GroupConfig>();

            // First, catalog what's in the target
            foreach (var group in targetGroups)
            {
                if (!string.IsNullOrWhiteSpace(group.Group))
                {
                    groupsByName[group.Group] = group;
                }
                else
                {
                    groupsWithoutNames.Add(group);
                }
            }

            // Process source groups
            foreach (var sourceGroup in sourceGroups)
            {
                if (!string.IsNullOrWhiteSpace(sourceGroup.Group))
                {
                    if (groupsByName.TryGetValue(sourceGroup.Group, out var existingGroup))
                    {
                        // Merge the groups
                        MergeGroupConfig(existingGroup, sourceGroup, options);
                    }
                    else
                    {
                        groupsByName[sourceGroup.Group] = sourceGroup;
                    }
                }
                else
                {
                    groupsWithoutNames.Add(sourceGroup);
                }
            }

            // Rebuild the groups list
            targetGroups.Clear();
            targetGroups.AddRange(groupsByName.Values.OrderBy(g => g.Group));
            targetGroups.AddRange(groupsWithoutNames);
        }

        /// <summary>
        /// Intelligently merges two tabs lists, combining tabs with the same name or overlapping paths.
        /// </summary>
        /// <param name="targetTabs">The target tabs list to merge into.</param>
        /// <param name="sourceTabs">The source tabs list to merge from.</param>
        /// <param name="options">Optional merge options to control merge behavior. When null, default behavior is used.</param>
        internal static void MergeTabsList(List<TabConfig> targetTabs, List<TabConfig> sourceTabs, MergeOptions? options = null)
        {
            var tabsByName = new Dictionary<string, TabConfig>();
            var tabsByHref = new Dictionary<string, TabConfig>();
            var processedTabs = new List<TabConfig>();

            // First, catalog what's in the target
            foreach (var tab in targetTabs)
            {
                processedTabs.Add(tab);
                if (!string.IsNullOrWhiteSpace(tab.Tab))
                {
                    tabsByName[tab.Tab] = tab;
                }
                if (!string.IsNullOrWhiteSpace(tab.Href))
                {
                    tabsByHref[tab.Href!] = tab;
                }
            }

            // Process source tabs
            foreach (var sourceTab in sourceTabs)
            {

                // First try to match by name
                if (!string.IsNullOrWhiteSpace(sourceTab.Tab) && tabsByName.TryGetValue(sourceTab.Tab, out var existingTab))
                {
                    MergeTabConfig(existingTab, sourceTab, options);
                    // Update href mapping if source tab has href and target didn't
                    if (!string.IsNullOrWhiteSpace(sourceTab.Href) && !tabsByHref.ContainsKey(sourceTab.Href!))
                    {
                        tabsByHref[sourceTab.Href!] = existingTab;
                    }
                }
                // Then try to match by href
                else if (!string.IsNullOrWhiteSpace(sourceTab.Href) && tabsByHref.TryGetValue(sourceTab.Href!, out existingTab))
                {
                    MergeTabConfig(existingTab, sourceTab, options);
                    // Update name mapping if source tab has name and target didn't
                    if (!string.IsNullOrWhiteSpace(sourceTab.Tab) && !tabsByName.ContainsKey(sourceTab.Tab))
                    {
                        tabsByName[sourceTab.Tab] = existingTab;
                    }
                }
                // If no match found, add as new tab
                else
                {
                    processedTabs.Add(sourceTab);
                    if (!string.IsNullOrWhiteSpace(sourceTab.Tab))
                    {
                        tabsByName[sourceTab.Tab] = sourceTab;
                    }
                    if (!string.IsNullOrWhiteSpace(sourceTab.Href))
                    {
                        tabsByHref[sourceTab.Href!] = sourceTab;
                    }
                }
            }

            // Replace the target tabs with processed result
            targetTabs.Clear();
            targetTabs.AddRange(processedTabs);
        }

        /// <summary>
        /// Merges two TabConfig objects with the same name or path.
        /// </summary>
        /// <param name="target">The target tab to merge into.</param>
        /// <param name="source">The source tab to merge from.</param>
        /// <param name="options">Optional merge options to control merge behavior. When null, default behavior is used.</param>
        internal static void MergeTabConfig(TabConfig target, TabConfig source, MergeOptions? options = null)
        {
            // Merge basic properties
            if (!string.IsNullOrWhiteSpace(source.Tab))
                target.Tab = source.Tab;
            if (!string.IsNullOrWhiteSpace(source.Href))
                target.Href = source.Href;
            if (source.Hidden.HasValue)
                target.Hidden = source.Hidden;
            if (source.Icon is not null)
                target.Icon = source.Icon;
            if (source.AsyncApi is not null)
                target.AsyncApi = source.AsyncApi;
            if (source.OpenApi is not null)
                target.OpenApi = source.OpenApi;
            if (source.Global is not null)
                target.Global = source.Global;

            // Merge pages recursively
            if (source.Pages is not null)
            {
                if (target.Pages is null)
                {
                    target.Pages = [.. source.Pages];
                }
                else
                {
                    MergePagesList(target.Pages, source.Pages, options);
                }
            }

            // Merge groups recursively
            if (source.Groups is not null)
            {
                if (target.Groups is null)
                {
                    target.Groups = [.. source.Groups];
                }
                else
                {
                    MergeGroupsList(target.Groups, source.Groups, options);
                }
            }

            // Merge other collections
            if (source.Anchors is not null)
            {
                target.Anchors ??= [];
                target.Anchors.AddRange(source.Anchors);
            }

            if (source.Dropdowns is not null)
            {
                target.Dropdowns ??= [];
                target.Dropdowns.AddRange(source.Dropdowns);
            }

            if (source.Languages is not null)
            {
                target.Languages ??= [];
                target.Languages.AddRange(source.Languages);
            }

            if (source.Versions is not null)
            {
                target.Versions ??= [];
                target.Versions.AddRange(source.Versions);
            }
        }

        /// <summary>
        /// Recursively populates navigation from a directory structure with advanced processing rules.
        /// </summary>
        /// <param name="currentPath">The current directory being processed.</param>
        /// <param name="pages">The list of pages to populate with navigation items.</param>
        /// <param name="rootPath">The root path for calculating relative URLs.</param>
        /// <param name="fileExtensions">The file extensions to include in processing.</param>
        /// <remarks>
        /// <para>This internal method implements the core navigation generation logic with the following processing order:</para>
        /// <list type="number">
        /// <item><description><strong>Navigation Override Check:</strong> First checks for navigation.json in the current directory.
        /// If found and valid, uses the custom GroupConfig and stops all automatic processing for this directory tree.</description></item>
        /// <item><description><strong>File and Directory Discovery:</strong> Scans for files matching the specified extensions
        /// and discovers subdirectories that aren't excluded.</description></item>
        /// <item><description><strong>Exclusion Filtering:</strong> Filters out directories starting with '.' and those
        /// matching the ExcludedDirectories list (case-insensitive).</description></item>
        /// <item><description><strong>Multi-level Sorting:</strong> Sorts entries with index files first, then regular files,
        /// then directories, with alphabetical sorting within each category.</description></item>
        /// <item><description><strong>Recursive Processing:</strong> Processes subdirectories recursively, respecting
        /// navigation.json overrides at each level.</description></item>
        /// </list>
        /// <para>Warning messages are added to ConfigurationErrors for .md files encountered during processing.</para>
        /// </remarks>
        internal void PopulateNavigationFromDirectory(string currentPath, List<object> pages, string rootPath, string[] fileExtensions)
        {
            // Check for navigation.json override first
            var navigationJsonPath = Path.Combine(currentPath, "navigation.json");
            if (File.Exists(navigationJsonPath))
            {
                try
                {
                    var jsonContent = File.ReadAllText(navigationJsonPath);
                    var customGroup = JsonSerializer.Deserialize<GroupConfig>(jsonContent, MintlifyConstants.JsonSerializerOptions);
                    if (customGroup is not null)
                    {
                        pages.Add(customGroup);
                        return; // User controls this directory and all subdirectories completely
                    }
                    else
                    {
                        ConfigurationErrors.Add(new CompilerError(navigationJsonPath, 0, 0, "NAVIGATION_JSON",
                            "navigation.json deserialized to null - check JSON format")
                        {
                            IsWarning = true
                        });
                    }
                }
                catch (JsonException ex)
                {
                    ConfigurationErrors.Add(new CompilerError(navigationJsonPath, 0, 0, "NAVIGATION_JSON",
                        $"Invalid navigation.json file: {ex.Message}")
                    {
                        IsWarning = true
                    });
                    // Continue with automatic processing as fallback
                }
                catch (Exception ex)
                {
                    ConfigurationErrors.Add(new CompilerError(navigationJsonPath, 0, 0, "NAVIGATION_JSON",
                        $"Error reading navigation.json file: {ex.Message}")
                    {
                        IsWarning = true
                    });
                    // Continue with automatic processing as fallback
                }
            }

            var entries = new List<(string name, string path, bool isDirectory)>();

            // Get all files and directories
            foreach (var file in Directory.GetFiles(currentPath))
            {
                var extension = Path.GetExtension(file);

                if (fileExtensions.Any(ext => extension.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    entries.Add((Path.GetFileNameWithoutExtension(file), file, false));
                }
                else if (extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
                {
                    // Add warning for .md files
                    ConfigurationErrors.Add(new CompilerError(file, 0, 0, "MD_FILE_WARNING",
                        $"Found .md file '{Path.GetFileName(file)}' - only .mdx files are supported in navigation")
                    {
                        IsWarning = true
                    });
                }
            }

            foreach (var directory in Directory.GetDirectories(currentPath))
            {
                var dirName = Path.GetFileName(directory);
                // Skip hidden directories and excluded directories
                if (!dirName.StartsWith(".") &&
#if NETSTANDARD2_0
                    !ExcludedDirectories.Any(excluded => string.Equals(excluded, dirName, StringComparison.OrdinalIgnoreCase))
#else
                    !ExcludedDirectories.Contains(dirName, StringComparer.OrdinalIgnoreCase)
#endif
                    )
                {
                    entries.Add((dirName, directory, true));
                }
            }

            // Multi-level sorting: index files first, then files before directories, then alphabetical
            entries = entries
                .OrderBy(e => e.name.Equals("index", StringComparison.OrdinalIgnoreCase) ? 0 : 1) // Index files first
                .ThenBy(e => e.isDirectory ? 1 : 0) // Files before directories
#if NETSTANDARD2_0
                .ThenBy(e => e.name.ToLowerInvariant()) // Alphabetical (case-insensitive for .NET Standard 2.0)
#else
                .ThenBy(e => e.name, StringComparer.OrdinalIgnoreCase) // Alphabetical (case-insensitive)
#endif
                .ToList();

            foreach (var (name, path, isDirectory) in entries)
            {
                if (isDirectory)
                {
                    var subPages = new List<object>();
                    PopulateNavigationFromDirectory(path, subPages, rootPath, fileExtensions);

                    if (subPages.Any())
                    {
                        // Check if the first item is already a GroupConfig (from navigation.json override)
                        if (subPages.Count == 1 && subPages[0] is GroupConfig existingGroup)
                        {
                            pages.Add(existingGroup);
                        }
                        else
                        {
                            var group = new GroupConfig
                            {
                                Group = FormatGroupName(name),
                                Pages = subPages
                            };
                            pages.Add(group);
                        }
                    }
                }
                else
                {
                    // Convert file path to relative URL
#if NETSTANDARD2_0
                    var rootUri = new Uri(rootPath.EndsWith("\\") ? rootPath : rootPath + "\\");
                    var pathUri = new Uri(path);
                    var relativePath = rootUri.MakeRelativeUri(pathUri).ToString().Replace('/', '\\');
#else
                    var relativePath = Path.GetRelativePath(rootPath, path);
#endif
                    var url = relativePath.Replace('\\', '/');
                    // Remove file extension
                    url = Path.ChangeExtension(url, null).TrimEnd('.');

                    // Include all files (index files now come first due to sorting)
                    pages.Add(url);
                }
            }
        }

        /// <summary>
        /// Formats a directory name into a user-friendly group name.
        /// </summary>
        internal static string FormatGroupName(string directoryName)
        {
            // Replace hyphens and underscores with spaces
            var formatted = directoryName.Replace('-', ' ').Replace('_', ' ');

            // Title case the result
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(formatted.ToLower());
        }

        /// <summary>
        /// Applies URL prefix to a list of pages.
        /// </summary>
        internal static void ApplyUrlPrefixToPages(List<object> pages, string prefix)
        {
            for (int i = 0; i < pages.Count; i++)
            {
                switch (pages[i])
                {
                    case string pageUrl:
                        pages[i] = $"{prefix}/{pageUrl}";
                        break;
                    case GroupConfig group:
                        ApplyUrlPrefixToGroup(group, prefix);
                        break;
                }
            }
        }

        /// <summary>
        /// Applies URL prefix to a group configuration.
        /// </summary>
        internal static void ApplyUrlPrefixToGroup(GroupConfig group, string prefix)
        {
            if (group is null)
                return;

            if (!string.IsNullOrWhiteSpace(group.Root))
            {
                group.Root = $"{prefix}/{group.Root}";
            }

            if (group.Pages is not null)
            {
                ApplyUrlPrefixToPages(group.Pages, prefix);
            }
        }

        /// <summary>
        /// Applies URL prefix to a tab configuration.
        /// </summary>
        internal static void ApplyUrlPrefixToTab(TabConfig tab, string prefix)
        {
            if (tab is null)
                return;

            if (!string.IsNullOrWhiteSpace(tab.Href))
            {
                tab.Href = $"{prefix}/{tab.Href}";
            }

            if (tab.Pages is not null)
            {
                ApplyUrlPrefixToPages(tab.Pages, prefix);
            }

            if (tab.Groups is not null)
            {
                foreach (var group in tab.Groups)
                {
                    ApplyUrlPrefixToGroup(group, prefix);
                }
            }

            if (tab.Anchors is not null)
            {
                foreach (var anchor in tab.Anchors)
                {
                    ApplyUrlPrefixToAnchor(anchor, prefix);
                }
            }
        }

        /// <summary>
        /// Applies URL prefix to an anchor configuration.
        /// </summary>
        internal static void ApplyUrlPrefixToAnchor(AnchorConfig anchor, string prefix)
        {
            if (anchor is null)
                return;

            if (!string.IsNullOrWhiteSpace(anchor.Href))
            {
                anchor.Href = $"{prefix}/{anchor.Href}";
            }

            if (anchor.Pages is not null)
            {
                ApplyUrlPrefixToPages(anchor.Pages, prefix);
            }

            if (anchor.Groups is not null)
            {
                foreach (var group in anchor.Groups)
                {
                    ApplyUrlPrefixToGroup(group, prefix);
                }
            }

            if (anchor.Tabs is not null)
            {
                foreach (var tab in anchor.Tabs)
                {
                    ApplyUrlPrefixToTab(tab, prefix);
                }
            }
        }

        #endregion

    }

}
