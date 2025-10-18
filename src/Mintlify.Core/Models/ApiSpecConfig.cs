using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents an API specification configuration for OpenAPI or AsyncAPI.
    /// </summary>
    /// <remarks>
    /// API specifications can be specified as simple URLs, multiple URLs, or detailed configurations
    /// with source and directory properties. This matches the official Mintlify schema.
    /// </remarks>
    public class ApiSpecConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the directory path for API specifications.
        /// </summary>
        /// <remarks>
        /// Specifies a directory containing API specification files.
        /// Used in conjunction with Source for complex configurations.
        /// </remarks>
        public string? Directory { get; set; }

        /// <summary>
        /// Gets or sets the source URL or path for API specifications.
        /// </summary>
        /// <remarks>
        /// Can be an absolute URL or relative path to an API specification file.
        /// </remarks>
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the list of URLs when multiple specifications are provided.
        /// </summary>
        /// <remarks>
        /// Used internally when the API config represents multiple URL strings.
        /// Not serialized directly as it's handled by the converter.
        /// </remarks>
        [JsonIgnore]
        public List<string>? Urls { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Implicitly converts a string URL to an ApiSpecConfig.
        /// </summary>
        /// <param name="url">The API specification URL.</param>
        /// <returns>An ApiSpecConfig with the specified source URL.</returns>
        public static implicit operator ApiSpecConfig?(string? url)
        {
            return url is null ? null : new ApiSpecConfig { Source = url };
        }

        /// <summary>
        /// Implicitly converts a list of URLs to an ApiSpecConfig.
        /// </summary>
        /// <param name="urls">The list of API specification URLs.</param>
        /// <returns>An ApiSpecConfig with the specified URLs.</returns>
        public static implicit operator ApiSpecConfig?(List<string>? urls)
        {
            return urls is null ? null : new ApiSpecConfig { Urls = urls };
        }

        /// <summary>
        /// Implicitly converts an ApiSpecConfig to a string.
        /// </summary>
        /// <param name="apiSpecConfig">The API specification configuration.</param>
        /// <returns>The source URL, first URL from the list, or null.</returns>
        public static implicit operator string?(ApiSpecConfig? apiSpecConfig)
        {
            return apiSpecConfig?.Source ?? apiSpecConfig?.Urls?[0];
        }

        /// <summary>
        /// Implicitly converts an ApiSpecConfig to a list of strings.
        /// </summary>
        /// <param name="apiSpecConfig">The API specification configuration.</param>
        /// <returns>The URLs list, or a single-item list with the source URL.</returns>
        public static implicit operator List<string>?(ApiSpecConfig? apiSpecConfig)
        {
            if (apiSpecConfig?.Urls is not null)
            {
                return apiSpecConfig.Urls;
            }
            return apiSpecConfig?.Source is not null ? [apiSpecConfig.Source] : null;
        }

        /// <summary>
        /// Returns the string representation of the API specification configuration.
        /// </summary>
        /// <returns>The source URL, first URL, or empty string.</returns>
        public override string ToString()
        {
            return Source ?? Urls?[0] ?? string.Empty;
        }

        #endregion

    }

}