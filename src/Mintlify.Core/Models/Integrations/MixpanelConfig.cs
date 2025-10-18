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

        #endregion

    }

}
