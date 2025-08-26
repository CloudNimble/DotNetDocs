using System.Text.Json.Serialization;

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

    }

}