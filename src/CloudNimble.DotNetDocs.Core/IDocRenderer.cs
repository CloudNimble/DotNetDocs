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
        /// <param name="model">The documentation assembly to render.</param>
        /// <returns>A task representing the asynchronous rendering operation.</returns>
        Task RenderAsync(DocAssembly model);

        #endregion

    }

}