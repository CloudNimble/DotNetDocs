namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a color configuration for Mintlify.
    /// Can be a simple hex color string or a complex color pair configuration with light and dark modes.
    /// </summary>
    public class ColorConfig
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

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorConfig"/> class.
        /// </summary>
        public ColorConfig()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorConfig"/> class with a single color.
        /// </summary>
        /// <param name="color">The hex color string for both light and dark modes.</param>
        public ColorConfig(string? color)
        {
            Light = color;
            Dark = color;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorConfig"/> class with separate light and dark colors.
        /// </summary>
        /// <param name="light">The hex color string for light mode.</param>
        /// <param name="dark">The hex color string for dark mode.</param>
        public ColorConfig(string? light, string? dark)
        {
            Light = light;
            Dark = dark;
        }

        #endregion

        #region Implicit Operators

        /// <summary>
        /// Implicitly converts a string color to a ColorConfig.
        /// </summary>
        /// <param name="color">The hex color string.</param>
        /// <returns>A ColorConfig with the specified color for both light and dark modes.</returns>
        public static implicit operator ColorConfig?(string? color)
        {
            return color is null ? null : new ColorConfig(color);
        }

        /// <summary>
        /// Implicitly converts a ColorConfig to a string.
        /// </summary>
        /// <param name="colorConfig">The color configuration.</param>
        /// <returns>The light color, dark color, or null.</returns>
        public static implicit operator string?(ColorConfig? colorConfig)
        {
            return colorConfig?.Light ?? colorConfig?.Dark;
        }

        /// <summary>
        /// Implicitly converts a ColorPairConfig to a ColorConfig.
        /// </summary>
        /// <param name="colorPairConfig">The color pair configuration.</param>
        /// <returns>A ColorConfig with the same light and dark values.</returns>
        public static implicit operator ColorConfig?(ColorPairConfig? colorPairConfig)
        {
            return colorPairConfig is null ? null : new ColorConfig(colorPairConfig.Light, colorPairConfig.Dark);
        }

        /// <summary>
        /// Implicitly converts a ColorConfig to a ColorPairConfig.
        /// </summary>
        /// <param name="colorConfig">The color configuration.</param>
        /// <returns>A ColorPairConfig with the same light and dark values.</returns>
        public static implicit operator ColorPairConfig?(ColorConfig? colorConfig)
        {
            return colorConfig is null ? null : new ColorPairConfig { Light = colorConfig.Light, Dark = colorConfig.Dark };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the string representation of the color configuration.
        /// </summary>
        /// <returns>The light color, dark color, or empty string.</returns>
        public override string ToString()
        {
            return Light ?? Dark ?? string.Empty;
        }

        #endregion

    }

}