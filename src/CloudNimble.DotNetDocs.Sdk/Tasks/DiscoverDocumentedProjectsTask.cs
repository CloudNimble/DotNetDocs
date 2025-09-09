using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CloudNimble.EasyAF.MSBuild;
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
        /// <returns>true if the task executed successfully; otherwise, false.</returns>
        public override bool Execute()
        {
            try
            {
                Log.LogMessage(MessageImportance.High, $"🔍 Discovering documented projects in {SolutionDir}...");

                // Ensure MSBuild is registered using EasyAF's helper
                MSBuildProjectManager.EnsureMSBuildRegistered();

                var documentedProjects = new List<ITaskItem>();
                var projectFiles = Directory.GetFiles(SolutionDir, "*.csproj", SearchOption.AllDirectories);
                var projectCollection = new ProjectCollection();

                // Set up global properties for evaluation
                var globalProperties = new Dictionary<string, string>
                {
                    { "Configuration", Configuration }
                };

                foreach (var projectFile in projectFiles)
                {
                    var projectName = Path.GetFileNameWithoutExtension(projectFile);
                    
                    // Skip excluded patterns (check against project name, not full path)
                    if (ExcludePatterns != null && ExcludePatterns.Any(pattern => 
                        projectName.Contains(pattern.Replace("*", ""), StringComparison.OrdinalIgnoreCase)))
                    {
                        Log.LogMessage(MessageImportance.Low, $"   ⏭️ Skipping excluded project: {projectName}");
                        continue;
                    }

                    try
                    {
                        // Use MSBuildProjectManager to load the project
                        var manager = new MSBuildProjectManager(projectFile);
                        manager.Load(preserveFormatting: false);

                        if (!manager.IsLoaded)
                        {
                            Log.LogWarning($"Could not load project {projectFile}: {string.Join(", ", manager.ProjectErrors.Select(e => e.ErrorText))}");
                            continue;
                        }

                        // Create an evaluated project to get computed property values
                        var project = new Project(projectFile, globalProperties, null, projectCollection);

                        try
                        {
                            // Check if it's a test project (IsTestProject=true)
                            var isTestProject = project.GetPropertyValue("IsTestProject");
                            if (string.Equals(isTestProject, "true", StringComparison.OrdinalIgnoreCase))
                            {
                                Log.LogMessage(MessageImportance.Low, $"   ⏭️ Skipping test project: {projectName}");
                                continue;
                            }

                            // Check if it's packable (IsPackable != false)
                            // Projects are packable by default unless explicitly set to false
                            var isPackable = project.GetPropertyValue("IsPackable");
                            if (string.Equals(isPackable, "false", StringComparison.OrdinalIgnoreCase))
                            {
                                Log.LogMessage(MessageImportance.Low, $"   ⏭️ Skipping non-packable project: {projectName}");
                                continue;
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