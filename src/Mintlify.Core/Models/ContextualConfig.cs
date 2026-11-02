using System.Collections.Generic;

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
        /// copy, view, assistant, chatgpt, claude, perplexity, grok, aistudio, devin,
        /// windsurf, download-pdf, download-spec, mcp, add-mcp, cursor, vscode, and
        /// devin-mcp. See https://www.mintlify.com/docs/organize/settings-structure#contextual.
        /// </remarks>
        public List<string>? Options { get; set; }

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
