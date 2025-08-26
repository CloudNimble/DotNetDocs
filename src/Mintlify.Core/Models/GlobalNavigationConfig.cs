using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents global navigation configuration that appears on all sections and pages.
    /// </summary>
    public class GlobalNavigationConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the anchors configuration.
        /// </summary>
        public List<GlobalAnchorConfig>? Anchors { get; set; }

        /// <summary>
        /// Gets or sets the dropdowns configuration.
        /// </summary>
        public List<GlobalDropdownConfig>? Dropdowns { get; set; }

        /// <summary>
        /// Gets or sets the languages configuration.
        /// </summary>
        public List<GlobalLanguageConfig>? Languages { get; set; }

        /// <summary>
        /// Gets or sets the tabs configuration.
        /// </summary>
        public List<GlobalTabConfig>? Tabs { get; set; }

        /// <summary>
        /// Gets or sets the versions configuration.
        /// </summary>
        public List<GlobalVersionConfig>? Versions { get; set; }

        #endregion

    }

}