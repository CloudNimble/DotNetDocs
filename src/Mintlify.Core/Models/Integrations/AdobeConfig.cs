namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for Adobe Analytics integration.
    /// </summary>
    /// <remarks>
    /// Adobe Analytics is an enterprise analytics platform that provides advanced
    /// reporting and analysis capabilities. This configuration enables tracking of
    /// documentation engagement through Adobe's analytics services.
    /// </remarks>
    public class AdobeConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Adobe Analytics launch URL.
        /// </summary>
        /// <remarks>
        /// This is the URL for the Adobe Analytics launch script. You can find this
        /// URL in your Adobe Experience Platform Launch configuration. The script
        /// will be loaded to enable analytics tracking on documentation pages.
        /// </remarks>
        public string? LaunchUrl { get; set; }

        #endregion

    }

}
