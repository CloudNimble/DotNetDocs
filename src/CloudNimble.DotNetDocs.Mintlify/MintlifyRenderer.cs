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
    public class MintlifyRenderer : RendererBase, IDocRenderer
    {

        #region Fields

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
        /// <param name="outputPath">The path where MDX files should be generated.</param>
        /// <param name="context">The project context providing rendering settings.</param>
        /// <returns>A task representing the asynchronous rendering operation.</returns>
        public async Task RenderAsync(DocAssembly model, string outputPath, ProjectContext context)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(outputPath);
            ArgumentNullException.ThrowIfNull(context);

            // Initialize DocsJsonManager if enabled
            DocsJsonConfig? docsConfig = null;
            if (_options.GenerateDocsJson && _docsJsonManager is not null)
            {
                docsConfig = _options.Template ?? DocsJsonManager.CreateDefault(
                    model.AssemblyName ?? "API Documentation",
                    "mint"
                );
                // Load configuration using JSON serialization
                var json = System.Text.Json.JsonSerializer.Serialize(docsConfig, global::Mintlify.Core.MintlifyConstants.JsonSerializerOptions);
                _docsJsonManager.Load(json);
            }

            // Ensure all necessary directories exist based on the file naming mode
            Context.EnsureOutputDirectoryStructure(model, outputPath);

            // Render assembly overview
            await RenderAssemblyAsync(model, outputPath);

            // Render each namespace
            foreach (var ns in model.Namespaces)
            {
                await RenderNamespaceAsync(ns, outputPath);

                // Render each type in the namespace
                foreach (var type in ns.Types)
                {
                    await RenderTypeAsync(type, ns, outputPath);
                }
            }

            // Generate docs.json if enabled
            if (_options.GenerateDocsJson && _docsJsonManager is not null && _docsJsonManager.Configuration is not null)
            {
                BuildNavigationStructure(_docsJsonManager.Configuration, model, outputPath);
                var docsJsonPath = Path.Combine(outputPath, "docs.json");
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
        /// <param name="outputPath">The output directory path.</param>
        internal void BuildNavigationStructure(DocsJsonConfig config, DocAssembly model, string outputPath)
        {
            config.Navigation ??= new NavigationConfig();
            config.Navigation.Pages = new List<object>();

            // Add the index page (assembly overview)
            config.Navigation.Pages.Add("index");

            // Create "API Reference" root group
            var apiReferenceGroup = new GroupConfig
            {
                Group = "API Reference",
                Icon = _options.IncludeIcons ? "code" : null,
                Pages = new List<object>()
            };

            // Build navigation based on file/folder mode
            if (Context.FileNamingOptions.NamespaceMode == NamespaceMode.Folder)
            {
                BuildFolderModeNavigation(apiReferenceGroup.Pages, model, outputPath);
            }
            else
            {
                BuildFileModeNavigation(apiReferenceGroup.Pages, model, outputPath);
            }

            // Only add the API Reference group if it has content
            if (apiReferenceGroup.Pages.Any())
            {
                config.Navigation.Pages.Add(apiReferenceGroup);
            }
        }

        /// <summary>
        /// Builds navigation for File mode (flat structure with namespace groups).
        /// </summary>
        /// <param name="pages">The pages list to populate.</param>
        /// <param name="model">The DocAssembly model.</param>
        /// <param name="outputPath">The output directory path.</param>
        internal void BuildFileModeNavigation(List<object> pages, DocAssembly model, string outputPath)
        {
            // Process each namespace as a group
            foreach (var ns in model.Namespaces.OrderBy(n => n.Name))
            {
                var namespaceName = base.GetSafeNamespaceName(ns);
                var group = new GroupConfig
                {
                    Group = ns.Name ?? "global",
                    Icon = _options.IncludeIcons ? MintlifyIcons.GetIconForNamespace(ns) : null,
                    Pages = new List<object>()
                };

                // Add namespace overview page
                var nsFilePath = GetNamespaceFilePath(ns, outputPath, "mdx");
                var nsRelativePath = Path.GetRelativePath(outputPath, nsFilePath)
                    .Replace('\\', '/')
                    .Replace(".mdx", "");
                group.Pages.Add(nsRelativePath);

                // Add type pages
                foreach (var type in ns.Types.OrderBy(t => t.Name))
                {
                    var typeFilePath = GetTypeFilePath(type, ns, outputPath, "mdx");
                    var typeRelativePath = Path.GetRelativePath(outputPath, typeFilePath)
                        .Replace('\\', '/')
                        .Replace(".mdx", "");
                    group.Pages.Add(typeRelativePath);
                }

                if (group.Pages.Any())
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
        /// <param name="outputPath">The output directory path.</param>
        internal void BuildFolderModeNavigation(List<object> pages, DocAssembly model, string outputPath)
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
                    var nsFilePath = GetNamespaceFilePath(ns, outputPath, "mdx");
                    var nsRelativePath = Path.GetRelativePath(outputPath, nsFilePath)
                        .Replace('\\', '/')
                        .Replace(".mdx", "");
                    currentLevel.Add(nsRelativePath);

                    // Add types in this namespace
                    foreach (var type in ns.Types.OrderBy(t => t.Name))
                    {
                        var typeFilePath = GetTypeFilePath(type, ns, outputPath, "mdx");
                        var typeRelativePath = Path.GetRelativePath(outputPath, typeFilePath)
                            .Replace('\\', '/')
                            .Replace(".mdx", "");
                        currentLevel.Add(typeRelativePath);
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
                DocAssembly assembly => assembly.AssemblyName ?? "Assembly",
                DocNamespace ns => ns.Name ?? "Namespace",
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
                    description = description.Substring(0, 157) + "...";
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
                    sidebarTitle = sidebarTitle.Substring(0, sidebarTitle.IndexOf('<')) + "<...>";
                }
                sb.AppendLine($"sidebarTitle: {sidebarTitle}");
            }

            // Add tags for special characteristics
            if (entity is DocType type)
            {
                if (type.Symbol.IsAbstract && type.TypeKind != Microsoft.CodeAnalysis.TypeKind.Interface)
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

            sb.AppendLine($"# {assembly.AssemblyName}");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(assembly.Summary))
            {
                sb.AppendLine("## Summary");
                sb.AppendLine();
                sb.AppendLine(assembly.Summary);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(assembly.Usage))
            {
                sb.AppendLine("## Usage");
                sb.AppendLine();
                sb.AppendLine(assembly.Usage);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(assembly.Remarks))
            {
                sb.AppendLine("## Remarks");
                sb.AppendLine();
                sb.AppendLine(assembly.Remarks);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(assembly.Examples))
            {
                sb.AppendLine("## Examples");
                sb.AppendLine();
                sb.AppendLine("```csharp");
                sb.AppendLine(RemoveIndentation(assembly.Examples));
                sb.AppendLine("```");
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(assembly.BestPractices))
            {
                sb.AppendLine("## Best Practices");
                sb.AppendLine();
                sb.AppendLine(assembly.BestPractices);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(assembly.Patterns))
            {
                sb.AppendLine("## Patterns");
                sb.AppendLine();
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
                foreach (var api in assembly.SeeAlso)
                {
                    sb.AppendLine($"- {api}");
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
                var nsFileName = Path.GetFileName(GetNamespaceFilePath(ns, outputPath, "mdx"));
                sb.AppendLine($"- [{ns.Name}]({nsFileName})");
            }

            var filePath = Path.Combine(outputPath, "index.mdx");
            await File.WriteAllTextAsync(filePath, sb.ToString());
        }

        internal async Task RenderNamespaceAsync(DocNamespace ns, string outputPath)
        {
            var sb = new StringBuilder();

            // Add frontmatter
            sb.Append(GenerateFrontmatter(ns));

            sb.AppendLine($"# {ns.Name}");
            sb.AppendLine();

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
                sb.AppendLine("```csharp");
                sb.AppendLine(RemoveIndentation(ns.Examples));
                sb.AppendLine("```");
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
                foreach (var api in ns.SeeAlso)
                {
                    sb.AppendLine($"- {api}");
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

                var enums = ns.Types.Where(t => t.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum).ToList();
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
            sb.AppendLine($"**Namespace:** {ns.Name}");
            sb.AppendLine();
            sb.AppendLine($"**Assembly:** {(type.AssemblyName is not null ? $"{type.AssemblyName}.dll" : "Unknown")}");

            if (!string.IsNullOrWhiteSpace(type.BaseType))
            {
                sb.AppendLine();
                sb.AppendLine($"**Inheritance:** {type.BaseType}");
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
                sb.AppendLine("## Summary");
                sb.AppendLine();
                sb.AppendLine(type.Summary);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(type.Usage))
            {
                sb.AppendLine("## Usage");
                sb.AppendLine();
                sb.AppendLine(type.Usage);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(type.Remarks))
            {
                sb.AppendLine("## Remarks");
                sb.AppendLine();
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
                sb.AppendLine("## Examples");
                sb.AppendLine();
                sb.AppendLine("```csharp");
                sb.AppendLine(RemoveIndentation(type.Examples));
                sb.AppendLine("```");
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(type.BestPractices))
            {
                sb.AppendLine("## Best Practices");
                sb.AppendLine();
                sb.AppendLine(type.BestPractices);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(type.Patterns))
            {
                sb.AppendLine("## Patterns");
                sb.AppendLine();
                sb.AppendLine(type.Patterns);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(type.Considerations))
            {
                sb.AppendLine("## Considerations");
                sb.AppendLine();
                sb.AppendLine(type.Considerations);
                sb.AppendLine();
            }

            // Render members
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
                foreach (var api in type.SeeAlso)
                {
                    sb.AppendLine($"- {api}");
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
            sb.AppendLine($"### {member.Name}");
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
                sb.AppendLine("```csharp");
                sb.AppendLine(RemoveIndentation(member.Examples));
                sb.AppendLine("```");
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
                foreach (var api in member.SeeAlso)
                {
                    sb.AppendLine($"- {api}");
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

        #endregion

        #region Private Methods

        /// <summary>
        /// Removes common leading indentation from multi-line text content.
        /// </summary>
        /// <param name="text">The text to remove indentation from.</param>
        /// <returns>The text with common indentation removed.</returns>
        private static string RemoveIndentation(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var lines = text.Split('\n');
            
            // Skip the first line when calculating minimum indentation since it's often already correct
            // (XML doc comments often start with content on the same line as the tag)
            int minIndent = int.MaxValue;
            bool skipFirst = true;
            
            foreach (var line in lines)
            {
                if (skipFirst)
                {
                    skipFirst = false;
                    continue;
                }
                
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                    
                int indent = 0;
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
            
            // If no indentation found, return original text
            if (minIndent == int.MaxValue || minIndent == 0)
                return text;
            
            // Process lines: keep first line as-is, remove indentation from subsequent lines
            var result = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                if (i == 0)
                {
                    // Keep first line as-is
                    result.AppendLine(line);
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    result.AppendLine();
                }
                else if (line.Length > minIndent)
                {
                    result.AppendLine(line.Substring(minIndent));
                }
                else
                {
                    result.AppendLine(line.TrimStart());
                }
            }
            
            return result.ToString().TrimEnd();
        }

        #endregion

    }

}