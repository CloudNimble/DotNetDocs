using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CloudNimble.EasyAF.Core;
using Mintlify.Core.Models;

namespace Mintlify.Core
{

    /// <summary>
    /// Validates Mintlify docs.json configuration against the official schema requirements.
    /// </summary>
    /// <remarks>
    /// This class provides comprehensive validation of the docs.json configuration to ensure
    /// it complies with the Mintlify schema and will work correctly when deployed.
    /// </remarks>
#if NET8_0_OR_GREATER
    public partial class DocsJsonValidator
#else
    public class DocsJsonValidator
#endif
    {

        #region Private Fields

        private static readonly string[] ValidThemes = { "mint", "maple", "palm", "willow", "linden", "almond", "aspen", "sequoia", "luma" };
        private static readonly string[] ValidAppearanceModes = { "system", "light", "dark" };
        private static readonly string[] ValidIconLibraries = { "fontawesome", "lucide", "tabler" };
        private static readonly string[] ValidSeoIndexing = { "navigable", "all" };
        private static readonly string[] ValidLanguages = { "en", "cn", "zh", "zh-Hans", "zh-Hant", "zh-CN", "zh-TW", "es", "fr", "fr-CA", "fr-ca", "ja", "jp", "ja-jp", "ja-JP", "pt", "pt-BR", "de", "ko", "it", "ru", "ro", "cs", "id", "ar", "tr", "hi", "sv", "no", "da", "lv", "nl", "uk", "vi", "pl", "uz", "he", "ca", "fi", "hu" };
        private static readonly string[] ValidApiAuthMethods = { "bearer", "basic", "key", "cobo" };
        private static readonly string[] ValidApiPlaygroundDisplayModes = { "interactive", "simple", "none", "auth" };
        private static readonly string[] ValidApiExamplesDefaults = { "all", "required" };
        private static readonly string[] ValidApiParamsExpanded = { "all", "closed" };
        private static readonly string[] ValidApiOptional = { "static", "expanded", "collapsed" };
        private static readonly string[] ValidBannerTypes = { "info", "warning", "critical" };
        private static readonly string[] ValidThumbnailPresets = { "classic", "headline", "minimal" };
        private static readonly string[] ValidThumbnailSurfaces = { "neutral", "brand" };
        private static readonly string[] ValidMixpanelRegions = { "us", "eu", "in" };
        private static readonly string[] ValidDirectoryLayouts = { "none", "accordion", "card" };
        private static readonly string[] ValidTabAlignments = { "start", "end" };
        private static readonly string[] ValidSdkFormats = { "typedoc", "docfx", "javadoc", "sphinx", "phpdoc", "go" };
        private static readonly HashSet<string> ValidThemeSet = new HashSet<string>(ValidThemes, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ValidAppearanceModeSet = new HashSet<string>(ValidAppearanceModes, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ValidIconLibrarySet = new HashSet<string>(ValidIconLibraries, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ValidSeoIndexingSet = new HashSet<string>(ValidSeoIndexing, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ValidApiAuthMethodSet = new HashSet<string>(ValidApiAuthMethods, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ValidApiPlaygroundDisplayModeSet = new HashSet<string>(ValidApiPlaygroundDisplayModes, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ValidApiExamplesDefaultSet = new HashSet<string>(ValidApiExamplesDefaults, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ValidApiParamsExpandedSet = new HashSet<string>(ValidApiParamsExpanded, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ValidApiOptionalSet = new HashSet<string>(ValidApiOptional, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ValidBannerTypeSet = new HashSet<string>(ValidBannerTypes, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ValidThumbnailPresetSet = new HashSet<string>(ValidThumbnailPresets, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ValidThumbnailSurfaceSet = new HashSet<string>(ValidThumbnailSurfaces, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ValidMixpanelRegionSet = new HashSet<string>(ValidMixpanelRegions, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ValidDirectoryLayoutSet = new HashSet<string>(ValidDirectoryLayouts, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ValidTabAlignmentSet = new HashSet<string>(ValidTabAlignments, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ValidSdkFormatSet = new HashSet<string>(ValidSdkFormats, StringComparer.OrdinalIgnoreCase);
#if NET7_0_OR_GREATER
        private static readonly Regex HexColorRegex = MyRegex();
#else
        private static readonly Regex HexColorRegex = new Regex(@"^#([a-fA-F0-9]{6}|[a-fA-F0-9]{3})$", RegexOptions.Compiled);
#endif

        #endregion

        #region Public Methods

        /// <summary>
        /// Validates a docs.json configuration against the Mintlify schema.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <returns>A list of validation errors. Empty if configuration is valid.</returns>
        public List<string> Validate(DocsJsonConfig config)
        {
            Ensure.ArgumentNotNull(config, nameof(config));

            var errors = new List<string>();

            ValidateRequired(config, errors);
            ValidateTheme(config, errors);
            ValidateThemeVersion(config, errors);
            ValidateColors(config, errors);
            ValidateLogo(config, errors);
            ValidateNavigation(config, errors);
            ValidateGroups(config, errors);
            ValidateAppearance(config, errors);
            ValidateIcons(config, errors);
            ValidateSeo(config, errors);
            ValidateApi(config, errors);
            ValidateBanner(config, errors);
            ValidateThumbnails(config, errors);
            ValidateIntegrations(config, errors);
            ValidateSpecRefs(config, errors);
            ValidateDirectoryAndSdk(config, errors);

            return errors;
        }

        /// <summary>
        /// Validates that the configuration has all required properties.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="errors">The list to add errors to.</param>
        public void ValidateRequired(DocsJsonConfig config, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(config.Theme))
            {
                errors.Add("Theme is required.");
            }

            if (string.IsNullOrWhiteSpace(config.Name))
            {
                errors.Add("Name is required.");
            }

            if (config.Colors is null)
            {
                errors.Add("Colors configuration is required.");
            }

            if (config.Navigation is null)
            {
                errors.Add("Navigation configuration is required.");
            }
        }

        /// <summary>
        /// Validates the theme configuration.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="errors">The list to add errors to.</param>
        public void ValidateTheme(DocsJsonConfig config, List<string> errors)
        {
            if (!string.IsNullOrWhiteSpace(config.Theme) && !IsKnown(ValidThemeSet, config.Theme))
            {
                errors.Add($"Invalid theme '{config.Theme}'. Valid themes are: {string.Join(", ", ValidThemes)}");
            }
        }

        /// <summary>
        /// Validates the theme version configuration.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="errors">The list to add errors to.</param>
        public void ValidateThemeVersion(DocsJsonConfig config, List<string> errors)
        {
            if (!config.ThemeVersion.HasValue)
            {
                return;
            }

            if (config.ThemeVersion.Value < 1 || config.ThemeVersion.Value > 2)
            {
                errors.Add($"Invalid themeVersion '{config.ThemeVersion.Value}'. Valid values are 1 or 2.");
            }

            if (!string.IsNullOrWhiteSpace(config.Theme) &&
                !string.Equals(config.Theme, "mint", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("themeVersion is only supported when theme is 'mint'.");
            }
        }

        /// <summary>
        /// Validates the color configuration.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="errors">The list to add errors to.</param>
        public void ValidateColors(DocsJsonConfig config, List<string> errors)
        {
            if (config.Colors is null)
                return;

            if (string.IsNullOrWhiteSpace(config.Colors.Primary))
            {
                errors.Add("Primary color is required in colors configuration.");
            }
            else if (!HexColorRegex.IsMatch(config.Colors.Primary))
            {
                errors.Add($"Primary color '{config.Colors.Primary}' must be a valid hex color (e.g., #FF0000 or #F00).");
            }

            if (!string.IsNullOrWhiteSpace(config.Colors.Light) && !HexColorRegex.IsMatch(config.Colors.Light))
            {
                errors.Add($"Light color '{config.Colors.Light}' must be a valid hex color (e.g., #FF0000 or #F00).");
            }

            if (!string.IsNullOrWhiteSpace(config.Colors.Dark) && !HexColorRegex.IsMatch(config.Colors.Dark))
            {
                errors.Add($"Dark color '{config.Colors.Dark}' must be a valid hex color (e.g., #FF0000 or #F00).");
            }
        }

        /// <summary>
        /// Validates the logo configuration.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="errors">The list to add errors to.</param>
        public void ValidateLogo(DocsJsonConfig config, List<string> errors)
        {
            if (config.Logo is null)
                return;

            if (config.Logo.Light is not null && !string.IsNullOrWhiteSpace(config.Logo.Light) && config.Logo.Light.Length < 3)
            {
                errors.Add("Logo light path must be at least 3 characters long.");
            }

            if (config.Logo.Dark is not null && !string.IsNullOrWhiteSpace(config.Logo.Dark) && config.Logo.Dark.Length < 3)
            {
                errors.Add("Logo dark path must be at least 3 characters long.");
            }

            if (!string.IsNullOrWhiteSpace(config.Logo.Href))
            {
                try
                {
                    new Uri(config.Logo.Href, UriKind.RelativeOrAbsolute);
                }
                catch
                {
                    errors.Add($"Logo href '{config.Logo.Href}' is not a valid URL.");
                }
            }
        }

        /// <summary>
        /// Validates the navigation configuration.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="errors">The list to add errors to.</param>
        public void ValidateNavigation(DocsJsonConfig config, List<string> errors)
        {
            if (config.Navigation is null)
                return;

            var hasNavigationContent = false;

            if (config.Navigation.Pages?.Count > 0)
                hasNavigationContent = true;
            if (config.Navigation.Groups?.Count > 0)
                hasNavigationContent = true;
            if (config.Navigation.Anchors?.Count > 0)
                hasNavigationContent = true;
            if (config.Navigation.Tabs?.Count > 0)
                hasNavigationContent = true;
            if (config.Navigation.Dropdowns?.Count > 0)
                hasNavigationContent = true;
            if (config.Navigation.Languages?.Count > 0)
                hasNavigationContent = true;
            if (config.Navigation.Versions?.Count > 0)
                hasNavigationContent = true;

            if (!hasNavigationContent)
            {
                errors.Add("Navigation must contain at least one of: pages, groups, anchors, tabs, dropdowns, languages, or versions.");
            }
        }

        /// <summary>
        /// Validates group configurations in navigation.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="errors">The list to add errors to.</param>
        public void ValidateGroups(DocsJsonConfig config, List<string> errors)
        {
            if (config.Navigation?.Pages is null)
                return;

            foreach (var page in config.Navigation.Pages)
            {
                if (page is GroupConfig group)
                {
                    // Check for null group names (should not happen with [NotNull] but defensive)
                    if (group.Group is null)
                    {
                        errors.Add("Group name cannot be null. Mintlify will reject configurations with null group names.");
                    }
                    // Check for empty/whitespace group names
                    else if (string.IsNullOrWhiteSpace(group.Group))
                    {
                        errors.Add($"Warning: Empty group name found. Mintlify treats empty groups as separate ungrouped sections and does not merge them together.");
                    }

                    // Recursively validate nested groups
                    if (group.Pages is not null)
                    {
                        foreach (var nestedPage in group.Pages)
                        {
                            if (nestedPage is GroupConfig nestedGroup)
                            {
                                if (nestedGroup.Group is null)
                                {
                                    errors.Add("Nested group name cannot be null. Mintlify will reject configurations with null group names.");
                                }
                                else if (string.IsNullOrWhiteSpace(nestedGroup.Group))
                                {
                                    errors.Add($"Warning: Empty nested group name found in group '{group.Group}'. Mintlify treats empty groups as separate ungrouped sections.");
                                }
                            }
                        }
                    }
                }
            }

            // Also check Groups property if present
            if (config.Navigation.Groups is not null)
            {
                foreach (var group in config.Navigation.Groups)
                {
                    if (group.Group is null)
                    {
                        errors.Add("Group name cannot be null in Groups list. Mintlify will reject configurations with null group names.");
                    }
                    else if (string.IsNullOrWhiteSpace(group.Group))
                    {
                        errors.Add($"Warning: Empty group name found in Groups list. Mintlify treats empty groups as separate ungrouped sections.");
                    }
                }
            }
        }

        /// <summary>
        /// Validates the appearance configuration.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="errors">The list to add errors to.</param>
        public void ValidateAppearance(DocsJsonConfig config, List<string> errors)
        {
            if (config.Appearance is null)
                return;

            if (!string.IsNullOrWhiteSpace(config.Appearance.Default) && !IsKnown(ValidAppearanceModeSet, config.Appearance.Default))
            {
                errors.Add($"Invalid appearance default '{config.Appearance.Default}'. Valid modes are: {string.Join(", ", ValidAppearanceModes)}");
            }
        }

        /// <summary>
        /// Validates the icons configuration.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="errors">The list to add errors to.</param>
        public void ValidateIcons(DocsJsonConfig config, List<string> errors)
        {
            if (config.Icons is null)
                return;

            if (!string.IsNullOrWhiteSpace(config.Icons.Library) && !IsKnown(ValidIconLibrarySet, config.Icons.Library))
            {
                errors.Add($"Invalid icon library '{config.Icons.Library}'. Valid libraries are: {string.Join(", ", ValidIconLibraries)}");
            }
        }

        /// <summary>
        /// Validates the SEO configuration.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="errors">The list to add errors to.</param>
        public void ValidateSeo(DocsJsonConfig config, List<string> errors)
        {
            if (config.Seo is null)
                return;

            if (!string.IsNullOrWhiteSpace(config.Seo.Indexing) && !IsKnown(ValidSeoIndexingSet, config.Seo.Indexing))
            {
                errors.Add($"Invalid SEO indexing mode '{config.Seo.Indexing}'. Valid modes are: {string.Join(", ", ValidSeoIndexing)}");
            }
        }

        /// <summary>
        /// Validates the API configuration.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="errors">The list to add errors to.</param>
        public void ValidateApi(DocsJsonConfig config, List<string> errors)
        {
            if (config.Api is null)
                return;

            // Validate MDX configuration
            if (config.Api.Mdx is not null)
            {
                if (config.Api.Mdx.Auth is not null)
                {
                    if (!string.IsNullOrWhiteSpace(config.Api.Mdx.Auth.Method) &&
                        !IsKnown(ValidApiAuthMethodSet, config.Api.Mdx.Auth.Method))
                    {
                        errors.Add($"Invalid API MDX auth method '{config.Api.Mdx.Auth.Method}'. Valid methods are: {string.Join(", ", ValidApiAuthMethods)}");
                    }

                    if (config.Api.Mdx.Auth.Method == "key" && string.IsNullOrWhiteSpace(config.Api.Mdx.Auth.Name))
                    {
                        errors.Add("API MDX auth name is required when using 'key' authentication method.");
                    }
                }
            }

            // Validate Playground configuration
            if (config.Api.Playground is not null)
            {
                if (!string.IsNullOrWhiteSpace(config.Api.Playground.Display) &&
                    !IsKnown(ValidApiPlaygroundDisplayModeSet, config.Api.Playground.Display))
                {
                    errors.Add($"Invalid API playground display mode '{config.Api.Playground.Display}'. Valid modes are: {string.Join(", ", ValidApiPlaygroundDisplayModes)}");
                }
            }

            // Validate Examples configuration
            if (config.Api.Examples is not null)
            {
                if (!string.IsNullOrWhiteSpace(config.Api.Examples.Defaults) &&
                    !IsKnown(ValidApiExamplesDefaultSet, config.Api.Examples.Defaults))
                {
                    errors.Add($"Invalid API examples defaults '{config.Api.Examples.Defaults}'. Valid values are: {string.Join(", ", ValidApiExamplesDefaults)}");
                }
            }

            if (!string.IsNullOrWhiteSpace(config.Api.Optional) &&
                !IsKnown(ValidApiOptionalSet, config.Api.Optional))
            {
                errors.Add($"Invalid API optional layout '{config.Api.Optional}'. Valid values are: {string.Join(", ", ValidApiOptional)}");
            }

            var expanded = config.Api.Params?.Expanded;
            if (!string.IsNullOrWhiteSpace(expanded) &&
                !IsKnown(ValidApiParamsExpandedSet, expanded))
            {
                errors.Add($"Invalid API params expanded '{expanded}'. Valid values are: {string.Join(", ", ValidApiParamsExpanded)}");
            }
        }

        /// <summary>
        /// Validates the banner configuration.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="errors">The list to add errors to.</param>
        public void ValidateBanner(DocsJsonConfig config, List<string> errors)
        {
            var bannerType = config.Banner?.Type;
            if (string.IsNullOrWhiteSpace(bannerType))
            {
                return;
            }

            if (!IsKnown(ValidBannerTypeSet, bannerType))
            {
                errors.Add($"Invalid banner type '{bannerType}'. Valid types are: {string.Join(", ", ValidBannerTypes)}");
            }
        }

        /// <summary>
        /// Validates the thumbnails configuration.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="errors">The list to add errors to.</param>
        public void ValidateThumbnails(DocsJsonConfig config, List<string> errors)
        {
            if (config.Thumbnails is null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(config.Thumbnails.Preset) &&
                !IsKnown(ValidThumbnailPresetSet, config.Thumbnails.Preset))
            {
                errors.Add($"Invalid thumbnail preset '{config.Thumbnails.Preset}'. Valid presets are: {string.Join(", ", ValidThumbnailPresets)}");
            }

            if (!string.IsNullOrWhiteSpace(config.Thumbnails.Surface) &&
                !IsKnown(ValidThumbnailSurfaceSet, config.Thumbnails.Surface))
            {
                errors.Add($"Invalid thumbnail surface '{config.Thumbnails.Surface}'. Valid surfaces are: {string.Join(", ", ValidThumbnailSurfaces)}");
            }
        }

        /// <summary>
        /// Validates the integrations configuration.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="errors">The list to add errors to.</param>
        public void ValidateIntegrations(DocsJsonConfig config, List<string> errors)
        {
            if (config.Integrations is null)
            {
                return;
            }

            var mixpanelRegion = config.Integrations.Mixpanel?.Region;
            if (!string.IsNullOrWhiteSpace(mixpanelRegion) &&
                !IsKnown(ValidMixpanelRegionSet, mixpanelRegion))
            {
                errors.Add($"Invalid Mixpanel region '{mixpanelRegion}'. Valid regions are: {string.Join(", ", ValidMixpanelRegions)}");
            }

            if (config.Integrations.Transcend is not null &&
                string.IsNullOrWhiteSpace(config.Integrations.Transcend.BundleId))
            {
                errors.Add("Transcend bundleId is required when the Transcend integration is configured.");
            }

            if (config.Integrations.OneTrust is not null &&
                string.IsNullOrWhiteSpace(config.Integrations.OneTrust.DomainScript))
            {
                errors.Add("OneTrust domainScript is required when the OneTrust integration is configured.");
            }
        }

        /// <summary>
        /// Validates GraphQL and MCP specification references in navigation.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="errors">The list to add errors to.</param>
        public void ValidateSpecRefs(DocsJsonConfig config, List<string> errors)
        {
            if (config.Navigation is null)
            {
                return;
            }

            ValidateSpecRef(config.Navigation.Graphql, "navigation.graphql", errors);
            ValidateSpecRef(config.Navigation.Mcp, "navigation.mcp", errors);
            ValidateSpecRefsOnItems(config.Navigation.Pages, "navigation.pages", errors);
            ValidateSpecRefsOnGroups(config.Navigation.Groups, "navigation.groups", errors);
            ValidateSpecRefsOnTabs(config.Navigation.Tabs, "navigation.tabs", errors);
            ValidateSpecRefsOnAnchors(config.Navigation.Anchors, "navigation.anchors", errors);
            ValidateSpecRefsOnDropdowns(config.Navigation.Dropdowns, "navigation.dropdowns", errors);
            ValidateSpecRefsOnProducts(config.Navigation.Products, "navigation.products", errors);
        }

        /// <summary>
        /// Validates directory layout, tab alignment, and SDK refs.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="errors">The list to add errors to.</param>
        public void ValidateDirectoryAndSdk(DocsJsonConfig config, List<string> errors)
        {
            if (config.Navigation is null)
            {
                return;
            }

            ValidateDirectory(config.Navigation.Directory, "navigation.directory", errors);
            ValidateSdk(config.Navigation.Sdk, "navigation.sdk", errors);
            ValidateDirectoryAndSdkOnItems(config.Navigation.Pages, "navigation.pages", errors);
            ValidateDirectoryAndSdkOnGroups(config.Navigation.Groups, "navigation.groups", errors);
            ValidateDirectoryAndSdkOnTabs(config.Navigation.Tabs, "navigation.tabs", errors);
            ValidateDirectoryAndSdkOnAnchors(config.Navigation.Anchors, "navigation.anchors", errors);
            ValidateDirectoryAndSdkOnDropdowns(config.Navigation.Dropdowns, "navigation.dropdowns", errors);
            ValidateDirectoryAndSdkOnProducts(config.Navigation.Products, "navigation.products", errors);
            ValidateDirectoryAndSdkOnLanguages(config.Navigation.Languages, "navigation.languages", errors);
            ValidateDirectoryAndSdkOnVersions(config.Navigation.Versions, "navigation.versions", errors);
        }

        #endregion

        #region Private Methods

        private static bool IsKnown(HashSet<string> allowed, string? value)
        {
            return value is not null && allowed.Contains(value);
        }

        private static void ValidateSpecRef(SpecRefConfig? specRef, string path, List<string> errors)
        {
            if (specRef is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(specRef.Source))
            {
                errors.Add($"{path} requires a source path or URL.");
            }
        }

        private static void ValidateSpecRefsOnItem(NavigationItemBase item, string path, List<string> errors)
        {
            ValidateSpecRef(item.Graphql, $"{path}.graphql", errors);
            ValidateSpecRef(item.Mcp, $"{path}.mcp", errors);
        }

        private static void ValidateDirectory(string? directory, string path, List<string> errors)
        {
            if (!string.IsNullOrWhiteSpace(directory) &&
                !IsKnown(ValidDirectoryLayoutSet, directory))
            {
                errors.Add($"Invalid directory layout '{directory}' at {path}. Valid values are: {string.Join(", ", ValidDirectoryLayouts)}");
            }
        }

        private static void ValidateSdk(SdkRefConfig? sdk, string path, List<string> errors)
        {
            if (sdk is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sdk.Format) ||
                !IsKnown(ValidSdkFormatSet, sdk.Format))
            {
                errors.Add($"{path}.format must be one of: {string.Join(", ", ValidSdkFormats)}");
            }

            if (string.IsNullOrWhiteSpace(sdk.Source))
            {
                errors.Add($"{path}.source is required when sdk is configured.");
            }
        }

        private static void ValidateDirectoryAndSdkOnItem(NavigationItemBase item, string path, List<string> errors)
        {
            ValidateDirectory(item.Directory, $"{path}.directory", errors);
            ValidateSdk(item.Sdk, $"{path}.sdk", errors);
        }

        private static void ValidateDirectoryAndSdkOnItems(List<object>? pages, string path, List<string> errors)
        {
            if (pages is null)
            {
                return;
            }

            for (var i = 0; i < pages.Count; i++)
            {
                if (pages[i] is GroupConfig group)
                {
                    ValidateDirectoryAndSdkOnItem(group, $"{path}[{i}]", errors);
                    ValidateDirectoryAndSdkOnItems(group.Pages, $"{path}[{i}].pages", errors);
                }
            }
        }

        private static void ValidateDirectoryAndSdkOnGroups(List<GroupConfig>? groups, string path, List<string> errors)
        {
            if (groups is null)
            {
                return;
            }

            for (var i = 0; i < groups.Count; i++)
            {
                var groupPath = $"{path}[{i}]";
                ValidateDirectoryAndSdkOnItem(groups[i], groupPath, errors);
                ValidateDirectoryAndSdkOnItems(groups[i].Pages, $"{groupPath}.pages", errors);
            }
        }

        private static void ValidateDirectoryAndSdkOnTabs(List<TabConfig>? tabs, string path, List<string> errors)
        {
            if (tabs is null)
            {
                return;
            }

            for (var i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                var tabPath = $"{path}[{i}]";
                ValidateDirectoryAndSdkOnItem(tab, tabPath, errors);
                if (!string.IsNullOrWhiteSpace(tab.Align) &&
                    !IsKnown(ValidTabAlignmentSet, tab.Align))
                {
                    errors.Add($"Invalid tab align '{tab.Align}'. Valid values are: {string.Join(", ", ValidTabAlignments)}");
                }

                ValidateDirectoryAndSdkOnItems(tab.Pages, $"{tabPath}.pages", errors);
                ValidateDirectoryAndSdkOnGroups(tab.Groups, $"{tabPath}.groups", errors);
                ValidateDirectoryAndSdkOnAnchors(tab.Anchors, $"{tabPath}.anchors", errors);
                ValidateDirectoryAndSdkOnDropdowns(tab.Dropdowns, $"{tabPath}.dropdowns", errors);
                ValidateDirectoryAndSdkOnMenus(tab.Menu, $"{tabPath}.menu", errors);
                ValidateDirectoryAndSdkOnLanguages(tab.Languages, $"{tabPath}.languages", errors);
                ValidateDirectoryAndSdkOnVersions(tab.Versions, $"{tabPath}.versions", errors);
            }
        }

        private static void ValidateDirectoryAndSdkOnAnchors(List<AnchorConfig>? anchors, string path, List<string> errors)
        {
            if (anchors is null)
            {
                return;
            }

            for (var i = 0; i < anchors.Count; i++)
            {
                var anchorPath = $"{path}[{i}]";
                ValidateDirectoryAndSdkOnItem(anchors[i], anchorPath, errors);
                ValidateDirectoryAndSdkOnItems(anchors[i].Pages, $"{anchorPath}.pages", errors);
                ValidateDirectoryAndSdkOnGroups(anchors[i].Groups, $"{anchorPath}.groups", errors);
            }
        }

        private static void ValidateDirectoryAndSdkOnDropdowns(List<DropdownConfig>? dropdowns, string path, List<string> errors)
        {
            if (dropdowns is null)
            {
                return;
            }

            for (var i = 0; i < dropdowns.Count; i++)
            {
                var dropdownPath = $"{path}[{i}]";
                ValidateDirectoryAndSdkOnItem(dropdowns[i], dropdownPath, errors);
                ValidateDirectoryAndSdkOnItems(dropdowns[i].Pages, $"{dropdownPath}.pages", errors);
                ValidateDirectoryAndSdkOnGroups(dropdowns[i].Groups, $"{dropdownPath}.groups", errors);
            }
        }

        private static void ValidateDirectoryAndSdkOnProducts(List<ProductConfig>? products, string path, List<string> errors)
        {
            if (products is null)
            {
                return;
            }

            for (var i = 0; i < products.Count; i++)
            {
                var productPath = $"{path}[{i}]";
                ValidateDirectoryAndSdkOnItem(products[i], productPath, errors);
                ValidateDirectoryAndSdkOnItems(products[i].Pages, $"{productPath}.pages", errors);
                ValidateDirectoryAndSdkOnGroups(products[i].Groups, $"{productPath}.groups", errors);
            }
        }

        private static void ValidateDirectoryAndSdkOnLanguages(List<LanguageConfig>? languages, string path, List<string> errors)
        {
            if (languages is null)
            {
                return;
            }

            for (var i = 0; i < languages.Count; i++)
            {
                var language = languages[i];
                var languagePath = $"{path}[{i}]";
                ValidateDirectory(language.Directory, $"{languagePath}.directory", errors);
                ValidateSdk(language.Sdk, $"{languagePath}.sdk", errors);
                ValidateDirectoryAndSdkOnItems(language.Pages, $"{languagePath}.pages", errors);
                ValidateDirectoryAndSdkOnGroups(language.Groups, $"{languagePath}.groups", errors);
                ValidateDirectoryAndSdkOnTabs(language.Tabs, $"{languagePath}.tabs", errors);
                ValidateDirectoryAndSdkOnAnchors(language.Anchors, $"{languagePath}.anchors", errors);
                ValidateDirectoryAndSdkOnDropdowns(language.Dropdowns, $"{languagePath}.dropdowns", errors);
                ValidateDirectoryAndSdkOnVersions(language.Versions, $"{languagePath}.versions", errors);
            }
        }

        private static void ValidateDirectoryAndSdkOnVersions(List<VersionConfig>? versions, string path, List<string> errors)
        {
            if (versions is null)
            {
                return;
            }

            for (var i = 0; i < versions.Count; i++)
            {
                var version = versions[i];
                var versionPath = $"{path}[{i}]";
                ValidateDirectory(version.Directory, $"{versionPath}.directory", errors);
                ValidateSdk(version.Sdk, $"{versionPath}.sdk", errors);
                ValidateDirectoryAndSdkOnItems(version.Pages, $"{versionPath}.pages", errors);
                ValidateDirectoryAndSdkOnGroups(version.Groups, $"{versionPath}.groups", errors);
                ValidateDirectoryAndSdkOnTabs(version.Tabs, $"{versionPath}.tabs", errors);
                ValidateDirectoryAndSdkOnAnchors(version.Anchors, $"{versionPath}.anchors", errors);
                ValidateDirectoryAndSdkOnDropdowns(version.Dropdowns, $"{versionPath}.dropdowns", errors);
                ValidateDirectoryAndSdkOnLanguages(version.Languages, $"{versionPath}.languages", errors);
            }
        }

        private static void ValidateDirectoryAndSdkOnMenus(List<MenuConfig>? menus, string path, List<string> errors)
        {
            if (menus is null)
            {
                return;
            }

            for (var i = 0; i < menus.Count; i++)
            {
                var menu = menus[i];
                var menuPath = $"{path}[{i}]";
                ValidateDirectory(menu.Directory, $"{menuPath}.directory", errors);
                ValidateSdk(menu.Sdk, $"{menuPath}.sdk", errors);
                ValidateDirectoryAndSdkOnItems(menu.Pages, $"{menuPath}.pages", errors);
                ValidateDirectoryAndSdkOnGroups(menu.Groups, $"{menuPath}.groups", errors);
            }
        }

        private static void ValidateSpecRefsOnItems(List<object>? pages, string path, List<string> errors)
        {
            if (pages is null)
            {
                return;
            }

            for (var i = 0; i < pages.Count; i++)
            {
                if (pages[i] is GroupConfig group)
                {
                    ValidateSpecRefsOnGroup(group, $"{path}[{i}]", errors);
                }
            }
        }

        private static void ValidateSpecRefsOnGroup(GroupConfig group, string path, List<string> errors)
        {
            ValidateSpecRefsOnItem(group, path, errors);
            ValidateSpecRefsOnItems(group.Pages, $"{path}.pages", errors);
        }

        private static void ValidateSpecRefsOnGroups(List<GroupConfig>? groups, string path, List<string> errors)
        {
            if (groups is null)
            {
                return;
            }

            for (var i = 0; i < groups.Count; i++)
            {
                ValidateSpecRefsOnGroup(groups[i], $"{path}[{i}]", errors);
            }
        }

        private static void ValidateSpecRefsOnTabs(List<TabConfig>? tabs, string path, List<string> errors)
        {
            if (tabs is null)
            {
                return;
            }

            for (var i = 0; i < tabs.Count; i++)
            {
                var tabPath = $"{path}[{i}]";
                ValidateSpecRefsOnItem(tabs[i], tabPath, errors);
                ValidateSpecRefsOnItems(tabs[i].Pages, $"{tabPath}.pages", errors);
                ValidateSpecRefsOnGroups(tabs[i].Groups, $"{tabPath}.groups", errors);
            }
        }

        private static void ValidateSpecRefsOnAnchors(List<AnchorConfig>? anchors, string path, List<string> errors)
        {
            if (anchors is null)
            {
                return;
            }

            for (var i = 0; i < anchors.Count; i++)
            {
                var anchorPath = $"{path}[{i}]";
                ValidateSpecRefsOnItem(anchors[i], anchorPath, errors);
                ValidateSpecRefsOnItems(anchors[i].Pages, $"{anchorPath}.pages", errors);
            }
        }

        private static void ValidateSpecRefsOnDropdowns(List<DropdownConfig>? dropdowns, string path, List<string> errors)
        {
            if (dropdowns is null)
            {
                return;
            }

            for (var i = 0; i < dropdowns.Count; i++)
            {
                var dropdownPath = $"{path}[{i}]";
                ValidateSpecRefsOnItem(dropdowns[i], dropdownPath, errors);
                ValidateSpecRefsOnItems(dropdowns[i].Pages, $"{dropdownPath}.pages", errors);
            }
        }

        private static void ValidateSpecRefsOnProducts(List<ProductConfig>? products, string path, List<string> errors)
        {
            if (products is null)
            {
                return;
            }

            for (var i = 0; i < products.Count; i++)
            {
                var productPath = $"{path}[{i}]";
                ValidateSpecRefsOnItem(products[i], productPath, errors);
                ValidateSpecRefsOnItems(products[i].Pages, $"{productPath}.pages", errors);
            }
        }

#if NET8_0_OR_GREATER
        [GeneratedRegex(@"^#([a-fA-F0-9]{6}|[a-fA-F0-9]{3})$", RegexOptions.Compiled)]
        private static partial Regex MyRegex();
#endif

        #endregion

    }

}
