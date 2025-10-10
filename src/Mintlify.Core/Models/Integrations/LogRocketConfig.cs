namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for LogRocket integration.
    /// </summary>
    /// <remarks>
    /// LogRocket is a session replay and analytics platform that records user sessions,
    /// including console logs, network activity, and DOM changes. This configuration
    /// enables LogRocket session recording on your documentation site.
    /// </remarks>
    public class LogRocketConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the LogRocket application ID.
        /// </summary>
        /// <remarks>
        /// This is the unique identifier for your LogRocket application, typically in
        /// the format "organization-name/app-name". You can find this ID in your LogRocket
        /// project settings. All session recordings and analytics will be sent to this
        /// LogRocket application.
        /// </remarks>
        public string? AppId { get; set; }

        #endregion

    }

}
