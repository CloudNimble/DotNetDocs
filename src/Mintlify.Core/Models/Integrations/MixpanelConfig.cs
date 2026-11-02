namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for Mixpanel analytics integration.
    /// </summary>
    /// <remarks>
    /// Mixpanel is a product analytics platform that helps you understand user behavior
    /// through event tracking and user analysis. This configuration enables automatic
    /// tracking of documentation engagement events to your Mixpanel project.
    /// </remarks>
    public class MixpanelConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Mixpanel project token.
        /// </summary>
        /// <remarks>
        /// This is the unique identifier for your Mixpanel project. You can find this
        /// token in your Mixpanel project settings. All documentation events will be
        /// sent to the project associated with this token.
        /// </remarks>
        public string? ProjectToken { get; set; }

        /// <summary>
        /// Gets or sets the Mixpanel data residency region.
        /// </summary>
        /// <example>
        /// <code>
        /// "mixpanel": {
        ///   "projectToken": "YOUR_MIXPANEL_PROJECT_TOKEN",
        ///   "region": "eu"
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// Supported values are <c>us</c> (default, <c>api.mixpanel.com</c>),
        /// <c>eu</c> (<c>api-eu.mixpanel.com</c>), and <c>in</c> (<c>api-in.mixpanel.com</c>).
        /// Omit the property to use the US host. Set this to match the Mixpanel project's
        /// data residency region or Mixpanel drops the events.
        /// See https://www.mintlify.com/docs/integrations/analytics/mixpanel.
        /// </remarks>
        public string? Region { get; set; }

        #endregion

    }

}
