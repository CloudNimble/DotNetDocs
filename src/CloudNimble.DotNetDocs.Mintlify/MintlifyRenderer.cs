using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Renderers;
using Microsoft.CodeAnalysis;

namespace CloudNimble.DotNetDocs.Mintlify
{

    /// <summary>
    /// Renders documentation as Markdown files.
    /// </summary>
    /// <remarks>
    /// Generates structured Markdown documentation with support for customizations including
    /// insertions, overrides, exclusions, transformations, and conditions.
    /// </remarks>
    public class MintlifyRenderer : RendererBase, IDocRenderer
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MintlifyRenderer"/> class.
        /// </summary>
        /// <param name="context">The project context. If null, a default context is created.</param>
        public MintlifyRenderer(ProjectContext? context = null) : base(context)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Renders the documentation assembly to Markdown files.
        /// </summary>
        /// <param name="model">The documentation assembly to render.</param>
        /// <param name="outputPath">The path where Markdown files should be generated.</param>
        /// <param name="context">The project context providing rendering settings.</param>
        /// <returns>A task representing the asynchronous rendering operation.</returns>
        public async Task RenderAsync(DocAssembly model, string outputPath, ProjectContext context)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(outputPath);
            ArgumentNullException.ThrowIfNull(context);

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
        }

        #endregion

        #region Internal Methods

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
                sb.AppendLine(assembly.Examples);
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
                sb.AppendLine(ns.Examples);
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

            sb.AppendLine($"# {type.Name}");
            sb.AppendLine();

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
                sb.AppendLine(type.Examples);
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
            if (member.MemberKind == Microsoft.CodeAnalysis.SymbolKind.Method &&
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
            if (member.MemberKind == Microsoft.CodeAnalysis.SymbolKind.Property && member.ReturnTypeName is not null)
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
                sb.AppendLine(member.Examples);
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

    }

}