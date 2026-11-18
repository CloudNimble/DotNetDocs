using System;
using System.Collections.Concurrent;
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
    public partial class DocumentationManager : IDisposable
    {

        #region Private Fields

        private readonly ConcurrentDictionary<string, AssemblyManager> assemblyManagerCache = new();
        private readonly IEnumerable<IDocEnricher> enrichers;
        private readonly IEnumerable<IDocReferenceHandler> referenceHandlers;
        private readonly IEnumerable<IDocRenderer> renderers;
        private readonly IEnumerable<IDocTransformer> transformers;
        private readonly ProjectContext projectContext;

        [GeneratedRegex(@"^\s*<!--\s*TODO:\s*REMOVE\s+THIS\s+COMMENT\s+AFTER\s+YOU\s+CUSTOMIZE\s+THIS\s+CONTENT\s*-->\s*$",
            RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
        private static partial Regex TodoPlaceholderRegex();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentationManager"/> class.
        /// </summary>
        /// <param name="projectContext">The project context containing configuration settings.</param>
        /// <param name="enrichers">The enrichers to apply to documentation entities.</param>
        /// <param name="transformers">The transformers to apply to the documentation model.</param>
        /// <param name="renderers">The renderers to generate output formats.</param>
        /// <param name="referenceHandlers">The handlers for processing documentation references.</param>
        /// <remarks>
        /// This constructor is designed to work with dependency injection containers.
        /// All parameters accept IEnumerable collections that are typically injected by the DI container.
        /// </remarks>
        public DocumentationManager(
            ProjectContext projectContext,
            IEnumerable<IDocEnricher>? enrichers = null,
            IEnumerable<IDocTransformer>? transformers = null,
            IEnumerable<IDocRenderer>? renderers = null,
            IEnumerable<IDocReferenceHandler>? referenceHandlers = null)
        {
            this.projectContext = projectContext ?? throw new ArgumentNullException(nameof(projectContext));
            this.enrichers = enrichers ?? [];
            this.transformers = transformers ?? [];
            this.renderers = renderers ?? [];
            this.referenceHandlers = referenceHandlers ?? [];
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
        /// <exception cref="InvalidOperationException">The project context has no <see cref="ProjectContext.ConceptualPath"/>.</exception>
        /// <remarks>
        /// The assemblies are documented and merged the same way <see cref="ProcessAsync(IEnumerable{ValueTuple{string, string}})"/>
        /// does it, and then <see cref="GeneratePlaceholdersAsync(DocAssembly)"/> writes the placeholders. Nothing is rendered.
        /// This method always writes placeholders, regardless of <see cref="ProjectContext.ConceptualDocsEnabled"/>: that
        /// setting controls the full pipeline, and calling this method is an explicit request for placeholders.
        /// The documented models are cached, so a later call to <see cref="ProcessAsync(IEnumerable{ValueTuple{string, string}})"/>
        /// with the same assemblies doesn't document them again.
        /// </remarks>
        public async Task CreateConceptualFilesAsync(IEnumerable<(string assemblyPath, string xmlPath)> assemblies)
        {
            ArgumentNullException.ThrowIfNull(assemblies);

            var mergedModel = await BuildMergedModelAsync([.. assemblies]);
            if (mergedModel is null)
            {
                return;
            }

            await GeneratePlaceholdersAsync(mergedModel);
        }

        /// <summary>
        /// Writes placeholder conceptual documentation files for every namespace and type in a documentation model.
        /// </summary>
        /// <param name="model">The documentation model to write placeholders for, usually the merged model of a run.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="model"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The project context has no <see cref="ProjectContext.ConceptualPath"/>.</exception>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// var model = await assemblyManager.DocumentAsync(projectContext);
        /// await documentationManager.GeneratePlaceholdersAsync(model);
        /// ]]>
        /// </code>
        /// </example>
        /// <remarks>
        /// Each namespace folder gets summary, usage, examples, best practices, patterns, considerations, and related APIs files,
        /// and each type folder gets the same files except summary, at the locations <see cref="ProjectContext.GetConceptualNamespacePath(string?)"/>
        /// and <see cref="ProjectContext.GetConceptualTypePath(string?, string)"/> return. A file that already exists is never
        /// overwritten, so edited content is safe. After the built-in placeholders are written, each registered renderer's
        /// <see cref="IDocRenderer.RenderPlaceholdersAsync(DocAssembly)"/> runs, one at a time, so a renderer can add files of its own.
        /// </remarks>
        public async Task GeneratePlaceholdersAsync(DocAssembly model)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (string.IsNullOrWhiteSpace(projectContext.ConceptualPath))
            {
                throw new InvalidOperationException($"Placeholders can't be generated because {nameof(ProjectContext)}.{nameof(ProjectContext.ConceptualPath)} is not set.");
            }

            Directory.CreateDirectory(projectContext.GetFullConceptualPath());

            foreach (var ns in model.Namespaces)
            {
                var namespaceName = ProjectContext.IsGlobalNamespaceName(ns.Name) ? ProjectContext.GlobalNamespaceName : ns.Name;
                var namespaceDirectory = projectContext.GetConceptualNamespacePath(ns.Name);
                Directory.CreateDirectory(namespaceDirectory);

                await WriteFileIfMissingAsync(namespaceDirectory, DocConstants.SummaryFileName, GetSummaryTemplate(namespaceName));
                await WritePlaceholderSetAsync(namespaceDirectory, namespaceName);

                foreach (var type in ns.Types)
                {
                    var typeDirectory = projectContext.GetConceptualTypePath(ns.Name, type.Name);
                    Directory.CreateDirectory(typeDirectory);

                    await WritePlaceholderSetAsync(typeDirectory, type.Name);
                }
            }

            // Renderers run one at a time, after the built-in files exist, so they can't race each other or the files above.
            foreach (var renderer in renderers)
            {
                await renderer.RenderPlaceholdersAsync(model);
            }
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
            var assemblyList = assemblies.ToList();
            var hasReferences = projectContext.DocumentationReferences.Any();

            // Documentation-only mode: process references and/or template without local assemblies
            if (assemblyList.Count == 0)
            {
                // Run pipeline if we have references OR a MintlifyTemplate
                if (hasReferences || projectContext.HasMintlifyTemplate)
                {
                    if (hasReferences)
                    {
                        // Process referenced documentation with format-specific handlers
                        await ProcessDocumentationReferencesAsync();
                    }

                    // Renderers run for navigation file processing (pass null model)
                    // For Mintlify: generates docs.json from template + discovers content files
                    foreach (var renderer in renderers)
                    {
                        await renderer.RenderAsync(null);
                    }
                }

                return;
            }

            // STEPS 1-2: Document every assembly and merge the models, shared with CreateConceptualFilesAsync
            var mergedModel = await BuildMergedModelAsync(assemblyList);

            if (mergedModel is not null && projectContext.ConceptualDocsEnabled)
            {
                // STEP 3: Generate placeholder files once for the merged model, so shadow classes are deduplicated.
                // Without a ConceptualPath there is nowhere to write them, so the pipeline skips this step.
                if (!string.IsNullOrWhiteSpace(projectContext.ConceptualPath))
                {
                    await GeneratePlaceholdersAsync(mergedModel);
                }

                // STEP 4: Load conceptual content for merged model (after placeholders exist)
                await LoadConceptualAsync(mergedModel);
            }

            // STEP 5: Apply enrichers, transformers, and renderers
            if (mergedModel is not null)
            {
                foreach (var enricher in enrichers)
                {
                    await enricher.EnrichAsync(mergedModel);
                }

                foreach (var transformer in transformers)
                {
                    await transformer.TransformAsync(mergedModel);
                }
            }

            foreach (var renderer in renderers)
            {
                await renderer.RenderAsync(mergedModel);
            }

            // STEP 6: Process referenced documentation files if any references exist
            // Note: Navigation combining happens inside each renderer's RenderAsync() before saving
            if (hasReferences)
            {
                await ProcessDocumentationReferencesAsync();
            }
        }


        #endregion

        #region Internal Methods

        /// <summary>
        /// Documents each assembly and merges the results into one model.
        /// </summary>
        /// <param name="assemblies">The assembly and XML path pairs to document.</param>
        /// <returns>The merged model, or null when no assemblies are provided.</returns>
        /// <remarks>
        /// The assemblies are documented one at a time with cached <see cref="AssemblyManager"/> instances and merged before
        /// anything else runs. When several assemblies extend the same external type, merging leaves one shadow class for it,
        /// so its placeholders are written once.
        /// </remarks>
        internal async Task<DocAssembly?> BuildMergedModelAsync(IReadOnlyList<(string assemblyPath, string xmlPath)> assemblies)
        {
            ArgumentNullException.ThrowIfNull(assemblies);

            var docAssemblies = new List<DocAssembly>();
            foreach (var (assemblyPath, xmlPath) in assemblies)
            {
                var manager = GetOrCreateAssemblyManager(assemblyPath, xmlPath);
                docAssemblies.Add(await manager.DocumentAsync(projectContext));
            }

            return await MergeDocAssembliesAsync(docAssemblies);
        }

        /// <summary>
        /// Merges multiple DocAssembly models into a single unified model.
        /// </summary>
        /// <param name="assemblies">The collection of DocAssembly models to merge.</param>
        /// <returns>A task representing the asynchronous merge operation, or null if no assemblies are provided.</returns>
        internal async Task<DocAssembly?> MergeDocAssembliesAsync(List<DocAssembly> assemblies)
        {
            ArgumentNullException.ThrowIfNull(assemblies);

            if (assemblies.Count == 0)
            {
                // Return null for documentation-only scenarios (no assemblies to document)
                return null;
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
            ArgumentNullException.ThrowIfNull(mergedAssembly);
            ArgumentNullException.ThrowIfNull(sourceNamespace);

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
            ArgumentNullException.ThrowIfNull(mergedNamespace);
            ArgumentNullException.ThrowIfNull(sourceType);

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
            ArgumentNullException.ThrowIfNull(mergedType);
            ArgumentNullException.ThrowIfNull(sourceMember);

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
        /// <remarks>
        /// This method is thread-safe and ensures that only one <see cref="AssemblyManager"/> instance
        /// is created per assembly path, even when called concurrently from multiple threads.
        /// </remarks>
        internal AssemblyManager GetOrCreateAssemblyManager(string assemblyPath, string xmlPath)
        {
            return assemblyManagerCache.GetOrAdd(assemblyPath, _ => new AssemblyManager(assemblyPath, xmlPath));
        }

        /// <summary>
        /// Loads conceptual content from the file system into the documentation model.
        /// </summary>
        /// <param name="assembly">The assembly to load conceptual content for.</param>
        internal async Task LoadConceptualAsync(DocAssembly assembly)
        {
            var conceptualPath = projectContext.GetFullConceptualPath();
            if (!Directory.Exists(conceptualPath))
            {
                return;
            }

            var showPlaceholders = projectContext.ShowPlaceholders;

            foreach (var ns in assembly.Namespaces)
            {
                // Load namespace-level conceptual content from the same folder the renderers write placeholders to
                var namespacePath = projectContext.GetConceptualNamespacePath(ns.Name);

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
                    // Type folders look like {conceptual}/System/Text/Json/JsonSerializer
                    var typeDir = projectContext.GetConceptualTypePath(ns.Name, type.Name);

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
                            var memberDir = projectContext.GetConceptualMemberPath(ns.Name, type.Name, member.Name);
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

                return TodoPlaceholderRegex().IsMatch(trimmed);
            }

            return false;
        }

        /// <summary>
        /// Processes referenced documentation using format-specific handlers.
        /// </summary>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        /// <remarks>
        /// Each reference is processed by the appropriate <see cref="IDocReferenceHandler"/>
        /// based on its documentation type. Handlers are responsible for copying files,
        /// rewriting content paths, and relocating resources.
        /// </remarks>
        internal async Task ProcessDocumentationReferencesAsync()
        {
            foreach (var reference in projectContext.DocumentationReferences)
            {
                var handler = referenceHandlers
                    .FirstOrDefault(h => h.DocumentationType == reference.DocumentationType);

                if (handler is not null)
                {
                    await handler.ProcessAsync(reference, projectContext.DocumentationRootPath);
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the placeholder template for best practices documentation.
        /// </summary>
        /// <param name="entityName">The name of the namespace or type.</param>
        /// <returns>The placeholder content.</returns>
        private static string GetBestPracticesTemplate(string entityName)
        {
            return $"""
                <!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
                # Best Practices

                Document best practices for `{entityName}` here.


                """;
        }

        /// <summary>
        /// Gets the placeholder template for considerations documentation.
        /// </summary>
        /// <param name="entityName">The name of the namespace or type.</param>
        /// <returns>The placeholder content.</returns>
        private static string GetConsiderationsTemplate(string entityName)
        {
            return $"""
                <!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
                # Considerations

                Document considerations for `{entityName}` here.


                """;
        }

        /// <summary>
        /// Gets the placeholder template for examples documentation.
        /// </summary>
        /// <param name="entityName">The name of the namespace or type.</param>
        /// <returns>The placeholder content.</returns>
        private static string GetExamplesTemplate(string entityName)
        {
            return $"""
                <!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
                # Examples

                Provide examples of using `{entityName}` here.

                ```csharp
                // Example code here
                ```


                """;
        }

        /// <summary>
        /// Gets the placeholder template for patterns documentation.
        /// </summary>
        /// <param name="entityName">The name of the namespace or type.</param>
        /// <returns>The placeholder content.</returns>
        private static string GetPatternsTemplate(string entityName)
        {
            return $"""
                <!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
                # Patterns

                Document common patterns for `{entityName}` here.


                """;
        }

        /// <summary>
        /// Gets the placeholder template for related APIs documentation.
        /// </summary>
        /// <param name="entityName">The name of the namespace or type.</param>
        /// <returns>The placeholder content.</returns>
        private static string GetRelatedApisTemplate(string entityName)
        {
            return $"""
                <!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
                # Related APIs

                - API 1
                - API 2


                """;
        }

        /// <summary>
        /// Gets the placeholder template for summary documentation.
        /// </summary>
        /// <param name="entityName">The name of the namespace.</param>
        /// <returns>The placeholder content.</returns>
        private static string GetSummaryTemplate(string entityName)
        {
            return $"""
                <!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
                # Summary

                Describe the purpose and overview of `{entityName}` here.


                """;
        }

        /// <summary>
        /// Gets the placeholder template for usage documentation.
        /// </summary>
        /// <param name="entityName">The name of the namespace or type.</param>
        /// <returns>The placeholder content.</returns>
        private static string GetUsageTemplate(string entityName)
        {
            return $"""
                <!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->
                # Usage

                Describe how to use `{entityName}` here.


                """;
        }

        /// <summary>
        /// Writes a file unless it already exists.
        /// </summary>
        /// <param name="directory">The folder to write the file in.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="content">The content to write, as UTF-8 without a byte order mark.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="FileMode.CreateNew"/> checks and creates the file in one step, so concurrent callers can't both write it.
        /// The caller that loses gets an <see cref="IOException"/> for an existing file and leaves that file alone.
        /// </para>
        /// <para>
        /// Line endings are converted to <see cref="Environment.NewLine"/>. A string literal takes its line endings from the
        /// source file, which depend on how the repository was checked out, so the written files don't.
        /// </para>
        /// </remarks>
        private static async Task WriteFileIfMissingAsync(string directory, string fileName, string content)
        {
            var path = Path.Combine(directory, fileName);

            try
            {
                await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(content.ReplaceLineEndings());
            }
            catch (IOException) when (File.Exists(path))
            {
                // The file already exists, either from an earlier run or from a concurrent caller, and is never overwritten.
            }
        }

        /// <summary>
        /// Writes the placeholders that namespaces and types share: usage, examples, best practices, patterns, considerations, and related APIs.
        /// </summary>
        /// <param name="directory">The conceptual folder of the namespace or type.</param>
        /// <param name="entityName">The name shown in the placeholder text.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Task WritePlaceholderSetAsync(string directory, string entityName)
        {
            await WriteFileIfMissingAsync(directory, DocConstants.UsageFileName, GetUsageTemplate(entityName));
            await WriteFileIfMissingAsync(directory, DocConstants.ExamplesFileName, GetExamplesTemplate(entityName));
            await WriteFileIfMissingAsync(directory, DocConstants.BestPracticesFileName, GetBestPracticesTemplate(entityName));
            await WriteFileIfMissingAsync(directory, DocConstants.PatternsFileName, GetPatternsTemplate(entityName));
            await WriteFileIfMissingAsync(directory, DocConstants.ConsiderationsFileName, GetConsiderationsTemplate(entityName));
            await WriteFileIfMissingAsync(directory, DocConstants.RelatedApisFileName, GetRelatedApisTemplate(entityName));
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