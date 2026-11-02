namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents per-node thumbnail overrides on a navigation item.
    /// </summary>
    /// <remarks>
    /// Each key overrides the nearest parent and site-level <c>thumbnails</c>.
    /// <see cref="Eyebrow"/> is literal text; an empty string hides it.
    /// </remarks>
    public class NavThumbnailConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets whether to render the thumbnail in light or dark mode.
        /// </summary>
        public string? Appearance { get; set; }

        /// <summary>
        /// Gets or sets the background image path or URL.
        /// </summary>
        public string? Background { get; set; }

        /// <summary>
        /// Gets or sets literal text shown above the title. An empty string hides it.
        /// </summary>
        /// <remarks>
        /// Site-level <c>thumbnails.eyebrow</c> uses source/style/color. Per-node
        /// overrides use this literal string instead.
        /// </remarks>
        public string? Eyebrow { get; set; }

        /// <summary>
        /// Gets or sets the layout preset: <c>classic</c>, <c>headline</c>, or <c>minimal</c>.
        /// </summary>
        public string? Preset { get; set; }

        /// <summary>
        /// Gets or sets the surface: <c>neutral</c> or <c>brand</c>.
        /// </summary>
        public string? Surface { get; set; }

        #endregion

    }

}
