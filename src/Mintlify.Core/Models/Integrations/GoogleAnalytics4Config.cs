namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for Google Analytics 4 integration.
    /// </summary>
    /// <remarks>
    /// Google Analytics 4 (GA4) is Google's next-generation analytics platform that
    /// provides insights about website traffic and user behavior. This configuration
    /// enables automatic tracking of documentation events to your GA4 property.
    /// </remarks>
    public class GoogleAnalytics4Config
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Google Analytics 4 measurement ID.
        /// </summary>
        /// <remarks>
        /// This is the unique identifier for your GA4 web data stream, typically in
        /// the format "G-XXXXXXXXXX". You can find this measurement ID in your Google
        /// Analytics property settings under Data Streams. Note that Google Analytics
        /// may take 2-3 days to start showing data after initial setup.
        /// </remarks>
        public string? MeasurementId { get; set; }

        #endregion

    }

}
