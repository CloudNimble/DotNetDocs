namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the metadata configuration for the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration controls metadata-related settings for documentation pages,
    /// including whether timestamps are displayed showing the last modified date.
    /// </remarks>
    public class MetadataConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets whether to show the last modified date on pages.
        /// </summary>
        /// <remarks>
        /// When enabled, each documentation page will display a timestamp indicating
        /// when the page was last modified. This helps readers assess the freshness
        /// of the content.
        /// </remarks>
        public bool? Timestamp { get; set; }

        #endregion

    }

}
