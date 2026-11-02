namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the Transcend Consent Management integration.
    /// </summary>
    /// <remarks>
    /// When Transcend is configured, Mintlify loads your Transcend bundle, shows
    /// Transcend's consent banner, and only loads analytics and feedback after
    /// visitors grant consent. Transcend owns the consent banner, so Mintlify's
    /// built-in banner does not appear. See https://www.mintlify.com/docs/integrations/privacy/transcend.
    /// </remarks>
    public class TranscendConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Transcend purpose name that gates analytics and feedback.
        /// </summary>
        /// <example>
        /// <code>
        /// "transcend": {
        ///   "bundleId": "YOUR_BUNDLE_ID",
        ///   "analyticsPurpose": "Product Analytics"
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// Defaults to <c>Analytics</c>. Set this if you renamed the purpose in Transcend.
        /// </remarks>
        public string? AnalyticsPurpose { get; set; }

        /// <summary>
        /// Gets or sets the Transcend bundle ID.
        /// </summary>
        /// <remarks>
        /// Required. The bundle ID appears in the Airgap script URL
        /// <c>https://transcend-cdn.com/cm/&lt;bundleId&gt;/airgap.js</c>.
        /// It can contain only letters, numbers, hyphens, and underscores.
        /// </remarks>
        public string? BundleId { get; set; }

        #endregion

    }

}
