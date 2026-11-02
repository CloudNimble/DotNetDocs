using System.Collections.Generic;
using System.Text.Json.Serialization;
using Mintlify.Core.Converters;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Base class for all navigation items in Mintlify.
    /// </summary>
    /// <remarks>
    /// Provides common properties shared across all navigation elements including
    /// API specifications, icons, pages, and visibility settings.
    /// </remarks>
    public abstract class NavigationItemBase
    {

        #region Properties

        /// <summary>
        /// Gets or sets the AsyncAPI configuration.
        /// </summary>
        /// <remarks>
        /// Can be a string URL, an array of URLs, or an object with source and directory properties
        /// pointing to AsyncAPI specification files.
        /// </remarks>
        [JsonPropertyName("asyncapi")]
        [JsonConverter(typeof(ApiConfigConverter))]
        public ApiSpecConfig? AsyncApi { get; set; }

        /// <summary>
        /// Gets or sets the GraphQL schema used to auto-populate reference pages.
        /// </summary>
        /// <example>
        /// <code>
        /// "tab": "GraphQL API",
        /// "graphql": "schema.graphql"
        /// </code>
        /// </example>
        /// <remarks>
        /// Accepts a local SDL path, an HTTPS URL, or an object with <c>source</c> and
        /// optional <c>directory</c>. You can declare <c>graphql</c> on a tab or a group.
        /// Defaults directory to <c>graphql-reference</c>.
        /// See https://www.mintlify.com/docs/api-playground/graphql-setup.
        /// </remarks>
        [JsonPropertyName("graphql")]
        [JsonConverter(typeof(SpecRefConverter))]
        public SpecRefConfig? Graphql { get; set; }

        /// <summary>
        /// Gets or sets whether the current option is default hidden.
        /// </summary>
        /// <remarks>
        /// When true, this navigation item will not be displayed in the navigation by default.
        /// </remarks>
        public bool? Hidden { get; set; }

        /// <summary>
        /// Gets or sets the icon to be displayed in the section.
        /// </summary>
        /// <remarks>
        /// Can be a Font Awesome icon name, Lucide icon name, JSX-compatible SVG code,
        /// URL to an externally hosted icon, or path to an icon file in your project.
        /// </remarks>
        [JsonConverter(typeof(IconConverter))]
        public IconConfig? Icon { get; set; }

        /// <summary>
        /// Gets or sets the MCP definition used to auto-populate reference pages.
        /// </summary>
        /// <remarks>
        /// Accepts a local path to an MCP definition JSON file, or an object with
        /// <c>source</c> and optional <c>directory</c>.
        /// </remarks>
        [JsonPropertyName("mcp")]
        [JsonConverter(typeof(SpecRefConverter))]
        public SpecRefConfig? Mcp { get; set; }

        /// <summary>
        /// Gets or sets the OpenAPI configuration.
        /// </summary>
        /// <remarks>
        /// Can be a string URL, an array of URLs, or an object with source and directory properties
        /// pointing to OpenAPI specification files.
        /// </remarks>
        [JsonPropertyName("openapi")]
        [JsonConverter(typeof(ApiConfigConverter))]
        public ApiSpecConfig? OpenApi { get; set; }

        /// <summary>
        /// Gets or sets the pages for this navigation item.
        /// </summary>
        /// <remarks>
        /// Pages can be strings (page paths) or nested GroupConfig objects for hierarchical navigation.
        /// </remarks>
        [JsonConverter(typeof(NavigationPageListConverter))]
        public List<object>? Pages { get; set; }

        #endregion

    }

}
