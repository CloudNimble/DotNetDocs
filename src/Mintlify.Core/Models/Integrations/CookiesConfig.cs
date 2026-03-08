namespace Mintlify.Core.Models.Integrations
{

    /// <summary>
    /// Represents the configuration for cookie settings.
    /// </summary>
    /// <remarks>
    /// This configuration defines a cookie that can be set on documentation pages,
    /// allowing for custom cookie-based functionality such as user preferences
    /// or integration with external services.
    /// </remarks>
    public class CookiesConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the cookie key.
        /// </summary>
        /// <remarks>
        /// Specifies the name of the cookie. This key is used to identify the cookie
        /// in the browser and should be a unique, descriptive string following standard
        /// cookie naming conventions.
        /// </remarks>
        public string? Key { get; set; }

        /// <summary>
        /// Gets or sets the cookie value.
        /// </summary>
        /// <remarks>
        /// Specifies the value to be stored in the cookie. The value should be
        /// appropriately encoded and should not contain sensitive information
        /// as cookies are stored in the user's browser.
        /// </remarks>
        public string? Value { get; set; }

        #endregion

    }

}
