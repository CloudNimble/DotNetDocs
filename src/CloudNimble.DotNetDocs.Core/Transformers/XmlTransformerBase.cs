using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core.Configuration;
using Microsoft.CodeAnalysis;

namespace CloudNimble.DotNetDocs.Core.Transformers
{

    /// <summary>
    /// Provides the format-neutral foundation for transformers that convert XML documentation comments into an output format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class owns everything that does not depend on the output format: walking the documentation model, deciding which
    /// strings contain XML documentation tags, normalizing the indentation inherited from the XML documentation file, indexing
    /// every entity so <c>cref</c> references can be resolved, and building Microsoft Learn URLs for framework types and C# keywords.
    /// </para>
    /// <para>
    /// Derived classes implement <see cref="ConvertXml(string, bool)"/> to turn the XML tags into their own format, such as
    /// Markdown or HTML, and call <see cref="ResolveReference(string, string)"/> to turn a <c>cref</c> into a <see cref="DocReference"/>.
    /// </para>
    /// </remarks>
    public abstract partial class XmlTransformerBase : IDocTransformer
    {

        #region Fields

        private readonly Dictionary<string, string> _entityPaths;
        private readonly Dictionary<string, DocEntity> _referenceMap;
        private int _stringsProcessed;
        private int _stringsSkipped;

        /// <summary>
        /// Microsoft Learn pages for the C# keywords that <c>&lt;see langword="..."/&gt;</c> commonly names, keyed by lowercase keyword.
        /// </summary>
        private static readonly Dictionary<string, string> LanguageKeywordUrls = new(StringComparer.Ordinal)
        {
            ["null"] = "https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null",
            ["true"] = "https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool",
            ["false"] = "https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool",
            ["void"] = "https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/void",
            ["async"] = "https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/async",
            ["await"] = "https://learn.microsoft.com/dotnet/csharp/language-reference/operators/await",
            ["static"] = "https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/static",
            ["abstract"] = "https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/abstract",
            ["virtual"] = "https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/virtual",
            ["override"] = "https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/override",
            ["sealed"] = "https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/sealed"
        };

        /// <summary>
        /// Pattern for the generic arity suffix in a documentation ID, such as the <c>`1</c> in <c>List`1</c>.
        /// </summary>
        [GeneratedRegex(@"`(\d+)")]
        private static partial Regex GenericArityPattern();

