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
        /// Gets or sets a banner specific to this language.
        /// </summary>
        /// <remarks>
        /// Accepts the same options as the top-level <c>banner</c> field.
        /// See https://www.mintlify.com/docs/organize/settings-structure#navigation-languages.
        /// </remarks>
        public BannerConfig? Banner { get; set; }

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
        /// Gets or sets the directory layout for this language and its descendants.
        /// </summary>
        /// <remarks>
        /// Valid values are <c>none</c>, <c>accordion</c>, and <c>card</c>.
        /// </remarks>
        public string? Directory { get; set; }

        /// <summary>
        /// Gets or sets the dropdowns for the language.
        /// </summary>
        public List<DropdownConfig>? Dropdowns { get; set; }

        /// <summary>
        /// Gets or sets the global navigation configuration.
        /// </summary>
        public GlobalNavigationConfig? Global { get; set; }

        /// <summary>
        /// Gets or sets a footer specific to this language.
        /// </summary>
        /// <remarks>
        /// Accepts the same options as the top-level <c>footer</c> field.
        /// </remarks>
        public FooterConfig? Footer { get; set; }

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
        /// Gets or sets a navbar specific to this language.
        /// </summary>
        /// <remarks>
        /// Accepts the same options as the top-level <c>navbar</c> field.
        /// </remarks>
        public NavbarConfig? Navbar { get; set; }

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
        /// Gets or sets the SDK documentation artifact used to auto-populate pages.
        /// </summary>
        public SdkRefConfig? Sdk { get; set; }

        /// <summary>
        /// Gets or sets the tabs for the language.
        /// </summary>
        public List<TabConfig>? Tabs { get; set; }

        /// <summary>
        /// Gets or sets thumbnail overrides for this language.
        /// </summary>
        public NavThumbnailConfig? Thumbnail { get; set; }

        /// <summary>
        /// Gets or sets the versions for the language.
        /// </summary>
        public List<VersionConfig>? Versions { get; set; }

        #endregion

    }

}