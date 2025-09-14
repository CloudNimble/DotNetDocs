using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core.Configuration;

namespace CloudNimble.DotNetDocs.Core.Transformers
{

    /// <summary>
    /// Transforms XML documentation tags in DocEntity properties to Markdown format.
    /// </summary>
    /// <remarks>
    /// This transformer processes all string properties in the DocEntity object graph,
    /// converting XML documentation tags to their Markdown equivalents. It uses performance-optimized
    /// regex patterns to skip strings without XML tags and builds cross-references in a single pass.
    /// </remarks>
    public class MarkdownXmlTransformer : IDocTransformer
    {

        #region Fields

        private readonly CrossReferenceResolver _resolver;
        private readonly ProjectContext _projectContext;
        private string? _currentPath;

        /// <summary>
        /// Compiled regex for quick detection of any XML documentation tags.
        /// </summary>
        private static readonly Regex HasXmlTags = new(
            @"<(?:see|c|code|para|b|i|br|list|item|paramref|typeparamref|exception|returns|summary|remarks|example|value)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Pattern for see cref tags.
        /// </summary>
        private static readonly Regex SeeRefPattern = new(
            @"<see\s+cref=""([^""]+)""\s*/?>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Pattern for see href tags with optional text.
        /// </summary>
        private static readonly Regex SeeHrefPattern = new(
            @"<see\s+href=""([^""]+)""(?:\s*/>|>(.*?)</see>)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Pattern for see langword tags.
        /// </summary>
        private static readonly Regex SeeLangwordPattern = new(
            @"<see\s+langword=""([^""]+)""\s*/?>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Pattern for paramref name tags.
        /// </summary>
        private static readonly Regex ParamRefPattern = new(
            @"<paramref\s+name=""([^""]+)""\s*/?>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Pattern for typeparamref name tags.
        /// </summary>
        private static readonly Regex TypeParamRefPattern = new(
            @"<typeparamref\s+name=""([^""]+)""\s*/?>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Pattern for c inline code tags.
        /// </summary>
        private static readonly Regex InlineCodePattern = new(
            @"<c>(.*?)</c>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Pattern for code block tags with optional language attribute.
        /// </summary>
        private static readonly Regex CodeBlockPattern = new(
            @"<code(?:\s+language=""([^""]+)"")?\s*>(.*?)</code>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Pattern for para tags.
        /// </summary>
        private static readonly Regex ParaPattern = new(
            @"<para>(.*?)</para>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Pattern for bold tags.
        /// </summary>
        private static readonly Regex BoldPattern = new(
            @"<b>(.*?)</b>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Pattern for italic tags.
        /// </summary>
        private static readonly Regex ItalicPattern = new(
            @"<i>(.*?)</i>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Pattern for line break tags.
        /// </summary>
        private static readonly Regex LineBreakPattern = new(
            @"<br\s*/?>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Pattern for list structures.
        /// </summary>
        private static readonly Regex ListPattern = new(
            @"<list\s+type=""(bullet|number|table)""[^>]*>(.*?)</list>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Pattern for list items.
        /// </summary>
        private static readonly Regex ListItemPattern = new(
            @"<item>(?:<term>(.*?)</term>)?(?:<description>(.*?)</description>)?</item>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Pattern for list headers (for tables).
        /// </summary>
        private static readonly Regex ListHeaderPattern = new(
            @"<listheader>(?:<term>(.*?)</term>)?(?:<description>(.*?)</description>)?</listheader>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Performance metrics (optional)
        private int _stringsProcessed;
        private int _stringsWithTags;
        private int _stringsSkipped;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownXmlTransformer"/> class.
        /// </summary>
        /// <param name="projectContext">The project context for configuration and file naming.</param>
        public MarkdownXmlTransformer(ProjectContext projectContext)
        {
            ArgumentNullException.ThrowIfNull(projectContext);
            _projectContext = projectContext;
            _resolver = new CrossReferenceResolver(projectContext);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Transforms a documentation entity by converting XML tags to Markdown.
        /// </summary>
        /// <param name="entity">The documentation entity to transform.</param>
        /// <returns>A task representing the asynchronous transformation operation.</returns>
        public async Task TransformAsync(DocEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            // Build reference map if this is an assembly
            if (entity is DocAssembly assembly)
            {
                _resolver.BuildReferenceMap(assembly);
            }

            var references = new Dictionary<string, DocEntity>();
            await TransformEntityRecursive(entity, references);

            // Optional: Log performance metrics
            if (_stringsProcessed > 0)
            {
                var skipRate = _stringsSkipped * 100.0 / _stringsProcessed;
                Console.WriteLine($"XML Transform Stats: {_stringsSkipped}/{_stringsProcessed} strings skipped ({skipRate:F1}%)");
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Recursively transforms an entity and its children, building references along the way.
        /// </summary>
        protected virtual async Task TransformEntityRecursive(DocEntity entity, Dictionary<string, DocEntity> references)
        {
            // Add entity to references for cross-referencing
            AddToReferences(entity, references);

            // Update current path context
            UpdateCurrentPath(entity);

            // Transform string properties
            entity.Summary = ConvertXmlToMarkdown(entity.Summary, references);
            entity.Remarks = ConvertXmlToMarkdown(entity.Remarks, references);
            entity.Returns = ConvertXmlToMarkdown(entity.Returns, references);
            entity.Usage = ConvertXmlToMarkdown(entity.Usage, references);
            entity.Examples = ConvertXmlToMarkdown(entity.Examples, references);
            entity.Value = ConvertXmlToMarkdown(entity.Value, references);
            entity.BestPractices = ConvertXmlToMarkdown(entity.BestPractices, references);
            entity.Patterns = ConvertXmlToMarkdown(entity.Patterns, references);
            entity.Considerations = ConvertXmlToMarkdown(entity.Considerations, references);

            // Transform collections
            if (entity.Exceptions != null)
            {
                foreach (var exception in entity.Exceptions)
                {
                    exception.Description = ConvertXmlToMarkdown(exception.Description, references);
                }
            }

            if (entity.TypeParameters != null)
            {
                foreach (var typeParam in entity.TypeParameters)
                {
                    typeParam.Description = ConvertXmlToMarkdown(typeParam.Description, references);
                }
            }

            // Resolve SeeAlso references
            if (entity.SeeAlso != null)
            {
                foreach (var seeAlsoRef in entity.SeeAlso)
                {
                    if (!seeAlsoRef.IsResolved)
                    {
                        var resolved = _resolver.ResolveReference(seeAlsoRef.RawReference, _currentPath ?? string.Empty);
                        // Copy resolved properties
                        seeAlsoRef.IsResolved = resolved.IsResolved;
                        seeAlsoRef.ReferenceType = resolved.ReferenceType;
                        seeAlsoRef.RelativePath = resolved.RelativePath;
                        seeAlsoRef.DisplayName = resolved.DisplayName;
                        seeAlsoRef.Anchor = resolved.Anchor;
                        seeAlsoRef.TargetEntity = resolved.TargetEntity;
                    }
                }
            }

            // Handle specific entity types
            if (entity is DocAssembly assembly)
            {
                foreach (var ns in assembly.Namespaces)
                {
                    await TransformEntityRecursive(ns, references);
                }
            }
            else if (entity is DocNamespace ns)
            {
                foreach (var type in ns.Types)
                {
                    await TransformEntityRecursive(type, references);
                }
            }
            else if (entity is DocType type)
            {
                foreach (var member in type.Members)
                {
                    await TransformEntityRecursive(member, references);
                }
            }
            else if (entity is DocMember member)
            {
                foreach (var parameter in member.Parameters)
                {
                    await TransformEntityRecursive(parameter, references);
                }
            }
            else if (entity is DocParameter parameter)
            {
                parameter.Usage = ConvertXmlToMarkdown(parameter.Usage, references);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Adds an entity to the references dictionary for cross-referencing.
        /// </summary>
        protected virtual void AddToReferences(DocEntity entity, Dictionary<string, DocEntity> references)
        {
            if (entity.DisplayName != null && !references.ContainsKey(entity.DisplayName))
            {
                references[entity.DisplayName] = entity;
            }

            // Also add by simple name for easier lookup
            if (entity is DocType type && type.Symbol?.Name != null)
            {
                if (!references.ContainsKey(type.Symbol.Name))
                {
                    references[type.Symbol.Name] = entity;
                }

                // Also add without namespace for simple lookups
                var simpleName = GetSimpleTypeName(type.Symbol.Name);
                if (!references.ContainsKey(simpleName))
                {
                    references[simpleName] = entity;
                }
            }
            else if (entity is DocMember member && member.Symbol?.Name != null)
            {
                if (!references.ContainsKey(member.Symbol.Name))
                {
                    references[member.Symbol.Name] = entity;
                }
            }
        }

        /// <summary>
        /// Main conversion method that orchestrates all XML to Markdown transformations.
        /// </summary>
        protected virtual string? ConvertXmlToMarkdown(string? text, Dictionary<string, DocEntity> references)
        {
            _stringsProcessed++;

            if (string.IsNullOrWhiteSpace(text))
            {
                _stringsSkipped++;
                return text;
            }

            // Quick check - skip processing if no XML tags present
            if (!HasXmlTags.IsMatch(text))
            {
                _stringsSkipped++;
                return text;
            }

            _stringsWithTags++;

            // Process transformations in order of likelihood/importance
            text = ConvertSeeReferences(text, references);
            text = ConvertCodeTags(text);
            text = ConvertReferenceParams(text);
            text = ConvertFormattingTags(text);
            text = ConvertLists(text);

            // Final cleanup - escape any remaining unprocessed XML tags
            text = EscapeRemainingXmlTags(text);

            return text;
        }

        /// <summary>
        /// Converts see reference tags to Markdown links or inline code.
        /// </summary>
        protected virtual string ConvertSeeReferences(string text, Dictionary<string, DocEntity> references)
        {
            // Convert <see cref=""/> tags
            text = SeeRefPattern.Replace(text, match =>
            {
                var typeRef = match.Groups[1].Value;
                var docRef = _resolver.ResolveReference(typeRef, _currentPath ?? string.Empty);
                return docRef.ToMarkdownLink();
            });

            // Convert <see href=""/> tags
            text = SeeHrefPattern.Replace(text, match =>
            {
                var url = match.Groups[1].Value;
                var linkText = match.Groups[2].Success ? match.Groups[2].Value : "link";
                return $"[{linkText}]({url})";
            });

            // Convert <see langword=""/> tags
            text = SeeLangwordPattern.Replace(text, match =>
            {
                var keyword = match.Groups[1].Value.ToLowerInvariant();
                var url = GetLanguageKeywordUrl(keyword);
                return url != null ? $"[`{keyword}`]({url})" : $"`{keyword}`";
            });

            return text;
        }

        /// <summary>
        /// Converts code-related tags to Markdown code formatting.
        /// </summary>
        protected virtual string ConvertCodeTags(string text)
        {
            // Convert <c></c> inline code tags
            text = InlineCodePattern.Replace(text, match =>
            {
                var code = match.Groups[1].Value.Trim();
                return string.IsNullOrEmpty(code) ? "" : $"`{code}`";
            });

            // Convert <code></code> block tags (including those with CDATA)
            text = CodeBlockPattern.Replace(text, match =>
            {
                var language = match.Groups[1].Success ? match.Groups[1].Value : "csharp";
                var code = match.Groups[2].Value;

                // Remove CDATA wrapper if present
                if (code.Contains("<![CDATA["))
                {
                    code = System.Text.RegularExpressions.Regex.Replace(code, @"^\s*<!\[CDATA\[", "", RegexOptions.Singleline);
                    code = System.Text.RegularExpressions.Regex.Replace(code, @"\]\]>\s*$", "", RegexOptions.Singleline);
                }

                // Remove excessive indentation from code blocks
                code = RemoveCommonIndentation(code);

                // Escape any existing triple backticks in the code to prevent Markdown parsing issues
                // Replace ``` with \`\`\` so they display as literal backticks in the code block
                code = code.Replace("```", "\\`\\`\\`");

                return string.IsNullOrEmpty(code) ? "" : $"```{language}\n{code}\n```";
            });

            return text;
        }

        /// <summary>
        /// Removes common leading whitespace from all lines in a code block.
        /// </summary>
        /// <param name="code">The code block to process.</param>
        /// <returns>The code block with common indentation removed.</returns>
        private static string RemoveCommonIndentation(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return code;

            var lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Find the minimum indentation (excluding empty lines)
            var minIndent = int.MaxValue;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var indent = 0;
                foreach (var ch in line)
                {
                    if (ch == ' ' || ch == '\t')
                        indent++;
                    else
                        break;
                }

                if (indent < minIndent)
                    minIndent = indent;
            }

            // If no indentation found, return as-is
            if (minIndent == int.MaxValue || minIndent == 0)
                return code.Trim();

            // Remove common indentation from each line
            var result = new System.Text.StringBuilder();
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (!string.IsNullOrWhiteSpace(line) && line.Length >= minIndent)
                {
                    result.AppendLine(line.Substring(minIndent));
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    // Preserve empty lines but without indentation
                    result.AppendLine();
                }
                else
                {
                    result.AppendLine(line);
                }
            }

            return result.ToString().Trim();
        }

        /// <summary>
        /// Converts parameter and type parameter reference tags.
        /// </summary>
        protected virtual string ConvertReferenceParams(string text)
        {
            // Convert <paramref name=""/> tags
            text = ParamRefPattern.Replace(text, match =>
            {
                var paramName = match.Groups[1].Value;
                return $"*{paramName}*";
            });

            // Convert <typeparamref name=""/> tags
            text = TypeParamRefPattern.Replace(text, match =>
            {
                var typeParamName = match.Groups[1].Value;
                return $"*{typeParamName}*";
            });

            return text;
        }

        /// <summary>
        /// Converts text formatting tags to Markdown equivalents.
        /// </summary>
        protected virtual string ConvertFormattingTags(string text)
        {
            // Convert <para></para> tags
            text = ParaPattern.Replace(text, match =>
            {
                var content = match.Groups[1].Value;
                return $"\n\n{content}\n\n";
            });

            // Convert <b></b> bold tags
            text = BoldPattern.Replace(text, match =>
            {
                var content = match.Groups[1].Value;
                return $"**{content}**";
            });

            // Convert <i></i> italic tags
            text = ItalicPattern.Replace(text, match =>
            {
                var content = match.Groups[1].Value;
                return $"*{content}*";
            });

            // Convert <br/> line break tags
            text = LineBreakPattern.Replace(text, "  \n");

            return text;
        }

        /// <summary>
        /// Converts list structures to Markdown format.
        /// </summary>
        protected virtual string ConvertLists(string text)
        {
            return ListPattern.Replace(text, match =>
            {
                var listType = match.Groups[1].Value.ToLowerInvariant();
                var listContent = match.Groups[2].Value;

                var items = ListItemPattern.Matches(listContent);
                var result = new StringBuilder();

                if (listType == "table")
                {
                    // Check for list header
                    var headerMatch = ListHeaderPattern.Match(listContent);
                    if (headerMatch.Success)
                    {
                        // Table format
                        result.AppendLine();
                        result.AppendLine($"| {headerMatch.Groups[1].Value} | {headerMatch.Groups[2].Value} |");
                        result.AppendLine("|----------|----------|");

                        foreach (Match item in items)
                        {
                            var term = item.Groups[1].Value;
                            var desc = item.Groups[2].Value;
                            result.AppendLine($"| {term} | {desc} |");
                        }
                    }
                    else
                    {
                        // Definition list format
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
                    // Bullet or numbered list
                    var counter = 1;
                    result.AppendLine();

                    foreach (Match item in items)
                    {
                        var description = item.Groups[2].Value;
                        if (string.IsNullOrWhiteSpace(description))
                        {
                            description = item.Groups[1].Value; // Try term if no description
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
        /// Updates the current path context based on the entity being processed.
        /// </summary>
        protected virtual void UpdateCurrentPath(DocEntity entity)
        {
            if (entity is DocNamespace ns)
            {
                var namespaceName = ns.Name ?? "Global";
                var folderPath = _projectContext.GetNamespaceFolderPath(namespaceName);

                if (_projectContext.FileNamingOptions.NamespaceMode == Configuration.NamespaceMode.Folder)
                {
                    _currentPath = System.IO.Path.Combine(folderPath, "index");
                }
                else
                {
                    var fileName = namespaceName.Replace('.', _projectContext.FileNamingOptions.NamespaceSeparator);
                    _currentPath = fileName;
                }
            }
            else if (entity is DocType type && entity.OriginalSymbol?.ContainingNamespace != null)
            {
                var namespaceName = entity.OriginalSymbol.ContainingNamespace.ToDisplayString();
                var typeName = type.Name;
                var folderPath = _projectContext.GetNamespaceFolderPath(namespaceName);

                if (_projectContext.FileNamingOptions.NamespaceMode == Configuration.NamespaceMode.Folder)
                {
                    _currentPath = System.IO.Path.Combine(folderPath, typeName).Replace('\\', '/');
                }
                else
                {
                    _currentPath = typeName;
                }
            }
        }

        /// <summary>
        /// Generates Microsoft Learn documentation URL for a .NET type.
        /// </summary>
        protected virtual string GetMicrosoftDocsUrl(string typeName)
        {
            // Remove generic arity (List`1 becomes List-1)
            typeName = Regex.Replace(typeName, @"`(\d+)", "-$1");

            // Handle nested types (+ becomes .)
            typeName = typeName.Replace('+', '.');

            // Convert to lowercase for URL
            typeName = typeName.ToLowerInvariant();

            return $"https://learn.microsoft.com/dotnet/api/{typeName}";
        }

        /// <summary>
        /// Gets the URL for a language keyword.
        /// </summary>
        protected virtual string? GetLanguageKeywordUrl(string keyword)
        {
            return keyword switch
            {
                "null" => "https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null",
                "true" or "false" => "https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool",
                "void" => "https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/void",
                "async" => "https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/async",
                "await" => "https://learn.microsoft.com/dotnet/csharp/language-reference/operators/await",
                "static" => "https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/static",
                "abstract" => "https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/abstract",
                "virtual" => "https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/virtual",
                "override" => "https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/override",
                "sealed" => "https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/sealed",
                _ => null
            };
        }

        /// <summary>
        /// Determines if a type reference is a .NET Framework type.
        /// </summary>
        protected virtual bool IsFrameworkType(string typeRef)
        {
            return typeRef.StartsWith("System.") ||
                   typeRef.StartsWith("Microsoft.") ||
                   typeRef.StartsWith("Windows.");
        }

        /// <summary>
        /// Gets the simple type name from a fully qualified reference.
        /// </summary>
        protected virtual string GetSimpleTypeName(string typeRef)
        {
            // Remove type prefix if present
            if (typeRef.Contains(':'))
            {
                typeRef = typeRef.Substring(typeRef.IndexOf(':') + 1);
            }

            // Get last part of qualified name
            var lastDot = typeRef.LastIndexOf('.');
            if (lastDot >= 0)
            {
                return typeRef.Substring(lastDot + 1);
            }

            return typeRef;
        }

        /// <summary>
        /// Escapes any remaining XML tags that weren't processed.
        /// </summary>
        protected virtual string EscapeRemainingXmlTags(string text)
        {
            // Use the existing method from RendererBase
            return text.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        #endregion

    }

}