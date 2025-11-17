namespace CloudNimble.DotNetDocs.Core.Configuration
{

    /// <summary>
    /// Specifies the type of documentation framework being used for the project.
    /// </summary>
    /// <remarks>
    /// This enum defines the supported documentation frameworks that DotNetDocs.Sdk can work with.
    /// Each type has specific file patterns, configuration files, and rendering behaviors defined in the SDK.
    /// The SDK automatically detects the documentation type based on the presence of framework-specific
    /// configuration files (e.g., docs.json for Mintlify, docfx.json for DocFX).
    /// </remarks>
    public enum SupportedDocumentationType
    {

        /// <summary>
        /// Mintlify documentation framework.
        /// </summary>
        /// <remarks>
        /// Mintlify is a modern documentation platform that uses MDX files and a docs.json configuration file.
        /// It provides features like interactive components, API reference generation, and customizable themes.
        /// Auto-detected by the presence of a docs.json file in the documentation root.
        /// </remarks>
        Mintlify,

        /// <summary>
        /// DocFX documentation framework.
        /// </summary>
        /// <remarks>
        /// DocFX is a documentation generation tool from Microsoft that works with .NET projects.
        /// It uses YAML table of contents (toc.yml) and a docfx.json configuration file.
        /// Auto-detected by the presence of a docfx.json file in the documentation root.
        /// </remarks>
        DocFX,

        /// <summary>
        /// MkDocs documentation framework.
        /// </summary>
        /// <remarks>
        /// MkDocs is a Python-based static site generator designed for project documentation.
        /// It uses Markdown files and a mkdocs.yml configuration file.
        /// Auto-detected by the presence of a mkdocs.yml file in the documentation root.
        /// </remarks>
        MkDocs,

        /// <summary>
        /// Jekyll static site generator.
        /// </summary>
        /// <remarks>
        /// Jekyll is a Ruby-based static site generator commonly used with GitHub Pages.
        /// It uses Markdown/HTML files and a _config.yml configuration file.
        /// Auto-detected by the presence of a _config.yml file in the documentation root.
        /// </remarks>
        Jekyll,

        /// <summary>
        /// Hugo static site generator.
        /// </summary>
        /// <remarks>
        /// Hugo is a Go-based static site generator known for its speed and flexibility.
        /// It uses Markdown files and a hugo.toml (or hugo.yaml/hugo.json) configuration file.
        /// Auto-detected by the presence of a hugo.toml file in the documentation root.
        /// </remarks>
        Hugo,

        /// <summary>
        /// Generic documentation type for frameworks not specifically supported.
        /// </summary>
        /// <remarks>
        /// This is the fallback type used when no specific documentation framework is detected.
        /// It provides basic support for common documentation file formats (Markdown, reStructuredText, etc.)
        /// without framework-specific features. Used when no framework-specific configuration file is found.
        /// </remarks>
        Generic

    }

}
