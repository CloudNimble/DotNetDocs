namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the OneTrust cookie consent integration.
    /// </summary>
    /// <remarks>
    /// Configure OneTrust when you use it as your consent management platform.
    /// <see cref="DomainScript"/> is required.
    /// </remarks>
    public class OneTrustConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the OneTrust cookie category ID used to gate analytics.
        /// </summary>
        public string? CategoryId { get; set; }

        /// <summary>
        /// Gets or sets the OneTrust domain script identifier.
        /// </summary>
        /// <remarks>
        /// This is the required data-domain-script value from the OneTrust snippet.
        /// </remarks>
        public string? DomainScript { get; set; }

        /// <summary>
        /// Gets or sets an alternate URI for the OneTrust stub script.
        /// </summary>
        public string? ScriptSource { get; set; }

        #endregion

    }

}
