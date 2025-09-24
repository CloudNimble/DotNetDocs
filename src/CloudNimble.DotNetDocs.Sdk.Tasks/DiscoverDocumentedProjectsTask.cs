using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CloudNimble.EasyAF.Core;
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

        #region Fields

        internal Dictionary<string, bool> _propertiesToEvaluate = new()
        {
            { "IsTestProject", false }, // Exclude test projects
            { "IsPackable", true }, // Include only packable projects (default is true)
            { "ExcludeFromDocumentation", false } // Exclude projects explicitly marked to be excluded
        };

        #endregion

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
        public ITaskItem[] DocumentedProjects { get; set; } = [];

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

                // Search for all .NET project file types and filter out excluded patterns
                var projectExtensions = new[] { "*.csproj", "*.vbproj", "*.fsproj" };
                var projectFiles = projectExtensions
                    .SelectMany(ext => Directory.GetFiles(SolutionDir, ext, SearchOption.AllDirectories))
                    .Where(file => ExcludePatterns == null || !ExcludePatterns.Any(pattern =>
                        Path.GetFileNameWithoutExtension(file).Contains(pattern.Replace("*", ""), StringComparison.OrdinalIgnoreCase)))
                    .ToArray();

                var projectCollection = new ProjectCollection();

                // Set up global properties for evaluation
                var globalProperties = new Dictionary<string, string>
                {
                    { "Configuration", Configuration }
                };

                foreach (var projectFile in projectFiles)
                {
                    var projectName = Path.GetFileNameWithoutExtension(projectFile);

                    try
                    {
                        // Create an evaluated project to get computed property values
                        var project = new Project(projectFile, globalProperties, null, projectCollection);

                        try
                        {

                            bool shouldSkip = false;
                            foreach (var prop in _propertiesToEvaluate)
                            {
                                var propValue = project.GetPropertyValue(prop.Key);
                                if (string.Equals(propValue, "true", StringComparison.OrdinalIgnoreCase) != prop.Value)
                                {
                                    Log.LogMessage(MessageImportance.Low, $"   ⏭️ Skipping project {projectName} due to property {prop.Key}={propValue}");
                                    shouldSkip = true;
                                    break;
                                }
                            }
                            if (shouldSkip)
                            {
                                continue; // Skip to next project
                            }

                            // This is a project we should document
                            var item = new TaskItem(projectFile);

                            // Set metadata from the evaluated project
                            var assemblyName = project.GetPropertyValue("AssemblyName");
                            if (string.IsNullOrEmpty(assemblyName))
                            {
                                assemblyName = projectName;
                            }

                            var targetFramework = project.GetPropertyValue("TargetFramework");
                            if (string.IsNullOrEmpty(targetFramework))
                            {
                                // For multi-targeted projects, use TargetFrameworks and pick the first one
                                var targetFrameworks = project.GetPropertyValue("TargetFrameworks");
                                if (!string.IsNullOrEmpty(targetFrameworks))
                                {
                                    targetFramework = targetFrameworks.Split(';')[0].Trim();
                                }
                                else
                                {
                                    targetFramework = TargetFramework; // Use default
                                }
                            }

                            item.SetMetadata("AssemblyName", assemblyName);
                            item.SetMetadata("OutputPath", $"bin\\{Configuration}");
                            item.SetMetadata("TargetFramework", targetFramework);
                            item.SetMetadata("TargetPath", $"bin\\{Configuration}\\{targetFramework}\\{assemblyName}.dll");

                            documentedProjects.Add(item);
                            Log.LogMessage(MessageImportance.High, $"   ✅ Found packable project: {projectName}");
                        }
                        finally
                        {
                            // Always unload the project to free resources
                            projectCollection.UnloadProject(project);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning($"Could not evaluate project {projectFile}: {ex.Message}");
                    }
                }

                // Clean up the project collection
                projectCollection.UnloadAllProjects();

                DocumentedProjects = [.. documentedProjects];
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

        #region Internal Methods

        /// <summary>
        /// Discovers project files in the specified directory.
        /// </summary>
        /// <param name="solutionDir">The solution directory to search.</param>
        /// <param name="excludePatterns">Patterns to exclude from discovery.</param>
        /// <returns>Array of discovered project file paths.</returns>
        internal string[] DiscoverProjectFiles(string solutionDir, string[]? excludePatterns)
        {
            Ensure.ArgumentNotNullOrWhiteSpace(solutionDir, nameof(solutionDir));

            var projectExtensions = new[] { "*.csproj", "*.vbproj", "*.fsproj" };
            return projectExtensions
                .SelectMany(ext => Directory.GetFiles(solutionDir, ext, SearchOption.AllDirectories))
                .Where(file => ShouldIncludeProject(file, excludePatterns))
                .ToArray();
        }

        /// <summary>
        /// Determines if a project file should be included based on exclude patterns.
        /// </summary>
        /// <param name="projectFilePath">The project file path.</param>
        /// <param name="excludePatterns">Patterns to exclude.</param>
        /// <returns>true if the project should be included; otherwise, false.</returns>
        internal bool ShouldIncludeProject(string projectFilePath, string[]? excludePatterns)
        {
            Ensure.ArgumentNotNullOrWhiteSpace(projectFilePath, nameof(projectFilePath));

            if (excludePatterns == null || excludePatterns.Length == 0)
            {
                return true;
            }

            var fileName = Path.GetFileNameWithoutExtension(projectFilePath);
            return !excludePatterns.Any(pattern =>
                fileName.Contains(pattern.Replace("*", ""), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Evaluates if a project should be documented based on its properties.
        /// </summary>
        /// <param name="project">The MSBuild project to evaluate.</param>
        /// <returns>true if the project should be documented; otherwise, false.</returns>
        internal bool ShouldDocumentProject(Project project)
        {
            Ensure.ArgumentNotNull(project, nameof(project));

            foreach (var prop in _propertiesToEvaluate)
            {
                var propValue = project.GetPropertyValue(prop.Key);
                if (string.Equals(propValue, "true", StringComparison.OrdinalIgnoreCase) != prop.Value)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a task item with metadata for a documented project.
        /// </summary>
        /// <param name="project">The MSBuild project.</param>
        /// <param name="projectFilePath">The project file path.</param>
        /// <returns>A task item with appropriate metadata.</returns>
        internal ITaskItem CreateProjectTaskItem(Project project, string projectFilePath)
        {
            Ensure.ArgumentNotNull(project, nameof(project));
            Ensure.ArgumentNotNullOrWhiteSpace(projectFilePath, nameof(projectFilePath));

            var projectName = Path.GetFileNameWithoutExtension(projectFilePath);
            var item = new TaskItem(projectFilePath);

            // Set metadata from the evaluated project
            var assemblyName = project.GetPropertyValue("AssemblyName");
            if (string.IsNullOrEmpty(assemblyName))
            {
                assemblyName = projectName;
            }

            var targetFramework = GetProjectTargetFramework(project);

            item.SetMetadata("AssemblyName", assemblyName);
            item.SetMetadata("OutputPath", $"bin\\{Configuration}");
            item.SetMetadata("TargetFramework", targetFramework);
            item.SetMetadata("TargetPath", $"bin\\{Configuration}\\{targetFramework}\\{assemblyName}.dll");

            return item;
        }

        /// <summary>
        /// Gets the target framework for a project, handling both single and multi-targeted projects.
        /// </summary>
        /// <param name="project">The MSBuild project.</param>
        /// <returns>The target framework string.</returns>
        internal string GetProjectTargetFramework(Project project)
        {
            Ensure.ArgumentNotNull(project, nameof(project));

            var targetFramework = project.GetPropertyValue("TargetFramework");
            if (!string.IsNullOrEmpty(targetFramework))
            {
                return targetFramework;
            }

            // For multi-targeted projects, use TargetFrameworks and pick the first one
            var targetFrameworks = project.GetPropertyValue("TargetFrameworks");
            if (!string.IsNullOrEmpty(targetFrameworks))
            {
                return targetFrameworks.Split(';')[0].Trim();
            }

            // Use default
            return TargetFramework;
        }

        #endregion

    }

}