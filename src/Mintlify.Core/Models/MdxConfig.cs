using System.Text.Json.Serialization;
using Mintlify.Core.Converters;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the MDX configuration for API pages generated from MDX files.
    /// </summary>
    /// <remarks>
    /// This configuration allows manual definition of API endpoints in individual MDX files
    /// rather than using an OpenAPI specification. It provides flexibility for custom content
    /// and is useful for documenting small APIs or prototyping.
    /// </remarks>
    public class MdxConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the authentication configuration for MDX-based API requests.
        /// </summary>
        /// <remarks>
        /// Defines the authentication method and parameters required for API calls
        /// made from the documentation playground.
        /// </remarks>
        [JsonPropertyName("auth")]
        public MdxAuthConfig? Auth { get; set; }

        /// <summary>
        /// Gets or sets the base server URL(s) for API requests.
        /// </summary>
        /// <remarks>
        /// Can be a single string URL or an array of URLs for multiple base endpoints.
        /// Used as the base URL for all API requests made from the playground.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Single server
        /// "server": "https://api.example.com"
        ///
        /// // Multiple servers
        /// "server": ["https://api1.example.com", "https://api2.example.com"]
        /// </code>
        /// </example>
        [JsonPropertyName("server")]
        [JsonConverter(typeof(ServerConfigConverter))]
        public ServerConfig? Server { get; set; }

        #endregion

    }

    /// <summary>
    /// Represents the authentication configuration for MDX-based API documentation.
    /// </summary>
    /// <remarks>
    /// Specifies how API requests should be authenticated when using the interactive
    /// playground in MDX-generated API documentation pages.
    /// </remarks>
    public class MdxAuthConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the authentication method.
        /// </summary>
        /// <remarks>
        /// Valid values are: "bearer", "basic", "key", "cobo".
        /// - bearer: Bearer token authentication (Authorization: Bearer TOKEN)
        /// - basic: Basic authentication (Authorization: Basic BASE64)
        /// - key: API key authentication (custom header)
        /// - cobo: Cobo-specific authentication
        /// </remarks>
        /// <example>
        /// <code>
        /// "auth": {
        ///   "method": "bearer"
        /// }
        /// </code>
        /// </example>
        [JsonPropertyName("method")]
        public string? Method { get; set; }

        /// <summary>
        /// Gets or sets the name of the authentication header or parameter.
        /// </summary>
        /// <remarks>
        /// For "key" authentication method, this specifies the header name (e.g., "x-api-key").
        /// For other methods, this may specify additional configuration parameters.
        /// </remarks>
        /// <example>
        /// <code>
        /// "auth": {
        ///   "method": "key",
        ///   "name": "x-api-key"
        /// }
        /// </code>
        /// </example>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        #endregion

    }

}