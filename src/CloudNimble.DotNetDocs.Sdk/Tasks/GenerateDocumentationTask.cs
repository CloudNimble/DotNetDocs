using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

#if NET8_0_OR_GREATER
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Core.Renderers;
using CloudNimble.DotNetDocs.Mintlify;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
#endif

namespace CloudNimble.DotNetDocs.Sdk.Tasks
{

    /// <summary>
    /// MSBuild task that generates documentation using DocumentationManager directly within the MSBuild process.
    /// </summary>
    public class GenerateDocumentationTask : Task
    {

        #region Properties

        /// <summary>
        /// Gets or sets the assemblies to generate documentation for.
        /// </summary>
        [Required]
        public ITaskItem[] Assemblies { get; set; } = Array.Empty<ITaskItem>();

        /// <summary>
        /// Gets or sets the output path for the generated documentation.
        /// </summary>
        [Required]
        public string OutputPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API reference path relative to the output path.
        /// </summary>
        public string ApiReferencePath { get; set; } = "api-reference";

        /// <summary>
        /// Gets or sets the namespace mode for organizing documentation files.
        /// </summary>
        public string NamespaceMode { get; set; } = "Folder";

        /// <summary>
        /// Gets or sets the documentation type to generate.
        /// </summary>
        public string DocumentationType { get; set; } = "Mintlify";

        /// <summary>
        /// Gets or sets the conceptual content path.
        /// </summary>
        public string? ConceptualPath { get; set; }

        /// <summary>
        /// Gets or sets whether to generate placeholders for missing documentation.
        /// </summary>
        public bool GeneratePlaceholders { get; set; } = true;

        /// <summary>
        /// Gets or sets the list of generated files (output).
        /// </summary>
        [Output]
        public ITaskItem[] GeneratedFiles { get; set; } = Array.Empty<ITaskItem>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes the task to generate documentation.
        /// </summary>
        /// <returns>true if the task executed successfully; otherwise, false.</returns>
        public override bool Execute()
        {
#if NET8_0_OR_GREATER
            try
            {
                Log.LogMessage(MessageImportance.High, $"🚀 Generating {DocumentationType} documentation...");

                // Set up dependency injection
                var services = new ServiceCollection();

                // Configure the project context
                services.AddDotNetDocsCore(context =>
                {
                    context.DocumentationRootPath = OutputPath;
                    context.ApiReferencePath = ApiReferencePath;
                    context.ConceptualPath = ConceptualPath ?? Path.Combine(OutputPath, "conceptual");
                    context.FileNamingOptions.NamespaceMode = Enum.TryParse<NamespaceMode>(NamespaceMode, true, out var mode) ? mode : Core.Configuration.NamespaceMode.Folder;

                    // NamespaceFileMode will be set via the NamespaceMode property
                    // This is handled internally by the Core library
                });

                // Add the appropriate renderer based on documentation type
                switch (DocumentationType.ToLowerInvariant())
                {
                    case "mintlify":
                        services.AddMintlifyServices(options =>
                        {
                            options.GenerateDocsJson = true;
                            options.GenerateNamespaceIndex = true;
                            options.IncludeIcons = true;
                        });
                        break;
                    case "markdown":
                        services.AddMarkdownRenderer();
                        break;
                    case "json":
                        services.AddJsonRenderer();
                        break;
                    case "yaml":
                        services.AddYamlRenderer();
                        break;
                    default:
                        Log.LogError($"Unknown documentation type: {DocumentationType}");
                        return false;
                }

                var serviceProvider = services.BuildServiceProvider();
                var manager = serviceProvider.GetRequiredService<DocumentationManager>();

                // Process each assembly
                var processedCount = 0;
                var markdownFiles = new List<string>();
                var imageFiles = new List<string>();

                foreach (var assembly in Assemblies)
                {
                    var assemblyPath = assembly.ItemSpec;
                    var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

                    if (!File.Exists(assemblyPath))
                    {
                        Log.LogWarning($"Assembly not found: {assemblyPath}");
                        continue;
                    }

                    Log.LogMessage(MessageImportance.High, $"   📖 Processing {Path.GetFileName(assemblyPath)}...");

                    try
                    {
                        manager.ProcessAsync(assemblyPath, xmlPath).GetAwaiter().GetResult();
                        processedCount++;

                        // Collect statistics
                        if (DocumentationType.Equals("Mintlify", StringComparison.OrdinalIgnoreCase))
                        {
                            var mdFiles = Directory.GetFiles(OutputPath, "*.mdx", SearchOption.AllDirectories);
                            markdownFiles.AddRange(mdFiles);

                            var pngFiles = Directory.GetFiles(OutputPath, "*.png", SearchOption.AllDirectories);
                            var jpgFiles = Directory.GetFiles(OutputPath, "*.jpg", SearchOption.AllDirectories);
                            var svgFiles = Directory.GetFiles(OutputPath, "*.svg", SearchOption.AllDirectories);
                            var imgFiles = pngFiles.Concat(jpgFiles).Concat(svgFiles);
                            imageFiles.AddRange(imgFiles);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"Failed to process {assemblyPath}: {ex.Message}");
                        return false;
                    }
                }

                // Log statistics
                Log.LogMessage(MessageImportance.High, "📊 Documentation Statistics:");
                Log.LogMessage(MessageImportance.High, $"   📄 Documentation type: {DocumentationType}");
                
                if (markdownFiles.Any())
                {
                    Log.LogMessage(MessageImportance.High, $"   📝 Markdown files: {markdownFiles.Distinct().Count()}");
                }
                
                if (imageFiles.Any())
                {
                    Log.LogMessage(MessageImportance.High, $"   🖼️ Image files: {imageFiles.Distinct().Count()}");
                }

                // Return generated files as output
                var allGeneratedFiles = markdownFiles.Concat(imageFiles).Distinct();
                GeneratedFiles = allGeneratedFiles.Select(f => new TaskItem(f)).ToArray();

                return processedCount > 0;
            }
            catch (Exception ex)
            {
                Log.LogError($"Documentation generation failed: {ex.Message}");
                Log.LogMessage(MessageImportance.Low, ex.StackTrace);
                return false;
            }
#else
            // For netstandard2.0, we can't use the documentation generation
            // This would typically shell out to a tool instead
            Log.LogWarning("Documentation generation is not supported on .NET Framework. Please use the dotnet tool instead.");
            return true;
#endif
        }

        #endregion

    }

}