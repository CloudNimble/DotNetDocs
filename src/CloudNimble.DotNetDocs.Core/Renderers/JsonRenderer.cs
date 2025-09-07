using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

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
        /// <returns>A task representing the asynchronous rendering operation.</returns>
        public async Task RenderAsync(DocAssembly model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var outputPath = Path.Combine(Context.DocumentationRootPath, Context.ApiReferencePath);

            // Ensure all necessary directories exist based on the file naming mode
            Context.EnsureOutputDirectoryStructure(model, outputPath);

            // Serialize the DocAssembly directly - it already has all the properties we need
            // The JsonSerializerOptions from DocEntity will handle nulls, formatting, etc.
            var mainFilePath = Path.Combine(outputPath, "documentation.json");
            var json = JsonSerializer.Serialize(model, _options.SerializerOptions);
            await File.WriteAllTextAsync(mainFilePath, json);

            // Also write individual namespace files for easier consumption
            foreach (var ns in model.Namespaces)
            {
                await RenderNamespaceFileAsync(ns, outputPath);
            }
        }

        #endregion

        #region Private Methods

        private async Task RenderNamespaceFileAsync(DocNamespace ns, string outputPath)
        {
            // Serialize the DocNamespace directly - it already has all the properties we need
            var fileName = GetNamespaceFileName(ns, "json");
            var filePath = Path.Combine(outputPath, fileName);
            var json = JsonSerializer.Serialize(ns, _options.SerializerOptions);
            await File.WriteAllTextAsync(filePath, json);
        }

        #endregion

    }

}