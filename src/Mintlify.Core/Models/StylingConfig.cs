using System.Text.Json.Serialization;
using Mintlify.Core.Converters;

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
        /// A string (<c>system</c>, <c>dark</c>, or a Shiki theme name) or an object with
        /// <c>theme</c> and optional custom languages. Defaults to <c>system</c>.
        /// </remarks>
        [JsonPropertyName("codeblocks")]
        [JsonConverter(typeof(CodeblocksConverter))]
        public CodeblocksConfig? Codeblocks { get; set; }

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

        /// <summary>
        /// Gets or sets whether to force loading the LaTeX stylesheet.
        /// </summary>
        /// <remarks>
        /// When set to true, the LaTeX stylesheet is always loaded. When false, it is never loaded.
        /// When not specified (null), Mintlify auto-detects LaTeX usage and loads the stylesheet
        /// only when mathematical expressions are found in the content.
        /// </remarks>
        public bool? Latex { get; set; }

        #endregion

    }

}