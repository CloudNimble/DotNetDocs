using System.Text.Json.Serialization;
using Mintlify.Core.Converters;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a custom contextual menu option.
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///   "title": "Request a feature",
    ///   "description": "Join the discussion on GitHub",
    ///   "icon": "plus",
    ///   "href": "https://github.com/orgs/mintlify/discussions"
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Custom options require <see cref="Title"/>, <see cref="Description"/>, and
    /// <see cref="Href"/>. See https://www.mintlify.com/docs/ai/contextual-menu.
    /// </remarks>
    public class ContextualOptionConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the option description shown beneath the title when the menu expands.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the destination URL or URL template.
        /// </summary>
        /// <remarks>
        /// A string for a simple link, or an object with <c>base</c> and <c>query</c>
        /// for dynamic links. Query values may use <c>$page</c>, <c>$path</c>, and
        /// <c>$mcp</c> placeholders.
        /// </remarks>
        [JsonConverter(typeof(ContextualHrefConverter))]
        public ContextualHrefConfig? Href { get; set; }

        /// <summary>
        /// Gets or sets the option icon.
        /// </summary>
        public IconConfig? Icon { get; set; }

        /// <summary>
        /// Gets or sets the option title.
        /// </summary>
        public string? Title { get; set; }

        #endregion

    }

}
