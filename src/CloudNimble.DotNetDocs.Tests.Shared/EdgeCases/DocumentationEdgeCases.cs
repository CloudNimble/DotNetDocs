using System.Collections.Generic;
using System.Linq;

namespace CloudNimble.DotNetDocs.Tests.Shared.EdgeCases
{

    /// <summary>
    /// A class with comprehensive XML documentation tags.
    /// </summary>
    /// <remarks>
    /// <para>This class demonstrates all available XML documentation tags.</para>
    /// <para>It includes multiple paragraphs in the remarks section.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var fullDocs = new ClassWithFullDocs();
    /// fullDocs.ComplexMethod("test", 42);
    /// </code>
    /// </example>
    /// <seealso cref="ClassWithMinimalDocs"/>
    /// <seealso cref="System.String"/>
    public class ClassWithFullDocs
    {

        #region Properties

        /// <summary>
        /// Gets or sets the value property.
        /// </summary>
        /// <value>The current value as a string.</value>
        /// <remarks>This property stores important data.</remarks>
        public string Value { get; set; } = string.Empty;

        #endregion

        #region Public Methods

        /// <summary>
        /// A complex method with full documentation.
        /// </summary>
        /// <param name="text">The text parameter to process.</param>
        /// <param name="number">The number to use in processing.</param>
        /// <returns>A processed result string.</returns>
        /// <remarks>
        /// <para>This method performs complex processing.</para>
        /// <list type="bullet">
        /// <item><description>First, it validates the input.</description></item>
        /// <item><description>Then, it processes the data.</description></item>
        /// <item><description>Finally, it returns the result.</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// var result = ComplexMethod("hello", 5);
        /// Console.WriteLine(result);
        /// </code>
        /// </example>
        /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when number is negative.</exception>
        /// <seealso cref="System.String.Format(string, object[])"/>
        public string ComplexMethod(string text, int number)
        {
            ArgumentNullException.ThrowIfNull(text);
            if (number < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(number), "Number must be non-negative.");
            }
            return $"{text}: {number}";
        }

        /// <summary>
        /// Filters and transforms a collection of numbers.
        /// </summary>
        /// <returns>A collection of transformed numbers.</returns>
        /// <example>
        /// <code>
        /// var numbers = Enumerable.Range(1, 10)
        ///     .Where(x => x % 2 == 0)
        ///     .Select(x => new
        ///     {
        ///         Number = x,
        ///         Square = x * x,
        ///         Cube = x * x * x
        ///     })
        ///     .ToList();
        /// </code>
        /// </example>
        public IEnumerable<object> ProcessNumbers()
        {
            var numbers = Enumerable.Range(1, 10)
                .Where(x => x % 2 == 0)
                .Select(x => new
                {
                    Number = x,
                    Square = x * x,
                    Cube = x * x * x
                })
                .ToList();
            return numbers;
        }

        #endregion

    }

}