        /// <summary>
        /// Compiled regex for quick detection of any XML documentation tags.
        /// </summary>
        [GeneratedRegex(@"<(?:see|c|code|para|b|i|br|list|item|paramref|typeparamref|exception|returns|summary|remarks|example|value)", RegexOptions.IgnoreCase)]
        private static partial Regex HasXmlTags();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the path of the documentation page for the entity currently being transformed.
        /// </summary>
        /// <remarks>
        /// The path is updated when the transformer reaches a namespace or a type. Members and parameters keep the path of the
        /// type that contains them, because they are rendered on the same page.
        /// </remarks>
        protected string CurrentPath { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the project context that supplies the configuration and file naming options.
        /// </summary>
        protected ProjectContext ProjectContext { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlTransformerBase"/> class.
        /// </summary>
        /// <param name="projectContext">The project context for configuration and file naming.</param>
        protected XmlTransformerBase(ProjectContext projectContext)
        {
            ArgumentNullException.ThrowIfNull(projectContext);
            ProjectContext = projectContext;
            _entityPaths = [with(StringComparer.OrdinalIgnoreCase)];
            _referenceMap = [with(StringComparer.OrdinalIgnoreCase)];
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Transforms a documentation entity and all of its children by converting their XML documentation tags.
        /// </summary>
        /// <param name="entity">The documentation entity to transform.</param>
        /// <returns>A task representing the asynchronous transformation operation.</returns>
        /// <remarks>
        /// When <paramref name="entity"/> is a <see cref="DocAssembly"/>, the reference map is rebuilt from it first, so
        /// <c>cref</c> references resolve against the assembly being transformed.
        /// </remarks>
        public async Task TransformAsync(DocEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            if (entity is DocAssembly assembly)
            {
                BuildReferenceMap(assembly);
            }

            await TransformEntityRecursive(entity);

            if (_stringsProcessed > 0)
            {
                var skipRate = _stringsSkipped * 100.0 / _stringsProcessed;
                Console.WriteLine($"XML Transform Stats: {_stringsSkipped}/{_stringsProcessed} strings skipped ({skipRate:F1}%)");
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Converts a documentation string, skipping the conversion when the string has nothing to convert.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <param name="skipEscaping">When true, the text is conceptual content that is already in the output format.</param>
        /// <returns>The converted text, or the original text when it is null, whitespace, or contains no tags.</returns>
        /// <remarks>
        /// Indentation is left as it is. <see cref="AssemblyManager"/> already removed the XML documentation file's indentation
        /// when it extracted the text, and conceptual content is indented on purpose, for example in code blocks and nested lists.
        /// </remarks>
        protected virtual string? ConvertText(string? text, bool skipEscaping = false)
        {
            _stringsProcessed++;

            if (string.IsNullOrWhiteSpace(text))
            {
                _stringsSkipped++;
                return text;
            }

            // Text with HTML tags is still converted, so the derived class can apply its escaping rules to it.
            if (!HasXmlTags().IsMatch(text) && !text.Contains('<'))
            {
                _stringsSkipped++;
                return text;
            }

            return ConvertXml(text, skipEscaping);
        }

        /// <summary>
        /// Converts the XML documentation tags in a string into the output format.
        /// </summary>
        /// <param name="text">The text to convert. It is never null or whitespace and contains at least one tag. Text extracted by <see cref="AssemblyManager"/> has no XML file indentation.</param>
        /// <param name="skipEscaping">When true, the text is conceptual content that is already in the output format, so tags that were not converted must be left as they are.</param>
        /// <returns>The converted text.</returns>
        protected abstract string ConvertXml(string text, bool skipEscaping);

        /// <summary>
        /// Gets the Microsoft Learn URL for a C# language keyword.
        /// </summary>
        /// <param name="keyword">The lowercase keyword, such as <c>null</c> or <c>async</c>.</param>
        /// <returns>The URL for the keyword, or null when the keyword has no dedicated page.</returns>
        protected virtual string? GetLanguageKeywordUrl(string keyword)
        {
            return LanguageKeywordUrls.GetValueOrDefault(keyword);
        }

        /// <summary>
        /// Transforms an entity's documentation strings, resolves its <c>seealso</c> references, and then transforms its children.
        /// </summary>
        /// <param name="entity">The entity to transform.</param>
        /// <returns>A task representing the asynchronous transformation operation.</returns>
        protected virtual async Task TransformEntityRecursive(DocEntity entity)
        {
            UpdateCurrentPath(entity);

            // Strings extracted from the XML documentation file, which may contain XML tags.
            entity.Summary = ConvertText(entity.Summary);
            entity.Remarks = ConvertText(entity.Remarks);
            entity.Returns = ConvertText(entity.Returns);
            entity.Examples = ConvertText(entity.Examples);
            entity.Value = ConvertText(entity.Value);

            // Conceptual content, which is already in the output format.
            entity.Usage = ConvertText(entity.Usage, skipEscaping: true);
            entity.BestPractices = ConvertText(entity.BestPractices, skipEscaping: true);
            entity.Patterns = ConvertText(entity.Patterns, skipEscaping: true);
            entity.Considerations = ConvertText(entity.Considerations, skipEscaping: true);

            if (entity.Exceptions is not null)
            {
                foreach (var exception in entity.Exceptions)
                {
                    exception.Description = ConvertText(exception.Description);
                }
            }

            if (entity.TypeParameters is not null)
            {
                foreach (var typeParam in entity.TypeParameters)
                {
                    typeParam.Description = ConvertText(typeParam.Description);
                }
            }

            if (entity.SeeAlso is not null)
            {
                foreach (var seeAlsoRef in entity.SeeAlso.Where(reference => !reference.IsResolved))
                {
                    var resolved = ResolveReference(seeAlsoRef.RawReference, CurrentPath);
                    seeAlsoRef.IsResolved = resolved.IsResolved;
                    seeAlsoRef.ReferenceType = resolved.ReferenceType;
                    seeAlsoRef.RelativePath = resolved.RelativePath;
                    seeAlsoRef.DisplayName = resolved.DisplayName;
                    seeAlsoRef.Anchor = resolved.Anchor;
                    seeAlsoRef.TargetEntity = resolved.TargetEntity;
                }
            }

            // A parameter's usage comes from its param tag, so it is converted like the other XML documentation strings.
            if (entity is DocParameter parameter)
            {
                parameter.Usage = ConvertText(parameter.Usage);
            }

            foreach (var child in GetChildren(entity))
            {
                await TransformEntityRecursive(child);
            }
        }

        /// <summary>
        /// Updates <see cref="CurrentPath"/> when the transformer reaches a namespace or a type.
        /// </summary>
        /// <param name="entity">The entity being transformed.</param>
        protected virtual void UpdateCurrentPath(DocEntity entity)
        {
            if (entity is DocNamespace ns)
            {
                var folderPath = ProjectContext.GetNamespaceFolderPath(ns.Name);

                CurrentPath = ProjectContext.FileNamingOptions.NamespaceMode == NamespaceMode.Folder
                    ? System.IO.Path.Combine(folderPath, "index")
                    : ns.Name.Replace('.', ProjectContext.FileNamingOptions.NamespaceSeparator);
            }
            else if (entity is DocType type && type.Symbol.ContainingNamespace is not null)
            {
                // Types without a containing namespace, such as array types, keep the path of the page they appear on.
                var namespaceName = type.Symbol.ContainingNamespace.ToDisplayString();
                var folderPath = ProjectContext.GetNamespaceFolderPath(namespaceName);

                CurrentPath = ProjectContext.FileNamingOptions.NamespaceMode == NamespaceMode.Folder
                    ? System.IO.Path.Combine(folderPath, type.Name).Replace('\\', '/')
                    : type.Name;
            }
        }

        /// <summary>
        /// Gets a root-relative path from one document to another, including the API reference path prefix.
        /// </summary>
        /// <param name="fromPath">The current document path. Root-relative paths do not depend on it, but an override that builds document-relative links can use it.</param>
        /// <param name="toPath">The target document path.</param>
        /// <returns>A root-relative path including the API reference path prefix.</returns>
        protected internal virtual string GetRelativePath(string fromPath, string toPath)
        {
            ArgumentNullException.ThrowIfNull(toPath);

            var apiRefPath = ProjectContext.ApiReferencePath.Replace('\\', '/').Trim('/');
            var cleanPath = toPath.Replace('\\', '/').Trim('/');

            // Join only the non-empty segments: an empty ApiReferencePath must not produce "//path", which browsers treat as a host name.
            var segments = new[] { apiRefPath, cleanPath }.Where(segment => !string.IsNullOrWhiteSpace(segment));

            return $"/{string.Join('/', segments)}";
        }

        /// <summary>
        /// Removes the leading whitespace that every non-empty line of a block shares.
        /// </summary>
        /// <param name="code">The block to process.</param>
        /// <returns>The block with the common indentation removed and the result trimmed.</returns>
        protected internal static string RemoveCommonIndentation(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return code;
            }

            var lines = code.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

            var minIndent = int.MaxValue;
            foreach (var line in lines.Where(line => !string.IsNullOrWhiteSpace(line)))
            {
                minIndent = Math.Min(minIndent, GetIndentation(line));
            }

            if (minIndent is int.MaxValue or 0)
            {
                return code.Trim();
            }

            var result = new StringBuilder();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    // Preserve empty lines, but without indentation.
                    result.AppendLine();
                }
                else
                {
                    // Every non-empty line is indented at least minIndent characters, because minIndent is their minimum.
                    result.AppendLine(line[minIndent..]);
                }
            }

            return result.ToString().Trim();
        }

        /// <summary>
        /// Resolves a documentation reference to a <see cref="DocReference"/> with its path, anchor, and display name.
        /// </summary>
        /// <param name="rawReference">The raw reference string, such as <c>T:System.String</c> or <c>F:NamespaceMode.File</c>.</param>
        /// <param name="currentPath">The current document path for calculating relative paths.</param>
        /// <returns>A <see cref="DocReference"/> that is resolved when the target is in the reference map, is a framework type, or is a URL.</returns>
        protected internal DocReference ResolveReference(string rawReference, string currentPath)
        {
            var docRef = new DocReference(rawReference);

            if (string.IsNullOrWhiteSpace(rawReference))
            {
                return docRef;
            }

            if (rawReference.StartsWith("http://") || rawReference.StartsWith("https://"))
            {
                docRef.ReferenceType = ReferenceType.External;
                docRef.RelativePath = rawReference;
                docRef.DisplayName = "link";
                docRef.IsResolved = true;
                return docRef;
            }

            var lookupKey = rawReference;
            if (!_referenceMap.ContainsKey(lookupKey))
            {
                lookupKey = StripPrefix(rawReference);
            }

            if (_referenceMap.TryGetValue(lookupKey, out var entity))
            {
                docRef.TargetEntity = entity;
                docRef.IsResolved = true;
                docRef.DisplayName = GetDisplayName(lookupKey, entity);

                if (_entityPaths.TryGetValue(lookupKey, out var targetPath))
                {
                    docRef.RelativePath = GetRelativePath(currentPath, targetPath);
                }

                docRef.Anchor = GetAnchor(entity);
            }
            else if (IsFrameworkType(lookupKey))
            {
                // Microsoft Learn has one page per method name, covering all its overloads.
                var nameWithoutParameters = StripParameters(lookupKey);
                docRef.ReferenceType = ReferenceType.Framework;
                docRef.RelativePath = GetFrameworkDocumentationUrl(nameWithoutParameters);
                docRef.DisplayName = GetSimpleTypeName(nameWithoutParameters);
                docRef.IsResolved = true;
            }
            else
            {
                docRef.DisplayName = GetSimpleTypeName(StripParameters(lookupKey));
            }

            return docRef;
        }

        /// <summary>
        /// Resolves every reference in a collection of raw reference strings.
        /// </summary>
        /// <param name="rawReferences">The raw reference strings.</param>
        /// <param name="currentPath">The current document path for calculating relative paths.</param>
        /// <returns>The resolved references, in the same order as <paramref name="rawReferences"/>.</returns>
        protected internal ICollection<DocReference> ResolveReferences(IEnumerable<string> rawReferences, string currentPath)
        {
            ArgumentNullException.ThrowIfNull(rawReferences);

            return [.. rawReferences.Select(rawReference => ResolveReference(rawReference, currentPath))];
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Adds an entity to the reference map with its documentation ID and path.
        /// </summary>
        /// <param name="entity">The entity to add to the map.</param>
        /// <param name="id">The documentation ID for the entity.</param>
        /// <param name="path">The relative path to the entity's documentation.</param>
        internal void AddToReferenceMap(DocEntity entity, string id, string path)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                _referenceMap[id] = entity;
                _entityPaths[id] = path;
            }
        }

        /// <summary>
        /// Rebuilds the reference map from a documentation assembly.
        /// </summary>
        /// <param name="assembly">The documentation assembly to index.</param>
        internal void BuildReferenceMap(DocAssembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            _referenceMap.Clear();
            _entityPaths.Clear();

            AddToReferenceMap(assembly, GetAssemblyDocumentationId(assembly), string.Empty);

            foreach (var ns in assembly.Namespaces)
            {
                IndexNamespace(ns);
            }
        }

        /// <summary>
        /// Gets the anchor for a resolved documentation reference.
        /// </summary>
        /// <param name="entity">The resolved entity.</param>
        /// <returns>
        /// The lowercase member name for members, or null for assemblies, namespaces, and types, which have their own pages,
        /// and for enum values, which are rows in their enum's table.
        /// </returns>
        internal string? GetAnchor(DocEntity entity)
        {
            return entity is DocMember member ? member.Name.ToLowerInvariant() : null;
        }

        /// <summary>
        /// Gets the documentation ID for an assembly using the A: prefix.
        /// </summary>
        /// <param name="assembly">The assembly to get the ID for.</param>
        /// <returns>The documentation ID in the format "A:AssemblyName".</returns>
        internal string GetAssemblyDocumentationId(DocAssembly assembly)
        {
            return $"A:{assembly.AssemblyName}";
        }

        /// <summary>
        /// Gets a human-readable display name for a documentation reference.
        /// </summary>
        /// <param name="reference">The raw reference string.</param>
        /// <param name="entity">The resolved entity.</param>
        /// <returns>A display name suitable for rendering in documentation.</returns>
        /// <remarks>
        /// Members and enum values include their type's simple name for clarity, such as <c>NamespaceMode.File</c>.
        /// </remarks>
        internal string GetDisplayName(string reference, DocEntity entity)
        {
            return entity switch
            {
                DocMember member => QualifyWithTypeName(reference, member.Name),
                DocEnumValue value => QualifyWithTypeName(reference, value.Name),
                DocType type => type.Name,
                DocNamespace ns => ns.Name,
                _ => entity.DisplayName ?? GetSimpleTypeName(reference)
            };
        }

        /// <summary>
        /// Generates a Microsoft Learn documentation URL for a .NET type.
        /// </summary>
        /// <param name="typeName">The type's documentation ID without its <c>T:</c> prefix, such as <c>System.Collections.Generic.List`1</c>.</param>
        /// <returns>The URL to the type's documentation on Microsoft Learn.</returns>
        /// <remarks>
        /// Microsoft Learn writes the generic arity as <c>-1</c> instead of <c>`1</c> and nested types with a dot, so
        /// <c>System.Collections.Generic.List`1</c> becomes <c>system.collections.generic.list-1</c>.
        /// </remarks>
        internal static string GetFrameworkDocumentationUrl(string typeName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(typeName);

            var urlName = GenericArityPattern().Replace(typeName, "-$1")
                .Replace('+', '.')
                .ToLowerInvariant();

            return $"https://learn.microsoft.com/dotnet/api/{urlName}";
        }

        /// <summary>
        /// Gets the documentation ID for a member using the appropriate prefix (F:, P:, M:, E:).
        /// </summary>
        /// <param name="member">The member to get the ID for.</param>
        /// <param name="parentType">The parent type containing the member.</param>
        /// <returns>The documentation ID in the format "Prefix:TypeName.MemberName".</returns>
        internal string GetMemberDocumentationId(DocMember member, DocType parentType)
        {
            var fullTypeName = parentType.FullName ?? parentType.Name;
            var prefix = member.MemberKind switch
            {
                SymbolKind.Field => "F:",
                SymbolKind.Property => "P:",
                SymbolKind.Method => "M:",
                SymbolKind.Event => "E:",
                _ => "M:"
            };

            return $"{prefix}{fullTypeName}.{member.Name}";
        }

        /// <summary>
        /// Gets the documentation ID for a namespace using the N: prefix.
        /// </summary>
        /// <param name="ns">The namespace to get the ID for.</param>
        /// <returns>The documentation ID in the format "N:NamespaceName".</returns>
        internal string GetNamespaceDocumentationId(DocNamespace ns)
        {
            return $"N:{ns.Name}";
        }

        /// <summary>
        /// Gets the documentation page path for a namespace.
        /// </summary>
        /// <param name="ns">The namespace to get the path for.</param>
        /// <returns>The relative path to the namespace documentation page.</returns>
        /// <remarks>
        /// Uses <see cref="ProjectContext.GetNamespacePagePath(string?)"/>, the same source the renderers use to write the page.
        /// </remarks>
        internal string GetNamespacePath(DocNamespace ns)
        {
            return ProjectContext.GetNamespacePagePath(ns.Name).Replace('\\', '/');
        }

        /// <summary>
        /// Gets the simple name of a type by removing its namespace.
        /// </summary>
        /// <param name="typeName">The fully qualified type name.</param>
        /// <returns>The type name without its namespace.</returns>
        internal string GetSimpleTypeName(string typeName)
        {
            var lastDot = typeName.LastIndexOf('.');
            return lastDot >= 0 ? typeName[(lastDot + 1)..] : typeName;
        }

        /// <summary>
        /// Gets the documentation ID for a type using the T: prefix.
        /// </summary>
        /// <param name="type">The type to get the ID for.</param>
        /// <returns>The documentation ID in the format "T:FullTypeName".</returns>
        internal string GetTypeDocumentationId(DocType type)
        {
            return $"T:{type.FullName ?? type.Name}";
        }

        /// <summary>
        /// Gets the documentation page path for a type.
        /// </summary>
        /// <param name="type">The type to get the path for.</param>
        /// <param name="ns">The parent namespace of the type.</param>
        /// <returns>The relative path to the type documentation page.</returns>
        /// <remarks>
        /// Uses <see cref="ProjectContext.GetTypePagePath(string?, string)"/>, the same source the renderers use to write the page.
        /// </remarks>
        internal string GetTypePath(DocType type, DocNamespace ns)
        {
            return ProjectContext.GetTypePagePath(ns.Name, type.Name).Replace('\\', '/');
        }

        /// <summary>
        /// Indexes an enum value in the reference map as <c>F:Enum.Value</c> and <c>Enum.Value</c>.
        /// </summary>
        /// <param name="value">The enum value to index.</param>
        /// <param name="parentEnum">The enum that declares the value.</param>
        /// <param name="ns">The namespace containing the enum.</param>
        /// <remarks>
        /// Enum values are rendered in a table on the enum's page, so they resolve to that page with no anchor.
        /// </remarks>
        internal void IndexEnumValue(DocEnumValue value, DocEnum parentEnum, DocNamespace ns)
        {
            var valueId = $"{parentEnum.FullName ?? parentEnum.Name}.{value.Name}";
            var valuePath = GetTypePath(parentEnum, ns);
            AddToReferenceMap(value, $"F:{valueId}", valuePath);

            // No other entity can own "Enum.Value": enums cannot contain types, and a namespace cannot share an enum's full name.
            AddToReferenceMap(value, valueId, valuePath);
        }

        /// <summary>
        /// Indexes a member in the reference map by its type-qualified name and by its Roslyn documentation ID.
        /// </summary>
        /// <param name="member">The member to index.</param>
        /// <param name="parentType">The parent type containing the member.</param>
        /// <param name="ns">The namespace containing the parent type.</param>
        internal void IndexMember(DocMember member, DocType parentType, DocNamespace ns)
        {
            var memberId = GetMemberDocumentationId(member, parentType);

            // Members are rendered on the same page as their type.
            var memberPath = GetTypePath(parentType, ns);
            AddToReferenceMap(member, memberId, memberPath);

            // The compiler writes crefs with Roslyn's documentation ID, which includes the parameter list that tells overloads
            // apart. For an extension method moved to the type it extends, the ID still names the static class that declares it.
            AddToReferenceMap(member, member.Symbol.GetDocumentationCommentId()!, memberPath);
        }

        /// <summary>
        /// Indexes a namespace and all its types in the reference map.
        /// </summary>
        /// <param name="ns">The namespace to index.</param>
        internal void IndexNamespace(DocNamespace ns)
        {
            AddToReferenceMap(ns, GetNamespaceDocumentationId(ns), GetNamespacePath(ns));

            foreach (var type in ns.Types)
            {
                IndexType(type, ns);
            }
        }

        /// <summary>
        /// Indexes a type and all its members in the reference map.
        /// </summary>
        /// <param name="type">The type to index.</param>
        /// <param name="ns">The parent namespace of the type.</param>
        internal void IndexType(DocType type, DocNamespace ns)
        {
            var typeId = GetTypeDocumentationId(type);
            var typePath = GetTypePath(type, ns);
            AddToReferenceMap(type, typeId, typePath);

            // Types are also referenced without the prefix and by their simple name.
            var withoutPrefix = StripPrefix(typeId);
            if (!_referenceMap.ContainsKey(withoutPrefix))
            {
                AddToReferenceMap(type, withoutPrefix, typePath);
            }

            // GetTypePath above has already rejected a type without a name.
            if (!_referenceMap.ContainsKey(type.Name))
            {
                AddToReferenceMap(type, type.Name, typePath);
            }

            foreach (var member in type.Members)
            {
                IndexMember(member, type, ns);
            }

            if (type is DocEnum docEnum)
            {
                foreach (var value in docEnum.Values)
                {
                    IndexEnumValue(value, docEnum, ns);
                }
            }
        }

        /// <summary>
        /// Determines whether a type name belongs to a .NET, Microsoft, or Windows namespace.
        /// </summary>
        /// <param name="typeName">The type name to check.</param>
        /// <returns>True if the type is in a System, Microsoft, or Windows namespace; otherwise, false.</returns>
        internal bool IsFrameworkType(string typeName)
        {
            return typeName.StartsWith("System.") ||
                   typeName.StartsWith("Microsoft.") ||
                   typeName.StartsWith("Windows.");
        }

        /// <summary>
        /// Normalizes indentation artifacts from XML documentation file formatting.
        /// </summary>
        /// <param name="text">The text to normalize.</param>
        /// <returns>The text with XML file indentation removed from all lines.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="AssemblyManager"/> calls this when it extracts text. Trimming the extracted string strips the first line's
        /// leading whitespace but leaves continuation lines with their XML file indentation.
        /// <see cref="RemoveCommonIndentation(string)"/> handles the uniform case (all lines indented);
        /// this method handles the mixed case where the first line has indent 0 but the
        /// rest share a common indent from the XML file structure.
        /// </para>
        /// </remarks>
        internal static string NormalizeXmlDocIndentation(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || !text.Contains('\n'))
            {
                return text;
            }

            var lines = text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

            // Lines without indentation, such as the first line after ExtractInnerXml trims it, are excluded
            // so they don't force the minimum to zero.
            var minIndent = int.MaxValue;
            foreach (var line in lines.Where(line => !string.IsNullOrWhiteSpace(line)))
            {
                var indent = GetIndentation(line);
                if (indent > 0 && indent < minIndent)
                {
                    minIndent = indent;
                }
            }

            if (minIndent == int.MaxValue)
            {
                return text.Trim();
            }

            // Remove the common indentation from indented lines, and leave the others as they are.
            var result = new StringBuilder();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    result.AppendLine();
                }
                else
                {
                    result.AppendLine(GetIndentation(line) >= minIndent ? line[minIndent..] : line);
                }
            }

