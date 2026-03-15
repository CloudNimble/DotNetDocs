using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CloudNimble.DotNetDocs.Mintlify.Validation
{

    /// <summary>
    /// Represents a code example extracted from a <c>&lt;DotNetRunner&gt;</c> component in an MDX file.
    /// </summary>
    public class CodeExample
    {

        #region Properties

        /// <summary>
        /// Gets or sets the C# code extracted from the <c>initialCode</c> prop.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the path to the MDX file from which this code example was extracted.
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

        #endregion

    }

    /// <summary>
    /// Extracts <c>&lt;DotNetRunner&gt;</c> component <c>initialCode</c> prop values from MDX content.
    /// </summary>
    /// <remarks>
    /// This class scans MDX file content for occurrences of the <c>&lt;DotNetRunner&gt;</c> component
    /// and extracts the embedded C# code from the <c>initialCode</c> prop, which uses JavaScript
    /// template literal syntax (backtick-delimited strings). The extracted code can then be validated
    /// at build time using Roslyn compilation to catch errors before deployment.
    /// </remarks>
    public static partial class DotNetRunnerExtractor
    {

        #region Fields

        /// <summary>
        /// Pattern matching <c>initialCode={`...`}</c> where the content between backticks is captured
        /// as the named group "code". Uses <c>SingleLine</c> mode so that <c>.</c> matches newlines,
        /// and a lazy quantifier to correctly handle multiple runners in the same file.
        /// </summary>
        private static readonly Regex InitialCodePattern = InitialCodeRegex();

        #endregion

        #region Public Methods

        /// <summary>
        /// Extracts all <c>&lt;DotNetRunner&gt;</c> code examples from the specified MDX content.
        /// </summary>
        /// <param name="mdxContent">The raw MDX file content to scan for <c>&lt;DotNetRunner&gt;</c> components.</param>
        /// <param name="sourceFile">The file path of the MDX file, used for diagnostic reporting.</param>
        /// <returns>
        /// A list of <see cref="CodeExample"/> instances, one for each <c>initialCode</c> prop found.
        /// Returns an empty list if <paramref name="mdxContent"/> is null or whitespace, or if no
        /// <c>&lt;DotNetRunner&gt;</c> components are found.
        /// </returns>
        /// <example>
        /// <code>
        /// var mdx = "&lt;DotNetRunner initialCode={`Console.WriteLine(\"Hello\");`} /&gt;";
        /// var examples = DotNetRunnerExtractor.Extract(mdx, "getting-started.mdx");
        /// // examples[0].Code == "Console.WriteLine(\"Hello\");"
        /// </code>
        /// </example>
        public static List<CodeExample> Extract(string mdxContent, string sourceFile)
        {
            var results = new List<CodeExample>();

            if (string.IsNullOrWhiteSpace(mdxContent))
            {
                return results;
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(sourceFile);

            var matches = InitialCodePattern.Matches(mdxContent);

            foreach (Match match in matches)
            {
                results.Add(new CodeExample
                {
                    Code = match.Groups["code"].Value,
                    SourceFile = sourceFile
                });
            }

            return results;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Source-generated regex for matching <c>initialCode={`...`}</c> prop values.
        /// </summary>
        /// <returns>A compiled <see cref="Regex"/> instance for matching <c>initialCode</c> props.</returns>
        [GeneratedRegex(@"initialCode=\{`(?<code>.*?)`\}", RegexOptions.Singleline)]
        private static partial Regex InitialCodeRegex();

        #endregion

    }

}
