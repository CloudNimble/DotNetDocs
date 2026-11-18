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

    }

}
