using System;
using System.Collections.Generic;
using System.Text;

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
        /// Escapes generic type syntax for safe rendering in Markdown/MDX.
        /// </summary>
        /// <param name="typeName">The type name potentially containing generic syntax.</param>
        /// <returns>The escaped type name safe for MDX rendering.</returns>
        /// <remarks>
        /// Converts angle brackets in generic type syntax to HTML entities.
        /// For example, JsonConverter&lt;object&gt; becomes JsonConverter&amp;lt;object&amp;gt;
        /// </remarks>
        protected static string EscapeTypeNameForMarkdown(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return typeName ?? string.Empty;
            }

            // Replace angle brackets with HTML entities
            return typeName.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        #endregion

    }

}
