using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Renders a single namespace to a JSON file.
        /// </summary>
        /// <param name="ns">The namespace to render.</param>
        /// <param name="outputPath">The output directory path.</param>
        /// <returns>A task representing the asynchronous render operation.</returns>
        internal async Task RenderNamespaceFileAsync(DocNamespace ns, string outputPath)
        {
            // Serialize the DocNamespace directly - it already has all the properties we need
            var fileName = GetNamespaceFileName(ns, "json");
            var filePath = Path.Combine(outputPath, fileName);
            var json = JsonSerializer.Serialize(ns, _options.SerializerOptions);
            await File.WriteAllTextAsync(filePath, json);
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
        /// Gets a template for usage documentation.
        /// </summary>
        /// <param name="entityName">The name of the entity (type, assembly, etc.).</param>
        /// <returns>A markdown template string for usage documentation.</returns>
        private static string GetUsageTemplate(string entityName)
        {
            return $@"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
# Usage

Describe how to use `{entityName}` here.

";
        }

        /// <summary>
        /// Gets a template for examples documentation.
        /// </summary>
        /// <param name="entityName">The name of the entity (type, assembly, etc.).</param>
        /// <returns>A markdown template string for examples documentation.</returns>
        private static string GetExamplesTemplate(string entityName)
        {
            return $@"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
# Examples

Provide examples of using `{entityName}` here.

```csharp
// Example code here
```

";
        }

        /// <summary>
        /// Gets a template for best practices documentation.
        /// </summary>
        /// <param name="entityName">The name of the entity (type, assembly, etc.).</param>
        /// <returns>A markdown template string for best practices documentation.</returns>
        private static string GetBestPracticesTemplate(string entityName)
        {
            return $@"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
# Best Practices

Document best practices for `{entityName}` here.

";
        }

        /// <summary>
        /// Gets a template for patterns documentation.
        /// </summary>
        /// <param name="entityName">The name of the entity (type, assembly, etc.).</param>
        /// <returns>A markdown template string for patterns documentation.</returns>
        private static string GetPatternsTemplate(string entityName)
        {
            return $@"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
# Patterns

Document common patterns for `{entityName}` here.

";
        }

        /// <summary>
        /// Gets a template for considerations documentation.
        /// </summary>
        /// <param name="entityName">The name of the entity (type, assembly, etc.).</param>
        /// <returns>A markdown template string for considerations documentation.</returns>
        private static string GetConsiderationsTemplate(string entityName)
        {
            return $@"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
# Considerations

Document considerations for `{entityName}` here.

";
        }

        /// <summary>
        /// Gets a template for related APIs documentation.
        /// </summary>
        /// <param name="entityName">The name of the entity (type, assembly, etc.).</param>
        /// <returns>A markdown template string for related APIs documentation.</returns>
        private static string GetRelatedApisTemplate(string entityName)
        {
            return $@"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
# Related APIs

- API 1
- API 2

";
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