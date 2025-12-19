namespace CloudNimble.DotNetDocs.Core.Configuration
{

    /// <summary>
    /// Defines constants for documentation renderer types and provides mapping functionality
    /// between documentation framework types and their corresponding renderers.
    /// </summary>
    /// <remarks>
    /// This class provides a centralized way to manage renderer type strings used throughout
    /// the documentation generation pipeline. It also provides intelligent mapping from
    /// source documentation framework types to appropriate output renderers.
    /// </remarks>
    public static class RendererType
    {

        #region Fields

        /// <summary>
        /// Mintlify MDX renderer - generates documentation in Mintlify's MDX format.
        /// </summary>
        /// <remarks>
        /// This is the default renderer for Mintlify-based documentation projects.
        /// Produces .mdx files with Mintlify-specific components and a docs.json configuration.
        /// </remarks>
        public const string Mintlify = "Mintlify";

        /// <summary>
        /// Markdown renderer - generates standard Markdown documentation.
        /// </summary>
        /// <remarks>
        /// Produces standard .md files compatible with most documentation frameworks.
        /// This is a universal format suitable for GitHub, GitLab, and other Markdown-based systems.
        /// </remarks>
        public const string Markdown = "Markdown";

        /// <summary>
        /// JSON renderer - generates documentation in JSON format.
        /// </summary>
        /// <remarks>
        /// Produces structured JSON files containing documentation data.
        /// Useful for custom integrations, APIs, or when documentation needs to be consumed programmatically.
        /// </remarks>
        public const string Json = "Json";

        /// <summary>
        /// YAML renderer - generates documentation in YAML format.
        /// </summary>
        /// <remarks>
        /// Produces YAML files containing documentation data.
        /// Commonly used with static site generators and CI/CD pipelines.
        /// </remarks>
        public const string Yaml = "Yaml";

        #endregion

        #region Public Methods

        /// <summary>
        /// Maps a <see cref="SupportedDocumentationType"/> to its corresponding renderer type constant.
        /// </summary>
        /// <param name="documentationType">The source documentation framework type.</param>
        /// <returns>
        /// The renderer type constant that should be used for the given documentation type.
        /// Returns <see cref="Mintlify"/> for Mintlify projects, and <see cref="Markdown"/> for all other types.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The mapping logic is as follows:
        /// </para>
        /// <list type="bullet">
        /// <item><description><see cref="SupportedDocumentationType.Mintlify"/> → <see cref="Mintlify"/> renderer</description></item>
        /// <item><description><see cref="SupportedDocumentationType.DocFX"/> → <see cref="Markdown"/> renderer</description></item>
        /// <item><description><see cref="SupportedDocumentationType.MkDocs"/> → <see cref="Markdown"/> renderer</description></item>
        /// <item><description><see cref="SupportedDocumentationType.Jekyll"/> → <see cref="Markdown"/> renderer</description></item>
        /// <item><description><see cref="SupportedDocumentationType.Hugo"/> → <see cref="Markdown"/> renderer</description></item>
        /// <item><description><see cref="SupportedDocumentationType.Generic"/> → <see cref="Markdown"/> renderer</description></item>
        /// </list>
        /// <para>
        /// This mapping ensures that documentation from various frameworks is rendered in a compatible format.
        /// Most frameworks work well with standard Markdown, while Mintlify requires its specific MDX format.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var docType = SupportedDocumentationType.Mintlify;
        /// var renderer = RendererType.GetRendererType(docType);
        /// // renderer == "Mintlify"
        ///
        /// var docType2 = SupportedDocumentationType.DocFX;
        /// var renderer2 = RendererType.GetRendererType(docType2);
        /// // renderer2 == "Markdown"
        /// </code>
        /// </example>
        public static string GetRendererType(SupportedDocumentationType documentationType)
        {
            return documentationType switch
            {
                SupportedDocumentationType.Mintlify => Mintlify,
                SupportedDocumentationType.DocFX => Markdown,
                SupportedDocumentationType.MkDocs => Markdown,
                SupportedDocumentationType.Jekyll => Markdown,
                SupportedDocumentationType.Hugo => Markdown,
                SupportedDocumentationType.Generic => Markdown,
                _ => Markdown
            };
        }

        #endregion

    }

}
