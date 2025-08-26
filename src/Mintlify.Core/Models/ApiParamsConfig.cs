using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the display settings for API parameters in the documentation.
    /// </summary>
    /// <remarks>
    /// Controls how API parameters are displayed in the interactive playground
    /// and documentation pages, including whether they are expanded by default.
    /// </remarks>
    public class ApiParamsConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets whether to expand all parameters by default.
        /// </summary>
        /// <remarks>
        /// When true, all API parameters will be displayed in an expanded state.
        /// When false or null, parameters will be collapsed by default.
        /// Defaults to collapsed (false) when not specified.
        /// </remarks>
        /// <example>
        /// <code>
        /// "params": {
        ///   "expanded": true
        /// }
        /// </code>
        /// </example>
        [JsonPropertyName("expanded")]
        public bool? Expanded { get; set; }

        #endregion

    }

}