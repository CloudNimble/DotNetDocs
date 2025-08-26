using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the API playground settings for interactive documentation.
    /// </summary>
    /// <remarks>
    /// Controls how the API playground is displayed and whether API requests
    /// are proxied through Mintlify's servers for CORS handling.
    /// </remarks>
    public class ApiPlaygroundConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the display mode of the API playground.
        /// </summary>
        /// <remarks>
        /// Valid values are:
        /// - "interactive": Full interactive playground with request/response testing
        /// - "simple": Copyable endpoint with no interactive features
        /// - "none": Hide the playground completely
        /// Defaults to "interactive" when not specified.
        /// </remarks>
        /// <example>
        /// <code>
        /// "playground": {
        ///   "display": "interactive"
        /// }
        /// </code>
        /// </example>
        [JsonPropertyName("display")]
        public string? Display { get; set; }

        /// <summary>
        /// Gets or sets whether to pass API requests through a proxy server.
        /// </summary>
        /// <remarks>
        /// When true, API requests from the playground are routed through Mintlify's
        /// proxy servers to handle CORS restrictions. This allows the playground to
        /// make requests to APIs that don't have CORS configured.
        /// Defaults to true when not specified.
        /// </remarks>
        /// <example>
        /// <code>
        /// "playground": {
        ///   "proxy": false
        /// }
        /// </code>
        /// </example>
        [JsonPropertyName("proxy")]
        public bool? Proxy { get; set; }

        #endregion

    }

}