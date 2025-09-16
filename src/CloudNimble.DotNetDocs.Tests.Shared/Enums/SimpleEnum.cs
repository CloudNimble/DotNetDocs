namespace CloudNimble.DotNetDocs.Tests.Shared.Enums
{

    /// <summary>
    /// A simple enum with default int underlying type.
    /// </summary>
    public enum SimpleEnum
    {

        /// <summary>
        /// No value specified.
        /// </summary>
        None,

        /// <summary>
        /// First option.
        /// </summary>
        First,

        /// <summary>
        /// Second option.
        /// </summary>
        Second,

        /// <summary>
        /// Third option with explicit value.
        /// </summary>
        Third = 10,

        /// <summary>
        /// Fourth option continues from Third.
        /// </summary>
        Fourth

    }

}