using CloudNimble.DotNetDocs.Core.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Base class for documentation reference handlers providing common file copying functionality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This abstract class contains shared logic for copying files from referenced documentation projects,
    /// including directory recursion, exclusion pattern matching, and glob pattern support.
    /// </para>
    /// <para>
    /// Derived classes should implement <see cref="ProcessAsync"/> to provide format-specific
    /// processing such as content rewriting and resource relocation.
    /// </para>
    /// </remarks>
    public abstract class DocReferenceHandlerBase : IDocReferenceHandler
    {

        #region Properties

        /// <inheritdoc />
        public abstract SupportedDocumentationType DocumentationType { get; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public abstract Task ProcessAsync(DocumentationReference reference, string documentationRootPath);

        #endregion

        #region Protected Methods

        /// <summary>
        /// Recursively copies a directory and its contents, excluding files that match exclusion patterns.
        /// </summary>
        /// <param name="sourceDir">The source directory to copy from.</param>
        /// <param name="destDir">The destination directory to copy to.</param>
        /// <param name="exclusionPatterns">List of glob patterns for files to exclude.</param>
        /// <param name="skipExisting">Whether to skip files that already exist in the destination.</param>
        /// <returns>A task representing the asynchronous copy operation.</returns>
        protected Task CopyDirectoryWithExclusionsAsync(string sourceDir, string destDir, List<string> exclusionPatterns, bool skipExisting = true)
        {
            return CopyDirectoryWithExclusionsInternalAsync(sourceDir, destDir, sourceDir, exclusionPatterns, skipExisting);
        }

        /// <summary>
        /// Gets file exclusion patterns for a given documentation type when copying DocumentationReferences.
        /// </summary>
        /// <param name="documentationType">The documentation type (Mintlify, DocFX, MkDocs, etc.).</param>
        /// <returns>A list of glob patterns for files that should be excluded from copying.</returns>
        protected List<string> GetExclusionPatternsForDocumentationType(SupportedDocumentationType documentationType)
        {
            return documentationType switch
            {
                SupportedDocumentationType.Mintlify =>
                [
                    "**/*.mdz",           // Generated zone files
                    "conceptual/**/*",    // Conceptual docs are project-specific
                    "**/*.css",           // Styles should come from collection project
                    "docs.json",          // Navigation file handled separately
                    "assembly-list.txt",  // Internal documentation generation file
                    "*.docsproj"          // MSBuild project file
                ],
                SupportedDocumentationType.DocFX =>
                [
                    "toc.yml",
                    "toc.yaml",
                    "docfx.json"
                ],
                SupportedDocumentationType.MkDocs =>
                [
                    "mkdocs.yml"
                ],
                SupportedDocumentationType.Jekyll =>
                [
                    "_config.yml",
                    "_config.yaml"
                ],
                SupportedDocumentationType.Hugo =>
                [
                    "hugo.toml",
                    "hugo.yaml",
                    "hugo.json",
                    "config.*"
                ],
                _ => []
            };
        }

        /// <summary>
        /// Determines if a directory should be excluded based on exclusion patterns.
        /// </summary>
        /// <param name="relativePath">The relative path of the directory.</param>
        /// <param name="exclusionPatterns">List of glob patterns for exclusion.</param>
        /// <returns>True if the directory should be excluded, false otherwise.</returns>
        protected bool ShouldExcludeDirectory(string relativePath, List<string> exclusionPatterns)
        {
            // Normalize path separators to forward slashes
            relativePath = relativePath.Replace("\\", "/");

            foreach (var pattern in exclusionPatterns)
            {
                var normalizedPattern = pattern.Replace("\\", "/");

                // Handle directory patterns like "conceptual/**/*"
                if (normalizedPattern.EndsWith("/**/*"))
                {
                    var prefix = normalizedPattern.Substring(0, normalizedPattern.Length - 5);
                    if (relativePath == prefix || relativePath.StartsWith(prefix + "/"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if a file should be excluded based on exclusion patterns.
        /// </summary>
        /// <param name="relativePath">The relative path of the file.</param>
        /// <param name="exclusionPatterns">List of glob patterns for exclusion.</param>
        /// <returns>True if the file should be excluded, false otherwise.</returns>
        protected bool ShouldExcludeFile(string relativePath, List<string> exclusionPatterns)
        {
            foreach (var pattern in exclusionPatterns)
            {
                if (MatchesGlobPattern(relativePath, pattern))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Matches a file path against a glob pattern.
        /// </summary>
        /// <param name="path">The file path to match.</param>
        /// <param name="pattern">The glob pattern.</param>
        /// <returns>True if the path matches the pattern, false otherwise.</returns>
        protected bool MatchesGlobPattern(string path, string pattern)
        {
            // Normalize path separators to forward slashes
            path = path.Replace("\\", "/");
            pattern = pattern.Replace("\\", "/");

            // Handle **/* patterns (matches files in any subdirectory)
            if (pattern.StartsWith("**/"))
            {
                var suffix = pattern.Substring(3);
                // Match if path ends with the suffix or contains it as a path component
                if (suffix.Contains("*"))
                {
                    // Convert suffix wildcard pattern to simple check
                    var extension = suffix.TrimStart('*');
                    return path.EndsWith(extension);
                }

                return path.EndsWith(suffix) || path.Contains("/" + suffix);
            }

            // Handle directory patterns like "conceptual/**/*"
            if (pattern.EndsWith("/**/*"))
            {
                var prefix = pattern.Substring(0, pattern.Length - 5);
                return path.StartsWith(prefix + "/") || path == prefix;
            }

            // Handle simple wildcards like "*.css"
            if (pattern.StartsWith("*."))
            {
                var extension = pattern.Substring(1);
                return path.EndsWith(extension);
            }

            // Exact match
            return path == pattern;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Recursively copies a directory and its contents, excluding files that match exclusion patterns.
        /// </summary>
        /// <param name="sourceDir">The current source directory to copy from.</param>
        /// <param name="destDir">The current destination directory to copy to.</param>
        /// <param name="baseSourceDir">The base source directory for calculating relative paths.</param>
        /// <param name="exclusionPatterns">List of glob patterns for files to exclude.</param>
        /// <param name="skipExisting">Whether to skip files that already exist in the destination.</param>
        /// <returns>A task representing the asynchronous copy operation.</returns>
        private async Task CopyDirectoryWithExclusionsInternalAsync(string sourceDir, string destDir, string baseSourceDir, List<string> exclusionPatterns, bool skipExisting)
        {
            if (!Directory.Exists(sourceDir))
            {
                return;
            }

            Directory.CreateDirectory(destDir);

            // Get all files to copy (excluding those that match patterns)
            var filesToCopy = Directory.GetFiles(sourceDir)
                .Select(sourceFile => new
                {
                    SourceFile = sourceFile,
                    FileName = Path.GetFileName(sourceFile),
                    RelativePath = Path.GetRelativePath(baseSourceDir, sourceFile).Replace("\\", "/"),
                    DestFile = Path.Combine(destDir, Path.GetFileName(sourceFile))
                })
                .Where(f => !ShouldExcludeFile(f.RelativePath, exclusionPatterns))
                .Where(f => !skipExisting || !File.Exists(f.DestFile))
                .ToList();

            // Copy files in parallel
            await Parallel.ForEachAsync(filesToCopy, async (fileInfo, ct) =>
            {
                await Task.Run(() =>
                {
                    File.Copy(fileInfo.SourceFile, fileInfo.DestFile, overwrite: !skipExisting);
                }, ct);
            });

            // Get all subdirectories (excluding those that match patterns)
            var subDirectories = Directory.GetDirectories(sourceDir)
                .Select(sourceSubDir => new
                {
                    SourceSubDir = sourceSubDir,
                    RelativePath = Path.GetRelativePath(baseSourceDir, sourceSubDir).Replace("\\", "/"),
                    DestSubDir = Path.Combine(destDir, Path.GetFileName(sourceSubDir))
                })
                .Where(d => !ShouldExcludeDirectory(d.RelativePath, exclusionPatterns))
                .ToList();

            // Recursively copy subdirectories in parallel
            await Parallel.ForEachAsync(subDirectories, async (dirInfo, ct) =>
            {
                await CopyDirectoryWithExclusionsInternalAsync(dirInfo.SourceSubDir, dirInfo.DestSubDir, baseSourceDir, exclusionPatterns, skipExisting);
            });
        }

        #endregion

    }

}
