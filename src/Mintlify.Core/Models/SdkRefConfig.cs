namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents an SDK documentation artifact used to auto-populate reference pages.
    /// </summary>
    /// <example>
    /// <code>
    /// "tab": "TypeScript SDK",
    /// "sdk": {
    ///   "format": "typedoc",
    ///   "source": "sdk-artifacts/typedoc.json",
    ///   "directory": "sdk/typescript"
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Declare <c>sdk</c> on a tab or a group. Mintlify reads the artifact and generates
    /// navigation groups and pages for every class, interface, module, and function.
    /// <c>format</c> is <c>typedoc</c>, <c>docfx</c>, <c>javadoc</c>, <c>sphinx</c>,
    /// <c>phpdoc</c>, or <c>go</c>. <see cref="Source"/> is a relative path or HTTPS URL
    /// (HTTP is not accepted). <see cref="Directory"/> defaults to <c>sdk-reference</c>.
    /// Nested groups inherit the parent <c>sdk</c> unless they set their own.
    /// See https://www.mintlify.com/docs/api-playground/sdk-reference-setup.
    /// </remarks>
    public class SdkRefConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the URL path prefix for generated pages.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>sdk-reference</c>. Use a unique prefix for each library to
        /// avoid route collisions.
        /// </remarks>
        public string? Directory { get; set; }

        /// <summary>
        /// Gets or sets the documentation generator that produced the artifact.
        /// </summary>
        /// <remarks>
        /// Valid values are <c>typedoc</c>, <c>docfx</c>, <c>javadoc</c>, <c>sphinx</c>,
        /// <c>phpdoc</c>, and <c>go</c>.
        /// </remarks>
        public string? Format { get; set; }

        /// <summary>
        /// Gets or sets the relative path or HTTPS URL of the generator artifact.
        /// </summary>
        /// <remarks>
        /// Directory-shaped formats such as DocFX, Javadoc, and Sphinx accept a zip
        /// (or a Javadoc jar) when the source is remote.
        /// </remarks>
        public string? Source { get; set; }

        #endregion

    }

}
