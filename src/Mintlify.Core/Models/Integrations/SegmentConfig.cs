namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for Segment integration.
    /// </summary>
    /// <remarks>
    /// Segment is a customer data platform that collects, transforms, and routes
    /// analytics data to various downstream tools. This configuration enables
    /// documentation event tracking to be sent to Segment, which can then forward
    /// the data to your configured destinations.
    /// </remarks>
    public class SegmentConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets a custom CDN URL for loading the Segment script.
        /// </summary>
        /// <remarks>
        /// Use this if you configure a custom domain in Segment to serve analytics from
        /// your own subdomain, which helps avoid ad blockers that block <c>cdn.segment.com</c>.
        /// Must be an HTTPS URL without query parameters or fragments. Defaults to
        /// <c>https://cdn.segment.com</c>. See https://www.mintlify.com/docs/integrations/analytics/segment.
        /// </remarks>
        public string? CdnUrl { get; set; }

        /// <summary>
        /// Gets or sets the Segment write key.
        /// </summary>
        /// <remarks>
        /// This is your Segment source's write key, which identifies which Segment
        /// source should receive the events. You can find this key in your Segment
        /// source settings. All documentation events will be sent to this Segment
        /// source and then forwarded to your configured destinations.
        /// </remarks>
        public string? Key { get; set; }

        #endregion

    }

}
