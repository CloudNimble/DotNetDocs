namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a query-string pair on a custom contextual option href.
    /// </summary>
    public class ContextualHrefQueryConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the query parameter name.
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Gets or sets the query parameter value.
        /// </summary>
        /// <remarks>
        /// Mintlify replaces <c>$page</c> with the current page Markdown, <c>$path</c>
        /// with the current page path, and <c>$mcp</c> with the hosted MCP server URL.
        /// </remarks>
        public string? Value { get; set; }

        #endregion

    }

}
