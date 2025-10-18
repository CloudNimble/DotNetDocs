using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a background image configuration in Mintlify.
    /// </summary>
    /// <remarks>
    /// Background images can be simple URL/path references or theme-specific configurations
    /// with separate images for light and dark modes. This matches the official Mintlify schema.
    /// </remarks>
    public class BackgroundImageConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the background image for dark mode.
        /// </summary>
        /// <remarks>
        /// Can be an absolute URL or relative path to an image file.
        /// Used when the theme switches to dark mode.
        /// </remarks>
        public string? Dark { get; set; }

        /// <summary>
        /// Gets or sets the background image for light mode.
        /// </summary>
        /// <remarks>
        /// Can be an absolute URL or relative path to an image file.
        /// Used when the theme is in light mode.
        /// </remarks>
        public string? Light { get; set; }

        /// <summary>
        /// Gets or sets the single image URL when not using theme-specific images.
        /// </summary>
        /// <remarks>
        /// Used internally when the background image is a simple string.
        /// Not serialized directly as it's handled by the converter.
        /// </remarks>
        [JsonIgnore]
        public string? Url { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Implicitly converts a string URL to a BackgroundImageConfig.
        /// </summary>
        /// <param name="url">The background image URL.</param>
        /// <returns>A BackgroundImageConfig with the specified URL.</returns>
        public static implicit operator BackgroundImageConfig?(string? url)
        {
            return url is null ? null : new BackgroundImageConfig { Url = url };
        }

        /// <summary>
        /// Implicitly converts a BackgroundImageConfig to a string.
        /// </summary>
        /// <param name="backgroundImageConfig">The background image configuration.</param>
        /// <returns>The URL, light image, dark image, or null.</returns>
        public static implicit operator string?(BackgroundImageConfig? backgroundImageConfig)
        {
            return backgroundImageConfig?.Url ?? backgroundImageConfig?.Light ?? backgroundImageConfig?.Dark;
        }

        /// <summary>
        /// Returns the string representation of the background image configuration.
        /// </summary>
        /// <returns>The URL, light image, dark image, or empty string.</returns>
        public override string ToString()
        {
            return Url ?? Light ?? Dark ?? string.Empty;
        }

        #endregion

    }

}