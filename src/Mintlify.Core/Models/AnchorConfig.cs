using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents an anchor configuration in Mintlify navigation.
    /// </summary>
    /// <remarks>
    /// Anchors provide navigation links in your documentation. The anchor name is required.
    /// </remarks>
    public class AnchorConfig : NavigationSectionBase
    {

        #region Properties

        /// <summary>
        /// Gets or sets the name of the anchor.
        /// </summary>
        /// <remarks>
        /// This is a required field that appears as the anchor label.
        /// </remarks>
        [NotNull]
        public string Anchor { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tabs for the anchor.
        /// </summary>
        /// <remarks>
        /// Allows creating multiple tabs within an anchor section.
        /// </remarks>
        public List<TabConfig>? Tabs { get; set; }

        #endregion

    }

}
