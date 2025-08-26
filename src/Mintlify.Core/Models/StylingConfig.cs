using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the styling configuration for various UI elements in the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration controls the visual styling of specific components such as
    /// breadcrumbs, section eyebrows, and code blocks throughout the documentation.
    /// </remarks>
    public class StylingConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the code block theme.
        /// </summary>
        /// <remarks>
        /// Valid values are "system" or "dark". The "system" option uses a theme that
        /// matches the current light/dark mode, while "dark" always uses a dark theme
        /// for code blocks regardless of the site's appearance mode. Defaults to "system".
        /// </remarks>
        [JsonPropertyName("codeblocks")]
        public string? Codeblocks { get; set; }

        /// <summary>
        /// Gets or sets the eyebrows style for content sections.
        /// </summary>
        /// <remarks>
        /// Valid values are "section" or "breadcrumbs". This controls the style of the
        /// small text that appears above page titles and section headers. "section" shows
        /// the current section name, while "breadcrumbs" shows the full navigation path.
        /// Defaults to "section".
        /// </remarks>
        public string? Eyebrows { get; set; }

        #endregion

    }

}