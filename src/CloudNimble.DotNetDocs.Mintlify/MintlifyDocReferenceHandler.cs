using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;

namespace CloudNimble.DotNetDocs.Mintlify
{

    /// <summary>
    /// Handles documentation references for Mintlify documentation format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler extends <see cref="MarkdownDocReferenceHandler"/> to add Mintlify-specific
    /// content rewriting patterns including ES imports, JSX attributes, and CSS url() references.
    /// </para>
    /// <para>
    /// It processes referenced documentation by:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Copying content files with path rewriting</description></item>
    /// <item><description>Relocating images to <c>/images/{DestinationPath}/</c></description></item>
    /// <item><description>Relocating snippets to <c>/snippets/{DestinationPath}/</c></description></item>
    /// </list>
    /// </remarks>
    public partial class MintlifyDocReferenceHandler : MarkdownDocReferenceHandler
    {

        #region Private Fields

        /// <summary>
        /// Matches ES import statements: import X from '/snippets/X.jsx'
        /// </summary>
        [GeneratedRegex(@"from\s+['""](?<path>/[^'""]+)['""]",
            RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
        private static partial Regex EsImportRegex();

        /// <summary>
        /// Matches JSX src attributes: src="/images/logo.png" or src='/images/logo.png'
        /// </summary>
        [GeneratedRegex(@"src\s*=\s*['""](?<path>/[^'""]+)['""]",
            RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
        private static partial Regex JsxSrcRegex();

        /// <summary>
        /// Matches JSX href attributes: href="/guides/x" or href='/guides/x'
        /// </summary>
        [GeneratedRegex(@"href\s*=\s*['""](?<path>/[^'""#?]+)(?<suffix>[^'""]*)['""]",
            RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
        private static partial Regex JsxHrefRegex();

        /// <summary>
        /// Matches CSS url() functions: url(/images/bg.svg) or url('/images/bg.svg')
        /// </summary>
        [GeneratedRegex(@"url\(\s*['""]?(?<path>/[^'""\)]+)['""]?\s*\)",
            RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
        private static partial Regex CssUrlRegex();

        /// <summary>
        /// File extensions that should have content rewritten.
        /// </summary>
        private static readonly HashSet<string> RewriteExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".md",
            ".mdx",
            ".jsx",
            ".tsx"
        };

        /// <summary>
        /// Directories that should be relocated to central locations.
        /// </summary>
        private static readonly HashSet<string> ResourceDirectoriesToRelocate = new(StringComparer.OrdinalIgnoreCase)
        {
            "images",
            "snippets"
        };

        #endregion

        #region Properties

        /// <inheritdoc />
        public override SupportedDocumentationType DocumentationType => SupportedDocumentationType.Mintlify;

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
            await CopyAndRewriteFilesAsync(sourcePath, destPath, reference.DestinationPath);

            // Step 2: Relocate resource directories to central locations
            await RelocateResourcesAsync(sourcePath, documentationRootPath, reference.DestinationPath);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Copies files from source to destination with content path rewriting.
        /// </summary>
        /// <param name="sourceDir">The source directory.</param>
        /// <param name="destDir">The destination directory.</param>
        /// <param name="destinationPath">The destination path prefix for rewriting.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        internal async Task CopyAndRewriteFilesAsync(string sourceDir, string destDir, string destinationPath)
        {
            if (!Directory.Exists(sourceDir))
            {
                return;
            }

            // Get exclusion patterns and add resource directories to exclude
            var exclusionPatterns = GetExclusionPatternsForDocumentationType(SupportedDocumentationType.Mintlify);

            // Add resource directories to exclusion (they get relocated separately)
            foreach (var resourceDir in ResourceDirectoriesToRelocate)
            {
                exclusionPatterns.Add($"{resourceDir}/**/*");
            }

            // Ensure destination directory exists
            Directory.CreateDirectory(destDir);

            await CopyDirectoryWithRewritingAsync(sourceDir, destDir, sourceDir, destinationPath, exclusionPatterns);
        }

        /// <summary>
        /// Rewrites content with Mintlify-specific patterns in addition to base Markdown.
        /// </summary>
        /// <param name="content">The content to rewrite.</param>
        /// <param name="destinationPath">The destination path prefix.</param>
        /// <returns>The rewritten content.</returns>
        internal string RewriteMintlifyContent(string content, string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(destinationPath))
            {
                return content;
            }

            // First apply base Markdown rewriting
            content = RewriteMarkdownContent(content, destinationPath);

            // Find code block ranges for skipping
            var codeBlockRanges = FindCodeBlockRangesInternal(content);

            // Rewrite ES imports: from '/snippets/X.jsx'
            content = EsImportRegex().Replace(content, match =>
            {
                if (IsInCodeBlock(match.Index, codeBlockRanges))
                {
                    return match.Value;
                }

                var path = match.Groups["path"].Value;
                var rewrittenPath = RewritePath(path, destinationPath);

                return $"from '{rewrittenPath}'";
            });

            // Rewrite JSX src attributes
            content = JsxSrcRegex().Replace(content, match =>
            {
                if (IsInCodeBlock(match.Index, codeBlockRanges))
                {
                    return match.Value;
                }

                var path = match.Groups["path"].Value;
                var rewrittenPath = RewritePath(path, destinationPath);

                return $"src=\"{rewrittenPath}\"";
            });

            // Rewrite JSX href attributes
            content = JsxHrefRegex().Replace(content, match =>
            {
                if (IsInCodeBlock(match.Index, codeBlockRanges))
                {
                    return match.Value;
                }

                var path = match.Groups["path"].Value;
                var suffix = match.Groups["suffix"].Value;
                var rewrittenPath = RewritePath(path, destinationPath);

                return $"href=\"{rewrittenPath}{suffix}\"";
            });

            // Rewrite CSS url() functions
            content = CssUrlRegex().Replace(content, match =>
            {
                if (IsInCodeBlock(match.Index, codeBlockRanges))
                {
                    return match.Value;
                }

                var path = match.Groups["path"].Value;
                var rewrittenPath = RewritePath(path, destinationPath);

                return $"url({rewrittenPath})";
            });

            return content;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Recursively copies a directory with content rewriting.
        /// </summary>
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
                        var rewrittenContent = RewriteMintlifyContent(content, destinationPath);
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
                .Where(d => !ResourceDirectoriesToRelocate.Contains(Path.GetFileName(d.SourceSubDir)))
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

        /// <summary>
        /// Finds all fenced code block ranges in the content.
        /// </summary>
        /// <param name="content">The content to analyze.</param>
        /// <returns>A list of tuples representing (start, end) positions of code blocks.</returns>
        private List<(int Start, int End)> FindCodeBlockRangesInternal(string content)
        {
            var ranges = new List<(int Start, int End)>();
            var lines = content.Split('\n');
            var currentPosition = 0;
            var inCodeBlock = false;
            var codeBlockStart = 0;
            var currentFence = "";

            foreach (var line in lines)
            {
                var trimmedLine = line.TrimStart();
                if (trimmedLine.StartsWith("```") || trimmedLine.StartsWith("~~~"))
                {
                    var fenceChar = trimmedLine[0];
                    if (!inCodeBlock)
                    {
                        inCodeBlock = true;
                        codeBlockStart = currentPosition;
                        currentFence = fenceChar.ToString();
                    }
                    else if (trimmedLine.StartsWith(currentFence + currentFence + currentFence) ||
                             (trimmedLine.Length >= 3 && trimmedLine.Substring(0, 3) == new string(fenceChar, 3)))
                    {
                        inCodeBlock = false;
                        ranges.Add((codeBlockStart, currentPosition + line.Length));
                        currentFence = "";
                    }
                }

                currentPosition += line.Length + 1;
            }

            if (inCodeBlock)
            {
                ranges.Add((codeBlockStart, content.Length));
            }

            return ranges;
        }

        #endregion

    }

}
