using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CloudNimble.DotNetDocs.Tools.Commands
{

    /// <summary>
    /// 
    /// </summary>
    [Command("build", Description = "Build documentation from assemblies")]
    public class BuildCommand
    {

        /// <summary>
        /// 
        /// </summary>
        [Option("--assembly-list|-a", Description = "Path to assembly list file")]
        [Required]
        public string AssemblyListFile { get; set; } = null!;

        /// <summary>
        /// 
        /// </summary>
        [Option("--output|-o", Description = "Output path for documentation")]
        [Required]
        public string OutputPath { get; set; } = null!;

        /// <summary>
        /// 
        /// </summary>
        [Option("--namespace-mode|-n", Description = "Namespace mode (File/Folder)")]
        public string NamespaceMode { get; set; } = "File";

        /// <summary>
        /// 
        /// </summary>
        [Option("--type|-t", Description = "Documentation type (Default/Mintlify/Json/Yaml)")]
        public string DocumentationType { get; set; } = "Default";

        /// <summary>
        /// 
        /// </summary>
        [Option("--api-reference-path", Description = "API reference path")]
        public string ApiReferencePath { get; set; } = "api-reference";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public async Task<int> OnExecute(CommandLineApplication app)
        {
            try
            {
                Console.WriteLine($"🔧 Starting documentation generation...");
                Console.WriteLine($"📁 Assembly list: {AssemblyListFile}");
                Console.WriteLine($"📂 Output path: {OutputPath}");
                Console.WriteLine($"🏷️  Namespace mode: {NamespaceMode}");
                Console.WriteLine($"📄 Documentation type: {DocumentationType}");
                Console.WriteLine($"📚 API Reference path: {ApiReferencePath}");

                // Read assembly list
                if (!File.Exists(AssemblyListFile))
                {
                    Console.WriteLine($"❌ Assembly list file not found: {AssemblyListFile}");
                    return 1;
                }

                var assemblyPaths = File.ReadAllLines(AssemblyListFile)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => line.Trim())
                    .ToList();

                Console.WriteLine($"📦 Found {assemblyPaths.Count} assemblies to process");

                if (assemblyPaths.Count == 0)
                {
                    Console.WriteLine("⚠️ No assemblies found to process");
                    return 1;
                }

                // Create assembly/XML pairs (only include assemblies with XML docs)
                var assemblies = new List<(string assemblyPath, string xmlPath)>();
                foreach (var assemblyPath in assemblyPaths)
                {
                    var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

                    // Only include assemblies that have corresponding XML documentation
                    if (File.Exists(xmlPath))
                    {
                        assemblies.Add((assemblyPath, xmlPath));
                        Console.WriteLine($"  📄 {Path.GetFileName(assemblyPath)} (with XML docs)");
                    }
                    else
                    {
                        Console.WriteLine($"  📦 {Path.GetFileName(assemblyPath)} (skipping - no XML docs)");
                    }
                }

                // Use Hosting APIs with DI container
                var host = Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        // Register DotNetDocs services
                        services.AddDotNetDocsCore(context =>
                        {
                            context.DocumentationRootPath = OutputPath;
                            context.ApiReferencePath = ApiReferencePath;
                            context.FileNamingOptions.NamespaceMode = Enum.Parse<NamespaceMode>(NamespaceMode);
                        });

                        // Register renderer based on documentation type
                        switch (DocumentationType.ToLowerInvariant())
                        {
                            case "mintlify":
                                services.AddMintlifyServices();
                                Console.WriteLine("🎨 Using Mintlify renderer");
                                break;
                            case "json":
                                services.AddJsonRenderer();
                                Console.WriteLine("🎨 Using JSON renderer");
                                break;
                            case "yaml":
                                services.AddYamlRenderer();
                                Console.WriteLine("🎨 Using YAML renderer");
                                break;
                            case "markdown":
                            case "default":
                            default:
                                services.AddMarkdownRenderer();
                                Console.WriteLine("🎨 Using Markdown renderer (default)");
                                break;
                        }
                    })
                    .Build();

                // Process assemblies with DocumentationManager from DI container
                Console.WriteLine("🚀 Processing assemblies with DocumentationManager (merged model)...");

                using (var scope = host.Services.CreateScope())
                {
                    var documentationManager = scope.ServiceProvider.GetRequiredService<DocumentationManager>();
                    await documentationManager.ProcessAsync(assemblies);
                }

                Console.WriteLine("✅ Documentation generation completed successfully!");
                Console.WriteLine("📚 Generated unified documentation from all assemblies");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during documentation generation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return 1;
            }

        }

    }

}