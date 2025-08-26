using System;
using System.Linq;

namespace Mintlify.Core
{

    /// <summary>
    /// Configuration options for Mintlify documentation generation.
    /// </summary>
    /// <remarks>
    /// This class contains all the configuration settings that control how the Mintlify
    /// documentation is generated, including output paths, filtering options, and formatting preferences.
    /// </remarks>
    public class MintlifyOptions
    {

        #region Properties

        /// <summary>
        /// Gets or sets the output directory for generated documentation.
        /// </summary>
        public string OutputDirectory { get; set; } = "./docs";

        /// <summary>
        /// Gets or sets whether to enable verbose output during generation.
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Gets or sets whether to include internal members in the documentation.
        /// </summary>
        public bool IncludeInternal { get; set; }

        /// <summary>
        /// Gets or sets the namespace filter regex pattern.
        /// Only namespaces matching this pattern will be included.
        /// </summary>
        public string? NamespaceFilter { get; set; }

        /// <summary>
        /// Gets or sets the type filter regex pattern.
        /// Only types matching this pattern will be included.
        /// </summary>
        public string? TypeFilter { get; set; }

        /// <summary>
        /// Gets or sets whether to generate a docs.json configuration file.
        /// </summary>
        public bool GenerateConfig { get; set; }

        /// <summary>
        /// Gets or sets whether to only generate the docs.json file without MDX files.
        /// </summary>
        public bool ConfigOnly { get; set; }

        /// <summary>
        /// Gets or sets whether to clean the output directory before generating.
        /// </summary>
        public bool Clean { get; set; }

        /// <summary>
        /// Gets or sets the base URL for cross-references to external documentation.
        /// </summary>
        public string BaseUrl { get; set; } = "https://learn.microsoft.com/dotnet/api/";

        /// <summary>
        /// Gets or sets the theme for the documentation site.
        /// </summary>
        public string Theme { get; set; } = "maple";

        /// <summary>
        /// Gets or sets the primary color for the documentation theme.
        /// </summary>
        public string PrimaryColor { get; set; } = "#0066CC";

        /// <summary>
        /// Gets or sets the primary color for dark mode.
        /// </summary>
        public string PrimaryDarkColor { get; set; } = "#0080FF";

        /// <summary>
        /// Gets or sets the name of the documentation site.
        /// </summary>
        public string SiteName { get; set; } = "API Documentation";

        /// <summary>
        /// Gets or sets the description of the documentation site.
        /// </summary>
        public string SiteDescription { get; set; } = "Complete API reference documentation";

