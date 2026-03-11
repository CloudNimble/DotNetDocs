namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a base configuration with Light and Dark theme variants.
    /// </summary>
    /// <remarks>
    /// This base class provides the shared Light/Dark string properties used across multiple
    /// Mintlify configuration types such as colors, logos, favicons, and background images.
    /// Subclasses may add additional properties and their own implicit operators.
    /// </remarks>
    public class ThemePairConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the value for dark mode.
        /// </summary>
        public virtual string? Dark { get; set; }

        /// <summary>
        /// Gets or sets the value for light mode.
        /// </summary>
        public virtual string? Light { get; set; }

        #endregion

        #region Implicit Operators

        /// <summary>
        /// Implicitly converts a string to a ThemePairConfig with the value set for both modes.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <returns>A ThemePairConfig with the specified value for both light and dark modes, or null.</returns>
        public static implicit operator ThemePairConfig?(string? value)
        {
            return value is null ? null : new ThemePairConfig { Light = value, Dark = value };
        }

        /// <summary>
        /// Implicitly converts a ThemePairConfig to a string.
        /// </summary>
        /// <param name="config">The theme pair configuration.</param>
        /// <returns>The light value, dark value, or null.</returns>
        public static implicit operator string?(ThemePairConfig? config)
        {
            return config?.Light ?? config?.Dark;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the string representation of the theme pair configuration.
        /// </summary>
        /// <returns>The light value, dark value, or empty string.</returns>
        public override string ToString()
        {
            return Light ?? Dark ?? string.Empty;
        }

        #endregion

    }

}
