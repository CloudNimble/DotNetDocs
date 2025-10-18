namespace CloudNimble.DotNetDocs.Tests.Shared.Enums
{

    /// <summary>
    /// An enum with long underlying type for large values.
    /// </summary>
    public enum LongEnum : long
    {

        /// <summary>
        /// Small value.
        /// </summary>
        Small = 100,

        /// <summary>
        /// Large value in millions.
        /// </summary>
        Million = 1_000_000,

        /// <summary>
        /// Large value in billions.
        /// </summary>
        Billion = 1_000_000_000,

        /// <summary>
        /// Very large value.
        /// </summary>
        VeryLarge = 9_223_372_036_854_775_807

    }

}