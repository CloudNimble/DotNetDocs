namespace Mintlify.Core.Models
{

    /// <summary>
    /// Defines merging as combining two navigation structures into one integrated structure.
    /// Options control how different aspects of the merge are handled.
    /// </summary>
    public class MergeOptions
    {

        #region Properties

        /// <summary>
        /// Gets or sets whether to combine groups with empty string names at the same navigation level.
        /// When true, multiple groups with empty names will be merged into a single group.
        /// When false (default), each empty group remains separate, matching Mintlify's behavior.
        /// </summary>
        public bool CombineEmptyGroups { get; set; } = false;

        // Future options can be added here for other merge behaviors

        #endregion

    }

}