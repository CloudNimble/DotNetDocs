using System.Text.RegularExpressions;

namespace CloudNimble.DotNetDocs.Core.Renderers
{

    /// <summary>
    /// Base class for Markdown-based renderers providing common formatting utilities.
    /// </summary>
    public abstract partial class MarkdownRendererBase : RendererBase
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

        /// <summary>
        /// Regex pattern to match markdown headers (lines starting with #).
        /// </summary>
        private static readonly Regex HeaderRegex = new(@"^\s*#+\s", RegexOptions.Compiled);

        /// <summary>
        /// Regex pattern to match the TODO comment marker.
        /// </summary>
        [GeneratedRegex(@"^\s*<!--\s*TODO:\s*REMOVE\s+THIS\s+COMMENT\s+AFTER\s+YOU\s+CUSTOMIZE\s+THIS\s+CONTENT\s*-->\s*$", RegexOptions.IgnoreCase)]
        private static partial Regex TodoCommentRegex();

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

        /// <summary>
        /// Gets a template for summary documentation.
        /// </summary>
        /// <param name="entityName">The name of the entity (namespace, assembly, etc.).</param>
        /// <returns>A markdown template string for summary documentation.</returns>
        protected static string GetSummaryTemplate(string entityName)
        {
            return $@"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
# Summary

Describe the purpose and overview of `{entityName}` here.

";
        }

        /// <summary>
        /// Gets a template for usage documentation.
        /// </summary>
        /// <param name="entityName">The name of the entity (type, assembly, etc.).</param>
        /// <returns>A markdown template string for usage documentation.</returns>
        protected static string GetUsageTemplate(string entityName)
        {
            return $@"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
# Usage

Describe how to use `{entityName}` here.

";
        }

        /// <summary>
        /// Gets a template for examples documentation.
        /// </summary>
        /// <param name="entityName">The name of the entity (type, assembly, etc.).</param>
        /// <returns>A markdown template string for examples documentation.</returns>
        protected static string GetExamplesTemplate(string entityName)
        {
            return $@"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
# Examples

Provide examples of using `{entityName}` here.

```csharp
// Example code here
```

";
        }

        /// <summary>
        /// Gets a template for best practices documentation.
        /// </summary>
        /// <param name="entityName">The name of the entity (type, assembly, etc.).</param>
        /// <returns>A markdown template string for best practices documentation.</returns>
        protected static string GetBestPracticesTemplate(string entityName)
        {
            return $@"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
# Best Practices

Document best practices for `{entityName}` here.

";
        }

        /// <summary>
        /// Gets a template for patterns documentation.
        /// </summary>
        /// <param name="entityName">The name of the entity (type, assembly, etc.).</param>
        /// <returns>A markdown template string for patterns documentation.</returns>
        protected static string GetPatternsTemplate(string entityName)
        {
            return $@"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
# Patterns

Document common patterns for `{entityName}` here.

";
        }

        /// <summary>
        /// Gets a template for considerations documentation.
        /// </summary>
        /// <param name="entityName">The name of the entity (type, assembly, etc.).</param>
        /// <returns>A markdown template string for considerations documentation.</returns>
        protected static string GetConsiderationsTemplate(string entityName)
        {
            return $@"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
# Considerations

Document considerations for `{entityName}` here.

";
        }

        /// <summary>
        /// Gets a template for related APIs documentation.
        /// </summary>
        /// <param name="entityName">The name of the entity (type, assembly, etc.).</param>
        /// <returns>A markdown template string for related APIs documentation.</returns>
        protected static string GetRelatedApisTemplate(string entityName)
        {
            return $@"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
# Related APIs

- API 1
- API 2

";
        }

        #endregion

    }

}
