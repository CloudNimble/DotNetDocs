using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents folder-scoped SEO meta tags.
    /// </summary>
    /// <remarks>
    /// Use this to apply different tags to a subset of pages, such as versioned docs or a
    /// specific product area, without repeating them in page frontmatter. Each path can
    /// appear only once. Paths match page URLs, so translated pages in a language folder
    /// need their own entries. Mintlify ignores a <c>canonical</c> key in path entries.
    /// See https://www.mintlify.com/docs/organize/settings-seo.
    /// </remarks>
    public class SeoPathConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets meta tags added to every page under <see cref="Path"/>.
        /// </summary>
        /// <remarks>
        /// Provide key-value pairs. Values must be non-empty strings.
        /// </remarks>
        [JsonPropertyName("metatags")]
        public Dictionary<string, string>? Metatags { get; set; }

        /// <summary>
        /// Gets or sets the folder path relative to the docs root.
        /// </summary>
        /// <remarks>
        /// Example: <c>guides/v2</c>. Do not include a leading or trailing slash.
        /// Applies to every page under that folder. Longer matching paths override
        /// shorter ones. Precedence is site-wide <c>seo.metatags</c>, then matching
        /// path entries, then page frontmatter.
        /// </remarks>
        public string? Path { get; set; }

        #endregion

    }

}
