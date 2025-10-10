namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for Plausible analytics integration.
    /// </summary>
    /// <remarks>
    /// Plausible is a lightweight, open-source, privacy-friendly web analytics platform
    /// that doesn't use cookies. This configuration enables tracking of documentation
    /// engagement to your Plausible analytics instance.
    /// </remarks>
    public class PlausibleConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the domain being tracked in Plausible.
        /// </summary>
        /// <remarks>
        /// This is the domain name that you registered in your Plausible account.
        /// It should match the domain where your documentation is hosted. All
        /// analytics events will be attributed to this domain in your Plausible dashboard.
        /// </remarks>
        public string? Domain { get; set; }

        /// <summary>
        /// Gets or sets the custom Plausible server URL.
        /// </summary>
        /// <remarks>
        /// This is optional and only needed if you are self-hosting Plausible or using
        /// a custom Plausible server. If not specified, the default Plausible cloud
        /// service will be used. The URL should point to your self-hosted Plausible
        /// instance.
        /// </remarks>
        public string? Server { get; set; }

        #endregion

    }

}
