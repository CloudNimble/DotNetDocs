using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a tab configuration in Mintlify navigation.
    /// </summary>
    /// <remarks>
    /// Tabs provide top-level navigation sections in your documentation.
    /// The tab name is required.
    /// </remarks>
    public class TabConfig : NavigationSectionBase
    {

        #region Properties

        /// <summary>
        /// Gets or sets the anchors for the tab.
        /// </summary>
        /// <remarks>
        /// Anchors provide persistent navigation items at the top of the sidebar.
        /// </remarks>
        public List<AnchorConfig>? Anchors { get; set; }

        /// <summary>
        /// Gets or sets the menu items for the tab.
        /// </summary>
        /// <remarks>
        /// Menu items create a dropdown menu within the tab navigation.
        /// </remarks>
        public List<MenuConfig>? Menu { get; set; }

        /// <summary>
        /// Gets or sets the name of the tab.
        /// </summary>
        /// <remarks>
        /// This is a required field that appears as the tab label in navigation.
        /// </remarks>
        [NotNull]
        public string Tab { get; set; } = string.Empty;

        #endregion

    }

}
