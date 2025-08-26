using System;
using System.Collections.Generic;

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
    /// <![CDATA[
    /// var context = new ProjectContext(["ref1.dll", "ref2.dll"]) { ConceptualPath = "conceptual" };
    /// var model = await manager.DocumentAsync("MyLib.dll", "MyLib.xml", context);
    /// ]]>
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
        public string? ConceptualPath { get; set; }

        /// <summary>
        /// Gets or sets the collection of paths to referenced assemblies.
        /// </summary>
        /// <value>
        /// A collection of file system paths to assemblies referenced by the project being documented.
        /// </value>
        public List<string> References { get; set; } = [];

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="ProjectContext"/> with optional referenced assemblies.
        /// </summary>
        /// <param name="references">Paths to referenced assemblies.</param>
        public ProjectContext(params string[] references)
        {
            ArgumentNullException.ThrowIfNull(references);
            References.AddRange(references);
        }

        #endregion

    }

}