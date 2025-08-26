using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a URL redirect configuration for the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration defines how requests to specific paths should be redirected
    /// to different URLs, useful for maintaining backward compatibility or reorganizing content.
    /// </remarks>
    public class RedirectConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the destination path where requests should be redirected.
        /// </summary>
        /// <remarks>
        /// Specifies where users should be redirected when they visit the source path.
        /// Can be a relative path (e.g., "/new-page") or an absolute URL
        /// (e.g., "https://example.com/page"). This is a required field.
        /// </remarks>
        [NotNull]
        public string Destination { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the redirect is permanent (301) or temporary (302).
        /// </summary>
        /// <remarks>
        /// When true, returns a 301 (Moved Permanently) status code, indicating to search
        /// engines that the move is permanent. When false or not specified, returns a 302
        /// (Found) status code for temporary redirects. Permanent redirects are better for SEO
        /// when content has permanently moved.
        /// </remarks>
        public bool? Permanent { get; set; }

        /// <summary>
        /// Gets or sets the source path that should be redirected.
        /// </summary>
        /// <remarks>
        /// Specifies the original path that users might visit. When a request is made
        /// to this path, it will be automatically redirected to the destination.
        /// Should be a relative path starting with "/" (e.g., "/old-page"). This is a required field.
        /// </remarks>
        [NotNull]
        public string Source { get; set; } = string.Empty;

        #endregion

    }

}