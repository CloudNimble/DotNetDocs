using System.Collections.Generic;
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
        /// Combines navigation from referenced documentation projects into the collection's navigation structure.
        /// </summary>
        /// <param name="references">The list of documentation references to integrate.</param>
        /// <returns>A task representing the asynchronous navigation combining operation.</returns>
        /// <remarks>
        /// This method is called after file copying to integrate navigation structures from referenced
        /// documentation projects. The default implementation is a no-op; renderers that support
        /// navigation combining (e.g., Mintlify) should override this method.
        /// </remarks>
        Task CombineReferencedNavigationAsync(List<DocumentationReference> references);

        /// <summary>
        /// Renders the documentation assembly to the output path specified in the project context.
        /// </summary>
        /// <param name="model">The documentation assembly to render.</param>
        /// <returns>A task representing the asynchronous rendering operation.</returns>
        Task RenderAsync(DocAssembly model);

        /// <summary>
        /// Renders placeholder conceptual content files for the documentation assembly.
        /// </summary>
        /// <param name="model">The documentation assembly to generate placeholders for.</param>
        /// <returns>A task representing the asynchronous placeholder rendering operation.</returns>
        Task RenderPlaceholdersAsync(DocAssembly model);

        #endregion

    }

}