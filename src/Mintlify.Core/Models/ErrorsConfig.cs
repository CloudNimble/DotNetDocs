using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the error pages configuration for the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration controls how various error conditions are handled,
    /// including 404 (Not Found) errors and their behavior.
    /// </remarks>
    public class ErrorsConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the configuration for 404 (Not Found) error handling.
        /// </summary>
        /// <remarks>
        /// Defines how the site behaves when users attempt to access pages that don't exist.
        /// This includes options for automatic redirection and custom error page behavior.
        /// </remarks>
        [JsonPropertyName("404")]
        public Error404Config? NotFound { get; set; }

        #endregion

    }

}