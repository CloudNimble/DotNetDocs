using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a navigation link in the Mintlify navbar.
    /// </summary>
    /// <remarks>
    /// Each navbar link consists of a label, optional icon, and destination URL.
    /// These links provide quick access to important pages or external resources.
    /// </remarks>
    public class NavbarLink
    {

        #region Properties

        /// <summary>
        /// Gets or sets the destination URL for the navigation link.
        /// </summary>
        /// <remarks>
        /// Can be an absolute URL (https://example.com) or a relative path (/docs/page).
        /// This determines where users navigate when clicking the link.
        /// </remarks>
        public string? Href { get; set; }

        /// <summary>
        /// Gets or sets the icon to display alongside the navigation link.
        /// </summary>
        /// <remarks>
        /// Can be a string icon name from the configured icon library, or an object
        /// with detailed icon configuration including style and library properties.
        /// The icon appears before the label text.
        /// </remarks>
        public object? Icon { get; set; }

        /// <summary>
        /// Gets or sets the display text for the navigation link.
        /// </summary>
        /// <remarks>
        /// This text appears in the navbar and should be concise and descriptive
        /// of the link's destination. This is a required field.
        /// </remarks>
        [NotNull]
        public string Label { get; set; } = string.Empty;

        #endregion

    }

}