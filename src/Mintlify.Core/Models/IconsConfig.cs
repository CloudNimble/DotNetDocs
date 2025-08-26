using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the icon library configuration for the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration determines which icon library is used throughout the documentation
    /// for displaying icons in navigation, buttons, and other UI elements.
    /// </remarks>
    public class IconsConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the icon library to be used throughout the documentation.
        /// </summary>
        /// <remarks>
        /// Valid values are "fontawesome" or "lucide". The selected library determines
        /// which icon names are available for use in navigation items, buttons, and other
        /// UI components. FontAwesome provides a comprehensive set of icons, while Lucide
        /// offers a more minimal, modern icon set. Defaults to "fontawesome".
        /// </remarks>
        public string? Library { get; set; }

        #endregion

    }

}
