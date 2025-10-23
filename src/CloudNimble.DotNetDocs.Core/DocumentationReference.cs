using System;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Represents a reference to external documentation to be combined into a documentation collection.
    /// </summary>
    /// <remarks>
    /// This class encapsulates information needed to copy documentation files from a referenced project
    /// and integrate its navigation structure into a collection portal. Similar to MSBuild's ProjectReference,
    /// but for documentation outputs.
    /// </remarks>
    public class DocumentationReference
    {

        #region Properties

        /// <summary>
        /// Gets or sets the path to the destination folder within the collection's documentation root.
        /// </summary>
        /// <value>The relative path where referenced documentation will be copied.</value>
        /// <example>For a microservice named "auth-service", this might be "services/auth".</example>
        public string DestinationPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the root directory containing the referenced documentation outputs.
        /// </summary>
        /// <value>The absolute path to the documentation root of the referenced project.</value>
        /// <example>C:\repos\auth-service\docs</example>
        public string DocumentationRoot { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the documentation type of the referenced project.
        /// </summary>
        /// <value>The documentation format (e.g., "Mintlify", "DocFX", "MkDocs").</value>
        /// <remarks>
        /// This determines which file patterns to copy and whether navigation combining is supported.
        /// </remarks>
        public string DocumentationType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the integration type for Mintlify navigation.
        /// </summary>
        /// <value>Either "Tabs" or "Products" for Mintlify navigation structure.</value>
        /// <remarks>
        /// Only applicable when DocumentationType is "Mintlify". Determines whether the referenced
        /// documentation appears in the top-level tabs or in the products section.
        /// </remarks>
        public string IntegrationType { get; set; } = "Tabs";

        /// <summary>
        /// Gets or sets the path to the navigation configuration file for the referenced documentation.
        /// </summary>
        /// <value>The absolute path to the navigation file (e.g., docs.json for Mintlify).</value>
        /// <example>C:\repos\auth-service\docs\docs.json</example>
        public string NavigationFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the path to the .docsproj file being referenced.
        /// </summary>
        /// <value>The absolute path to the documentation project file.</value>
        /// <example>C:\repos\auth-service\docs\AuthService.docsproj</example>
        public string ProjectPath { get; set; } = string.Empty;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentationReference"/> class.
        /// </summary>
        public DocumentationReference()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentationReference"/> class with a project path.
        /// </summary>
        /// <param name="projectPath">The path to the .docsproj file being referenced.</param>
        public DocumentationReference(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                throw new ArgumentException("Project path cannot be null or whitespace.", nameof(projectPath));
            }

            ProjectPath = projectPath;
        }

        #endregion

    }

}
