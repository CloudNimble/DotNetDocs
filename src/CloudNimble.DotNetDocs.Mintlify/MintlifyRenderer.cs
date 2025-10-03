using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Core.Renderers;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Options;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace CloudNimble.DotNetDocs.Mintlify
{

    /// <summary>
    /// Renders documentation as MDX files with Mintlify frontmatter and navigation.
    /// </summary>
    /// <remarks>
    /// Generates structured MDX documentation with Mintlify-specific features including
    /// frontmatter with icons, tags, and SEO metadata. Optionally generates docs.json
    /// navigation configuration for Mintlify documentation sites.
    /// </remarks>
    public class MintlifyRenderer : MarkdownRendererBase, IDocRenderer
    {

        #region Fields

        /// <summary>
        /// The icon size to use for member headers (properties, methods, etc.) in the generated MDX.
        /// </summary>
        private const int MemberIconSize = 24;

        /// <summary>
        /// The icon type to use for member headers (properties, methods, etc.) in the generated MDX.
        /// </summary>
        private const string MemberIconType = "duotone";

        private readonly MintlifyRendererOptions _options;
        private readonly DocsJsonManager? _docsJsonManager;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MintlifyRenderer"/> class.
        /// </summary>
        /// <param name="context">The project context.</param>
        /// <param name="options">The Mintlify renderer options.</param>
        /// <param name="docsJsonManager">The DocsJsonManager for navigation generation.</param>
        public MintlifyRenderer(
            ProjectContext context,
            IOptions<MintlifyRendererOptions> options,
            DocsJsonManager docsJsonManager) : base(context)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(docsJsonManager);
            _options = options.Value;
            _docsJsonManager = docsJsonManager;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Renders the documentation assembly to MDX files with optional docs.json generation.
        /// </summary>
        /// <param name="model">The documentation assembly to render.</param>
        /// <returns>A task representing the asynchronous rendering operation.</returns>
        public async Task RenderAsync(DocAssembly model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var apiOutputPath = Path.Combine(Context.DocumentationRootPath, Context.ApiReferencePath);
            Console.WriteLine($"📝 Rendering documentation to: {apiOutputPath}");

            // Initialize DocsJsonManager if enabled (only on first call)
            DocsJsonConfig? docsConfig = null;
            if (_options.GenerateDocsJson && _docsJsonManager is not null)
            {
                // Only initialize if not already configured
                if (_docsJsonManager.Configuration == null)
                {
                    docsConfig = _options.Template ?? DocsJsonManager.CreateDefault(
                        model.AssemblyName ?? "API Documentation",
                        "mint"
                    );
                    // Load configuration directly without JSON round-trip
                    _docsJsonManager.Load(docsConfig);
                }
                else
                {
                    docsConfig = _docsJsonManager.Configuration;
                }
            }

            // Ensure all necessary directories exist based on the file naming mode
            Context.EnsureOutputDirectoryStructure(model, apiOutputPath);

            // Render assembly overview
            await RenderAssemblyAsync(model, apiOutputPath);

            // Render each namespace
            foreach (var ns in model.Namespaces)
            {
                await RenderNamespaceAsync(ns, apiOutputPath);

                // Render each type in the namespace
                foreach (var type in ns.Types)
                {
                    await RenderTypeAsync(type, ns, apiOutputPath);
                }
            }

            // Generate docs.json if enabled
            if (_options.GenerateDocsJson && _docsJsonManager is not null && _docsJsonManager.Configuration is not null)
            {
                // First: Discover existing MDX files in documentation root, preserving template navigation
                _docsJsonManager.PopulateNavigationFromPath(Context.DocumentationRootPath, new[] { ".mdx" }, includeApiReference: false, preserveExisting: true);
                
                // Second: Add API reference content to existing navigation
                BuildNavigationStructure(_docsJsonManager.Configuration, model);
                // Write docs.json to the DocumentationRootPath
                var docsJsonPath = Path.Combine(Context.DocumentationRootPath, "docs.json");
                _docsJsonManager.Save(docsJsonPath);
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Builds the navigation structure from the DocAssembly model.
        /// </summary>
        /// <param name="config">The DocsJsonConfig to populate.</param>
        /// <param name="model">The DocAssembly model containing the documentation structure.</param>
        internal void BuildNavigationStructure(DocsJsonConfig config, DocAssembly model)
        {
            config.Navigation ??= new NavigationConfig();

            // Initialize Pages if null, but don't reset if already populated
            if (config.Navigation.Pages == null)
            {
                config.Navigation.Pages =
                [
                    // Add the index page (assembly overview)
                    "index",
                ];
            }
            else
            {
                // If pages already exists (from PopulateNavigationFromPath),
                // only add index if it's not already known
                if (_docsJsonManager is not null && !_docsJsonManager.IsPathKnown("index"))
                {
                    config.Navigation.Pages.Insert(0, "index");
                }
            }

            // Note: Index page handling is now done by PopulateNavigationFromPath

            if (_options.NavigationMode == NavigationMode.Unified)
            {
                // Find or create the API Reference group - smart merging with template
                var apiReferenceGroup = FindOrCreateApiReferenceGroup(config);

                // Build navigation based on file/folder mode, merging with existing pages
                if (Context.FileNamingOptions.NamespaceMode == NamespaceMode.Folder)
                {
                    BuildFolderModeNavigation(apiReferenceGroup.Pages!, model);
                }
                else
                {
                    BuildFileModeNavigation(apiReferenceGroup.Pages!, model);
                }
            }
            else // NavigationMode.ByAssembly
            {
                // Group namespaces by assembly name
                var namespacesByAssembly = model.Namespaces
                    .SelectMany(ns => ns.Types.Select(t => new { Namespace = ns, Type = t }))
                    .GroupBy(x => x.Type.AssemblyName ?? "Unknown")
                    .OrderBy(g => g.Key);

                foreach (var assemblyGroup in namespacesByAssembly)
                {
                    // Find or create assembly group - smart merging with template
                    var assemblyNav = FindOrCreateAssemblyGroup(config, assemblyGroup.Key);

                    // Get unique namespaces for this assembly
                    var assemblyNamespaces = assemblyGroup
                        .Select(x => x.Namespace)
                        .Distinct()
                        .OrderBy(ns => ns.Name)
                        .ToList();

                    // Build navigation for this assembly's namespaces, merging with existing pages
                    if (Context.FileNamingOptions.NamespaceMode == NamespaceMode.Folder)
                    {
                        BuildFolderModeNavigation(assemblyNav.Pages!, assemblyNamespaces);
                    }
                    else
                    {
                        BuildFileModeNavigation(assemblyNav.Pages!, assemblyNamespaces);
                    }
                }
            }
        }

        /// <summary>
        /// Builds navigation for File mode (flat structure with namespace groups).
        /// </summary>
        /// <param name="pages">The pages list to populate.</param>
        /// <param name="model">The DocAssembly model.</param>
        internal void BuildFileModeNavigation(List<object> pages, DocAssembly model)
        {
            // Process each namespace as a group
            foreach (var ns in model.Namespaces.OrderBy(n => n.Name))
            {
                // Find or create namespace group - preserve existing template groups
                var namespaceDisplayName = ns.Name ?? "global";
                var existingGroup = pages.OfType<GroupConfig>()
                    .FirstOrDefault(g => g.Group == namespaceDisplayName);

                GroupConfig group;
                if (existingGroup != null)
                {
                    group = existingGroup;
                    group.Pages ??= [];

                    // Set icon if not already set and we're including icons
                    if (string.IsNullOrWhiteSpace(group.Icon) && _options.IncludeIcons)
                    {
                        group.Icon = MintlifyIcons.GetIconForNamespace(ns);
                    }
                }
                else
                {
                    group = new GroupConfig
                    {
                        Group = namespaceDisplayName,
                        Icon = _options.IncludeIcons ? MintlifyIcons.GetIconForNamespace(ns) : null,
                        Pages = []
                    };
                    pages.Add(group);
                }

                // Add namespace overview page (if not already present)
                var apiOutputPath = Path.Combine(Context.DocumentationRootPath, Context.ApiReferencePath);
                var nsFilePath = GetNamespaceFilePath(ns, apiOutputPath, "mdx");
                var nsRelativePath = Path.GetRelativePath(Context.DocumentationRootPath, nsFilePath)
                    .Replace(Path.DirectorySeparatorChar, '/')
                    .Replace(".mdx", "");

                if (!group.Pages.OfType<string>().Contains(nsRelativePath))
                {
                    group.Pages.Add(nsRelativePath);
                }

                // Add type pages (if not already present)
                foreach (var type in ns.Types.OrderBy(t => t.Name))
                {
                    var typeFilePath = GetTypeFilePath(type, ns, apiOutputPath, "mdx");
                    var typeRelativePath = Path.GetRelativePath(Context.DocumentationRootPath, typeFilePath)
                        .Replace(Path.DirectorySeparatorChar, '/')
                        .Replace(".mdx", "");

                    if (!group.Pages.OfType<string>().Contains(typeRelativePath))
                    {
                        group.Pages.Add(typeRelativePath);
                    }
                }
            }
        }

        /// <summary>
        /// Builds navigation for File mode (flat structure) for a list of namespaces.
        /// </summary>
        /// <param name="pages">The pages list to populate.</param>
        /// <param name="namespaces">The namespaces to build navigation for.</param>
        internal void BuildFileModeNavigation(List<object> pages, List<DocNamespace> namespaces)
        {
            foreach (var ns in namespaces.OrderBy(n => n.Name))
            {
                var group = new GroupConfig
                {
                    Group = ns.Name ?? "global",
                    Icon = _options.IncludeIcons ? MintlifyIcons.GetIconForNamespace(ns) : null,
                    Pages = []
                };

                // Add namespace overview page
                var apiOutputPath = Path.Combine(Context.DocumentationRootPath, Context.ApiReferencePath);
                var nsFilePath = GetNamespaceFilePath(ns, apiOutputPath, "mdx");
                var nsRelativePath = Path.GetRelativePath(Context.DocumentationRootPath, nsFilePath)
                    .Replace(Path.DirectorySeparatorChar, '/')
                    .Replace(".mdx", "");
                group.Pages.Add(nsRelativePath);

                // Add type pages
                foreach (var type in ns.Types.OrderBy(t => t.Name))
                {
                    var typeFilePath = GetTypeFilePath(type, ns, apiOutputPath, "mdx");
                    var typeRelativePath = Path.GetRelativePath(Context.DocumentationRootPath, typeFilePath)
                        .Replace(Path.DirectorySeparatorChar, '/')
                        .Replace(".mdx", "");
                    group.Pages.Add(typeRelativePath);
                }

                if (group.Pages.Count != 0)
                {
                    pages.Add(group);
                }
            }
        }

        /// <summary>
        /// Builds navigation for Folder mode (hierarchical structure).
        /// </summary>
        /// <param name="pages">The pages list to populate.</param>
        /// <param name="model">The DocAssembly model.</param>
        internal void BuildFolderModeNavigation(List<object> pages, DocAssembly model)
        {
            // Group namespaces by their hierarchical structure
            var namespaceTree = new Dictionary<string, GroupConfig>();

            foreach (var ns in model.Namespaces.OrderBy(n => n.Name))
            {
                var namespaceName = base.GetSafeNamespaceName(ns);
                var parts = namespaceName.Split('.');

                // Build nested structure for namespace hierarchy
                var currentLevel = pages;
                GroupConfig? parentGroup = null;
                string currentPath = "";

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    currentPath = i == 0 ? part : $"{currentPath}.{part}";

                    // Find or create group at this level
                    var existingGroup = currentLevel
                        .OfType<GroupConfig>()
                        .FirstOrDefault(g => g.Group == part);

                    if (existingGroup is null)
                    {
                        existingGroup = new GroupConfig
                        {
                            Group = part,
                            Icon = _options.IncludeIcons ? MintlifyIcons.Namespace : null,
                            Pages = []
                        };
                        currentLevel.Add(existingGroup);
                    }

                    parentGroup = existingGroup;
                    currentLevel = existingGroup.Pages!;
                }

                // At the deepest level, add the namespace index and all types
                if (parentGroup is not null && currentLevel is not null)
                {
                    // Add namespace index
                    var apiOutputPath = Path.Combine(Context.DocumentationRootPath, Context.ApiReferencePath);
                    var nsFilePath = GetNamespaceFilePath(ns, apiOutputPath, "mdx");
                    var nsRelativePath = Path.GetRelativePath(Context.DocumentationRootPath, nsFilePath)
                        .Replace(Path.DirectorySeparatorChar, '/')
                        .Replace(".mdx", "");
                    currentLevel.Add(nsRelativePath);

                    // Add types in this namespace
                    foreach (var type in ns.Types.OrderBy(t => t.Name))
                    {
                        var typeFilePath = GetTypeFilePath(type, ns, apiOutputPath, "mdx");
                        var typeRelativePath = Path.GetRelativePath(Context.DocumentationRootPath, typeFilePath)
                            .Replace(Path.DirectorySeparatorChar, '/')
                            .Replace(".mdx", "");
                        currentLevel.Add(typeRelativePath);
                    }
                }
            }
        }

        /// <summary>
        /// Builds navigation for Folder mode (hierarchical structure) for a list of namespaces.
        /// </summary>
        /// <param name="pages">The pages list to populate.</param>
        /// <param name="namespaces">The namespaces to build navigation for.</param>
        internal void BuildFolderModeNavigation(List<object> pages, List<DocNamespace> namespaces)
        {
            // Group namespaces by their hierarchical structure
            var namespaceTree = new Dictionary<string, GroupConfig>();

            foreach (var ns in namespaces.OrderBy(n => n.Name))
            {
                var namespaceName = base.GetSafeNamespaceName(ns);
                var parts = namespaceName.Split('.');

                // Build nested structure for namespace hierarchy
                var currentLevel = pages;
                GroupConfig? parentGroup = null;
                string currentPath = "";

                for (int i = 0; i < parts.Length; i++)
                {
                    currentPath = i == 0 ? parts[i] : $"{currentPath}.{parts[i]}";

                    // Check if this level already exists
                    var existingGroup = currentLevel?.OfType<GroupConfig>()
                        .FirstOrDefault(g => g.Group == parts[i]);

                    if (existingGroup == null)
                    {
                        // Create new group for this level
                        existingGroup = new GroupConfig
                        {
                            Group = parts[i],
                            Icon = _options.IncludeIcons && i == 0 ? MintlifyIcons.GetIconForNamespace(ns) : null,
                            Pages = []
                        };
                        currentLevel?.Add(existingGroup);
                    }

                    parentGroup = existingGroup;
                    currentLevel = existingGroup.Pages;
                }

                // Add namespace overview page if it's a complete namespace
                if (parentGroup != null && ns.Name == currentPath)
                {
                    var apiOutputPath = Path.Combine(Context.DocumentationRootPath, Context.ApiReferencePath);
                    var nsFilePath = GetNamespaceFilePath(ns, apiOutputPath, "mdx");
                    var nsRelativePath = Path.GetRelativePath(Context.DocumentationRootPath, nsFilePath)
                        .Replace(Path.DirectorySeparatorChar, '/')
                        .Replace(".mdx", "");

                    // Add overview page first
                    currentLevel?.Insert(0, nsRelativePath);

                    // Add type pages
                    foreach (var type in ns.Types.OrderBy(t => t.Name))
                    {
                        var typeFilePath = GetTypeFilePath(type, ns, apiOutputPath, "mdx");
                        var typeRelativePath = Path.GetRelativePath(Context.DocumentationRootPath, typeFilePath)
                            .Replace(Path.DirectorySeparatorChar, '/')
                            .Replace(".mdx", "");
                        currentLevel?.Add(typeRelativePath);
                    }
                }
            }
        }

        /// <summary>
        /// Generates Mintlify frontmatter for any documentation entity.
        /// </summary>
        /// <param name="entity">The documentation entity (assembly, namespace, type, or member).</param>
        /// <param name="parentNamespace">The parent namespace for context (optional).</param>
        /// <returns>The frontmatter YAML string.</returns>
        internal string GenerateFrontmatter(DocEntity entity, DocNamespace? parentNamespace = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("---");

            // Generate title
            string title = entity switch
            {
                DocAssembly assembly => "Overview" ?? "Assembly",
                DocNamespace ns => "Overview" ?? "Namespace",
                DocType dt => dt.Name,
                DocMember member => member.Name,
                _ => entity.GetType().Name
            };
            sb.AppendLine($"title: {title}");

            // Generate description from summary
            if (!string.IsNullOrWhiteSpace(entity.Summary))
            {
                // Clean up the summary for single-line description
                var description = entity.Summary
                    .Replace("\r\n", " ")
                    .Replace("\n", " ")
                    .Replace("\"", "'")
                    .Trim();

                // Limit to 160 characters for SEO
                if (description.Length > 160)
                {
                    description = string.Concat(description.AsSpan(0, 157), "...");
                }

                sb.AppendLine($"description: \"{description}\"");
            }

            // Generate icon based on entity type
            string icon = entity switch
            {
                DocAssembly => MintlifyIcons.Assembly,
                DocNamespace ns => MintlifyIcons.GetIconForNamespace(ns),
                DocType dt => MintlifyIcons.GetIconForType(dt),
                DocMember member => MintlifyIcons.GetIconForMember(member),
                _ => MintlifyIcons.GetIconForEntity(entity)
            };
            sb.AppendLine($"icon: {icon}");

            // Add sidebar title for long type names
            if (entity is DocType docType && docType.Name.Length > 30)
            {
                // Shorten generic types for sidebar
                var sidebarTitle = docType.Name;
                if (sidebarTitle.Contains('<'))
                {
                    sidebarTitle = string.Concat(sidebarTitle.AsSpan(0, sidebarTitle.IndexOf('<')), "<...>");
                }
                sb.AppendLine($"sidebarTitle: {sidebarTitle}");
            }

            // Add tags for special characteristics
            if (entity is DocType type)
            {
                // Check for enum first (including DocEnum instances)
                if (type is DocEnum || type.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum)
                {
                    sb.AppendLine("tag: \"ENUM\"");
                }
                else if (type.Symbol.IsAbstract && type.TypeKind != Microsoft.CodeAnalysis.TypeKind.Interface)
                {
                    sb.AppendLine("tag: \"ABSTRACT\"");
                }
                else if (type.Symbol.IsSealed)
                {
                    sb.AppendLine("tag: \"SEALED\"");
                }
                else if (type.Symbol.IsStatic)
                {
                    sb.AppendLine("tag: \"STATIC\"");
                }
                else if (type.Symbol.GetAttributes().Any(a => a.AttributeClass?.Name == "ObsoleteAttribute"))
                {
                    sb.AppendLine("tag: \"OBSOLETE\"");
                }
            }

            // Add mode for certain page types
            if (entity is DocAssembly || entity is DocNamespace)
            {
                // Use wide mode for overview pages with lots of content
                sb.AppendLine("mode: wide");
            }

            // Add keywords for better searchability
            var keywords = new List<string>();

            if (entity is DocType typeForKeywords)
            {
                keywords.Add(typeForKeywords.Name);

                if (!string.IsNullOrWhiteSpace(typeForKeywords.FullName))
                {
                    keywords.Add(typeForKeywords.FullName);
                }

                if (parentNamespace != null)
                {
                    keywords.Add(parentNamespace.Name ?? "");
                }

                // Add type kind as keyword
                keywords.Add(typeForKeywords.TypeKind.ToString().ToLower());

                // Add base type and interfaces as keywords
                if (!string.IsNullOrWhiteSpace(typeForKeywords.BaseType))
                {
                    keywords.Add(typeForKeywords.BaseType);
                }

                if (typeForKeywords.RelatedApis?.Any() == true)
                {
                    keywords.AddRange(typeForKeywords.RelatedApis.Take(5)); // Limit to avoid too many keywords
                }
            }
            else if (entity is DocNamespace ns)
            {
                keywords.Add(ns.Name ?? "");
                keywords.Add("namespace");

                // Add major type names as keywords
                if (ns.Types?.Any() == true)
                {
                    keywords.AddRange(ns.Types.Take(10).Select(t => t.Name));
                }
            }

            if (keywords.Any())
            {
                var keywordList = string.Join(", ", keywords.Distinct().Select(k => $"'{k}'"));
                sb.AppendLine($"keywords: [{keywordList}]");
            }

            sb.AppendLine("---");
            sb.AppendLine();

            return sb.ToString();
        }

        internal async Task RenderAssemblyAsync(DocAssembly assembly, string outputPath)
        {
            var sb = new StringBuilder();

            // Add frontmatter
            sb.Append(GenerateFrontmatter(assembly));

            //sb.AppendLine($"# {assembly.AssemblyName}");
            //sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(assembly.Summary))
            {
                if (!IsFirstLineHeader(assembly.Summary))
                {
                    sb.AppendLine("## Summary");
                    sb.AppendLine();
                }
                sb.AppendLine(assembly.Summary);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(assembly.Usage))
            {
                if (!IsFirstLineHeader(assembly.Usage))
                {
                    sb.AppendLine("## Usage");
                    sb.AppendLine();
                }
                sb.AppendLine(assembly.Usage);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(assembly.Remarks))
            {
                if (!IsFirstLineHeader(assembly.Remarks))
                {
                    sb.AppendLine("## Remarks");
                    sb.AppendLine();
                }
                sb.AppendLine(assembly.Remarks);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(assembly.Examples))
            {
                if (!IsFirstLineHeader(assembly.Examples))
                {
                    sb.AppendLine("## Examples");
                    sb.AppendLine();
                }
                sb.AppendLine(RemoveIndentation(assembly.Examples));
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(assembly.BestPractices))
            {
                if (!IsFirstLineHeader(assembly.BestPractices))
                {
                    sb.AppendLine("## Best Practices");
                    sb.AppendLine();
                }
                sb.AppendLine(assembly.BestPractices);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(assembly.Patterns))
            {
                if (!IsFirstLineHeader(assembly.Patterns))
                {
                    sb.AppendLine("## Patterns");
                    sb.AppendLine();
                }
                sb.AppendLine(assembly.Patterns);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(assembly.Considerations))
            {
                sb.AppendLine("## Considerations");
                sb.AppendLine();
                sb.AppendLine(assembly.Considerations);
                sb.AppendLine();
            }

            if (assembly.SeeAlso?.Any() == true)
            {
                sb.AppendLine("## See Also");
                sb.AppendLine();
                foreach (var reference in assembly.SeeAlso)
                {
                    sb.AppendLine($"- {reference.ToMarkdownLink()}");
                }
                sb.AppendLine();
            }
            else if (assembly.RelatedApis?.Any() == true)
            {
                sb.AppendLine("## Related APIs");
                sb.AppendLine();
                foreach (var api in assembly.RelatedApis)
                {
                    sb.AppendLine($"- {api}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("## Namespaces");
            sb.AppendLine();
            foreach (var ns in assembly.Namespaces)
            {
                // Get the safe namespace name
                var namespaceName = GetSafeNamespaceName(ns);
                // Get the folder path for the namespace
                var folderPath = Context.GetNamespaceFolderPath(namespaceName);

                // Build the link based on the namespace mode
                string link;
                if (FileNamingOptions.NamespaceMode == NamespaceMode.Folder)
                {
                    // For folder mode, link to the folder (Mintlify will automatically find index.mdx)
                    // Remove trailing slash if present to ensure clean links
                    link = folderPath.Replace('\\', '/').TrimEnd('/');
                }
                else
                {
                    // For file mode, use the file name with the separator
                    link = $"{namespaceName.Replace('.', FileNamingOptions.NamespaceSeparator)}.mdx";
                }

                sb.AppendLine($"- [{ns.Name}]({link})");
            }

            var filePath = Path.Combine(outputPath, "index.mdx");
            await File.WriteAllTextAsync(filePath, sb.ToString());
        }

        internal async Task RenderNamespaceAsync(DocNamespace ns, string outputPath)
        {
            var sb = new StringBuilder();

            // Add frontmatter
            sb.Append(GenerateFrontmatter(ns));

            //sb.AppendLine($"# {ns.Name}");
            //sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(ns.Summary))
            {
                sb.AppendLine("## Summary");
                sb.AppendLine();
                sb.AppendLine(ns.Summary);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(ns.Usage))
            {
                sb.AppendLine("## Usage");
                sb.AppendLine();
                sb.AppendLine(ns.Usage);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(ns.Examples))
            {
                sb.AppendLine("## Examples");
                sb.AppendLine();
                sb.AppendLine(RemoveIndentation(ns.Examples));
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(ns.BestPractices))
            {
                sb.AppendLine("## Best Practices");
                sb.AppendLine();
                sb.AppendLine(ns.BestPractices);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(ns.Patterns))
            {
                sb.AppendLine("## Patterns");
                sb.AppendLine();
                sb.AppendLine(ns.Patterns);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(ns.Considerations))
            {
                sb.AppendLine("## Considerations");
                sb.AppendLine();
                sb.AppendLine(ns.Considerations);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(ns.Remarks))
            {
                sb.AppendLine("## Remarks");
                sb.AppendLine();
                sb.AppendLine(ns.Remarks);
                sb.AppendLine();
            }

            if (ns.SeeAlso?.Any() == true)
            {
                sb.AppendLine("## See Also");
                sb.AppendLine();
                foreach (var reference in ns.SeeAlso)
                {
                    sb.AppendLine($"- {reference.ToMarkdownLink()}");
                }
                sb.AppendLine();
            }
            else if (ns.RelatedApis?.Any() == true)
            {
                sb.AppendLine("## Related APIs");
                sb.AppendLine();
                foreach (var api in ns.RelatedApis)
                {
                    sb.AppendLine($"- {api}");
                }
                sb.AppendLine();
            }

            if (ns.Types.Any())
            {
                sb.AppendLine("## Types");
                sb.AppendLine();

                var classes = ns.Types.Where(t => t.TypeKind == Microsoft.CodeAnalysis.TypeKind.Class).ToList();
                if (classes.Any())
                {
                    sb.AppendLine("### Classes");
                    sb.AppendLine();
                    foreach (var type in classes)
                    {
                        var typeFileName = Path.GetFileName(GetTypeFilePath(type, ns, outputPath, "mdx"));
                        sb.AppendLine($"- [{type.Name}]({typeFileName})");
                    }
                    sb.AppendLine();
                }

                var interfaces = ns.Types.Where(t => t.TypeKind == Microsoft.CodeAnalysis.TypeKind.Interface).ToList();
                if (interfaces.Any())
                {
                    sb.AppendLine("### Interfaces");
                    sb.AppendLine();
                    foreach (var type in interfaces)
                    {
                        var typeFileName = Path.GetFileName(GetTypeFilePath(type, ns, outputPath, "mdx"));
                        sb.AppendLine($"- [{type.Name}]({typeFileName})");
                    }
                    sb.AppendLine();
                }

                var structs = ns.Types.Where(t => t.TypeKind == Microsoft.CodeAnalysis.TypeKind.Struct).ToList();
                if (structs.Any())
                {
                    sb.AppendLine("### Structs");
                    sb.AppendLine();
                    foreach (var type in structs)
                    {
                        var typeFileName = Path.GetFileName(GetTypeFilePath(type, ns, outputPath, "mdx"));
                        sb.AppendLine($"- [{type.Name}]({typeFileName})");
                    }
                    sb.AppendLine();
                }

                var enums = ns.Types.Where(t => t is DocEnum || t.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum).ToList();
                if (enums.Any())
                {
                    sb.AppendLine("### Enums");
                    sb.AppendLine();
                    foreach (var type in enums)
                    {
                        var typeFileName = Path.GetFileName(GetTypeFilePath(type, ns, outputPath, "mdx"));
                        sb.AppendLine($"- [{type.Name}]({typeFileName})");
                    }
                    sb.AppendLine();
                }

                var delegates = ns.Types.Where(t => t.TypeKind == Microsoft.CodeAnalysis.TypeKind.Delegate).ToList();
                if (delegates.Any())
                {
                    sb.AppendLine("### Delegates");
                    sb.AppendLine();
                    foreach (var type in delegates)
                    {
                        var typeFileName = Path.GetFileName(GetTypeFilePath(type, ns, outputPath, "mdx"));
                        sb.AppendLine($"- [{type.Name}]({typeFileName})");
                    }
                    sb.AppendLine();
                }
            }

            var filePath = GetNamespaceFilePath(ns, outputPath, "mdx");
            await File.WriteAllTextAsync(filePath, sb.ToString());
        }

        internal async Task RenderTypeAsync(DocType type, DocNamespace ns, string outputPath)
        {
            var sb = new StringBuilder();

            // Add frontmatter with namespace context
            sb.Append(GenerateFrontmatter(type, ns));

            //sb.AppendLine($"# {type.Name}");
            //sb.AppendLine();

            // Type metadata section
            sb.AppendLine("## Definition");
            sb.AppendLine();
            sb.AppendLine($"**Assembly:** {(type.AssemblyName is not null ? $"{type.AssemblyName}.dll" : "Unknown")}");
            sb.AppendLine();
            sb.AppendLine($"**Namespace:** {ns.Name}");

            if (!string.IsNullOrWhiteSpace(type.BaseType))
            {
                sb.AppendLine();
                // Escape angle brackets for generic types to prevent MDX parsing errors
                sb.AppendLine($"**Inheritance:** {type.BaseType.Replace("<", "&lt;").Replace(">", "&gt;")}");
            }

            // TODO: Add interface information when available in DocType
            sb.AppendLine();

            // Type signature
            sb.AppendLine("## Syntax");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine(type.Signature ?? type.FullName ?? type.Name);
            sb.AppendLine("```");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(type.Summary))
            {
                if (!IsFirstLineHeader(type.Summary))
                {
                    sb.AppendLine("## Summary");
                    sb.AppendLine();
                }
                sb.AppendLine(type.Summary);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(type.Usage))
            {
                if (!IsFirstLineHeader(type.Usage))
                {
                    sb.AppendLine("## Usage");
                    sb.AppendLine();
                }
                sb.AppendLine(type.Usage);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(type.Remarks))
            {
                if (!IsFirstLineHeader(type.Remarks))
                {
                    sb.AppendLine("## Remarks");
                    sb.AppendLine();
                }
                sb.AppendLine(type.Remarks);
                sb.AppendLine();
            }

            // Type Parameters
            if (type.TypeParameters?.Any() == true)
            {
                sb.AppendLine("## Type Parameters");
                sb.AppendLine();
                foreach (var typeParam in type.TypeParameters)
                {
                    sb.AppendLine($"- `{typeParam.Name}` - {typeParam.Description ?? "No description provided"}");
                }
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(type.Examples))
            {
                if (!IsFirstLineHeader(type.Examples))
                {
                    sb.AppendLine("## Examples");
                    sb.AppendLine();
                }
                sb.AppendLine(RemoveIndentation(type.Examples));
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(type.BestPractices))
            {
                if (!IsFirstLineHeader(type.BestPractices))
                {
                    sb.AppendLine("## Best Practices");
                    sb.AppendLine();
                }
                sb.AppendLine(type.BestPractices);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(type.Patterns))
            {
                if (!IsFirstLineHeader(type.Patterns))
                {
                    sb.AppendLine("## Patterns");
                    sb.AppendLine();
                }
                sb.AppendLine(type.Patterns);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(type.Considerations))
            {
                if (!IsFirstLineHeader(type.Considerations))
                {
                    sb.AppendLine("## Considerations");
                    sb.AppendLine();
                }
                sb.AppendLine(type.Considerations);
                sb.AppendLine();
            }

            // Render enum values if this is an enum
            if (type is DocEnum enumType)
            {
                if (enumType.Values.Any())
                {
                    sb.AppendLine("## Values");
                    sb.AppendLine();
                    sb.AppendLine("| Name | Value | Description |");
                    sb.AppendLine("|------|-------|-------------|");
                    foreach (var enumValue in enumType.Values)
                    {
                        sb.AppendLine($"| `{enumValue.Name}` | {enumValue.NumericValue ?? ""} | {enumValue.Summary ?? ""} |");
                    }
                    sb.AppendLine();
                }
            }
            // Render members for non-enum types
            else
            {
                var constructors = type.Members.Where(m => m.MemberKind == Microsoft.CodeAnalysis.SymbolKind.Method && m.MethodKind == Microsoft.CodeAnalysis.MethodKind.Constructor).ToList();
                if (constructors.Any())
                {
                    sb.AppendLine("## Constructors");
                    sb.AppendLine();
                    foreach (var ctor in constructors)
                    {
                        RenderMember(sb, ctor);
                    }
                }

                var properties = type.Members.Where(m => m.MemberKind == Microsoft.CodeAnalysis.SymbolKind.Property).ToList();
                if (properties.Any())
                {
                    sb.AppendLine("## Properties");
                    sb.AppendLine();
                    foreach (var prop in properties.OrderBy(p => p.Name))
                    {
                        RenderMember(sb, prop);
                    }
                }

                var methods = type.Members.Where(m => m.MemberKind == Microsoft.CodeAnalysis.SymbolKind.Method && m.MethodKind == Microsoft.CodeAnalysis.MethodKind.Ordinary).ToList();
                if (methods.Any())
                {
                    sb.AppendLine("## Methods");
                    sb.AppendLine();
                    foreach (var method in methods.OrderBy(m => m.Name))
                    {
                        RenderMember(sb, method);
                    }
                }

                var events = type.Members.Where(m => m.MemberKind == Microsoft.CodeAnalysis.SymbolKind.Event).ToList();
                if (events.Any())
                {
                    sb.AppendLine("## Events");
                    sb.AppendLine();
                    foreach (var evt in events.OrderBy(e => e.Name))
                    {
                        RenderMember(sb, evt);
                    }
                }

                // Only render fields if explicitly requested
                if (Context.IncludeFields)
                {
                    var fields = type.Members.Where(m => m.MemberKind == Microsoft.CodeAnalysis.SymbolKind.Field).ToList();
                    if (fields.Any())
                    {
                        sb.AppendLine("## Fields");
                        sb.AppendLine();
                        foreach (var field in fields.OrderBy(f => f.Name))
                        {
                            RenderMember(sb, field);
                        }
                    }
                }
            }

            // Exceptions for type (if any)
            if (type.Exceptions?.Any() == true)
            {
                sb.AppendLine("## Exceptions");
                sb.AppendLine();
                sb.AppendLine("| Exception | Description |");
                sb.AppendLine("|-----------|-------------|");
                foreach (var exception in type.Exceptions)
                {
                    var description = exception.Description ?? "-";
                    sb.AppendLine($"| `{exception.Type}` | {description} |");
                }
                sb.AppendLine();
            }

            if (type.SeeAlso?.Any() == true)
            {
                sb.AppendLine("## See Also");
                sb.AppendLine();
                foreach (var reference in type.SeeAlso)
                {
                    sb.AppendLine($"- {reference.ToMarkdownLink()}");
                }
                sb.AppendLine();
            }
            else if (type.RelatedApis?.Any() == true)
            {
                sb.AppendLine("## Related APIs");
                sb.AppendLine();
                foreach (var api in type.RelatedApis)
                {
                    sb.AppendLine($"- {api}");
                }
                sb.AppendLine();
            }

            var filePath = GetTypeFilePath(type, ns, outputPath, "mdx");
            await File.WriteAllTextAsync(filePath, sb.ToString());
        }

        internal void RenderMember(StringBuilder sb, DocMember member)
        {
            // Get the primary color from the template configuration or use default
            var primaryColor = _options?.Template?.Colors?.Primary ?? "#0D9373";

            // Add the member header with icon including iconType, color, size, and margin
            sb.AppendLine($"### <Icon icon=\"{MintlifyIcons.GetIconForMember(member)}\" iconType=\"{MemberIconType}\" color=\"{primaryColor}\" size={{{MemberIconSize}}} style={{{{ paddingRight: '8px' }}}} />  {member.Name}");
            sb.AppendLine();

            // Summary/Description
            if (!string.IsNullOrWhiteSpace(member.Summary))
            {
                sb.AppendLine(member.Summary);
                sb.AppendLine();
            }
            else if (!string.IsNullOrWhiteSpace(member.Usage))
            {
                sb.AppendLine(member.Usage);
                sb.AppendLine();
            }

            // Syntax
            sb.AppendLine("#### Syntax");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine(member.Signature ?? member.DisplayName ?? member.Name);
            sb.AppendLine("```");
            sb.AppendLine();

            // Parameters
            if (member.Parameters?.Any() == true)
            {
                sb.AppendLine("#### Parameters");
                sb.AppendLine();
                sb.AppendLine("| Name | Type | Description |");
                sb.AppendLine("|------|------|-------------|");
                foreach (var param in member.Parameters)
                {
                    var paramType = param.TypeName ?? "unknown";
                    var description = !string.IsNullOrWhiteSpace(param.Usage) ? param.Usage : param.Summary ?? "-";
                    sb.AppendLine($"| `{param.Name}` | `{paramType}` | {description} |");
                }
                sb.AppendLine();
            }

            // Returns (for methods only, not properties)
            if (member.MemberKind == SymbolKind.Method &&
                member.ReturnTypeName is not null && member.ReturnTypeName != "void")
            {
                sb.AppendLine("#### Returns");
                sb.AppendLine();
                sb.AppendLine($"Type: `{member.ReturnTypeName}`");
                if (!string.IsNullOrWhiteSpace(member.Returns))
                {
                    sb.AppendLine(member.Returns);
                }
                sb.AppendLine();
            }

            // Property type (for properties)
            if (member.MemberKind == SymbolKind.Property && member.ReturnTypeName is not null)
            {
                sb.AppendLine("#### Property Value");
                sb.AppendLine();
                sb.AppendLine($"Type: `{member.ReturnTypeName}`");
                if (!string.IsNullOrWhiteSpace(member.Value))
                {
                    sb.AppendLine(member.Value);
                }
                sb.AppendLine();
            }

            // Type Parameters
            if (member.TypeParameters?.Any() == true)
            {
                sb.AppendLine("#### Type Parameters");
                sb.AppendLine();
                foreach (var typeParam in member.TypeParameters)
                {
                    sb.AppendLine($"- `{typeParam.Name}` - {typeParam.Description ?? "No description provided"}");
                }
                sb.AppendLine();
            }

            // Exceptions
            if (member.Exceptions?.Any() == true)
            {
                sb.AppendLine("#### Exceptions");
                sb.AppendLine();
                sb.AppendLine("| Exception | Description |");
                sb.AppendLine("|-----------|-------------|");
                foreach (var exception in member.Exceptions)
                {
                    var description = exception.Description ?? "-";
                    sb.AppendLine($"| `{exception.Type}` | {description} |");
                }
                sb.AppendLine();
            }

            // Examples
            if (!string.IsNullOrWhiteSpace(member.Examples))
            {
                sb.AppendLine("#### Examples");
                sb.AppendLine();
                sb.AppendLine(RemoveIndentation(member.Examples));
                sb.AppendLine();
            }

            // Remarks
            if (!string.IsNullOrWhiteSpace(member.Remarks))
            {
                sb.AppendLine("#### Remarks");
                sb.AppendLine();
                sb.AppendLine(member.Remarks);
                sb.AppendLine();
            }
            else if (!string.IsNullOrWhiteSpace(member.BestPractices))
            {
                sb.AppendLine("#### Best Practices");
                sb.AppendLine();
                sb.AppendLine(member.BestPractices);
                sb.AppendLine();
            }

            // Considerations
            if (!string.IsNullOrWhiteSpace(member.Considerations))
            {
                sb.AppendLine("#### Considerations");
                sb.AppendLine();
                sb.AppendLine(member.Considerations);
                sb.AppendLine();
            }

            // See Also
            if (member.SeeAlso?.Any() == true)
            {
                sb.AppendLine("#### See Also");
                sb.AppendLine();
                foreach (var reference in member.SeeAlso)
                {
                    sb.AppendLine($"- {reference.ToMarkdownLink()}");
                }
                sb.AppendLine();
            }
            else if (member.RelatedApis?.Any() == true)
            {
                sb.AppendLine("#### Related APIs");
                sb.AppendLine();
                foreach (var api in member.RelatedApis)
                {
                    sb.AppendLine($"- {api}");
                }
                sb.AppendLine();
            }
        }

        // All signature and file name methods are now inherited from RendererBase

        /// <summary>
        /// Finds or creates the API Reference group, preserving template-defined groups and their order.
        /// </summary>
        /// <param name="config">The docs configuration.</param>
        /// <returns>The API Reference group config.</returns>
        private GroupConfig FindOrCreateApiReferenceGroup(DocsJsonConfig config)
        {
            // First, look for an existing API Reference group (respecting template)
            var apiReferenceGroup = config.Navigation?.Pages?
                .OfType<GroupConfig>()
                .FirstOrDefault(g => g.Group == _options.UnifiedGroupName);

            if (apiReferenceGroup != null)
            {
                // Found existing group - ensure it has a Pages list and preserve existing content
                apiReferenceGroup.Pages ??= [];

                // Set icon if not already set and we're including icons
                if (string.IsNullOrWhiteSpace(apiReferenceGroup.Icon) && _options.IncludeIcons)
                {
                    apiReferenceGroup.Icon = "code";
                }

                return apiReferenceGroup;
            }

            // No existing group found - create new one and add to end
            apiReferenceGroup = new GroupConfig
            {
                Group = _options.UnifiedGroupName,
                Icon = _options.IncludeIcons ? "code" : null,
                Pages = []
            };

            config.Navigation!.Pages!.Add(apiReferenceGroup);
            return apiReferenceGroup;
        }

        /// <summary>
        /// Finds or creates an assembly group, preserving template-defined groups and their order.
        /// </summary>
        /// <param name="config">The docs configuration.</param>
        /// <param name="assemblyName">The assembly name.</param>
        /// <returns>The assembly group config.</returns>
        private GroupConfig FindOrCreateAssemblyGroup(DocsJsonConfig config, string assemblyName)
        {
            // Look for existing assembly group in template
            var assemblyGroup = config.Navigation?.Pages?
                .OfType<GroupConfig>()
                .FirstOrDefault(g => g.Group == assemblyName);

            if (assemblyGroup != null)
            {
                // Found existing group - ensure it has a Pages list and preserve existing content
                assemblyGroup.Pages ??= [];

                // Set icon if not already set and we're including icons
                if (string.IsNullOrWhiteSpace(assemblyGroup.Icon) && _options.IncludeIcons)
                {
                    assemblyGroup.Icon = "package";
                }

                return assemblyGroup;
            }

            // No existing group found - create new one and add to end
            assemblyGroup = new GroupConfig
            {
                Group = assemblyName,
                Icon = _options.IncludeIcons ? "package" : null,
                Pages = []
            };

            config.Navigation!.Pages!.Add(assemblyGroup);
            return assemblyGroup;
        }

        #endregion

        #region IDocRenderer Implementation

        /// <summary>
        /// Renders placeholder conceptual content files for the documentation assembly.
        /// </summary>
        /// <param name="model">The documentation assembly to generate placeholders for.</param>
        /// <returns>A task representing the asynchronous placeholder rendering operation.</returns>
        public async Task RenderPlaceholdersAsync(DocAssembly model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var conceptualPath = Context.ConceptualPath;
            if (string.IsNullOrWhiteSpace(conceptualPath))
            {
                return;
            }

            // Ensure conceptual directory exists
            Directory.CreateDirectory(conceptualPath);

            // Generate placeholders for assembly-level conceptual content
            await GenerateAssemblyPlaceholdersAsync(model, conceptualPath);

            // Generate placeholders for namespace-level conceptual content
            foreach (var ns in model.Namespaces)
            {
                await GenerateNamespacePlaceholdersAsync(ns, conceptualPath);
            }

            // Generate placeholders for type-level conceptual content
            foreach (var ns in model.Namespaces)
            {
                foreach (var type in ns.Types)
                {
                    await GenerateTypePlaceholdersAsync(type, ns, conceptualPath);
                }
            }
        }

        /// <summary>
        /// Generates placeholder files for assembly-level conceptual content.
        /// </summary>
        /// <param name="assembly">The assembly to generate placeholders for.</param>
        /// <param name="conceptualPath">The base conceptual content path.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task GenerateAssemblyPlaceholdersAsync(DocAssembly assembly, string conceptualPath)
        {
            // Assembly-level placeholders would go in the root conceptual directory
            // For now, we'll focus on type-level placeholders as requested
            return Task.CompletedTask;
        }

        /// <summary>
        /// Generates placeholder files for namespace-level conceptual content.
        /// </summary>
        /// <param name="ns">The namespace to generate placeholders for.</param>
        /// <param name="conceptualPath">The base conceptual content path.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task GenerateNamespacePlaceholdersAsync(DocNamespace ns, string conceptualPath)
        {
            // Namespace-level placeholders would go in namespace-specific directories
            // For now, we'll focus on type-level placeholders as requested
            return Task.CompletedTask;
        }

        /// <summary>
        /// Generates placeholder files for type-level conceptual content.
        /// </summary>
        /// <param name="type">The type to generate placeholders for.</param>
        /// <param name="ns">The namespace containing the type.</param>
        /// <param name="conceptualPath">The base conceptual content path.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task GenerateTypePlaceholdersAsync(DocType type, DocNamespace ns, string conceptualPath)
        {
            // Build the type directory path
            var namespacePath = Context.GetNamespaceFolderPath(ns.Name ?? "global");
            var typeDir = Path.Combine(conceptualPath, namespacePath, type.Name);

            Directory.CreateDirectory(typeDir);

            // Generate individual placeholder files
            var usagePath = Path.Combine(typeDir, DocConstants.UsageFileName);
            if (!File.Exists(usagePath))
            {
                await File.WriteAllTextAsync(usagePath, GetUsageTemplate(type.Name));
            }

            var examplesPath = Path.Combine(typeDir, DocConstants.ExamplesFileName);
            if (!File.Exists(examplesPath))
            {
                await File.WriteAllTextAsync(examplesPath, GetExamplesTemplate(type.Name));
            }

            var bestPracticesPath = Path.Combine(typeDir, DocConstants.BestPracticesFileName);
            if (!File.Exists(bestPracticesPath))
            {
                await File.WriteAllTextAsync(bestPracticesPath, GetBestPracticesTemplate(type.Name));
            }

            var patternsPath = Path.Combine(typeDir, DocConstants.PatternsFileName);
            if (!File.Exists(patternsPath))
            {
                await File.WriteAllTextAsync(patternsPath, GetPatternsTemplate(type.Name));
            }

            var considerationsPath = Path.Combine(typeDir, DocConstants.ConsiderationsFileName);
            if (!File.Exists(considerationsPath))
            {
                await File.WriteAllTextAsync(considerationsPath, GetConsiderationsTemplate(type.Name));
            }

            var relatedApisPath = Path.Combine(typeDir, DocConstants.RelatedApisFileName);
            if (!File.Exists(relatedApisPath))
            {
                await File.WriteAllTextAsync(relatedApisPath, GetRelatedApisTemplate(type.Name));
            }
        }

        #endregion

    }

}