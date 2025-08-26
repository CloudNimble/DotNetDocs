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

        private static readonly string[] ValidThemes = { "mint", "maple", "palm", "willow", "linden", "almond", "aspen" };
        private static readonly string[] ValidAppearanceModes = { "system", "light", "dark" };
        private static readonly string[] ValidIconLibraries = { "fontawesome", "lucide" };
        private static readonly string[] ValidSeoIndexing = { "navigable", "all" };
        private static readonly string[] ValidLanguages = { "en", "cn", "zh", "zh-Hans", "zh-Hant", "es", "fr", "fr-CA", "ja", "jp", "pt", "pt-BR", "de", "ko", "it", "ru", "id", "ar", "tr", "hi" };
        private static readonly string[] ValidApiAuthMethods = { "bearer", "basic", "key", "cobo" };
        private static readonly string[] ValidApiPlaygroundDisplayModes = { "interactive", "simple", "none" };
        private static readonly string[] ValidApiExamplesDefaults = { "all", "required" };
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
            ValidateColors(config, errors);
            ValidateLogo(config, errors);
            ValidateNavigation(config, errors);
            ValidateGroups(config, errors);
            ValidateAppearance(config, errors);
            ValidateIcons(config, errors);
            ValidateSeo(config, errors);
            ValidateApi(config, errors);

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
            if (!string.IsNullOrWhiteSpace(config.Theme) && !ValidThemes.Contains(config.Theme, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"Invalid theme '{config.Theme}'. Valid themes are: {string.Join(", ", ValidThemes)}");
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

            if (!string.IsNullOrWhiteSpace(config.Appearance.Default) && !ValidAppearanceModes.Contains(config.Appearance.Default, StringComparer.OrdinalIgnoreCase))
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

            if (!string.IsNullOrWhiteSpace(config.Icons.Library) && !ValidIconLibraries.Contains(config.Icons.Library, StringComparer.OrdinalIgnoreCase))
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

            if (!string.IsNullOrWhiteSpace(config.Seo.Indexing) && !ValidSeoIndexing.Contains(config.Seo.Indexing, StringComparer.OrdinalIgnoreCase))
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
                        !ValidApiAuthMethods.Contains(config.Api.Mdx.Auth.Method, StringComparer.OrdinalIgnoreCase))
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
                    !ValidApiPlaygroundDisplayModes.Contains(config.Api.Playground.Display, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"Invalid API playground display mode '{config.Api.Playground.Display}'. Valid modes are: {string.Join(", ", ValidApiPlaygroundDisplayModes)}");
                }
            }

            // Validate Examples configuration
            if (config.Api.Examples is not null)
            {
                if (!string.IsNullOrWhiteSpace(config.Api.Examples.Defaults) &&
                    !ValidApiExamplesDefaults.Contains(config.Api.Examples.Defaults, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"Invalid API examples defaults '{config.Api.Examples.Defaults}'. Valid values are: {string.Join(", ", ValidApiExamplesDefaults)}");
                }
            }
        }

#if NET8_0_OR_GREATER
        [GeneratedRegex(@"^#([a-fA-F0-9]{6}|[a-fA-F0-9]{3})$", RegexOptions.Compiled)]
        private static partial Regex MyRegex();
#endif

        #endregion

    }

}
