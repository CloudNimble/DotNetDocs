namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the integrations configuration for third-party services in the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration enables integration with various analytics, feedback, and other third-party
    /// services. Each integration has specific configuration requirements that can be added as properties.
    /// Common integrations include Google Analytics, Amplitude, Intercom, Hotjar, and others.
    /// </remarks>
    public class IntegrationsConfig
    {

        #region Properties

        // NOTE: Individual integration configurations can be added here as needed.
        // Examples of supported integrations:
        // - Google Analytics 4 (ga4): measurementId
        // - Amplitude: apiKey
        // - Intercom: appId  
        // - Hotjar: hjid, hjsv
        // - Mixpanel: projectToken
        // - PostHog: apiKey, apiHost
        // - Segment: key
        // - Plausible: domain, server
        // - And many others as documented in the Mintlify schema

        #endregion

    }

}