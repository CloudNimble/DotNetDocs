using System.Collections.Generic;
using System.IO;
using CloudNimble.DotNetDocs.Core;

namespace CloudNimble.DotNetDocs.Tests.Core.Helpers
{

    /// <summary>
    /// Edits conceptual content files the way a person would, so tests can verify those edits reach the rendered output.
    /// </summary>
    /// <remarks>
    /// The expected folder layout is spelled out here independently of <see cref="ProjectContext"/>, so a test fails if the
    /// shared conceptual path helpers drift from the documented layout. It is shared with the Mintlify test project through a
    /// linked Compile item.
    /// </remarks>
    internal static class ConceptualRoundTrip
    {

        #region Public Methods

        /// <summary>
        /// Gets the documented conceptual folder for a namespace, type, or member, relative to the conceptual root.
        /// </summary>
        /// <param name="namespaceName">The namespace name, or <see langword="null"/> or empty for the global namespace.</param>
        /// <param name="names">The type name, optionally followed by a member name.</param>
        /// <returns>One folder per namespace segment (or <c>global</c>), followed by one folder per name.</returns>
        public static string GetExpectedFolder(string? namespaceName, params string[] names)
        {
            string[] namespaceParts = string.IsNullOrWhiteSpace(namespaceName) ? ["global"] : namespaceName.Split('.');

            return Path.Combine([.. namespaceParts, .. names]);
        }

        /// <summary>
        /// Gets the unique marker text written into the usage file of a conceptual folder.
        /// </summary>
        /// <param name="relativeFolder">The folder, relative to the conceptual root.</param>
        /// <returns>
        /// Text built only from letters, digits, and dashes, so no renderer or transformer rewrites it. The <c>-end</c> suffix keeps a
        /// namespace's marker from matching as a prefix of the markers inside it.
        /// </returns>
        public static string GetMarker(string relativeFolder)
        {
            return $"marker-{relativeFolder.Replace('\\', '-').Replace('/', '-').Replace('.', '-')}-end";
        }

        /// <summary>
        /// Writes a marker into a conceptual usage file, creating its folder if needed.
        /// </summary>
        /// <param name="conceptualRoot">The full path of the conceptual root.</param>
        /// <param name="relativeFolder">The folder, relative to the conceptual root.</param>
        /// <returns>The marker that was written.</returns>
        public static string WriteUsageMarker(string conceptualRoot, string relativeFolder)
        {
            var folder = Path.Combine(conceptualRoot, relativeFolder);
            Directory.CreateDirectory(folder);

            var marker = GetMarker(relativeFolder);
            File.WriteAllText(Path.Combine(folder, DocConstants.UsageFileName), marker);

            return marker;
        }

        /// <summary>
        /// Replaces every generated usage placeholder under the conceptual root with a marker naming its own folder.
        /// </summary>
        /// <param name="conceptualRoot">The full path of the conceptual root.</param>
        /// <returns>The markers that were written.</returns>
        public static List<string> WriteUsageMarkers(string conceptualRoot)
        {
            List<string> markers = [];

            foreach (var file in Directory.EnumerateFiles(conceptualRoot, DocConstants.UsageFileName, SearchOption.AllDirectories))
            {
                var relativeFolder = Path.GetRelativePath(conceptualRoot, Path.GetDirectoryName(file)!);
                markers.Add(WriteUsageMarker(conceptualRoot, relativeFolder));
            }

            return markers;
        }

        #endregion

    }

}
