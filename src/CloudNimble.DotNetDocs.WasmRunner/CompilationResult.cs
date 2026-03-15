namespace CloudNimble.DotNetDocs.WasmRunner
{

    /// <summary>
    /// Represents the result of compiling and executing a C# code snippet.
    /// </summary>
    public class CompilationResult
    {

        #region Properties

        /// <summary>
        /// Gets the compiler diagnostic messages, if any.
        /// </summary>
        public string Diagnostics { get; init; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether compilation and execution succeeded.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// Gets the captured console output from execution.
        /// </summary>
        public string Output { get; init; } = string.Empty;

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a failed compilation result with the specified diagnostics.
        /// </summary>
        /// <param name="diagnostics">The compiler diagnostic messages.</param>
        /// <returns>A new <see cref="CompilationResult"/> representing failure.</returns>
        public static CompilationResult Failure(string diagnostics) => new()
        {
            IsSuccess = false,
            Diagnostics = diagnostics
        };

        /// <summary>
        /// Creates a successful compilation result with the specified output.
        /// </summary>
        /// <param name="output">The captured console output.</param>
        /// <returns>A new <see cref="CompilationResult"/> representing success.</returns>
        public static CompilationResult Success(string output) => new()
        {
            IsSuccess = true,
            Output = output
        };

        #endregion

    }

}
