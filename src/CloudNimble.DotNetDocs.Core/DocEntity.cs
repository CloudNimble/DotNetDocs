using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using YamlDotNet.Serialization;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Base class for documentation entities, providing common contextual metadata.
    /// </summary>
    /// <remarks>
    /// Represents shared documentation properties for assemblies, namespaces, types, members, or parameters.
    /// Now supports both XML documentation extraction and conceptual content loading with proper separation.
    /// </remarks>
    /// <example>
    /// <code><![CDATA[
    /// var docType = new DocType(symbol)
    /// {
    ///     Summary = "Represents a logging service.",
    ///     Usage = "Use this class for application logging.",
    ///     Examples = "```csharp\nLogger.LogInfo(\"Message\");\n```"
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

        /// <summary>
        /// Backing field for the original symbol reference.
        /// </summary>
        private ISymbol? _originalSymbol;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the best practices documentation content.
        /// </summary>
        /// <value>Markdown content with best practices, recommendations, and guidelines from conceptual documentation.</value>
        public string? BestPractices { get; set; }

        /// <summary>
        /// Gets or sets the display name of the entity.
        /// </summary>
        /// <value>The fully qualified display name extracted from the Symbol.</value>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the considerations or notes related to the current context.
        /// </summary>
        /// <value>Markdown content with gotchas, performance, or security notes from conceptual documentation.</value>
        public string? Considerations { get; set; }

        /// <summary>
        /// Gets or sets the collection of exceptions that can be thrown.
        /// </summary>
        /// <value>Collection of exception documentation from XML &lt;exception&gt; tags.</value>
        public ICollection<DocException>? Exceptions { get; set; }

        /// <summary>
        /// Gets or sets the examples documentation content.
        /// </summary>
        /// <value>Markdown content containing code examples from XML &lt;example&gt; tags.</value>
        public string? Examples { get; set; }

        /// <summary>
        /// Gets or sets the list of member accessibilities to include (default: Public).
        /// </summary>
        /// <value>List of accessibility levels to include when processing child members.</value>
        [NotNull]
        [JsonIgnore]
        [YamlIgnore]
        public List<Accessibility> IncludedMembers { get; set; } = [Accessibility.Public];

        /// <summary>
        /// Gets the original symbol this documentation entity was created from.
        /// </summary>
        /// <value>The Roslyn ISymbol that was used to create this entity, preserved for reference.</value>
        [JsonIgnore]
        [YamlIgnore]
        public ISymbol? OriginalSymbol
        {
            get => _originalSymbol;
            protected set => _originalSymbol = value;
        }

        /// <summary>
        /// Gets or sets the patterns documentation content.
        /// </summary>
        /// <value>Markdown content explaining common usage patterns and architectural guidance from conceptual documentation.</value>
        public string? Patterns { get; set; }

        /// <summary>
        /// Gets or sets a list of related API names.
        /// </summary>
        /// <value>List of fully qualified names or URLs for related APIs from conceptual documentation.</value>
        public ICollection<string>? RelatedApis { get; set; }

        /// <summary>
        /// Gets or sets the remarks from XML documentation.
        /// </summary>
        /// <value>Content from the XML documentation's &lt;remarks&gt; element.</value>
        public string? Remarks { get; set; }

        /// <summary>
        /// Gets or sets the return value documentation.
        /// </summary>
        /// <value>Description of the return value from XML &lt;returns&gt; tag.</value>
        public string? Returns { get; set; }

        /// <summary>
        /// Gets or sets the collection of see-also references.
        /// </summary>
        /// <value>Collection of related items from XML &lt;seealso&gt; tags.</value>
        public ICollection<string>? SeeAlso { get; set; }

        /// <summary>
        /// Gets or sets the summary from XML documentation.
        /// </summary>
        /// <value>Brief description of what the API element IS, from XML &lt;summary&gt; tag.</value>
        public string? Summary { get; set; }

        /// <summary>
        /// Gets or sets the collection of type parameters.
        /// </summary>
        /// <value>Collection of type parameter documentation from XML &lt;typeparam&gt; tags.</value>
        public ICollection<DocTypeParameter>? TypeParameters { get; set; }

        /// <summary>
        /// Gets or sets the usage documentation content.
        /// </summary>
        /// <value>Markdown content explaining HOW to use the API element, from conceptual documentation.</value>
        public string? Usage { get; set; }

        /// <summary>
        /// Gets or sets the value description for properties.
        /// </summary>
        /// <value>Description of what the property represents from XML &lt;value&gt; tag.</value>
        public string? Value { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DocEntity"/> class.
        /// </summary>
        protected DocEntity()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocEntity"/> class with an ISymbol.
        /// </summary>
        /// <param name="symbol">The symbol to store as the original reference.</param>
        protected DocEntity(ISymbol? symbol)
        {
            OriginalSymbol = symbol;
        }

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