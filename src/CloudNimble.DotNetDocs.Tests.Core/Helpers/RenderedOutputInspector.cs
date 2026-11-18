using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CloudNimble.DotNetDocs.Tests.Core.Helpers
{

    /// <summary>
    /// Inspects rendered documentation output on disk to verify that links and navigation entries point at real files.
    /// </summary>
    /// <remarks>
    /// This class checks the output a user actually gets, rather than the values returned by individual path helpers.
    /// It is shared with the Mintlify test project through a linked Compile item.
    /// </remarks>
    internal static partial class RenderedOutputInspector
    {

        #region Public Methods

        /// <summary>
        /// Finds every local link in the rendered pages under <paramref name="scanRoot"/> that is broken.
        /// </summary>
        /// <param name="documentationRootPath">The documentation root that root-relative links (starting with <c>/</c>) resolve against.</param>
        /// <param name="scanRoot">The folder containing the rendered pages to scan.</param>
        /// <param name="pageExtension">The rendered page extension, without the dot (for example, <c>md</c> or <c>mdx</c>).</param>
        /// <param name="allowSiteStyleLinks">
        /// <see langword="true"/> to accept relative links without an extension and links to a folder containing an index page,
        /// which documentation sites such as Mintlify resolve; <see langword="false"/> to require relative links to name an existing file.
        /// Root-relative links always target a documentation site, so they are always resolved site-style.
        /// </param>
        /// <returns>
        /// A description of each link whose target doesn't exist, that points back at the page containing it,
        /// or that starts with <c>//</c> (which browsers treat as a host name), in the form <c>source file -&gt; link</c>.
        /// </returns>
        public static List<string> FindBrokenLinks(string documentationRootPath, string scanRoot, string pageExtension, bool allowSiteStyleLinks)
        {
            var brokenLinks = new List<string>();

            foreach (var page in Directory.EnumerateFiles(scanRoot, $"*.{pageExtension}", SearchOption.AllDirectories))
            {
                foreach (var link in ExtractLinks(File.ReadAllText(page)))
                {
                    var target = ResolveLink(link, page, documentationRootPath, pageExtension, allowSiteStyleLinks);
                    var isSelfLink = !link.Contains('#') && string.Equals(target, Path.GetFullPath(page), StringComparison.OrdinalIgnoreCase);

                    if (link.StartsWith("//", StringComparison.Ordinal) || target is null || isSelfLink)
                    {
                        brokenLinks.Add($"{Path.GetRelativePath(documentationRootPath, page)} -> {link}");
                    }
                }
            }

            return brokenLinks;
        }

        /// <summary>
        /// Resolves every local link on a rendered page to the file it points at.
        /// </summary>
        /// <param name="page">The rendered page to read.</param>
        /// <param name="documentationRootPath">The documentation root that root-relative links resolve against.</param>
        /// <param name="pageExtension">The rendered page extension, without the dot.</param>
        /// <param name="allowSiteStyleLinks">Whether relative extensionless and folder-index links are accepted.</param>
        /// <returns>The full path of each link target that exists.</returns>
        public static List<string> GetLinkedFiles(string page, string documentationRootPath, string pageExtension, bool allowSiteStyleLinks)
        {
            return ExtractLinks(File.ReadAllText(page))
                .Select(link => ResolveLink(link, page, documentationRootPath, pageExtension, allowSiteStyleLinks))
                .OfType<string>()
                .ToList();
        }

        /// <summary>
        /// Finds every page referenced by a Mintlify <c>docs.json</c> navigation that does not exist on disk.
        /// </summary>
        /// <param name="documentationRootPath">The documentation root containing <c>docs.json</c>.</param>
        /// <returns>Each navigation page path that has no matching <c>.mdx</c> or <c>.md</c> file.</returns>
        public static List<string> FindMissingNavigationPages(string documentationRootPath)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(documentationRootPath, "docs.json")));

            return GetNavigationPages(document.RootElement)
                .Distinct()
                .Where(page => !File.Exists(Path.Combine(documentationRootPath, $"{page}.mdx"))
                    && !File.Exists(Path.Combine(documentationRootPath, $"{page}.md")))
                .ToList();
        }

        /// <summary>
        /// Gets every page path referenced by a Mintlify navigation element.
        /// </summary>
        /// <param name="element">The JSON element to search.</param>
        /// <returns>The values of every <c>pages</c> array entry and <c>root</c> property found at any depth.</returns>
        public static IEnumerable<string> GetNavigationPages(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        if (property.Name == "pages" && property.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in property.Value.EnumerateArray())
                            {
                                if (item.ValueKind == JsonValueKind.String)
                                {
                                    yield return item.GetString()!;
                                }
                                else
                                {
                                    foreach (var page in GetNavigationPages(item))
                                    {
                                        yield return page;
                                    }
                                }
                            }
                        }
                        else if (property.Name == "root" && property.Value.ValueKind == JsonValueKind.String)
                        {
                            yield return property.Value.GetString()!;
                        }
                        else
                        {
                            foreach (var page in GetNavigationPages(property.Value))
                            {
                                yield return page;
                            }
                        }
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        foreach (var page in GetNavigationPages(item))
                        {
                            yield return page;
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Extracts the local link targets from Markdown or MDX content.
        /// </summary>
        /// <param name="content">The page content.</param>
        /// <returns>
        /// The targets of Markdown links and <c>href</c> attributes, excluding external URLs, pure anchors, and anything inside code.
        /// </returns>
        public static IEnumerable<string> ExtractLinks(string content)
        {
            // Code can contain bracket-paren sequences that look like links, so remove it before scanning.
            var prose = InlineCodeRegex().Replace(FencedCodeRegex().Replace(content, string.Empty), string.Empty);

            return MarkdownLinkRegex().Matches(prose).Select(match => match.Groups["url"].Value)
                .Concat(HrefRegex().Matches(prose).Select(match => match.Groups["url"].Value))
                .Where(IsLocalLink);
        }

        /// <summary>
        /// Resolves a link to the file it points at.
        /// </summary>
        /// <param name="link">The link target.</param>
        /// <param name="sourceFile">The page containing the link, which relative links resolve against.</param>
        /// <param name="documentationRootPath">The documentation root that root-relative links resolve against.</param>
        /// <param name="pageExtension">The rendered page extension, without the dot.</param>
        /// <param name="allowSiteStyleLinks">Whether relative extensionless and folder-index links are accepted.</param>
        /// <returns>The full path of the target file, or <see langword="null"/> if it doesn't exist.</returns>
        public static string? ResolveLink(string link, string sourceFile, string documentationRootPath, string pageExtension, bool allowSiteStyleLinks)
        {
            var path = Uri.UnescapeDataString(link.Split('#', '?')[0]);
            var isRootRelative = path.StartsWith('/');
            var basePath = isRootRelative ? documentationRootPath : Path.GetDirectoryName(sourceFile)!;
            var target = Path.GetFullPath(Path.Combine(basePath, path.TrimStart('/')));

            List<string> candidates = [target];
            if (allowSiteStyleLinks || isRootRelative)
            {
                candidates.Add($"{target.TrimEnd(Path.DirectorySeparatorChar)}.{pageExtension}");
                candidates.Add(Path.Combine(target, $"index.{pageExtension}"));
            }

            return candidates.FirstOrDefault(File.Exists);
        }

        #endregion

        #region Private Methods

        [GeneratedRegex(@"^\s*(```|~~~).*?^\s*\1", RegexOptions.Multiline | RegexOptions.Singleline)]
        private static partial Regex FencedCodeRegex();

        [GeneratedRegex(@"href=""(?<url>[^""]+)""")]
        private static partial Regex HrefRegex();

        [GeneratedRegex(@"`[^`\r\n]*`")]
        private static partial Regex InlineCodeRegex();

        [GeneratedRegex(@"\[(?:[^\[\]]|\[[^\]]*\])*\]\((?<url>[^)\s]+)(?:\s+""[^""]*"")?\)")]
        private static partial Regex MarkdownLinkRegex();

        /// <summary>
        /// Determines whether a link target points into the rendered output rather than an external site or the same page.
        /// </summary>
        /// <param name="link">The link target.</param>
        /// <returns><see langword="true"/> for local file links; otherwise, <see langword="false"/>.</returns>
        private static bool IsLocalLink(string link)
        {
            return !string.IsNullOrWhiteSpace(link)
                && !link.StartsWith('#')
                && !link.Contains("://", StringComparison.Ordinal)
                && !link.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

    }

}
