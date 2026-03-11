namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for Koala analytics integration.
    /// </summary>
    /// <remarks>
    /// Koala is an analytics platform focused on identifying and understanding user
    /// behavior. This configuration enables tracking of documentation engagement
    /// through your Koala analytics account.
    /// </remarks>
    public class KoalaConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Koala public API key.
        /// </summary>
        /// <remarks>
        /// This is the public API key for your Koala analytics account. You can find
        /// this key in your Koala dashboard under account settings. All documentation
        /// events will be tracked using this API key.
        /// </remarks>
        public string? PublicApiKey { get; set; }

        #endregion

    }

}
