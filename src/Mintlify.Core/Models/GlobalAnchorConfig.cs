using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents an anchor configuration for global navigation in the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration defines an anchor link that appears globally across all sections
    /// and pages, providing users with quick access to important external resources or key pages.
    /// </remarks>
    public class GlobalAnchorConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the display name for the anchor link.
        /// </summary>
        /// <remarks>
        /// Specifies the text that will be shown for the anchor link. Should be concise
        /// and descriptive of the link's destination, such as "GitHub", "API Status",
        /// "Support", etc. This is a required field.
        /// </remarks>
        [NotNull]
        public string Anchor { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the color configuration for the anchor.
        /// </summary>
        /// <remarks>
        /// Defines custom colors for the anchor link in light and dark modes.
        /// This allows the anchor to have distinctive styling that matches
        /// your brand or indicates different types of external resources.
        /// </remarks>
        public ColorPairConfig? Color { get; set; }

        /// <summary>
        /// Gets or sets whether this anchor is hidden by default.
        /// </summary>
        /// <remarks>
        /// When true, this anchor will not be visible in the global navigation unless
        /// specifically shown. This can be useful for anchors that are temporary
        /// or not ready for public access.
        /// </remarks>
        public bool? Hidden { get; set; }

        /// <summary>
        /// Gets or sets the URL or path for this anchor link.
        /// </summary>
        /// <remarks>
        /// Specifies where users should be directed when they click on this anchor.
        /// Can be a relative path within the documentation or an absolute URL for
        /// external resources such as GitHub repositories, status pages, or support portals.
        /// </remarks>
        public string? Href { get; set; }

        /// <summary>
        /// Gets or sets the icon to display alongside the anchor name.
        /// </summary>
        /// <remarks>
        /// Can be a string icon name from the configured icon library, or an object
        /// with detailed icon configuration including style and library properties.
        /// The icon appears before the anchor text to provide visual context.
        /// </remarks>
        public object? Icon { get; set; }

        #endregion

    }

}