using System.Threading.Tasks;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Defines a transformation step in the documentation processing pipeline.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface modify the documentation model before rendering,
    /// applying customizations such as overrides and transformations.
    /// </remarks>
    public interface IDocTransformer
    {

        #region Public Methods

        /// <summary>
        /// Transforms a documentation entity.
        /// </summary>
        /// <param name="entity">The documentation entity to transform.</param>
        /// <returns>A task representing the asynchronous transformation operation.</returns>
        Task TransformAsync(DocEntity entity);

        #endregion

    }

}