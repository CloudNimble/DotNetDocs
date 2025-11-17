using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the interaction configuration for navigation elements in Mintlify.
    /// </summary>
    /// <remarks>
    /// Controls how users interact with navigation elements such as groups and dropdowns,
    /// including whether expanding a group automatically navigates to its first page.
    /// </remarks>
    public class InteractionConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets whether to automatically navigate to the first page when expanding a navigation group.
        /// </summary>
        /// <remarks>
        /// Valid values are:
        /// - true: Force automatic navigation to the first page when a navigation group is selected
        /// - false: Prevent navigation and only expand or collapse the group when it is selected
        /// - null: Use the theme's default behavior
        ///
        /// Some themes will automatically navigate to the first page in a group when it is expanded.
        /// This setting allows you to override the theme's default behavior.
        /// </remarks>
        /// <example>
        /// <code>
        /// "interaction": {
        ///   "drilldown": false  // Never navigate, only expand or collapse the group
        /// }
        /// </code>
        /// </example>
        /// <example>
        /// <code>
        /// "interaction": {
        ///   "drilldown": true  // Force navigation to first page when a user expands a dropdown
        /// }
        /// </code>
        /// </example>
        [JsonPropertyName("drilldown")]
        public bool? Drilldown { get; set; }

        #endregion

    }

}
