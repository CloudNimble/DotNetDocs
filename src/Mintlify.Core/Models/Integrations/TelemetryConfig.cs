namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for telemetry and feedback functionality.
    /// </summary>
    /// <remarks>
    /// This configuration controls whether telemetry and user feedback features are
    /// enabled on documentation pages, allowing collection of usage data and reader
    /// feedback to improve documentation quality.
    /// </remarks>
    public class TelemetryConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets whether telemetry and feedback collection is enabled.
        /// </summary>
        /// <remarks>
        /// When enabled, documentation pages will include telemetry tracking and
        /// feedback collection mechanisms. This allows documentation maintainers
        /// to gather insights about how readers interact with the content.
        /// </remarks>
        public bool? Enabled { get; set; }

        #endregion

    }

}
