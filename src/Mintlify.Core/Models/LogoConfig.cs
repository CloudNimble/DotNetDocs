namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the logo configuration for Mintlify.
    /// Can be a single image path for both light and dark mode, or separate paths for each mode.
    /// </summary>
    public class LogoConfig : ThemePairConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the URL to redirect to when clicking the logo.
        /// If not provided, the logo will link to the homepage.
        /// </summary>
        public string? Href { get; set; }

        #endregion

        #region Implicit Operators

        /// <summary>
        /// Implicitly converts a string path to a LogoConfig.
        /// </summary>
        /// <param name="path">The logo file path.</param>
        /// <returns>A LogoConfig with the specified path for both light and dark modes.</returns>
        public static implicit operator LogoConfig?(string? path)
        {
            return path is null ? null : new LogoConfig { Light = path, Dark = path };
        }

        /// <summary>
        /// Implicitly converts a LogoConfig to a string.
        /// </summary>
        /// <param name="logoConfig">The logo configuration.</param>
        /// <returns>The light logo path, dark logo path, or null.</returns>
        public static implicit operator string?(LogoConfig? logoConfig)
        {
            return logoConfig?.Light ?? logoConfig?.Dark;
        }

        #endregion

    }

}
