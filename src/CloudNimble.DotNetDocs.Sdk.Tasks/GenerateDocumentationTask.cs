using System;
using System.Collections.Generic;
using System.IO;
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
        /// Gets or sets the generated documentation files.
        /// </summary>
        [Output]
        public ITaskItem[] GeneratedFiles { get; set; } = Array.Empty<ITaskItem>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes the task to generate documentation.
        /// </summary>
        /// <returns>True if the task executed successfully; otherwise, false.</returns>
        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.High, "USING COMPILED TASK1");
                Log.LogMessage(MessageImportance.High, "🚀 Generating unified documentation from solution...");
                Log.LogMessage(MessageImportance.Normal, $"📂 Output path: {OutputPath}");
                Log.LogMessage(MessageImportance.Normal, $"🏷️ Namespace mode: {NamespaceMode}");
                Log.LogMessage(MessageImportance.Normal, $"📄 Documentation type: {DocumentationType}");
                Log.LogMessage(MessageImportance.Normal, $"📚 API Reference path: {ApiReferencePath}");

#if NETSTANDARD2_0 || NETFRAMEWORK
                // .NET Framework fallback - just create placeholder files for now
                Log.LogWarning("Documentation generation on .NET Framework MSBuild is limited. Please use dotnet build for full functionality.");
                
                // Create a simple placeholder file to indicate the task ran
                var placeholderPath = Path.Combine(OutputPath, ApiReferencePath, "README.md");
                Directory.CreateDirectory(Path.GetDirectoryName(placeholderPath));
                File.WriteAllText(placeholderPath, "# API Documentation\n\nPlease build with `dotnet build` for full documentation generation.");
                
                GeneratedFiles = new[] { new TaskItem(placeholderPath) };
                return true;
#else

                // Filter assemblies to only those with XML documentation
                var assemblyPairs = new List<(string assemblyPath, string xmlPath)>();
                foreach (var assembly in Assemblies)
                {
                    var assemblyPath = assembly.ItemSpec;
                    var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

                    if (File.Exists(xmlPath))
                    {
                        assemblyPairs.Add((assemblyPath, xmlPath));
                        Log.LogMessage(MessageImportance.Normal, $"  📄 {Path.GetFileName(assemblyPath)} (with XML docs)");
                    }
                    else
                    {
                        Log.LogMessage(MessageImportance.Normal, $"  📦 {Path.GetFileName(assemblyPath)} (skipping - no XML docs)");
                    }
                }

                if (assemblyPairs.Count == 0)
                {
                    Log.LogWarning("No assemblies with XML documentation found to process");
                    GeneratedFiles = Array.Empty<ITaskItem>();
                    return true;
                }

                Log.LogMessage(MessageImportance.Normal, $"📦 Found {assemblyPairs.Count} assemblies to process");

                // Create DI container
                var services = new ServiceCollection();
                
                // Add Options support (required for renderer configuration)
                services.AddOptions();

                // Configure ProjectContext
                services.AddDotNetDocsCore(context =>
                {
                    context.DocumentationRootPath = OutputPath;
                    context.ApiReferencePath = ApiReferencePath;
                    
                    // Parse namespace mode
                    if (Enum.TryParse<NamespaceMode>(NamespaceMode, out var mode))
                    {
                        context.FileNamingOptions.NamespaceMode = mode;
                    }
                    else
                    {
                        Log.LogWarning($"Invalid NamespaceMode '{NamespaceMode}', using default 'Folder'");
                        context.FileNamingOptions.NamespaceMode = Core.Configuration.NamespaceMode.Folder;
                    }
                });

                // Register renderer based on type
                switch (DocumentationType?.ToLowerInvariant())
                {
                    case "mintlify":
                        // Configure MintlifyRendererOptions with default values
                        services.Configure<MintlifyRendererOptions>(options =>
                        {
                            options.GenerateDocsJson = true;
                            options.IncludeIcons = true;
                        });
                        services.AddMintlifyServices();
                        Log.LogMessage(MessageImportance.Normal, "🎨 Using Mintlify renderer");
                        break;
                    case "json":
                        // Configure JsonRendererOptions with default values
                        services.Configure<JsonRendererOptions>(options =>
                        {
                            // Options will use their own defaults
                        });
                        services.AddJsonRenderer();
                        Log.LogMessage(MessageImportance.Normal, "🎨 Using JSON renderer");
                        break;
                    case "yaml":
                        services.AddYamlRenderer();
                        Log.LogMessage(MessageImportance.Normal, "🎨 Using YAML renderer");
                        break;
                    case "markdown":
                    case "default":
                    default:
                        services.AddMarkdownRenderer();
                        Log.LogMessage(MessageImportance.Normal, "🎨 Using Markdown renderer (default)");
                        break;
                }

                // Build service provider
                var serviceProvider = services.BuildServiceProvider();
                var documentationManager = serviceProvider.GetRequiredService<DocumentationManager>();

                Log.LogMessage(MessageImportance.Normal, "🔄 Processing assemblies with DocumentationManager (merged model)...");

                // Run async operation synchronously (MSBuild tasks are synchronous)
                var task = documentationManager.ProcessAsync(assemblyPairs);
                task.GetAwaiter().GetResult();

                // Collect generated files for output (for incremental build support)
                var apiPath = Path.Combine(OutputPath, ApiReferencePath);
                var generatedFilesList = new List<ITaskItem>();

                if (Directory.Exists(apiPath))
                {
                    var files = Directory.GetFiles(apiPath, "*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        var item = new TaskItem(file);
                        item.SetMetadata("RelativePath", Path.GetRelativePath(OutputPath, file));
                        generatedFilesList.Add(item);
                    }
                }

                // Also include docs.json if it exists (for Mintlify)
                var docsJsonPath = Path.Combine(OutputPath, "docs.json");
                if (File.Exists(docsJsonPath))
                {
                    var item = new TaskItem(docsJsonPath);
                    item.SetMetadata("RelativePath", "docs.json");
                    generatedFilesList.Add(item);
                }

                GeneratedFiles = generatedFilesList.ToArray();

                Log.LogMessage(MessageImportance.High, "✅ Documentation generation completed successfully!");
                Log.LogMessage(MessageImportance.Normal, $"📄 Generated {GeneratedFiles.Length} files");

                // Dispose service provider
                if (serviceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                return true;
#endif
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, true);
                return false;
            }
        }

        #endregion

    }

}