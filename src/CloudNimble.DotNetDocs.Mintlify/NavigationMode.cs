namespace CloudNimble.DotNetDocs.Mintlify
{

    /// <summary>
    /// Specifies how navigation should be organized when generating documentation from multiple assemblies.
    /// </summary>
    public enum NavigationMode
    {

        /// <summary>
        /// All assemblies are merged into a single unified navigation structure.
        /// This is the default mode for backward compatibility.
        /// </summary>
        Unified = 0,

        /// <summary>
        /// Each assembly gets its own top-level group in the navigation.
        /// Useful for large solutions with distinct assembly boundaries.
        /// </summary>
        ByAssembly = 1

    }

}