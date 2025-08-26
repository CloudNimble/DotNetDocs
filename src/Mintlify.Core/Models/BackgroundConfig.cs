using System.Text.Json.Serialization;
using Mintlify.Core.Converters;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the background configuration for the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration controls the background appearance including images, decorations,
    /// and colors for the documentation site.
    /// </remarks>
    public class BackgroundConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the background color configuration.
        /// </summary>
        /// <remarks>
        /// Can be a hex color string or an object with color configuration properties.
        /// This controls the base background color of the documentation site.
        /// </remarks>
        [JsonPropertyName("color")]
        [JsonConverter(typeof(ColorConverter))]
        public object? Color { get; set; }

        /// <summary>
        /// Gets or sets the background decoration style.
        /// </summary>
        /// <remarks>
        /// Valid values are "gradient", "grid", or "windows". This adds decorative
        /// background patterns to enhance the visual appearance of the site.
        /// </remarks>
        public string? Decoration { get; set; }

        /// <summary>
        /// Gets or sets the background image configuration.
        /// </summary>
        /// <remarks>
        /// Can be a string URL for a single image, or an object with "light" and "dark" 
        /// properties for different images in each mode. Should be an absolute URL or 
        /// relative path to the image file.
        /// </remarks>
        [JsonPropertyName("image")]
        [JsonConverter(typeof(BackgroundImageConverter))]
        public object? Image { get; set; }

        #endregion

    }

}