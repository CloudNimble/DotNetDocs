namespace CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios
{

    /// <summary>
    /// Extension methods for ITestInterface using the same-namespace discoverable pattern.
    /// </summary>
    /// <remarks>
    /// This class demonstrates extending an interface using the discoverable pattern.
    /// Extension methods on interfaces are particularly useful as they provide default
    /// implementations that all implementers can use.
    /// </remarks>
    public static class TestsShared_ITestInterfaceExtensions
    {

        #region Public Methods

        /// <summary>
        /// Gets a formatted string from the interface.
        /// </summary>
        /// <param name="instance">The interface instance.</param>
        /// <returns>A formatted string containing the test value.</returns>
        /// <example>
        /// <code>
        /// ITestInterface test = new TestImplementation();
        /// var formatted = test.GetFormattedValue();
        /// </code>
        /// </example>
        public static string GetFormattedValue(this ITestInterface instance)
        {
            return $"Formatted: {instance.TestValue}";
        }

        /// <summary>
        /// Validates the interface instance.
        /// </summary>
        /// <param name="instance">The interface instance.</param>
        /// <returns>True if the instance is valid, otherwise false.</returns>
        /// <remarks>
        /// This extension provides common validation logic for all ITestInterface implementers.
        /// </remarks>
        public static bool Validate(this ITestInterface instance)
        {
            return instance is not null && !string.IsNullOrWhiteSpace(instance.TestValue);
        }

        #endregion

    }

}
