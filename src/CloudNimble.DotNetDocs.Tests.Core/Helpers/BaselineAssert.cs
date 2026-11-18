using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core.Helpers
{

    /// <summary>
    /// Compares a whole rendered output folder against its baseline folder.
    /// </summary>
    /// <remarks>
    /// Every file is checked before the test fails, so a single run reports every missing, unexpected, or changed file
    /// instead of stopping at the first one.
    /// </remarks>
    internal static class BaselineAssert
    {

        #region Public Methods

        /// <summary>
        /// Fails the test, once, when the output folder does not match the baseline folder exactly.
        /// </summary>
        /// <param name="baselinesDir">The folder holding the committed baseline files.</param>
        /// <param name="actualDir">The folder the renderer wrote to.</param>
        /// <remarks>
        /// Line endings are normalized before contents are compared. The failure message lists every problem found.
        /// </remarks>
        public static void DirectoryMatches(string baselinesDir, string actualDir)
        {
            if (!Directory.Exists(baselinesDir))
            {
                Assert.Fail($"No baselines were found at {Path.GetFullPath(baselinesDir)}. Run 'dotnet breakdance generate' to create them.");
            }

            var baselineFiles = GetRelativeFiles(baselinesDir);
            var actualFiles = Directory.Exists(actualDir) ? GetRelativeFiles(actualDir) : [];
            var problems = new List<string>();

            foreach (var file in baselineFiles.Except(actualFiles, StringComparer.Ordinal))
            {
                problems.Add($"Missing from output: {file}");
            }

            foreach (var file in actualFiles.Except(baselineFiles, StringComparer.Ordinal))
            {
                problems.Add($"Not in baselines: {file}");
            }

            foreach (var file in baselineFiles.Intersect(actualFiles, StringComparer.Ordinal))
            {
                var difference = DescribeDifference(Path.Combine(baselinesDir, file), Path.Combine(actualDir, file));
                if (difference is not null)
                {
                    problems.Add($"Changed: {file} ({difference})");
                }
            }

            if (problems.Count > 0)
            {
                Assert.Fail($"{problems.Count} baseline problem(s) in {Path.GetFullPath(baselinesDir)}. If the changes are intentional, run 'dotnet breakdance generate'.{Environment.NewLine}" +
                    string.Join(Environment.NewLine, problems.Select(p => $"  - {p}")));
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Describes the first line that differs between two files, or returns null when they match.
        /// </summary>
        /// <param name="baselinePath">The baseline file.</param>
        /// <param name="actualPath">The rendered file.</param>
        private static string? DescribeDifference(string baselinePath, string actualPath)
        {
            var baselineLines = File.ReadAllText(baselinePath).ReplaceLineEndings("\n").Split('\n');
            var actualLines = File.ReadAllText(actualPath).ReplaceLineEndings("\n").Split('\n');

            for (var i = 0; i < Math.Max(baselineLines.Length, actualLines.Length); i++)
            {
                var expected = i < baselineLines.Length ? baselineLines[i] : "<end of file>";
                var actual = i < actualLines.Length ? actualLines[i] : "<end of file>";
                if (!string.Equals(expected, actual, StringComparison.Ordinal))
                {
                    return $"line {i + 1}: expected '{expected}', got '{actual}'";
                }
            }

            return null;
        }

        /// <summary>
        /// Gets every file under a folder as a path relative to it, sorted ordinally.
        /// </summary>
        /// <param name="root">The folder to list.</param>
        private static List<string> GetRelativeFiles(string root) =>
            [.. Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(root, f))
                .OrderBy(f => f, StringComparer.Ordinal)];

        #endregion

    }

}
