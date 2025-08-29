using System;
using System.Collections.Generic;

namespace CloudNimble.DotNetDocs.Tests.Shared.Parameters
{

    /// <summary>
    /// A class demonstrating various parameter types and patterns.
    /// </summary>
    /// <remarks>
    /// This class contains methods with different parameter modifiers and types.
    /// </remarks>
    public class ParameterVariations
    {

        #region Public Methods

        /// <summary>
        /// A method with an optional parameter.
        /// </summary>
        /// <param name="required">The required string parameter.</param>
        /// <param name="optional">The optional integer parameter with a default value.</param>
        /// <returns>A formatted string combining both parameters.</returns>
        /// <example>
        /// <code>
        /// var result1 = MethodWithOptionalParam("test");      // Uses default value 42
        /// var result2 = MethodWithOptionalParam("test", 100); // Uses provided value
        /// </code>
        /// </example>
        public string MethodWithOptionalParam(string required, int optional = 42)
        {
            return $"{required}: {optional}";
        }

        /// <summary>
        /// A method with a params array.
        /// </summary>
        /// <param name="values">Variable number of integer values.</param>
        /// <returns>The sum of all provided values.</returns>
        /// <example>
        /// <code>
        /// var sum1 = MethodWithParams(1, 2, 3);        // Returns 6
        /// var sum2 = MethodWithParams(new[] { 1, 2 }); // Returns 3
        /// </code>
        /// </example>
        public int MethodWithParams(params int[] values)
        {
            int sum = 0;
            foreach (var value in values)
            {
                sum += value;
            }
            return sum;
        }

        /// <summary>
        /// A method with a ref parameter.
        /// </summary>
        /// <param name="value">The value to be modified by reference.</param>
        /// <remarks>
        /// This method doubles the input value.
        /// </remarks>
        public void MethodWithRef(ref int value)
        {
            value *= 2;
        }

        /// <summary>
        /// A method with an out parameter.
        /// </summary>
        /// <param name="input">The input string to parse.</param>
        /// <param name="value">The output integer value if parsing succeeds.</param>
        /// <returns>true if the parsing was successful; otherwise, false.</returns>
        public bool MethodWithOut(string input, out int value)
        {
            return int.TryParse(input, out value);
        }

        /// <summary>
        /// A generic method with a type parameter.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The value to process.</param>
        /// <returns>The string representation of the value.</returns>
        /// <example>
        /// <code>
        /// var result1 = GenericMethod&lt;int&gt;(42);
        /// var result2 = GenericMethod("hello");
        /// </code>
        /// </example>
        public string GenericMethod<T>(T value)
        {
            return value?.ToString() ?? "null";
        }

        /// <summary>
        /// A method with multiple generic type parameters.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>A key-value pair.</returns>
        public KeyValuePair<TKey, TValue> GenericMethodWithMultipleTypes<TKey, TValue>(TKey key, TValue value)
        {
            return new KeyValuePair<TKey, TValue>(key, value);
        }

        /// <summary>
        /// A method with nullable parameters.
        /// </summary>
        /// <param name="nullableInt">An optional nullable integer.</param>
        /// <param name="nullableString">An optional nullable string.</param>
        /// <returns>A description of the provided values.</returns>
        public string MethodWithNullables(int? nullableInt, string? nullableString)
        {
            var intPart = nullableInt.HasValue ? nullableInt.Value.ToString() : "null";
            var stringPart = nullableString ?? "null";
            return $"Int: {intPart}, String: {stringPart}";
        }

        /// <summary>
        /// A method demonstrating parameter constraints.
        /// </summary>
        /// <typeparam name="T">The type parameter constrained to class types.</typeparam>
        /// <param name="item">The item to process.</param>
        /// <returns>The type name of the item.</returns>
        public string MethodWithConstraints<T>(T item) where T : class
        {
            return item?.GetType().Name ?? "null";
        }

        #endregion

    }

}