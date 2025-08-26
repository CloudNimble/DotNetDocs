using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the search functionality configuration for the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration controls the appearance and behavior of the search feature,
    /// including placeholder text and search display settings.
    /// </remarks>
    public class SearchConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the placeholder text displayed in the search input field.
        /// </summary>
        /// <remarks>
        /// This text appears in the search bar when it's empty, providing guidance
        /// to users about what they can search for. Should be concise and helpful.
        /// </remarks>
        public string? Prompt { get; set; }

        #endregion

    }

}