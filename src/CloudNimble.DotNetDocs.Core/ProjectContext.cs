using System;
using System.Collections.Generic;
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
        /// Gets or sets the path to the conceptual documentation folder.
        /// </summary>
        /// <value>
        /// The file system path to the folder containing conceptual documentation files.
        /// </value>
        public string? ConceptualPath { get; init; }

        /// <summary>
        /// Gets or sets custom settings for transformation and rendering.
        /// </summary>
        /// <value>
        /// An object containing custom settings for specific transformer or renderer implementations.
        /// </value>
        public object CustomSettings { get; set; } = new();

        /// <summary>
        /// Gets or sets the output path for generated documentation.
        /// </summary>
        /// <value>
        /// The file system path where documentation output will be generated.
        /// </value>
        public string OutputPath { get; init; } = "docs";

        /// <summary>
        /// Gets or sets the collection of paths to referenced assemblies.
        /// </summary>
        /// <value>
        /// A collection of file system paths to assemblies referenced by the project being documented.
        /// </value>
        public List<string> References { get; init; } = [];

        /// <summary>
        /// Gets or sets whether to show placeholder content in the documentation.
        /// </summary>
        /// <value>
        /// When true (default), placeholder content is included. When false, files containing the
        /// TODO marker comment are skipped during loading.
        /// </value>
        public bool ShowPlaceholders { get; init; } = true;

        /// <summary>
        /// Gets or sets the list of member accessibilities to include in documentation.
        /// </summary>
        /// <value>
        /// List of accessibility levels to include. Defaults to Public only.
        /// </value>
        public List<Accessibility> IncludedMembers { get; init; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="ProjectContext"/> with optional included members and referenced assemblies.
        /// </summary>
        /// <param name="includedMembers">List of member accessibilities to include. Defaults to Public if null.</param>
        /// <param name="references">Paths to referenced assemblies.</param>
        public ProjectContext(List<Accessibility>? includedMembers = null, params string[] references)
        {
            IncludedMembers = includedMembers ?? [Accessibility.Public];
            ArgumentNullException.ThrowIfNull(references);
            References.AddRange(references);
        }

        #endregion

    }

}