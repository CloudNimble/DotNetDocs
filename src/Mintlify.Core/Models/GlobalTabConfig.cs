using System.Diagnostics.CodeAnalysis;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a tab configuration for global navigation in the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration defines a tab that appears globally across all sections and pages,
    /// providing users with quick access to different areas or types of documentation.
    /// </remarks>
    public class GlobalTabConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the display name for the tab.
        /// </summary>
        /// <remarks>
        /// Specifies the text that will be shown on the tab button. Should be concise
        /// and descriptive of the tab's content or purpose, such as "API Reference",
        /// "Guides", "Examples", etc. This is a required field.
        /// </remarks>
        [NotNull]
        public string Tab { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the icon to display alongside the tab name.
        /// </summary>
        /// <remarks>
        /// Can be a string icon name from the configured icon library, or an object
        /// with detailed icon configuration including style and library properties.
        /// The icon appears before or alongside the tab text to provide visual context.
        /// </remarks>
        public object? Icon { get; set; }

        /// <summary>
        /// Gets or sets whether this tab is hidden by default.
        /// </summary>
        /// <remarks>
        /// When true, this tab will not be visible in the global navigation unless
        /// specifically shown. This can be useful for tabs that are in development
        /// or not ready for public access.
        /// </remarks>
        public bool? Hidden { get; set; }

        /// <summary>
        /// Gets or sets the URL or path for this tab.
        /// </summary>
        /// <remarks>
        /// Specifies where users should be directed when they click on this tab.
        /// Can be a relative path (e.g., "/api/") or an absolute URL for external content.
        /// This determines the landing page for the tab.
        /// </remarks>
        public string? Href { get; set; }

        #endregion

    }

}