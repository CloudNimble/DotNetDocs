namespace CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios
{

    /// <summary>
    /// A simple class for testing basic documentation extraction.
    /// </summary>
    /// <remarks>
    /// These are remarks about the SimpleClass. They provide additional context
    /// and information beyond what's in the summary.
    /// </remarks>
    /// <example>
    /// <code>
    /// var simple = new SimpleClass();
    /// simple.DoWork();
    /// </code>
    /// </example>
    public class SimpleClass
    {

        #region Public Methods

        /// <summary>
        /// Performs some work.
        /// </summary>
        /// <remarks>
        /// This method doesn't actually do anything, but it has documentation.
        /// </remarks>
        public void DoWork()
        {
            // Intentionally empty for testing
        }

        #endregion

    }

}