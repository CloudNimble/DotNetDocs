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
        /// Gets or sets the directory layout for this node and its descendants.
        /// </summary>
        /// <remarks>
        /// Valid values are <c>none</c> (default, no listing), <c>accordion</c> (collapsible
        /// list grouped by section), and <c>card</c> (horizontal cards). The value inherits
        /// recursively; a descendant can override it. Directory listings only appear on
        /// groups that have a <c>root</c> page.
        /// See https://www.mintlify.com/docs/organize/navigation#directory-listings.
        /// </remarks>
        [JsonPropertyName("directory")]
        public string? Directory { get; set; }

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

        /// <summary>
        /// Gets or sets the SDK documentation artifact used to auto-populate pages.
        /// </summary>
        /// <remarks>
        /// Declare <c>sdk</c> on a tab or a group. Nested groups inherit the parent
        /// settings unless they set their own. See <see cref="SdkRefConfig"/>.
        /// </remarks>
        public SdkRefConfig? Sdk { get; set; }

        /// <summary>
        /// Gets or sets whether hidden descendants remain indexed for search and llms.txt.
        /// </summary>
        /// <remarks>
        /// When <c>true</c> on a hidden tab or group, descendant pages stay in site search,
        /// <c>sitemap.xml</c>, AI assistant context, MCP search, and search-engine indexing.
        /// The tab or group itself stays hidden. Has no effect when <see cref="Hidden"/> is
        /// false. A page's own <c>hidden</c> frontmatter always wins.
        /// See https://www.mintlify.com/docs/organize/hidden-pages.
        /// </remarks>
        public bool? Searchable { get; set; }

        /// <summary>
        /// Gets or sets thumbnail overrides for pages under this node.
        /// </summary>
        /// <remarks>
        /// Each key overrides the nearest parent and site-level <c>thumbnails</c>.
        /// In an override, <c>eyebrow</c> is literal text and <c>""</c> hides it.
        /// See https://www.mintlify.com/docs/organize/settings-appearance#override-thumbnails-for-a-page-or-section.
        /// </remarks>
        public NavThumbnailConfig? Thumbnail { get; set; }

        #endregion

    }

}
