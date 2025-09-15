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

                await GenerateConceptualFilesForAssembly(model);
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
            foreach (var pair in assemblies)
            {
                var manager = GetOrCreateAssemblyManager(pair.assemblyPath, pair.xmlPath);
                var model = await manager.DocumentAsync(projectContext);

                // Load conceptual content if path provided
                if (!string.IsNullOrWhiteSpace(projectContext.ConceptualPath))
                {
                    await LoadConceptualAsync(model);
                }

                docAssemblies.Add(model);
            }

            // Merge all DocAssembly models into unified model
            var mergedModel = await MergeDocAssembliesAsync(docAssemblies);

            // Apply enrichers to merged model
            foreach (var enricher in enrichers)
            {
                await enricher.EnrichAsync(mergedModel);
            }

            // Apply transformers to merged model
            foreach (var transformer in transformers)
            {
                await transformer.TransformAsync(mergedModel);
            }

            // Apply renderers once with merged model
            foreach (var renderer in renderers)
            {
                await renderer.RenderAsync(mergedModel);
            }
        }

        #endregion

        #region Private Methods

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

            if (existingNamespace == null)
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

            if (existingType == null)
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

            if (existingMember == null)
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
        /// Recursively enriches a documentation model and all its children.
        /// </summary>
        internal async Task EnrichModelAsync(DocEntity entity, IDocEnricher enricher)
        {
            await enricher.EnrichAsync(entity);

            // Recursively enrich children
            if (entity is DocAssembly assembly)
            {
                foreach (var ns in assembly.Namespaces)
                {
                    await EnrichModelAsync(ns, enricher);
                }
            }
            else if (entity is DocNamespace ns)
            {
                foreach (var type in ns.Types)
                {
                    await EnrichModelAsync(type, enricher);
                }
            }
            else if (entity is DocType type)
            {
                foreach (var member in type.Members)
                {
                    await EnrichModelAsync(member, enricher);
                }
            }
            else if (entity is DocMember member)
            {
                foreach (var param in member.Parameters)
                {
                    await EnrichModelAsync(param, enricher);
                }
            }
        }

        /// <summary>
        /// Recursively transforms a documentation model and all its children.
        /// </summary>
        internal async Task TransformModelAsync(DocEntity entity, IDocTransformer transformer)
        {
            await transformer.TransformAsync(entity);

            // Recursively transform children
            if (entity is DocAssembly assembly)
            {
                foreach (var ns in assembly.Namespaces)
                {
                    await TransformModelAsync(ns, transformer);
                }
            }
            else if (entity is DocNamespace ns)
            {
                foreach (var type in ns.Types)
                {
                    await TransformModelAsync(type, transformer);
                }
            }
            else if (entity is DocType type)
            {
                foreach (var member in type.Members)
                {
                    await TransformModelAsync(member, transformer);
                }
            }
            else if (entity is DocMember member)
            {
                foreach (var param in member.Parameters)
                {
                    await TransformModelAsync(param, transformer);
                }
            }
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

                                // Load parameter-specific documentation
                                foreach (var parameter in member.Parameters)
                                {
                                    var paramFile = Path.Combine(memberDir, $"{DocConstants.ParameterFilePrefix}{parameter.Symbol.Name}{DocConstants.ParameterFileExtension}");
                                    if (File.Exists(paramFile))
                                    {
                                        var content = await File.ReadAllTextAsync(paramFile);
                                        content = content.Trim();
                                        
                                        // Check if this is a placeholder file and if we should skip it
                                        if (!showPlaceholders && content.StartsWith("<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->"))
                                        {
                                            // Skip loading placeholder content
                                            continue;
                                        }
                                        
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
        /// <param name="showPlaceholders">Whether to load files containing the TODO marker.</param>
        internal async Task LoadConceptualFileAsync(string directory, string fileName, Action<string> setter, bool showPlaceholders = true)
        {
            var filePath = Path.Combine(directory, fileName);
            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath);
                content = content.Trim();
                
                // Check if this is a placeholder file and if we should process it
                if (!showPlaceholders && content.StartsWith("<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->"))
                {
                    // Remove the placeholder marker and any immediately following placeholder content
                    var lines = content.Split('\n', StringSplitOptions.None);
                    var filteredLines = new List<string>();
                    var skipNextEmptyLine = true;
                    
                    foreach (var line in lines.Skip(1)) // Skip the first line (the comment)
                    {
                        var trimmedLine = line.Trim();
                        
                        // Skip the immediate placeholder content line after the comment
                        if (skipNextEmptyLine && 
                            (trimmedLine.Contains("placeholder", StringComparison.OrdinalIgnoreCase) ||
                             trimmedLine.StartsWith("This is placeholder", StringComparison.OrdinalIgnoreCase)))
                        {
                            skipNextEmptyLine = false; // We've found and skipped the placeholder line
                            continue;
                        }
                        
                        // Skip empty lines immediately after placeholder content
                        if (!skipNextEmptyLine && string.IsNullOrWhiteSpace(line))
                        {
                            continue; // Skip empty lines between placeholder and real content
                        }
                        
                        // If we get here with content, it's real content
                        if (!string.IsNullOrWhiteSpace(trimmedLine))
                        {
                            skipNextEmptyLine = false; // We've found real content
                            filteredLines.Add(line);
                        }
                    }
                    
                    content = string.Join('\n', filteredLines).Trim();
                    
                    // If no real content remains after filtering, skip entirely
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        return;
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(content))
                {
                    setter(content);
                }
            }
        }

        /// <summary>
        /// Generates placeholder conceptual documentation files for an assembly.
        /// </summary>
        /// <param name="assembly">The assembly to generate conceptual files for.</param>
        internal async Task GenerateConceptualFilesForAssembly(DocAssembly assembly)
        {
            foreach (var ns in assembly.Namespaces)
            {
                // Generate namespace-level conceptual files
                var namespaceName = projectContext.GetSafeNamespaceName(ns.Symbol);
                var namespacePath = namespaceName == "global" 
                    ? "global"
                    : namespaceName.Replace('.', Path.DirectorySeparatorChar);
                var namespaceDir = Path.Combine(projectContext.ConceptualPath, namespacePath);
                
                // Create namespace directory if it doesn't exist
                Directory.CreateDirectory(namespaceDir);
                
                // Generate namespace summary file (only for namespaces, not types)
                await GeneratePlaceholderFileAsync(namespaceDir, DocConstants.SummaryFileName,
                    $"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\n# Summary\n\nProvide a brief description of the `{namespaceName}` namespace's purpose and functionality.\n");

                foreach (var type in ns.Types)
                {
                    // Type directory is within the namespace directory
                    var typeDir = Path.Combine(namespaceDir, type.Symbol.Name);

                    // Create directory if it doesn't exist
                    Directory.CreateDirectory(typeDir);

                    // Generate type-level conceptual placeholder files
                    await GeneratePlaceholderFileAsync(typeDir, DocConstants.UsageFileName,
                        $"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\n# Usage\n\nDescribe how to use `{type.Symbol.Name}` here.\n");
                    
                    await GeneratePlaceholderFileAsync(typeDir, DocConstants.ExamplesFileName,
                        $"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\n# Examples\n\nProvide examples of using `{type.Symbol.Name}` here.\n");
                    
                    await GeneratePlaceholderFileAsync(typeDir, DocConstants.BestPracticesFileName,
                        $"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\n# Best Practices\n\nDocument best practices for `{type.Symbol.Name}` here.\n");
                    
                    await GeneratePlaceholderFileAsync(typeDir, DocConstants.PatternsFileName,
                        $"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\n# Patterns\n\nDescribe common patterns when using `{type.Symbol.Name}` here.\n");
                    
                    await GeneratePlaceholderFileAsync(typeDir, DocConstants.ConsiderationsFileName,
                        $"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\n# Considerations\n\nNote important considerations for `{type.Symbol.Name}` here.\n");
                    
                    await GeneratePlaceholderFileAsync(typeDir, DocConstants.RelatedApisFileName,
                        $"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\n# Related APIs\n\n<!-- List related APIs one per line -->\n");

                    // Generate member-specific conceptual placeholder files
                    foreach (var member in type.Members)
                    {
                        var memberDir = Path.Combine(typeDir, member.Symbol.Name);
                        Directory.CreateDirectory(memberDir);

                        await GeneratePlaceholderFileAsync(memberDir, DocConstants.UsageFileName,
                            $"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\n# Usage\n\nDescribe how to use `{member.Symbol.Name}` here.\n");
                        
                        await GeneratePlaceholderFileAsync(memberDir, DocConstants.ExamplesFileName,
                            $"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\n# Examples\n\nProvide examples of using `{member.Symbol.Name}` here.\n");
                        
                        await GeneratePlaceholderFileAsync(memberDir, DocConstants.BestPracticesFileName,
                            $"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\n# Best Practices\n\nDocument best practices for `{member.Symbol.Name}` here.\n");
                        
                        await GeneratePlaceholderFileAsync(memberDir, DocConstants.PatternsFileName,
                            $"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\n# Patterns\n\nDescribe common patterns when using `{member.Symbol.Name}` here.\n");
                        
                        await GeneratePlaceholderFileAsync(memberDir, DocConstants.ConsiderationsFileName,
                            $"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\n# Considerations\n\nNote important considerations for `{member.Symbol.Name}` here.\n");
                        
                        await GeneratePlaceholderFileAsync(memberDir, DocConstants.RelatedApisFileName,
                            $"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\n# Related APIs\n\n<!-- List related APIs one per line -->\n");

                        // Generate parameter-specific placeholder files
                        foreach (var parameter in member.Parameters)
                        {
                            var paramFile = Path.Combine(memberDir, 
                                $"{DocConstants.ParameterFilePrefix}{parameter.Symbol.Name}{DocConstants.ParameterFileExtension}");
                            
                            await GeneratePlaceholderFileAsync(paramFile,
                                $"<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\n" +
                                $"<!-- Enhanced documentation for parameter '{parameter.Symbol.Name}' -->\n" +
                                $"<!-- This overrides the XML documentation for this parameter -->\n\n" +
                                $"Describe the `{parameter.Symbol.Name}` parameter in detail here.\n");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates a placeholder file if it doesn't already exist.
        /// </summary>
        /// <param name="directory">The directory to create the file in.</param>
        /// <param name="fileName">The name of the file to create.</param>
        /// <param name="content">The placeholder content to write.</param>
        internal async Task GeneratePlaceholderFileAsync(string directory, string fileName, string content)
        {
            var filePath = Path.Combine(directory, fileName);
            await GeneratePlaceholderFileAsync(filePath, content);
        }

        /// <summary>
        /// Generates a placeholder file if it doesn't already exist.
        /// </summary>
        /// <param name="filePath">The full path of the file to create.</param>
        /// <param name="content">The placeholder content to write.</param>
        internal async Task GeneratePlaceholderFileAsync(string filePath, string content)
        {
            // Only create if file doesn't exist - don't overwrite existing content
            if (!File.Exists(filePath))
            {
                await File.WriteAllTextAsync(filePath, content);
            }
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