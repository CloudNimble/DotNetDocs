using System;
using System.Collections.Generic;
using System.IO;
using CloudNimble.DotNetDocs.Core.Configuration;
using Microsoft.CodeAnalysis;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Represents MSBuild project context for source intent in documentation generation.
    /// </summary>
    /// <remarks>
    /// Provides metadata such as referenced assembly paths and the conceptual documentation folder path.
    /// Used by <see cref="AssemblyManager.DocumentAsync"/> to enhance metadata extraction.
    /// <example>
    /// <code>
    /// // Default (public members only)
    /// var context = new ProjectContext("ref1.dll", "ref2.dll") { ConceptualPath = "conceptual" };
    ///
    /// // Include public and internal members
    /// var context = new ProjectContext([Accessibility.Public, Accessibility.Internal], "ref1.dll", "ref2.dll");
    ///
    /// var model = await manager.DocumentAsync("MyLib.dll", "MyLib.xml", context);
    /// </code>
    /// </example>
    /// </remarks>
    public class ProjectContext
    {

        #region Properties

        /// <summary>
        /// Gets or sets the path to the API reference documentation.
        /// </summary>
        public string ApiReferencePath { get; set; } = "api-reference";

        /// <summary>
        /// Gets or sets the path to the conceptual documentation folder.
        /// </summary>
        /// <value>
        /// The file system path to the folder containing conceptual documentation files.
        /// </value>
        public string ConceptualPath { get; set; } = "conceptual";

        /// <summary>
        /// Gets or sets the output path for generated documentation.
        /// </summary>
        /// <value>
        /// The file system path where documentation output will be generated.
        /// </value>
        public string DocumentationRootPath { get; set; } = "docs";

        /// <summary>
        /// Gets or sets the list of type patterns to exclude from documentation.
        /// </summary>
        /// <value>
        /// Set of type patterns to exclude. Supports wildcards (*) for flexible matching.
        /// This is useful for filtering out compiler-generated or test framework-injected types.
        /// </value>
        /// <remarks>
        /// Patterns can use wildcards:
        /// - "*.TypeName" matches TypeName in any namespace
        /// - "Namespace.*.TypeName" matches TypeName in any sub-namespace of Namespace
        /// - "Full.Namespace.TypeName" matches exact fully qualified type name
        /// </remarks>
        /// <example>
        /// ExcludedTypes = new HashSet&lt;string&gt; 
        /// { 
        ///     "*.MicrosoftTestingPlatformEntryPoint",  // Matches in any namespace
        ///     "*.SelfRegisteredExtensions",             // Matches in any namespace
        ///     "System.Runtime.CompilerServices.*"       // Matches any type in this namespace
        /// }
        /// </example>
        public HashSet<string> ExcludedTypes { get; set; }

        /// <summary>
        /// Gets or sets the file naming options for documentation generation.
        /// </summary>
        /// <value>
        /// Configuration for how documentation files are named and organized.
        /// </value>
        public FileNamingOptions FileNamingOptions { get; set; } = new FileNamingOptions();

        /// <summary>
        /// Gets or sets the list of member accessibilities to include in documentation.
        /// </summary>
        /// <value>
        /// List of accessibility levels to include. Defaults to Public only.
        /// </value>
        public List<Accessibility> IncludedMembers { get; set; }

        /// <summary>
        /// Gets or sets the collection of paths to referenced assemblies.
        /// </summary>
        /// <value>
        /// A collection of file system paths to assemblies referenced by the project being documented.
        /// </value>
        public List<string> References { get; set; } = [];

        /// <summary>
        /// Gets or sets whether to show placeholder content in the documentation.
        /// </summary>
        /// <value>
        /// When true (default), placeholder content is included. When false, files containing the
        /// TODO marker comment are skipped during loading.
        /// </value>
        public bool ShowPlaceholders { get; set; } = true;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="ProjectContext"/> with default settings.
        /// </summary>
        public ProjectContext() : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ProjectContext"/> with optional included members and referenced assemblies.
        /// </summary>
        /// <param name="includedMembers">List of member accessibilities to include. Defaults to Public if null.</param>
        /// <param name="references">Paths to referenced assemblies.</param>
        public ProjectContext(List<Accessibility>? includedMembers, params string[]? references)
        {
            IncludedMembers = includedMembers ?? [Accessibility.Public];
            if (references is not null)
            {
                References.AddRange(references);
            }
            
            // Default exclusions for common test framework injected types
            ExcludedTypes = new HashSet<string>
            {
                "*.MicrosoftTestingPlatformEntryPoint",
                "*.SelfRegisteredExtensions"
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Converts a namespace string to a folder path based on the configured file naming options.
        /// </summary>
        /// <param name="namespaceName">The namespace name to convert (e.g., "System.Collections.Generic").</param>
        /// <returns>The folder path representation of the namespace.</returns>
        /// <remarks>
        /// When <see cref="FileNamingOptions.NamespaceMode"/> is <see cref="NamespaceMode.Folder"/>,
        /// this returns a path with folders for each namespace part (e.g., "System/Collections/Generic").
        /// When using <see cref="NamespaceMode.File"/>, this returns an empty string as no folder
        /// structure is created.
        /// </remarks>
        public string GetNamespaceFolderPath(string namespaceName)
        {
            if (FileNamingOptions.NamespaceMode == NamespaceMode.Folder)
            {
                // Handle global namespace
                if (namespaceName == "global" || string.IsNullOrWhiteSpace(namespaceName))
                {
                    return "global";
                }

                // Split namespace and create folder path
                var namespaceParts = namespaceName.Split('.');
                return Path.Combine(namespaceParts);
            }

            // In File mode, no folder structure is created
            return string.Empty;
        }

        /// <summary>
        /// Gets the full file path for a type, including namespace folder structure if in Folder mode.
        /// </summary>
        /// <param name="fullyQualifiedTypeName">The fully qualified type name (e.g., "System.Text.Json.JsonSerializer").</param>
        /// <param name="extension">The file extension without the dot (e.g., "md", "yaml").</param>
        /// <returns>The file path for the type.</returns>
        /// <remarks>
        /// In Folder mode: "System.Text.Json.JsonSerializer" becomes "System/Text/Json/JsonSerializer.md"
        /// In File mode: "System.Text.Json.JsonSerializer" becomes "System_Text_Json_JsonSerializer.md" (using configured separator)
        /// </remarks>
        public string GetTypeFilePath(string fullyQualifiedTypeName, string extension)
        {
            if (string.IsNullOrWhiteSpace(fullyQualifiedTypeName))
            {
                throw new ArgumentException("Type name cannot be null or whitespace.", nameof(fullyQualifiedTypeName));
            }

            // Split the fully qualified name into namespace and type name
            var lastDotIndex = fullyQualifiedTypeName.LastIndexOf('.');
            string namespacePart;
            string typeName;

            if (lastDotIndex > 0)
            {
                namespacePart = fullyQualifiedTypeName.Substring(0, lastDotIndex);
                typeName = fullyQualifiedTypeName.Substring(lastDotIndex + 1);
            }
            else
            {
                // No namespace (global namespace)
                namespacePart = "global";
                typeName = fullyQualifiedTypeName;
            }

            if (FileNamingOptions.NamespaceMode == NamespaceMode.Folder)
            {
                // Create folder path from namespace
                var folderPath = GetNamespaceFolderPath(namespacePart);
                return Path.Combine(folderPath, $"{typeName}.{extension}");
            }
            else
            {
                // Use flat file structure with separator
                var fileName = $"{fullyQualifiedTypeName.Replace('.', FileNamingOptions.NamespaceSeparator)}.{extension}";
                return fileName;
            }
        }

        /// <summary>
        /// Gets the safe namespace name for a given namespace symbol.
        /// </summary>
        /// <param name="namespaceSymbol">The namespace symbol.</param>
        /// <returns>A safe namespace name, using "global" for the global namespace.</returns>
        public string GetSafeNamespaceName(INamespaceSymbol namespaceSymbol)
        {
            return namespaceSymbol.IsGlobalNamespace ? "global" : namespaceSymbol.ToDisplayString();
        }

        /// <summary>
        /// Checks if a type should be excluded from documentation based on exclusion patterns.
        /// </summary>
        /// <param name="fullyQualifiedTypeName">The fully qualified type name to check.</param>
        /// <returns>True if the type should be excluded; otherwise, false.</returns>
        public bool IsTypeExcluded(string fullyQualifiedTypeName)
        {
            if (ExcludedTypes is null || ExcludedTypes.Count == 0)
            {
                return false;
            }

            foreach (var pattern in ExcludedTypes)
            {
                if (MatchesPattern(fullyQualifiedTypeName, pattern))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a fully qualified type name matches a wildcard pattern.
        /// </summary>
        /// <param name="typeName">The fully qualified type name.</param>
        /// <param name="pattern">The pattern with optional wildcards (*).</param>
        /// <returns>True if the type name matches the pattern; otherwise, false.</returns>
        internal static bool MatchesPattern(string typeName, string pattern)
        {
            // Convert wildcard pattern to regex pattern
            // Escape special regex characters except *
            var regexPattern = System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*");  // Convert * to regex .*
            
            // Add anchors to match the entire string
            regexPattern = "^" + regexPattern + "$";
            
            return System.Text.RegularExpressions.Regex.IsMatch(typeName, regexPattern);
        }

        /// <summary>
        /// Ensures that the output directory structure exists for all namespaces in the assembly model.
        /// </summary>
        /// <param name="assemblyModel">The assembly model containing namespaces to create directories for.</param>
        /// <param name="outputPath">The base output path where directories will be created.</param>
        /// <remarks>
        /// This method creates the necessary folder structure when using Folder mode.
        /// In File mode, it simply ensures the base output directory exists.
        /// This centralizes folder creation logic so renderers can assume directories exist.
        /// </remarks>
        public void EnsureOutputDirectoryStructure(DocAssembly assemblyModel, string outputPath)
        {
            ArgumentNullException.ThrowIfNull(assemblyModel);
            ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

            // Always ensure the base output directory exists
            Directory.CreateDirectory(outputPath);

            // If we're in Folder mode, create the namespace folder structure
            if (FileNamingOptions.NamespaceMode == NamespaceMode.Folder)
            {
                foreach (var ns in assemblyModel.Namespaces)
                {
                    var namespaceName = GetSafeNamespaceName(ns.Symbol);
                    var namespaceFolderPath = GetNamespaceFolderPath(namespaceName);
                    
                    if (!string.IsNullOrEmpty(namespaceFolderPath))
                    {
                        var fullPath = Path.Combine(outputPath, namespaceFolderPath);
                        Directory.CreateDirectory(fullPath);
                    }
                }
            }
        }

        #endregion

    }

}