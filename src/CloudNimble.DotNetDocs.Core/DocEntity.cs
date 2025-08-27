using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Base class for documentation entities, providing common contextual metadata.
    /// </summary>
    /// <remarks>
    /// Represents shared documentation properties for assemblies, namespaces, types, members, or parameters.
    /// Use to store conceptual content not directly extractable from Roslyn or XML comments.
    /// </remarks>
    /// <example>
    /// <code><![CDATA[
    /// var docType = new DocType(symbol)
    /// {
    ///     Usage = "Use this class for logging.",
    ///     Examples = "```csharp\nLogger.LogInfo();\n```"
    /// };
    /// ]]></code>
    /// </example>
    public abstract class DocEntity
    {

        #region Fields

        /// <summary>
        /// Gets the JSON serializer options for consistent serialization.
        /// </summary>
        public static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            MaxDepth = 64,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the best practices documentation content.
        /// </summary>
        /// <value>Markdown content with best practices, recommendations, and guidelines.</value>
        [NotNull]
        public string BestPractices { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the considerations or notes related to the current context.
        /// </summary>
        /// <value>Markdown content with gotchas, performance, or security notes.</value>
        [NotNull]
        public string Considerations { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the examples documentation content.
        /// </summary>
        /// <value>Markdown content containing code examples and demonstrations.</value>
        [NotNull]
        public string Examples { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the patterns documentation content.
        /// </summary>
        /// <value>Markdown content explaining common usage patterns and architectural guidance.</value>
        [NotNull]
        public string Patterns { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a list of related API names.
        /// </summary>
        /// <value>List of fully qualified names or URLs for related APIs.</value>
        [NotNull]
        public List<string> RelatedApis { get; set; } = [];

        /// <summary>
        /// Gets or sets the usage documentation content.
        /// </summary>
        /// <value>Markdown content explaining how to use the API element.</value>
        [NotNull]
        public string Usage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of member accessibilities to include (default: Public).
        /// </summary>
        /// <value>List of accessibility levels to include when processing child members.</value>
        [NotNull]
        public List<Accessibility> IncludedMembers { get; set; } = [Accessibility.Public];

        #endregion

        #region Public Methods

        /// <summary>
        /// Serializes this entity to JSON using consistent options.
        /// </summary>
        /// <returns>The JSON string representation of this entity.</returns>
        public string ToJson() => JsonSerializer.Serialize(this, GetType(), JsonSerializerOptions);

        #endregion

    }

}