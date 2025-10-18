using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the footer configuration for Mintlify.
    /// </summary>
    public class FooterConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the footer links.
        /// </summary>
        public List<FooterLinkGroup>? Links { get; set; }

        /// <summary>
        /// Gets or sets the social media links.
        /// </summary>
        public Dictionary<string, string>? Socials { get; set; }

        #endregion

    }

    /// <summary>
    /// Represents a group of footer links.
    /// </summary>
    public class FooterLinkGroup
    {

        #region Properties

        /// <summary>
        /// Gets or sets the header title of the column.
        /// </summary>
        /// <remarks>
        /// This is a required field that appears as the column title in the footer.
        /// </remarks>
        [NotNull]
        public string Header { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the items in the footer group.
        /// </summary>
        public List<FooterLink>? Items { get; set; }

        #endregion

    }

    /// <summary>
    /// Represents a footer link.
    /// </summary>
    public class FooterLink
    {

        #region Properties

        /// <summary>
        /// Gets or sets the URL of the link.
        /// </summary>
        public string? Href { get; set; }

        /// <summary>
        /// Gets or sets the label of the link.
        /// </summary>
        /// <remarks>
        /// This is a required field that appears as the link text in the footer.
        /// </remarks>
        [NotNull]
        public string Label { get; set; } = string.Empty;

        #endregion

    }

}