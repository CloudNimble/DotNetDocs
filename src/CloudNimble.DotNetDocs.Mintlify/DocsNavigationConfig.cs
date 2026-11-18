using System.Collections.Generic;
using Mintlify.Core.Models;

namespace CloudNimble.DotNetDocs.Mintlify
{

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

        /// <summary>
        /// Gets or sets template tabs that should be appended after DocumentationReference tabs.
        /// </summary>
        /// <value>
        /// Tabs parsed from <c>&lt;Tab Position="End"&gt;</c> in the MintlifyTemplate.
        /// Empty when no trailing tabs are declared. Never serialized to docs.json.
        /// </value>
        /// <remarks>
        /// The renderer appends these tabs after <see cref="MintlifyRenderer.ApplyNavigationType"/>
        /// and <see cref="MintlifyRenderer.CombineReferencedNavigation"/> have added the root and
        /// collection tabs, matching a sibling-product href tab sitting at the end of the tab bar.
        /// </remarks>
        public List<TabConfig> TrailingTabs { get; set; } = [];

        #endregion

    }

}
