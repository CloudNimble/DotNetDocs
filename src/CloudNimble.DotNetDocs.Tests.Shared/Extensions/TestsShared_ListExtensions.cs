namespace System.Collections.Generic
{

    /// <summary>
    /// Extension methods for List&lt;T&gt; using the same-namespace discoverable pattern.
    /// </summary>
    /// <remarks>
    /// This class demonstrates extending an external type (List&lt;T&gt; from System.Collections.Generic)
    /// using the discoverable pattern. By placing extensions in the same namespace as List&lt;T&gt;,
    /// they are automatically available wherever List&lt;T&gt; is used.
    ///
    /// This pattern is particularly useful for framework types where users already have
    /// the namespace imported.
    /// </remarks>
    public static class TestsShared_ListExtensions
    {

        #region Public Methods

        /// <summary>
        /// Checks if a list is null or empty.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to check.</param>
        /// <returns>True if the list is null or empty, otherwise false.</returns>
        /// <example>
        /// <code>
        /// var numbers = new List&lt;int&gt;();
        /// if (numbers.IsNullOrEmpty())
        /// {
        ///     // Handle empty list
        /// }
        /// </code>
        /// </example>
        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            return list is null || list.Count == 0;
        }

        /// <summary>
        /// Adds multiple items to a list in one call.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to add items to.</param>
        /// <param name="items">The items to add.</param>
        /// <returns>The list for fluent chaining.</returns>
        /// <example>
        /// <code>
        /// var numbers = new List&lt;int&gt;()
        ///     .AddMultiple(1, 2, 3, 4, 5);
        /// </code>
        /// </example>
        public static List<T> AddMultiple<T>(this List<T> list, params T[] items)
        {
            if (list is not null && items is not null)
            {
                list.AddRange(items);
            }
            return list!;
        }

        /// <summary>
        /// Shuffles the elements in a list randomly.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to shuffle.</param>
        /// <returns>The shuffled list.</returns>
        /// <remarks>
        /// This uses the Fisher-Yates shuffle algorithm for randomization.
        /// </remarks>
        public static List<T> Shuffle<T>(this List<T> list)
        {
            if (list is null || list.Count <= 1)
            {
                return list!;
            }

            var random = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
            return list;
        }

        #endregion

    }

}
