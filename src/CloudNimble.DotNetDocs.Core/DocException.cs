using System.Text.Json.Serialization;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Represents exception documentation extracted from XML documentation comments.
    /// </summary>
    public class DocException
    {

        #region Properties

        /// <summary>
        /// Gets or sets the description of when the exception is thrown.
        /// </summary>
        /// <value>The description text from the exception XML documentation.</value>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the exception type name.
        /// </summary>
        /// <value>The fully qualified or simple name of the exception type (e.g., "ArgumentNullException").</value>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        #endregion

    }

}