namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for Amplitude analytics integration.
    /// </summary>
    /// <remarks>
    /// Amplitude is a product analytics platform that helps teams build better products
    /// through data-driven insights. This configuration enables automatic tracking of
    /// documentation engagement events to your Amplitude project.
    /// </remarks>
    public class AmplitudeConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Amplitude API key for your project.
        /// </summary>
        /// <remarks>
        /// This is the unique identifier for your Amplitude project. You can find this
        /// key in your Amplitude project settings. All documentation events will be
        /// sent to the project associated with this API key.
        /// </remarks>
        public string? ApiKey { get; set; }

        #endregion

    }

}
