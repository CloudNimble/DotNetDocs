namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for Heap analytics integration.
    /// </summary>
    /// <remarks>
    /// Heap is an analytics platform that automatically captures every user interaction
    /// on your site without requiring manual event tracking. This configuration enables
    /// Heap to track all documentation engagement automatically.
    /// </remarks>
    public class HeapConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Heap application ID.
        /// </summary>
        /// <remarks>
        /// This is the unique identifier for your Heap project. You can find this ID
        /// in your Heap project settings under Installation. All user interactions on
        /// your documentation will be automatically captured and sent to this Heap project.
        /// </remarks>
        public string? AppId { get; set; }

        #endregion

    }

}