        /// <summary>
        /// Gets or sets whether to include code examples in the generated documentation.
        /// </summary>
        public bool IncludeExamples { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include inheritance information.
        /// </summary>
        public bool IncludeInheritance { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include see also references.
        /// </summary>
        public bool IncludeSeeAlso { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum depth for nested type documentation.
        /// </summary>
        public int MaxDepth { get; set; } = 10;

        /// <summary>
        /// Gets or sets the solution name prefix to strip from project names when generating paths.
        /// </summary>
        /// <remarks>
        /// For example, if the solution is "CloudNimble.Breakdance" and projects are named
        /// "CloudNimble.Breakdance.AspNetCore", the output path will be "/aspnetcore/" instead
        /// of "/cloudnimble-breakdance-aspnetcore/".
        /// </remarks>
        public string? SolutionNamePrefix { get; set; }

        /// <summary>
        /// Gets or sets the path to the light logo file.
        /// </summary>
        public string LogoLight { get; set; } = "/logo/light.svg";

        /// <summary>
        /// Gets or sets the path to the dark logo file.
        /// </summary>
        public string LogoDark { get; set; } = "/logo/dark.svg";

        /// <summary>
        /// Gets or sets the URL to redirect to when clicking the logo.
        /// </summary>
        public string? LogoHref { get; set; }

        /// <summary>
        /// Gets or sets the favicon path or light mode favicon.
        /// </summary>
        public string FaviconLight { get; set; } = "/favicon.svg";

        /// <summary>
        /// Gets or sets the dark mode favicon path.
        /// </summary>
        public string? FaviconDark { get; set; }

        /// <summary>
        /// Gets or sets the default appearance mode (system, light, dark).
        /// </summary>
        public string AppearanceDefault { get; set; } = "system";

        /// <summary>
        /// Gets or sets whether to hide the light/dark mode toggle.
        /// </summary>
        public bool AppearanceStrict { get; set; }

        /// <summary>
        /// Gets or sets the icon library to use (fontawesome, lucide).
        /// </summary>
        public string IconLibrary { get; set; } = "fontawesome";

        /// <summary>
        /// Gets or sets the GitHub URL for footer social links.
        /// </summary>
        public string GitHubUrl { get; set; } = "https://github.com/CloudNimble/EasyAF";

        /// <summary>
        /// Gets or sets the website URL for footer social links.
        /// </summary>
        public string WebsiteUrl { get; set; } = "https://nimbleapps.cloud";

        /// <summary>
        /// Gets or sets the search prompt text.
        /// </summary>
        public string? SearchPrompt { get; set; }

        /// <summary>
        /// Gets or sets the SEO indexing mode (navigable, all).
        /// </summary>
        public string SeoIndexing { get; set; } = "navigable";

        /// <summary>
        /// Gets or sets whether to preserve existing docs.json configuration when updating.
        /// </summary>
        /// <remarks>
        /// When true (default), the generator will read any existing docs.json file and merge
        /// the generated API documentation with existing configuration, preserving custom
        /// navigation, styling, integrations, and other settings. When false, the generator
        /// will completely replace the docs.json file with new configuration.
        /// </remarks>
        public bool PreserveExistingConfig { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to force a rebuild instead of using existing binaries.
        /// </summary>
        /// <remarks>
        /// When false (default), DocFX will attempt to use existing compiled assemblies 
        /// for faster documentation generation. When true, forces a complete rebuild 
        /// which may be slower but ensures all dependencies are up to date.
        /// </remarks>
        public bool ForceBuild { get; set; }

        /// <summary>
        /// Gets or sets whether to strip backticks around parameter and return value links.
        /// </summary>
        /// <remarks>
        /// When true (default), removes backticks around markdown links in parameter descriptions
        /// and return value documentation to ensure proper link rendering in Mintlify.
        /// This fixes the issue where links inside code formatting don't render correctly.
        /// </remarks>
        public bool StripBackticksAroundLinks { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to generate navigation.json files in non-api-reference folders.
        /// </summary>
        /// <remarks>
        /// When true, the generator will create navigation.json files in directories that don't
        /// already have them, preserving the auto-discovered navigation structure. This allows
        /// users to customize navigation later without losing their changes during regeneration.
        /// </remarks>
        public bool GenerateNavigationFiles { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Validates the options and throws an exception if any are invalid.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(OutputDirectory))
            {
                throw new ArgumentException("Output directory cannot be null or empty.", nameof(OutputDirectory));
            }

            if (MaxDepth < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(MaxDepth), "Max depth must be at least 1.");
            }

            if (!string.IsNullOrWhiteSpace(NamespaceFilter))
            {
                try
                {
                    System.Text.RegularExpressions.Regex.IsMatch("test", NamespaceFilter);
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException($"Invalid namespace filter regex: {ex.Message}", nameof(NamespaceFilter), ex);
                }
            }

            if (!string.IsNullOrWhiteSpace(TypeFilter))
            {
                try
                {
                    System.Text.RegularExpressions.Regex.IsMatch("test", TypeFilter);
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException($"Invalid type filter regex: {ex.Message}", nameof(TypeFilter), ex);
                }
            }

            var validThemes = new[] { "mint", "maple", "palm", "willow", "linden", "almond", "aspen" };
            if (!string.IsNullOrWhiteSpace(Theme) && !validThemes.Contains(Theme, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid theme '{Theme}'. Valid themes are: {string.Join(", ", validThemes)}", nameof(Theme));
            }

            var validAppearanceModes = new[] { "system", "light", "dark" };
            if (!string.IsNullOrWhiteSpace(AppearanceDefault) && !validAppearanceModes.Contains(AppearanceDefault, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid appearance mode '{AppearanceDefault}'. Valid modes are: {string.Join(", ", validAppearanceModes)}", nameof(AppearanceDefault));
            }

            var validIconLibraries = new[] { "fontawesome", "lucide" };
            if (!string.IsNullOrWhiteSpace(IconLibrary) && !validIconLibraries.Contains(IconLibrary, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid icon library '{IconLibrary}'. Valid libraries are: {string.Join(", ", validIconLibraries)}", nameof(IconLibrary));
            }

            var validSeoIndexing = new[] { "navigable", "all" };
            if (!string.IsNullOrWhiteSpace(SeoIndexing) && !validSeoIndexing.Contains(SeoIndexing, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid SEO indexing mode '{SeoIndexing}'. Valid modes are: {string.Join(", ", validSeoIndexing)}", nameof(SeoIndexing));
            }
        }

        #endregion

    }

}