namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the font configuration for the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration allows customization of typography by specifying custom fonts
    /// including font family, weight, source URLs, and format specifications.
    /// </remarks>
    public class FontsConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the font family name.
        /// </summary>
        /// <remarks>
        /// Specifies the name of the font family to use, such as "Open Sans" or "Playfair Display".
        /// This should match the font family name defined in the font file.
        /// </remarks>
        public string? Family { get; set; }

        /// <summary>
        /// Gets or sets the font file format.
        /// </summary>
        /// <remarks>
        /// Specifies the format of the font file. Valid values are "woff" or "woff2".
        /// WOFF2 is preferred for modern browsers as it provides better compression.
        /// </remarks>
        public string? Format { get; set; }

        /// <summary>
        /// Gets or sets the font source URL.
        /// </summary>
        /// <remarks>
        /// Specifies the URL where the font file can be downloaded from.
        /// Should be a complete URL pointing to the font file, such as
        /// "https://mintlify-assets.b-cdn.net/fonts/Hubot-Sans.woff2".
        /// </remarks>
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the font weight.
        /// </summary>
        /// <remarks>
        /// Specifies the font weight as a numeric value such as 400 (normal) or 700 (bold).
        /// Precise font weights like 550 are supported for variable fonts.
        /// Common values include 300 (light), 400 (normal), 500 (medium), 600 (semi-bold), 700 (bold).
        /// </remarks>
        public int? Weight { get; set; }

        #endregion

    }

}