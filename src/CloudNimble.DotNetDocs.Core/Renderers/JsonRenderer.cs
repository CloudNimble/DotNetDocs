using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace CloudNimble.DotNetDocs.Core.Renderers
{

    /// <summary>
    /// Renders documentation as JSON files.
    /// </summary>
    /// <remarks>
    /// Generates structured JSON documentation suitable for API consumption and integration
    /// with documentation tools.
    /// </remarks>
    public class JsonRenderer : RendererBase, IDocRenderer
    {

        #region Fields

        private readonly JsonRendererOptions _options;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the JsonSerializerOptions used by this renderer.
        /// </summary>
        internal JsonSerializerOptions SerializerOptions => _options.SerializerOptions;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonRenderer"/> class.
        /// </summary>
        /// <param name="context">The project context. If null, a default context is created.</param>
        /// <param name="options">The rendering options. If null, default options are used.</param>
        public JsonRenderer(ProjectContext? context = null, JsonRendererOptions? options = null) : base(context)
        {
            _options = options ?? new JsonRendererOptions();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Renders the documentation assembly to JSON files.
        /// </summary>
        /// <param name="model">The documentation assembly to render.</param>
        /// <param name="outputPath">The path where JSON files should be generated.</param>
        /// <param name="context">The project context providing rendering settings.</param>
        /// <returns>A task representing the asynchronous rendering operation.</returns>
        public async Task RenderAsync(DocAssembly model, string outputPath, ProjectContext context)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(outputPath);
            ArgumentNullException.ThrowIfNull(context);

            // Ensure all necessary directories exist based on the file naming mode
            Context.EnsureOutputDirectoryStructure(model, outputPath);

            // Create the main documentation structure
            var documentation = new
            {
                Assembly = new
                {
                    Name = model.AssemblyName,
                    Version = model.Symbol.Identity.Version.ToString(),
                    model.Usage,
                    model.Examples,
                    model.BestPractices,
                    model.Patterns,
                    model.Considerations,
                    model.RelatedApis,
                    Namespaces = SerializeNamespaces(model)
                }
            };

            // Write main documentation file
            var mainFilePath = Path.Combine(outputPath, "documentation.json");
            var json = JsonSerializer.Serialize(documentation, _options.SerializerOptions);
            await File.WriteAllTextAsync(mainFilePath, json);

            // Also write individual namespace files for easier consumption
            foreach (var ns in model.Namespaces)
            {
                await RenderNamespaceFileAsync(ns, outputPath);
            }
        }

        #endregion

        #region Internal Methods

        internal object SerializeNamespaces(DocAssembly assembly)
        {
            return assembly.Namespaces.Select(ns => new
            {
                Name = ns.Symbol.ToDisplayString(),
                Summary = ns.Summary,
                Usage = ns.Usage,
                Examples = ns.Examples,
                BestPractices = ns.BestPractices,
                Patterns = ns.Patterns,
                Considerations = ns.Considerations,
                RelatedApis = ns.RelatedApis,
                Types = SerializeTypes(ns)
            });
        }

        internal object SerializeTypes(DocNamespace ns)
        {
            return ns.Types.Select(type => new
            {
                Name = type.Symbol.Name,
                FullName = type.Symbol.ToDisplayString(),
                Kind = type.Symbol.TypeKind.ToString(),
                BaseType = type.BaseType,
                Usage = type.Usage,
                Examples = type.Examples,
                BestPractices = type.BestPractices,
                Patterns = type.Patterns,
                Considerations = type.Considerations,
                RelatedApis = type.RelatedApis,
                Members = SerializeMembers(type)
            });
        }

        internal object SerializeMembers(DocType type)
        {
            return type.Members.Select(member => new
            {
                Name = member.Symbol.Name,
                Kind = member.Symbol.Kind.ToString(),
                Accessibility = member.Symbol.DeclaredAccessibility.ToString(),
                Usage = member.Usage,
                Examples = member.Examples,
                BestPractices = member.BestPractices,
                Patterns = member.Patterns,
                Considerations = member.Considerations,
                RelatedApis = member.RelatedApis,
                Signature = GetMemberSignature(member),
                Parameters = SerializeParameters(member),
                ReturnType = GetReturnType(member)
            });
        }

        internal object? SerializeParameters(DocMember member)
        {
            if (member.Parameters is null || !member.Parameters.Any())
                return null;

            return member.Parameters.Select(param => new
            {
                Name = param.Symbol.Name,
                Type = param.Symbol.Type.ToDisplayString(),
                IsOptional = param.Symbol.HasExplicitDefaultValue,
                DefaultValue = param.Symbol.HasExplicitDefaultValue ? param.Symbol.ExplicitDefaultValue?.ToString() : null,
                Usage = param.Usage,
                Examples = param.Examples,
                BestPractices = param.BestPractices,
                Considerations = param.Considerations
            });
        }

        internal async Task RenderNamespaceFileAsync(DocNamespace ns, string outputPath)
        {
            var namespaceData = new
            {
                Namespace = new
                {
                    Name = ns.Symbol.ToDisplayString(),
                    ns.Usage,
                    ns.Examples,
                    ns.BestPractices,
                    ns.Patterns,
                    ns.Considerations,
                    ns.RelatedApis,
                    Types = SerializeTypes(ns)
                }
            };

            var filePath = Path.Combine(outputPath, GetNamespaceFileName(ns, "json"));
            var json = JsonSerializer.Serialize(namespaceData, _options.SerializerOptions);
            await File.WriteAllTextAsync(filePath, json);
        }

        // GetMemberSignature and GetMethodSignature are inherited from RendererBase

        internal string? GetReturnType(DocMember member)
        {
            return member.Symbol switch
            {
                IMethodSymbol method when method.MethodKind != MethodKind.Constructor => method.ReturnType.ToDisplayString(),
                IPropertySymbol property => property.Type.ToDisplayString(),
                IFieldSymbol field => field.Type.ToDisplayString(),
                _ => null
            };
        }

        #endregion

    }

}