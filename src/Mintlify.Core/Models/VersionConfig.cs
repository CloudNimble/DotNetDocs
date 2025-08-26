using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a version configuration in Mintlify navigation.
    /// </summary>
    /// <remarks>
    /// Versions allow you to maintain multiple documentation versions.
    /// The version name is required.
    /// </remarks>
    public class VersionConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the anchors for the version.
        /// </summary>
        public List<AnchorConfig>? Anchors { get; set; }

        /// <summary>
        /// Gets or sets the AsyncAPI configuration.
        /// </summary>
        [JsonPropertyName("asyncapi")]
        public object? AsyncApi { get; set; }

        /// <summary>
        /// Gets or sets whether this version is the default version.
        /// </summary>
        [JsonPropertyName("default")]
        public bool? Default { get; set; }

        /// <summary>
        /// Gets or sets the dropdowns for the version.
        /// </summary>
        public List<DropdownConfig>? Dropdowns { get; set; }

        /// <summary>
        /// Gets or sets the global navigation configuration.
        /// </summary>
        public GlobalNavigationConfig? Global { get; set; }

        /// <summary>
        /// Gets or sets the groups for the version.
        /// </summary>
        public List<GroupConfig>? Groups { get; set; }

        /// <summary>
        /// Gets or sets whether the current option is default hidden.
        /// </summary>
        public bool? Hidden { get; set; }

        /// <summary>
        /// Gets or sets the URL or path for the version.
        /// </summary>
        public string? Href { get; set; }

        /// <summary>
        /// Gets or sets the languages for the version.
        /// </summary>
        public List<LanguageConfig>? Languages { get; set; }

        /// <summary>
        /// Gets or sets the OpenAPI configuration.
        /// </summary>
        [JsonPropertyName("openapi")]
        public object? OpenApi { get; set; }

        /// <summary>
        /// Gets or sets the pages for the version.
        /// </summary>
        public List<object>? Pages { get; set; }

        /// <summary>
        /// Gets or sets the tabs for the version.
        /// </summary>
        public List<TabConfig>? Tabs { get; set; }

        /// <summary>
        /// Gets or sets the name of the version.
        /// </summary>
        /// <remarks>
        /// This is a required field that identifies the version.
        /// </remarks>
        [NotNull]
        public string Version { get; set; } = string.Empty;

        #endregion

    }

}