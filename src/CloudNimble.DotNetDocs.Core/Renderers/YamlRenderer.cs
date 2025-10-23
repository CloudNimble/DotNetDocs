using System;
using System.Collections.Generic;
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
            // Prevent circular references by setting max recursion depth
            .WithMaximumRecursion(50)
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
        /// <returns>A task representing the asynchronous rendering operation.</returns>
        public async Task RenderAsync(DocAssembly model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var outputPath = Path.Combine(Context.DocumentationRootPath, Context.ApiReferencePath);

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