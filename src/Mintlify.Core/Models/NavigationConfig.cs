using System.Collections.Generic;
using System.Text.Json.Serialization;
using Mintlify.Core.Converters;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the navigation configuration for Mintlify.
    /// </summary>
    /// <remarks>
    /// This is a required field that defines the structure of your documentation.
    /// At minimum, you need to define some navigation items (pages, groups, tabs, etc.).
    /// </remarks>
    public class NavigationConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the anchors in the navigation.
        /// </summary>
        public List<AnchorConfig>? Anchors { get; set; }

        /// <summary>
        /// Gets or sets the dropdowns in the navigation.
        /// </summary>
        public List<DropdownConfig>? Dropdowns { get; set; }

        /// <summary>
        /// Gets or sets global navigation items that appear on all sections and pages.
        /// </summary>
        public GlobalNavigationConfig? Global { get; set; }

        /// <summary>
        /// Gets or sets the groups in the navigation.
        /// </summary>
        public List<GroupConfig>? Groups { get; set; }

        /// <summary>
        /// Gets or sets the languages in the navigation.
        /// </summary>
        public List<LanguageConfig>? Languages { get; set; }

        /// <summary>
        /// Gets or sets the pages in the navigation.
        /// </summary>
        /// <remarks>
        /// Pages can be either strings (page paths) or nested GroupConfig objects.
        /// </remarks>
        [JsonPropertyName("pages")]
        [JsonConverter(typeof(NavigationPageListConverter))]
        public List<object>? Pages { get; set; }

        /// <summary>
        /// Gets or sets the tabs in the navigation.
        /// </summary>
        public List<TabConfig>? Tabs { get; set; }

        /// <summary>
        /// Gets or sets the versions in the navigation.
        /// </summary>
        public List<VersionConfig>? Versions { get; set; }

        #endregion

    }

}