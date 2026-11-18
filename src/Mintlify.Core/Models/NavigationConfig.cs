using System.Collections.Generic;
using System.Text.Json.Serialization;
using Mintlify.Core.Converters;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the navigation configuration for Mintlify.
    /// </summary>
    /// <remarks>
    /// This is a required field that defines the structure of your documentation.
    /// At minimum, you need to define some navigation items (pages, groups, tabs, etc.).
    /// </remarks>
    public class NavigationConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the anchors in the navigation.
        /// </summary>
        public List<AnchorConfig>? Anchors { get; set; }

        /// <summary>
        /// Gets or sets the dropdowns in the navigation.
        /// </summary>
        public List<DropdownConfig>? Dropdowns { get; set; }

        /// <summary>
        /// Gets or sets global navigation items that appear on all sections and pages.
        /// </summary>
        public GlobalNavigationConfig? Global { get; set; }

        /// <summary>
        /// Gets or sets the directory layout for this navigation tree.
        /// </summary>
        /// <remarks>
        /// Valid values are <c>none</c>, <c>accordion</c>, and <c>card</c>. Descendants
        /// inherit the value unless they override it.
        /// See https://www.mintlify.com/docs/organize/navigation#directory-listings.
        /// </remarks>
        [JsonPropertyName("directory")]
        public string? Directory { get; set; }

        /// <summary>
        /// Gets or sets the GraphQL schema used to auto-populate reference pages.
        /// </summary>
        /// <remarks>
        /// Accepts a local SDL path, an HTTPS URL, or an object with <c>source</c> and
        /// optional <c>directory</c>. See https://www.mintlify.com/docs/api-playground/graphql-setup.
        /// </remarks>
        [JsonPropertyName("graphql")]
        [JsonConverter(typeof(SpecRefConverter))]
        public SpecRefConfig? Graphql { get; set; }

        /// <summary>
        /// Gets or sets the groups in the navigation.
        /// </summary>
        public List<GroupConfig>? Groups { get; set; }

        /// <summary>
        /// Gets or sets the languages in the navigation.
        /// </summary>
        public List<LanguageConfig>? Languages { get; set; }

        /// <summary>
        /// Gets or sets the MCP definition used to auto-populate reference pages.
        /// </summary>
        [JsonPropertyName("mcp")]
        [JsonConverter(typeof(SpecRefConverter))]
        public SpecRefConfig? Mcp { get; set; }

        /// <summary>
        /// Gets or sets the pages in the navigation.
        /// </summary>
        /// <remarks>
        /// Pages can be either strings (page paths) or nested GroupConfig objects.
        /// </remarks>
        [JsonPropertyName("pages")]
        [JsonConverter(typeof(NavigationPageListConverter))]
        public List<object>? Pages { get; set; }

        /// <summary>
        /// Gets or sets the products in the navigation.
        /// </summary>
        /// <remarks>
        /// Products separate documentation into distinct product-specific sections.
        /// </remarks>
        public List<ProductConfig>? Products { get; set; }

        /// <summary>
        /// Gets or sets the SDK documentation artifact used to auto-populate pages.
        /// </summary>
        /// <remarks>
        /// Typically declared on a tab or group rather than the navigation root.
        /// See <see cref="SdkRefConfig"/> and
        /// https://www.mintlify.com/docs/api-playground/sdk-reference-setup.
        /// </remarks>
        public SdkRefConfig? Sdk { get; set; }

        /// <summary>
        /// Gets or sets the tabs in the navigation.
        /// </summary>
        public List<TabConfig>? Tabs { get; set; }

        /// <summary>
        /// Gets or sets the versions in the navigation.
        /// </summary>
        public List<VersionConfig>? Versions { get; set; }

        #endregion

    }

}