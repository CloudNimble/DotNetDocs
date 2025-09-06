using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CloudNimble.DotNetDocs.Tools
{

    internal class Program
    {

        static async Task<int> Main(string[] args)
        {

            try
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: dotnet run <assembly-list-file> <output-path> [namespace-mode] [documentation-type]");
                    Console.WriteLine();
                    Console.WriteLine("Example: dotnet run assembly-list.txt docs File Mintlify");
                    return 1;
                }

                string assemblyListFile = args[0];
                string outputPath = args[1];
                string namespaceMode = args.Length >= 3 ? args[2] : "File";
                string documentationType = args.Length >= 4 ? args[3] : "Default";

                Console.WriteLine($"🔧 Starting documentation generation...");
                Console.WriteLine($"📁 Assembly list: {assemblyListFile}");
                Console.WriteLine($"📂 Output path: {outputPath}");
                Console.WriteLine($"🏷️  Namespace mode: {namespaceMode}");
                Console.WriteLine($"📄 Documentation type: {documentationType}");

                // Read assembly list
                if (!File.Exists(assemblyListFile))
                {
                    Console.WriteLine($"❌ Assembly list file not found: {assemblyListFile}");
                    return 1;
                }

                var assemblyPaths = File.ReadAllLines(assemblyListFile)
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
                            context.OutputPath = outputPath;
                            context.FileNamingOptions.NamespaceMode = Enum.Parse<NamespaceMode>(namespaceMode);
                        });

                        // Register renderer based on documentation type
                        switch (documentationType.ToLowerInvariant())
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