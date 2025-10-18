using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Tools.Commands.Base;
using McMaster.Extensions.CommandLineUtils;

namespace CloudNimble.DotNetDocs.Tools.Commands
{

    /// <summary>
    /// Command-line tool for updating existing .docsproj files to use the latest DotNetDocs.Sdk version from NuGet.
    /// </summary>
    /// <remarks>
    /// This command searches for all .docsproj files in the current directory (and optionally subdirectories),
    /// queries NuGet.org for the latest SDK version, and updates the SDK reference in each file.
    /// </remarks>
    [Command("update", Description = "Update .docsproj files to use the latest DotNetDocs.Sdk version from NuGet.")]
    public class UpdateCommand : DocsCommandBase
    {

        #region Properties

        /// <summary>
        /// Gets or sets the project name or path to a specific .docsproj file to update.
        /// </summary>
        [Option("--project|-p", Description = "Optional. Project name or path to .docsproj file to update. If not specified, updates all .docsproj files in current directory.")]
        public string? ProjectPath { get; set; }

        /// <summary>
        /// Gets or sets whether to search recursively in subdirectories.
        /// </summary>
        [Option("--recursive|-r", Description = "Optional. Search for .docsproj files recursively in subdirectories.")]
        public bool Recursive { get; set; }

        /// <summary>
        /// Gets or sets whether to use the latest prerelease version of the DotNetDocs.Sdk.
        /// </summary>
        [Option("--prerelease", Description = "Optional. Update to the latest prerelease version of DotNetDocs.Sdk instead of the latest stable version.")]
        public bool UsePrerelease { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes the command to update .docsproj SDK references to the latest version from NuGet.
        /// </summary>
        /// <param name="app">The command-line application context used to access command-line arguments and configuration.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is 0 if the update is
        /// successful; otherwise, 1.</returns>
        /// <remarks>
        /// If a specific project is not specified, the method searches for all .docsproj files in the current directory
        /// (and subdirectories if --recursive is specified). The --project option accepts either a project name
        /// (e.g., "MyProject.Docs") or a file path (e.g., "MyProject.Docs.docsproj" or "path/to/MyProject.Docs.docsproj").
        /// Any errors encountered during execution are reported to the console, and a nonzero exit code is returned.
        /// </remarks>
        public async Task<int> OnExecute(CommandLineApplication app)
        {
            WriteHeader();
            try
            {
                // Get latest SDK version from NuGet
                Console.WriteLine($"🔍 Querying NuGet for latest DotNetDocs.Sdk version...");
                string? sdkVersion = await GetLatestSdkVersionAsync(UsePrerelease);
                if (string.IsNullOrEmpty(sdkVersion))
                {
                    Console.WriteLine("❌ Could not retrieve latest version from NuGet");
                    return 1;
                }

                Console.WriteLine($"✅ Found version: {sdkVersion}");

                // Find .docsproj files to update
                string[] docsprojFiles;
                if (!string.IsNullOrEmpty(ProjectPath))
                {
                    // Update specific project
                    string projectFile;

                    // If it doesn't end with .docsproj, assume it's a project name and append .docsproj
                    if (!ProjectPath.EndsWith(".docsproj", StringComparison.OrdinalIgnoreCase))
                    {
                        projectFile = $"{ProjectPath}.docsproj";
                    }
                    else
                    {
                        projectFile = ProjectPath;
                    }

                    // Check if file exists
                    if (!File.Exists(projectFile))
                    {
                        Console.WriteLine($"❌ File not found: {projectFile}");
                        return 1;
                    }

                    docsprojFiles = new[] { projectFile };
                }
                else
                {
                    // Find all .docsproj files
                    var searchOption = Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    docsprojFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.docsproj", searchOption);

                    if (docsprojFiles.Length == 0)
                    {
                        Console.WriteLine($"❌ No .docsproj files found in current directory{(Recursive ? " or subdirectories" : "")}");
                        return 1;
                    }

                    Console.WriteLine($"📁 Found {docsprojFiles.Length} .docsproj file{(docsprojFiles.Length == 1 ? "" : "s")}");
                }

                // Update each file
                int updatedCount = 0;
                int skippedCount = 0;

                foreach (var file in docsprojFiles)
                {
                    var fileName = Path.GetFileName(file);
                    Console.WriteLine($"📝 Processing {fileName}...");

                    var content = await File.ReadAllTextAsync(file);
                    var originalContent = content;

                    // Update SDK reference
                    var updatedContent = System.Text.RegularExpressions.Regex.Replace(
                        content,
                        @"Sdk=""DotNetDocs\.Sdk/[^""]*""",
                        $"Sdk=\"DotNetDocs.Sdk/{sdkVersion}\""
                    );

                    if (updatedContent == originalContent)
                    {
                        Console.WriteLine($"   ⏭️  Skipped (no DotNetDocs.Sdk reference found or already up to date)");
                        skippedCount++;
                    }
                    else
                    {
                        await File.WriteAllTextAsync(file, updatedContent);
                        Console.WriteLine($"   ✅ Updated to DotNetDocs.Sdk/{sdkVersion}");
                        updatedCount++;
                    }
                }

                Console.WriteLine();
                Console.WriteLine($"🎉 Update complete!");
                Console.WriteLine($"   Updated: {updatedCount} file{(updatedCount == 1 ? "" : "s")}");
                if (skippedCount > 0)
                {
                    Console.WriteLine($"   Skipped: {skippedCount} file{(skippedCount == 1 ? "" : "s")}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating .docsproj files: {ex.Message}");
                return 1;
            }
        }

        #endregion

    }

}
