namespace CloudNimble.DotNetDocs.Tests.Core.Helpers
{

    /// <summary>
    /// Conceptual content whose indentation must reach the rendered page unchanged, shared by the Markdown and Mintlify renderer tests.
    /// </summary>
    internal static class IndentationSamples
    {

        #region Fields

        /// <summary>
        /// A code sample with nested initializers, a tab-indented line, and a raw string literal with its own indentation.
        /// </summary>
        internal const string ComplexCodeExample = """"
            ```csharp
            var client = new Client(new Options
            {
                Retries = 3,
                Handlers =
                {
                    new LoggingHandler(),
                },
            });

            if (client.IsReady)
            {
            	// A tab-indented comment.
                var text = """
                    Indented inside a raw string.
                      Two more spaces.
                    """;
            }
            ```
            """";

        /// <summary>
        /// Considerations with nested bullets.
        /// </summary>
        internal const string NestedConsiderations = """
            - **Threading.** Not thread-safe.
              - Create one per request.
              - Don't share it across threads.
            - **Disposal.** Nothing to dispose.
            """;

        /// <summary>
        /// An example whose second line is a nested list item, followed by code that starts at column 0.
        /// </summary>
        /// <remarks>
        /// The renderers used to guess the indentation to remove from the first two lines, and removed two characters from every
        /// line of this sample, including the code.
        /// </remarks>
        internal const string ProseWithNestedListExample = """
            Set up the widget:
              - Create it.
                - Pass the options.
              - Call `DoWork`.

            ```csharp
            var widget = new SimpleClass();
            widget.DoWork();
            ```
            """;

        #endregion

    }

}
