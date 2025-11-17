using System.Diagnostics.CodeAnalysis;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a group configuration in Mintlify navigation.
    /// </summary>
    /// <remarks>
    /// Groups organize pages into sections in your navigation. The group name is required.
    /// </remarks>
    public class GroupConfig : NavigationItemBase
    {

        #region Properties

        /// <summary>
        /// Gets or sets whether the group is expanded by default in the navigation sidebar.
        /// </summary>
        /// <remarks>
        /// When true, the group will be expanded by default showing all its pages.
        /// When false or null, the group will be collapsed by default.
        /// </remarks>
        public bool? Expanded { get; set; }

        /// <summary>
        /// Gets or sets the name of the group.
        /// </summary>
        /// <remarks>
        /// This is a required field that appears as the group title in navigation.
        /// Group names cannot be null. While empty string names are technically accepted,
        /// they are not recommended as Mintlify treats each empty group as a separate
        /// ungrouped navigation section rather than merging them together.
        /// </remarks>
        [NotNull]
        public string Group { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the root page for the group.
        /// </summary>
        /// <remarks>
        /// Specifies the default page to display when the group is selected.
        /// </remarks>
        public string? Root { get; set; }

        /// <summary>
        /// Gets or sets the tag for the group.
        /// </summary>
        /// <remarks>
        /// Displays a label tag (e.g., "NEW", "BETA") next to the group name.
        /// </remarks>
        public string? Tag { get; set; }

        #endregion

    }

}
