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

        internal void AddToReferenceMap(DocEntity entity, string id, string path)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                _referenceMap[id] = entity;
                _entityPaths[id] = path;
            }
        }

        internal string GetAssemblyDocumentationId(DocAssembly assembly)
        {
            return $"A:{assembly.AssemblyName}";
        }

        internal string GetNamespaceDocumentationId(DocNamespace ns)
        {
            return $"N:{ns.Name}";
        }

        internal string GetTypeDocumentationId(DocType type)
        {
            var fullName = type.FullName ?? type.Name;
            return $"T:{fullName}";
        }

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

        internal string GetRelativePath(string fromPath, string toPath)
        {
            // For now, use a simple ../ prefix
            // In a more sophisticated implementation, we'd calculate the actual relative path
            if (string.IsNullOrWhiteSpace(fromPath) || string.IsNullOrWhiteSpace(toPath))
            {
                return $"../{toPath}";
            }

            // If paths are in the same directory, just use the filename
            var fromDir = Path.GetDirectoryName(fromPath)?.Replace('\\', '/') ?? string.Empty;
            var toDir = Path.GetDirectoryName(toPath)?.Replace('\\', '/') ?? string.Empty;

            if (fromDir == toDir)
            {
                return Path.GetFileName(toPath);
            }

            // Otherwise, use ../ to go up and then the target path
            return $"../{toPath}";
        }

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

        internal string StripPrefix(string reference)
        {
            if (reference.Contains(':'))
            {
                return reference.Substring(reference.IndexOf(':') + 1);
            }
            return reference;
        }

        internal string GetSimpleTypeName(string typeName)
        {
            var lastDot = typeName.LastIndexOf('.');
            if (lastDot >= 0)
            {
                return typeName.Substring(lastDot + 1);
            }
            return typeName;
        }

        internal bool IsFrameworkType(string typeName)
        {
            return typeName.StartsWith("System.") ||
                   typeName.StartsWith("Microsoft.") ||
                   typeName.StartsWith("Windows.");
        }

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