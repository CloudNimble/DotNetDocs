namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for Fathom analytics integration.
    /// </summary>
    /// <remarks>
    /// Fathom is a simple, privacy-focused website analytics platform that doesn't
    /// use cookies and is GDPR compliant. This configuration enables tracking of
    /// documentation engagement to your Fathom analytics dashboard.
    /// </remarks>
    public class FathomConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Fathom site ID.
        /// </summary>
        /// <remarks>
        /// This is the unique identifier for your site in Fathom analytics. You can
        /// find this ID in your Fathom dashboard under site settings. All documentation
        /// events will be tracked under this site.
        /// </remarks>
        public string? SiteId { get; set; }

        #endregion

    }

}
