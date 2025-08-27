using System.Threading.Tasks;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Defines an enricher for conceptual documentation augmentation.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface can enhance documentation entities with additional content
    /// from various sources such as conceptual files, AI services, or other data sources.
    /// </remarks>
    public interface IDocEnricher
    {

        #region Public Methods

        /// <summary>
        /// Enriches a documentation entity with additional conceptual content.
        /// </summary>
        /// <param name="entity">The documentation entity to enrich.</param>
        /// <param name="context">The project context providing configuration and paths for enrichment.</param>
        /// <returns>A task representing the asynchronous enrichment operation.</returns>
        Task EnrichAsync(DocEntity entity, ProjectContext context);

        #endregion

    }

}