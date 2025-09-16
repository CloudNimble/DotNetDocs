namespace CloudNimble.DotNetDocs.Tests.Shared.Enums
{

    /// <summary>
    /// An enum with byte underlying type.
    /// </summary>
    /// <remarks>
    /// This enum uses a byte as its underlying type to save memory.
    /// Values are limited to 0-255.
    /// </remarks>
    public enum ByteEnum : byte
    {

        /// <summary>
        /// Minimum value.
        /// </summary>
        Min = 0,

        /// <summary>
        /// Low value.
        /// </summary>
        Low = 50,

        /// <summary>
        /// Medium value.
        /// </summary>
        Medium = 100,

        /// <summary>
        /// High value.
        /// </summary>
        High = 200,

        /// <summary>
        /// Maximum value for byte.
        /// </summary>
        Max = 255

    }

}