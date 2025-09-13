namespace CloudNimble.DotNetDocs.Core.Renderers
{

    /// <summary>
    /// Base class for Markdown-based renderers providing common formatting utilities.
    /// </summary>
    public abstract class MarkdownRendererBase : RendererBase
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownRendererBase"/> class.
        /// </summary>
        /// <param name="context">The project context. If null, a default context is created.</param>
        public MarkdownRendererBase(ProjectContext? context = null) : base(context)
        {
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Escapes XML/HTML tag syntax for safe rendering in Markdown/MDX.
        /// </summary>
        /// <param name="stringToEscape">The string potentially containing XML/HTML tag syntax or generic type brackets.</param>
        /// <returns>The escaped string safe for MDX rendering.</returns>
        /// <remarks>
        /// Converts angle brackets to HTML entities to prevent MDX parser interpretation as JSX/HTML tags.
        /// This handles both generic type syntax (e.g., JsonConverter&lt;object&gt;) and XML documentation tags
        /// (e.g., &lt;see cref="..."&gt;) by converting them to JsonConverter&amp;lt;object&amp;gt; and
        /// &amp;lt;see cref="..."&amp;gt; respectively.
        /// </remarks>
        protected static string EscapeXmlTagsInString(string? stringToEscape)
        {
            if (string.IsNullOrWhiteSpace(stringToEscape))
            {
                return stringToEscape ?? string.Empty;
            }

            // Replace angle brackets with HTML entities
            return stringToEscape.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        #endregion

    }

}
