using System.Text.Json.Serialization;
using Mintlify.Core.Models.Integrations;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the integrations configuration for third-party services in the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration enables integration with various analytics, feedback, and other third-party
    /// services. Each integration has specific configuration requirements defined in their respective
    /// configuration classes. These integrations allow automatic tracking of documentation engagement
    /// and user behavior to your preferred analytics platforms.
    /// </remarks>
    public class IntegrationsConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Amplitude analytics integration configuration.
        /// </summary>
        /// <remarks>
        /// Amplitude provides product analytics to understand user behavior and engagement.
        /// When configured, all documentation events are automatically sent to your Amplitude project.
        /// </remarks>
        public AmplitudeConfig? Amplitude { get; set; }

        /// <summary>
        /// Gets or sets the Clearbit data enrichment integration configuration.
        /// </summary>
        /// <remarks>
        /// Clearbit enriches your analytics data with company and demographic information.
        /// When configured, visitor data is enhanced with business intelligence.
        /// </remarks>
        public ClearbitConfig? Clearbit { get; set; }

        /// <summary>
        /// Gets or sets the Fathom analytics integration configuration.
        /// </summary>
        /// <remarks>
        /// Fathom provides simple, privacy-focused analytics that is GDPR compliant
        /// and doesn't use cookies. When configured, page views and events are tracked
        /// in your Fathom dashboard.
        /// </remarks>
        public FathomConfig? Fathom { get; set; }

        /// <summary>
        /// Gets or sets the Google Analytics 4 integration configuration.
        /// </summary>
        /// <remarks>
        /// Google Analytics 4 (GA4) is Google's next-generation analytics platform.
        /// When configured, all documentation events are sent to your GA4 property.
        /// Note that GA4 data may take 2-3 days to appear after initial setup.
        /// </remarks>
        [JsonPropertyName("ga4")]
        public GoogleAnalytics4Config? GoogleAnalytics4 { get; set; }

        /// <summary>
        /// Gets or sets the Google Tag Manager integration configuration.
        /// </summary>
        /// <remarks>
        /// Google Tag Manager (GTM) allows flexible tag management for analytics and marketing.
        /// When configured, you can manage all tracking tags through the GTM interface.
        /// </remarks>
        public GtmConfig? Gtm { get; set; }

        /// <summary>
        /// Gets or sets the Heap analytics integration configuration.
        /// </summary>
        /// <remarks>
        /// Heap automatically captures all user interactions without manual event tracking.
        /// When configured, every user interaction on your documentation is automatically tracked.
        /// </remarks>
        public HeapConfig? Heap { get; set; }

        /// <summary>
        /// Gets or sets the Hightouch data activation integration configuration.
        /// </summary>
        /// <remarks>
        /// Hightouch syncs data from your warehouse to business tools. When configured,
        /// documentation events can be synchronized to your data warehouse and downstream tools.
        /// </remarks>
        public HightouchConfig? Hightouch { get; set; }

        /// <summary>
        /// Gets or sets the Hotjar behavior analytics integration configuration.
        /// </summary>
        /// <remarks>
        /// Hotjar provides heatmaps, session recordings, and user feedback tools.
        /// When configured, you can visualize how users interact with your documentation.
        /// </remarks>
        public HotjarConfig? Hotjar { get; set; }

        /// <summary>
        /// Gets or sets the LogRocket session replay integration configuration.
        /// </summary>
        /// <remarks>
        /// LogRocket records user sessions including console logs and network activity.
        /// When configured, you can replay user sessions to understand issues and behavior.
        /// </remarks>
        [JsonPropertyName("logrocket")]
        public LogRocketConfig? LogRocket { get; set; }

        /// <summary>
        /// Gets or sets the Mixpanel product analytics integration configuration.
        /// </summary>
        /// <remarks>
        /// Mixpanel provides detailed product analytics and user behavior tracking.
        /// When configured, all documentation events are sent to your Mixpanel project.
        /// </remarks>
        public MixpanelConfig? Mixpanel { get; set; }

        /// <summary>
        /// Gets or sets the Pirsch analytics integration configuration.
        /// </summary>
        /// <remarks>
        /// Pirsch is a privacy-friendly, GDPR-compliant analytics platform.
        /// When configured, page views and events are tracked without cookies.
        /// </remarks>
        public PirschConfig? Pirsch { get; set; }

        /// <summary>
        /// Gets or sets the Plausible analytics integration configuration.
        /// </summary>
        /// <remarks>
        /// Plausible is a lightweight, open-source, privacy-focused analytics platform.
        /// When configured, documentation engagement is tracked without cookies.
        /// </remarks>
        public PlausibleConfig? Plausible { get; set; }

        /// <summary>
        /// Gets or sets the PostHog product analytics integration configuration.
        /// </summary>
        /// <remarks>
        /// PostHog provides session recording, feature flags, and product analytics.
        /// When configured, all documentation events are sent to your PostHog instance.
        /// </remarks>
        [JsonPropertyName("posthog")]
        public PostHogConfig? PostHog { get; set; }

        /// <summary>
        /// Gets or sets the Segment customer data platform integration configuration.
        /// </summary>
        /// <remarks>
        /// Segment collects, transforms, and routes analytics data to various destinations.
        /// When configured, documentation events are sent to Segment and forwarded to your
        /// configured downstream tools.
        /// </remarks>
        public SegmentConfig? Segment { get; set; }

        #endregion

    }

}