            return result.ToString().Trim();
        }

        /// <summary>
        /// Removes the parameter list from a method or indexer reference.
        /// </summary>
        /// <param name="reference">The reference, such as <c>M:System.String.Format(System.String,System.Object)</c>.</param>
        /// <returns>The reference up to its parameter list, such as <c>M:System.String.Format</c>, or the reference unchanged when it has none.</returns>
        /// <remarks>
        /// Parameter types contain dots, so names must be split only after the parameter list is removed.
        /// </remarks>
        internal static string StripParameters(string reference)
        {
            var parenthesisIndex = reference.IndexOf('(');
            return parenthesisIndex >= 0 ? reference[..parenthesisIndex] : reference;
        }

        /// <summary>
        /// Strips the documentation prefix (T:, M:, F:, P:, E:, N:, A:) from a reference string.
        /// </summary>
        /// <param name="reference">The reference string that may contain a prefix.</param>
        /// <returns>The reference string without the prefix.</returns>
        internal string StripPrefix(string reference)
        {
            var colonIndex = reference.IndexOf(':');
            return colonIndex >= 0 ? reference[(colonIndex + 1)..] : reference;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the child entities for recursive transformation.
        /// </summary>
        /// <param name="entity">The parent entity.</param>
        /// <returns>The child entities to transform.</returns>
        private static IEnumerable<DocEntity> GetChildren(DocEntity entity) => entity switch
        {
            DocAssembly assembly => assembly.Namespaces,
            DocNamespace ns => ns.Types,
            // The renderers write enum values from Values, so they need the same conversion as members.
            DocEnum docEnum => [.. docEnum.Members, .. docEnum.Values],
            DocType type => type.Members,
            DocMember member => member.Parameters,
            _ => []
        };

        /// <summary>
        /// Counts the leading spaces and tabs on a line.
        /// </summary>
        /// <param name="line">The line to measure.</param>
        /// <returns>The number of leading whitespace characters.</returns>
        private static int GetIndentation(string line)
        {
            return line.Length - line.AsSpan().TrimStart(" \t").Length;
        }

        /// <summary>
        /// Prefixes a member name with the simple name of the type in a reference.
        /// </summary>
        /// <param name="reference">The reference, such as <c>F:Namespace.Type.Member</c>.</param>
        /// <param name="memberName">The member's name.</param>
        /// <returns><c>Type.Member</c>, or the member name alone when the reference has no type qualifier.</returns>
        private string QualifyWithTypeName(string reference, string memberName)
        {
            var withoutPrefix = StripParameters(StripPrefix(reference));
            var lastDot = withoutPrefix.LastIndexOf('.');
            return lastDot > 0
                ? $"{GetSimpleTypeName(withoutPrefix[..lastDot])}.{memberName}"
                : memberName;
        }

        #endregion

    }

}
