namespace CloudNimble.DotNetDocs.Core.Renderers
{

    /// <summary>
    /// Base class for Markdown-based renderers providing common formatting utilities.
    /// </summary>
    public abstract class MarkdownRendererBase : RendererBase
    {

        #region Fields

        /// <summary>
        /// Header text for best practices sections.
        /// </summary>
        protected const string BestPracticesHeader = "## Best Practices";

        /// <summary>
        /// Header text for considerations sections.
        /// </summary>
        protected const string ConsiderationsHeader = "## Considerations";

        /// <summary>
        /// Header text for examples sections.
        /// </summary>
        protected const string ExamplesHeader = "## Examples";

        /// <summary>
        /// Header text for patterns sections.
        /// </summary>
        protected const string PatternsHeader = "## Patterns";

        /// <summary>
        /// Header text for remarks sections.
        /// </summary>
        protected const string RemarksHeader = "## Remarks";

        /// <summary>
        /// Header text for summary sections.
        /// </summary>
        protected const string SummaryHeader = "## Summary";

        /// <summary>
        /// Header text for usage sections.
        /// </summary>
        protected const string UsageHeader = "## Usage";

        #endregion

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
        /// Gets the header text to use for a content section, or null if no header should be added.
        /// Returns null if the content already contains the specified header.
        /// </summary>
        /// <param name="content">The content to check.</param>
        /// <param name="headerText">The header text that would be added (e.g., "## Summary").</param>
        /// <returns>The header text to add, or null if no header should be added.</returns>
        protected static string? GetHeaderText(string? content, string headerText)
        {
            return string.IsNullOrWhiteSpace(content) || !content.Contains(headerText)
                    ? headerText
                    : null;
        }

        #endregion

    }

}
