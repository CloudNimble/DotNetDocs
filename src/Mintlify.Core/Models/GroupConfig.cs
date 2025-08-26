using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Mintlify.Core.Converters;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a group configuration in Mintlify navigation.
    /// </summary>
    /// <remarks>
    /// Groups organize pages into sections in your navigation. The group name is required.
    /// </remarks>
    public class GroupConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the AsyncAPI configuration for the group.
        /// </summary>
        [JsonPropertyName("asyncapi")]
        [JsonConverter(typeof(ApiConfigConverter))]
        public object? AsyncApi { get; set; }

        /// <summary>
        /// Gets or sets the name of the group.
        /// </summary>
        /// <remarks>
        /// This is a required field that appears as the group title in navigation.
        /// Group names cannot be null. While empty string names are technically accepted,
        /// they are not recommended as Mintlify treats each empty group as a separate
        /// ungrouped navigation section rather than merging them together.
        /// </remarks>
        [NotNull]
        public string Group { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the current option is default hidden.
        /// </summary>
        public bool? Hidden { get; set; }

        /// <summary>
        /// Gets or sets the icon to be displayed in the section.
        /// </summary>
        [JsonConverter(typeof(IconConverter))]
        public object? Icon { get; set; }

        /// <summary>
        /// Gets or sets the OpenAPI configuration for the group.
        /// </summary>
        [JsonPropertyName("openapi")]
        [JsonConverter(typeof(ApiConfigConverter))]
        public object? OpenApi { get; set; }

        /// <summary>
        /// Gets or sets the pages in the group.
        /// </summary>
        /// <remarks>
        /// Pages can be strings (page paths) or nested GroupConfig objects.
        /// </remarks>
        [JsonConverter(typeof(NavigationPageListConverter))]
        public List<object>? Pages { get; set; }

        /// <summary>
        /// Gets or sets the root page for the group.
        /// </summary>
        public string? Root { get; set; }

        /// <summary>
        /// Gets or sets the tag for the group.
        /// </summary>
        public string? Tag { get; set; }

        #endregion

    }

}
