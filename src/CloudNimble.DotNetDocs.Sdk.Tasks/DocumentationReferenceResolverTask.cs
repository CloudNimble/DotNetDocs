using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CloudNimble.DotNetDocs.Sdk.Tasks
{

    /// <summary>
    /// MSBuild task that resolves and validates DocumentationReference items.
    /// </summary>
    /// <remarks>
    /// This task processes DocumentationReference items from the .docsproj file, validates that
    /// referenced projects exist and have valid documentation outputs, and populates metadata
    /// needed by the documentation generation pipeline.
    /// </remarks>
    public class DocumentationReferenceResolverTask : Task
    {

        #region Properties

        /// <summary>
        /// Gets or sets the DocumentationReference items to resolve.
        /// </summary>
        [Required]
        public ITaskItem[] DocumentationReferences { get; set; } = [];

        /// <summary>
        /// Gets or sets the configuration to use when evaluating referenced projects.
        /// </summary>
        public string Configuration { get; set; } = "Release";

        /// <summary>
        /// Gets or sets the documentation type of the collection project.
        /// </summary>
        /// <remarks>
        /// Used to validate that referenced projects use compatible documentation formats.
        /// References with mismatched types will be skipped with a warning.
        /// </remarks>
        public string? DocumentationType { get; set; }

        /// <summary>
        /// Gets or sets the resolved DocumentationReference items with populated metadata.
        /// </summary>
        [Output]
        public ITaskItem[] ResolvedDocumentationReferences { get; set; } = [];

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes the task to resolve documentation references.
        /// </summary>
        /// <returns>true if the task executed successfully; otherwise, false.</returns>
        public override bool Execute()
        {
            try
            {
                if (DocumentationReferences is null || DocumentationReferences.Length == 0)
                {
                    Log.LogMessage(MessageImportance.Low, "No DocumentationReferences to resolve");
                    ResolvedDocumentationReferences = [];
                    return true;
                }

                Log.LogMessage(MessageImportance.High, $"🔍 Resolving {DocumentationReferences.Length} DocumentationReference(s)...");

                var resolvedReferences = new List<ITaskItem>();

                foreach (var reference in DocumentationReferences)
                {
                    var projectPath = reference.ItemSpec;

                    if (string.IsNullOrWhiteSpace(projectPath))
                    {
                        Log.LogWarning($"DocumentationReference has empty project path, skipping");
                        continue;
                    }

                    // Resolve relative path to absolute
                    if (!Path.IsPathRooted(projectPath))
                    {
                        var baseDirectory = Directory.GetCurrentDirectory();
                        projectPath = Path.GetFullPath(Path.Combine(baseDirectory, projectPath));
                    }

                    // Validate that the referenced project exists
                    if (!File.Exists(projectPath))
                    {
                        Log.LogError($"Referenced documentation project not found: {projectPath}");
                        return false;
                    }

                    Log.LogMessage(MessageImportance.Normal, $"   ✅ Found reference: {Path.GetFileName(projectPath)}");

                    // Load the referenced project to extract properties
                    var globalProperties = new Dictionary<string, string>
                    {
                        { "Configuration", Configuration }
                    };

                    var projectCollection = new ProjectCollection();

                    try
                    {
                        var project = new Project(projectPath, globalProperties, null, projectCollection);

                        // Extract required properties from the referenced project
                        var documentationRoot = project.GetPropertyValue("DocumentationRoot");
                        var documentationType = project.GetPropertyValue("DocumentationType");

                        if (string.IsNullOrWhiteSpace(documentationRoot))
                        {
                            Log.LogError($"Referenced project {projectPath} does not have a DocumentationRoot property");
                            return false;
                        }

                        if (string.IsNullOrWhiteSpace(documentationType))
                        {
                            Log.LogError($"Referenced project {projectPath} does not have a DocumentationType property");
                            return false;
                        }

                        // Check if the referenced project's documentation type matches the collection's type
                        if (!string.IsNullOrWhiteSpace(DocumentationType) &&
                            !documentationType.Equals(DocumentationType, StringComparison.OrdinalIgnoreCase))
                        {
                            Log.LogWarning($"⚠️  Skipping documentation reference '{Path.GetFileName(projectPath)}' because it uses '{documentationType}' format. " +
                                         $"Only '{DocumentationType}' documentation can be combined with {DocumentationType} collections. " +
                                         $"Cross-format documentation combination is not currently supported.");
                            continue;
                        }

                        // Validate that the documentation root exists
                        if (!Directory.Exists(documentationRoot))
                        {
                            Log.LogError($"Documentation root does not exist for {projectPath}: {documentationRoot}");
                            return false;
                        }

                        // Get or default the DestinationPath from metadata
                        var destinationPath = reference.GetMetadata("DestinationPath");
                        if (string.IsNullOrWhiteSpace(destinationPath))
                        {
                            // Default to project name if not specified
                            destinationPath = Path.GetFileNameWithoutExtension(projectPath);
                            Log.LogMessage(MessageImportance.Low, $"      DestinationPath not specified, defaulting to: {destinationPath}");
                        }

                        // Get IntegrationType from metadata or default to "Tabs"
                        var integrationType = reference.GetMetadata("IntegrationType");
                        if (string.IsNullOrWhiteSpace(integrationType))
                        {
                            integrationType = "Tabs";
                        }

                        // Determine navigation file path based on documentation type
                        var navigationFilePath = GetNavigationFilePath(documentationRoot, documentationType);
                        if (!string.IsNullOrWhiteSpace(navigationFilePath) && !File.Exists(navigationFilePath))
                        {
                            Log.LogWarning($"Navigation file not found for {projectPath}: {navigationFilePath}");
                            navigationFilePath = string.Empty;
                        }

                        // Create the resolved task item with all metadata
                        var resolvedItem = new TaskItem(projectPath);
                        resolvedItem.SetMetadata("ProjectPath", projectPath);
                        resolvedItem.SetMetadata("DocumentationRoot", documentationRoot);
                        resolvedItem.SetMetadata("DestinationPath", destinationPath);
                        resolvedItem.SetMetadata("DocumentationType", documentationType);
                        resolvedItem.SetMetadata("IntegrationType", integrationType);
                        resolvedItem.SetMetadata("NavigationFilePath", navigationFilePath);

                        resolvedReferences.Add(resolvedItem);

                        Log.LogMessage(MessageImportance.Normal, $"      DocumentationType: {documentationType}");
                        Log.LogMessage(MessageImportance.Normal, $"      DestinationPath: {destinationPath}");
                        Log.LogMessage(MessageImportance.Normal, $"      IntegrationType: {integrationType}");
                    }
                    finally
                    {
                        projectCollection.UnloadAllProjects();
                        projectCollection.Dispose();
                    }
                }

                ResolvedDocumentationReferences = resolvedReferences.ToArray();
                Log.LogMessage(MessageImportance.High, $"✅ Resolved {ResolvedDocumentationReferences.Length} DocumentationReference(s)");

                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, showStackTrace: true);
                return false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Determines the navigation file path based on documentation type.
        /// </summary>
        /// <param name="documentationRoot">The documentation root directory.</param>
        /// <param name="documentationType">The type of documentation system.</param>
        /// <returns>The path to the navigation file, or empty string if not applicable.</returns>
        private string GetNavigationFilePath(string documentationRoot, string documentationType)
        {
            return documentationType.ToLowerInvariant() switch
            {
                "mintlify" => Path.Combine(documentationRoot, "docs.json"),
                "docfx" => Path.Combine(documentationRoot, "toc.yml"),
                "mkdocs" => Path.Combine(documentationRoot, "mkdocs.yml"),
                "jekyll" => Path.Combine(documentationRoot, "_config.yml"),
                "hugo" => Path.Combine(documentationRoot, "hugo.toml"),
                _ => string.Empty
            };
        }

        #endregion

    }

}
