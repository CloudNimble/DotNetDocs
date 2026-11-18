using System;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        #region Missing Pages Tests

        /// <summary>
        /// Tests that a missing page is reported from every navigation container, and that links and git source mounts are skipped.
        /// </summary>
        [TestMethod]
        public void FindMissingPages_EveryNavigationContainer_ReportsEachMissingPage()
        {
            var config = CreateConfigWithEveryNavigationContainer();
            var rootPath = CreateMissingPagesRoot();

            try
            {
                foreach (var page in EveryNavigationContainerPages)
                {
                    WritePage(rootPath, $"{page}.mdx");
                }

                DocsJsonManager.FindMissingPages(config, rootPath).Should().BeEmpty();

                foreach (var page in EveryNavigationContainerPages)
                {
                    var pageFile = Path.Combine(rootPath, $"{page}.mdx");
                    File.Delete(pageFile);

                    DocsJsonManager.FindMissingPages(config, rootPath).Should().ContainSingle().Which.Should().Be(page);

                    WritePage(rootPath, $"{page}.mdx");
                }
            }
            finally
            {
                Directory.Delete(rootPath, true);
            }
        }

        /// <summary>
        /// Tests that a .md file satisfies an entry without an extension.
        /// </summary>
        [TestMethod]
        public void FindMissingPages_MdFile_IsFound()
        {
            var rootPath = CreateMissingPagesRoot();

            try
            {
                WritePage(rootPath, "guides/setup.md");

                DocsJsonManager.FindMissingPages(CreateConfigWithPages("guides/setup"), rootPath).Should().BeEmpty();
            }
            finally
            {
                Directory.Delete(rootPath, true);
            }
        }

        /// <summary>
        /// Tests that an entry ending in .mdx or .md must exist exactly as written.
        /// </summary>
        [TestMethod]
        public void FindMissingPages_EntryWithExtension_MustExistAsWritten()
        {
            var rootPath = CreateMissingPagesRoot();

            try
            {
                WritePage(rootPath, "guides/setup.mdx");
                WritePage(rootPath, "guides/other.mdx");

                DocsJsonManager.FindMissingPages(CreateConfigWithPages("guides/setup.mdx", "guides/other.md"), rootPath)
                    .Should().ContainSingle().Which.Should().Be("guides/other.md");
            }
            finally
            {
                Directory.Delete(rootPath, true);
            }
        }

        /// <summary>
        /// Tests that a leading slash is ignored when the file is resolved, and that a missing entry is reported as written.
        /// </summary>
        [TestMethod]
        public void FindMissingPages_LeadingSlash_IsIgnoredWhenResolving()
        {
            var rootPath = CreateMissingPagesRoot();

            try
            {
                WritePage(rootPath, "guides/setup.mdx");

                DocsJsonManager.FindMissingPages(CreateConfigWithPages("/guides/setup", "/guides/missing"), rootPath)
                    .Should().ContainSingle().Which.Should().Be("/guides/missing");
            }
            finally
            {
                Directory.Delete(rootPath, true);
            }
        }

        /// <summary>
        /// Tests that OpenAPI operation and webhook entries are skipped, while pages that merely start with a lowercase method name are checked.
        /// </summary>
        [TestMethod]
        public void FindMissingPages_OpenApiEntries_AreSkipped()
        {
            var rootPath = CreateMissingPagesRoot();

            try
            {
                var config = CreateConfigWithPages(
                    "/openapi.json GET /users",
                    "api/openapi.yaml DELETE /users/{id}",
                    "POST /users",
                    "openapi.json webhook newUser",
                    "get started");

                DocsJsonManager.FindMissingPages(config, rootPath).Should().ContainSingle().Which.Should().Be("get started");
            }
            finally
            {
                Directory.Delete(rootPath, true);
            }
        }

        /// <summary>
        /// Tests that a page listed in several navigation sections is reported once.
        /// </summary>
        [TestMethod]
        public void FindMissingPages_PageInSeveralSections_IsReportedOnce()
        {
            var rootPath = CreateMissingPagesRoot();

            try
            {
                var config = new DocsJsonConfig
                {
                    Name = "Test",
                    Navigation = new NavigationConfig
                    {
                        Pages = ["shared/page"],
                        Tabs = [new TabConfig { Tab = "Guides", Groups = [new GroupConfig { Group = "Start", Pages = ["shared/page"] }] }]
                    }
                };

                DocsJsonManager.FindMissingPages(config, rootPath).Should().ContainSingle().Which.Should().Be("shared/page");
            }
            finally
            {
                Directory.Delete(rootPath, true);
            }
        }

        /// <summary>
        /// Tests that the file overload resolves pages against the folder that holds docs.json.
        /// </summary>
        [TestMethod]
        public void FindMissingPages_DocsJsonPath_ResolvesAgainstItsFolder()
        {
            var rootPath = CreateMissingPagesRoot();

            try
            {
                WritePage(rootPath, "index.mdx");
                var docsJsonPath = Path.Combine(rootPath, "docs.json");
                File.WriteAllText(docsJsonPath, """
                    {
                      "name": "Test",
                      "navigation": {
                        "groups": [{ "group": "Guides", "root": "guides/overview", "pages": ["index"] }]
                      }
                    }
                    """);

                DocsJsonManager.FindMissingPages(docsJsonPath).Should().ContainSingle().Which.Should().Be("guides/overview");
            }
            finally
            {
                Directory.Delete(rootPath, true);
            }
        }

        /// <summary>
        /// Tests that the file overload rejects a path that does not exist.
        /// </summary>
        [TestMethod]
        public void FindMissingPages_DocsJsonPathNotFound_Throws()
        {
            var act = () => DocsJsonManager.FindMissingPages(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "docs.json"));

            act.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Tests that the message names both candidate files for an entry without an extension.
        /// </summary>
        [TestMethod]
        public void GetMissingPageMessage_EntryWithoutExtension_NamesBothFiles()
        {
            DocsJsonManager.GetMissingPageMessage("guides/setup")
                .Should().Be("docs.json navigation lists 'guides/setup', but guides/setup.mdx or guides/setup.md were not found");
        }

        /// <summary>
        /// Tests that the message names the single file for an entry with an extension.
        /// </summary>
        [TestMethod]
        public void GetMissingPageMessage_EntryWithExtension_NamesTheFile()
        {
            DocsJsonManager.GetMissingPageMessage("/guides/setup.mdx")
                .Should().Be("docs.json navigation lists '/guides/setup.mdx', but guides/setup.mdx was not found");
        }

        #endregion

        #region Missing Pages Helpers

        /// <summary>
        /// Creates a configuration whose top-level navigation lists the specified pages.
        /// </summary>
        /// <param name="pages">The page entries.</param>
        private static DocsJsonConfig CreateConfigWithPages(params string[] pages) => new()
        {
            Name = "Test",
            Navigation = new NavigationConfig { Pages = [.. pages] }
        };

        /// <summary>
        /// Creates an empty temporary documentation root.
        /// </summary>
        private static string CreateMissingPagesRoot()
        {
            var rootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(rootPath);

            return rootPath;
        }

        /// <summary>
        /// Writes a page file, creating its folder when needed.
        /// </summary>
        /// <param name="rootPath">The documentation root.</param>
        /// <param name="relativePath">The page file path relative to the root.</param>
        private static void WritePage(string rootPath, string relativePath)
        {
            var filePath = Path.Combine(rootPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, "# Page");
        }

        #endregion

    }

}
