namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for Pirsch analytics integration.
    /// </summary>
    /// <remarks>
    /// Pirsch is a privacy-friendly, GDPR-compliant web analytics platform that provides
    /// simple, cookie-free website analytics. This configuration enables tracking of
    /// documentation engagement to your Pirsch analytics dashboard.
    /// </remarks>
    public class PirschConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Pirsch identification code.
        /// </summary>
        /// <remarks>
        /// This is the unique identifier for your Pirsch analytics instance. You can
        /// find this ID in your Pirsch dashboard settings. All documentation events
        /// will be tracked under this Pirsch account.
        /// </remarks>
        public string? Id { get; set; }

        #endregion

    }

}
