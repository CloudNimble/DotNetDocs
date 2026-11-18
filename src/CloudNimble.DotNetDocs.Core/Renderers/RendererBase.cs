using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CloudNimble.DotNetDocs.Core.Configuration;
using Microsoft.CodeAnalysis;

namespace CloudNimble.DotNetDocs.Core.Renderers
{

    /// <summary>
    /// Base class for documentation renderers providing common functionality.
    /// </summary>
    public abstract class RendererBase
    {

        #region Properties

        /// <summary>
        /// Gets the project context for this renderer.
        /// </summary>
        /// <value>The project context containing configuration and settings.</value>
        protected ProjectContext Context { get; }

        /// <summary>
        /// Gets the file naming options for this renderer.
        /// </summary>
        /// <value>The file naming configuration.</value>
        protected FileNamingOptions FileNamingOptions => Context.FileNamingOptions;

        /// <summary>
        /// When more than one Renderer is registered, allows a renderer to be turned off.
        /// </summary>
        public bool Enabled { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RendererBase"/> class.
        /// </summary>
        /// <param name="context">The project context. If null, a default context is created.</param>
        /// <param name="enabled"></param>
        protected RendererBase(ProjectContext? context = null, bool enabled = true)
        {
            Context = context ?? new ProjectContext();
            Enabled = enabled;
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

        /// <summary>
        /// Gets a safe namespace name for use in file names and display.
        /// </summary>
        /// <param name="ns">The namespace to get the name for.</param>
        /// <returns>A safe namespace name, using "global" for the global namespace.</returns>
        public string GetSafeNamespaceName(DocNamespace ns)
        {
            return string.IsNullOrEmpty(ns.Name) ? "global" : ns.Name;
        }

        /// <summary>
        /// Gets a safe type name for use in file names, removing invalid characters.
        /// </summary>
        /// <param name="type">The type to get the name for.</param>
        /// <returns>A safe type name with invalid characters replaced.</returns>
        protected string GetSafeTypeName(DocType type)
        {
            return ProjectContext.GetSafeFileName(type.Name);
        }

        /// <summary>
        /// Gets the file path for a namespace documentation file.
        /// </summary>
        /// <param name="ns">The namespace to get the file path for.</param>
        /// <param name="outputPath">The base output path.</param>
        /// <param name="extension">The file extension (without the dot).</param>
        /// <returns>The full file path for the namespace documentation.</returns>
        /// <remarks>
        /// The location comes from <see cref="ProjectContext.GetNamespacePagePath(string?)"/>, which cross-reference links also use.
        /// </remarks>
        protected string GetNamespaceFilePath(DocNamespace ns, string outputPath, string extension)
        {
            return Path.Combine(outputPath, $"{Context.GetNamespacePagePath(ns.Name)}.{extension}");
        }

        /// <summary>
        /// Gets a link to a namespace documentation page, relative to the documentation output root.
        /// </summary>
        /// <param name="ns">The namespace to get the link for.</param>
        /// <param name="extension">The file extension (without the dot).</param>
        /// <param name="omitIndexFile">
        /// <see langword="true"/> to link to the namespace folder instead of its index file in <see cref="NamespaceMode.Folder"/>,
        /// for renderers whose output platform resolves a folder link to its index page.
        /// </param>
        /// <returns>The relative link to the namespace page, using forward slashes.</returns>
        /// <example>
        /// For the namespace <c>CloudNimble.DotNetDocs.Core</c> with the extension <c>md</c>:
        /// <code>
        /// // NamespaceMode.File (separator '-'):            CloudNimble-DotNetDocs-Core.md
        /// // NamespaceMode.Folder:                          CloudNimble/DotNetDocs/Core/index.md
        /// // NamespaceMode.Folder, omitIndexFile = true:    CloudNimble/DotNetDocs/Core
        /// </code>
        /// </example>
        /// <remarks>
        /// Use this for links from the assembly index page, which sits at the output root. It shares the path logic in
        /// <see cref="GetNamespaceFilePath(DocNamespace, string, string)"/>, so links always match where the namespace pages are written.
        /// </remarks>
        protected string GetNamespaceLink(DocNamespace ns, string extension, bool omitIndexFile = false)
        {
            var relativePath = GetNamespaceFilePath(ns, string.Empty, extension);

            if (omitIndexFile && FileNamingOptions.NamespaceMode == NamespaceMode.Folder)
            {
                relativePath = Path.GetDirectoryName(relativePath) ?? string.Empty;
            }

            return relativePath.Replace('\\', '/');
        }

        /// <summary>
        /// Gets the file path for a type documentation file.
        /// </summary>
        /// <param name="type">The type to get the file path for.</param>
        /// <param name="ns">The namespace containing the type.</param>
        /// <param name="outputPath">The base output path.</param>
        /// <param name="extension">The file extension (without the dot).</param>
        /// <returns>The full file path for the type documentation.</returns>
        /// <remarks>
        /// The location comes from <see cref="ProjectContext.GetTypePagePath(string?, string)"/>, which cross-reference links also use.
        /// </remarks>
        protected string GetTypeFilePath(DocType type, DocNamespace ns, string outputPath, string extension)
        {
            return Path.Combine(outputPath, $"{Context.GetTypePagePath(ns.Name, type.Name)}.{extension}");
        }

        /// <summary>
        /// Gets a safe file name for a namespace, suitable for use in file systems.
        /// </summary>
        /// <param name="ns">The namespace to get the file name for.</param>
        /// <param name="extension">The file extension (without the dot).</param>
        /// <returns>A safe file name for the namespace.</returns>
        /// <remarks>This method is deprecated. Use GetNamespaceFilePath instead.</remarks>
        public string GetNamespaceFileName(DocNamespace ns, string extension)
        {
            var namespaceName = GetSafeNamespaceName(ns);
            return $"{namespaceName.Replace('.', FileNamingOptions.NamespaceSeparator)}.{extension}";
        }

        #endregion

        #region Protected Static Methods

        /// <summary>
        /// Removes common leading indentation from multi-line text content.
        /// </summary>
        /// <param name="text">The text to remove indentation from.</param>
        /// <returns>The text with common indentation removed.</returns>
        internal static string RemoveIndentation(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            ReadOnlySpan<char> span = text.AsSpan();
            var lines = new List<Range>();

            // Find all line ranges efficiently
            int start = 0;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == '\n')
                {
                    lines.Add(start..i);
                    start = i + 1;
                }
            }
            if (start < span.Length)
                lines.Add(start..span.Length);

            if (lines.Count == 0)
                return text;

            // Step 1: Count leading whitespace in first line
            ReadOnlySpan<char> firstLine = span[lines[0]].TrimEnd("\r\n");
            int firstLineIndent = 0;
            foreach (var ch in firstLine)
            {
                if (ch is ' ' or '\t')
                    firstLineIndent++;
                else
                    break;
            }

            // Pre-calculate capacity for StringBuilder
            var resultBuilder = new StringBuilder(text.Length);

            // Process first line - remove its indentation if any
            if (firstLineIndent > 0 && firstLine.Length > firstLineIndent)
            {
                resultBuilder.Append(firstLine[firstLineIndent..].TrimEnd());
            }
            else
            {
                resultBuilder.Append(firstLine.TrimEnd());
            }

            if (lines.Count == 1)
                return resultBuilder.ToString();

            // Step 2: After removing first line indent from line 2, count its remaining indent
            int secondLineBaseIndent = 0;
            if (lines.Count > 1)
            {
                ReadOnlySpan<char> secondLine = span[lines[1]].TrimEnd("\r\n");

                // Skip past the first line's indentation amount
                int pos = Math.Min(firstLineIndent, secondLine.Length);

                // Count remaining indentation after removing first line's indent
                for (int i = pos; i < secondLine.Length; i++)
                {
                    if (secondLine[i] is ' ' or '\t')
                        secondLineBaseIndent++;
                    else
                    {
                        // Check if first non-whitespace char is '.'
                        if (secondLine[i] == '.')
                        {
                            secondLineBaseIndent = Math.Max(0, secondLineBaseIndent - 4);
                        }
                        break;
                    }
                }
            }

            // Step 3: Process remaining lines
            for (int i = 1; i < lines.Count; i++)
            {
                ReadOnlySpan<char> line = span[lines[i]].TrimEnd("\r\n");
                resultBuilder.AppendLine();

                if (line.IsEmpty)
                    continue;

                // Remove first line indent + second line base indent
                int totalIndentToRemove = firstLineIndent + secondLineBaseIndent;

                if (line.Length > totalIndentToRemove)
                {
                    resultBuilder.Append(line[totalIndentToRemove..].TrimEnd());
                }
                else if (line.Length > firstLineIndent)
                {
                    // If we can't remove all the indent, at least remove what we can
                    resultBuilder.Append(line[firstLineIndent..].TrimEnd());
                }
                else
                {
                    resultBuilder.Append(line.TrimEnd());
                }
            }

            return resultBuilder.ToString();
        }

        #endregion

    }

}
