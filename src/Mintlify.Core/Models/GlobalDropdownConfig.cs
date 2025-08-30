using System.Diagnostics.CodeAnalysis;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a dropdown configuration for global navigation in the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration defines a dropdown menu that appears globally across all sections
    /// and pages, providing users with organized access to multiple related links or sections.
    /// </remarks>
    public class GlobalDropdownConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the display name for the dropdown button.
        /// </summary>
        /// <remarks>
        /// Specifies the text that will be shown on the dropdown button. Should be concise
        /// and descriptive of the dropdown's contents, such as "Resources", "Tools",
        /// "Community", etc. This is a required field.
        /// </remarks>
        [NotNull]
        public string Dropdown { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the icon to display alongside the dropdown name.
        /// </summary>
        /// <remarks>
        /// Can be a string icon name from the configured icon library, or an object
        /// with detailed icon configuration including style and library properties.
        /// The icon appears before the dropdown text to provide visual context.
        /// </remarks>
        public object? Icon { get; set; }

        /// <summary>
        /// Gets or sets the color configuration for the dropdown.
        /// </summary>
        /// <remarks>
        /// Defines custom colors for the dropdown in light and dark modes.
        /// This allows the dropdown to have distinctive styling that matches
        /// your brand or indicates different types of content.
        /// </remarks>
        public ColorPairConfig? Color { get; set; }

        /// <summary>
        /// Gets or sets the description text for the dropdown.
        /// </summary>
        /// <remarks>
        /// Optional descriptive text that can appear in the dropdown or as a tooltip.
        /// Provides additional context about what users will find in the dropdown menu.
        /// </remarks>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets whether this dropdown is hidden by default.
        /// </summary>
        /// <remarks>
        /// When true, this dropdown will not be visible in the global navigation unless
        /// specifically shown. This can be useful for dropdowns that are in development
        /// or not ready for public access.
        /// </remarks>
        public bool? Hidden { get; set; }

        /// <summary>
        /// Gets or sets the primary URL or path for this dropdown.
        /// </summary>
        /// <remarks>
        /// Specifies where users should be directed when they click directly on the dropdown
        /// button (rather than selecting a specific item). Can be a relative path or absolute URL.
        /// This is optional if the dropdown only contains sub-items.
        /// </remarks>
        public string? Href { get; set; }

        #endregion

    }

}