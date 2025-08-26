using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the appearance configuration for light and dark mode settings in Mintlify.
    /// </summary>
    /// <remarks>
    /// This configuration controls the default appearance mode and whether users can toggle
    /// between light and dark modes in the documentation site.
    /// </remarks>
    public class AppearanceConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the default light/dark mode for the documentation site.
        /// </summary>
        /// <remarks>
        /// Valid values are "system" (follows user's system preference), "light", or "dark".
        /// Defaults to "system" if not specified.
        /// </remarks>
        public string? Default { get; set; }

        /// <summary>
        /// Gets or sets whether to hide the light/dark mode toggle from users.
        /// </summary>
        /// <remarks>
        /// When set to true, users will not be able to switch between light and dark modes,
        /// and the site will use only the default mode specified. Defaults to false.
        /// </remarks>
        public bool? Strict { get; set; }

        #endregion

    }

}
