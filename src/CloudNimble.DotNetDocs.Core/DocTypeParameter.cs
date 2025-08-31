using System.Text.Json.Serialization;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Represents type parameter documentation extracted from XML documentation comments.
    /// </summary>
    public class DocTypeParameter
    {

        #region Properties

        /// <summary>
        /// Gets or sets the description of the type parameter.
        /// </summary>
        /// <value>The description text from the typeparam XML documentation.</value>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the type parameter name.
        /// </summary>
        /// <value>The name of the type parameter (e.g., "T", "TKey", "TValue").</value>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        #endregion

    }

}