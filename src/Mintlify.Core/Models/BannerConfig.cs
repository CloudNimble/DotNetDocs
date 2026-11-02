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
        /// Gets or sets a custom banner background color.
        /// </summary>
        /// <remarks>
        /// When set, this takes precedence over the color implied by <see cref="Type"/>.
        /// Banner text is white, so choose a background dark enough to remain legible.
        /// If only one of light or dark is provided, it is used in both modes.
        /// See https://www.mintlify.com/docs/components/banner.
        /// </remarks>
        public ColorPairConfig? Color { get; set; }

        /// <summary>
        /// Gets or sets whether to show a dismiss button on the banner.
        /// </summary>
        /// <remarks>
        /// When true, displays a dismiss button (X) on the right side of the banner,
        /// allowing users to close the banner. When false or not specified, the banner
        /// cannot be dismissed by users and will always be visible.
        /// </remarks>
        public bool? Dismissible { get; set; }

        /// <summary>
        /// Gets or sets the visual style of the banner.
        /// </summary>
        /// <remarks>
        /// Controls the background color so visitors can recognize urgency. Defaults to
        /// <c>info</c>. Valid values:
        /// <list type="bullet">
        /// <item><description><c>info</c>: primary brand color, for announcements and updates.</description></item>
        /// <item><description><c>warning</c>: amber background, for maintenance or deprecations.</description></item>
        /// <item><description><c>critical</c>: red background, for outages or required actions.</description></item>
        /// </list>
        /// See https://www.mintlify.com/docs/components/banner.
        /// </remarks>
        public string? Type { get; set; }

        #endregion

    }

}