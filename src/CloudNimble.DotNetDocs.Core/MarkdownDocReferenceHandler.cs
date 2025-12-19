using CloudNimble.DotNetDocs.Core.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Handler for Markdown-based documentation references providing content path rewriting.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class extends <see cref="DocReferenceHandlerBase"/> to add Markdown-specific
    /// content rewriting functionality. It handles standard Markdown image and link syntax,
    /// rewriting absolute paths to include the destination path prefix.
    /// </para>
    /// <para>
    /// Derived classes can extend this behavior to handle format-specific patterns such as
    /// JSX imports, component props, or other content-specific path references.
    /// </para>
    /// </remarks>
    public partial class MarkdownDocReferenceHandler : DocReferenceHandlerBase
    {

        #region Private Fields

        /// <summary>
        /// Matches Markdown image syntax: ![alt text](/path/to/image.png)
        /// </summary>
        [GeneratedRegex(@"!\[[^\]]*\]\((?<path>/[^)\s#?]+)(?<suffix>[^)]*)\)",
            RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
        private static partial Regex MarkdownImageRegex();

        /// <summary>
        /// Matches Markdown link syntax: [text](/path/to/page)
        /// </summary>
        [GeneratedRegex(@"(?<!\!)\[(?<text>[^\]]*)\]\((?<path>/[^)\s#?]+)(?<suffix>[^)]*)\)",
            RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
        private static partial Regex MarkdownLinkRegex();

        /// <summary>
        /// Matches fenced code blocks (``` or ~~~) for detection.
        /// </summary>
        [GeneratedRegex(@"^(?<fence>`{3,}|~{3,})",
            RegexOptions.Multiline | RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
        private static partial Regex CodeFenceRegex();

        /// <summary>
        /// Resource directories that get their own namespaced subdirectories.
        /// </summary>
        private static readonly HashSet<string> ResourceDirectories = new(StringComparer.OrdinalIgnoreCase)
        {
            "images",
            "snippets"
        };

        /// <summary>
        /// File extensions that should have content rewritten.
        /// </summary>
        private static readonly HashSet<string> RewriteExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".md"
        };

        #endregion

        #region Properties

        /// <inheritdoc />
        public override SupportedDocumentationType DocumentationType => SupportedDocumentationType.Generic;

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override async Task ProcessAsync(DocumentationReference reference, string documentationRootPath)
        {
            ArgumentNullException.ThrowIfNull(reference);
            ArgumentException.ThrowIfNullOrWhiteSpace(documentationRootPath);

            var sourcePath = reference.DocumentationRoot;
            var destPath = Path.Combine(documentationRootPath, reference.DestinationPath);

            // Step 1: Copy and rewrite content files (excluding resource directories)
            await CopyAndRewriteFilesAsync(sourcePath, destPath, reference.DestinationPath, reference.DocumentationType);

            // Step 2: Relocate resource directories to central locations
            await RelocateResourcesAsync(sourcePath, documentationRootPath, reference.DestinationPath);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Copies files from source to destination with content path rewriting.
        /// </summary>
        /// <param name="sourceDir">The source directory.</param>
        /// <param name="destDir">The destination directory.</param>
        /// <param name="destinationPath">The destination path prefix for rewriting.</param>
        /// <param name="documentationType">The documentation type for exclusion patterns.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected virtual async Task CopyAndRewriteFilesAsync(string sourceDir, string destDir, string destinationPath, SupportedDocumentationType documentationType)
        {
            if (!Directory.Exists(sourceDir))
            {
                return;
            }

            // Get exclusion patterns and add resource directories to exclude
            var exclusionPatterns = GetExclusionPatternsForDocumentationType(documentationType);

            // Add resource directories to exclusion (they get relocated separately)
            foreach (var resourceDir in ResourceDirectories)
            {
                exclusionPatterns.Add($"{resourceDir}/**/*");
            }

            // Ensure destination directory exists
            Directory.CreateDirectory(destDir);

            await CopyDirectoryWithRewritingAsync(sourceDir, destDir, sourceDir, destinationPath, exclusionPatterns);
        }

        /// <summary>
        /// Relocates resource directories to central locations with namespacing.
        /// </summary>
        /// <param name="sourceDir">The source documentation root.</param>
        /// <param name="documentationRootPath">The collection documentation root.</param>
        /// <param name="destinationPath">The destination path for namespacing.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected virtual async Task RelocateResourcesAsync(string sourceDir, string documentationRootPath, string destinationPath)
        {
            foreach (var resourceDir in ResourceDirectories)
            {
                var sourceResourceDir = Path.Combine(sourceDir, resourceDir);
                if (!Directory.Exists(sourceResourceDir))
                {
                    continue;
                }

                // Relocate to central location: /images/{destPath}/ or /snippets/{destPath}/
                var destResourceDir = Path.Combine(documentationRootPath, resourceDir, destinationPath);

                await CopyDirectoryWithExclusionsAsync(sourceResourceDir, destResourceDir, [], skipExisting: false);
            }
        }

        /// <summary>
        /// Rewrites Markdown content to update absolute paths with the destination path prefix.
        /// </summary>
        /// <param name="content">The Markdown content to rewrite.</param>
        /// <param name="destinationPath">The destination path prefix to apply.</param>
        /// <returns>The content with rewritten paths.</returns>
        /// <remarks>
        /// <para>
        /// This method handles standard Markdown image and link syntax:
        /// </para>
        /// <list type="bullet">
        /// <item><description><c>![alt](/images/x.png)</c> → <c>![alt](/images/{dest}/x.png)</c></description></item>
        /// <item><description><c>[text](/guides/x)</c> → <c>[text]/{dest}/guides/x)</c></description></item>
        /// </list>
        /// <para>
        /// Content inside fenced code blocks is preserved and not rewritten.
        /// </para>
        /// </remarks>
        protected virtual string RewriteMarkdownContent(string content, string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(destinationPath))
            {
                return content;
            }

            // Find all code block regions to skip
            var codeBlockRanges = FindCodeBlockRanges(content);

            // Rewrite Markdown images
            content = MarkdownImageRegex().Replace(content, match =>
            {
                if (IsInCodeBlock(match.Index, codeBlockRanges))
                {
                    return match.Value;
                }

                var path = match.Groups["path"].Value;
                var suffix = match.Groups["suffix"].Value;
                var rewrittenPath = RewritePath(path, destinationPath);

                return $"![{GetImageAltText(match.Value)}]({rewrittenPath}{suffix})";
            });

            // Rewrite Markdown links
            content = MarkdownLinkRegex().Replace(content, match =>
            {
                if (IsInCodeBlock(match.Index, codeBlockRanges))
                {
                    return match.Value;
                }

                var text = match.Groups["text"].Value;
                var path = match.Groups["path"].Value;
                var suffix = match.Groups["suffix"].Value;
                var rewrittenPath = RewritePath(path, destinationPath);

                return $"[{text}]({rewrittenPath}{suffix})";
            });

            return content;
        }

        /// <summary>
        /// Rewrites an absolute path to include the destination path prefix.
        /// </summary>
        /// <param name="originalPath">The original absolute path (starting with /).</param>
        /// <param name="destinationPath">The destination path prefix to apply.</param>
        /// <returns>The rewritten path with the appropriate prefix.</returns>
        /// <remarks>
        /// <para>
        /// Resource paths (images, snippets) get the prefix inserted after the resource directory:
        /// <c>/images/logo.png</c> → <c>/images/{dest}/logo.png</c>
        /// </para>
        /// <para>
        /// Page paths get the prefix at the root:
        /// <c>/guides/overview</c> → <c>/{dest}/guides/overview</c>
        /// </para>
        /// </remarks>
        protected virtual string RewritePath(string originalPath, string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(originalPath) || !originalPath.StartsWith("/"))
            {
                return originalPath;
            }

            // Skip if already prefixed with destination path
            if (originalPath.StartsWith($"/{destinationPath}/", StringComparison.OrdinalIgnoreCase))
            {
                return originalPath;
            }

            // Skip external URLs (shouldn't have / prefix but check anyway)
            if (originalPath.StartsWith("//"))
            {
                return originalPath;
            }

            // Extract the first path segment
            var pathWithoutLeadingSlash = originalPath.TrimStart('/');
            var firstSlashIndex = pathWithoutLeadingSlash.IndexOf('/');
            var firstSegment = firstSlashIndex >= 0
                ? pathWithoutLeadingSlash.Substring(0, firstSlashIndex)
                : pathWithoutLeadingSlash;

            // Check if this is a resource directory
            if (ResourceDirectories.Contains(firstSegment))
            {
                // Resource paths: /images/logo.png → /images/{dest}/logo.png
                var remainingPath = firstSlashIndex >= 0
                    ? pathWithoutLeadingSlash.Substring(firstSlashIndex)
                    : "";

                return $"/{firstSegment}/{destinationPath}{remainingPath}";
            }

            // Page paths: /guides/overview → /{dest}/guides/overview
            return $"/{destinationPath}{originalPath}";
        }

        /// <summary>
        /// Determines if a position in the content is inside a fenced code block.
        /// </summary>
        /// <param name="position">The character position to check.</param>
        /// <param name="codeBlockRanges">The list of code block ranges in the content.</param>
        /// <returns>True if the position is inside a code block, false otherwise.</returns>
        protected bool IsInCodeBlock(int position, List<(int Start, int End)> codeBlockRanges)
        {
            foreach (var (start, end) in codeBlockRanges)
            {
                if (position >= start && position <= end)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if a position in the content is inside a fenced code block.
        /// </summary>
        /// <param name="content">The full content to analyze.</param>
        /// <param name="position">The character position to check.</param>
        /// <returns>True if the position is inside a code block, false otherwise.</returns>
        protected bool IsInsideCodeBlock(string content, int position)
        {
            var ranges = FindCodeBlockRanges(content);
            return IsInCodeBlock(position, ranges);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Finds all fenced code block ranges in the content.
        /// </summary>
        /// <param name="content">The content to analyze.</param>
        /// <returns>A list of tuples representing (start, end) positions of code blocks.</returns>
        private List<(int Start, int End)> FindCodeBlockRanges(string content)
        {
            var ranges = new List<(int Start, int End)>();
            var lines = content.Split('\n');
            var currentPosition = 0;
            var inCodeBlock = false;
            var codeBlockStart = 0;
            var currentFence = "";

            foreach (var line in lines)
            {
                var match = CodeFenceRegex().Match(line);
                if (match.Success)
                {
                    var fence = match.Groups["fence"].Value;
                    if (!inCodeBlock)
                    {
                        // Starting a code block
                        inCodeBlock = true;
                        codeBlockStart = currentPosition;
                        currentFence = fence.Substring(0, 1); // Get just ` or ~
                    }
                    else if (line.TrimStart().StartsWith(currentFence))
                    {
                        // Ending a code block (must use same fence character)
                        var fenceInLine = CodeFenceRegex().Match(line.TrimStart());
                        if (fenceInLine.Success)
                        {
                            inCodeBlock = false;
                            ranges.Add((codeBlockStart, currentPosition + line.Length));
                            currentFence = "";
                        }
                    }
                }

                currentPosition += line.Length + 1; // +1 for the newline
            }

            // Handle unclosed code block at end of content
            if (inCodeBlock)
            {
                ranges.Add((codeBlockStart, content.Length));
            }

            return ranges;
        }

        /// <summary>
        /// Extracts the alt text from a Markdown image match.
        /// </summary>
        /// <param name="matchValue">The full match value.</param>
        /// <returns>The alt text, or empty string if not found.</returns>
        private string GetImageAltText(string matchValue)
        {
            var startIndex = matchValue.IndexOf('[') + 1;
            var endIndex = matchValue.IndexOf(']');
            if (startIndex >= 0 && endIndex > startIndex)
            {
                return matchValue.Substring(startIndex, endIndex - startIndex);
            }

            return "";
        }

        /// <summary>
        /// Recursively copies a directory with content rewriting.
        /// </summary>
        /// <param name="sourceDir">The current source directory.</param>
        /// <param name="destDir">The current destination directory.</param>
        /// <param name="baseSourceDir">The base source directory for relative paths.</param>
        /// <param name="destinationPath">The destination path prefix for rewriting.</param>
        /// <param name="exclusionPatterns">Patterns for files to exclude.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CopyDirectoryWithRewritingAsync(
            string sourceDir,
            string destDir,
            string baseSourceDir,
            string destinationPath,
            List<string> exclusionPatterns)
        {
            if (!Directory.Exists(sourceDir))
            {
                return;
            }

            Directory.CreateDirectory(destDir);

            // Get all files to copy
            var files = Directory.GetFiles(sourceDir);
            var filesToProcess = files
                .Select(sourceFile => new
                {
                    SourceFile = sourceFile,
                    RelativePath = Path.GetRelativePath(baseSourceDir, sourceFile).Replace("\\", "/"),
                    DestFile = Path.Combine(destDir, Path.GetFileName(sourceFile)),
                    Extension = Path.GetExtension(sourceFile)
                })
                .Where(f => !ShouldExcludeFile(f.RelativePath, exclusionPatterns))
                .ToList();

            // Process files
            await Parallel.ForEachAsync(filesToProcess, async (fileInfo, ct) =>
            {
                await Task.Run(() =>
                {
                    if (RewriteExtensions.Contains(fileInfo.Extension))
                    {
                        // Read, rewrite, and write content
                        var content = File.ReadAllText(fileInfo.SourceFile);
                        var rewrittenContent = RewriteMarkdownContent(content, destinationPath);
                        File.WriteAllText(fileInfo.DestFile, rewrittenContent);
                    }
                    else
                    {
                        // Just copy the file
                        File.Copy(fileInfo.SourceFile, fileInfo.DestFile, overwrite: true);
                    }
                }, ct);
            });

            // Get subdirectories to process
            var subDirectories = Directory.GetDirectories(sourceDir)
                .Select(subDir => new
                {
                    SourceSubDir = subDir,
                    RelativePath = Path.GetRelativePath(baseSourceDir, subDir).Replace("\\", "/"),
                    DestSubDir = Path.Combine(destDir, Path.GetFileName(subDir))
                })
                .Where(d => !ShouldExcludeDirectory(d.RelativePath, exclusionPatterns))
                .Where(d => !ResourceDirectories.Contains(Path.GetFileName(d.SourceSubDir)))
                .ToList();

            // Recursively process subdirectories
            await Parallel.ForEachAsync(subDirectories, async (dirInfo, ct) =>
            {
                await CopyDirectoryWithRewritingAsync(
                    dirInfo.SourceSubDir,
                    dirInfo.DestSubDir,
                    baseSourceDir,
                    destinationPath,
                    exclusionPatterns);
            });
        }

        #endregion

    }

}
