namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents an icon configuration in Mintlify.
    /// </summary>
    /// <remarks>
    /// Icons can be simple string references or detailed configurations with style and library options.
    /// This matches the official Mintlify schema at https://mintlify.com/docs.json
    /// </remarks>
    public class IconConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the icon name.
        /// </summary>
        /// <remarks>
        /// This is the specific icon name, such as "home", "folder", "user", etc.
        /// </remarks>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the icon library.
        /// </summary>
        /// <remarks>
        /// Specifies which icon library to use. Defaults to "fontawesome" if not specified.
        /// Supported libraries include "fontawesome" and "lucide".
        /// </remarks>
        public string? Library { get; set; }

        /// <summary>
        /// Gets or sets the icon style.
        /// </summary>
        /// <remarks>
        /// Specifies the style variant of the icon. Common styles include:
        /// "brands", "duotone", "light", "regular", "solid".
        /// </remarks>
        public string? Style { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Implicitly converts a string to an IconConfig.
        /// </summary>
        /// <param name="iconName">The icon name.</param>
        /// <returns>An IconConfig with the specified name.</returns>
        public static implicit operator IconConfig?(string? iconName)
        {
            return iconName is null ? null : new IconConfig { Name = iconName };
        }

        /// <summary>
        /// Implicitly converts an IconConfig to a string.
        /// </summary>
        /// <param name="iconConfig">The icon configuration.</param>
        /// <returns>The icon name, or null if the configuration is null.</returns>
        public static implicit operator string?(IconConfig? iconConfig)
        {
            return iconConfig?.Name;
        }

        /// <summary>
        /// Returns the string representation of the icon.
        /// </summary>
        /// <returns>The icon name or an empty string if null.</returns>
        public override string ToString()
        {
            return Name ?? string.Empty;
        }

        #endregion

    }

}