using System.Collections.Generic;
using System.Text.Json.Serialization;
using Mintlify.Core.Converters;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the contextual options configuration for the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration controls the contextual options that appear in the documentation,
    /// such as copy buttons, view source links, and AI assistant integrations.
    /// </remarks>
    public class ContextualConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the list of contextual options to enable.
        /// </summary>
        /// <remarks>
        /// The first option in the array appears as the default action. Built-in options:
        /// <c>copy</c>, <c>view</c>, <c>assistant</c>, <c>chatgpt</c>, <c>claude</c>,
        /// <c>perplexity</c>, <c>grok</c>, <c>aistudio</c>, <c>devin</c>, <c>windsurf</c>,
        /// <c>download-pdf</c>, <c>download-spec</c>, <c>mcp</c>, <c>add-mcp</c>,
        /// <c>cursor</c>, <c>vscode</c>, and <c>devin-mcp</c>. Custom objects use
        /// <c>title</c>, <c>description</c>, <c>icon</c>, and <c>href</c>.
        /// See https://www.mintlify.com/docs/ai/contextual-menu.
        /// </remarks>
        [JsonConverter(typeof(ContextualOptionListConverter))]
        public List<object>? Options { get; set; }

        /// <summary>
        /// Gets or sets the display location of the contextual menu.
        /// </summary>
        /// <remarks>
        /// Valid values are "header" or "toc". Controls where the contextual options
        /// appear on the page. Defaults to "header".
        /// </remarks>
        public string? Display { get; set; }

        #endregion

    }

}
