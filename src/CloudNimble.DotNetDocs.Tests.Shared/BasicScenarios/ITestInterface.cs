namespace CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios
{

    /// <summary>
    /// A test interface for demonstrating interface inheritance and extension methods.
    /// </summary>
    /// <remarks>
    /// This interface is used to test extension methods on interfaces and to verify
    /// that inherited members from interfaces are properly documented.
    /// </remarks>
    public interface ITestInterface
    {

        #region Properties

        /// <summary>
        /// Gets the test value.
        /// </summary>
        string TestValue { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Performs a test operation.
        /// </summary>
        /// <returns>The result of the test operation.</returns>
        void TestMethod();

        #endregion

    }

}
