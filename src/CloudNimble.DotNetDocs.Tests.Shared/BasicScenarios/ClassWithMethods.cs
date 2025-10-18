using System;

namespace CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios
{

    /// <summary>
    /// A class demonstrating various method documentation scenarios.
    /// </summary>
    /// <remarks>
    /// Contains methods with different signatures, parameters, and return types.
    /// </remarks>
    /// <example>
    /// <code>
    /// var obj = new ClassWithMethods();
    /// var result = obj.Calculate(5, 10);
    /// </code>
    /// </example>
    public class ClassWithMethods
    {

        #region Public Methods

        /// <summary>
        /// Calculates the sum of two numbers.
        /// </summary>
        /// <param name="a">The first number.</param>
        /// <param name="b">The second number.</param>
        /// <returns>The sum of a and b.</returns>
        /// <example>
        /// <code>
        /// var result = Calculate(3, 4); // Returns 7
        /// </code>
        /// </example>
        public int Calculate(int a, int b)
        {
            return a + b;
        }

        /// <summary>
        /// Processes the input string.
        /// </summary>
        /// <param name="input">The string to process.</param>
        /// <returns>The processed string in uppercase.</returns>
        /// <remarks>
        /// This method performs a simple transformation for testing purposes.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
        public string Process(string input)
        {
            ArgumentNullException.ThrowIfNull(input);
            return input.ToUpper();
        }

        /// <summary>
        /// A void method that performs an action.
        /// </summary>
        /// <remarks>
        /// This method doesn't return anything.
        /// </remarks>
        public void PerformAction()
        {
            // Intentionally empty for testing
        }

        /// <summary>
        /// Gets a value based on a condition.
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <returns>Returns "Yes" if condition is true, "No" otherwise.</returns>
        public string GetConditionalValue(bool condition)
        {
            return condition ? "Yes" : "No";
        }

        #endregion

    }

}