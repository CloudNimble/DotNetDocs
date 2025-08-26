using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a language configuration for global navigation in the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration defines a language option that appears globally across all sections
    /// and pages, allowing users to switch between different language versions of the documentation.
    /// </remarks>
    public class GlobalLanguageConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the language code in ISO 639-1 format.
        /// </summary>
        /// <remarks>
        /// Specifies the language using a standard two-letter code such as "en" for English,
        /// "es" for Spanish, "fr" for French, etc. Extended codes like "zh-Hans" for Simplified
        /// Chinese or "fr-CA" for Canadian French are also supported. This is a required field.
        /// </remarks>
        [NotNull]
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this language is the default language for the documentation.
        /// </summary>
        /// <remarks>
        /// When true, this language will be selected by default when users first visit
        /// the documentation. Only one language should be marked as default.
        /// </remarks>
        [JsonPropertyName("default")]
        public bool? Default { get; set; }

        /// <summary>
        /// Gets or sets whether this language option is hidden by default.
        /// </summary>
        /// <remarks>
        /// When true, this language option will not be visible in the language selector
        /// unless specifically shown. This can be useful for languages that are in development
        /// or not ready for public access.
        /// </remarks>
        public bool? Hidden { get; set; }

        /// <summary>
        /// Gets or sets the URL or path for this language version.
        /// </summary>
        /// <remarks>
        /// Specifies where users should be directed when they select this language.
        /// Can be a relative path (e.g., "/es/") or an absolute URL for a different domain
        /// (e.g., "https://es.example.com/").
        /// </remarks>
        public string? Href { get; set; }

        #endregion

    }

}