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
        /// Gets or sets the navigation mode for multi-assembly documentation.
        /// </summary>
        /// <value>
        /// The navigation organization mode. Default is NavigationMode.Unified.
        /// </value>
        public NavigationMode NavigationMode { get; set; } = NavigationMode.Unified;

        /// <summary>
        /// Gets or sets the group name used when NavigationMode is Unified.
        /// </summary>
        /// <value>
        /// The name of the unified API reference group. Default is "API Reference".
        /// </value>
        public string UnifiedGroupName { get; set; } = "API Reference";

        /// <summary>
        /// Gets or sets the integration type for the root project's navigation.
        /// </summary>
        /// <value>
        /// The integration type for the root project. Valid values are "Pages" (default), "Tabs", or "Products".
        /// When set to "Pages", the root project appears in the main navigation.
        /// When set to "Tabs", the root project appears as a top-level tab.
        /// When set to "Products", the root project appears as a product in the products section.
        /// </value>
        /// <remarks>
        /// This property controls how the root project's documentation is integrated into the navigation structure.
        /// Referenced projects use the IntegrationType specified in their DocumentationReference metadata.
        /// </remarks>
        public string RootIntegrationType { get; set; } = "Pages";

        /// <summary>
        /// Gets or sets the name/title for the root project when RootIntegrationType is "Tabs" or "Products".
        /// </summary>
        /// <value>
        /// The display name for the root project tab or product. If not specified, the project name will be used.
        /// </value>
        public string? RootIntegrationName { get; set; }

        #endregion

    }

}