namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the font configuration for documentation thumbnails.
    /// </summary>
    /// <remarks>
    /// This configuration controls the fonts used in thumbnail rendering,
    /// allowing specification of a Google Fonts family for consistent
    /// typography in generated thumbnails.
    /// </remarks>
    public class ThumbnailFontsConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Google Fonts family name.
        /// </summary>
        /// <remarks>
        /// Specifies the name of a Google Fonts family to use for text rendered
        /// within thumbnails. The font will be loaded from Google Fonts and applied
        /// during thumbnail generation.
        /// </remarks>
        public string? Family { get; set; }

        /// <summary>
        /// Gets or sets the font file format.
        /// </summary>
        /// <remarks>
        /// Valid values are <c>woff</c>, <c>ttf</c>, or <c>otf</c>.
        /// </remarks>
        public string? Format { get; set; }

        /// <summary>
        /// Gets or sets the URL of a hosted font file used in thumbnail rendering.
        /// </summary>
        public string? Source { get; set; }

        #endregion

    }

}
