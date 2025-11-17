namespace CloudNimble.DotNetDocs.Tests.Shared.Extensions
{

    /// <summary>
    /// Extension methods for string using the traditional separate namespace pattern.
    /// </summary>
    /// <remarks>
    /// This class demonstrates the traditional extension method pattern where extensions
    /// are placed in a separate namespace (e.g., MyProject.Extensions). Users must add
    /// a using directive to access these extensions.
    /// </remarks>
    public static class StringExtensions
    {

        #region Public Methods

        /// <summary>
        /// Reverses the characters in a string.
        /// </summary>
        /// <param name="value">The string to reverse.</param>
        /// <returns>The reversed string.</returns>
        /// <example>
        /// <code>
        /// using CloudNimble.DotNetDocs.Tests.Shared.Extensions;
        ///
        /// var result = "hello".Reverse();
        /// // result = "olleh"
        /// </code>
        /// </example>
        public static string Reverse(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var chars = value.ToCharArray();
            System.Array.Reverse(chars);
            return new string(chars);
        }

        /// <summary>
        /// Repeats a string a specified number of times.
        /// </summary>
        /// <param name="value">The string to repeat.</param>
        /// <param name="count">The number of times to repeat the string.</param>
        /// <returns>The repeated string.</returns>
        /// <example>
        /// <code>
        /// var result = "ha".Repeat(3);
        /// // result = "hahaha"
        /// </code>
        /// </example>
        public static string Repeat(this string value, int count)
        {
            if (string.IsNullOrEmpty(value) || count <= 0)
            {
                return string.Empty;
            }

            return string.Concat(System.Linq.Enumerable.Repeat(value, count));
        }

        #endregion

    }

}
