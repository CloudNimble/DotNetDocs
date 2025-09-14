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

        #region Fields

        Dictionary<string, bool> _propertiesToEvaluate = new()
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

    }

}