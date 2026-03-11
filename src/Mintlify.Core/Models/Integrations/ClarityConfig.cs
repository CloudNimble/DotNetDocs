namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for Microsoft Clarity integration.
    /// </summary>
    /// <remarks>
    /// Microsoft Clarity is a free behavioral analytics tool that provides session
    /// recordings and heatmaps. This configuration enables tracking of user behavior
    /// on documentation pages through your Clarity project.
    /// </remarks>
    public class ClarityConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Clarity project ID.
        /// </summary>
        /// <remarks>
        /// This is the unique identifier for your project in Microsoft Clarity. You
        /// can find this ID in your Clarity dashboard under project settings. All
        /// documentation session recordings and heatmaps will be tracked under this project.
        /// </remarks>
        public string? ProjectId { get; set; }

        #endregion

    }

}
