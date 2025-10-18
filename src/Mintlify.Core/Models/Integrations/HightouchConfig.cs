namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for Hightouch integration.
    /// </summary>
    /// <remarks>
    /// Hightouch is a data activation platform that syncs data from your warehouse
    /// to business tools. This configuration enables Hightouch tracking on your
    /// documentation site.
    /// </remarks>
    public class HightouchConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Hightouch API key.
        /// </summary>
        /// <remarks>
        /// This is your Hightouch API key that enables event tracking and data
        /// synchronization from your documentation site to Hightouch.
        /// </remarks>
        public string? ApiKey { get; set; }

        #endregion

    }

}
