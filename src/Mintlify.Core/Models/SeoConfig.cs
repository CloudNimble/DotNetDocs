using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the SEO (Search Engine Optimization) configuration for the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration controls how search engines index and display the documentation,
    /// including meta tags and indexing behavior.
    /// </remarks>
    public class SeoConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets which pages should be indexed by search engines.
        /// </summary>
        /// <remarks>
        /// Valid values are "navigable" (only pages in navigation) or "all" (all pages).
        /// The "navigable" setting indexes only pages that appear in the site navigation,
        /// while "all" indexes every page in the documentation. Defaults to "navigable".
        /// </remarks>
        public string? Indexing { get; set; }

        /// <summary>
        /// Gets or sets custom meta tags to be added to every page.
        /// </summary>
        /// <remarks>
        /// Each key-value pair represents a meta tag name and its content.
        /// These tags are included in the HTML head section of all pages
        /// to provide additional information to search engines.
        /// </remarks>
        [JsonPropertyName("metatags")]
        public Dictionary<string, string>? Metatags { get; set; }

        /// <summary>
        /// Gets or sets the organization used as the publisher entity in structured data.
        /// </summary>
        /// <remarks>
        /// All fields are optional. If omitted, Mintlify derives the organization from the
        /// site name, docs logo, and site URL. See https://www.mintlify.com/docs/optimize/seo.
        /// </remarks>
        public SeoOrganizationConfig? Organization { get; set; }

        /// <summary>
        /// Gets or sets meta tags scoped to a folder.
        /// </summary>
        /// <remarks>
        /// Use this to apply different tags to a subset of pages without repeating them in
        /// frontmatter. Each entry has a folder <c>path</c> and <c>metatags</c>.
        /// See https://www.mintlify.com/docs/organize/settings-seo.
        /// </remarks>
        public List<SeoPathConfig>? Paths { get; set; }

        /// <summary>
        /// Gets or sets whether SEO URLs include a trailing slash.
        /// </summary>
        /// <example>
        /// <code>
        /// "seo": {
        ///   "trailingSlash": true
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// Enable this when hosting serves every page with a trailing slash, such as
        /// <c>/quickstart/</c> instead of <c>/quickstart</c>, so canonical, Open Graph,
        /// and sitemap URLs match.
        /// </remarks>
        public bool? TrailingSlash { get; set; }

        #endregion

    }

}