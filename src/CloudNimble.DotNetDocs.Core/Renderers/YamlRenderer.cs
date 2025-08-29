using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CloudNimble.DotNetDocs.Core.Renderers
{

    /// <summary>
    /// Renders documentation as YAML files.
    /// </summary>
    /// <remarks>
    /// Generates structured YAML documentation suitable for configuration files and
    /// integration with various documentation platforms.
    /// </remarks>
    public class YamlRenderer : RendererBase, IDocRenderer
    {

        #region Fields

        private static readonly ISerializer _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        #endregion

        #region Public Methods

        /// <summary>
        /// Renders the documentation assembly to YAML files.
        /// </summary>
        /// <param name="model">The documentation assembly to render.</param>
        /// <param name="outputPath">The path where YAML files should be generated.</param>
        /// <param name="context">The project context providing rendering settings.</param>
        /// <returns>A task representing the asynchronous rendering operation.</returns>
        public async Task RenderAsync(DocAssembly model, string outputPath, ProjectContext context)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(outputPath);
            ArgumentNullException.ThrowIfNull(context);

            Directory.CreateDirectory(outputPath);

            // Create the main documentation structure
            var documentation = new Dictionary<string, object>
            {
                ["assembly"] = new Dictionary<string, object>
                {
                    ["name"] = model.AssemblyName,
                    ["version"] = model.Symbol.Identity.Version.ToString(),
                    ["usage"] = model.Usage,
                    ["examples"] = model.Examples,
                    ["bestPractices"] = model.BestPractices,
                    ["patterns"] = model.Patterns,
                    ["considerations"] = model.Considerations,
                    ["relatedApis"] = model.RelatedApis,
                    ["namespaces"] = SerializeNamespaces(model)
                }
            };

            // Write main documentation file
            var mainFilePath = Path.Combine(outputPath, "documentation.yaml");
            var yaml = _yamlSerializer.Serialize(documentation);
            await File.WriteAllTextAsync(mainFilePath, yaml);

            // Also write individual namespace files for easier consumption
            foreach (var ns in model.Namespaces)
            {
                await RenderNamespaceFileAsync(ns, outputPath);
            }

            // Generate a table of contents file
            await RenderTableOfContentsAsync(model, outputPath);
        }

        #endregion

        #region Private Methods

        private List<Dictionary<string, object>> SerializeNamespaces(DocAssembly assembly)
        {
            return assembly.Namespaces.Select(ns => new Dictionary<string, object>
            {
                ["name"] = ns.Symbol.ToDisplayString(),
                ["usage"] = ns.Usage,
                ["examples"] = ns.Examples,
                ["bestPractices"] = ns.BestPractices,
                ["patterns"] = ns.Patterns,
                ["considerations"] = ns.Considerations,
                ["relatedApis"] = ns.RelatedApis,
                ["types"] = SerializeTypes(ns)
            }).ToList();
        }

        private List<Dictionary<string, object>> SerializeTypes(DocNamespace ns)
        {
            return ns.Types.Select(type => new Dictionary<string, object>
            {
                ["name"] = type.Symbol.Name,
                ["fullName"] = type.Symbol.ToDisplayString(),
                ["kind"] = type.Symbol.TypeKind.ToString(),
                ["baseType"] = type.BaseType!,
                ["usage"] = type.Usage,
                ["examples"] = type.Examples,
                ["bestPractices"] = type.BestPractices,
                ["patterns"] = type.Patterns,
                ["considerations"] = type.Considerations,
                ["relatedApis"] = type.RelatedApis,
                ["members"] = SerializeMembers(type)
            }).ToList();
        }

        private List<Dictionary<string, object>> SerializeMembers(DocType type)
        {
            return type.Members.Select(member => new Dictionary<string, object>
            {
                ["name"] = member.Symbol.Name,
                ["kind"] = member.Symbol.Kind.ToString(),
                ["accessibility"] = member.Symbol.DeclaredAccessibility.ToString(),
                ["usage"] = member.Usage,
                ["examples"] = member.Examples,
                ["bestPractices"] = member.BestPractices,
                ["patterns"] = member.Patterns,
                ["considerations"] = member.Considerations,
                ["relatedApis"] = member.RelatedApis,
                ["signature"] = GetMemberSignature(member),
                ["parameters"] = SerializeParameters(member)!,
                ["returnType"] = GetReturnType(member)!,
                ["modifiers"] = GetModifiers(member)
            }).ToList();
        }

        private List<Dictionary<string, object>>? SerializeParameters(DocMember member)
        {
            if (member.Parameters is null || !member.Parameters.Any())
                return null;

            return member.Parameters.Select(param => new Dictionary<string, object>
            {
                ["name"] = param.Symbol.Name,
                ["type"] = param.Symbol.Type.ToDisplayString(),
                ["isOptional"] = param.Symbol.HasExplicitDefaultValue,
                ["defaultValue"] = param.Symbol.HasExplicitDefaultValue ? (param.Symbol.ExplicitDefaultValue?.ToString() ?? "null") : null!,
                ["isParams"] = param.Symbol.IsParams,
                ["isRef"] = param.Symbol.RefKind == RefKind.Ref,
                ["isOut"] = param.Symbol.RefKind == RefKind.Out,
                ["isIn"] = param.Symbol.RefKind == RefKind.In,
                ["usage"] = param.Usage,
                ["examples"] = param.Examples,
                ["bestPractices"] = param.BestPractices,
                ["considerations"] = param.Considerations
            }).ToList();
        }

        private async Task RenderNamespaceFileAsync(DocNamespace ns, string outputPath)
        {
            var namespaceData = new Dictionary<string, object>
            {
                ["namespace"] = new Dictionary<string, object>
                {
                    ["name"] = ns.Symbol.ToDisplayString(),
                    ["usage"] = ns.Usage,
                    ["examples"] = ns.Examples,
                    ["bestPractices"] = ns.BestPractices,
                    ["patterns"] = ns.Patterns,
                    ["considerations"] = ns.Considerations,
                    ["relatedApis"] = ns.RelatedApis,
                    ["types"] = SerializeTypes(ns)
                }
            };

            var namespaceName = ns.Symbol.IsGlobalNamespace ? "global" : ns.Symbol.ToDisplayString();
            var fileName = $"{namespaceName.Replace('.', '-')}.yaml";
            var filePath = Path.Combine(outputPath, fileName);
            var yaml = _yamlSerializer.Serialize(namespaceData);
            await File.WriteAllTextAsync(filePath, yaml);
        }

        private async Task RenderTableOfContentsAsync(DocAssembly model, string outputPath)
        {
            var toc = new Dictionary<string, object>
            {
                ["items"] = model.Namespaces.Select(ns =>
                {
                    var namespaceName = ns.Symbol.IsGlobalNamespace ? "global" : ns.Symbol.ToDisplayString();
                    return new Dictionary<string, object>
                    {
                        ["name"] = namespaceName,
                        ["href"] = $"{namespaceName.Replace('.', '-')}.yaml",
                        ["items"] = ns.Types.Select(type => new Dictionary<string, object>
                        {
                            ["name"] = type.Symbol.Name,
                            ["fullName"] = type.Symbol.ToDisplayString(),
                            ["kind"] = type.Symbol.TypeKind.ToString()
                        }).ToList()
                    };
                }).ToList()
            };

            var tocFilePath = Path.Combine(outputPath, "toc.yaml");
            var yaml = _yamlSerializer.Serialize(toc);
            await File.WriteAllTextAsync(tocFilePath, yaml);
        }

        // GetMemberSignature and GetMethodSignature are inherited from RendererBase

        private string? GetReturnType(DocMember member)
        {
            return member.Symbol switch
            {
                IMethodSymbol method when method.MethodKind != MethodKind.Constructor => method.ReturnType.ToDisplayString(),
                IPropertySymbol property => property.Type.ToDisplayString(),
                IFieldSymbol field => field.Type.ToDisplayString(),
                _ => null
            };
        }

        private List<string> GetModifiers(DocMember member)
        {
            var modifiers = new List<string>();
            
            switch (member.Symbol)
            {
                case IMethodSymbol method:
                    if (method.IsStatic) modifiers.Add("static");
                    if (method.IsVirtual) modifiers.Add("virtual");
                    if (method.IsOverride) modifiers.Add("override");
                    if (method.IsAbstract) modifiers.Add("abstract");
                    if (method.IsAsync) modifiers.Add("async");
                    if (method.IsSealed) modifiers.Add("sealed");
                    if (method.IsExtern) modifiers.Add("extern");
                    break;
                    
                case IPropertySymbol property:
                    if (property.IsStatic) modifiers.Add("static");
                    if (property.IsVirtual) modifiers.Add("virtual");
                    if (property.IsOverride) modifiers.Add("override");
                    if (property.IsAbstract) modifiers.Add("abstract");
                    if (property.IsSealed) modifiers.Add("sealed");
                    if (property.IsReadOnly) modifiers.Add("readonly");
                    if (property.IsWriteOnly) modifiers.Add("writeonly");
                    break;
                    
                case IFieldSymbol field:
                    if (field.IsStatic) modifiers.Add("static");
                    if (field.IsReadOnly) modifiers.Add("readonly");
                    if (field.IsConst) modifiers.Add("const");
                    if (field.IsVolatile) modifiers.Add("volatile");
                    break;
                    
                case IEventSymbol evt:
                    if (evt.IsStatic) modifiers.Add("static");
                    if (evt.IsVirtual) modifiers.Add("virtual");
                    if (evt.IsOverride) modifiers.Add("override");
                    if (evt.IsAbstract) modifiers.Add("abstract");
                    if (evt.IsSealed) modifiers.Add("sealed");
                    break;
            }
            
            return modifiers;
        }

        #endregion

    }

}