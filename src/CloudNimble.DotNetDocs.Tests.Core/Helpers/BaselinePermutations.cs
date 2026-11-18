using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace CloudNimble.DotNetDocs.Tests.Core.Helpers
{

    /// <summary>
    /// Maps a baseline permutation's member visibility to the settings and folder names its baselines use.
    /// </summary>
    /// <remarks>
    /// Every baseline exists once for public members only and once for public plus internal members, so documentation built for
    /// internal audiences is protected as well. The public baselines keep their original paths; the internal ones add
    /// <see cref="WithInternalsSuffix"/>.
    /// </remarks>
    internal static class BaselinePermutations
    {

        #region Fields

        /// <summary>
        /// The suffix added to a baseline folder or file name when internal members are documented.
        /// </summary>
        public const string WithInternalsSuffix = "WithInternals";

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the member accessibilities to document for a permutation.
        /// </summary>
        /// <param name="includeInternals">Whether internal members are documented.</param>
        /// <returns>Public only, or public and internal.</returns>
        public static List<Accessibility> GetIncludedMembers(bool includeInternals) =>
            includeInternals ? [Accessibility.Public, Accessibility.Internal] : [Accessibility.Public];

        /// <summary>
        /// Gets the baseline name for a permutation.
        /// </summary>
        /// <param name="baseName">The name the public-only baseline uses, such as "FolderMode" or "BasicAssembly".</param>
        /// <param name="includeInternals">Whether internal members are documented.</param>
        /// <returns><paramref name="baseName"/>, with <see cref="WithInternalsSuffix"/> appended when internals are documented.</returns>
        public static string GetName(string baseName, bool includeInternals) =>
            includeInternals ? $"{baseName}{WithInternalsSuffix}" : baseName;

        #endregion

    }

}
