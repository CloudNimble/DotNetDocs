using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Mintlify.Core.Converters;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a menu item configuration in Mintlify navigation.
    /// </summary>
    /// <remarks>
    /// Menu items appear as dropdown menu options within tabs.
    /// The item name is required.
    /// </remarks>
    public class MenuConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the description of the menu item.
        /// </summary>
        /// <remarks>
        /// Provides additional descriptive text for the menu item.
        /// </remarks>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the groups for the menu item.
        /// </summary>
        public List<GroupConfig>? Groups { get; set; }

        /// <summary>
        /// Gets or sets the icon to be displayed for the menu item.
        /// </summary>
        [JsonConverter(typeof(IconConverter))]
        public IconConfig? Icon { get; set; }

        /// <summary>
        /// Gets or sets the name of the menu item.
        /// </summary>
        /// <remarks>
        /// This is a required field that appears as the menu item label.
        /// </remarks>
        [NotNull]
        public string Item { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the pages for the menu item.
        /// </summary>
        /// <remarks>
        /// Pages can be strings (page paths) or nested GroupConfig objects.
        /// </remarks>
        [JsonConverter(typeof(NavigationPageListConverter))]
        public List<object>? Pages { get; set; }

        #endregion

    }

}
