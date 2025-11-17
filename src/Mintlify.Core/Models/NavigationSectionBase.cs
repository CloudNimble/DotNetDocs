using System.Collections.Generic;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Base class for top-level navigation sections (tabs, anchors, dropdowns).
    /// </summary>
    /// <remarks>
    /// Extends NavigationContainerBase to add support for colors, descriptions,
    /// global navigation, versioning, and language support. Used by tabs, anchors,
    /// and dropdowns which are the primary navigation sections.
    /// </remarks>
    public abstract class NavigationSectionBase : NavigationContainerBase
    {

        #region Properties

        /// <summary>
        /// Gets or sets the color configuration for this navigation section.
        /// </summary>
        /// <remarks>
        /// Defines the primary and secondary colors used for this section's visual styling.
        /// </remarks>
        public ColorPairConfig? Color { get; set; }

        /// <summary>
        /// Gets or sets the description of this navigation section.
        /// </summary>
        /// <remarks>
        /// Provides additional descriptive text about this navigation section.
        /// </remarks>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the dropdowns for this navigation section.
        /// </summary>
        /// <remarks>
        /// Dropdowns create expandable menu sections within this navigation section.
        /// </remarks>
        public List<DropdownConfig>? Dropdowns { get; set; }

        /// <summary>
        /// Gets or sets global navigation items that appear on all sections and pages.
        /// </summary>
        /// <remarks>
        /// Global navigation items persist across different tabs and pages for consistent navigation.
        /// </remarks>
        public GlobalNavigationConfig? Global { get; set; }

        /// <summary>
        /// Gets or sets the languages for this navigation section.
        /// </summary>
        /// <remarks>
        /// Allows partitioning navigation into different language-specific versions.
        /// </remarks>
        public List<LanguageConfig>? Languages { get; set; }

        /// <summary>
        /// Gets or sets the versions for this navigation section.
        /// </summary>
        /// <remarks>
        /// Allows partitioning navigation into different version-specific documentation.
        /// </remarks>
        public List<VersionConfig>? Versions { get; set; }

        #endregion

    }

}
