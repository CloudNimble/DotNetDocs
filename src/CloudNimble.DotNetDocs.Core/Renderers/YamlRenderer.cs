using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core.Renderers.YamlConverters;
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
    public partial class YamlRenderer : RendererBase, IDocRenderer
    {

        #region Fields

        private static readonly ISerializer _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitEmptyCollections)
            .DisableAliases()
            .IgnoreFields()
            // Type converters for Roslyn types to ensure proper serialization
            .WithTypeConverter(new SymbolTypeConverter())  // Handle ISymbol types first
            .WithTypeConverter(new AccessibilityTypeConverter())
            .WithTypeConverter(new SymbolKindTypeConverter())
            .WithTypeConverter(new TypeKindTypeConverter())
            .WithTypeConverter(new RefKindTypeConverter())
            .Build();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlRenderer"/> class.
        /// </summary>
        /// <param name="context">The project context. If null, a default context is created.</param>
        public YamlRenderer(ProjectContext? context = null) : base(context)
        {
        }

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

            // Ensure all necessary directories exist based on the file naming mode
            Context.EnsureOutputDirectoryStructure(model, outputPath);

            // Serialize the entire model directly
            var yaml = _yamlSerializer.Serialize(model);
            
            // Write main documentation file
            var mainFilePath = Path.Combine(outputPath, "documentation.yaml");
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

        #region Internal Methods

        internal async Task RenderNamespaceFileAsync(DocNamespace ns, string outputPath)
        {
            var filePath = GetNamespaceFilePath(ns, outputPath, "yaml");
            var yaml = _yamlSerializer.Serialize(ns);
            await File.WriteAllTextAsync(filePath, yaml);
        }

        internal async Task RenderTableOfContentsAsync(DocAssembly model, string outputPath)
        {
            var toc = new
            {
                Title = model.AssemblyName,
                Items = model.Namespaces.Select(ns => new
                {
                    Name = GetSafeNamespaceName(ns),
                    Href = Path.GetFileName(GetNamespaceFilePath(ns, outputPath, "yaml")),
                    Types = ns.Types.Select(t => new
                    {
                        t.Name,
                        Kind = t.TypeKind.ToString()
                    })
                })
            };

            var tocFilePath = Path.Combine(outputPath, "toc.yaml");
            var yaml = _yamlSerializer.Serialize(toc);
            await File.WriteAllTextAsync(tocFilePath, yaml);
        }

        #endregion

    }

}