using System.Threading.Tasks;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Defines a renderer for documentation output generation.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface generate specific output formats (Markdown, JSON, YAML, etc.)
    /// from the documentation model after all transformations have been applied.
    /// </remarks>
    public interface IDocRenderer
    {

        #region Public Methods

        /// <summary>
        /// Renders the documentation assembly to the output path specified in the project context.
        /// </summary>
        /// <param name="model">The documentation assembly to render, or null for documentation-only mode.</param>
        /// <returns>A task representing the asynchronous rendering operation.</returns>
        /// <remarks>
        /// <para>
        /// When model is null, the renderer should skip API reference generation but still process
        /// any other operations like navigation file merging (e.g., combining DocumentationReferences).
        /// </para>
        /// <para>
        /// Renderers that support navigation combining (e.g., Mintlify) should access
        /// Context.DocumentationReferences and combine navigation before saving their configuration.
        /// </para>
        /// </remarks>
        Task RenderAsync(DocAssembly? model);

        /// <summary>
        /// Adds this renderer's own files to the conceptual content folder.
        /// </summary>
        /// <param name="model">The documentation model that placeholders are being generated for.</param>
        /// <returns>A task representing the asynchronous placeholder rendering operation.</returns>
        /// <remarks>
        /// <see cref="DocumentationManager.GeneratePlaceholdersAsync(DocAssembly)"/> writes the standard namespace and type
        /// placeholders itself, then calls this method on each registered renderer, one at a time. Use it only for files the
        /// standard placeholders don't cover; <see cref="Renderers.RendererBase"/> provides an implementation that does nothing.
        /// </remarks>
        Task RenderPlaceholdersAsync(DocAssembly model);

        #endregion

    }

}