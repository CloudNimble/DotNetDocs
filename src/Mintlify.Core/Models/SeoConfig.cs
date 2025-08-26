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

        #endregion

    }

}