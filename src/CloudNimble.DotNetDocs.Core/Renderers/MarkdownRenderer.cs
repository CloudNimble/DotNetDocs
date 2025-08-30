using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace CloudNimble.DotNetDocs.Core.Renderers
{

    /// <summary>
    /// Renders documentation as Markdown files.
    /// </summary>
    /// <remarks>
    /// Generates structured Markdown documentation with support for customizations including
    /// insertions, overrides, exclusions, transformations, and conditions.
    /// </remarks>
    public class MarkdownRenderer : RendererBase, IDocRenderer
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownRenderer"/> class.
        /// </summary>
        /// <param name="context">The project context. If null, a default context is created.</param>
        public MarkdownRenderer(ProjectContext? context = null) : base(context)
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

        internal async Task RenderAssemblyAsync(DocAssembly assembly, string outputPath)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"# {assembly.AssemblyName}");
            sb.AppendLine();
            
            if (!string.IsNullOrWhiteSpace(assembly.Usage))
            {
                sb.AppendLine("## Overview");
                sb.AppendLine();
                sb.AppendLine(assembly.Usage);
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

            if (assembly.RelatedApis?.Any() == true)
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
                var namespaceName = GetSafeNamespaceName(ns);
                sb.AppendLine($"- [{namespaceName}]({GetNamespaceFileName(ns, "md")})");
            }

            var filePath = Path.Combine(outputPath, "index.md");
            await File.WriteAllTextAsync(filePath, sb.ToString());
        }

        internal async Task RenderNamespaceAsync(DocNamespace ns, string outputPath)
        {
            var sb = new StringBuilder();
            var namespaceName = GetSafeNamespaceName(ns);
            
            sb.AppendLine($"# {namespaceName}");
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
                sb.AppendLine("## Overview");
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

            if (ns.RelatedApis?.Any() == true)
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
                
                var classes = ns.Types.Where(t => t.Symbol.TypeKind == TypeKind.Class).ToList();
                if (classes.Any())
                {
                    sb.AppendLine("### Classes");
                    sb.AppendLine();
                    foreach (var type in classes)
                    {
                        sb.AppendLine($"- [{type.Symbol.Name}]({GetTypeFileName(type, ns, "md")})");
                    }
                    sb.AppendLine();
                }

                var interfaces = ns.Types.Where(t => t.Symbol.TypeKind == TypeKind.Interface).ToList();
                if (interfaces.Any())
                {
                    sb.AppendLine("### Interfaces");
                    sb.AppendLine();
                    foreach (var type in interfaces)
                    {
                        sb.AppendLine($"- [{type.Symbol.Name}]({GetTypeFileName(type, ns, "md")})");
                    }
                    sb.AppendLine();
                }

                var structs = ns.Types.Where(t => t.Symbol.TypeKind == TypeKind.Struct).ToList();
                if (structs.Any())
                {
                    sb.AppendLine("### Structs");
                    sb.AppendLine();
                    foreach (var type in structs)
                    {
                        sb.AppendLine($"- [{type.Symbol.Name}]({GetTypeFileName(type, ns, "md")})");
                    }
                    sb.AppendLine();
                }

                var enums = ns.Types.Where(t => t.Symbol.TypeKind == TypeKind.Enum).ToList();
                if (enums.Any())
                {
                    sb.AppendLine("### Enums");
                    sb.AppendLine();
                    foreach (var type in enums)
                    {
                        sb.AppendLine($"- [{type.Symbol.Name}]({GetTypeFileName(type, ns, "md")})");
                    }
                    sb.AppendLine();
                }

                var delegates = ns.Types.Where(t => t.Symbol.TypeKind == TypeKind.Delegate).ToList();
                if (delegates.Any())
                {
                    sb.AppendLine("### Delegates");
                    sb.AppendLine();
                    foreach (var type in delegates)
                    {
                        sb.AppendLine($"- [{type.Symbol.Name}]({GetTypeFileName(type, ns, "md")})");
                    }
                    sb.AppendLine();
                }
            }

            var filePath = GetNamespaceFilePath(ns, outputPath, "md");
            await File.WriteAllTextAsync(filePath, sb.ToString());
        }

        internal async Task RenderTypeAsync(DocType type, DocNamespace ns, string outputPath)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"# {type.Symbol.Name}");
            sb.AppendLine();
            
            // Type metadata section
            sb.AppendLine("## Definition");
            sb.AppendLine();
            var namespaceName = GetSafeNamespaceName(ns);
            sb.AppendLine($"**Namespace:** {namespaceName}");
            sb.AppendLine($"**Assembly:** {type.Symbol.ContainingAssembly?.Name ?? "Unknown"}");
            
            if (!string.IsNullOrWhiteSpace(type.BaseType))
            {
                sb.AppendLine($"**Inheritance:** {type.BaseType}");
            }
            
            var interfaces = type.Symbol.AllInterfaces;
            if (interfaces.Any())
            {
                sb.AppendLine($"**Implements:** {string.Join(", ", interfaces.Select(i => i.ToDisplayString()))}");
            }
            sb.AppendLine();
            
            // Type signature
            sb.AppendLine("## Syntax");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine(GetTypeSignature(type));
            sb.AppendLine("```");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(type.Usage))
            {
                sb.AppendLine("## Description");
                sb.AppendLine();
                sb.AppendLine(type.Usage);
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
            var constructors = type.Members.Where(m => m.Symbol.Kind == SymbolKind.Method && ((IMethodSymbol)m.Symbol).MethodKind == MethodKind.Constructor).ToList();
            if (constructors.Any())
            {
                sb.AppendLine("## Constructors");
                sb.AppendLine();
                foreach (var ctor in constructors)
                {
                    RenderMember(sb, ctor);
                }
            }

            var properties = type.Members.Where(m => m.Symbol.Kind == SymbolKind.Property).ToList();
            if (properties.Any())
            {
                sb.AppendLine("## Properties");
                sb.AppendLine();
                foreach (var prop in properties.OrderBy(p => p.Symbol.Name))
                {
                    RenderMember(sb, prop);
                }
            }

            var methods = type.Members.Where(m => m.Symbol.Kind == SymbolKind.Method && ((IMethodSymbol)m.Symbol).MethodKind == MethodKind.Ordinary).ToList();
            if (methods.Any())
            {
                sb.AppendLine("## Methods");
                sb.AppendLine();
                foreach (var method in methods.OrderBy(m => m.Symbol.Name))
                {
                    RenderMember(sb, method);
                }
            }

            var events = type.Members.Where(m => m.Symbol.Kind == SymbolKind.Event).ToList();
            if (events.Any())
            {
                sb.AppendLine("## Events");
                sb.AppendLine();
                foreach (var evt in events.OrderBy(e => e.Symbol.Name))
                {
                    RenderMember(sb, evt);
                }
            }

            var fields = type.Members.Where(m => m.Symbol.Kind == SymbolKind.Field).ToList();
            if (fields.Any())
            {
                sb.AppendLine("## Fields");
                sb.AppendLine();
                foreach (var field in fields.OrderBy(f => f.Symbol.Name))
                {
                    RenderMember(sb, field);
                }
            }

            if (type.RelatedApis?.Any() == true)
            {
                sb.AppendLine("## Related APIs");
                sb.AppendLine();
                foreach (var api in type.RelatedApis)
                {
                    sb.AppendLine($"- {api}");
                }
                sb.AppendLine();
            }

            var filePath = GetTypeFilePath(type, ns, outputPath, "md");
            await File.WriteAllTextAsync(filePath, sb.ToString());
        }

        internal void RenderMember(StringBuilder sb, DocMember member)
        {
            sb.AppendLine($"### {member.Symbol.Name}");
            sb.AppendLine();
            
            // Summary/Description
            if (!string.IsNullOrWhiteSpace(member.Usage))
            {
                sb.AppendLine(member.Usage);
                sb.AppendLine();
            }
            
            // Syntax
            sb.AppendLine("#### Syntax");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine(GetMemberSignature(member));
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
                    var paramType = param.Symbol.Type.ToDisplayString();
                    var description = !string.IsNullOrWhiteSpace(param.Usage) ? param.Usage : "-";
                    sb.AppendLine($"| `{param.Symbol.Name}` | `{paramType}` | {description} |");
                }
                sb.AppendLine();
            }

            // Returns (for methods)
            if (member.Symbol is IMethodSymbol method && method.ReturnsVoid == false)
            {
                sb.AppendLine("#### Returns");
                sb.AppendLine();
                sb.AppendLine($"Type: `{method.ReturnType.ToDisplayString()}`");
                // TODO: Add return documentation when available in DocMember
                sb.AppendLine();
            }

            // Property type (for properties)
            if (member.Symbol is IPropertySymbol property)
            {
                sb.AppendLine("#### Property Value");
                sb.AppendLine();
                sb.AppendLine($"Type: `{property.Type.ToDisplayString()}`");
                // TODO: Add property value documentation when available in DocMember
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

            // Remarks/Best Practices
            if (!string.IsNullOrWhiteSpace(member.BestPractices))
            {
                sb.AppendLine("#### Remarks");
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
            if (member.RelatedApis?.Any() == true)
            {
                sb.AppendLine("#### See Also");
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