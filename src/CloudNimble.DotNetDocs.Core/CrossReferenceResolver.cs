using System;
using System.Collections.Generic;
using System.IO;
using CloudNimble.DotNetDocs.Core.Configuration;
using Microsoft.CodeAnalysis;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Resolves cross-references in documentation to their target entities and generates appropriate links.
    /// </summary>
    /// <remarks>
    /// This class builds a comprehensive map of all documentation entities and their identifiers,
    /// allowing for resolution of see and seealso references to the correct relative paths and anchors.
    /// It handles all types of references including types, members, parameters, and external references.
    /// </remarks>
    public class CrossReferenceResolver
    {

        #region Fields

        private readonly Dictionary<string, DocEntity> _referenceMap;
        private readonly Dictionary<string, string> _entityPaths;
        private readonly ProjectContext _context;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossReferenceResolver"/> class.
        /// </summary>
        /// <param name="context">The project context containing configuration and file naming options.</param>
        public CrossReferenceResolver(ProjectContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            _context = context;
            _referenceMap = new Dictionary<string, DocEntity>(StringComparer.OrdinalIgnoreCase);
            _entityPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Builds the reference map from a documentation assembly.
        /// </summary>
        /// <param name="assembly">The documentation assembly to index.</param>
        public void BuildReferenceMap(DocAssembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            // Clear existing maps
            _referenceMap.Clear();
            _entityPaths.Clear();

            // Index the assembly itself
            AddToReferenceMap(assembly, GetAssemblyDocumentationId(assembly), string.Empty);

            // Index all namespaces and their contents
            foreach (var ns in assembly.Namespaces)
            {
                IndexNamespace(ns);
            }
        }

        /// <summary>
        /// Resolves a documentation reference to a DocReference object with full path and anchor information.
        /// </summary>
        /// <param name="rawReference">The raw reference string (e.g., "T:System.String" or "F:NamespaceMode.File").</param>
        /// <param name="currentPath">The current document path for calculating relative paths.</param>
        /// <returns>A resolved DocReference object.</returns>
        public DocReference ResolveReference(string rawReference, string currentPath)
        {
            var docRef = new DocReference(rawReference);

            if (string.IsNullOrWhiteSpace(rawReference))
            {
                return docRef;
            }

            // Handle external URLs
            if (rawReference.StartsWith("http://") || rawReference.StartsWith("https://"))
            {
                docRef.ReferenceType = ReferenceType.External;
                docRef.RelativePath = rawReference;
                docRef.DisplayName = "link";
                docRef.IsResolved = true;
                return docRef;
            }

            // Try to find the entity in our reference map
            if (_referenceMap.TryGetValue(rawReference, out var entity))
            {
                docRef.TargetEntity = entity;
                docRef.IsResolved = true;
                docRef.DisplayName = GetDisplayName(rawReference, entity);

                // Get the path to the entity's documentation
                if (_entityPaths.TryGetValue(rawReference, out var targetPath))
                {
                    docRef.RelativePath = GetRelativePath(currentPath, targetPath);
                }

                // Extract anchor for member references
                docRef.Anchor = GetAnchor(rawReference, entity);
            }
            else
            {
                // Try without prefix
                var withoutPrefix = StripPrefix(rawReference);
                if (_referenceMap.TryGetValue(withoutPrefix, out entity))
                {
                    docRef.TargetEntity = entity;
                    docRef.IsResolved = true;
                    docRef.DisplayName = GetDisplayName(withoutPrefix, entity);

                    if (_entityPaths.TryGetValue(withoutPrefix, out var targetPath))
                    {
                        docRef.RelativePath = GetRelativePath(currentPath, targetPath);
                    }

                    docRef.Anchor = GetAnchor(withoutPrefix, entity);
                }
                else
                {
                    // Check if it's a .NET Framework type
                    if (IsFrameworkType(withoutPrefix))
                    {
                        docRef.ReferenceType = ReferenceType.Framework;
                        docRef.RelativePath = GetFrameworkDocumentationUrl(withoutPrefix);
                        docRef.DisplayName = GetSimpleTypeName(withoutPrefix);
                        docRef.IsResolved = true;
                    }
                    else
                    {
                        // Couldn't resolve - use simple name as display
                        docRef.DisplayName = GetSimpleTypeName(withoutPrefix);
                    }
                }
            }

            return docRef;
        }

        /// <summary>
        /// Resolves all references in a collection of raw reference strings.
        /// </summary>
        /// <param name="rawReferences">The collection of raw reference strings.</param>
        /// <param name="currentPath">The current document path for calculating relative paths.</param>
        /// <returns>A collection of resolved DocReference objects.</returns>
        public ICollection<DocReference> ResolveReferences(IEnumerable<string> rawReferences, string currentPath)
        {
            var resolved = new List<DocReference>();
            foreach (var rawRef in rawReferences)
            {
                resolved.Add(ResolveReference(rawRef, currentPath));
            }
            return resolved;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Indexes a namespace and all its types in the reference map.
        /// </summary>
        /// <param name="ns">The namespace to index.</param>
        internal void IndexNamespace(DocNamespace ns)
        {
            var nsId = GetNamespaceDocumentationId(ns);
            var nsPath = GetNamespacePath(ns);
            AddToReferenceMap(ns, nsId, nsPath);

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

            // Also add without prefix for easier lookup
            var withoutPrefix = StripPrefix(typeId);
            if (!_referenceMap.ContainsKey(withoutPrefix))
            {
                AddToReferenceMap(type, withoutPrefix, typePath);
            }

            // Add simple name for easier lookup
            if (!string.IsNullOrWhiteSpace(type.Name) && !_referenceMap.ContainsKey(type.Name))
            {
                AddToReferenceMap(type, type.Name, typePath);
            }

            foreach (var member in type.Members)
            {
                IndexMember(member, type, ns);
            }
        }

        /// <summary>
        /// Indexes a member in the reference map with multiple lookup keys.
        /// </summary>
        /// <param name="member">The member to index.</param>
        /// <param name="parentType">The parent type containing the member.</param>
        /// <param name="ns">The namespace containing the parent type.</param>
        internal void IndexMember(DocMember member, DocType parentType, DocNamespace ns)
        {
            var memberId = GetMemberDocumentationId(member, parentType);
            var memberPath = GetTypePath(parentType, ns); // Members are in the same file as their type
            AddToReferenceMap(member, memberId, memberPath);

            // For enum fields, also index with the enum.field pattern
            if (member.MemberKind == SymbolKind.Field && parentType.TypeKind == TypeKind.Enum)
            {
                var enumFieldId = $"{parentType.FullName ?? parentType.Name}.{member.Name}";
                if (!_referenceMap.ContainsKey(enumFieldId))
                {
                    AddToReferenceMap(member, enumFieldId, memberPath);
                }

                // Also add with F: prefix
                var withPrefix = $"F:{enumFieldId}";
                if (!_referenceMap.ContainsKey(withPrefix))
                {
                    AddToReferenceMap(member, withPrefix, memberPath);
                }
            }
        }

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
        /// Gets the documentation ID for an assembly using the A: prefix.
        /// </summary>
        /// <param name="assembly">The assembly to get the ID for.</param>
        /// <returns>The documentation ID in the format "A:AssemblyName".</returns>
        internal string GetAssemblyDocumentationId(DocAssembly assembly)
        {
            return $"A:{assembly.AssemblyName}";
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
        /// Gets the documentation ID for a type using the T: prefix.
        /// </summary>
        /// <param name="type">The type to get the ID for.</param>
        /// <returns>The documentation ID in the format "T:FullTypeName".</returns>
        internal string GetTypeDocumentationId(DocType type)
        {
            var fullName = type.FullName ?? type.Name;
            return $"T:{fullName}";
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

            // For methods, we might need to include parameters, but for now keep it simple
            return $"{prefix}{fullTypeName}.{member.Name}";
        }

        /// <summary>
        /// Gets the documentation file path for a namespace based on the configured naming mode.
        /// </summary>
        /// <param name="ns">The namespace to get the path for.</param>
        /// <returns>The relative path to the namespace documentation file.</returns>
        internal string GetNamespacePath(DocNamespace ns)
        {
            var namespaceName = ns.Name ?? "Global";
            var folderPath = _context.GetNamespaceFolderPath(namespaceName);

            if (_context.FileNamingOptions.NamespaceMode == NamespaceMode.Folder)
            {
                return Path.Combine(folderPath, "index");
            }
            else
            {
                var fileName = namespaceName.Replace('.', _context.FileNamingOptions.NamespaceSeparator);
                return fileName;
            }
        }

        /// <summary>
        /// Gets the documentation file path for a type based on the configured naming mode.
        /// </summary>
        /// <param name="type">The type to get the path for.</param>
        /// <param name="ns">The parent namespace of the type.</param>
        /// <returns>The relative path to the type documentation file.</returns>
        internal string GetTypePath(DocType type, DocNamespace ns)
        {
            var namespaceName = ns.Name ?? "Global";
            var typeName = type.Name;
            var folderPath = _context.GetNamespaceFolderPath(namespaceName);

            if (_context.FileNamingOptions.NamespaceMode == NamespaceMode.Folder)
            {
                return Path.Combine(folderPath, typeName).Replace('\\', '/');
            }
            else
            {
                return typeName;
            }
        }

        /// <summary>
        /// Gets a root-relative path from one document to another, including the API reference path prefix.
        /// </summary>
        /// <param name="fromPath">The current document path (unused for root-relative paths).</param>
        /// <param name="toPath">The target document path.</param>
        /// <returns>A root-relative path including the API reference path prefix.</returns>
        internal string GetRelativePath(string fromPath, string toPath)
        {
            // Use root-relative paths starting with /
            // This ensures all links work regardless of the current document's depth
            if (string.IsNullOrWhiteSpace(toPath))
            {
                return $"/{_context.ApiReferencePath}";
            }

            // Ensure the path starts with the API reference path for root-relative
            var cleanPath = toPath.Replace('\\', '/');

            // Prepend the API reference path
            var apiRefPath = _context.ApiReferencePath?.Replace('\\', '/').Trim('/') ?? "api-reference";
            cleanPath = $"/{apiRefPath}/{cleanPath.TrimStart('/')}";

            return cleanPath;
        }

        /// <summary>
        /// Gets the anchor for a documentation reference, typically for member references.
        /// </summary>
        /// <param name="reference">The raw reference string.</param>
        /// <param name="entity">The resolved entity, if found.</param>
        /// <returns>The anchor string for member references, or null for types and namespaces.</returns>
        internal string? GetAnchor(string reference, DocEntity entity)
        {
            // For member references, generate an anchor
            if (entity is DocMember member)
            {
                return member.Name.ToLowerInvariant();
            }

            // Types and namespaces don't have anchors
            if (entity is DocType || entity is DocNamespace || entity is DocAssembly)
            {
                return null;
            }

            // If we couldn't resolve to a specific entity, check if the reference looks like a member
            // This handles cases where we have a reference string but couldn't find the entity
            var withoutPrefix = StripPrefix(reference);
            var lastDot = withoutPrefix.LastIndexOf('.');
            if (lastDot > 0)
            {
                var possibleMemberName = withoutPrefix.Substring(lastDot + 1);
                // Only consider it a member if it doesn't look like a namespace segment
                if (!string.IsNullOrWhiteSpace(possibleMemberName) &&
                    char.IsLower(possibleMemberName[0]))
                {
                    return possibleMemberName.ToLowerInvariant();
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a human-readable display name for a documentation reference.
        /// </summary>
        /// <param name="reference">The raw reference string.</param>
        /// <param name="entity">The resolved entity.</param>
        /// <returns>A formatted display name suitable for rendering in documentation.</returns>
        internal string GetDisplayName(string reference, DocEntity entity)
        {
            // For members, include the type name for clarity
            if (entity is DocMember member)
            {
                var withoutPrefix = StripPrefix(reference);
                var lastDot = withoutPrefix.LastIndexOf('.');
                if (lastDot > 0)
                {
                    var typePart = withoutPrefix.Substring(0, lastDot);
                    var simpleTypeName = GetSimpleTypeName(typePart);
                    return $"{simpleTypeName}.{member.Name}";
                }
                return member.Name;
            }

            // For types, use the simple name
            if (entity is DocType type)
            {
                return type.Name;
            }

            // For namespaces
            if (entity is DocNamespace ns)
            {
                return ns.Name ?? "Global";
            }

            return entity.DisplayName ?? GetSimpleTypeName(reference);
        }

        /// <summary>
        /// Strips the documentation prefix (T:, M:, F:, P:, E:, N:, A:) from a reference string.
        /// </summary>
        /// <param name="reference">The reference string that may contain a prefix.</param>
        /// <returns>The reference string without the prefix.</returns>
        internal string StripPrefix(string reference)
        {
            if (reference.Contains(':'))
            {
                return reference.Substring(reference.IndexOf(':') + 1);
            }
            return reference;
        }

        /// <summary>
        /// Gets the simple name of a type by removing the namespace prefix.
        /// </summary>
        /// <param name="typeName">The fully qualified type name.</param>
        /// <returns>The simple type name without namespace.</returns>
        internal string GetSimpleTypeName(string typeName)
        {
            var lastDot = typeName.LastIndexOf('.');
            if (lastDot >= 0)
            {
                return typeName.Substring(lastDot + 1);
            }
            return typeName;
        }

        /// <summary>
        /// Determines whether a type name represents a .NET Framework or Microsoft type.
        /// </summary>
        /// <param name="typeName">The type name to check.</param>
        /// <returns>True if the type is from System, Microsoft, or Windows namespaces; otherwise, false.</returns>
        internal bool IsFrameworkType(string typeName)
        {
            return typeName.StartsWith("System.") ||
                   typeName.StartsWith("Microsoft.") ||
                   typeName.StartsWith("Windows.");
        }

        /// <summary>
        /// Generates a Microsoft Learn documentation URL for a .NET Framework type.
        /// </summary>
        /// <param name="typeName">The fully qualified type name.</param>
        /// <returns>The URL to the type's documentation on Microsoft Learn.</returns>
        internal string GetFrameworkDocumentationUrl(string typeName)
        {
            // Remove generic arity - replace `1 with -1, `2 with -2, etc.
            typeName = System.Text.RegularExpressions.Regex.Replace(typeName, @"`(\d+)", "-$1");

            // Handle nested types
            typeName = typeName.Replace('+', '.');

            // Convert to lowercase for URL
            typeName = typeName.ToLowerInvariant();

            return $"https://learn.microsoft.com/dotnet/api/{typeName}";
        }

        #endregion

    }

}