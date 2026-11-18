using System;
using System.Text;
using System.Text.RegularExpressions;

namespace CloudNimble.DotNetDocs.Core.Transformers
{

    /// <summary>
    /// Transforms XML documentation tags in DocEntity properties to Markdown format.
    /// </summary>
    /// <remarks>
    /// This transformer converts XML documentation tags to their Markdown equivalents. The traversal of the documentation
    /// model, the skipping of strings without tags, and the resolution of <c>cref</c> references come from
    /// <see cref="XmlTransformerBase"/>; this class only decides how each tag is written in Markdown.
    /// </remarks>
    public partial class MarkdownXmlTransformer : XmlTransformerBase
    {

        #region Fields

        /// <summary>
        /// Pattern for see cref tags.
        /// </summary>
        [GeneratedRegex(@"<see\s+cref=""([^""]+)""\s*/?>", RegexOptions.IgnoreCase)]
        private static partial Regex SeeRefPattern();

        /// <summary>
        /// Pattern for see href tags with optional text.
        /// </summary>
        [GeneratedRegex(@"<see\s+href=""([^""]+)""(?:\s*/>|>(.*?)</see>)", RegexOptions.IgnoreCase)]
        private static partial Regex SeeHrefPattern();

        /// <summary>
        /// Pattern for see langword tags.
        /// </summary>
        [GeneratedRegex(@"<see\s+langword=""([^""]+)""\s*/?>", RegexOptions.IgnoreCase)]
        private static partial Regex SeeLangwordPattern();

        /// <summary>
        /// Pattern for paramref name tags.
        /// </summary>
        [GeneratedRegex(@"<paramref\s+name=""([^""]+)""\s*/?>", RegexOptions.IgnoreCase)]
        private static partial Regex ParamRefPattern();

        /// <summary>
        /// Pattern for typeparamref name tags.
        /// </summary>
        [GeneratedRegex(@"<typeparamref\s+name=""([^""]+)""\s*/?>", RegexOptions.IgnoreCase)]
        private static partial Regex TypeParamRefPattern();

