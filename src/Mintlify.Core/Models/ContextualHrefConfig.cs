using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a custom contextual option destination.
    /// </summary>
    /// <remarks>
    /// May be a URL string or an object with <see cref="Base"/> and <see cref="Query"/>.
    /// See https://www.mintlify.com/docs/ai/contextual-menu.
    /// </remarks>
    public class ContextualHrefConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the base URL when using a query object.
        /// </summary>
        public string? Base { get; set; }

        /// <summary>
        /// Gets or sets query-string pairs appended to <see cref="Base"/>.
        /// </summary>
        public List<ContextualHrefQueryConfig>? Query { get; set; }

        /// <summary>
        /// Gets or sets the URL when the href is a simple string.
        /// </summary>
        [JsonIgnore]
        public string? Url { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Implicitly converts a URL string to a href config.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>A href config, or null.</returns>
        public static implicit operator ContextualHrefConfig?(string? url)
        {
            return url is null ? null : new ContextualHrefConfig { Url = url };
        }

        /// <summary>
        /// Implicitly converts a href config to a URL string.
        /// </summary>
        /// <param name="href">The href configuration.</param>
        /// <returns>The URL or base, or null.</returns>
        public static implicit operator string?(ContextualHrefConfig? href)
        {
            return href?.Url ?? href?.Base;
        }

        #endregion

    }

}
