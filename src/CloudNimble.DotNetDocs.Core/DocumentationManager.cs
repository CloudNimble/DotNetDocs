using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    public class DocumentationManager : IDisposable
    {

        #region Private Fields

        private readonly Dictionary<string, AssemblyManager> assemblyManagerCache = [];
        private readonly IEnumerable<IDocEnricher> enrichers;
        private readonly IEnumerable<IDocRenderer> renderers;
        private readonly IEnumerable<IDocTransformer> transformers;
        private readonly ProjectContext projectContext;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentationManager"/> class.
        /// </summary>
        /// <param name="projectContext">The project context containing configuration settings.</param>
        /// <param name="enrichers">The enrichers to apply to documentation entities.</param>
        /// <param name="transformers">The transformers to apply to the documentation model.</param>
        /// <param name="renderers">The renderers to generate output formats.</param>
        /// <remarks>
        /// This constructor is designed to work with dependency injection containers.
        /// All parameters accept IEnumerable collections that are typically injected by the DI container.
        /// </remarks>
        public DocumentationManager(
            ProjectContext projectContext,
            IEnumerable<IDocEnricher>? enrichers = null,
            IEnumerable<IDocTransformer>? transformers = null,
            IEnumerable<IDocRenderer>? renderers = null)
        {
            this.projectContext = projectContext ?? throw new ArgumentNullException(nameof(projectContext));
            this.enrichers = enrichers ?? [];
            this.transformers = transformers ?? [];
            this.renderers = renderers ?? [];
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates placeholder conceptual documentation files for a single assembly.
        /// </summary>
        /// <param name="assemblyPath">The path to the assembly file.</param>
        /// <param name="xmlPath">The path to the XML documentation file.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CreateConceptualFilesAsync(string assemblyPath, string xmlPath)
        {
            await CreateConceptualFilesAsync([(assemblyPath, xmlPath)]);
        }

        /// <summary>
        /// Creates placeholder conceptual documentation files for multiple assemblies.
        /// </summary>
        /// <param name="assemblies">The collection of assembly and XML path pairs.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CreateConceptualFilesAsync(IEnumerable<(string assemblyPath, string xmlPath)> assemblies)
        {
            var tasks = assemblies.Select(async pair =>
            {
                var manager = GetOrCreateAssemblyManager(pair.assemblyPath, pair.xmlPath);
                var model = await manager.DocumentAsync(projectContext);
            });

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Processes a single assembly through the documentation pipeline.
        /// </summary>
        /// <param name="assemblyPath">The path to the assembly file.</param>
        /// <param name="xmlPath">The path to the XML documentation file.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        public async Task ProcessAsync(string assemblyPath, string xmlPath)
        {
            await ProcessAsync([(assemblyPath, xmlPath)]);
        }

        /// <summary>
        /// Processes multiple assemblies through the documentation pipeline.
        /// </summary>
        /// <param name="assemblies">The collection of assembly and XML path pairs.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        public async Task ProcessAsync(IEnumerable<(string assemblyPath, string xmlPath)> assemblies)
        {
            // Collect all DocAssembly models
            var docAssemblies = new List<DocAssembly>();
            foreach (var (assemblyPath, xmlPath) in assemblies)
            {
                var manager = GetOrCreateAssemblyManager(assemblyPath, xmlPath);
                var model = await manager.DocumentAsync(projectContext);
                docAssemblies.Add(model);
            }

            if (projectContext.ConceptualDocsEnabled)
            {
                // Process assemblies in parallel for better performance
                // Each assembly gets its own conceptual directory subtree, so no file conflicts
                var assemblyTasks = docAssemblies.Select(async assembly =>
                {
                    // STEP 1: Generate placeholder files for this assembly with all renderers
                    var placeholderTasks = renderers.Select(renderer => renderer.RenderPlaceholdersAsync(assembly));
                    await Task.WhenAll(placeholderTasks);

                    // STEP 2: Load conceptual content for this assembly (after placeholders exist)
                    await LoadConceptualAsync(assembly);
                });

                await Task.WhenAll(assemblyTasks);
            }

            // STEP 3: Merge all DocAssembly models (now with conceptual content loaded)
            var mergedModel = await MergeDocAssembliesAsync(docAssemblies);

            // STEP 4: Apply enrichers, transformers, and renderers
            foreach (var enricher in enrichers)
            {
                await enricher.EnrichAsync(mergedModel);
            }

            foreach (var transformer in transformers)
            {
                await transformer.TransformAsync(mergedModel);
            }

            foreach (var renderer in renderers)
            {
                await renderer.RenderAsync(mergedModel);
            }
        }


        #endregion

        #region Internal Methods

        /// <summary>
        /// Merges multiple DocAssembly models into a single unified model.
        /// </summary>
        /// <param name="assemblies">The collection of DocAssembly models to merge.</param>
        /// <returns>A task representing the asynchronous merge operation.</returns>
        internal async Task<DocAssembly> MergeDocAssembliesAsync(List<DocAssembly> assemblies)
        {
            if (assemblies.Count == 0)
            {
                throw new ArgumentException("At least one assembly must be provided", nameof(assemblies));
            }

            if (assemblies.Count == 1)
            {
                return assemblies[0];
            }

            // Use the first assembly as the base
            var mergedAssembly = assemblies[0];

            // Merge namespaces from additional assemblies
            for (int i = 1; i < assemblies.Count; i++)
            {
                var assembly = assemblies[i];
                foreach (var ns in assembly.Namespaces)
                {
                    await MergeNamespaceAsync(mergedAssembly, ns);
                }
            }

            return mergedAssembly;
        }

        /// <summary>
        /// Merges a namespace into the merged assembly, handling conflicts.
        /// </summary>
        /// <param name="mergedAssembly">The assembly being merged into.</param>
        /// <param name="sourceNamespace">The namespace to merge.</param>
        /// <returns>A task representing the asynchronous merge operation.</returns>
        internal async Task MergeNamespaceAsync(DocAssembly mergedAssembly, DocNamespace sourceNamespace)
        {
            // Find existing namespace or create new one
            var existingNamespace = mergedAssembly.Namespaces.FirstOrDefault(ns =>
                ns.Symbol.ToDisplayString() == sourceNamespace.Symbol.ToDisplayString());

            if (existingNamespace is null)
            {
                // Add new namespace
                mergedAssembly.Namespaces.Add(sourceNamespace);
            }
            else
            {
                // Merge types into existing namespace
                foreach (var type in sourceNamespace.Types)
                {
                    await MergeTypeAsync(existingNamespace, type);
                }

                // Merge namespace-level content (summary, usage, etc.)
                if (!string.IsNullOrWhiteSpace(sourceNamespace.Summary) &&
                    string.IsNullOrWhiteSpace(existingNamespace.Summary))
                {
                    existingNamespace.Summary = sourceNamespace.Summary;
                }
                // TODO: Merge other namespace content as needed
            }
        }

        /// <summary>
        /// Merges a type into the merged namespace, handling conflicts.
        /// </summary>
        /// <param name="mergedNamespace">The namespace being merged into.</param>
        /// <param name="sourceType">The type to merge.</param>
        /// <returns>A task representing the asynchronous merge operation.</returns>
        internal async Task MergeTypeAsync(DocNamespace mergedNamespace, DocType sourceType)
        {
            // Find existing type or create new one
            var existingType = mergedNamespace.Types.FirstOrDefault(t =>
                t.Symbol.ToDisplayString() == sourceType.Symbol.ToDisplayString());

            if (existingType is null)
            {
                // Add new type
                mergedNamespace.Types.Add(sourceType);
            }
            else
            {
                // Merge members into existing type
                foreach (var member in sourceType.Members)
                {
                    await MergeMemberAsync(existingType, member);
                }

                // Merge type-level content (summary, usage, etc.)
                if (!string.IsNullOrWhiteSpace(sourceType.Summary) &&
                    string.IsNullOrWhiteSpace(existingType.Summary))
                {
                    existingType.Summary = sourceType.Summary;
                }
                // TODO: Merge other type content as needed
            }
        }

        /// <summary>
        /// Merges a member into the merged type, handling conflicts.
        /// </summary>
        /// <param name="mergedType">The type being merged into.</param>
        /// <param name="sourceMember">The member to merge.</param>
        /// <returns>A task representing the asynchronous merge operation.</returns>
        internal Task MergeMemberAsync(DocType mergedType, DocMember sourceMember)
        {
            // Find existing member or create new one
            var existingMember = mergedType.Members.FirstOrDefault(m =>
                m.Symbol.ToDisplayString() == sourceMember.Symbol.ToDisplayString());

            if (existingMember is null)
            {
                // Add new member
                mergedType.Members.Add(sourceMember);
            }
            else
            {
                // TODO: Handle member conflicts - for now, prefer the first one encountered
                // This could be enhanced to merge documentation from multiple sources
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets or creates a cached <see cref="AssemblyManager"/> instance for the specified assembly path.
        /// </summary>
        /// <param name="assemblyPath">The path to the assembly file.</param>
        /// <param name="xmlPath">The path to the XML documentation file.</param>
        /// <returns>A cached or newly created <see cref="AssemblyManager"/> instance.</returns>
        internal AssemblyManager GetOrCreateAssemblyManager(string assemblyPath, string xmlPath)
        {
            if (!assemblyManagerCache.TryGetValue(assemblyPath, out var manager))
            {
                manager = new AssemblyManager(assemblyPath, xmlPath);
                assemblyManagerCache[assemblyPath] = manager;
            }
            return manager;
        }

        /// <summary>
        /// Loads conceptual content from the file system into the documentation model.
        /// </summary>
        /// <param name="assembly">The assembly to load conceptual content for.</param>
        internal async Task LoadConceptualAsync(DocAssembly assembly)
        {
            if (!Directory.Exists(projectContext.ConceptualPath))
            {
                return;
            }

            var showPlaceholders = projectContext?.ShowPlaceholders ?? true;

            foreach (var ns in assembly.Namespaces)
            {
                // Load namespace-level conceptual content
                var namespacePath = ns.Symbol.IsGlobalNamespace
                    ? projectContext?.ConceptualPath
                    : Path.Combine(projectContext?.ConceptualPath ?? "", ns.Symbol.ToDisplayString().Replace('.', Path.DirectorySeparatorChar));

                if (Directory.Exists(namespacePath))
                {
                    await LoadConceptualFileAsync(namespacePath, DocConstants.SummaryFileName, content => ns.Summary = content, showPlaceholders);
                    await LoadConceptualFileAsync(namespacePath, DocConstants.UsageFileName, content => ns.Usage = content, showPlaceholders);
                    await LoadConceptualFileAsync(namespacePath, DocConstants.ExamplesFileName, content => ns.Examples = content, showPlaceholders);
                    await LoadConceptualFileAsync(namespacePath, DocConstants.BestPracticesFileName, content => ns.BestPractices = content, showPlaceholders);
                    await LoadConceptualFileAsync(namespacePath, DocConstants.PatternsFileName, content => ns.Patterns = content, showPlaceholders);
                    await LoadConceptualFileAsync(namespacePath, DocConstants.ConsiderationsFileName, content => ns.Considerations = content, showPlaceholders);

                    // Load namespace related APIs if markdown file exists
                    var relatedApisPath = Path.Combine(namespacePath, DocConstants.RelatedApisFileName);
                    if (File.Exists(relatedApisPath))
                    {
                        var content = await File.ReadAllTextAsync(relatedApisPath);

                        // Check if this is a placeholder file and if we should skip it
                        if (!showPlaceholders && content.TrimStart().StartsWith("<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->"))
                        {
                            // Skip placeholder files when ShowPlaceholders is false
                        }
                        else
                        {
                            // Simple markdown file with one API per line for now
                            var lines = content.Split('\n');
                            ns.RelatedApis = lines
                                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("<!--"))
                                .Select(line => line.Trim())
                                .ToList();
                        }
                    }
                }

                foreach (var type in ns.Types)
                {
                    // Build type path like /conceptual/System/Text/Json/JsonSerializer/
                    var typeDir = Path.Combine(namespacePath ?? "", type.Symbol.Name);

                    if (Directory.Exists(typeDir))
                    {

                        // Load type-level conceptual content
                        await LoadConceptualFileAsync(typeDir, DocConstants.UsageFileName, content => type.Usage = content, showPlaceholders);
                        await LoadConceptualFileAsync(typeDir, DocConstants.ExamplesFileName, content => type.Examples = content, showPlaceholders);
                        await LoadConceptualFileAsync(typeDir, DocConstants.BestPracticesFileName, content => type.BestPractices = content, showPlaceholders);
                        await LoadConceptualFileAsync(typeDir, DocConstants.PatternsFileName, content => type.Patterns = content, showPlaceholders);
                        await LoadConceptualFileAsync(typeDir, DocConstants.ConsiderationsFileName, content => type.Considerations = content, showPlaceholders);

                        // Load related APIs if markdown file exists
                        var relatedApisPath = Path.Combine(typeDir, DocConstants.RelatedApisFileName);
                        if (File.Exists(relatedApisPath))
                        {
                            var content = await File.ReadAllTextAsync(relatedApisPath);

                            // Check if this is a placeholder file and if we should skip it
                            if (!showPlaceholders && content.TrimStart().StartsWith("<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->"))
                            {
                                // Skip loading placeholder content
                            }
                            else
                            {
                                // Simple markdown file with one API per line for now
                                var lines = content.Split('\n');
                                type.RelatedApis = lines
                                    .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("<!--"))
                                    .Select(line => line.Trim())
                                    .ToList();
                            }
                        }

                        // Load member-specific conceptual content
                        foreach (var member in type.Members)
                        {
                            var memberDir = Path.Combine(typeDir, member.Symbol.Name);
                            if (Directory.Exists(memberDir))
                            {
                                await LoadConceptualFileAsync(memberDir, DocConstants.UsageFileName, content => member.Usage = content, showPlaceholders);
                                await LoadConceptualFileAsync(memberDir, DocConstants.ExamplesFileName, content => member.Examples = content, showPlaceholders);
                                await LoadConceptualFileAsync(memberDir, DocConstants.BestPracticesFileName, content => member.BestPractices = content, showPlaceholders);
                                await LoadConceptualFileAsync(memberDir, DocConstants.PatternsFileName, content => member.Patterns = content, showPlaceholders);
                                await LoadConceptualFileAsync(memberDir, DocConstants.ConsiderationsFileName, content => member.Considerations = content, showPlaceholders);

                                // Load member-specific related APIs
                                var memberRelatedApisPath = Path.Combine(memberDir, DocConstants.RelatedApisFileName);
                                if (File.Exists(memberRelatedApisPath))
                                {
                                    var content = await File.ReadAllTextAsync(memberRelatedApisPath);

                                    // Check if this is a placeholder file and if we should skip it
                                    if (!showPlaceholders && content.TrimStart().StartsWith("<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->"))
                                    {
                                        // Skip loading placeholder content
                                    }
                                    else
                                    {
                                        var lines = content.Split('\n');
                                        member.RelatedApis = lines
                                            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("<!--"))
                                            .Select(line => line.Trim())
                                            .ToList();
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
        /// <param name="showPlaceholders">Whether to load files containing the TODO marker.</param>
        /// <remarks>
        /// This method loads conceptual content as-is without modification. Renderers are responsible
        /// for manipulating the final output based on the content structure (e.g., handling headers,
        /// formatting, or integration with other content).
        /// </remarks>
        internal async Task LoadConceptualFileAsync(string directory, string fileName, Action<string> setter, bool showPlaceholders = true)
        {
            var filePath = Path.Combine(directory, fileName);
            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath);

                // Skip BOM if present
                if (content.Length > 0 && content[0] == '\uFEFF')
                {
                    content = content.Substring(1);
                }

                content = content.Trim();

                // Skip placeholder files entirely when ShowPlaceholders is false
                if (!showPlaceholders && IsTodoPlaceholderFile(content))
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(content))
                {
                    setter(content);
                }
            }
        }

        /// <summary>
        /// Determines if a file contains a TODO placeholder comment.
        /// </summary>
        /// <param name="content">The file content to check.</param>
        /// <returns>true if the content starts with a TODO placeholder comment; otherwise, false.</returns>
        internal static bool IsTodoPlaceholderFile(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            var lines = content.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                // Check if this line matches the TODO comment pattern
                var regex = new Regex(@"^\s*<!--\s*TODO:\s*REMOVE\s+THIS\s+COMMENT\s+AFTER\s+YOU\s+CUSTOMIZE\s+THIS\s+CONTENT\s*-->\s*$", RegexOptions.IgnoreCase);
                return regex.IsMatch(trimmed);
            }

            return false;
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes of all cached <see cref="AssemblyManager"/> instances.
        /// </summary>
        public void Dispose()
        {
            foreach (var manager in assemblyManagerCache.Values)
            {
                manager?.Dispose();
            }
            assemblyManagerCache.Clear();
        }

        #endregion

    }

}