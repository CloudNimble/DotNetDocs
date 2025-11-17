using System.Collections.Generic;
using Mintlify.Core.Models;

namespace CloudNimble.DotNetDocs.Mintlify
{

    /// <summary>
    /// Configuration options for the Mintlify documentation renderer.
    /// </summary>
    public class MintlifyRendererOptions
    {

        #region Properties

        /// <summary>
        /// Gets or sets whether to generate the docs.json navigation file.
        /// </summary>
        /// <value>
        /// True to generate docs.json alongside MDX files; otherwise, false.
        /// Default is true.
        /// </value>
        public bool GenerateDocsJson { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to generate index files for namespaces.
        /// </summary>
        /// <value>
        /// True to generate index.mdx files for namespace documentation; otherwise, false.
        /// Default is true.
        /// </value>
        public bool GenerateNamespaceIndex { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include icons in navigation and frontmatter.
        /// </summary>
        /// <value>
        /// True to include FontAwesome icons; otherwise, false.
        /// Default is true.
        /// </value>
        public bool IncludeIcons { get; set; } = true;

        /// <summary>
        /// Gets or sets the custom order for namespaces in navigation.
        /// </summary>
        /// <value>
        /// A list of namespace patterns in the desired order, or null for alphabetical ordering.
        /// Supports wildcards (e.g., "System.*" matches all System namespaces).
        /// </value>
        public List<string>? NamespaceOrder { get; set; }

        /// <summary>
        /// Gets or sets a custom template for the docs.json configuration.
        /// </summary>
        /// <value>
        /// A DocsJsonConfig instance to use as a template, or null to use defaults.
        /// The navigation section will be generated automatically.
        /// </value>
        public DocsJsonConfig? Template { get; set; }

        /// <summary>
        /// Gets or sets the navigation configuration for DotNetDocs-specific properties.
        /// </summary>
        /// <value>
        /// A DocsNavigationConfig instance that controls navigation mode, type, and name.
        /// Default is a new instance with Mode=Unified, Type=Pages, and Name=null.
        /// </value>
        public DocsNavigationConfig Navigation { get; set; } = new DocsNavigationConfig();

        /// <summary>
        /// Gets or sets the group name used when NavigationMode is Unified.
        /// </summary>
        /// <value>
        /// The name of the unified API reference group. Default is "API Reference".
        /// </value>
        public string UnifiedGroupName { get; set; } = "API Reference";

        #endregion

    }

}