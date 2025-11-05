using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        internal readonly DocsJsonManager? _docsJsonManager;

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
                if (_docsJsonManager.Configuration is null)
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

            // Create DocsBadge component snippet for Mintlify (used by inherited/extension member badges)
            await CreateDocsBadgeSnippetAsync();

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

                // Third: Apply NavigationType from template to move root content to Tabs/Products if configured
                ApplyNavigationType();

                // Fourth: Combine referenced navigation
                CombineReferencedNavigation();

                // Write docs.json to the DocumentationRootPath (only once, with everything combined)
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
            if (config.Navigation.Pages is null)
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
                if (existingGroup is not null)
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
                var namespaceName = GetSafeNamespaceName(ns);
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
                var namespaceName = GetSafeNamespaceName(ns);
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

                    if (existingGroup is null)
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
                if (parentGroup is not null && ns.Name == currentPath)
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
                DocNamespace => "Overview" ?? "Namespace",
                DocType dt => dt.Name,
                DocMember member => member.Name,
                _ => entity.GetType().Name
            };
            sb.AppendLine($"title: {title}");

            // Generate description from summary
            if (entity is DocNamespace docNamespace)
            {
                // Hard-code description for namespaces
                var namespaceName = docNamespace.Name ?? "global";
                sb.AppendLine($"description: \"Summary of the {namespaceName} Namespace\"");
            }
            else if (entity is DocType externalType && externalType.IsExternalReference)
            {
                // For external types, use a shorter description without the URL
                sb.AppendLine($"description: \"Extension methods for {externalType.Name} from {externalType.AssemblyName ?? "external assembly"}\"");
            }
            else if (!string.IsNullOrWhiteSpace(entity.Summary))
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
                DocNamespace nsIcon => MintlifyIcons.GetIconForNamespace(nsIcon),
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
                if (type is DocEnum || type.TypeKind == TypeKind.Enum)
                {
                    sb.AppendLine("tag: \"ENUM\"");
                }
                else if (type.Symbol.IsAbstract && type.TypeKind != TypeKind.Interface)
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

                if (parentNamespace is not null)
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
            else if (entity is DocNamespace nsKeywords)
            {
                keywords.Add(nsKeywords.Name ?? "");
                keywords.Add("namespace");

                // Add major type names as keywords
                if (nsKeywords.Types?.Any() == true)
                {
                    keywords.AddRange(nsKeywords.Types.Take(10).Select(t => t.Name));
                }
            }

            if (keywords.Any())
            {
                var keywordList = string.Join(", ", keywords.Distinct().Select(k => $"'{k}'"));
                sb.AppendLine($"keywords: [{keywordList}]");
            }

            sb.AppendLine("---");
            sb.AppendLine();

            // Add snippet import for DocType pages to support DocsBadge component
            if (entity is DocType)
            {
                sb.AppendLine("import { DocsBadge } from '/snippets/DocsBadge.jsx';");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        internal async Task RenderAssemblyAsync(DocAssembly assembly, string outputPath)
        {
            var sb = new StringBuilder();

            // Add frontmatter
            sb.Append(GenerateFrontmatter(assembly));

            if (!string.IsNullOrWhiteSpace(assembly.Summary))
            {
                var header = GetHeaderText(assembly.Summary, SummaryHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
                sb.AppendLine(assembly.Summary);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(assembly.Usage))
            {
                var header = GetHeaderText(assembly.Usage, UsageHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
                sb.AppendLine(assembly.Usage);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(assembly.Remarks))
            {
                var header = GetHeaderText(assembly.Remarks, RemarksHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
                sb.AppendLine(assembly.Remarks);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(assembly.Examples))
            {
                var header = GetHeaderText(assembly.Examples, ExamplesHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
                sb.AppendLine(RemoveIndentation(assembly.Examples));
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(assembly.BestPractices))
            {
                var header = GetHeaderText(assembly.BestPractices, BestPracticesHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
                sb.AppendLine(assembly.BestPractices);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(assembly.Patterns))
            {
                var header = GetHeaderText(assembly.Patterns, PatternsHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
                sb.AppendLine(assembly.Patterns);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(assembly.Considerations))
            {
                var header = GetHeaderText(assembly.Considerations, ConsiderationsHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
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
                    sb.AppendLine($"- {EscapeXmlTagsInString(api)}");
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

            // Get the primary color from the template configuration or use default
            var primaryColor = _options?.Template?.Colors?.Primary ?? "#0D9373";

            // Add frontmatter
            sb.Append(GenerateFrontmatter(ns));

            //sb.AppendLine($"# {ns.Name}");
            //sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(ns.Summary))
            {
                var header = GetHeaderText(ns.Summary, SummaryHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
                sb.AppendLine(ns.Summary);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(ns.Usage))
            {
                var header = GetHeaderText(ns.Usage, UsageHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
                sb.AppendLine(ns.Usage);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(ns.Examples))
            {
                var header = GetHeaderText(ns.Examples, ExamplesHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
                sb.AppendLine(RemoveIndentation(ns.Examples));
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(ns.BestPractices))
            {
                var header = GetHeaderText(ns.BestPractices, BestPracticesHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
                sb.AppendLine(ns.BestPractices);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(ns.Patterns))
            {
                var header = GetHeaderText(ns.Patterns, PatternsHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
                sb.AppendLine(ns.Patterns);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(ns.Considerations))
            {
                var header = GetHeaderText(ns.Considerations, ConsiderationsHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
                sb.AppendLine(ns.Considerations);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(ns.Remarks))
            {
                var header = GetHeaderText(ns.Remarks, RemarksHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
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
                    sb.AppendLine($"- {EscapeXmlTagsInString(api)}");
                }
                sb.AppendLine();
            }

            if (ns.Types.Any())
            {
                sb.AppendLine("## Types");
                sb.AppendLine();

                var classes = ns.Types.Where(t => t.TypeKind == TypeKind.Class).ToList();
                if (classes.Any())
                {
                    sb.AppendLine($"### <Icon icon=\"{MintlifyIcons.Class}\" iconType=\"{MemberIconType}\" color=\"{primaryColor}\" size={{{MemberIconSize}}} style={{{{ paddingRight: '8px' }}}} /> Classes");
                    sb.AppendLine();
                    sb.AppendLine("| Name | Summary |");
                    sb.AppendLine("| ---- | ------- |");
                    foreach (var type in classes)
                    {
                        var typeFilePath = GetTypeFilePath(type, ns, outputPath, "mdx");
                        var typeRelativePath = "/" + Path.GetRelativePath(Context.DocumentationRootPath, typeFilePath)
                            .Replace(Path.DirectorySeparatorChar, '/')
                            .Replace(".mdx", "");
                        var summary = type.Summary?.Replace("\n", " ").Replace("\r", " ").Replace("|", "\\|") ?? "";
                        sb.AppendLine($"| [{type.Name}]({typeRelativePath}) | {summary} |");
                    }
                    sb.AppendLine();
                }

                var interfaces = ns.Types.Where(t => t.TypeKind == TypeKind.Interface).ToList();
                if (interfaces.Any())
                {
                    sb.AppendLine($"### <Icon icon=\"{MintlifyIcons.Interface}\" iconType=\"{MemberIconType}\" color=\"{primaryColor}\" size={{{MemberIconSize}}} style={{{{ paddingRight: '8px' }}}} /> Interfaces");
                    sb.AppendLine();
                    sb.AppendLine("| Name | Summary |");
                    sb.AppendLine("| ---- | ------- |");
                    foreach (var type in interfaces)
                    {
                        var typeFilePath = GetTypeFilePath(type, ns, outputPath, "mdx");
                        var typeRelativePath = "/" + Path.GetRelativePath(Context.DocumentationRootPath, typeFilePath)
                            .Replace(Path.DirectorySeparatorChar, '/')
                            .Replace(".mdx", "");
                        var summary = type.Summary?.Replace("\n", " ").Replace("\r", " ").Replace("|", "\\|") ?? "";
                        sb.AppendLine($"| [{type.Name}]({typeRelativePath}) | {summary} |");
                    }
                    sb.AppendLine();
                }

                var structs = ns.Types.Where(t => t.TypeKind == TypeKind.Struct).ToList();
                if (structs.Any())
                {
                    sb.AppendLine($"### <Icon icon=\"{MintlifyIcons.Struct}\" iconType=\"{MemberIconType}\" color=\"{primaryColor}\" size={{{MemberIconSize}}} style={{{{ paddingRight: '8px' }}}} /> Structs");
                    sb.AppendLine();
                    sb.AppendLine("| Name | Summary |");
                    sb.AppendLine("| ---- | ------- |");
                    foreach (var type in structs)
                    {
                        var typeFilePath = GetTypeFilePath(type, ns, outputPath, "mdx");
                        var typeRelativePath = "/" + Path.GetRelativePath(Context.DocumentationRootPath, typeFilePath)
                            .Replace(Path.DirectorySeparatorChar, '/')
                            .Replace(".mdx", "");
                        var summary = type.Summary?.Replace("\n", " ").Replace("\r", " ").Replace("|", "\\|") ?? "";
                        sb.AppendLine($"| [{type.Name}]({typeRelativePath}) | {summary} |");
                    }
                    sb.AppendLine();
                }

                var enums = ns.Types.Where(t => t is DocEnum || t.TypeKind == TypeKind.Enum).ToList();
                if (enums.Any())
                {
                    sb.AppendLine($"### <Icon icon=\"{MintlifyIcons.Enum}\" iconType=\"{MemberIconType}\" color=\"{primaryColor}\" size={{{MemberIconSize}}} style={{{{ paddingRight: '8px' }}}} /> Enums");
                    sb.AppendLine();
                    sb.AppendLine("| Name | Summary |");
                    sb.AppendLine("| ---- | ------- |");
                    foreach (var type in enums)
                    {
                        var typeFilePath = GetTypeFilePath(type, ns, outputPath, "mdx");
                        var typeRelativePath = "/" + Path.GetRelativePath(Context.DocumentationRootPath, typeFilePath)
                            .Replace(Path.DirectorySeparatorChar, '/')
                            .Replace(".mdx", "");
                        var summary = type.Summary?.Replace("\n", " ").Replace("\r", " ").Replace("|", "\\|") ?? "";
                        sb.AppendLine($"| [{type.Name}]({typeRelativePath}) | {summary} |");
                    }
                    sb.AppendLine();
                }

                var delegates = ns.Types.Where(t => t.TypeKind == TypeKind.Delegate).ToList();
                if (delegates.Any())
                {
                    sb.AppendLine($"### <Icon icon=\"{MintlifyIcons.Delegate}\" iconType=\"{MemberIconType}\" color=\"{primaryColor}\" size={{{MemberIconSize}}} style={{{{ paddingRight: '8px' }}}} /> Delegates");
                    sb.AppendLine();
                    sb.AppendLine("| Name | Summary |");
                    sb.AppendLine("| ---- | ------- |");
                    foreach (var type in delegates)
                    {
                        var typeFilePath = GetTypeFilePath(type, ns, outputPath, "mdx");
                        var typeRelativePath = "/" + Path.GetRelativePath(Context.DocumentationRootPath, typeFilePath)
                            .Replace(Path.DirectorySeparatorChar, '/')
                            .Replace(".mdx", "");
                        var summary = type.Summary?.Replace("\n", " ").Replace("\r", " ").Replace("|", "\\|") ?? "";
                        sb.AppendLine($"| [{type.Name}]({typeRelativePath}) | {summary} |");
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
                var header = GetHeaderText(type.Summary, SummaryHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
                sb.AppendLine(type.Summary);
                sb.AppendLine();
            }


            if (!string.IsNullOrWhiteSpace(type.Usage))
            {
                var header = GetHeaderText(type.Usage, UsageHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
                sb.AppendLine(type.Usage);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(type.Remarks))
            {
                var header = GetHeaderText(type.Remarks, RemarksHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
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
                var header = GetHeaderText(type.Examples, ExamplesHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
                sb.AppendLine(RemoveIndentation(type.Examples));
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(type.BestPractices))
            {
                var header = GetHeaderText(type.BestPractices, BestPracticesHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
                sb.AppendLine(type.BestPractices);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(type.Patterns))
            {
                var header = GetHeaderText(type.Patterns, PatternsHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
                sb.AppendLine(type.Patterns);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(type.Considerations))
            {
                var header = GetHeaderText(type.Considerations, ConsiderationsHeader);
                if (!string.IsNullOrWhiteSpace(header))
                {
                    sb.AppendLine(header);
                    sb.AppendLine();
                }
                sb.AppendLine(type.Considerations);
                sb.AppendLine();
            }

            // Render enum values if this is an enum
            if (type is DocEnum enumType)
            {
                // Render enum values table
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
                var constructors = type.Members.Where(m => m.MemberKind == SymbolKind.Method && m.MethodKind == MethodKind.Constructor).ToList();
                if (constructors.Any())
                {
                    sb.AppendLine("## Constructors");
                    sb.AppendLine();
                    foreach (var ctor in constructors)
                    {
                        RenderMember(sb, ctor);
                    }
                }

                var properties = type.Members.Where(m => m.MemberKind == SymbolKind.Property).ToList();
                if (properties.Any())
                {
                    sb.AppendLine("## Properties");
                    sb.AppendLine();
                    foreach (var prop in properties.OrderBy(p => p.Name))
                    {
                        RenderMember(sb, prop);
                    }
                }

                var methods = type.Members.Where(m => m.MemberKind == SymbolKind.Method && m.MethodKind == MethodKind.Ordinary).ToList();
                if (methods.Any())
                {
                    sb.AppendLine("## Methods");
                    sb.AppendLine();
                    foreach (var method in methods.OrderBy(m => m.Name))
                    {
                        RenderMember(sb, method);
                    }
                }

                var events = type.Members.Where(m => m.MemberKind == SymbolKind.Event).ToList();
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
                    var fields = type.Members.Where(m => m.MemberKind == SymbolKind.Field).ToList();
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
                    sb.AppendLine($"- {EscapeXmlTagsInString(api)}");
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

            // Build badges for member provenance
            var badges = new List<string>();

            if (member.IsExtensionMethod)
            {
                badges.Add("<DocsBadge text=\"Extension\" variant=\"success\" />");
            }

            if (member.IsInherited && !member.IsOverride)
            {
                badges.Add("<DocsBadge text=\"Inherited\" variant=\"neutral\" />");
            }

            if (member.IsOverride)
            {
                badges.Add("<DocsBadge text=\"Override\" variant=\"info\" />");
            }

            if (member.IsVirtual && !member.IsOverride)
            {
                badges.Add("<DocsBadge text=\"Virtual\" variant=\"warning\" />");
            }

            if (member.IsAbstract)
            {
                badges.Add("<DocsBadge text=\"Abstract\" variant=\"warning\" />");
            }

            var badgeString = badges.Any() ? " " + string.Join(" ", badges) : "";

            // Add the member header with icon including iconType, color, size, and margin
            sb.AppendLine($"### <Icon icon=\"{MintlifyIcons.GetIconForMember(member)}\" iconType=\"{MemberIconType}\" color=\"{primaryColor}\" size={{{MemberIconSize}}} style={{{{ paddingRight: '8px' }}}} />  {member.Name}{badgeString}");
            sb.AppendLine();

            // Add provenance note if inherited or extension
            if (member.IsExtensionMethod && !string.IsNullOrWhiteSpace(member.DeclaringTypeName))
            {
                sb.AppendLine($"<Note>Extension method from `{member.DeclaringTypeName}`</Note>");
                sb.AppendLine();
            }
            else if (member.IsInherited && !string.IsNullOrWhiteSpace(member.DeclaringTypeName))
            {
                sb.AppendLine($"<Note>Inherited from `{member.DeclaringTypeName}`</Note>");
                sb.AppendLine();
            }

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
                    sb.AppendLine($"- {EscapeXmlTagsInString(api)}");
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

            if (apiReferenceGroup is not null)
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

            if (assemblyGroup is not null)
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

        #region Internal Methods - Snippet Generation

        /// <summary>
        /// Creates the DocsBadge.jsx component snippet for displaying member provenance badges.
        /// </summary>
        /// <returns>A task representing the asynchronous file write operation.</returns>
        internal async Task CreateDocsBadgeSnippetAsync()
        {
            var snippetsPath = Path.Combine(Context.DocumentationRootPath, "snippets");
            Directory.CreateDirectory(snippetsPath);

            var badgeFilePath = Path.Combine(snippetsPath, "DocsBadge.jsx");

            var badgeComponent = """
/**
 * DocsBadge Component for Mintlify Documentation
 *
 * A customizable badge component that matches Mintlify's design system.
 * Used to display member provenance (Extension, Inherited, Override, Virtual, Abstract).
 *
 * Usage:
 *   <DocsBadge text="Extension" variant="success" />
 *   <DocsBadge text="Inherited" variant="neutral" />
 *   <DocsBadge text="Override" variant="info" />
 *   <DocsBadge text="Virtual" variant="warning" />
 *   <DocsBadge text="Abstract" variant="warning" />
 */

export function DocsBadge({ text, variant = 'neutral' }) {
  // Tailwind color classes for consistent theming
  // Using standard Tailwind colors that work in both light and dark modes
  const variantClasses = {
    success: 'mint-bg-green-500/10 mint-text-green-600 dark:mint-text-green-400 mint-border-green-500/20',
    neutral: 'mint-bg-slate-500/10 mint-text-slate-600 dark:mint-text-slate-400 mint-border-slate-500/20',
    info: 'mint-bg-blue-500/10 mint-text-blue-600 dark:mint-text-blue-400 mint-border-blue-500/20',
    warning: 'mint-bg-amber-500/10 mint-text-amber-600 dark:mint-text-amber-400 mint-border-amber-500/20',
    danger: 'mint-bg-red-500/10 mint-text-red-600 dark:mint-text-red-400 mint-border-red-500/20'
  };

  const classes = variantClasses[variant] || variantClasses.neutral;

  return (
    <span
      className={`mint-inline-flex mint-items-center mint-px-2 mint-py-0.5 mint-rounded-full mint-text-xs mint-font-medium mint-tracking-wide mint-border mint-ml-1.5 mint-align-middle mint-whitespace-nowrap ${classes}`}
    >
      {text}
    </span>
  );
}
""";

            await File.WriteAllTextAsync(badgeFilePath, badgeComponent);
        }

        #endregion

        #region Internal Methods - Navigation Combining

        /// <summary>
        /// Combines navigation from referenced Mintlify projects into the collection's docs.json.
        /// </summary>
        /// <remarks>
        /// This method is called during RenderAsync after all normal rendering is complete but before
        /// saving the docs.json. It loads each referenced docs.json, applies URL prefixes, and adds
        /// the navigation to either Tabs or Products arrays in the existing _docsJsonManager.
        /// The configuration is then saved once at the end of RenderAsync with everything combined.
        /// </remarks>
        internal void CombineReferencedNavigation()
        {
            // Check if we have references and a loaded manager
            if (Context.DocumentationReferences.Count == 0 ||
                _docsJsonManager is null ||
                !_docsJsonManager.IsLoaded ||
                _docsJsonManager.Configuration is null)
            {
                return;
            }

            foreach (var reference in Context.DocumentationReferences)
            {
                // Skip if no navigation file exists
                if (string.IsNullOrWhiteSpace(reference.NavigationFilePath) || !File.Exists(reference.NavigationFilePath))
                {
                    continue;
                }

                // Create a DocsJsonManager for the referenced docs.json
                var refManager = new DocsJsonManager(reference.NavigationFilePath);
                refManager.Load();

                if (!refManager.IsLoaded || refManager.Configuration?.Navigation?.Pages is null)
                {
                    continue;
                }

                // Apply URL prefix to navigation
                refManager.ApplyUrlPrefix(reference.DestinationPath);

                // Add to Tabs or Products based on IntegrationType
                if (reference.IntegrationType.Equals("Products", StringComparison.OrdinalIgnoreCase))
                {
                    AddToProducts(refManager, reference);
                }
                else // Default to Tabs
                {
                    AddToTabs(refManager, reference);
                }
            }
        }

        /// <summary>
        /// Adds referenced documentation to the Tabs array.
        /// </summary>
        /// <param name="source">The referenced documentation's DocsJsonManager.</param>
        /// <param name="reference">The documentation reference metadata.</param>
        internal void AddToTabs(DocsJsonManager source, DocumentationReference reference)
        {
            _docsJsonManager!.Configuration!.Navigation!.Tabs ??= [];

            _docsJsonManager.Configuration.Navigation.Tabs.Add(new TabConfig
            {
                Tab = GetProjectName(reference.ProjectPath),
                Href = reference.DestinationPath,
                Pages = source.Configuration!.Navigation!.Pages
            });
        }

        /// <summary>
        /// Adds referenced documentation to the Products array.
        /// </summary>
        /// <param name="source">The referenced documentation's DocsJsonManager.</param>
        /// <param name="reference">The documentation reference metadata.</param>
        internal void AddToProducts(DocsJsonManager source, DocumentationReference reference)
        {
            _docsJsonManager!.Configuration!.Navigation!.Products ??= [];

            _docsJsonManager.Configuration.Navigation.Products.Add(new ProductConfig
            {
                Product = GetProjectName(reference.ProjectPath),
                Href = reference.DestinationPath,
                Pages = source.Configuration!.Navigation!.Pages,
                Groups = source.Configuration.Navigation.Groups
            });
        }

        /// <summary>
        /// Extracts the project name from a .docsproj file path.
        /// </summary>
        /// <param name="projectPath">The full path to the .docsproj file.</param>
        /// <returns>The project name without extension.</returns>
        internal string GetProjectName(string projectPath)
        {
            return Path.GetFileNameWithoutExtension(projectPath);
        }

        /// <summary>
        /// Applies the NavigationType setting from the template to move the root project's navigation
        /// from Pages to Tabs or Products if configured.
        /// </summary>
        /// <remarks>
        /// This method is called after BuildNavigationStructure and before CombineReferencedNavigation.
        /// It moves the entire Pages navigation (including the API Reference group) into a Tab or Product
        /// based on the NavigationType setting in the template.
        /// </remarks>
        internal void ApplyNavigationType()
        {
            // Check if we have a manager with loaded configuration
            if (_docsJsonManager is null ||
                !_docsJsonManager.IsLoaded ||
                _docsJsonManager.Configuration?.Navigation?.Pages is null ||
                _options.Template is null)
            {
                return;
            }

            var navigationType = _options.Template.NavigationType?.Trim() ?? "Pages";

            // If NavigationType is Pages (default), do nothing
            if (navigationType.Equals("Pages", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Get the current pages and groups to move
            var currentPages = _docsJsonManager.Configuration.Navigation.Pages;
            var currentGroups = _docsJsonManager.Configuration.Navigation.Groups;

            // Determine the name for the root project
            var rootName = !string.IsNullOrWhiteSpace(_options.Template.NavigationName)
                ? _options.Template.NavigationName
                : _options.Template.Name ?? "Documentation";

            // Move to Tabs or Products based on setting
            if (navigationType.Equals("Tabs", StringComparison.OrdinalIgnoreCase))
            {
                // Initialize Tabs if needed
                _docsJsonManager.Configuration.Navigation.Tabs ??= [];

                // Add root content as a new tab
                _docsJsonManager.Configuration.Navigation.Tabs.Add(new TabConfig
                {
                    Tab = rootName,
                    Pages = currentPages
                });

                // Clear the Pages array since content is now in Tabs
                _docsJsonManager.Configuration.Navigation.Pages = [];
            }
            else if (navigationType.Equals("Products", StringComparison.OrdinalIgnoreCase))
            {
                // Initialize Products if needed
                _docsJsonManager.Configuration.Navigation.Products ??= [];

                // Add root content as a new product
                _docsJsonManager.Configuration.Navigation.Products.Add(new ProductConfig
                {
                    Product = rootName,
                    Pages = currentPages,
                    Groups = currentGroups
                });

                // Clear the Pages and Groups arrays since content is now in Products
                _docsJsonManager.Configuration.Navigation.Pages = [];
                _docsJsonManager.Configuration.Navigation.Groups = null;
            }
        }

        #endregion

        #region Private Methods - Placeholder Generation

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
        private async Task GenerateNamespacePlaceholdersAsync(DocNamespace ns, string conceptualPath)
        {
            // Build the namespace directory path
            var namespacePath = Context.GetNamespaceFolderPath(ns.Name ?? "global");
            var namespaceDir = Path.Combine(conceptualPath, namespacePath);

            Console.WriteLine($"📝 Generating namespace placeholders for: {ns.Name} at {namespaceDir}");
            Directory.CreateDirectory(namespaceDir);

            // Generate individual placeholder files
            var summaryPath = Path.Combine(namespaceDir, DocConstants.SummaryFileName);
            if (!File.Exists(summaryPath))
            {
                await File.WriteAllTextAsync(summaryPath, GetSummaryTemplate(ns.Name ?? "global"));
            }

            var usagePath = Path.Combine(namespaceDir, DocConstants.UsageFileName);
            if (!File.Exists(usagePath))
            {
                await File.WriteAllTextAsync(usagePath, GetUsageTemplate(ns.Name ?? "global"));
            }

            var examplesPath = Path.Combine(namespaceDir, DocConstants.ExamplesFileName);
            if (!File.Exists(examplesPath))
            {
                await File.WriteAllTextAsync(examplesPath, GetExamplesTemplate(ns.Name ?? "global"));
            }

            var bestPracticesPath = Path.Combine(namespaceDir, DocConstants.BestPracticesFileName);
            if (!File.Exists(bestPracticesPath))
            {
                await File.WriteAllTextAsync(bestPracticesPath, GetBestPracticesTemplate(ns.Name ?? "global"));
            }

            var patternsPath = Path.Combine(namespaceDir, DocConstants.PatternsFileName);
            if (!File.Exists(patternsPath))
            {
                await File.WriteAllTextAsync(patternsPath, GetPatternsTemplate(ns.Name ?? "global"));
            }

            var considerationsPath = Path.Combine(namespaceDir, DocConstants.ConsiderationsFileName);
            if (!File.Exists(considerationsPath))
            {
                await File.WriteAllTextAsync(considerationsPath, GetConsiderationsTemplate(ns.Name ?? "global"));
            }

            var relatedApisPath = Path.Combine(namespaceDir, DocConstants.RelatedApisFileName);
            if (!File.Exists(relatedApisPath))
            {
                await File.WriteAllTextAsync(relatedApisPath, GetRelatedApisTemplate(ns.Name ?? "global"));
            }
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