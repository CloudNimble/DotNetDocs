using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents an external or outbound link in a Mintlify navigation pages array.
    /// </summary>
    /// <remarks>
    /// Serialized as <c>{ "type": "link", "href": "...", "title": "..." }</c>.
    /// See https://www.mintlify.com/docs/organize/navigation.
    /// </remarks>
    public class NavigationLinkConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the destination URL.
        /// </summary>
        public string? Href { get; set; }

        /// <summary>
        /// Gets or sets the link label.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the discriminator. Always <c>link</c>.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "link";

        #endregion

    }

}
