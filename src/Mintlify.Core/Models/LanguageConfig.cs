using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a language configuration in Mintlify navigation.
    /// </summary>
    public class LanguageConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the anchors for the language.
        /// </summary>
        public List<AnchorConfig>? Anchors { get; set; }

        /// <summary>
        /// Gets or sets the AsyncAPI configuration.
        /// </summary>
        [JsonPropertyName("asyncapi")]
        public object? AsyncApi { get; set; }

        /// <summary>
        /// Gets or sets whether this language is the default language.
        /// </summary>
        [JsonPropertyName("default")]
        public bool? Default { get; set; }

        /// <summary>
        /// Gets or sets the dropdowns for the language.
        /// </summary>
        public List<DropdownConfig>? Dropdowns { get; set; }

        /// <summary>
        /// Gets or sets the global navigation configuration.
        /// </summary>
        public GlobalNavigationConfig? Global { get; set; }

        /// <summary>
        /// Gets or sets the groups for the language.
        /// </summary>
        public List<GroupConfig>? Groups { get; set; }

        /// <summary>
        /// Gets or sets whether the current option is default hidden.
        /// </summary>
        public bool? Hidden { get; set; }

        /// <summary>
        /// Gets or sets the URL or path for the language.
        /// </summary>
        public string? Href { get; set; }

        /// <summary>
        /// Gets or sets the language code in ISO 639-1 format.
        /// </summary>
        /// <remarks>
        /// This is a required field that identifies the language.
        /// </remarks>
        [NotNull]
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the OpenAPI configuration.
        /// </summary>
        [JsonPropertyName("openapi")]
        public object? OpenApi { get; set; }

        /// <summary>
        /// Gets or sets the pages for the language.
        /// </summary>
        public List<object>? Pages { get; set; }

        /// <summary>
        /// Gets or sets the tabs for the language.
        /// </summary>
        public List<TabConfig>? Tabs { get; set; }

        /// <summary>
        /// Gets or sets the versions for the language.
        /// </summary>
        public List<VersionConfig>? Versions { get; set; }

        #endregion

    }

}