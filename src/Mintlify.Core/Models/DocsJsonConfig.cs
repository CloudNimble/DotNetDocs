using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the root configuration object for Mintlify docs.json.
    /// </summary>
    /// <remarks>
    /// This class represents the complete structure of a Mintlify docs.json configuration file
    /// as defined by the official Mintlify schema. It supports all themes and configuration options.
    /// </remarks>
    public class DocsJsonConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the API reference configuration.
        /// </summary>
        [JsonPropertyName("api")]
        public ApiConfig? Api { get; set; }

        /// <summary>
        /// Gets or sets the appearance configuration.
        /// </summary>
        [JsonPropertyName("appearance")]
        public AppearanceConfig? Appearance { get; set; }

        /// <summary>
        /// Gets or sets the background configuration.
        /// </summary>
        [JsonPropertyName("background")]
        public BackgroundConfig? Background { get; set; }

        /// <summary>
        /// Gets or sets the banner configuration.
        /// </summary>
        [JsonPropertyName("banner")]
        public BannerConfig? Banner { get; set; }

        /// <summary>
        /// Gets or sets the color configuration.
        /// </summary>
        /// <remarks>
        /// This is a required field. At minimum, the primary color must be defined.
        /// </remarks>
        [JsonPropertyName("colors")]
        [NotNull]
        public ColorsConfig Colors { get; set; } = new ColorsConfig();

        /// <summary>
        /// Gets or sets the contextual options configuration.
        /// </summary>
        [JsonPropertyName("contextual")]
        public ContextualConfig? Contextual { get; set; }

        /// <summary>
        /// Gets or sets the optional description used for SEO and LLM indexing.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the error pages configuration.
        /// </summary>
        [JsonPropertyName("errors")]
        public ErrorsConfig? Errors { get; set; }

        /// <summary>
        /// Gets or sets the favicon configuration.
        /// </summary>
        [JsonPropertyName("favicon")]
        public FaviconConfig? Favicon { get; set; }

        /// <summary>
        /// Gets or sets the fonts configuration.
        /// </summary>
        [JsonPropertyName("fonts")]
        public FontsConfig? Fonts { get; set; }

        /// <summary>
        /// Gets or sets the footer configuration.
        /// </summary>
        [JsonPropertyName("footer")]
        public FooterConfig? Footer { get; set; }

        /// <summary>
        /// Gets or sets the icons configuration.
        /// </summary>
        [JsonPropertyName("icons")]
        public IconsConfig? Icons { get; set; }

        /// <summary>
        /// Gets or sets the integrations configuration.
        /// </summary>
        [JsonPropertyName("integrations")]
        public IntegrationsConfig? Integrations { get; set; }

        /// <summary>
        /// Gets or sets the interaction configuration for navigation elements.
        /// </summary>
        /// <remarks>
        /// Controls how users interact with navigation elements such as groups and dropdowns,
        /// including whether expanding a group automatically navigates to its first page.
        /// </remarks>
        [JsonPropertyName("interaction")]
        public InteractionConfig? Interaction { get; set; }

        /// <summary>
        /// Gets or sets the logo configuration.
        /// </summary>
        [JsonPropertyName("logo")]
        public LogoConfig? Logo { get; set; }

        /// <summary>
        /// Gets or sets the name of the project, organization, or product.
        /// </summary>
        /// <remarks>
        /// This is a required field.
        /// </remarks>
        [JsonPropertyName("name")]
        [NotNull]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the navbar configuration.
        /// </summary>
        [JsonPropertyName("navbar")]
        public NavbarConfig? Navbar { get; set; }

        /// <summary>
        /// Gets or sets the navigation structure.
        /// </summary>
        /// <remarks>
        /// This is a required field that defines the structure of your documentation.
        /// </remarks>
        [JsonPropertyName("navigation")]
        [NotNull]
        public NavigationConfig Navigation { get; set; } = new NavigationConfig();

        /// <summary>
        /// Gets or sets the redirects.
        /// </summary>
        [JsonPropertyName("redirects")]
        public List<RedirectConfig>? Redirects { get; set; }

        /// <summary>
        /// Gets or sets the JSON schema URL.
        /// </summary>
        [JsonPropertyName("$schema")]
        public string? Schema { get; set; } = "https://mintlify.com/docs.json";

        /// <summary>
        /// Gets or sets the search configuration.
        /// </summary>
        [JsonPropertyName("search")]
        public SearchConfig? Search { get; set; }

        /// <summary>
        /// Gets or sets the SEO configuration.
        /// </summary>
        [JsonPropertyName("seo")]
        public SeoConfig? Seo { get; set; }

        /// <summary>
        /// Gets or sets the styling configuration.
        /// </summary>
        [JsonPropertyName("styling")]
        public StylingConfig? Styling { get; set; }

        /// <summary>
        /// Gets or sets the theme name.
        /// </summary>
        /// <remarks>
        /// This is a required field. Valid values are: mint, maple, palm, willow, linden, almond, aspen.
        /// </remarks>
        [JsonPropertyName("theme")]
        [NotNull]
        public string Theme { get; set; } = "mint";

        /// <summary>
        /// Gets or sets the thumbnails configuration.
        /// </summary>
        /// <remarks>
        /// Defines custom thumbnail images for social media sharing and link previews.
        /// Can include background images and other preview assets.
        /// </remarks>
        [JsonPropertyName("thumbnails")]
        public Dictionary<string, string>? Thumbnails { get; set; }

        /// <summary>
        /// Gets or sets the navigation type for this documentation project.
        /// </summary>
        /// <value>
        /// The navigation type. Valid values are "Pages" (default), "Tabs", or "Products".
        /// When set to "Pages", the documentation appears in the main navigation.
        /// When set to "Tabs", the documentation appears as a top-level tab.
        /// When set to "Products", the documentation appears as a product in the products section.
        /// </value>
        /// <remarks>
        /// This property is NOT serialized to docs.json. It's used internally during documentation generation
        /// to control how the navigation structure is organized. This is typically set via the MintlifyTemplate
        /// in .docsproj files and applies to the root project. Referenced projects use IntegrationType metadata.
        /// </remarks>
        [JsonIgnore]
        public string NavigationType { get; set; } = "Pages";

        /// <summary>
        /// Gets or sets the navigation name/title for this documentation project when NavigationType is "Tabs" or "Products".
        /// </summary>
        /// <value>
        /// The display name for the tab or product. If not specified, the project name will be used.
        /// </value>
        /// <remarks>
        /// This property is NOT serialized to docs.json. It's used internally during documentation generation.
        /// </remarks>
        [JsonIgnore]
        public string? NavigationName { get; set; }

        #endregion

    }

}
