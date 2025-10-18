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
