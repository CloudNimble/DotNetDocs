using CloudNimble.DotNetDocs.Core.Configuration;
using System.Threading.Tasks;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Defines a handler for processing documentation references from other projects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations of this interface are responsible for copying files from referenced documentation
    /// projects, rewriting internal paths in content files, and relocating resources to appropriate
    /// locations in the collection project.
    /// </para>
    /// <para>
    /// This interface follows the same pattern as <see cref="IDocEnricher"/>, <see cref="IDocTransformer"/>,
    /// and <see cref="IDocRenderer"/> for format-specific documentation processing.
    /// </para>
    /// </remarks>
    public interface IDocReferenceHandler
    {

        #region Properties

        /// <summary>
        /// Gets the documentation type this handler supports.
        /// </summary>
        /// <value>
        /// The <see cref="SupportedDocumentationType"/> that this handler can process.
        /// </value>
        SupportedDocumentationType DocumentationType { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Processes a documentation reference by copying files, rewriting content paths,
        /// and relocating resources as appropriate for this documentation format.
        /// </summary>
        /// <param name="reference">The documentation reference to process.</param>
        /// <param name="documentationRootPath">The root path of the collection documentation output.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        /// <remarks>
        /// <para>
        /// Implementations should handle the following responsibilities:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Copy content files from the source documentation root to the destination path.</description></item>
        /// <item><description>Rewrite internal absolute paths in content files to use the destination path prefix.</description></item>
        /// <item><description>Relocate resources (images, snippets, etc.) to central locations with proper namespacing.</description></item>
        /// </list>
        /// </remarks>
        Task ProcessAsync(DocumentationReference reference, string documentationRootPath);

        #endregion

    }

}
