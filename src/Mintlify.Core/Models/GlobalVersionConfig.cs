using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a version configuration for global navigation in the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration defines a version option that appears globally across all sections
    /// and pages, allowing users to switch between different versions of the documentation.
    /// </remarks>
    public class GlobalVersionConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the version identifier or name.
        /// </summary>
        /// <remarks>
        /// Specifies the version name that will be displayed to users, such as "v1.0",
        /// "v2.0", "latest", "beta", or any descriptive version label. This should be
        /// concise and meaningful to your users. This is a required field.
        /// </remarks>
        [NotNull]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this version is the default version for the documentation.
        /// </summary>
        /// <remarks>
        /// When true, this version will be selected by default when users first visit
        /// the documentation. Only one version should be marked as default.
        /// </remarks>
        [JsonPropertyName("default")]
        public bool? Default { get; set; }

        /// <summary>
        /// Gets or sets whether this version option is hidden by default.
        /// </summary>
        /// <remarks>
        /// When true, this version option will not be visible in the version selector
        /// unless specifically shown. This can be useful for versions that are deprecated,
        /// in development, or not ready for public access.
        /// </remarks>
        public bool? Hidden { get; set; }

        /// <summary>
        /// Gets or sets the URL or path for this version of the documentation.
        /// </summary>
        /// <remarks>
        /// Specifies where users should be directed when they select this version.
        /// Can be a relative path (e.g., "/v2/") or an absolute URL for a different domain
        /// (e.g., "https://v2.docs.example.com/").
        /// </remarks>
        public string? Href { get; set; }

        #endregion

    }

}