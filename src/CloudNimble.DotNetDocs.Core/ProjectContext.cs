using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        #region Fields

        /// <summary>
        /// The name used for the global namespace in file and folder paths.
        /// </summary>
        internal const string GlobalNamespaceName = "global";

        #endregion

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
        /// Gets or sets whether to include fields in the documentation.
        /// </summary>
        /// <value>
        /// When true, public fields (including constants) are included in the documentation.
        /// Defaults to false since fields are generally discouraged in favor of properties.
        /// </value>
        public bool IncludeFields { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include members inherited from <see cref="System.Object"/>.
        /// </summary>
        /// <value>
        /// <c>true</c> to document <c>ToString()</c>, <c>GetHashCode()</c>, <c>Equals()</c>, etc.
        /// on every type; <c>false</c> to exclude these common members. Default is <c>true</c>.
        /// </value>
        /// <remarks>
        /// Setting this to <c>false</c> reduces documentation noise while still including
        /// members inherited from other base types and interfaces. This matches the behavior
        /// of Microsoft's official .NET documentation.
        /// </remarks>
        public bool IncludeSystemObjectInheritance { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to create documentation for external types that have extension methods.
        /// </summary>
        /// <value>
        /// <c>true</c> to create minimal <see cref="DocType"/> entries for types outside the assembly
        /// that are extended by extension methods; <c>false</c> to only relocate extensions for types
        /// within the assembly. Default is <c>true</c>.
        /// </value>
        /// <remarks>
        /// When enabled, extending <c>IServiceCollection</c> creates a documentation page showing
        /// only your extension methods, with a link to Microsoft's official documentation for the
        /// complete type definition.
        /// </remarks>
        public bool CreateExternalTypeReferences { get; set; } = true;

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

        /// <summary>
        /// Gets or sets whether conceptual documentation features are enabled.
        /// </summary>
        /// <value>
        /// When true (default), conceptual documentation files are generated for new types
        /// and existing conceptual content is loaded. When false, only API documentation
        /// from XML comments is processed.
        /// </value>
        public bool ConceptualDocsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether a MintlifyTemplate is defined.
        /// </summary>
        /// <value>
        /// When true, docs.json will be generated even without assemblies to document.
        /// The presence of a template is an explicit signal that the user wants documentation output.
        /// </value>
        public bool HasMintlifyTemplate { get; set; } = false;

        /// <summary>
        /// Gets or sets the collection of external documentation references to combine into this documentation collection.
        /// </summary>
        /// <value>
        /// A list of <see cref="DocumentationReference"/> objects representing external documentation to be combined.
        /// </value>
        /// <remarks>
        /// Similar to MSBuild's ProjectReference, this allows creating unified documentation portals by combining
        /// multiple separate .docsproj outputs. Each reference specifies where to copy files from and how to
        /// integrate navigation.
        /// </remarks>
        /// <example>
        /// <code>
        /// var context = new ProjectContext
        /// {
        ///     DocumentationReferences = new List&lt;DocumentationReference&gt;
        ///     {
        ///         new DocumentationReference
        ///         {
        ///             ProjectPath = @"C:\repos\auth-service\docs\AuthService.docsproj",
        ///             DocumentationRoot = @"C:\repos\auth-service\docs",
        ///             DestinationPath = "services/auth",
        ///             DocumentationType = "Mintlify",
        ///             IntegrationType = "Tabs"
        ///         }
        ///     }
        /// };
        /// </code>
        /// </example>
        public List<DocumentationReference> DocumentationReferences { get; set; } = [];

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
            ExcludedTypes =
            [
                "*.MicrosoftTestingPlatformEntryPoint",
                "*.SelfRegisteredExtensions"
            ];
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the full path to the conceptual content folder for a member.
        /// </summary>
        /// <param name="namespaceName">The namespace containing the member's type, or <see langword="null"/> or empty for the global namespace.</param>
        /// <param name="typeName">The simple name of the type containing the member.</param>
        /// <param name="memberName">The simple name of the member. Overloads share a folder.</param>
        /// <returns>The member's folder inside its type's conceptual folder.</returns>
        /// <example>
        /// <code>
        /// context.GetConceptualMemberPath("System.Text.Json", "JsonSerializer", "Serialize");
        /// // Returns "{conceptual}/System/Text/Json/JsonSerializer/Serialize"
        /// </code>
        /// </example>
        /// <remarks>
        /// See <see cref="GetConceptualNamespacePath(string?)"/> for how the conceptual layout is structured.
        /// </remarks>
        public string GetConceptualMemberPath(string? namespaceName, string typeName, string memberName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

            return Path.Combine(GetConceptualTypePath(namespaceName, typeName), GetSafeFileName(memberName));
        }

        /// <summary>
        /// Gets the full path to the conceptual content folder for a namespace.
        /// </summary>
        /// <param name="namespaceName">The namespace name, or <see langword="null"/> or empty for the global namespace.</param>
        /// <returns>
        /// A nested folder with one level per namespace segment under <see cref="GetFullConceptualPath"/>,
        /// or a <c>global</c> folder for the global namespace.
        /// </returns>
        /// <example>
        /// <code>
        /// context.GetConceptualNamespacePath("System.Text.Json"); // Returns "{conceptual}/System/Text/Json"
        /// context.GetConceptualNamespacePath(string.Empty);       // Returns "{conceptual}/global"
        /// </code>
        /// </example>
        /// <remarks>
        /// Conceptual content is a source tree that people edit, not rendered output, so it always uses this nested layout
        /// regardless of <see cref="FileNamingOptions.NamespaceMode"/>. Placeholder generation and
        /// <see cref="DocumentationManager"/> both resolve folders through these methods, so content is always read from
        /// where it was written.
        /// </remarks>
        public string GetConceptualNamespacePath(string? namespaceName)
        {
            if (IsGlobalNamespaceName(namespaceName))
            {
                return Path.Combine(GetFullConceptualPath(), GlobalNamespaceName);
            }

            return Path.Combine([GetFullConceptualPath(), .. namespaceName!.Split('.').Select(GetSafeFileName)]);
        }

        /// <summary>
        /// Gets the full path to the conceptual content folder for a type.
        /// </summary>
        /// <param name="namespaceName">The namespace containing the type, or <see langword="null"/> or empty for the global namespace.</param>
        /// <param name="typeName">The simple name of the type.</param>
        /// <returns>The type's folder inside its namespace's conceptual folder.</returns>
        /// <example>
        /// <code>
        /// context.GetConceptualTypePath("System.Text.Json", "JsonSerializer");
        /// // Returns "{conceptual}/System/Text/Json/JsonSerializer"
        /// </code>
        /// </example>
        /// <remarks>
        /// See <see cref="GetConceptualNamespacePath(string?)"/> for how the conceptual layout is structured.
        /// </remarks>
        public string GetConceptualTypePath(string? namespaceName, string typeName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(typeName);

            return Path.Combine(GetConceptualNamespacePath(namespaceName), GetSafeFileName(typeName));
        }

        /// <summary>
        /// Gets the full path to the conceptual documentation folder.
        /// </summary>
        /// <returns>
        /// The absolute path to the conceptual documentation folder if <see cref="ConceptualPath"/> is absolute,
        /// or the path combined with <see cref="DocumentationRootPath"/> if <see cref="ConceptualPath"/> is relative.
        /// </returns>
        /// <remarks>
        /// This method ensures that conceptual paths are always resolved to absolute paths,
        /// making it easier to work with conceptual documentation files regardless of whether
        /// the path is configured as relative or absolute.
        /// </remarks>
        /// <example>
        /// <code>
        /// // When ConceptualPath is absolute
        /// context.ConceptualPath = "C:\\Docs\\conceptual";
        /// var path = context.GetFullConceptualPath(); // Returns "C:\Docs\conceptual"
        ///
        /// // When ConceptualPath is relative
        /// context.DocumentationRootPath = "C:\\Docs";
        /// context.ConceptualPath = "conceptual";
        /// var path = context.GetFullConceptualPath(); // Returns "C:\Docs\conceptual"
        /// </code>
        /// </example>
        public string GetFullConceptualPath()
        {
            if (Path.IsPathRooted(ConceptualPath))
            {
                return ConceptualPath;
            }

            return Path.Combine(DocumentationRootPath, ConceptualPath);
        }

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
        /// Gets the path of a namespace's documentation page, relative to the API reference output folder and without an extension.
        /// </summary>
        /// <param name="namespaceName">The namespace name, or <see langword="null"/> or empty for the global namespace.</param>
        /// <returns>The page path for the configured <see cref="FileNamingOptions"/>.</returns>
        /// <example>
        /// <code>
        /// // NamespaceMode.File with separator '-':
        /// context.GetNamespacePagePath("System.Text.Json"); // Returns "System-Text-Json"
        ///
        /// // NamespaceMode.Folder:
        /// context.GetNamespacePagePath("System.Text.Json"); // Returns "System/Text/Json/index"
        /// </code>
        /// </example>
        /// <remarks>
        /// This is the single source of truth for namespace page locations. Renderers use it to decide where pages are written
        /// and <see cref="Transformers.XmlTransformerBase"/> uses it to build links to them, so the two always agree.
        /// </remarks>
        public string GetNamespacePagePath(string? namespaceName)
        {
            var safeNamespaceName = IsGlobalNamespaceName(namespaceName) ? GlobalNamespaceName : namespaceName!;

            return FileNamingOptions.NamespaceMode == NamespaceMode.Folder
                ? Path.Combine(GetNamespaceFolderPath(safeNamespaceName), "index")
                : safeNamespaceName.Replace('.', FileNamingOptions.NamespaceSeparator);
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
        /// Gets the path of a type's documentation page, relative to the API reference output folder and without an extension.
        /// </summary>
        /// <param name="namespaceName">The namespace containing the type, or <see langword="null"/> or empty for the global namespace.</param>
        /// <param name="typeName">The simple name of the type.</param>
        /// <returns>The page path for the configured <see cref="FileNamingOptions"/>.</returns>
        /// <example>
        /// <code>
        /// // NamespaceMode.File with separator '-':
        /// context.GetTypePagePath("System.Text.Json", "JsonSerializer"); // Returns "System-Text-Json.JsonSerializer"
        ///
        /// // NamespaceMode.Folder:
        /// context.GetTypePagePath("System.Text.Json", "JsonSerializer"); // Returns "System/Text/Json/JsonSerializer"
        /// </code>
        /// </example>
        /// <remarks>
        /// This is the single source of truth for type page locations. See <see cref="GetNamespacePagePath(string?)"/>.
        /// </remarks>
        public string GetTypePagePath(string? namespaceName, string typeName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(typeName);

            var safeNamespaceName = IsGlobalNamespaceName(namespaceName) ? GlobalNamespaceName : namespaceName!;
            var safeTypeName = GetSafeFileName(typeName);

            return FileNamingOptions.NamespaceMode == NamespaceMode.Folder
                ? Path.Combine(GetNamespaceFolderPath(safeNamespaceName), safeTypeName)
                : $"{safeNamespaceName.Replace('.', FileNamingOptions.NamespaceSeparator)}.{safeTypeName}";
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
        /// Replaces characters that are invalid or unsafe in file and folder names.
        /// </summary>
        /// <param name="name">The name to make safe.</param>
        /// <returns>The name with angle brackets, backticks, path separators, and other invalid characters replaced by underscores.</returns>
        internal static string GetSafeFileName(string name)
        {
            // Angle brackets come from <Module> and generic types; the rest are invalid on at least one file system.
            return name
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_')
                .Replace('/', '_')
                .Replace('\\', '_')
                .Replace(':', '_')
                .Replace('*', '_')
                .Replace('?', '_')
                .Replace('"', '_')
                .Replace('|', '_');
        }

        /// <summary>
        /// Determines whether a namespace name refers to the global namespace.
        /// </summary>
        /// <param name="namespaceName">The namespace name to check.</param>
        /// <returns><see langword="true"/> for <see langword="null"/>, empty, whitespace, or <c>global</c>; otherwise, <see langword="false"/>.</returns>
        internal static bool IsGlobalNamespaceName(string? namespaceName)
        {
            return string.IsNullOrWhiteSpace(namespaceName) || namespaceName == GlobalNamespaceName;
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