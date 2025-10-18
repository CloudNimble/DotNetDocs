using System.Diagnostics.CodeAnalysis;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the color configuration for Mintlify themes.
    /// </summary>
    /// <remarks>
    /// The colors to use in your documentation. At the very least, you must define the primary color.
    /// </remarks>
    public class ColorsConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the dark color of the theme in hex format. Used for light mode.
        /// </summary>
        public string? Dark { get; set; }

        /// <summary>
        /// Gets or sets the light color of the theme in hex format. Used for dark mode.
        /// </summary>
        public string? Light { get; set; }

        /// <summary>
        /// Gets or sets the primary color of the theme in hex format.
        /// </summary>
        /// <remarks>
        /// This is a required field. Must be a valid hex color in format #RRGGBB or #RGB.
        /// </remarks>
        [NotNull]
        public string Primary { get; set; } = "#000000";

        #endregion

    }

}