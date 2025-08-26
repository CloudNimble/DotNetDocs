using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Mintlify.Core.Converters;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents an anchor configuration in Mintlify navigation.
    /// </summary>
    /// <remarks>
    /// Anchors provide navigation links in your documentation. The anchor name is required.
    /// </remarks>
    public class AnchorConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the name of the anchor.
        /// </summary>
        /// <remarks>
        /// This is a required field that appears as the anchor label.
        /// </remarks>
        [NotNull]
        public string Anchor { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the AsyncAPI configuration.
        /// </summary>
        [JsonPropertyName("asyncapi")]
        [JsonConverter(typeof(ApiConfigConverter))]
        public object? AsyncApi { get; set; }

        /// <summary>
        /// Gets or sets the color configuration for the anchor.
        /// </summary>
        public ColorPairConfig? Color { get; set; }

        /// <summary>
        /// Gets or sets the dropdowns for the anchor.
        /// </summary>
        public List<DropdownConfig>? Dropdowns { get; set; }

        /// <summary>
        /// Gets or sets the global navigation configuration.
        /// </summary>
        public GlobalNavigationConfig? Global { get; set; }

        /// <summary>
        /// Gets or sets the groups for the anchor.
        /// </summary>
        public List<GroupConfig>? Groups { get; set; }

        /// <summary>
        /// Gets or sets whether the current option is default hidden.
        /// </summary>
        public bool? Hidden { get; set; }

        /// <summary>
        /// Gets or sets the URL or path for the anchor.
        /// </summary>
        public string? Href { get; set; }

        /// <summary>
        /// Gets or sets the icon to be displayed in the section.
        /// </summary>
        [JsonConverter(typeof(IconConverter))]
        public object? Icon { get; set; }

        /// <summary>
        /// Gets or sets the languages for the anchor.
        /// </summary>
        public List<LanguageConfig>? Languages { get; set; }

        /// <summary>
        /// Gets or sets the OpenAPI configuration.
        /// </summary>
        [JsonPropertyName("openapi")]
        [JsonConverter(typeof(ApiConfigConverter))]
        public object? OpenApi { get; set; }

        /// <summary>
        /// Gets or sets the pages for the anchor.
        /// </summary>
        [JsonConverter(typeof(NavigationPageListConverter))]
        public List<object>? Pages { get; set; }

        /// <summary>
        /// Gets or sets the tabs for the anchor.
        /// </summary>
        public List<TabConfig>? Tabs { get; set; }

        /// <summary>
        /// Gets or sets the versions for the anchor.
        /// </summary>
        public List<VersionConfig>? Versions { get; set; }

        #endregion

    }

}