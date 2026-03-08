namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for Intercom integration.
    /// </summary>
    /// <remarks>
    /// Intercom is a customer messaging platform that provides live chat, help desk,
    /// and customer engagement features. This configuration enables the Intercom
    /// messenger widget on documentation pages for user support.
    /// </remarks>
    public class IntercomConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Intercom app ID.
        /// </summary>
        /// <remarks>
        /// This is the unique identifier for your Intercom application. You can find
        /// this ID in your Intercom dashboard under app settings. The Intercom messenger
        /// widget will be loaded using this app ID on documentation pages.
        /// </remarks>
        public string? AppId { get; set; }

        #endregion

    }

}
