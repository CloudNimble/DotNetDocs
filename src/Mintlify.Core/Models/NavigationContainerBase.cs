using System.Collections.Generic;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Base class for navigation items that can contain groups and have external links.
    /// </summary>
    /// <remarks>
    /// Extends NavigationItemBase to add support for nested groups and external URLs.
    /// Used by products and other container-level navigation elements.
    /// </remarks>
    public abstract class NavigationContainerBase : NavigationItemBase
    {

        #region Properties

        /// <summary>
        /// Gets or sets the groups for this navigation container.
        /// </summary>
        /// <remarks>
        /// Groups organize pages into labeled sections within this navigation container.
        /// </remarks>
        public List<GroupConfig>? Groups { get; set; }

        /// <summary>
        /// Gets or sets the URL or path for this navigation container.
        /// </summary>
        /// <remarks>
        /// Can be used to link to an external URL or specify a path for this container.
        /// </remarks>
        public string? Href { get; set; }

        #endregion

    }

}
