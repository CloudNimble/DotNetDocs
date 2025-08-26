using System;

namespace CloudNimble.DotNetDocs.Tests.Shared
{

    /// <summary>
    /// A sample class for testing documentation generation.
    /// </summary>
    public class SampleClass
    {

        #region Properties

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public int Value { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Performs a sample operation.
        /// </summary>
        /// <param name="input">The input parameter.</param>
        /// <returns>The result of the operation.</returns>
        public string DoSomething(string input)
        {
            ArgumentNullException.ThrowIfNull(input);
            return $"Processed: {input}";
        }

        /// <summary>
        /// Gets the display value.
        /// </summary>
        /// <returns>A formatted string containing the name and value.</returns>
        public string GetDisplay()
        {
            return $"{Name}: {Value}";
        }

        /// <summary>
        /// Method with optional parameter.
        /// </summary>
        /// <param name="required">Required parameter.</param>
        /// <param name="optional">Optional parameter with default value.</param>
        /// <returns>Combined result.</returns>
        public string MethodWithOptional(string required, int optional = 42)
        {
            return $"{required}: {optional}";
        }

        /// <summary>
        /// Method with params array.
        /// </summary>
        /// <param name="values">Variable number of values.</param>
        /// <returns>Sum of values.</returns>
        public int MethodWithParams(params int[] values)
        {
            int sum = 0;
            foreach (var value in values)
            {
                sum += value;
            }
            return sum;
        }

        #endregion

    }

}