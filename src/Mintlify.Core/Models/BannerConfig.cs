namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the banner configuration for displaying announcements or notifications in the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration allows you to display a banner at the top of your documentation
    /// for important announcements, updates, or notifications. The banner supports MDX formatting.
    /// </remarks>
    public class BannerConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the content to display in the banner.
        /// </summary>
        /// <remarks>
        /// The text or MDX content that will be displayed in the banner. MDX formatting
        /// is supported, allowing for rich content including links, emphasis, and other
        /// formatting. This content should be concise but informative.
        /// </remarks>
        public string? Content { get; set; }

        /// <summary>
        /// Gets or sets whether to show a dismiss button on the banner.
        /// </summary>
        /// <remarks>
        /// When true, displays a dismiss button (X) on the right side of the banner,
        /// allowing users to close the banner. When false or not specified, the banner
        /// cannot be dismissed by users and will always be visible.
        /// </remarks>
        public bool? Dismissible { get; set; }

        #endregion

    }

}