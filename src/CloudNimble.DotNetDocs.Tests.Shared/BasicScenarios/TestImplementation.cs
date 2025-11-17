namespace CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios
{

    /// <summary>
    /// A test implementation of ITestInterface.
    /// </summary>
    /// <remarks>
    /// This class implements ITestInterface to demonstrate interface member inheritance
    /// in documentation generation.
    /// </remarks>
    public class TestImplementation : ITestInterface
    {

        #region Properties

        /// <summary>
        /// Gets the test value.
        /// </summary>
        public string TestValue { get; } = "Test";

        #endregion

        #region Public Methods

        /// <summary>
        /// Performs a test operation.
        /// </summary>
        public void TestMethod()
        {
            // Implementation
        }

        /// <summary>
        /// An additional method specific to the implementation.
        /// </summary>
        public void AdditionalMethod()
        {
            // Implementation-specific method
        }

        #endregion

    }

}
