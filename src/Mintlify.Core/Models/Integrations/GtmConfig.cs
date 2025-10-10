namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for Google Tag Manager integration.
    /// </summary>
    /// <remarks>
    /// Google Tag Manager (GTM) is a tag management system that allows you to quickly
    /// update measurement codes and related code fragments (tags) on your website.
    /// This configuration enables GTM integration for flexible analytics tracking.
    /// </remarks>
    public class GtmConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Google Tag Manager container ID.
        /// </summary>
        /// <remarks>
        /// This is the unique identifier for your GTM container, typically in the format
        /// "GTM-XXXXXX". You can find this ID in your Google Tag Manager workspace
        /// settings. The container ID allows you to manage all tracking tags through
        /// the GTM interface.
        /// </remarks>
        public string? TagId { get; set; }

        #endregion

    }

}
