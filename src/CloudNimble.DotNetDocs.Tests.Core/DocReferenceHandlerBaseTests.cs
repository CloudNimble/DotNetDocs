using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Tests for <see cref="DocReferenceHandlerBase"/>, covering the base file copying
    /// and exclusion pattern matching functionality.
    /// </summary>
    [TestClass]
    public class DocReferenceHandlerBaseTests : DotNetDocsTestBase
    {

        #region Private Fields

        private string? _tempDirectory;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"DocRefHandlerTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDirectory);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (_tempDirectory is not null && Directory.Exists(_tempDirectory))
            {
                try
                {
                    Directory.Delete(_tempDirectory, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors in tests
                }
            }
        }

        #endregion

        #region GetExclusionPatternsForDocumentationType Tests

        [TestMethod]
        public void GetExclusionPatternsForDocumentationType_Mintlify_ReturnsCorrectPatterns()
        {
            var handler = new TestDocReferenceHandler();

            var patterns = handler.TestGetExclusionPatternsForDocumentationType(SupportedDocumentationType.Mintlify);

            patterns.Should().NotBeNull();
            patterns.Should().Contain("**/*.mdz");
            patterns.Should().Contain("conceptual/**/*");
            patterns.Should().Contain("**/*.css");
            patterns.Should().Contain("docs.json");
            patterns.Should().Contain("assembly-list.txt");
            patterns.Should().Contain("*.docsproj");
        }

        [TestMethod]
        public void GetExclusionPatternsForDocumentationType_DocFX_ReturnsCorrectPatterns()
        {
            var handler = new TestDocReferenceHandler();

            var patterns = handler.TestGetExclusionPatternsForDocumentationType(SupportedDocumentationType.DocFX);

            patterns.Should().NotBeNull();
            patterns.Should().Contain("toc.yml");
            patterns.Should().Contain("toc.yaml");
            patterns.Should().Contain("docfx.json");
        }

        [TestMethod]
        public void GetExclusionPatternsForDocumentationType_MkDocs_ReturnsCorrectPatterns()
        {
            var handler = new TestDocReferenceHandler();

            var patterns = handler.TestGetExclusionPatternsForDocumentationType(SupportedDocumentationType.MkDocs);

            patterns.Should().NotBeNull();
            patterns.Should().Contain("mkdocs.yml");
        }

        [TestMethod]
        public void GetExclusionPatternsForDocumentationType_Jekyll_ReturnsCorrectPatterns()
        {
            var handler = new TestDocReferenceHandler();

            var patterns = handler.TestGetExclusionPatternsForDocumentationType(SupportedDocumentationType.Jekyll);

            patterns.Should().NotBeNull();
            patterns.Should().Contain("_config.yml");
            patterns.Should().Contain("_config.yaml");
        }

        [TestMethod]
        public void GetExclusionPatternsForDocumentationType_Hugo_ReturnsCorrectPatterns()
        {
            var handler = new TestDocReferenceHandler();

            var patterns = handler.TestGetExclusionPatternsForDocumentationType(SupportedDocumentationType.Hugo);

            patterns.Should().NotBeNull();
            patterns.Should().Contain("hugo.toml");
            patterns.Should().Contain("hugo.yaml");
            patterns.Should().Contain("hugo.json");
            patterns.Should().Contain("config.*");
        }

        [TestMethod]
        public void GetExclusionPatternsForDocumentationType_UnknownType_ReturnsEmptyList()
        {
            var handler = new TestDocReferenceHandler();

            var patterns = handler.TestGetExclusionPatternsForDocumentationType(SupportedDocumentationType.Generic);

            patterns.Should().NotBeNull();
            patterns.Should().BeEmpty();
        }

        #endregion

        #region ShouldExcludeFile Tests

        [TestMethod]
        public void ShouldExcludeFile_WithMdzPattern_ExcludesMdzFiles()
        {
            var handler = new TestDocReferenceHandler();
            var exclusionPatterns = new List<string> { "**/*.mdz" };

            handler.TestShouldExcludeFile("file.mdz", exclusionPatterns).Should().BeTrue();
            handler.TestShouldExcludeFile("api-reference/test.mdz", exclusionPatterns).Should().BeTrue();
            handler.TestShouldExcludeFile("deeply/nested/path/file.mdz", exclusionPatterns).Should().BeTrue();
            handler.TestShouldExcludeFile("file.md", exclusionPatterns).Should().BeFalse();
            handler.TestShouldExcludeFile("file.mdx", exclusionPatterns).Should().BeFalse();
        }

        [TestMethod]
        public void ShouldExcludeFile_WithConceptualPattern_ExcludesConceptualFolder()
        {
            var handler = new TestDocReferenceHandler();
            var exclusionPatterns = new List<string> { "conceptual/**/*" };

            handler.TestShouldExcludeFile("conceptual/guide.md", exclusionPatterns).Should().BeTrue();
            handler.TestShouldExcludeFile("conceptual/nested/file.mdx", exclusionPatterns).Should().BeTrue();
            handler.TestShouldExcludeFile("api-reference/file.md", exclusionPatterns).Should().BeFalse();
            handler.TestShouldExcludeFile("guide.md", exclusionPatterns).Should().BeFalse();
        }

        [TestMethod]
        public void ShouldExcludeFile_WithCssPattern_ExcludesCssFiles()
        {
            var handler = new TestDocReferenceHandler();
            var exclusionPatterns = new List<string> { "**/*.css" };

            handler.TestShouldExcludeFile("style.css", exclusionPatterns).Should().BeTrue();
            handler.TestShouldExcludeFile("assets/main.css", exclusionPatterns).Should().BeTrue();
            handler.TestShouldExcludeFile("deeply/nested/theme.css", exclusionPatterns).Should().BeTrue();
            handler.TestShouldExcludeFile("style.scss", exclusionPatterns).Should().BeFalse();
            handler.TestShouldExcludeFile("file.css.map", exclusionPatterns).Should().BeFalse();
        }

        [TestMethod]
        public void ShouldExcludeFile_WithDocsJsonPattern_ExcludesDocsJson()
        {
            var handler = new TestDocReferenceHandler();
            var exclusionPatterns = new List<string> { "docs.json" };

            handler.TestShouldExcludeFile("docs.json", exclusionPatterns).Should().BeTrue();
            handler.TestShouldExcludeFile("nested/docs.json", exclusionPatterns).Should().BeFalse(); // Exact match only
            handler.TestShouldExcludeFile("docs-template.json", exclusionPatterns).Should().BeFalse();
        }

        [TestMethod]
        public void ShouldExcludeFile_WithAssemblyListPattern_ExcludesAssemblyList()
        {
            var handler = new TestDocReferenceHandler();
            var exclusionPatterns = new List<string> { "assembly-list.txt" };

            handler.TestShouldExcludeFile("assembly-list.txt", exclusionPatterns).Should().BeTrue();
            handler.TestShouldExcludeFile("nested/assembly-list.txt", exclusionPatterns).Should().BeFalse(); // Exact match only
            handler.TestShouldExcludeFile("assembly-list-backup.txt", exclusionPatterns).Should().BeFalse();
        }

        [TestMethod]
        public void ShouldExcludeFile_WithDocsprojPattern_ExcludesDocsprojFiles()
        {
            var handler = new TestDocReferenceHandler();
            var exclusionPatterns = new List<string> { "*.docsproj" };

            handler.TestShouldExcludeFile("MyProject.docsproj", exclusionPatterns).Should().BeTrue();
            handler.TestShouldExcludeFile("Documentation.docsproj", exclusionPatterns).Should().BeTrue();
            handler.TestShouldExcludeFile("nested/Project.docsproj", exclusionPatterns).Should().BeTrue();
            handler.TestShouldExcludeFile("MyProject.csproj", exclusionPatterns).Should().BeFalse();
        }

        [TestMethod]
        public void ShouldExcludeFile_NonMatchingFile_DoesNotExclude()
        {
            var handler = new TestDocReferenceHandler();
            var exclusionPatterns = new List<string> { "**/*.mdz", "conceptual/**/*", "**/*.css", "docs.json", "assembly-list.txt", "*.docsproj" };

            handler.TestShouldExcludeFile("index.mdx", exclusionPatterns).Should().BeFalse();
            handler.TestShouldExcludeFile("api-reference/class.md", exclusionPatterns).Should().BeFalse();
            handler.TestShouldExcludeFile("snippets/example.jsx", exclusionPatterns).Should().BeFalse();
            handler.TestShouldExcludeFile("images/logo.png", exclusionPatterns).Should().BeFalse();
        }

        #endregion

        #region ShouldExcludeDirectory Tests

        [TestMethod]
        public void ShouldExcludeDirectory_WithConceptualPattern_ExcludesConceptualDirectory()
        {
            var handler = new TestDocReferenceHandler();
            var exclusionPatterns = new List<string> { "conceptual/**/*" };

            handler.TestShouldExcludeDirectory("conceptual", exclusionPatterns).Should().BeTrue();
            handler.TestShouldExcludeDirectory("conceptual/guides", exclusionPatterns).Should().BeTrue();
            handler.TestShouldExcludeDirectory("conceptual/guides/nested", exclusionPatterns).Should().BeTrue();
            handler.TestShouldExcludeDirectory("api-reference", exclusionPatterns).Should().BeFalse();
            handler.TestShouldExcludeDirectory("guides", exclusionPatterns).Should().BeFalse();
        }

        #endregion

        #region MatchesGlobPattern Tests

        [TestMethod]
        public void MatchesGlobPattern_WithVariousPatterns_MatchesCorrectly()
        {
            var handler = new TestDocReferenceHandler();

            // Test **/*.ext pattern
            handler.TestMatchesGlobPattern("file.mdz", "**/*.mdz").Should().BeTrue();
            handler.TestMatchesGlobPattern("path/file.mdz", "**/*.mdz").Should().BeTrue();
            handler.TestMatchesGlobPattern("deep/nested/file.mdz", "**/*.mdz").Should().BeTrue();
            handler.TestMatchesGlobPattern("file.md", "**/*.mdz").Should().BeFalse();

            // Test directory/**/* pattern
            handler.TestMatchesGlobPattern("conceptual/file.md", "conceptual/**/*").Should().BeTrue();
            handler.TestMatchesGlobPattern("conceptual/nested/file.md", "conceptual/**/*").Should().BeTrue();
            handler.TestMatchesGlobPattern("api-reference/file.md", "conceptual/**/*").Should().BeFalse();

            // Test exact match
            handler.TestMatchesGlobPattern("docs.json", "docs.json").Should().BeTrue();
            handler.TestMatchesGlobPattern("nested/docs.json", "docs.json").Should().BeFalse();

            // Test *.ext pattern
            handler.TestMatchesGlobPattern("file.css", "*.css").Should().BeTrue();
            handler.TestMatchesGlobPattern("path/file.css", "*.css").Should().BeTrue();
            handler.TestMatchesGlobPattern("file.scss", "*.css").Should().BeFalse();
        }

        #endregion

        #region CopyDirectoryWithExclusionsAsync Tests

        [TestMethod]
        public async Task CopyDirectoryWithExclusionsAsync_SkipsExcludedFiles()
        {
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");
            Directory.CreateDirectory(sourceDir);

            File.WriteAllText(Path.Combine(sourceDir, "index.mdx"), "content");
            File.WriteAllText(Path.Combine(sourceDir, "test.mdz"), "content");
            File.WriteAllText(Path.Combine(sourceDir, "style.css"), "content");
            File.WriteAllText(Path.Combine(sourceDir, "docs.json"), "content");

            var handler = new TestDocReferenceHandler();
            var exclusionPatterns = new List<string> { "**/*.mdz", "**/*.css", "docs.json" };

            await handler.TestCopyDirectoryWithExclusionsAsync(sourceDir, destDir, exclusionPatterns);

            File.Exists(Path.Combine(destDir, "index.mdx")).Should().BeTrue("index.mdx should be copied");
            File.Exists(Path.Combine(destDir, "test.mdz")).Should().BeFalse("test.mdz should be excluded");
            File.Exists(Path.Combine(destDir, "style.css")).Should().BeFalse("style.css should be excluded");
            File.Exists(Path.Combine(destDir, "docs.json")).Should().BeFalse("docs.json should be excluded");
        }

        [TestMethod]
        public async Task CopyDirectoryWithExclusionsAsync_CopiesNestedDirectories()
        {
            var sourceDir = Path.Combine(_tempDirectory!, "source2");
            var destDir = Path.Combine(_tempDirectory!, "dest2");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(Path.Combine(sourceDir, "api-reference"));
            Directory.CreateDirectory(Path.Combine(sourceDir, "conceptual"));

            File.WriteAllText(Path.Combine(sourceDir, "index.mdx"), "content");
            File.WriteAllText(Path.Combine(sourceDir, "api-reference", "class.md"), "content");
            File.WriteAllText(Path.Combine(sourceDir, "conceptual", "guide.md"), "content");

            var handler = new TestDocReferenceHandler();
            var exclusionPatterns = new List<string> { "conceptual/**/*" };

            await handler.TestCopyDirectoryWithExclusionsAsync(sourceDir, destDir, exclusionPatterns);

            File.Exists(Path.Combine(destDir, "index.mdx")).Should().BeTrue("index.mdx should be copied");
            File.Exists(Path.Combine(destDir, "api-reference", "class.md")).Should().BeTrue("api-reference/class.md should be copied");
            Directory.Exists(Path.Combine(destDir, "conceptual")).Should().BeFalse("conceptual directory should not be copied");
            File.Exists(Path.Combine(destDir, "conceptual", "guide.md")).Should().BeFalse("conceptual/guide.md should be excluded");
        }

        [TestMethod]
        public async Task CopyDirectoryWithExclusionsAsync_SkipsExistingFiles_WhenSkipExistingTrue()
        {
            var sourceDir = Path.Combine(_tempDirectory!, "source3");
            var destDir = Path.Combine(_tempDirectory!, "dest3");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destDir);

            File.WriteAllText(Path.Combine(sourceDir, "file.txt"), "new content");
            File.WriteAllText(Path.Combine(destDir, "file.txt"), "existing content");

            var handler = new TestDocReferenceHandler();

            await handler.TestCopyDirectoryWithExclusionsAsync(sourceDir, destDir, [], skipExisting: true);

            File.ReadAllText(Path.Combine(destDir, "file.txt")).Should().Be("existing content", "existing file should not be overwritten");
        }

        [TestMethod]
        public async Task CopyDirectoryWithExclusionsAsync_OverwritesFiles_WhenSkipExistingFalse()
        {
            var sourceDir = Path.Combine(_tempDirectory!, "source4");
            var destDir = Path.Combine(_tempDirectory!, "dest4");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destDir);

            File.WriteAllText(Path.Combine(sourceDir, "file.txt"), "new content");
            File.WriteAllText(Path.Combine(destDir, "file.txt"), "existing content");

            var handler = new TestDocReferenceHandler();

            await handler.TestCopyDirectoryWithExclusionsAsync(sourceDir, destDir, [], skipExisting: false);

            File.ReadAllText(Path.Combine(destDir, "file.txt")).Should().Be("new content", "existing file should be overwritten");
        }

        [TestMethod]
        public async Task CopyDirectoryWithExclusionsAsync_SourceDirectoryMissing_ReturnsWithoutError()
        {
            var sourceDir = Path.Combine(_tempDirectory!, "nonexistent");
            var destDir = Path.Combine(_tempDirectory!, "dest5");

            var handler = new TestDocReferenceHandler();

            await handler.TestCopyDirectoryWithExclusionsAsync(sourceDir, destDir, []);

            Directory.Exists(destDir).Should().BeFalse("destination should not be created when source doesn't exist");
        }

        #endregion

        #region Test Helper Class

        /// <summary>
        /// Concrete implementation of <see cref="DocReferenceHandlerBase"/> for testing.
        /// Exposes protected methods as public for test access.
        /// </summary>
        private class TestDocReferenceHandler : DocReferenceHandlerBase
        {
            public override SupportedDocumentationType DocumentationType => SupportedDocumentationType.Generic;

            public override Task ProcessAsync(DocumentationReference reference, string documentationRootPath)
            {
                return Task.CompletedTask;
            }

            public List<string> TestGetExclusionPatternsForDocumentationType(SupportedDocumentationType documentationType)
            {
                return GetExclusionPatternsForDocumentationType(documentationType);
            }

            public bool TestShouldExcludeFile(string relativePath, List<string> exclusionPatterns)
            {
                return ShouldExcludeFile(relativePath, exclusionPatterns);
            }

            public bool TestShouldExcludeDirectory(string relativePath, List<string> exclusionPatterns)
            {
                return ShouldExcludeDirectory(relativePath, exclusionPatterns);
            }

            public bool TestMatchesGlobPattern(string path, string pattern)
            {
                return MatchesGlobPattern(path, pattern);
            }

            public Task TestCopyDirectoryWithExclusionsAsync(string sourceDir, string destDir, List<string> exclusionPatterns, bool skipExisting = true)
            {
                return CopyDirectoryWithExclusionsAsync(sourceDir, destDir, exclusionPatterns, skipExisting);
            }
        }

        #endregion

    }

}
