using System.Collections.Generic;
using System.Text.Json.Serialization;
using Mintlify.Core.Converters;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the navigation bar configuration for the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration controls the content and appearance of the top navigation bar,
    /// including custom links and primary call-to-action buttons.
    /// </remarks>
    public class NavbarConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the list of navigation links to display in the navbar.
        /// </summary>
        /// <remarks>
        /// Each link should have a label, optional icon, and href pointing to the destination.
        /// These links appear in the top navigation bar of the documentation site.
        /// </remarks>
        public List<NavbarLink>? Links { get; set; }

        /// <summary>
        /// Gets or sets the primary call-to-action configuration in the navbar.
        /// </summary>
        /// <remarks>
        /// Can be a button configuration with type "button", label, and href properties,
        /// or a GitHub configuration with type "github" and href properties.
        /// This appears prominently in the navbar to drive user actions.
        /// </remarks>
        [JsonPropertyName("primary")]
        [JsonConverter(typeof(PrimaryNavigationConverter))]
        public PrimaryNavigationConfig? Primary { get; set; }

        #endregion

    }

}