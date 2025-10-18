namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for PostHog analytics integration.
    /// </summary>
    /// <remarks>
    /// PostHog is an open-source product analytics platform that provides session
    /// recording, feature flags, and product analytics. This configuration enables
    /// automatic tracking of documentation events to your PostHog instance.
    /// </remarks>
    public class PostHogConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the PostHog project API key.
        /// </summary>
        /// <remarks>
        /// This is your PostHog project's API key, which typically starts with "phc_".
        /// You can find this key in your PostHog project settings. All documentation
        /// events will be sent to the PostHog project associated with this API key.
        /// </remarks>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the PostHog API host URL.
        /// </summary>
        /// <remarks>
        /// This is optional and only needed if you are self-hosting PostHog. If not
        /// specified, events will be sent to the default PostHog cloud service at
        /// https://app.posthog.com. If you are self-hosting, specify the full URL
        /// to your PostHog instance.
        /// </remarks>
        public string? ApiHost { get; set; }

        #endregion

    }

}
