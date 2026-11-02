namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a git-source navigation section inlined into a pages array.
    /// </summary>
    /// <remarks>
    /// Serialized as <c>{ "sourceRef": "..." }</c>. The value is the mount path of the git source.
    /// </remarks>
    public class SourceRefConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the git-source mount path to inline.
        /// </summary>
        public string? SourceRef { get; set; }

        #endregion

    }

}
