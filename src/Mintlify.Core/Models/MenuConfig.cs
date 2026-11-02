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
        /// Gets or sets the directory layout for this menu item and its descendants.
        /// </summary>
        /// <remarks>
        /// Valid values are <c>none</c>, <c>accordion</c>, and <c>card</c>.
        /// </remarks>
        public string? Directory { get; set; }

        /// <summary>
        /// Gets or sets the groups for the menu item.
        /// </summary>
        public List<GroupConfig>? Groups { get; set; }

        /// <summary>
        /// Gets or sets whether the menu item is hidden by default.
        /// </summary>
        public bool? Hidden { get; set; }

        /// <summary>
        /// Gets or sets the URL or path for the menu item.
        /// </summary>
        public string? Href { get; set; }

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

        /// <summary>
        /// Gets or sets the SDK documentation artifact used to auto-populate pages.
        /// </summary>
        public SdkRefConfig? Sdk { get; set; }

        /// <summary>
        /// Gets or sets thumbnail overrides for pages under this menu item.
        /// </summary>
        public NavThumbnailConfig? Thumbnail { get; set; }

        #endregion

    }

}
