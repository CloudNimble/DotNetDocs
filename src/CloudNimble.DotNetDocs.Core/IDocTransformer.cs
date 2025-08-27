using System.Threading.Tasks;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Defines a transformation step in the documentation rendering pipeline.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface modify the documentation model before rendering,
    /// applying customizations such as insertions, overrides, exclusions, and transformations.
    /// </remarks>
    public interface IDocTransformer
    {

        #region Public Methods

        /// <summary>
        /// Transforms a documentation entity.
        /// </summary>
        /// <param name="entity">The documentation entity to transform.</param>
        /// <param name="context">The project context providing transformation settings.</param>
        /// <returns>A task representing the asynchronous transformation operation.</returns>
        Task TransformAsync(DocEntity entity, ProjectContext context);

        #endregion

    }

}