        /// <summary>
        /// Pattern for c inline code tags.
        /// </summary>
        [GeneratedRegex(@"<c>(.*?)</c>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex InlineCodePattern();

        /// <summary>
        /// Pattern for code block tags with optional language attribute.
        /// </summary>
        [GeneratedRegex(@"<code(?:\s+language=""([^""]+)"")?\s*>(.*?)</code>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex CodeBlockPattern();

        /// <summary>
        /// Pattern for para tags.
        /// </summary>
        [GeneratedRegex(@"<para>(.*?)</para>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex ParaPattern();

        /// <summary>
        /// Pattern for bold tags.
        /// </summary>
        [GeneratedRegex(@"<b>(.*?)</b>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex BoldPattern();

        /// <summary>
        /// Pattern for italic tags.
        /// </summary>
        [GeneratedRegex(@"<i>(.*?)</i>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex ItalicPattern();

        /// <summary>
        /// Pattern for line break tags.
        /// </summary>
        [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
        private static partial Regex LineBreakPattern();

        /// <summary>
        /// Pattern for collapsing three or more consecutive newlines into exactly two.
        /// </summary>
        [GeneratedRegex(@"\n{3,}")]
        private static partial Regex ConsecutiveBlankLinesPattern();

        /// <summary>
        /// Pattern for list structures.
        /// </summary>
        [GeneratedRegex(@"<list\s+type=""(bullet|number|table)""[^>]*>(.*?)</list>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex ListPattern();

        /// <summary>
        /// Pattern for list items.
        /// </summary>
        [GeneratedRegex(@"<item>(?:<term>(.*?)</term>)?(?:<description>(.*?)</description>)?</item>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex ListItemPattern();

        /// <summary>
        /// Pattern for list headers (for tables).
        /// </summary>
        [GeneratedRegex(@"<listheader>(?:<term>(.*?)</term>)?(?:<description>(.*?)</description>)?</listheader>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex ListHeaderPattern();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownXmlTransformer"/> class.
        /// </summary>
        /// <param name="projectContext">The project context for configuration and file naming.</param>
        public MarkdownXmlTransformer(ProjectContext projectContext) : base(projectContext)
        {
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Converts code-related tags to Markdown code formatting.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <returns>The text with <c>c</c> and <c>code</c> tags converted to inline code and fenced code blocks.</returns>
        protected virtual string ConvertCodeTags(string text)
        {
            text = InlineCodePattern().Replace(text, match =>
            {
                var code = match.Groups[1].Value.Trim();
                return string.IsNullOrEmpty(code) ? "" : $"`{code}`";
            });

            // Code blocks may wrap their content in CDATA.
            text = CodeBlockPattern().Replace(text, match =>
            {
                var language = match.Groups[1].Success ? match.Groups[1].Value : "csharp";
                var code = match.Groups[2].Value;

                if (code.Contains("<![CDATA["))
                {
                    code = Regex.Replace(code, @"^\s*<!\[CDATA\[", "", RegexOptions.Singleline);
                    code = Regex.Replace(code, @"\]\]>\s*$", "", RegexOptions.Singleline);
                }

                code = RemoveCommonIndentation(code);

                // Escape existing triple backticks so they display as literal backticks instead of closing the fence.
                code = code.Replace("```", "\\`\\`\\`");

                return string.IsNullOrEmpty(code) ? "" : $"```{language}\n{code}\n```";
            });

            return text;
        }

        /// <summary>
        /// Converts text formatting tags to Markdown equivalents.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <returns>The text with <c>para</c>, <c>b</c>, <c>i</c>, and <c>br</c> tags converted.</returns>
        protected virtual string ConvertFormattingTags(string text)
        {
            // Trim the indentation that para content inherits from the XML doc comment.
            text = ParaPattern().Replace(text, match =>
            {
                var content = RemoveCommonIndentation(match.Groups[1].Value);
                return $"\n\n{content}\n\n";
            });

            text = BoldPattern().Replace(text, match => $"**{match.Groups[1].Value}**");
            text = ItalicPattern().Replace(text, match => $"*{match.Groups[1].Value}*");
            text = LineBreakPattern().Replace(text, "  \n");

            // Collapse runs of 3+ newlines (from para conversion + inter-tag whitespace) to at most one blank line,
            // then trim leading/trailing whitespace left over from XML doc comment formatting
            text = ConsecutiveBlankLinesPattern().Replace(text, "\n\n").Trim();

            return text;
        }

        /// <summary>
        /// Converts list structures to Markdown format.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <returns>The text with bullet and number lists converted to Markdown lists, and table lists converted to tables or definition lists.</returns>
        protected virtual string ConvertLists(string text)
        {
            return ListPattern().Replace(text, match =>
            {
                var listType = match.Groups[1].Value.ToLowerInvariant();
                var listContent = match.Groups[2].Value;

                var items = ListItemPattern().Matches(listContent);
                var result = new StringBuilder();

                if (listType == "table")
                {
                    var headerMatch = ListHeaderPattern().Match(listContent);
                    if (headerMatch.Success)
                    {
                        result.AppendLine();
                        result.AppendLine($"| {headerMatch.Groups[1].Value} | {headerMatch.Groups[2].Value} |");
                        result.AppendLine("|----------|----------|");

                        foreach (Match item in items)
                        {
                            result.AppendLine($"| {item.Groups[1].Value} | {item.Groups[2].Value} |");
                        }
                    }
                    else
                    {
                        // A table without a header is written as a definition list.
                        foreach (Match item in items)
                        {
                            var term = item.Groups[1].Value;
                            var desc = item.Groups[2].Value;

                            if (!string.IsNullOrWhiteSpace(term))
                            {
                                result.AppendLine($"\n**{term}**");
                            }

                            if (!string.IsNullOrWhiteSpace(desc))
                            {
                                result.AppendLine(desc);
                            }
                        }
                    }
                }
                else
                {
                    var counter = 1;
                    result.AppendLine();

                    foreach (Match item in items)
                    {
                        // Fall back to the term when an item has no description.
                        var description = item.Groups[2].Value;
                        if (string.IsNullOrWhiteSpace(description))
                        {
                            description = item.Groups[1].Value;
                        }

                        if (!string.IsNullOrWhiteSpace(description))
                        {
                            var prefix = listType == "bullet" ? "-" : $"{counter}.";
                            result.AppendLine($"{prefix} {description}");
                            counter++;
                        }
                    }
                }

                return result.ToString();
            });
        }

        /// <summary>
        /// Converts parameter and type parameter reference tags.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <returns>The text with <c>paramref</c> and <c>typeparamref</c> tags converted to italic names.</returns>
        protected virtual string ConvertReferenceParams(string text)
        {
            text = ParamRefPattern().Replace(text, match => $"*{match.Groups[1].Value}*");
            text = TypeParamRefPattern().Replace(text, match => $"*{match.Groups[1].Value}*");

            return text;
        }

        /// <summary>
        /// Converts see reference tags to Markdown links or inline code.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <returns>The text with <c>see cref</c>, <c>see href</c>, and <c>see langword</c> tags converted.</returns>
        protected virtual string ConvertSeeReferences(string text)
        {
            text = SeeRefPattern().Replace(text, match => ResolveReference(match.Groups[1].Value, CurrentPath).ToMarkdownLink());

            text = SeeHrefPattern().Replace(text, match =>
            {
                var url = match.Groups[1].Value;
                var linkText = match.Groups[2].Success ? match.Groups[2].Value : "link";
                return $"[{linkText}]({url})";
            });

            text = SeeLangwordPattern().Replace(text, match =>
            {
                var keyword = match.Groups[1].Value.ToLowerInvariant();
                var url = GetLanguageKeywordUrl(keyword);
                return url is not null ? $"[`{keyword}`]({url})" : $"`{keyword}`";
            });

            return text;
        }

        /// <summary>
        /// Converts the XML documentation tags in a string to Markdown.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <param name="skipEscaping">When true, skips the final XML tag escaping step. Use for conceptual content that is already valid Markdown.</param>
        /// <returns>The text converted to Markdown.</returns>
        protected override string ConvertXml(string text, bool skipEscaping)
        {
            text = ConvertSeeReferences(text);
            text = ConvertCodeTags(text);
            text = ConvertReferenceParams(text);
            text = ConvertFormattingTags(text);
            text = ConvertLists(text);

            if (!skipEscaping)
            {
                text = EscapeRemainingXmlTags(text);
            }

            return text;
        }

        /// <summary>
        /// Escapes any remaining XML tags that weren't processed.
        /// </summary>
        /// <param name="text">The text to escape.</param>
        /// <returns>The text with the remaining tags escaped outside of code.</returns>
        /// <remarks>
        /// This method preserves content inside various delimiter pairs to avoid double-escaping:
        /// - Single backticks (inline code): `code`
        /// - Triple backticks (code fences): ```language\ncode\n```
        /// - No-escape blocks: ```noescape\ncontent\n``` - content is extracted without backticks or escaping
        /// </remarks>
        protected virtual string EscapeRemainingXmlTags(string text)
        {
            var result = new StringBuilder();
            var position = 0;

            while (position < text.Length)
            {
                var nextBacktick = text.IndexOf('`', position);

                if (nextBacktick == -1)
                {
                    result.Append(EscapeXmlTags(text[position..]));
                    break;
                }

                if (nextBacktick > position)
                {
                    result.Append(EscapeXmlTags(text[position..nextBacktick]));
                }

                if (nextBacktick + 2 < text.Length &&
                    text[nextBacktick + 1] == '`' &&
                    text[nextBacktick + 2] == '`')
                {
                    // Triple backtick code fence
                    var fenceStart = nextBacktick + 3;
                    var isNoEscape = text.IndexOf("noescape", fenceStart, Math.Min(20, text.Length - fenceStart), StringComparison.Ordinal) == fenceStart;
                    var closingPos = text.IndexOf("```", fenceStart, StringComparison.Ordinal);

                    if (closingPos == -1)
                    {
                        // No closing fence, escape the rest
                        result.Append(EscapeXmlTags(text[nextBacktick..]));
                        break;
                    }

                    if (isNoEscape)
                    {
                        // Extract the content without the delimiters or escaping, skipping "noescape" (8 chars) and the newline after it.
                        var contentStart = fenceStart + 8;
                        if (contentStart < text.Length && text[contentStart] == '\r') contentStart++;
                        if (contentStart < text.Length && text[contentStart] == '\n') contentStart++;

                        result.Append(text[contentStart..closingPos]);
                    }
                    else
                    {
                        result.Append(text[nextBacktick..(closingPos + 3)]);
                    }

                    position = closingPos + 3;
                }
                else
                {
                    var closingPos = text.IndexOf('`', nextBacktick + 1);

                    if (closingPos == -1)
                    {
                        // No closing backtick, escape the rest
                        result.Append(EscapeXmlTags(text[nextBacktick..]));
                        break;
                    }

                    result.Append(text[nextBacktick..(closingPos + 1)]);
                    position = closingPos + 1;
                }
            }

            return result.ToString();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Escapes XML special characters for display outside of code blocks.
        /// </summary>
        /// <param name="text">The text to escape.</param>
        /// <returns>The text with &lt; and &gt; escaped as HTML entities.</returns>
        private static string EscapeXmlTags(string text)
            => text.Replace("<", "&lt;").Replace(">", "&gt;");

        #endregion

    }

}
