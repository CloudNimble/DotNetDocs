namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the label shown above the page title in generated thumbnails.
    /// </summary>
    /// <remarks>
    /// Site-wide eyebrow <c>source</c>, <c>style</c>, and <c>color</c> are set here.
    /// Per-page or per-section thumbnail overrides use literal eyebrow text instead.
    /// See https://www.mintlify.com/docs/organize/settings-appearance.
    /// </remarks>
    public class ThumbnailEyebrowConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the hex color of the eyebrow on preset thumbnails.
        /// </summary>
        /// <remarks>
        /// Defaults to a color derived from the theme.
        /// </remarks>
        public string? Color { get; set; }

        /// <summary>
        /// Gets or sets the navigation level whose name appears as the eyebrow.
        /// </summary>
        /// <remarks>
        /// Valid values are <c>group</c>, <c>tab</c>, <c>dropdown</c>, <c>anchor</c>,
        /// <c>language</c>, <c>product</c>, <c>version</c>, <c>item</c>, or <c>none</c>.
        /// If a page is not inside a navigation element of the chosen type, the eyebrow
        /// shows the group name instead. <c>none</c> hides the eyebrow. Defaults to <c>group</c>.
        /// </remarks>
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets how the eyebrow is drawn on preset thumbnails.
        /// </summary>
        /// <remarks>
        /// Valid values are <c>plain</c>, <c>bar</c>, or <c>pill</c>. Defaults to <c>bar</c>
        /// for the classic preset, <c>pill</c> for headline, and <c>plain</c> for minimal.
        /// </remarks>
        public string? Style { get; set; }

        #endregion

    }

}
