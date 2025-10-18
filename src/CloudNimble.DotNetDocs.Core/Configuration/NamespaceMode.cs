namespace CloudNimble.DotNetDocs.Core.Configuration
{

    /// <summary>
    /// Specifies how namespaces should be organized in the output file structure.
    /// </summary>
    public enum NamespaceMode
    {

        /// <summary>
        /// Each namespace is rendered as a single file in the root output directory.
        /// </summary>
        /// <remarks>
        /// When using File mode, namespace names in filenames are separated by the configured separator character.
        /// For example, "System.Collections.Generic" becomes "System-Collections-Generic.md" with a '-' separator.
        /// </remarks>
        File,

        /// <summary>
        /// Each namespace is rendered in its own folder hierarchy matching the namespace structure.
        /// </summary>
        /// <remarks>
        /// When using Folder mode, the namespace separator setting is ignored and the namespace hierarchy
        /// is preserved as a folder structure. For example, "System.Collections.Generic" would create
        /// the folder structure "System/Collections/Generic/" with documentation files inside.
        /// </remarks>
        Folder

    }

}