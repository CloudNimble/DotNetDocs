namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a specific font configuration for the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration defines a font used for headings or body text in the
    /// documentation, including the font family, weight, and optional source URL
    /// for custom hosted fonts.
    /// </remarks>
    public class FontConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the font family name.
        /// </summary>
        /// <remarks>
        /// Specifies the name of the font family to use. This can be a standard
        /// web font or a custom font loaded from the specified source URL.
        /// </remarks>
        public string? Family { get; set; }

        /// <summary>
        /// Gets or sets the font weight.
        /// </summary>
        /// <remarks>
        /// Specifies the weight of the font as a numeric value, such as 400 for
        /// normal weight or 700 for bold. The available weights depend on the
        /// specific font family being used.
        /// </remarks>
        public int? Weight { get; set; }

        /// <summary>
        /// Gets or sets the URL to the hosted font file.
        /// </summary>
        /// <remarks>
        /// Specifies the URL where the font file is hosted. This is used to load
        /// custom fonts that are not available through standard font services.
        /// The URL should point directly to the font file.
        /// </remarks>
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the font file format.
        /// </summary>
        /// <remarks>
        /// Specifies the format of the font file, such as "woff" or "woff2".
        /// This helps the browser correctly interpret the font file loaded from
        /// the source URL.
        /// </remarks>
        public string? Format { get; set; }

        #endregion

    }

}
