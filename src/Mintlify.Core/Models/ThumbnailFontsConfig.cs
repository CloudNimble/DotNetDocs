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

        #endregion

    }

}
