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

        /// <summary>
        /// Stores the set of page paths that are recognized as known within the application.
        /// </summary>
        internal readonly HashSet<string> _knownPagePaths = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The validator used for validating DocsJsonConfig instances.
        /// </summary>
        private readonly DocsJsonValidator _validator;

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
        public List<CompilerError> ConfigurationErrors { get; private set; } = [];

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
        /// <param name="validator">The validator to use for validating configurations. If null, a new instance will be created.</param>
        public DocsJsonManager(DocsJsonValidator? validator = null)
        {
            _validator = validator ?? new DocsJsonValidator();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocsJsonManager"/> class with the specified file path.
        /// </summary>
        /// <param name="filePath">The path to the docs.json file to load.</param>
        /// <param name="validator">The validator to use for validating configurations. If null, a new instance will be created.</param>
        /// <exception cref="ArgumentException">Thrown when the file path does not exist or is not a JSON file.</exception>
        public DocsJsonManager(string filePath, DocsJsonValidator? validator = null) : this(validator)
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
        /// Loads the specified configuration and applies validation and cleaning steps.
        /// </summary>
        /// <remarks>This method replaces any existing configuration and clears previous configuration
        /// errors. After loading, the configuration is validated and navigation groups are cleaned to ensure
        /// consistency.</remarks>
        /// <param name="config">The configuration object to load. Cannot be null.</param>
        public void Load(DocsJsonConfig config)
        {
            Ensure.ArgumentNotNull(config, nameof(config));

            ConfigurationErrors.Clear();
            Configuration = config;

            // Populate known page paths from the loaded configuration
            // Allow duplicates in loaded data (assume user intent), but track them for future additions
            if (Configuration.Navigation?.Pages is not null)
            {
                PopulateKnownPaths(Configuration.Navigation.Pages);
            }
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
                        new GroupConfig
                        {
                            Group = "Getting Started",
                            Pages = ["index", "quickstart"]
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
        /// <param name="name">The name to use for the documentation site if not already set.</param>
        /// <param name="theme">The theme to use if not already set (defaults to "mint").</param>
        /// <exception cref="InvalidOperationException">Thrown when no configuration is loaded.</exception>
        public void ApplyDefaults(string? name = null, string theme = "mint")
        {
            if (Configuration is null)
            {
                throw new InvalidOperationException("No configuration is loaded. Load a configuration before applying defaults.");
            }

            // Create defaults once and merge missing properties
            var defaults = CreateDefault(
                string.IsNullOrWhiteSpace(Configuration.Name) ? (name ?? "API Documentation") : Configuration.Name,
                string.IsNullOrWhiteSpace(Configuration.Theme) || Configuration.Theme == "mint" ? theme : Configuration.Theme
            );

            // Apply missing properties from defaults
            if (string.IsNullOrWhiteSpace(Configuration.Name))
                Configuration.Name = defaults.Name;
            if (string.IsNullOrWhiteSpace(Configuration.Theme) || Configuration.Theme == "mint")
                Configuration.Theme = defaults.Theme;
            Configuration.Schema ??= defaults.Schema;

            // Apply navigation defaults if missing or empty
            if (Configuration.Navigation is null || (Configuration.Navigation.Pages is null || Configuration.Navigation.Pages.Count == 0))
            {
                Configuration.Navigation = defaults.Navigation;

                // Add discovered navigation paths to _knownPagePaths
                if (Configuration.Navigation?.Pages is not null)
                {
                    PopulateKnownPaths(Configuration.Navigation.Pages);
                }
            }

            // Apply colors defaults if missing or using default values
            if (Configuration.Colors is null || Configuration.Colors.Primary == "#000000")
            {
                Configuration.Colors = defaults.Colors;
            }
        }

        /// <summary>
        /// Merges navigation from another NavigationConfig into the current configuration's navigation.
        /// </summary>
        /// <param name="sourceNavigation">The navigation configuration to merge from.</param>
        /// <param name="options">Optional merge options to control merge behavior.</param>
        /// <exception cref="ArgumentNullException">Thrown when sourceNavigation is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no configuration is loaded.</exception>
        /// <remarks>
        /// This method intelligently merges navigation structures, combining groups with the same name
        /// and deduplicating page references. Use the MergeOptions parameter to control specific
        /// behaviors like how empty groups are handled.
        /// </remarks>
        public void MergeNavigation(NavigationConfig sourceNavigation, MergeOptions? options = null)
        {
            if (sourceNavigation is null)
                return;

            if (Configuration is null)
            {
                throw new InvalidOperationException("No configuration is loaded. Load a configuration before merging navigation.");
            }

            Configuration.Navigation ??= new NavigationConfig();
            MergeNavigation(Configuration.Navigation, sourceNavigation, options);
        }

        /// <summary>
        /// Merges navigation from an existing docs.json file into the current configuration.
        /// </summary>
        /// <param name="filePath">The path to the docs.json file containing navigation to merge.</param>
        /// <param name="options">Optional merge options to control merge behavior.</param>
        /// <exception cref="ArgumentException">Thrown when filePath is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no configuration is loaded.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
        /// <remarks>
        /// This method loads navigation from an external docs.json file and merges it into the
        /// current configuration. Only the navigation structure is merged; other configuration
        /// properties are not affected.
        /// </remarks>
        public void MergeNavigation(string filePath, MergeOptions? options = null)
        {
            Ensure.ArgumentNotNullOrWhiteSpace(filePath, nameof(filePath));

            if (Configuration is null)
            {
                throw new InvalidOperationException("No configuration is loaded. Load a configuration before merging navigation.");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The specified docs.json file does not exist: {filePath}", filePath);
            }

            var content = File.ReadAllText(filePath);
            var otherConfig = JsonSerializer.Deserialize<DocsJsonConfig>(content, MintlifyConstants.JsonSerializerOptions);
            
            if (otherConfig?.Navigation is not null)
            {
                MergeNavigation(otherConfig.Navigation, options);
            }
        }

        /// <summary>
        /// Populates the navigation structure from a directory path by scanning for MDX files.
        /// </summary>
        /// <param name="path">The directory path to scan for documentation files.</param>
        /// <param name="fileExtensions">The file extensions to include (defaults to .mdx only).</param>
        /// <param name="includeApiReference">Whether to include the 'api-reference' directory in discovery (defaults to false).</param>
        /// <param name="preserveExisting">Whether to preserve existing navigation structure and merge discovered content (defaults to true).</param>
        /// <param name="allowDuplicatePaths">If true, allows adding duplicate paths; otherwise, skips duplicates.</param>
        /// <param name="excludeDirectories">Optional array of directory names to exclude from navigation discovery (e.g., DocumentationReference output directories).</param>
        /// <exception cref="ArgumentException">Thrown when path is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no configuration is loaded.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the specified path does not exist.</exception>
        /// <remarks>
        /// <para>This method scans the specified directory recursively and builds a navigation structure
        /// based on the folder hierarchy and MDX files found. The method includes several advanced features:</para>
        /// <list type="bullet">
        /// <item><description><strong>Template Preservation:</strong> When preserveExisting is true (default),
        /// preserves existing navigation structure (like templates) and intelligently merges discovered content.</description></item>
        /// <item><description><strong>Root File Grouping:</strong> Files in the root directory are automatically
        /// grouped under "Getting Started" and placed at the top of the navigation.</description></item>
        /// <item><description><strong>Directory Exclusions:</strong> Automatically excludes 'node_modules', 'conceptual',
        /// 'overrides', 'api-reference' (unless includeApiReference is true), and any directories starting with '.' (dot directories).</description></item>
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
        public void PopulateNavigationFromPath(string path, string[]? fileExtensions = null, bool includeApiReference = false, bool preserveExisting = true, bool allowDuplicatePaths = false, string[]? excludeDirectories = null)
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

            if (preserveExisting)
            {
                // Create a temporary configuration to hold discovered navigation
                var discoveredNavigation = new NavigationConfig { Pages = [] };

                // Populate the discovered navigation from directory structure
                PopulateNavigationFromDirectory(path, discoveredNavigation.Pages, path, fileExtensions, includeApiReference, true, allowDuplicatePaths, excludeDirectories);

                // Merge discovered navigation into existing configuration, adding new root pages to Getting Started
                var mergeOptions = new MergeOptions();
                MergeNavigation(Configuration.Navigation, discoveredNavigation, mergeOptions);
            }
            else
            {
                // Clear existing pages to repopulate (legacy behavior)
                Configuration.Navigation.Pages.Clear();

                // Populate from directory structure
                PopulateNavigationFromDirectory(path, Configuration.Navigation.Pages, path, fileExtensions, includeApiReference, true, allowDuplicatePaths, excludeDirectories);
            }
        }

        /// <summary>
        /// Adds a page to the navigation structure if it doesn't already exist.
        /// </summary>
        /// <param name="pages">The pages collection to add to.</param>
        /// <param name="pagePath">The page path to add.</param>
        /// <param name="allowDuplicatePaths">If true, allows adding duplicate paths; otherwise, skips if the path already exists.</param>
        /// <param name="updateKnownPaths">If true, updates the known page paths tracking; otherwise, only adds to the pages collection.</param>
        /// <returns>True if the page was added; false if it already existed and duplicates are not allowed.</returns>
        public bool AddPage(List<object> pages, string pagePath, bool allowDuplicatePaths = false, bool updateKnownPaths = true)
        {
            Ensure.ArgumentNotNull(pages, nameof(pages));
            Ensure.ArgumentNotNullOrWhiteSpace(pagePath, nameof(pagePath));

            // Check if page already exists in known paths
            if (!allowDuplicatePaths && _knownPagePaths.Contains(pagePath))
            {
                return false;
            }

            // Add to pages collection
            pages.Add(pagePath);
            // Track in known paths if requested
            if (updateKnownPaths)
            {
                _knownPagePaths.Add(pagePath);
            }
            return true;
        }

        /// <summary>
        /// Adds a page to a group if it doesn't already exist.
        /// </summary>
        /// <param name="group">The group to add the page to.</param>
        /// <param name="pagePath">The page path to add.</param>
        /// <param name="allowDuplicatePaths">If true, allows adding duplicate paths; otherwise, skips if the path already exists.</param>
        /// <param name="updateKnownPaths">If true, updates the known page paths tracking; otherwise, only adds to the pages collection.</param>
        /// <returns>True if the page was added; false if it already existed and duplicates are not allowed.</returns>
        public bool AddPageToGroup(GroupConfig group, string pagePath, bool allowDuplicatePaths = false, bool updateKnownPaths = true)
        {
            Ensure.ArgumentNotNull(group, nameof(group));
            Ensure.ArgumentNotNullOrWhiteSpace(pagePath, nameof(pagePath));

            // Check if page already exists in known paths
            if (!allowDuplicatePaths && _knownPagePaths.Contains(pagePath))
            {
                return false;
            }

            // Initialize pages collection if needed
            group.Pages ??= [];

            // Add to group pages
            group.Pages.Add(pagePath);
            // Track in known paths if requested
            if (updateKnownPaths)
            {
                _knownPagePaths.Add(pagePath);
            }
            return true;
        }

        /// <summary>
        /// Adds a page to a hierarchical group path (slash-separated) in the navigation structure.
        /// </summary>
        /// <param name="groupPath">The hierarchical group path (e.g., "Getting Started/API Reference").</param>
        /// <param name="pagePath">The page path to add.</param>
        /// <param name="allowDuplicatePaths">If true, allows adding duplicate paths; otherwise, skips if the path already exists.</param>
        /// <returns>True if the page was added; false if it already existed and duplicates are not allowed.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no configuration is loaded.</exception>
        public bool AddPage(string groupPath, string pagePath, bool allowDuplicatePaths = false)
        {
            if (Configuration is null)
            {
                throw new InvalidOperationException("No configuration is loaded. Load a configuration before adding pages.");
            }

            Configuration.Navigation ??= new NavigationConfig();
            Configuration.Navigation.Pages ??= [];

            var targetPages = Configuration.Navigation.Pages;
            if (!string.IsNullOrWhiteSpace(groupPath))
            {
                var groups = groupPath.Split('/');
                foreach (var groupName in groups)
                {
                    var group = FindOrCreateGroup(targetPages, groupName);
                    targetPages = group.Pages ??= [];
                }
            }

            return AddPage(targetPages, pagePath, allowDuplicatePaths);
        }

        /// <summary>
        /// Checks if a page path is already known (tracked for duplicate prevention).
        /// </summary>
        /// <param name="pagePath">The page path to check.</param>
        /// <returns>True if the path is already known; otherwise, false.</returns>
        public bool IsPathKnown(string pagePath) => _knownPagePaths.Contains(pagePath);

        /// <summary>
        /// Finds or creates a group with the specified name in the pages collection.
        /// </summary>
        /// <param name="pages">The pages collection to search/add to.</param>
        /// <param name="groupName">The name of the group to find or create.</param>
        /// <returns>The existing or newly created group.</returns>
        public GroupConfig FindOrCreateGroup(List<object> pages, string groupName)
        {
            Ensure.ArgumentNotNull(pages, nameof(pages));
            Ensure.ArgumentNotNullOrWhiteSpace(groupName, nameof(groupName));

            // Look for existing group
            var existingGroup = pages.OfType<GroupConfig>().FirstOrDefault(g => g.Group == groupName);
            if (existingGroup is not null)
            {
                return existingGroup;
            }

            // Create new group
            var newGroup = new GroupConfig
            {
                Group = groupName,
                Pages = []
            };
            pages.Add(newGroup);
            return newGroup;
        }

        /// <summary>
        /// Adds a navigation item (page or group) to the specified collection, tracking paths appropriately.
        /// </summary>
        /// <param name="pages">The pages collection to add to.</param>
        /// <param name="item">The item to add (string page path or GroupConfig).</param>
        /// <returns>True if the item was added; false if it was skipped (duplicate).</returns>
        public bool AddNavigationItem(List<object> pages, object item)
        {
            Ensure.ArgumentNotNull(pages, nameof(pages));
            Ensure.ArgumentNotNull(item, nameof(item));

            switch (item)
            {
                case string pagePath:
                    return AddPage(pages, pagePath);

                case GroupConfig group:
                    // For groups, we need to check if a group with the same name exists
                    var existingGroup = pages.OfType<GroupConfig>().FirstOrDefault(g => g.Group == group.Group);
                    if (existingGroup is not null)
                    {
                        // Merge into existing group
                        MergeGroupConfig(existingGroup, group);
                        return false; // Didn't add a new group, merged into existing
                    }
                    else
                    {
                        // Add new group and track all its pages
                        pages.Add(group);
                        if (group.Pages is not null)
                        {
                            PopulateKnownPaths(group.Pages);
                        }
                        return true;
                    }

                default:
                    // Unknown type, add as-is (shouldn't happen in practice)
                    pages.Add(item);
                    return true;
            }
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

        #region Private Methods

        /// <summary>
        /// Recursively populates the _knownPagePaths hashset from a navigation structure.
        /// </summary>
        /// <param name="pages">The pages list to extract page paths from.</param>
        internal void PopulateKnownPaths(List<object> pages)
        {
            foreach (var page in pages)
            {
                switch (page)
                {
                    case string stringPage:
                        _knownPagePaths.Add(stringPage);
                        break;
                    case GroupConfig groupConfig when groupConfig.Pages is not null:
                        PopulateKnownPaths(groupConfig.Pages);
                        break;
                }
            }
        }

        // Removed RefreshKnownPagePaths - we now maintain _knownPagePaths incrementally
        // through AddPage, AddPageToGroup, and AddNavigationItem methods

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

                // Remove null groups that would cause Mintlify to reject the configuration
                RemoveNullGroups();

                // Apply validation using injected DocsJsonValidator
                var validationErrors = _validator.Validate(Configuration);
                foreach (var error in validationErrors)
                {
                    ConfigurationErrors.Add(new CompilerError(FilePath ?? "string", 0, 0, "VALIDATION", error)
                    {
                        IsWarning = true
                    });
                }
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
        /// Removes groups with null names from the navigation structure.
        /// Groups with null names will cause Mintlify to reject the configuration.
        /// </summary>
        internal void RemoveNullGroups()
        {
            if (Configuration?.Navigation?.Pages is null)
                return;

            var cleanedPages = new List<object>();

            foreach (var page in Configuration.Navigation.Pages)
            {
                if (page is GroupConfig group)
                {
                    if (group.Group is null)
                    {
                        // Skip groups with null names - these would cause Mintlify to reject the config
                        continue;
                    }

                    // Recursively clean nested groups if present
                    if (group.Pages is not null)
                    {
                        RemoveNullGroupsFromPages(group.Pages);
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

            // DO NOT refresh known page paths - we maintain them incrementally

            // Also clean Groups property if present
            if (Configuration.Navigation.Groups is not null)
            {
                var cleanedGroups = new List<GroupConfig>();
                foreach (var group in Configuration.Navigation.Groups)
                {
                    if (group.Group is null)
                    {
                        // Skip groups with null names
                        continue;
                    }
                    cleanedGroups.Add(group);
                }
                Configuration.Navigation.Groups = cleanedGroups;
            }
        }

        /// <summary>
        /// Recursively removes groups with null names from a pages list.
        /// </summary>
        /// <param name="pages">The pages list to clean.</param>
        internal void RemoveNullGroupsFromPages(List<object> pages)
        {
            var cleanedPages = new List<object>();
            foreach (var page in pages)
            {
                if (page is GroupConfig group)
                {
                    if (group.Group is null)
                    {
                        // Skip groups with null names
                        continue;
                    }

                    // Recursively clean further nested groups
                    if (group.Pages is not null)
                    {
                        RemoveNullGroupsFromPages(group.Pages);
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
        /// Merges navigation configurations intelligently.
        /// </summary>
        /// <param name="target">The target navigation configuration to merge into.</param>
        /// <param name="source">The source navigation configuration to merge from.</param>
        /// <param name="options">Optional merge options to control merge behavior. When null, default behavior is used.</param>
        internal void MergeNavigation(NavigationConfig target, NavigationConfig source, MergeOptions? options = null)
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
                    MergePagesList(source.Pages, target.Pages, options);
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
                    MergeGroupsList(source.Groups, target.Groups, options);
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
                    MergeTabsList(source.Tabs, target.Tabs, options);
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
        /// If targetPages is null, uses the loaded configuration's navigation pages and updates _knownPagePaths.
        /// </summary>
        /// <param name="sourcePages">The source pages list to merge from.</param>
        /// <param name="targetPages">The target pages list to merge into. If null, uses Configuration.Navigation.Pages.</param>
        /// <param name="options">Optional merge options to control merge behavior. When null, default behavior is used.</param>
        internal void MergePagesList(List<object> sourcePages, List<object>? targetPages = null, MergeOptions? options = null)
        {
            // Use loaded configuration if targetPages is null
            targetPages ??= Configuration?.Navigation?.Pages ?? throw new InvalidOperationException("No configuration is loaded and no target pages provided.");

            var processedPages = new List<object>();
            var seenPagePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var groupsByName = new Dictionary<string, GroupConfig>();

            // Determine if we should combine empty groups based on options
            // When options?.CombineEmptyGroups == true, empty groups are merged together
            // When options?.CombineEmptyGroups != true (default), empty groups remain separate (Mintlify behavior)
            var combineEmptyGroups = options?.CombineEmptyGroups ?? false;

            // First pass: process target pages, collecting all page paths and keeping order
            CollectPagePaths(targetPages, seenPagePaths);

            foreach (var page in targetPages)
            {
                switch (page)
                {
                    case string stringPage:
                        processedPages.Add(stringPage);
                        // Make sure it's tracked in _knownPagePaths
                        _knownPagePaths.Add(stringPage);
                        break;
                    case GroupConfig groupConfig when groupConfig.Group is null:
                        // Skip groups with null names - these are invalid and will be caught by validation
                        break;
                    case GroupConfig groupConfig when !string.IsNullOrWhiteSpace(groupConfig.Group):
                        if (!groupsByName.ContainsKey(groupConfig.Group))
                        {
                            groupsByName[groupConfig.Group] = groupConfig;
                            processedPages.Add(groupConfig);
                            // Track pages in this group
                            if (groupConfig.Pages is not null)
                            {
                                PopulateKnownPaths(groupConfig.Pages);
                            }
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
                            // Track pages in this group
                            if (groupConfig.Pages is not null)
                            {
                                PopulateKnownPaths(groupConfig.Pages);
                            }
                        }
                        break;
                    default:
                        processedPages.Add(page);
                        break;
                }
            }

            // Second pass: process source pages, merging groups and adding new items that don't already exist
            foreach (var page in sourcePages)
            {
                switch (page)
                {
                    case string stringPage:
                        if (!seenPagePaths.Contains(stringPage))
                        {
                            // Check if we should add this to "Getting Started" instead of root level
                            if (options?.AddRootPagesToGettingStarted == true && groupsByName.TryGetValue("Getting Started", out var gettingStartedGroup))
                            {
                                // Add to Getting Started group instead of root level
                                AddPageToGroup(gettingStartedGroup, stringPage);
                            }
                            else
                            {
                                // Use AddPage to properly track the page
                                if (AddPage(processedPages, stringPage))
                                {
                                    seenPagePaths.Add(stringPage);
                                }
                            }
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
        /// Collects all page paths from a navigation structure into a hashset.
        /// This includes both root-level string pages and pages within groups.
        /// </summary>
        /// <param name="pages">The pages list to collect paths from.</param>
        /// <param name="pagePaths">The hashset to add page paths to.</param>
        internal void CollectPagePaths(List<object> pages, HashSet<string> pagePaths)
        {
            foreach (var page in pages)
            {
                switch (page)
                {
                    case string stringPage:
                        pagePaths.Add(stringPage);
                        break;
                    case GroupConfig groupConfig when groupConfig.Pages is not null:
                        CollectPagePaths(groupConfig.Pages, pagePaths);
                        break;
                }
            }
        }

        /// <summary>
        /// Merges two GroupConfig objects with the same group name.
        /// </summary>
        /// <param name="target">The target group to merge into.</param>
        /// <param name="source">The source group to merge from.</param>
        /// <param name="options">Optional merge options to control merge behavior. When null, default behavior is used.</param>
        internal void MergeGroupConfig(GroupConfig target, GroupConfig source, MergeOptions? options = null)
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
                    MergePagesList(source.Pages, target.Pages, options);
                }
            }
        }

        /// <summary>
        /// Intelligently merges two groups lists, combining groups with the same name.
        /// If targetGroups is null, uses the loaded configuration's navigation groups and updates _knownPagePaths.
        /// </summary>
        /// <param name="sourceGroups">The source groups list to merge from.</param>
        /// <param name="targetGroups">The target groups list to merge into. If null, uses Configuration.Navigation.Groups.</param>
        /// <param name="options">Optional merge options to control merge behavior. When null, default behavior is used.</param>
        internal void MergeGroupsList(List<GroupConfig> sourceGroups, List<GroupConfig>? targetGroups = null, MergeOptions? options = null)
        {
            // Use loaded configuration if targetGroups is null
            targetGroups ??= Configuration?.Navigation?.Groups ?? throw new InvalidOperationException("No configuration is loaded and no target groups provided.");

            var groupsByName = new Dictionary<string, GroupConfig>();
            var groupsWithoutNames = new List<GroupConfig>();
            var targetGroupNames = new HashSet<string>();

            // First, catalog what's in the target
            foreach (var group in targetGroups)
            {
                if (!string.IsNullOrWhiteSpace(group.Group))
                {
                    groupsByName[group.Group] = group;
                    targetGroupNames.Add(group.Group);
                }
                else
                {
                    groupsWithoutNames.Add(group);
                }
            }

            // Process source groups and track new ones
            var newGroups = new List<GroupConfig>();
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
                        // This is a new group from source, add it to our tracking list
                        groupsByName[sourceGroup.Group] = sourceGroup;
                        newGroups.Add(sourceGroup);
                    }
                }
                else
                {
                    groupsWithoutNames.Add(sourceGroup);
                }
            }

            // Rebuild the groups list:
            // 1. Preserve target groups in original order
            // 2. Append new source groups in alphabetical order
            targetGroups.Clear();

            // Add existing target groups in their original order (preserved by dictionary insertion order)
            foreach (var group in groupsByName.Values)
            {
                if (targetGroupNames.Contains(group.Group))
                {
                    targetGroups.Add(group);
                }
            }

            // Add new groups in alphabetical order
            targetGroups.AddRange(newGroups.OrderBy(g => g.Group));

            // Add groups without names
            targetGroups.AddRange(groupsWithoutNames);
        }

        /// <summary>
        /// Intelligently merges two tabs lists, combining tabs with the same name or overlapping paths.
        /// </summary>
        /// <param name="sourceTabs">The source tabs list to merge from.</param>
        /// <param name="targetTabs">The target tabs list to merge into.</param>
        /// <param name="options">Optional merge options to control merge behavior. When null, default behavior is used.</param>
        internal void MergeTabsList(List<TabConfig> sourceTabs, List<TabConfig> targetTabs, MergeOptions? options = null)
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
        internal void MergeTabConfig(TabConfig target, TabConfig source, MergeOptions? options = null)
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
                    MergePagesList(source.Pages, target.Pages, options);
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
                    MergeGroupsList(source.Groups, target.Groups, options);
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
        /// <param name="includeApiReference">Whether to include the 'api-reference' directory in discovery (defaults to false).</param>
        /// <param name="groupRootFiles">Whether to group root-level files under "Getting Started" (defaults to false).</param>
        /// <param name="allowDuplicatePaths">If true, allows adding duplicate paths; otherwise, skips duplicates.</param>
        /// <param name="excludeDirectories">Optional array of directory names to exclude from navigation discovery (e.g., DocumentationReference output directories).</param>
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
        internal void PopulateNavigationFromDirectory(string currentPath, List<object> pages, string rootPath, string[] fileExtensions, bool includeApiReference = false, bool groupRootFiles = false, bool allowDuplicatePaths = false, string[]? excludeDirectories = null)
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
                        // Populate known page paths from the custom navigation group
                        if (customGroup.Pages is not null)
                        {
                            PopulateKnownPaths(customGroup.Pages);
                        }
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
                // Skip hidden directories, excluded directories, and optionally api-reference
                if (!dirName.StartsWith(".") &&
#if NETSTANDARD2_0
                    !ExcludedDirectories.Any(excluded => string.Equals(excluded, dirName, StringComparison.OrdinalIgnoreCase)) &&
                    (excludeDirectories is null || !excludeDirectories.Any(excluded => string.Equals(excluded, dirName, StringComparison.OrdinalIgnoreCase))) &&
#else
                    !ExcludedDirectories.Contains(dirName, StringComparer.OrdinalIgnoreCase) &&
                    (excludeDirectories is null || !excludeDirectories.Contains(dirName, StringComparer.OrdinalIgnoreCase)) &&
#endif
                    (includeApiReference || !string.Equals(dirName, "api-reference", StringComparison.OrdinalIgnoreCase))
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
                    PopulateNavigationFromDirectory(path, subPages, rootPath, fileExtensions, includeApiReference, false, allowDuplicatePaths, excludeDirectories);

                    if (subPages.Any())
                    {
                        // Check if the first item is already a GroupConfig (from navigation.json override)
                        if (subPages.Count == 1 && subPages[0] is GroupConfig existingGroup)
                        {
                            pages.Add(existingGroup);
                        }
                        else
                        {
                            var groupName = FormatGroupName(name);

                            // Avoid conflicts with the root files "Getting Started" group by using a different name
                            if (groupName == "Getting Started" && groupRootFiles)
                            {
                                groupName = $"Getting Started ({name})";
                            }

                            var group = new GroupConfig
                            {
                                Group = groupName,
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

                    // If we're processing root files and groupRootFiles is true, group them
                    if (groupRootFiles && currentPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
                    {
                        // Find or create "Getting Started" group
                        var gettingStartedGroup = FindOrCreateGroup(pages, "Getting Started");

                        // Insert at beginning to keep it at top if it's newly created
                        if (pages[pages.Count - 1] == gettingStartedGroup && pages.Count > 1)
                        {
                            pages.RemoveAt(pages.Count - 1);
                            pages.Insert(0, gettingStartedGroup);
                        }

                        // Use AddPageToGroup which handles duplicate checking
                        AddPageToGroup(gettingStartedGroup, url, allowDuplicatePaths, updateKnownPaths: false);
                    }
                    else
                    {
                        // Use AddPage which handles duplicate checking and path tracking
                        AddPage(pages, url, allowDuplicatePaths, updateKnownPaths: false);
                    }
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
        internal void ApplyUrlPrefixToPages(List<object> pages, string prefix)
        {
            for (int i = 0; i < pages.Count; i++)
            {
                switch (pages[i])
                {
                    case string pageUrl:
                        var newUrl = $"{prefix}/{pageUrl}";
                        // Update _knownPagePaths: remove old, add new
                        _knownPagePaths.Remove(pageUrl);
                        _knownPagePaths.Add(newUrl);
                        pages[i] = newUrl;
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
        internal void ApplyUrlPrefixToGroup(GroupConfig group, string prefix)
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
        internal void ApplyUrlPrefixToTab(TabConfig tab, string prefix)
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
        internal void ApplyUrlPrefixToAnchor(AnchorConfig anchor, string prefix)
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
