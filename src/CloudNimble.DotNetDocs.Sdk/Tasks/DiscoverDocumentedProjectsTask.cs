using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CloudNimble.DotNetDocs.Sdk.Tasks
{

    /// <summary>
    /// MSBuild task that discovers which projects in a solution should be documented based on their MSBuild properties.
    /// </summary>
    public class DiscoverDocumentedProjectsTask : Task
    {

        #region Properties

        /// <summary>
        /// Gets or sets the solution directory to search for projects.
        /// </summary>
        [Required]
        public string SolutionDir { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the configuration to use when evaluating projects.
        /// </summary>
        public string Configuration { get; set; } = "Release";

        /// <summary>
        /// Gets or sets the target framework to use when evaluating projects.
        /// </summary>
        public string TargetFramework { get; set; } = "net8.0";

        /// <summary>
        /// Gets or sets the exclude patterns for projects that should not be documented.
        /// </summary>
        public string[]? ExcludePatterns { get; set; }

        /// <summary>
        /// Gets or sets the discovered projects that should be documented.
        /// </summary>
        [Output]
        public ITaskItem[] DocumentedProjects { get; set; } = Array.Empty<ITaskItem>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes the task to discover documented projects.
        /// </summary>
        /// <returns>true if the task executed successfully; otherwise, false.</returns>
        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.High, $"🔍 Discovering documented projects in {SolutionDir}...");

                var documentedProjects = new List<ITaskItem>();
                var projectFiles = Directory.GetFiles(SolutionDir, "*.csproj", SearchOption.AllDirectories);

                foreach (var projectFile in projectFiles)
                {
                    // Skip excluded patterns
                    if (ExcludePatterns != null && ExcludePatterns.Any(pattern => projectFile.Contains(pattern)))
                    {
                        Log.LogMessage(MessageImportance.Low, $"   ⏭️ Skipping excluded project: {Path.GetFileName(projectFile)}");
                        continue;
                    }

                    try
                    {
                        // For MSBuild tasks, we'll use a simplified approach
                        // Parse the project file to check for GenerateDocumentationFile
                        var projectContent = File.ReadAllText(projectFile);
                        if (projectContent.Contains("<GenerateDocumentationFile>true</GenerateDocumentationFile>", StringComparison.OrdinalIgnoreCase))
                        {
                            var item = new TaskItem(projectFile);
                            // Set basic metadata
                            var projectName = Path.GetFileNameWithoutExtension(projectFile);
                            item.SetMetadata("AssemblyName", projectName);
                            item.SetMetadata("OutputPath", $"bin\\{Configuration}");
                            item.SetMetadata("TargetFramework", TargetFramework);
                            item.SetMetadata("TargetPath", $"bin\\{Configuration}\\{TargetFramework}\\{projectName}.dll");

                            documentedProjects.Add(item);
                            Log.LogMessage(MessageImportance.High, $"   ✅ Found documented project: {Path.GetFileName(projectFile)}");
                        }
                        else
                        {
                            Log.LogMessage(MessageImportance.Low, $"   ⏭️ Skipping undocumented project: {Path.GetFileName(projectFile)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning($"Could not evaluate project {projectFile}: {ex.Message}");
                    }
                }

                DocumentedProjects = documentedProjects.ToArray();
                Log.LogMessage(MessageImportance.High, $"📊 Found {DocumentedProjects.Length} documented projects");

                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to discover documented projects: {ex.Message}");
                return false;
            }
        }

        #endregion

    }

}