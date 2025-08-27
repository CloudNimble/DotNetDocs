using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Orchestrates the documentation pipeline for one or more assemblies.
    /// </summary>
    /// <remarks>
    /// This class manages the complete documentation lifecycle including enrichment,
    /// transformation, and rendering. It handles <see cref="AssemblyManager"/> creation,
    /// usage, and disposal for processing assemblies.
    /// </remarks>
    public class DocumentationManager
    {

        #region Private Fields

        private readonly IEnumerable<IDocEnricher> enrichers;
        private readonly IEnumerable<IDocTransformer> transformers;
        private readonly IEnumerable<IDocRenderer> renderers;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentationManager"/> class.
        /// </summary>
        /// <param name="enrichers">The enrichers to apply to documentation entities.</param>
        /// <param name="transformers">The transformers to apply to the documentation model.</param>
        /// <param name="renderers">The renderers to generate output formats.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public DocumentationManager(
            List<IDocEnricher>? enrichers = null,
            List<IDocTransformer>? transformers = null,
            List<IDocRenderer>? renderers = null)
        {
            this.enrichers = enrichers ?? [];
            this.transformers = transformers ?? [];
            this.renderers = renderers ?? [];
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Processes a single assembly through the documentation pipeline.
        /// </summary>
        /// <param name="assemblyPath">The path to the assembly file.</param>
        /// <param name="xmlPath">The path to the XML documentation file.</param>
        /// <param name="projectContext">Optional project context with additional settings.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        public async Task ProcessAsync(string assemblyPath, string xmlPath, ProjectContext? projectContext = null)
        {
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var model = await manager.DocumentAsync(projectContext);

            // Load conceptual content if path provided
            if (!string.IsNullOrWhiteSpace(projectContext?.ConceptualPath))
            {
                await LoadConceptualAsync(model, projectContext.ConceptualPath);
            }

            // Apply enrichers
            foreach (var enricher in enrichers)
            {
                await EnrichModelAsync(model, enricher, projectContext);
            }

            // Apply transformers
            foreach (var transformer in transformers)
            {
                await TransformModelAsync(model, transformer, projectContext);
            }

            // Apply renderers
            foreach (var renderer in renderers)
            {
                await renderer.RenderAsync(
                    model,
                    projectContext?.OutputPath ?? "docs",
                    projectContext ?? new ProjectContext());
            }
        }

        /// <summary>
        /// Processes multiple assemblies through the documentation pipeline.
        /// </summary>
        /// <param name="assemblies">The collection of assembly and XML path pairs.</param>
        /// <param name="projectContext">Optional project context with additional settings.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        public async Task ProcessAsync(IEnumerable<(string assemblyPath, string xmlPath)> assemblies, ProjectContext? projectContext = null)
        {
            var tasks = assemblies.Select(async pair =>
            {
                using var manager = new AssemblyManager(pair.assemblyPath, pair.xmlPath);
                var model = await manager.DocumentAsync(projectContext);

                // Load conceptual content if path provided
                if (!string.IsNullOrWhiteSpace(projectContext?.ConceptualPath))
                {
                    await LoadConceptualAsync(model, projectContext.ConceptualPath);
                }

                // Apply enrichers
                foreach (var enricher in enrichers)
                {
                    await EnrichModelAsync(model, enricher, projectContext);
                }

                // Apply transformers
                foreach (var transformer in transformers)
                {
                    await TransformModelAsync(model, transformer, projectContext);
                }

                // Apply renderers
                foreach (var renderer in renderers)
                {
                    await renderer.RenderAsync(
                        model,
                        projectContext?.OutputPath ?? "docs",
                        projectContext ?? new ProjectContext());
                }
            });

            await Task.WhenAll(tasks);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Recursively enriches a documentation model and all its children.
        /// </summary>
        private async Task EnrichModelAsync(DocEntity entity, IDocEnricher enricher, ProjectContext? projectContext)
        {
            await enricher.EnrichAsync(entity, projectContext ?? new ProjectContext());

            // Recursively enrich children
            if (entity is DocAssembly assembly)
            {
                foreach (var ns in assembly.Namespaces)
                {
                    await EnrichModelAsync(ns, enricher, projectContext);
                }
            }
            else if (entity is DocNamespace ns)
            {
                foreach (var type in ns.Types)
                {
                    await EnrichModelAsync(type, enricher, projectContext);
                }
            }
            else if (entity is DocType type)
            {
                foreach (var member in type.Members)
                {
                    await EnrichModelAsync(member, enricher, projectContext);
                }
            }
            else if (entity is DocMember member)
            {
                foreach (var param in member.Parameters)
                {
                    await EnrichModelAsync(param, enricher, projectContext);
                }
            }
        }

        /// <summary>
        /// Recursively transforms a documentation model and all its children.
        /// </summary>
        private async Task TransformModelAsync(DocEntity entity, IDocTransformer transformer, ProjectContext? projectContext)
        {
            await transformer.TransformAsync(entity, projectContext ?? new ProjectContext());

            // Recursively transform children
            if (entity is DocAssembly assembly)
            {
                foreach (var ns in assembly.Namespaces)
                {
                    await TransformModelAsync(ns, transformer, projectContext);
                }
            }
            else if (entity is DocNamespace ns)
            {
                foreach (var type in ns.Types)
                {
                    await TransformModelAsync(type, transformer, projectContext);
                }
            }
            else if (entity is DocType type)
            {
                foreach (var member in type.Members)
                {
                    await TransformModelAsync(member, transformer, projectContext);
                }
            }
            else if (entity is DocMember member)
            {
                foreach (var param in member.Parameters)
                {
                    await TransformModelAsync(param, transformer, projectContext);
                }
            }
        }

        /// <summary>
        /// Loads conceptual content from the file system into the documentation model.
        /// </summary>
        /// <param name="assembly">The assembly to load conceptual content for.</param>
        /// <param name="conceptualPath">The path to the conceptual content folder.</param>
        private async Task LoadConceptualAsync(DocAssembly assembly, string conceptualPath)
        {
            if (!Directory.Exists(conceptualPath))
            {
                return;
            }

            foreach (var ns in assembly.Namespaces)
            {
                foreach (var type in ns.Types)
                {
                    // Build namespace path like /conceptual/System/Text/Json/JsonSerializer/
                    var namespacePath = ns.Symbol.IsGlobalNamespace
                        ? string.Empty
                        : ns.Symbol.ToDisplayString().Replace('.', Path.DirectorySeparatorChar);
                    var typeDir = Path.Combine(conceptualPath, namespacePath, type.Symbol.Name);

                    if (Directory.Exists(typeDir))
                    {
                        // Load type-level conceptual content
                        await LoadConceptualFileAsync(typeDir, DotNetDocsConstants.UsageFileName, content => type.Usage = content);
                        await LoadConceptualFileAsync(typeDir, DotNetDocsConstants.ExamplesFileName, content => type.Examples = content);
                        await LoadConceptualFileAsync(typeDir, DotNetDocsConstants.BestPracticesFileName, content => type.BestPractices = content);
                        await LoadConceptualFileAsync(typeDir, DotNetDocsConstants.PatternsFileName, content => type.Patterns = content);
                        await LoadConceptualFileAsync(typeDir, DotNetDocsConstants.ConsiderationsFileName, content => type.Considerations = content);

                        // Load related APIs if markdown file exists
                        var relatedApisPath = Path.Combine(typeDir, DotNetDocsConstants.RelatedApisFileName);
                        if (File.Exists(relatedApisPath))
                        {
                            // Simple markdown file with one API per line for now
                            var lines = await File.ReadAllLinesAsync(relatedApisPath);
                            type.RelatedApis = lines
                                .Where(line => !string.IsNullOrWhiteSpace(line))
                                .Select(line => line.Trim())
                                .ToList();
                        }

                        // Load member-specific conceptual content
                        foreach (var member in type.Members)
                        {
                            var memberDir = Path.Combine(typeDir, member.Symbol.Name);
                            if (Directory.Exists(memberDir))
                            {
                                await LoadConceptualFileAsync(memberDir, DotNetDocsConstants.UsageFileName, content => member.Usage = content);
                                await LoadConceptualFileAsync(memberDir, DotNetDocsConstants.ExamplesFileName, content => member.Examples = content);
                                await LoadConceptualFileAsync(memberDir, DotNetDocsConstants.BestPracticesFileName, content => member.BestPractices = content);
                                await LoadConceptualFileAsync(memberDir, DotNetDocsConstants.PatternsFileName, content => member.Patterns = content);
                                await LoadConceptualFileAsync(memberDir, DotNetDocsConstants.ConsiderationsFileName, content => member.Considerations = content);

                                // Load member-specific related APIs
                                var memberRelatedApisPath = Path.Combine(memberDir, DotNetDocsConstants.RelatedApisFileName);
                                if (File.Exists(memberRelatedApisPath))
                                {
                                    var lines = await File.ReadAllLinesAsync(memberRelatedApisPath);
                                    member.RelatedApis = lines
                                        .Where(line => !string.IsNullOrWhiteSpace(line))
                                        .Select(line => line.Trim())
                                        .ToList();
                                }

                                // Load parameter-specific documentation
                                foreach (var parameter in member.Parameters)
                                {
                                    var paramFile = Path.Combine(memberDir, $"{DotNetDocsConstants.ParameterFilePrefix}{parameter.Symbol.Name}{DotNetDocsConstants.ParameterFileExtension}");
                                    if (File.Exists(paramFile))
                                    {
                                        var content = await File.ReadAllTextAsync(paramFile);
                                        content = content.Trim();
                                        if (!string.IsNullOrWhiteSpace(content))
                                        {
                                            // Override XML documentation with conceptual content
                                            parameter.Usage = content;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Helper method to load a conceptual file if it exists.
        /// </summary>
        /// <param name="directory">The directory to look in.</param>
        /// <param name="fileName">The file name to load.</param>
        /// <param name="setter">Action to set the content on the target object.</param>
        private async Task LoadConceptualFileAsync(string directory, string fileName, Action<string> setter)
        {
            var filePath = Path.Combine(directory, fileName);
            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath);
                content = content.Trim();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    setter(content);
                }
            }
        }

        #endregion

    }

}