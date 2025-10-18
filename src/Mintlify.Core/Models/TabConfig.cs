using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Mintlify.Core.Converters;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a tab configuration in Mintlify navigation.
    /// </summary>
    /// <remarks>
    /// Tabs provide top-level navigation sections in your documentation.
    /// The tab name is required.
    /// </remarks>
    public class TabConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the anchors for the tab.
        /// </summary>
        public List<AnchorConfig>? Anchors { get; set; }

        /// <summary>
        /// Gets or sets the AsyncAPI configuration.
        /// </summary>
        [JsonPropertyName("asyncapi")]
        [JsonConverter(typeof(ApiConfigConverter))]
        public ApiSpecConfig? AsyncApi { get; set; }

        /// <summary>
        /// Gets or sets the dropdowns for the tab.
        /// </summary>
        public List<DropdownConfig>? Dropdowns { get; set; }

        /// <summary>
        /// Gets or sets the global navigation configuration.
        /// </summary>
        public GlobalNavigationConfig? Global { get; set; }

        /// <summary>
        /// Gets or sets the groups for the tab.
        /// </summary>
        public List<GroupConfig>? Groups { get; set; }

        /// <summary>
        /// Gets or sets whether the current option is default hidden.
        /// </summary>
        public bool? Hidden { get; set; }

        /// <summary>
        /// Gets or sets the URL or path for the tab.
        /// </summary>
        public string? Href { get; set; }

        /// <summary>
        /// Gets or sets the icon to be displayed in the section.
        /// </summary>
        [JsonConverter(typeof(IconConverter))]
        public IconConfig? Icon { get; set; }

        /// <summary>
        /// Gets or sets the languages for the tab.
        /// </summary>
        public List<LanguageConfig>? Languages { get; set; }

        /// <summary>
        /// Gets or sets the OpenAPI configuration.
        /// </summary>
        [JsonPropertyName("openapi")]
        [JsonConverter(typeof(ApiConfigConverter))]
        public ApiSpecConfig? OpenApi { get; set; }

        /// <summary>
        /// Gets or sets the pages for the tab.
        /// </summary>
        [JsonConverter(typeof(NavigationPageListConverter))]
        public List<object>? Pages { get; set; }

        /// <summary>
        /// Gets or sets the name of the tab.
        /// </summary>
        /// <remarks>
        /// This is a required field that appears as the tab label in navigation.
        /// </remarks>
        [NotNull]
        public string Tab { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the versions for the tab.
        /// </summary>
        public List<VersionConfig>? Versions { get; set; }

        #endregion

    }

}