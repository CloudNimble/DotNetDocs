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

        #endregion

    }

}
