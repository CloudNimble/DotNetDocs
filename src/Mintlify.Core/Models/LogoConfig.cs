namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the logo configuration for Mintlify.
    /// Can be a single image path for both light and dark mode, or separate paths for each mode.
    /// </summary>
    public class LogoConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the path to the dark logo file, including the file extension.
        /// </summary>
        public string? Dark { get; set; }

        /// <summary>
        /// Gets or sets the URL to redirect to when clicking the logo.
        /// If not provided, the logo will link to the homepage.
        /// </summary>
        public string? Href { get; set; }

        /// <summary>
        /// Gets or sets the path to the light logo file, including the file extension.
        /// </summary>
        public string? Light { get; set; }

        #endregion

    }

}