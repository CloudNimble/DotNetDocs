namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for Clearbit data enrichment integration.
    /// </summary>
    /// <remarks>
    /// Clearbit provides business intelligence and data enrichment services, helping
    /// you understand your documentation visitors by enriching analytics data with
    /// company and demographic information.
    /// </remarks>
    public class ClearbitConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the Clearbit public API key.
        /// </summary>
        /// <remarks>
        /// This is your public API key for Clearbit, which enables data enrichment
        /// features on your documentation site. The public key is safe to use in
        /// client-side code.
        /// </remarks>
        public string? PublicApiKey { get; set; }

        #endregion

    }

}
