using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a server configuration for API endpoints.
    /// </summary>
    /// <remarks>
    /// Server configurations can be simple URL strings or arrays of URLs for multiple endpoints.
    /// This is used specifically for MDX server configurations and API playground base URLs.
    /// </remarks>
    public class ServerConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the single server URL.
        /// </summary>
        /// <remarks>
        /// Used when only a single server URL is specified.
        /// </remarks>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the list of server URLs.
        /// </summary>
        /// <remarks>
        /// Used when multiple server URLs are provided.
        /// Not serialized directly as it's handled by the converter.
        /// </remarks>
        [JsonIgnore]
        public List<string>? Urls { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Implicitly converts a string URL to a ServerConfig.
        /// </summary>
        /// <param name="url">The server URL.</param>
        /// <returns>A ServerConfig with the specified URL.</returns>
        public static implicit operator ServerConfig?(string? url)
        {
            return url is null ? null : new ServerConfig { Url = url };
        }

        /// <summary>
        /// Implicitly converts a list of URLs to a ServerConfig.
        /// </summary>
        /// <param name="urls">The list of server URLs.</param>
        /// <returns>A ServerConfig with the specified URLs.</returns>
        public static implicit operator ServerConfig?(List<string>? urls)
        {
            return urls is null ? null : new ServerConfig { Urls = urls };
        }

        /// <summary>
        /// Implicitly converts a ServerConfig to a string.
        /// </summary>
        /// <param name="serverConfig">The server configuration.</param>
        /// <returns>The single URL, first URL from the list, or null.</returns>
        public static implicit operator string?(ServerConfig? serverConfig)
        {
            return serverConfig?.Url ?? serverConfig?.Urls?[0];
        }

        /// <summary>
        /// Implicitly converts a ServerConfig to a list of strings.
        /// </summary>
        /// <param name="serverConfig">The server configuration.</param>
        /// <returns>The URLs list, or a single-item list with the URL.</returns>
        public static implicit operator List<string>?(ServerConfig? serverConfig)
        {
            if (serverConfig?.Urls is not null)
            {
                return serverConfig.Urls;
            }
            return serverConfig?.Url is not null ? [serverConfig.Url] : null;
        }

        /// <summary>
        /// Returns the string representation of the server configuration.
        /// </summary>
        /// <returns>The primary URL or empty string.</returns>
        public override string ToString()
        {
            return Url ?? Urls?[0] ?? string.Empty;
        }

        #endregion

    }

}