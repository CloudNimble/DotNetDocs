using System.Collections.Generic;
using System.Text.Json.Serialization;
using Mintlify.Core.Converters;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents settings for the Markdown that Mintlify serves to AI tools and agents.
    /// </summary>
    /// <remarks>
    /// These settings control Markdown export used by <c>.md</c> URLs, <c>llms.txt</c>,
    /// and AI agents. See https://www.mintlify.com/docs/ai/markdown-export.
    /// </remarks>
    public class MarkdownConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets custom agent instructions appended to generated Markdown.
        /// </summary>
        /// <example>
        /// <code>
        /// "markdown": {
        ///   "instructions": "Always cite the API version. Prefer the TypeScript SDK in examples."
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// Provide a single string or an array of strings. Mintlify joins array items with
        /// line breaks and renders them as an <c>Agent Instructions</c> block on every page,
        /// as well as in <c>llms.txt</c> and <c>llms-full.txt</c>.
        /// </remarks>
        [JsonConverter(typeof(MarkdownInstructionsConverter))]
        public List<string>? Instructions { get; set; }

        /// <summary>
        /// Gets or sets whether to include the full OpenAPI or AsyncAPI specification in
        /// Markdown exports of API reference pages.
        /// </summary>
        /// <example>
        /// <code>
        /// "markdown": {
        ///   "schema": false
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// Defaults to true. Set to false to omit the specification from Markdown output.
        /// </remarks>
        public bool? Schema { get; set; }

        #endregion

    }

}
