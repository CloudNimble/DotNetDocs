namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for Front chat widget integration.
    /// </summary>
    /// <remarks>
    /// Front is a customer communication platform that provides a chat widget for
    /// real-time support. This configuration enables the Front chat widget on
    /// documentation pages, allowing users to reach support directly.
    /// </remarks>
    public class FrontChatConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Front chat snippet ID.
        /// </summary>
        /// <remarks>
        /// This is the unique identifier for your Front chat widget snippet. You can
        /// find this ID in your Front dashboard under the chat widget configuration.
        /// The chat widget will be loaded and displayed on documentation pages.
        /// </remarks>
        public string? SnippetId { get; set; }

        #endregion

    }

}
