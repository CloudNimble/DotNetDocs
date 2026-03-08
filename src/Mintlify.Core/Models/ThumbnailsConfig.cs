namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the thumbnail configuration for the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration controls the appearance and styling of thumbnails used
    /// throughout the documentation, including visual theme, background images,
    /// and font settings.
    /// </remarks>
    public class ThumbnailsConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the visual theme for thumbnails.
        /// </summary>
        /// <remarks>
        /// Controls the overall appearance of generated thumbnails. Valid values are
        /// "light" or "dark", which determine the color scheme used for thumbnail
        /// rendering.
        /// </remarks>
        public string? Appearance { get; set; }

        /// <summary>
        /// Gets or sets the background image path for thumbnails.
        /// </summary>
        /// <remarks>
        /// Specifies the path to a background image used when rendering thumbnails.
        /// This allows for custom branding and visual consistency across all
        /// documentation thumbnails.
        /// </remarks>
        public string? Background { get; set; }

        /// <summary>
        /// Gets or sets the font configuration for thumbnails.
        /// </summary>
        /// <remarks>
        /// Controls the fonts used in thumbnail rendering, allowing customization
        /// of the font family used for text displayed within thumbnails.
        /// </remarks>
        public ThumbnailFontsConfig? Fonts { get; set; }

        #endregion

    }

}
