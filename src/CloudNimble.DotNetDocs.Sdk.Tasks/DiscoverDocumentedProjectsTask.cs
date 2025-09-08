using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
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
        /// <returns>True if the task executed successfully; otherwise, false.</returns>
        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.High, "USING COMPILED TASK2");
                Log.LogMessage(MessageImportance.High, "🔍 Discovering documented projects in solution...");

                var projectCollection = new ProjectCollection();
                var projects = new List<ITaskItem>();

                // Find all .csproj files in the solution directory
                var projectFiles = Directory.GetFiles(SolutionDir, "*.csproj", SearchOption.AllDirectories)
                    .Where(f => !IsExcluded(f))
                    .ToArray();

                Log.LogMessage(MessageImportance.Normal, $"   Found {projectFiles.Length} project files");

                foreach (var projectFile in projectFiles)
                {
                    try
                    {
                        // Set global properties for evaluation
                        var globalProperties = new Dictionary<string, string>
                        {
                            ["Configuration"] = Configuration,
                            ["TargetFramework"] = TargetFramework
                        };

                        // Load project to read properties
                        var project = projectCollection.LoadProject(projectFile, globalProperties, null);

                        // Check if project should be documented
                        var isPackable = project.GetPropertyValue("IsPackable");
                        var isTestProject = project.GetPropertyValue("IsTestProject");
                        var generateDocs = project.GetPropertyValue("GenerateDotNetDocs");
                        var isDocumentationProject = project.GetPropertyValue("IsDocumentationProject");

                        // Skip documentation projects themselves
                        if (isDocumentationProject == "true")
                        {
                            Log.LogMessage(MessageImportance.Low, $"   Skipping documentation project: {Path.GetFileName(projectFile)}");
                            continue;
                        }

                        // Include project if:
                        // 1. GenerateDotNetDocs is explicitly true, OR
                        // 2. IsPackable is not false AND IsTestProject is not true
                        if (generateDocs == "true" || (isPackable != "false" && isTestProject != "true"))
                        {
                            var projectName = Path.GetFileNameWithoutExtension(projectFile);
                            var item = new TaskItem(projectFile);

                            // Get the actual target frameworks from the project
                            var targetFrameworks = project.GetPropertyValue("TargetFrameworks");
                            var targetFramework = project.GetPropertyValue("TargetFramework");
                            
                            // Use TargetFrameworks (multi-targeting) or TargetFramework (single)
                            var frameworksToUse = !string.IsNullOrEmpty(targetFrameworks) ? targetFrameworks : targetFramework;
                            
                            // For multi-targeting, pick the first framework that matches our preference order
                            string actualFramework = targetFramework;
                            if (!string.IsNullOrEmpty(targetFrameworks))
                            {
                                var frameworks = targetFrameworks.Split(';');
                                // Prefer net8.0, then net9.0, then net10.0, then whatever is first
                                actualFramework = frameworks.FirstOrDefault(f => f == "net8.0") ??
                                                frameworks.FirstOrDefault(f => f == "net9.0") ??
                                                frameworks.FirstOrDefault(f => f == "net10.0") ??
                                                frameworks.FirstOrDefault() ??
                                                "net8.0";
                            }

                            // Add metadata about the project
                            item.SetMetadata("ProjectName", projectName);
                            item.SetMetadata("Configuration", Configuration);
                            item.SetMetadata("TargetFramework", actualFramework);
                            item.SetMetadata("TargetFrameworks", frameworksToUse);
                            item.SetMetadata("IsPackable", isPackable);
                            item.SetMetadata("IsTestProject", isTestProject);
                            item.SetMetadata("GenerateDotNetDocs", generateDocs);

                            projects.Add(item);
                            Log.LogMessage(MessageImportance.Normal, $"   Including project: {projectName}");
                        }
                        else
                        {
                            Log.LogMessage(MessageImportance.Low,
                                $"   Excluding project: {Path.GetFileName(projectFile)} (IsPackable={isPackable}, IsTestProject={isTestProject})");
                        }

                        // Unload project to free memory
                        projectCollection.UnloadProject(project);
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning($"Failed to evaluate project {projectFile}: {ex.Message}");
                    }
                }

                DocumentedProjects = [.. projects];
                Log.LogMessage(MessageImportance.High, $"📚 Found {DocumentedProjects.Length} documented projects for documentation");

                // List the project names
                if (DocumentedProjects.Length > 0)
                {
                    var projectNames = string.Join(", ", DocumentedProjects.Select(p => p.GetMetadata("ProjectName")));
                    Log.LogMessage(MessageImportance.Normal, $"   Projects: {projectNames}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Checks if a project file should be excluded based on exclude patterns.
        /// </summary>
        /// <param name="projectFile">The project file path to check.</param>
        /// <returns>True if the project should be excluded; otherwise, false.</returns>
        private bool IsExcluded(string projectFile)
        {
            if (ExcludePatterns == null || ExcludePatterns.Length == 0)
                return false;

            var fileName = Path.GetFileName(projectFile);
            var directoryName = Path.GetDirectoryName(projectFile);

            foreach (var pattern in ExcludePatterns)
            {
                // Simple wildcard matching
                if (pattern.Contains("*"))
                {
                    var regexPattern = "^" + pattern.Replace(".", "\\.").Replace("*", ".*") + "$";
                    if (System.Text.RegularExpressions.Regex.IsMatch(fileName, regexPattern))
                        return true;

                    // Also check directory patterns
                    if (directoryName != null && System.Text.RegularExpressions.Regex.IsMatch(directoryName, pattern.Replace("*", ".*")))
                        return true;
                }
                else if (fileName.Contains(pattern) || (directoryName != null && directoryName.Contains(pattern)))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

    }

}