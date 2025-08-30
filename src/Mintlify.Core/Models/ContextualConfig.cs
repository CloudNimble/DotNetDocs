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
        /// Valid options include:
        /// - "copy": Shows a copy button for code blocks and other copyable content
        /// - "view": Provides view source or view raw options for content
        /// - "chatgpt": Enables ChatGPT integration for AI assistance
        /// - "claude": Enables Claude AI integration for AI assistance
        /// - "perplexity": Enables Perplexity AI integration for AI assistance
        /// - "mcp": Provides details for connecting any AI to the documentation through the MCP protocol
        /// - "cursor": Enables Cursor integration for AI assistance
        /// - "vscode": Enables VS Code integration for AI assistance
        /// These options enhance user interaction with the documentation content.
        /// </remarks>
        public List<string>? Options { get; set; }

        #endregion

    }

}
