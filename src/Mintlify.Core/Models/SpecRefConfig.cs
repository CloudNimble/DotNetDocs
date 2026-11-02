namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a GraphQL or MCP specification reference.
    /// </summary>
    /// <remarks>
    /// Mintlify accepts either a string path or HTTPS URL, or an object with
    /// <see cref="Source"/> and optional <see cref="Directory"/>. HTTP URLs are not accepted.
    /// GraphQL defaults <see cref="Directory"/> to <c>graphql-reference</c>.
    /// See https://www.mintlify.com/docs/api-playground/graphql-setup.
    /// </remarks>
    public class SpecRefConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the directory used as the URL prefix for generated pages.
        /// </summary>
        public string? Directory { get; set; }

        /// <summary>
        /// Gets or sets the local path or HTTPS URL of the specification.
        /// </summary>
        public string? Source { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Implicitly converts a string path or URL to a <see cref="SpecRefConfig"/>.
        /// </summary>
        /// <param name="source">The specification path or URL.</param>
        /// <returns>A spec reference with the specified source, or null.</returns>
        public static implicit operator SpecRefConfig?(string? source)
        {
            return source is null ? null : new SpecRefConfig { Source = source };
        }

        /// <summary>
        /// Implicitly converts a <see cref="SpecRefConfig"/> to a string.
        /// </summary>
        /// <param name="specRef">The specification reference.</param>
        /// <returns>The source path, or null.</returns>
        public static implicit operator string?(SpecRefConfig? specRef)
        {
            return specRef?.Source;
        }

        /// <summary>
        /// Returns the source path or an empty string.
        /// </summary>
        /// <returns>The source path or an empty string.</returns>
        public override string ToString()
        {
            return Source ?? string.Empty;
        }

        #endregion

    }

}
