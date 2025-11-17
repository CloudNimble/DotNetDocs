using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a dropdown configuration in Mintlify navigation.
    /// </summary>
    /// <remarks>
    /// Dropdowns create expandable navigation menus. The dropdown name is required.
    /// </remarks>
    public class DropdownConfig : NavigationSectionBase
    {

        #region Properties

        /// <summary>
        /// Gets or sets the anchors for the dropdown.
        /// </summary>
        /// <remarks>
        /// Anchors provide persistent navigation items within the dropdown.
        /// </remarks>
        public List<AnchorConfig>? Anchors { get; set; }

        /// <summary>
        /// Gets or sets the name of the dropdown.
        /// </summary>
        /// <remarks>
        /// This is a required field that appears as the dropdown label in navigation.
        /// </remarks>
        [NotNull]
        public string Dropdown { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tabs for the dropdown.
        /// </summary>
        /// <remarks>
        /// Allows creating multiple tabs within a dropdown section.
        /// </remarks>
        public List<TabConfig>? Tabs { get; set; }

        #endregion

    }

}
