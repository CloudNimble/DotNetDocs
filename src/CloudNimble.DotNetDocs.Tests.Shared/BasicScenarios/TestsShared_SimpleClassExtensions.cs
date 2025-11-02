namespace CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios
{

    /// <summary>
    /// Extension methods for SimpleClass using the same-namespace discoverable pattern.
    /// </summary>
    /// <remarks>
    /// This class demonstrates the discoverable extension method pattern where extensions
    /// are placed in the same namespace as the type being extended. Users automatically
    /// have access to these extensions when they use the extended type, without needing
    /// an additional using directive.
    ///
    /// The naming convention is {AssemblyPrefix}_{TypeName}Extensions to avoid naming
    /// conflicts while maintaining discoverability.
    /// </remarks>
    public static class TestsShared_SimpleClassExtensions
    {

        #region Public Methods

        /// <summary>
        /// Converts a SimpleClass instance to a display string.
        /// </summary>
        /// <param name="instance">The SimpleClass instance.</param>
        /// <returns>A formatted string representation.</returns>
        /// <example>
        /// <code>
        /// var simple = new SimpleClass();
        /// var display = simple.ToDisplayString();
        /// </code>
        /// </example>
        public static string ToDisplayString(this SimpleClass instance)
        {
            return $"SimpleClass: {instance.GetType().Name}";
        }

        /// <summary>
        /// Checks if a SimpleClass instance is valid.
        /// </summary>
        /// <param name="instance">The SimpleClass instance.</param>
        /// <returns>True if valid, otherwise false.</returns>
        /// <remarks>
        /// This is a simple validation extension for demonstration purposes.
        /// </remarks>
        public static bool IsValid(this SimpleClass instance)
        {
            return instance is not null;
        }

        #endregion

    }

}
