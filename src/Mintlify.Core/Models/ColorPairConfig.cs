namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a color pair configuration for light and dark modes.
    /// </summary>
    public class ColorPairConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the color in hex format to use in dark mode.
        /// </summary>
        public string? Dark { get; set; }

        /// <summary>
        /// Gets or sets the color in hex format to use in light mode.
        /// </summary>
        public string? Light { get; set; }

        #endregion

        #region Implicit Operators

        /// <summary>
        /// Implicitly converts a string color to a ColorPairConfig.
        /// </summary>
        /// <param name="color">The hex color string.</param>
        /// <returns>A ColorPairConfig with the specified color for both light and dark modes.</returns>
        public static implicit operator ColorPairConfig?(string? color)
        {
            return color is null ? null : new ColorPairConfig { Light = color, Dark = color };
        }

        /// <summary>
        /// Implicitly converts a ColorPairConfig to a string.
        /// </summary>
        /// <param name="colorPairConfig">The color pair configuration.</param>
        /// <returns>The light color, dark color, or null.</returns>
        public static implicit operator string?(ColorPairConfig? colorPairConfig)
        {
            return colorPairConfig?.Light ?? colorPairConfig?.Dark;
        }

        /// <summary>
        /// Returns the string representation of the color pair configuration.
        /// </summary>
        /// <returns>The light color, dark color, or empty string.</returns>
        public override string ToString()
        {
            return Light ?? Dark ?? string.Empty;
        }

        #endregion

    }

}