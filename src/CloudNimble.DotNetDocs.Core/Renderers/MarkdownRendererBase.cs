using System;
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
        /// Checks if the first non-blank, non-comment line in the content is a markdown header.
        /// Only checks the first 4 lines to avoid breaking on subheaders within content.
        /// </summary>
        /// <param name="content">The content to check.</param>
        /// <returns>true if the first meaningful line is a header; otherwise, false.</returns>
        protected static bool IsFirstLineHeader(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            var lines = content.Split('\n');
            int linesToCheck = Math.Min(4, lines.Length);

            for (int i = 0; i < linesToCheck; i++)
            {
                var trimmed = lines[i].Trim();

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                // Skip TODO comment lines
                if (TodoCommentRegex().IsMatch(trimmed))
                {
                    continue;
                }

                // Check if this line is a header
                return HeaderRegex.IsMatch(trimmed);
            }

            return false;
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
