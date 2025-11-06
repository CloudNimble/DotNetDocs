namespace CloudNimble.DotNetDocs.Mintlify
{

    /// <summary>
    /// Defines the navigation type for documentation integration.
    /// </summary>
    public enum NavigationType
    {
        /// <summary>
        /// Documentation appears in the main navigation as pages.
        /// </summary>
        Pages,

        /// <summary>
        /// Documentation appears as a top-level tab.
        /// </summary>
        Tabs,

        /// <summary>
        /// Documentation appears as a product in the products section.
        /// </summary>
        Products
    }

    /// <summary>
    /// Configuration for DotNetDocs-specific navigation properties.
    /// </summary>
    /// <remarks>
    /// This class holds metadata that bridges .docsproj XML templates to Mintlify.Core,
    /// but is not part of the Mintlify docs.json specification itself.
    /// These properties control how DotNetDocs organizes and integrates documentation
    /// into the Mintlify navigation structure.
    /// </remarks>
    public class DocsNavigationConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the navigation mode for multi-assembly documentation.
        /// </summary>
        /// <value>
        /// The navigation organization mode. Default is NavigationMode.Unified.
        /// </value>
        public NavigationMode Mode { get; set; } = NavigationMode.Unified;

        /// <summary>
        /// Gets or sets the navigation type for this documentation project.
        /// </summary>
        /// <value>
        /// The navigation type. Default is NavigationType.Pages.
        /// </value>
        public NavigationType Type { get; set; } = NavigationType.Pages;

        /// <summary>
        /// Gets or sets the navigation name/title for this documentation project when Type is Tabs or Products.
        /// </summary>
        /// <value>
        /// The display name for the tab or product. If not specified, the project name will be used.
        /// </value>
        public string? Name { get; set; }

        #endregion

    }

}
