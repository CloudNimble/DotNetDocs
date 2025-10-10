namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for Hotjar integration.
    /// </summary>
    /// <remarks>
    /// Hotjar is a behavior analytics tool that provides heatmaps, session recordings,
    /// and feedback polls to understand how users interact with your documentation.
    /// This configuration enables Hotjar tracking on your site.
    /// </remarks>
    public class HotjarConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Hotjar site ID.
        /// </summary>
        /// <remarks>
        /// This is the unique identifier for your site in Hotjar, found in your Hotjar
        /// tracking code. It is typically a numeric value that identifies which Hotjar
        /// site configuration to use.
        /// </remarks>
        public string? Hjid { get; set; }

        /// <summary>
        /// Gets or sets the Hotjar script version.
        /// </summary>
        /// <remarks>
        /// This is the version number of the Hotjar tracking script, also found in your
        /// Hotjar tracking code. It ensures that the correct version of the tracking
        /// script is loaded for your site.
        /// </remarks>
        public string? Hjsv { get; set; }

        #endregion

    }

}
