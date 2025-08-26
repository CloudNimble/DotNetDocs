using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Mintlify.Core.Converters;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a dropdown configuration in Mintlify navigation.
    /// </summary>
    public class DropdownConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the anchors for the dropdown.
        /// </summary>
        public List<AnchorConfig>? Anchors { get; set; }

        /// <summary>
        /// Gets or sets the AsyncAPI configuration.
        /// </summary>
        [JsonPropertyName("asyncapi")]
        [JsonConverter(typeof(ApiConfigConverter))]
        public object? AsyncApi { get; set; }

        /// <summary>
        /// Gets or sets the color configuration for the dropdown.
        /// </summary>
        public ColorPairConfig? Color { get; set; }

        /// <summary>
        /// Gets or sets the description of the dropdown.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the name of the dropdown.
        /// </summary>
        /// <remarks>
        /// This is a required field that appears as the dropdown label in navigation.
        /// </remarks>
        [NotNull]
        public string Dropdown { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the global navigation configuration.
        /// </summary>
        public GlobalNavigationConfig? Global { get; set; }

        /// <summary>
        /// Gets or sets the groups for the dropdown.
        /// </summary>
        public List<GroupConfig>? Groups { get; set; }

        /// <summary>
        /// Gets or sets whether the current option is default hidden.
        /// </summary>
        public bool? Hidden { get; set; }

        /// <summary>
        /// Gets or sets the URL or path for the dropdown.
        /// </summary>
        public string? Href { get; set; }

        /// <summary>
        /// Gets or sets the icon to be displayed in the section.
        /// </summary>
        [JsonConverter(typeof(IconConverter))]
        public object? Icon { get; set; }

        /// <summary>
        /// Gets or sets the languages for the dropdown.
        /// </summary>
        public List<LanguageConfig>? Languages { get; set; }

        /// <summary>
        /// Gets or sets the OpenAPI configuration.
        /// </summary>
        [JsonPropertyName("openapi")]
        [JsonConverter(typeof(ApiConfigConverter))]
        public object? OpenApi { get; set; }

        /// <summary>
        /// Gets or sets the pages for the dropdown.
        /// </summary>
        [JsonConverter(typeof(NavigationPageListConverter))]
        public List<object>? Pages { get; set; }

        /// <summary>
        /// Gets or sets the tabs for the dropdown.
        /// </summary>
        public List<TabConfig>? Tabs { get; set; }

        /// <summary>
        /// Gets or sets the versions for the dropdown.
        /// </summary>
        public List<VersionConfig>? Versions { get; set; }

        #endregion

    }

}