using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Tests for DocumentationManager, which orchestrates the documentation pipeline.
    /// </summary>
    [TestClass]
    public class DocumentationManagerTests : DotNetDocsTestBase
    {

        #region Private Fields

        private string? _tempDirectory;
        private string? _testAssemblyPath;
        private string? _testXmlPath;

        [TestMethod]
        public void GetFilePatternsForDocumentationType_Mintlify_ReturnsCorrectPatterns()
        {
            // Arrange
            var manager = GetDocumentationManager();

            // Act
            var patterns = manager.GetFilePatternsForDocumentationType("Mintlify");

            // Assert
            patterns.Should().NotBeNull();
            patterns.Should().Contain("*.md");
            patterns.Should().Contain("*.mdx");
            patterns.Should().Contain("*.mdz");
            patterns.Should().Contain("docs.json");
            patterns.Should().Contain("images/**/*");
            patterns.Should().Contain("favicon.*");
            patterns.Should().Contain("snippets/**/*");
        }

        [TestMethod]
        public void GetFilePatternsForDocumentationType_DocFX_ReturnsCorrectPatterns()
        {
            // Arrange
            var manager = GetDocumentationManager();

            // Act
            var patterns = manager.GetFilePatternsForDocumentationType("DocFX");

            // Assert
            patterns.Should().NotBeNull();
            patterns.Should().Contain("*.md");
            patterns.Should().Contain("*.yml");
            patterns.Should().Contain("toc.yml");
            patterns.Should().Contain("docfx.json");
            patterns.Should().Contain("images/**/*");
        }

        [TestMethod]
        public void GetFilePatternsForDocumentationType_MkDocs_ReturnsCorrectPatterns()
        {
            // Arrange
            var manager = GetDocumentationManager();

            // Act
            var patterns = manager.GetFilePatternsForDocumentationType("MkDocs");

            // Assert
            patterns.Should().NotBeNull();
            patterns.Should().Contain("*.md");
            patterns.Should().Contain("mkdocs.yml");
            patterns.Should().Contain("docs/**/*");
        }

        [TestMethod]
        public void GetFilePatternsForDocumentationType_Unknown_ReturnsDefaultPatterns()
        {
            // Arrange
            var manager = GetDocumentationManager();

            // Act
            var patterns = manager.GetFilePatternsForDocumentationType("Unknown");

            // Assert
            patterns.Should().NotBeNull();
            patterns.Should().Contain("*.md");
            patterns.Should().Contain("*.html");
            patterns.Should().Contain("images/**/*");
            patterns.Should().Contain("assets/**/*");
        }

        [TestMethod]
        public async Task CopyFilesAsync_SimplePattern_CopiesMatchingFiles()
        {
            // Arrange
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(sourceDir);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "test1.md"), "Test content 1");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "test2.md"), "Test content 2");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "test.txt"), "Not copied");

            // Act
            await manager.CopyFilesAsync(sourceDir, destDir, "*.md", skipExisting: true);

            // Assert
            File.Exists(Path.Combine(destDir, "test1.md")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "test2.md")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "test.txt")).Should().BeFalse();
        }

        [TestMethod]
        public async Task CopyFilesAsync_SkipExisting_PreservesExistingFiles()
        {
            // Arrange
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destDir);

            await File.WriteAllTextAsync(Path.Combine(sourceDir, "test.md"), "New content");
            await File.WriteAllTextAsync(Path.Combine(destDir, "test.md"), "Original content");

            // Act
            await manager.CopyFilesAsync(sourceDir, destDir, "*.md", skipExisting: true);

            // Assert
            var content = await File.ReadAllTextAsync(Path.Combine(destDir, "test.md"));
            content.Should().Be("Original content", "existing files should not be overwritten when skipExisting is true");
        }

        [TestMethod]
        public async Task CopyDirectoryRecursiveAsync_PreservesStructure()
        {
            // Arrange
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(Path.Combine(sourceDir, "subdir1"));
            Directory.CreateDirectory(Path.Combine(sourceDir, "subdir2"));
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "root.md"), "Root file");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "subdir1", "file1.md"), "File 1");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "subdir2", "file2.md"), "File 2");

            // Act
            await manager.CopyDirectoryRecursiveAsync(sourceDir, destDir, "*", skipExisting: true);

            // Assert
            File.Exists(Path.Combine(destDir, "root.md")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "subdir1", "file1.md")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "subdir2", "file2.md")).Should().BeTrue();
        }

        [TestMethod]
        public async Task CopyReferencedDocumentationAsync_ProcessesAllReferences()
        {
            // Arrange
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();

            var sourceDir1 = Path.Combine(_tempDirectory!, "ref1");
            var sourceDir2 = Path.Combine(_tempDirectory!, "ref2");
            Directory.CreateDirectory(sourceDir1);
            Directory.CreateDirectory(sourceDir2);

            await File.WriteAllTextAsync(Path.Combine(sourceDir1, "test1.md"), "Content 1");
            await File.WriteAllTextAsync(Path.Combine(sourceDir2, "test2.md"), "Content 2");

            context.DocumentationReferences.Add(new DocumentationReference
            {
                DocumentationRoot = sourceDir1,
                DestinationPath = "ref1",
                DocumentationType = "Mintlify"
            });

            context.DocumentationReferences.Add(new DocumentationReference
            {
                DocumentationRoot = sourceDir2,
                DestinationPath = "ref2",
                DocumentationType = "Mintlify"
            });

            // Act
            await manager.CopyReferencedDocumentationAsync();

            // Assert
            File.Exists(Path.Combine(context.DocumentationRootPath, "ref1", "test1.md")).Should().BeTrue();
            File.Exists(Path.Combine(context.DocumentationRootPath, "ref2", "test2.md")).Should().BeTrue();
        }

        #endregion

        #region IsTodoPlaceholderFile Tests

        [TestMethod]
        public void IsTodoPlaceholderFile_ValidTodoComment_ReturnsTrue()
        {
            var content = "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->";

            var result = DocumentationManager.IsTodoPlaceholderFile(content);

            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsTodoPlaceholderFile_ValidTodoCommentWithLeadingWhitespace_ReturnsTrue()
        {
            var content = "   \n\t\n  <!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->";

            var result = DocumentationManager.IsTodoPlaceholderFile(content);

            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsTodoPlaceholderFile_ValidTodoCommentMixedCase_ReturnsTrue()
        {
            var content = "<!-- TODO: Remove This Comment After You Customize This Content -->";

            var result = DocumentationManager.IsTodoPlaceholderFile(content);

            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsTodoPlaceholderFile_ValidTodoCommentExtraSpaces_ReturnsTrue()
        {
            var content = "<!--  TODO:  REMOVE  THIS  COMMENT  AFTER  YOU  CUSTOMIZE  THIS  CONTENT  -->";

            var result = DocumentationManager.IsTodoPlaceholderFile(content);

            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsTodoPlaceholderFile_EmptyString_ReturnsFalse()
        {
            var content = "";

            var result = DocumentationManager.IsTodoPlaceholderFile(content);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsTodoPlaceholderFile_WhitespaceOnly_ReturnsFalse()
        {
            var content = "   \n\t\n  ";

            var result = DocumentationManager.IsTodoPlaceholderFile(content);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsTodoPlaceholderFile_NullString_ReturnsFalse()
        {
            string? content = null;

            var result = DocumentationManager.IsTodoPlaceholderFile(content!);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsTodoPlaceholderFile_TodoCommentNotAtStart_ReturnsFalse()
        {
            var content = "Some content\n<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->";

            var result = DocumentationManager.IsTodoPlaceholderFile(content);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsTodoPlaceholderFile_MalformedTodoComment_ReturnsFalse()
        {
            var content = "<!-- TODO REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->";

            var result = DocumentationManager.IsTodoPlaceholderFile(content);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsTodoPlaceholderFile_MissingAfterYouCustomize_ReturnsFalse()
        {
            var content = "<!-- TODO: REMOVE THIS COMMENT -->";

            var result = DocumentationManager.IsTodoPlaceholderFile(content);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsTodoPlaceholderFile_MissingColons_ReturnsFalse()
        {
            var content = "<!-- TODO REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->";

            var result = DocumentationManager.IsTodoPlaceholderFile(content);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsTodoPlaceholderFile_NoSpacesInComment_ReturnsFalse()
        {
            var content = "<!--TODO:REMOVETHISCOMMENTAFTERYOUCUSTOMIZETHISCONTENT-->";

            var result = DocumentationManager.IsTodoPlaceholderFile(content);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsTodoPlaceholderFile_TodoCommentWithContentAfter_ReturnsTrue()
        {
            var content = "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nSome other content here";

            var result = DocumentationManager.IsTodoPlaceholderFile(content);

            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsTodoPlaceholderFile_MultipleTodoComments_ReturnsTrue()
        {
            var content = "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\n<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->";

            var result = DocumentationManager.IsTodoPlaceholderFile(content);

            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsTodoPlaceholderFile_RegularComment_ReturnsFalse()
        {
            var content = "<!-- This is a regular comment -->";

            var result = DocumentationManager.IsTodoPlaceholderFile(content);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsTodoPlaceholderFile_UnicodeContent_HandlesCorrectly()
        {
            var content = "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nSome Unicode: こんにちは";

            var result = DocumentationManager.IsTodoPlaceholderFile(content);

            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsTodoPlaceholderFile_MultilineContent_ChecksFirstLine()
        {
            var content = "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nLine 2\nLine 3\nLine 4";

            var result = DocumentationManager.IsTodoPlaceholderFile(content);

            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsTodoPlaceholderFile_Lowercase_ReturnsTrue()
        {
            var content = "<!-- todo: remove this comment after you customize this content -->";

            var result = DocumentationManager.IsTodoPlaceholderFile(content);

            result.Should().BeTrue();
        }

        #endregion

        #region Merge Tests - MergeDocAssembliesAsync

        [TestMethod]
        public async Task MergeDocAssembliesAsync_EmptyList_ThrowsArgumentException()
        {
            var manager = GetDocumentationManager();
            var emptyList = new List<DocAssembly>();

            Func<Task> act = async () => await manager.MergeDocAssembliesAsync(emptyList);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task MergeDocAssembliesAsync_SingleAssembly_ReturnsSameAssembly()
        {
            var manager = GetDocumentationManager();
            var assembly = await GetSingleTestAssembly();

            var result = await manager.MergeDocAssembliesAsync([assembly]);

            result.Should().BeSameAs(assembly);
        }

        [TestMethod]
        public async Task MergeDocAssembliesAsync_TwoAssembliesDistinctNamespaces_ContainsBothNamespaces()
        {
            var manager = GetDocumentationManager();
            var assembly1 = await GetSingleTestAssembly();
            var assembly2 = await GetSingleTestAssembly();

            var ns1 = assembly1.Namespaces.First(ns => ns.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");
            var ns2 = assembly2.Namespaces.First(ns => ns.Name != ns1.Name);

            assembly1.Namespaces.Clear();
            assembly1.Namespaces.Add(ns1);

            assembly2.Namespaces.Clear();
            assembly2.Namespaces.Add(ns2);

            var result = await manager.MergeDocAssembliesAsync([assembly1, assembly2]);

            result.Namespaces.Should().HaveCount(2);
            result.Namespaces.Should().Contain(ns => ns.Symbol.ToDisplayString() == ns1.Symbol.ToDisplayString());
            result.Namespaces.Should().Contain(ns => ns.Symbol.ToDisplayString() == ns2.Symbol.ToDisplayString());
        }

        [TestMethod]
        public async Task MergeDocAssembliesAsync_NullList_ThrowsArgumentNullException()
        {
            var manager = GetDocumentationManager();

            Func<Task> act = async () => await manager.MergeDocAssembliesAsync(null!);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task MergeDocAssembliesAsync_AssembliesWithNoNamespaces_ReturnsFirst()
        {
            var manager = GetDocumentationManager();
            var assembly1 = await GetSingleTestAssembly();
            var assembly2 = await GetSingleTestAssembly();

            assembly1.Namespaces.Clear();
            assembly2.Namespaces.Clear();

            var result = await manager.MergeDocAssembliesAsync([assembly1, assembly2]);

            result.Should().BeSameAs(assembly1);
            result.Namespaces.Should().BeEmpty();
        }

        #endregion

        #region Merge Tests - MergeNamespaceAsync

        [TestMethod]
        public async Task MergeNamespaceAsync_NewNamespace_AddsToAssembly()
        {
            var manager = GetDocumentationManager();
            var targetAssembly = await GetSingleTestAssembly();
            var sourceAssembly = await GetSingleTestAssembly();

            var nsToAdd = sourceAssembly.Namespaces.First(ns => ns.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");
            targetAssembly.Namespaces.Clear();
            var initialCount = targetAssembly.Namespaces.Count;

            await manager.MergeNamespaceAsync(targetAssembly, nsToAdd);

            targetAssembly.Namespaces.Should().HaveCount(initialCount + 1);
            targetAssembly.Namespaces.Should().Contain(ns => ns.Symbol.ToDisplayString() == nsToAdd.Symbol.ToDisplayString());
        }

        [TestMethod]
        public async Task MergeNamespaceAsync_ExistingNamespace_MergesTypes()
        {
            var manager = GetDocumentationManager();
            var targetAssembly = await GetSingleTestAssembly();
            var sourceAssembly = await GetSingleTestAssembly();

            var targetNs = targetAssembly.Namespaces.First(ns => ns.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");
            var sourceNs = sourceAssembly.Namespaces.First(ns => ns.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");

            var simpleClass = targetNs.Types.First(t => t.Name == "SimpleClass");
            targetNs.Types.Clear();
            targetNs.Types.Add(simpleClass);

            var baseClass = sourceNs.Types.First(t => t.Name == "BaseClass");
            sourceNs.Types.Clear();
            sourceNs.Types.Add(baseClass);

            await manager.MergeNamespaceAsync(targetAssembly, sourceNs);

            var mergedNs = targetAssembly.Namespaces.First(ns => ns.Symbol.ToDisplayString() == targetNs.Symbol.ToDisplayString());
            mergedNs.Types.Should().HaveCount(2);
            mergedNs.Types.Should().Contain(t => t.Name == "SimpleClass");
            mergedNs.Types.Should().Contain(t => t.Name == "BaseClass");
        }

        [TestMethod]
        public async Task MergeNamespaceAsync_SourceHasSummaryTargetEmpty_CopiesSummary()
        {
            var manager = GetDocumentationManager();
            var targetAssembly = await GetSingleTestAssembly();
            var sourceNs = targetAssembly.Namespaces.First();

            var targetNs = new DocNamespace(sourceNs.Symbol);
            targetAssembly.Namespaces.Clear();
            targetAssembly.Namespaces.Add(targetNs);

            sourceNs.Summary = "Source summary content";
            targetNs.Summary = null;

            await manager.MergeNamespaceAsync(targetAssembly, sourceNs);

            var merged = targetAssembly.Namespaces.First();
            merged.Summary.Should().Be("Source summary content");
        }

        [TestMethod]
        public async Task MergeNamespaceAsync_BothHaveSummary_KeepsTargetSummary()
        {
            var manager = GetDocumentationManager();
            var targetAssembly = await GetSingleTestAssembly();
            var sourceNs = targetAssembly.Namespaces.First();

            var targetNs = new DocNamespace(sourceNs.Symbol)
            {
                Summary = "Target summary - should be kept"
            };
            targetAssembly.Namespaces.Clear();
            targetAssembly.Namespaces.Add(targetNs);

            sourceNs.Summary = "Source summary - should be ignored";

            await manager.MergeNamespaceAsync(targetAssembly, sourceNs);

            var merged = targetAssembly.Namespaces.First();
            merged.Summary.Should().Be("Target summary - should be kept");
        }

        [TestMethod]
        public async Task MergeNamespaceAsync_NullMergedAssembly_ThrowsArgumentNullException()
        {
            var manager = GetDocumentationManager();
            var assembly = await GetSingleTestAssembly();
            var ns = assembly.Namespaces.First();

            Func<Task> act = async () => await manager.MergeNamespaceAsync(null!, ns);

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("mergedAssembly");
        }

        [TestMethod]
        public async Task MergeNamespaceAsync_NullSourceNamespace_ThrowsArgumentNullException()
        {
            var manager = GetDocumentationManager();
            var assembly = await GetSingleTestAssembly();

            Func<Task> act = async () => await manager.MergeNamespaceAsync(assembly, null!);

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("sourceNamespace");
        }

        #endregion

        #region Merge Tests - MergeTypeAsync

        [TestMethod]
        public async Task MergeTypeAsync_NewType_AddsToNamespace()
        {
            var manager = GetDocumentationManager();
            var assembly = await GetSingleTestAssembly();
            var ns = assembly.Namespaces.First(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");

            var typeToAdd = ns.Types.First(t => t.Name == "SimpleClass");
            ns.Types.Clear();

            await manager.MergeTypeAsync(ns, typeToAdd);

            ns.Types.Should().HaveCount(1);
            ns.Types.Should().Contain(t => t.Symbol.ToDisplayString() == typeToAdd.Symbol.ToDisplayString());
        }

        [TestMethod]
        public async Task MergeTypeAsync_ExistingType_MergesMembers()
        {
            var manager = GetDocumentationManager();
            var assembly = await GetSingleTestAssembly();
            var ns = assembly.Namespaces.First(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");

            var targetType = ns.Types.First(t => t.Name == "SimpleClass");
            var sourceType = ns.Types.First(t => t.Name == "SimpleClass");

            var member1 = targetType.Members.FirstOrDefault();
            if (member1 is not null)
            {
                targetType.Members.Clear();
                targetType.Members.Add(member1);
            }

            var member2 = sourceType.Members.Skip(1).FirstOrDefault();
            if (member2 is not null)
            {
                sourceType.Members.Clear();
                sourceType.Members.Add(member2);
            }

            ns.Types.Clear();
            ns.Types.Add(targetType);

            await manager.MergeTypeAsync(ns, sourceType);

            var merged = ns.Types.First();
            if (member1 is not null && member2 is not null)
            {
                merged.Members.Should().HaveCount(2);
                merged.Members.Should().Contain(m => m.Symbol.ToDisplayString() == member1.Symbol.ToDisplayString());
                merged.Members.Should().Contain(m => m.Symbol.ToDisplayString() == member2.Symbol.ToDisplayString());
            }
        }

        [TestMethod]
        public async Task MergeTypeAsync_SourceHasSummaryTargetEmpty_CopiesSummary()
        {
            var manager = GetDocumentationManager();
            var assembly = await GetSingleTestAssembly();
            var ns = assembly.Namespaces.First(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");

            var sourceType = ns.Types.First(t => t.Name == "SimpleClass");
            var targetType = new DocType(sourceType.Symbol);

            ns.Types.Clear();
            ns.Types.Add(targetType);

            sourceType.Summary = "Source type summary";
            targetType.Summary = null;

            await manager.MergeTypeAsync(ns, sourceType);

            var merged = ns.Types.First();
            merged.Summary.Should().Be("Source type summary");
        }

        [TestMethod]
        public async Task MergeTypeAsync_BothHaveSummary_KeepsTargetSummary()
        {
            var manager = GetDocumentationManager();
            var assembly = await GetSingleTestAssembly();
            var ns = assembly.Namespaces.First(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");

            var sourceType = ns.Types.First(t => t.Name == "SimpleClass");
            var targetType = new DocType(sourceType.Symbol)
            {
                Summary = "Target type summary - keep this"
            };

            ns.Types.Clear();
            ns.Types.Add(targetType);

            sourceType.Summary = "Source type summary - ignore this";

            await manager.MergeTypeAsync(ns, sourceType);

            var merged = ns.Types.First();
            merged.Summary.Should().Be("Target type summary - keep this");
        }

        [TestMethod]
        public async Task MergeTypeAsync_NullMergedNamespace_ThrowsArgumentNullException()
        {
            var manager = GetDocumentationManager();
            var assembly = await GetSingleTestAssembly();
            var type = assembly.Namespaces.First().Types.First();

            Func<Task> act = async () => await manager.MergeTypeAsync(null!, type);

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("mergedNamespace");
        }

        [TestMethod]
        public async Task MergeTypeAsync_NullSourceType_ThrowsArgumentNullException()
        {
            var manager = GetDocumentationManager();
            var assembly = await GetSingleTestAssembly();
            var ns = assembly.Namespaces.First();

            Func<Task> act = async () => await manager.MergeTypeAsync(ns, null!);

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("sourceType");
        }

        #endregion

        #region Merge Tests - MergeMemberAsync

        [TestMethod]
        public async Task MergeMemberAsync_NewMember_AddsToType()
        {
            var manager = GetDocumentationManager();
            var assembly = await GetSingleTestAssembly();
            var type = assembly.Namespaces
                .First(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios")
                .Types.First(t => t.Name == "SimpleClass");

            var memberToAdd = type.Members.First();
            type.Members.Clear();

            await manager.MergeMemberAsync(type, memberToAdd);

            type.Members.Should().HaveCount(1);
            type.Members.Should().Contain(m => m.Symbol.ToDisplayString() == memberToAdd.Symbol.ToDisplayString());
        }

        [TestMethod]
        public async Task MergeMemberAsync_DuplicateMember_SkipsAddition()
        {
            var manager = GetDocumentationManager();
            var assembly = await GetSingleTestAssembly();
            var type = assembly.Namespaces
                .First(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios")
                .Types.First(t => t.Name == "SimpleClass");

            var member = type.Members.First();
            type.Members.Clear();
            type.Members.Add(member);

            await manager.MergeMemberAsync(type, member);

            type.Members.Should().HaveCount(1, "duplicate member should not be added");
        }

        [TestMethod]
        public async Task MergeMemberAsync_NullMergedType_ThrowsArgumentNullException()
        {
            var manager = GetDocumentationManager();
            var assembly = await GetSingleTestAssembly();
            var member = assembly.Namespaces.First().Types.First().Members.First();

            Func<Task> act = async () => await manager.MergeMemberAsync(null!, member);

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("mergedType");
        }

        [TestMethod]
        public async Task MergeMemberAsync_NullSourceMember_ThrowsArgumentNullException()
        {
            var manager = GetDocumentationManager();
            var assembly = await GetSingleTestAssembly();
            var type = assembly.Namespaces.First().Types.First();

            Func<Task> act = async () => await manager.MergeMemberAsync(type, null!);

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("sourceMember");
        }

        #endregion

        #region ProcessAsync Tests

        [TestMethod]
        public async Task ProcessAsync_SingleAssembly_CompletesSuccessfully()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualDocsEnabled = false;

            await manager.ProcessAsync(_testAssemblyPath!, _testXmlPath!);

            manager.Should().NotBeNull();
        }

        [TestMethod]
        public async Task ProcessAsync_MultipleAssemblies_MergesCorrectly()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualDocsEnabled = false;

            var assemblies = new[]
            {
                (_testAssemblyPath!, _testXmlPath!),
                (_testAssemblyPath!, _testXmlPath!)
            };

            await manager.ProcessAsync(assemblies);

            manager.Should().NotBeNull();
        }

        [TestMethod]
        public async Task ProcessAsync_ConceptualDocsDisabled_SkipsConceptualLoading()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualDocsEnabled = false;
            context.ConceptualPath = Path.Combine(_tempDirectory!, "nonexistent");

            await manager.ProcessAsync(_testAssemblyPath!, _testXmlPath!);

            Directory.Exists(context.ConceptualPath).Should().BeFalse("conceptual loading should be skipped");
        }

        [TestMethod]
        public async Task ProcessAsync_ConceptualDocsEnabled_LoadsConceptual()
        {
            await CreateConceptualContentAsync();

            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualDocsEnabled = true;
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");

            await manager.ProcessAsync(_testAssemblyPath!, _testXmlPath!);

            Directory.Exists(context.ConceptualPath).Should().BeTrue();
        }

        [TestMethod]
        public async Task ProcessAsync_WithDocumentationReferences_CopiesReferenceFiles()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualDocsEnabled = false;

            var sourceDir = Path.Combine(_tempDirectory!, "refSource");
            Directory.CreateDirectory(sourceDir);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "test.md"), "Reference content");

            context.DocumentationReferences.Add(new DocumentationReference
            {
                DocumentationRoot = sourceDir,
                DestinationPath = Path.Combine(context.DocumentationRootPath, "references"),
                DocumentationType = "Mintlify"
            });

            await manager.ProcessAsync(_testAssemblyPath!, _testXmlPath!);

            context.DocumentationReferences.Should().HaveCount(1);
        }

        [TestMethod]
        public async Task ProcessAsync_WithEnrichers_AppliesEnrichment()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualDocsEnabled = false;

            await manager.ProcessAsync(_testAssemblyPath!, _testXmlPath!);

            manager.Should().NotBeNull();
        }

        [TestMethod]
        public async Task ProcessAsync_WithTransformers_AppliesTransformations()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualDocsEnabled = false;

            await manager.ProcessAsync(_testAssemblyPath!, _testXmlPath!);

            manager.Should().NotBeNull();
        }

        [TestMethod]
        public async Task ProcessAsync_WithRenderers_GeneratesOutput()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualDocsEnabled = false;

            await manager.ProcessAsync(_testAssemblyPath!, _testXmlPath!);

            manager.Should().NotBeNull();
        }

        [TestMethod]
        public async Task ProcessAsync_EmptyAssemblyList_CompletesWithoutError()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualDocsEnabled = false;

            var emptyAssemblies = Array.Empty<(string assemblyPath, string xmlPath)>();

            Func<Task> act = async () => await manager.ProcessAsync(emptyAssemblies);

            await act.Should().ThrowAsync<ArgumentException>("empty assembly list should throw");
        }

        #endregion

        #region LoadConceptualAsync Tests

        [TestMethod]
        public async Task LoadConceptualAsync_MissingConceptualDirectory_ReturnsWithoutError()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "nonexistent");

            var assembly = await GetSingleTestAssembly();

            await manager.LoadConceptualAsync(assembly);

            Directory.Exists(context.ConceptualPath).Should().BeFalse();
        }

        [TestMethod]
        public async Task LoadConceptualAsync_GlobalNamespace_LoadsContentCorrectly()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");

            Directory.CreateDirectory(context.ConceptualPath);
            await File.WriteAllTextAsync(Path.Combine(context.ConceptualPath, "summary.mdz"), "Global namespace summary");

            var assembly = await GetSingleTestAssembly();

            await manager.LoadConceptualAsync(assembly);

            var globalNs = assembly.Namespaces.FirstOrDefault(ns => ns.Symbol.IsGlobalNamespace);
            if (globalNs is not null)
            {
                globalNs.Summary.Should().Be("Global namespace summary");
            }
        }

        [TestMethod]
        public async Task LoadConceptualAsync_NamespaceRelatedApis_ParsesCorrectly()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");

            var nsPath = Path.Combine(context.ConceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios");
            Directory.CreateDirectory(nsPath);

            var relatedApisContent = @"System.Collections.Generic.List<T>
System.Linq.Enumerable
<!-- This is a comment -->
System.String";
            await File.WriteAllTextAsync(Path.Combine(nsPath, "related-apis.mdz"), relatedApisContent);

            var assembly = await GetSingleTestAssembly();

            await manager.LoadConceptualAsync(assembly);

            var ns = assembly.Namespaces.FirstOrDefault(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");
            ns.Should().NotBeNull();
            ns!.RelatedApis.Should().NotBeNull();
            ns.RelatedApis.Should().HaveCount(3);
            ns.RelatedApis.Should().Contain("System.Collections.Generic.List<T>");
            ns.RelatedApis.Should().Contain("System.Linq.Enumerable");
            ns.RelatedApis.Should().Contain("System.String");
            ns.RelatedApis.Should().NotContain(line => line.Contains("<!--"));
        }

        [TestMethod]
        public async Task LoadConceptualAsync_TypeRelatedApis_ParsesCorrectly()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");

            var typePath = Path.Combine(context.ConceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios", "SimpleClass");
            Directory.CreateDirectory(typePath);

            var relatedApisContent = @"CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.BaseClass
CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.DerivedClass";
            await File.WriteAllTextAsync(Path.Combine(typePath, "related-apis.mdz"), relatedApisContent);

            var assembly = await GetSingleTestAssembly();

            await manager.LoadConceptualAsync(assembly);

            var type = assembly.Namespaces
                .FirstOrDefault(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios")
                ?.Types.FirstOrDefault(t => t.Name == "SimpleClass");

            type.Should().NotBeNull();
            type!.RelatedApis.Should().NotBeNull();
            type.RelatedApis.Should().HaveCount(2);
        }

        [TestMethod]
        public async Task LoadConceptualAsync_MemberRelatedApis_ParsesCorrectly()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");

            var assembly = await GetSingleTestAssembly();
            var type = assembly.Namespaces
                .FirstOrDefault(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios")
                ?.Types.FirstOrDefault(t => t.Name == "SimpleClass");

            type.Should().NotBeNull();

            var member = type!.Members.FirstOrDefault();
            if (member is not null)
            {
                var memberDir = Path.Combine(context.ConceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios", "SimpleClass", member.Symbol.Name);
                Directory.CreateDirectory(memberDir);

                var relatedApisContent = "System.String.Empty\nSystem.String.IsNullOrWhiteSpace";
                await File.WriteAllTextAsync(Path.Combine(memberDir, "related-apis.mdz"), relatedApisContent);

                await manager.LoadConceptualAsync(assembly);

                member.RelatedApis.Should().NotBeNull();
                member.RelatedApis.Should().HaveCount(2);
            }
        }

        [TestMethod]
        public async Task LoadConceptualAsync_RelatedApisPlaceholder_SkipsWhenShowPlaceholdersFalse()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            context.ShowPlaceholders = false;

            var nsPath = Path.Combine(context.ConceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios");
            Directory.CreateDirectory(nsPath);

            var placeholderContent = "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->";
            await File.WriteAllTextAsync(Path.Combine(nsPath, "related-apis.mdz"), placeholderContent);

            var assembly = await GetSingleTestAssembly();

            await manager.LoadConceptualAsync(assembly);

            var ns = assembly.Namespaces.FirstOrDefault(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");
            ns.Should().NotBeNull();
            ns!.RelatedApis.Should().BeNull("placeholder should be skipped when ShowPlaceholders is false");
        }

        [TestMethod]
        public async Task LoadConceptualAsync_AllConceptualFileTypes_LoadsAll()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");

            var nsPath = Path.Combine(context.ConceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios");
            Directory.CreateDirectory(nsPath);

            await File.WriteAllTextAsync(Path.Combine(nsPath, "summary.mdz"), "Summary content");
            await File.WriteAllTextAsync(Path.Combine(nsPath, "usage.mdz"), "Usage content");
            await File.WriteAllTextAsync(Path.Combine(nsPath, "examples.mdz"), "Examples content");
            await File.WriteAllTextAsync(Path.Combine(nsPath, "best-practices.mdz"), "Best practices content");
            await File.WriteAllTextAsync(Path.Combine(nsPath, "patterns.mdz"), "Patterns content");
            await File.WriteAllTextAsync(Path.Combine(nsPath, "considerations.mdz"), "Considerations content");

            var assembly = await GetSingleTestAssembly();

            await manager.LoadConceptualAsync(assembly);

            var ns = assembly.Namespaces.FirstOrDefault(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");
            ns.Should().NotBeNull();
            ns!.Summary.Should().Be("Summary content");
            ns.Usage.Should().Be("Usage content");
            ns.Examples.Should().Be("Examples content");
            ns.BestPractices.Should().Be("Best practices content");
            ns.Patterns.Should().Be("Patterns content");
            ns.Considerations.Should().Be("Considerations content");
        }

        [TestMethod]
        public async Task LoadConceptualAsync_TypeConceptualFiles_LoadsAll()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");

            var typePath = Path.Combine(context.ConceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios", "SimpleClass");
            Directory.CreateDirectory(typePath);

            await File.WriteAllTextAsync(Path.Combine(typePath, "usage.mdz"), "Type usage");
            await File.WriteAllTextAsync(Path.Combine(typePath, "examples.mdz"), "Type examples");
            await File.WriteAllTextAsync(Path.Combine(typePath, "best-practices.mdz"), "Type best practices");
            await File.WriteAllTextAsync(Path.Combine(typePath, "patterns.mdz"), "Type patterns");
            await File.WriteAllTextAsync(Path.Combine(typePath, "considerations.mdz"), "Type considerations");

            var assembly = await GetSingleTestAssembly();

            await manager.LoadConceptualAsync(assembly);

            var type = assembly.Namespaces
                .FirstOrDefault(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios")
                ?.Types.FirstOrDefault(t => t.Name == "SimpleClass");

            type.Should().NotBeNull();
            type!.Usage.Should().Be("Type usage");
            type.Examples.Should().Be("Type examples");
            type.BestPractices.Should().Be("Type best practices");
            type.Patterns.Should().Be("Type patterns");
            type.Considerations.Should().Be("Type considerations");
        }

        #endregion

        #region LoadConceptualFileAsync Tests

        [TestMethod]
        public async Task LoadConceptualFileAsync_FileWithBOM_RemovesBOM()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            var testDir = Path.Combine(_tempDirectory!, "bomTest");
            Directory.CreateDirectory(testDir);

            var contentWithBom = "\uFEFFTest content with BOM";
            await File.WriteAllTextAsync(Path.Combine(testDir, "test.md"), contentWithBom);

            string? loadedContent = null;
            await manager.LoadConceptualFileAsync(testDir, "test.md", content => loadedContent = content);

            loadedContent.Should().Be("Test content with BOM", "BOM should be removed");
            loadedContent.Should().NotStartWith("\uFEFF");
        }

        [TestMethod]
        public async Task LoadConceptualFileAsync_PlaceholderFile_SkipsWhenShowPlaceholdersFalse()
        {
            var manager = GetDocumentationManager();
            var testDir = Path.Combine(_tempDirectory!, "placeholderTest");
            Directory.CreateDirectory(testDir);

            var placeholderContent = "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->";
            await File.WriteAllTextAsync(Path.Combine(testDir, "test.md"), placeholderContent);

            string? loadedContent = null;
            await manager.LoadConceptualFileAsync(testDir, "test.md", content => loadedContent = content, showPlaceholders: false);

            loadedContent.Should().BeNull("placeholder should be skipped when showPlaceholders is false");
        }

        [TestMethod]
        public async Task LoadConceptualFileAsync_PlaceholderFile_LoadsWhenShowPlaceholdersTrue()
        {
            var manager = GetDocumentationManager();
            var testDir = Path.Combine(_tempDirectory!, "placeholderTest");
            Directory.CreateDirectory(testDir);

            var placeholderContent = "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->";
            await File.WriteAllTextAsync(Path.Combine(testDir, "test.md"), placeholderContent);

            string? loadedContent = null;
            await manager.LoadConceptualFileAsync(testDir, "test.md", content => loadedContent = content, showPlaceholders: true);

            loadedContent.Should().Be(placeholderContent, "placeholder should be loaded when showPlaceholders is true");
        }

        [TestMethod]
        public async Task LoadConceptualFileAsync_EmptyFile_DoesNotCallSetter()
        {
            var manager = GetDocumentationManager();
            var testDir = Path.Combine(_tempDirectory!, "emptyTest");
            Directory.CreateDirectory(testDir);

            await File.WriteAllTextAsync(Path.Combine(testDir, "empty.md"), "");

            bool setterCalled = false;
            await manager.LoadConceptualFileAsync(testDir, "empty.md", content => setterCalled = true);

            setterCalled.Should().BeFalse("setter should not be called for empty file");
        }

        [TestMethod]
        public async Task LoadConceptualFileAsync_WhitespaceOnlyFile_DoesNotCallSetter()
        {
            var manager = GetDocumentationManager();
            var testDir = Path.Combine(_tempDirectory!, "whitespaceTest");
            Directory.CreateDirectory(testDir);

            await File.WriteAllTextAsync(Path.Combine(testDir, "whitespace.md"), "   \n\t\r\n   ");

            bool setterCalled = false;
            await manager.LoadConceptualFileAsync(testDir, "whitespace.md", content => setterCalled = true);

            setterCalled.Should().BeFalse("setter should not be called for whitespace-only file");
        }

        [TestMethod]
        public async Task LoadConceptualFileAsync_FileDoesNotExist_DoesNothing()
        {
            var manager = GetDocumentationManager();
            var testDir = Path.Combine(_tempDirectory!, "nonexistentTest");
            Directory.CreateDirectory(testDir);

            bool setterCalled = false;
            await manager.LoadConceptualFileAsync(testDir, "nonexistent.md", content => setterCalled = true);

            setterCalled.Should().BeFalse("setter should not be called when file doesn't exist");
        }

        [TestMethod]
        public async Task LoadConceptualFileAsync_ValidContent_TrimsWhitespace()
        {
            var manager = GetDocumentationManager();
            var testDir = Path.Combine(_tempDirectory!, "trimTest");
            Directory.CreateDirectory(testDir);

            var contentWithWhitespace = "\n\n  Content with whitespace  \n\n";
            await File.WriteAllTextAsync(Path.Combine(testDir, "trim.md"), contentWithWhitespace);

            string? loadedContent = null;
            await manager.LoadConceptualFileAsync(testDir, "trim.md", content => loadedContent = content);

            loadedContent.Should().Be("Content with whitespace", "leading and trailing whitespace should be trimmed");
        }

        [TestMethod]
        public async Task LoadConceptualFileAsync_ContentWithBOMAndWhitespace_RemovesBothCorrectly()
        {
            var manager = GetDocumentationManager();
            var testDir = Path.Combine(_tempDirectory!, "bomWhitespaceTest");
            Directory.CreateDirectory(testDir);

            var contentWithBomAndWhitespace = "\uFEFF  \n\nActual content\n\n  ";
            await File.WriteAllTextAsync(Path.Combine(testDir, "combined.md"), contentWithBomAndWhitespace);

            string? loadedContent = null;
            await manager.LoadConceptualFileAsync(testDir, "combined.md", content => loadedContent = content);

            loadedContent.Should().Be("Actual content");
            loadedContent.Should().NotStartWith("\uFEFF");
            loadedContent.Should().NotStartWith(" ");
            loadedContent.Should().NotEndWith(" ");
        }

        [TestMethod]
        public async Task LoadConceptualFileAsync_PlaceholderWithLeadingWhitespace_DetectsPlaceholder()
        {
            var manager = GetDocumentationManager();
            var testDir = Path.Combine(_tempDirectory!, "placeholderWhitespaceTest");
            Directory.CreateDirectory(testDir);

            var placeholderWithWhitespace = "  \n  <!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->  \n";
            await File.WriteAllTextAsync(Path.Combine(testDir, "placeholder.md"), placeholderWithWhitespace);

            string? loadedContent = null;
            await manager.LoadConceptualFileAsync(testDir, "placeholder.md", content => loadedContent = content, showPlaceholders: false);

            loadedContent.Should().BeNull("placeholder with leading whitespace should still be detected");
        }

        [TestMethod]
        public async Task LoadConceptualFileAsync_NonPlaceholderContent_LoadsNormally()
        {
            var manager = GetDocumentationManager();
            var testDir = Path.Combine(_tempDirectory!, "normalTest");
            Directory.CreateDirectory(testDir);

            var normalContent = "This is normal content\nWith multiple lines\nAnd no placeholder";
            await File.WriteAllTextAsync(Path.Combine(testDir, "normal.md"), normalContent);

            string? loadedContent = null;
            await manager.LoadConceptualFileAsync(testDir, "normal.md", content => loadedContent = content, showPlaceholders: false);

            loadedContent.Should().Be("This is normal content\nWith multiple lines\nAnd no placeholder");
        }

        #endregion

        #region GetOrCreateAssemblyManager Tests

        [TestMethod]
        public void GetOrCreateAssemblyManager_FirstCall_CreatesNewManager()
        {
            var manager = GetDocumentationManager();

            var assemblyManager = manager.GetOrCreateAssemblyManager(_testAssemblyPath!, _testXmlPath!);

            assemblyManager.Should().NotBeNull();
        }

        [TestMethod]
        public void GetOrCreateAssemblyManager_SecondCallSamePath_ReturnsCachedManager()
        {
            var manager = GetDocumentationManager();

            var first = manager.GetOrCreateAssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var second = manager.GetOrCreateAssemblyManager(_testAssemblyPath!, _testXmlPath!);

            second.Should().BeSameAs(first, "second call should return cached instance");
        }

        [TestMethod]
        public void GetOrCreateAssemblyManager_DifferentPaths_CreatesDistinctManagers()
        {
            var manager = GetDocumentationManager();

            var first = manager.GetOrCreateAssemblyManager(_testAssemblyPath!, _testXmlPath!);

            var tempAssemblyPath = Path.Combine(_tempDirectory!, "temp.dll");
            File.Copy(_testAssemblyPath!, tempAssemblyPath, overwrite: true);

            var second = manager.GetOrCreateAssemblyManager(tempAssemblyPath, _testXmlPath!);

            second.Should().NotBeSameAs(first, "different paths should create different managers");
        }

        [TestMethod]
        public void GetOrCreateAssemblyManager_MultipleCalls_MaintainsCache()
        {
            var manager = GetDocumentationManager();

            var first = manager.GetOrCreateAssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var second = manager.GetOrCreateAssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var third = manager.GetOrCreateAssemblyManager(_testAssemblyPath!, _testXmlPath!);

            first.Should().BeSameAs(second);
            second.Should().BeSameAs(third);
            first.Should().BeSameAs(third);
        }

        [TestMethod]
        public void GetOrCreateAssemblyManager_CaseInsensitivePaths_TreatsAsDistinct()
        {
            var manager = GetDocumentationManager();

            var lowerPath = _testAssemblyPath!.ToLowerInvariant();
            var upperPath = _testAssemblyPath!.ToUpperInvariant();

            if (lowerPath != upperPath)
            {
                var first = manager.GetOrCreateAssemblyManager(lowerPath, _testXmlPath!);
                var second = manager.GetOrCreateAssemblyManager(upperPath, _testXmlPath!);

                first.Should().NotBeSameAs(second, "cache should be case-sensitive on file paths");
            }
        }

        #endregion

        #region Constructor Tests

        [TestMethod]
        public void Constructor_NullProjectContext_ThrowsArgumentNullException()
        {
            Action act = () => new DocumentationManager(null!);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("projectContext");
        }

        [TestMethod]
        public void Constructor_OnlyProjectContext_InitializesWithEmptyCollections()
        {
            var context = GetService<ProjectContext>();

            var manager = new DocumentationManager(context);

            manager.Should().NotBeNull();
        }

        [TestMethod]
        public void Constructor_NullEnrichers_InitializesWithEmptyCollection()
        {
            var context = GetService<ProjectContext>();

            var manager = new DocumentationManager(context, enrichers: null);

            manager.Should().NotBeNull();
        }

        [TestMethod]
        public void Constructor_NullTransformers_InitializesWithEmptyCollection()
        {
            var context = GetService<ProjectContext>();

            var manager = new DocumentationManager(context, transformers: null);

            manager.Should().NotBeNull();
        }

        [TestMethod]
        public void Constructor_NullRenderers_InitializesWithEmptyCollection()
        {
            var context = GetService<ProjectContext>();

            var manager = new DocumentationManager(context, renderers: null);

            manager.Should().NotBeNull();
        }

        [TestMethod]
        public void Constructor_AllParametersNull_OnlyThrowsForProjectContext()
        {
            Action act = () => new DocumentationManager(null!, null, null, null);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("projectContext");
        }

        #endregion

        #region CreateConceptualFilesAsync Tests

        [TestMethod]
        public async Task CreateConceptualFilesAsync_SingleAssembly_CreatesPlaceholders()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualDocsEnabled = true;

            await manager.CreateConceptualFilesAsync(_testAssemblyPath!, _testXmlPath!);

            manager.Should().NotBeNull();
        }

        [TestMethod]
        public async Task CreateConceptualFilesAsync_MultipleAssemblies_CreatesAllPlaceholders()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualDocsEnabled = true;

            var assemblies = new[]
            {
                (_testAssemblyPath!, _testXmlPath!),
                (_testAssemblyPath!, _testXmlPath!)
            };

            await manager.CreateConceptualFilesAsync(assemblies);

            manager.Should().NotBeNull();
        }

        [TestMethod]
        public async Task CreateConceptualFilesAsync_EmptyAssemblyList_CompletesWithoutError()
        {
            var manager = GetDocumentationManager();

            var emptyAssemblies = Array.Empty<(string assemblyPath, string xmlPath)>();

            await manager.CreateConceptualFilesAsync(emptyAssemblies);

            manager.Should().NotBeNull();
        }

        [TestMethod]
        public async Task CreateConceptualFilesAsync_InvalidAssemblyPath_ThrowsException()
        {
            var manager = GetDocumentationManager();

            Func<Task> act = async () => await manager.CreateConceptualFilesAsync("nonexistent.dll", "nonexistent.xml");

            await act.Should().ThrowAsync<Exception>();
        }

        [TestMethod]
        public async Task CreateConceptualFilesAsync_UsesAssemblyManagerCache()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualDocsEnabled = true;

            await manager.CreateConceptualFilesAsync(_testAssemblyPath!, _testXmlPath!);
            await manager.CreateConceptualFilesAsync(_testAssemblyPath!, _testXmlPath!);

            var cachedManager = manager.GetOrCreateAssemblyManager(_testAssemblyPath!, _testXmlPath!);
            cachedManager.Should().NotBeNull("assembly manager should be cached");
        }

        [TestMethod]
        public async Task CreateConceptualFilesAsync_ParallelExecution_CompletesSuccessfully()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualDocsEnabled = true;

            var assemblies = Enumerable.Repeat((_testAssemblyPath!, _testXmlPath!), 5);

            await manager.CreateConceptualFilesAsync(assemblies);

            manager.Should().NotBeNull();
        }

        #endregion

        #region CopyFilesAsync Additional Tests

        [TestMethod]
        public async Task CopyFilesAsync_RecursivePattern_CopiesNestedFiles()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(Path.Combine(sourceDir, "images", "icons"));
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "images", "logo.png"), "logo");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "images", "icons", "check.png"), "check");

            await manager.CopyFilesAsync(sourceDir, destDir, "images/**/*", skipExisting: true);

            File.Exists(Path.Combine(destDir, "images", "logo.png")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "images", "icons", "check.png")).Should().BeTrue();
        }

        [TestMethod]
        public async Task CopyFilesAsync_SourceDirectoryMissing_ReturnsWithoutError()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "nonexistent");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            await manager.CopyFilesAsync(sourceDir, destDir, "*.md", skipExisting: true);

            Directory.Exists(destDir).Should().BeFalse("destination should not be created when source doesn't exist");
        }

        [TestMethod]
        public async Task CopyFilesAsync_SkipExistingFalse_OverwritesFiles()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destDir);

            await File.WriteAllTextAsync(Path.Combine(sourceDir, "test.md"), "New content");
            await File.WriteAllTextAsync(Path.Combine(destDir, "test.md"), "Original content");

            await manager.CopyFilesAsync(sourceDir, destDir, "*.md", skipExisting: false);

            var content = await File.ReadAllTextAsync(Path.Combine(destDir, "test.md"));
            content.Should().Be("New content", "file should be overwritten when skipExisting is false");
        }

        [TestMethod]
        public async Task CopyFilesAsync_MultiplePatternMatches_CopiesAll()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(sourceDir);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "file1.md"), "content1");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "file2.md"), "content2");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "file3.md"), "content3");

            await manager.CopyFilesAsync(sourceDir, destDir, "*.md", skipExisting: true);

            Directory.GetFiles(destDir, "*.md").Should().HaveCount(3);
        }

        [TestMethod]
        public async Task CopyFilesAsync_SpecificFileName_CopiesSingleFile()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(sourceDir);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "docs.json"), "{}");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "other.json"), "{}");

            await manager.CopyFilesAsync(sourceDir, destDir, "docs.json", skipExisting: true);

            File.Exists(Path.Combine(destDir, "docs.json")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "other.json")).Should().BeFalse();
        }

        [TestMethod]
        public async Task CopyFilesAsync_NoMatchingFiles_CreatesEmptyDestination()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(sourceDir);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "test.txt"), "content");

            await manager.CopyFilesAsync(sourceDir, destDir, "*.md", skipExisting: true);

            Directory.Exists(destDir).Should().BeTrue("destination directory should be created");
            Directory.GetFiles(destDir).Should().BeEmpty("no files should match the pattern");
        }

        [TestMethod]
        public async Task CopyFilesAsync_RecursivePatternWithSubdirectory_CopiesCorrectStructure()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            var imagesDir = Path.Combine(sourceDir, "images");
            var iconsDir = Path.Combine(imagesDir, "icons");
            var badgesDir = Path.Combine(imagesDir, "badges");

            Directory.CreateDirectory(iconsDir);
            Directory.CreateDirectory(badgesDir);

            await File.WriteAllTextAsync(Path.Combine(iconsDir, "icon1.png"), "icon1");
            await File.WriteAllTextAsync(Path.Combine(iconsDir, "icon2.svg"), "icon2");
            await File.WriteAllTextAsync(Path.Combine(badgesDir, "badge.png"), "badge");

            await manager.CopyFilesAsync(sourceDir, destDir, "images/**/*", skipExisting: true);

            File.Exists(Path.Combine(destDir, "images", "icons", "icon1.png")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "images", "icons", "icon2.svg")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "images", "badges", "badge.png")).Should().BeTrue();
        }

        [TestMethod]
        public async Task CopyFilesAsync_InvalidRecursivePattern_HandlesGracefully()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(sourceDir);

            await manager.CopyFilesAsync(sourceDir, destDir, "**/**/**/*", skipExisting: true);

            Directory.Exists(destDir).Should().BeTrue();
        }

        [TestMethod]
        public async Task CopyFilesAsync_LargeNumberOfFiles_CopiesAll()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(sourceDir);

            for (int i = 0; i < 50; i++)
            {
                await File.WriteAllTextAsync(Path.Combine(sourceDir, $"file{i}.md"), $"content{i}");
            }

            await manager.CopyFilesAsync(sourceDir, destDir, "*.md", skipExisting: true);

            Directory.GetFiles(destDir, "*.md").Should().HaveCount(50);
        }

        [TestMethod]
        public async Task CopyFilesAsync_MixedExtensions_CopiesOnlyMatching()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(sourceDir);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "file.md"), "md");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "file.mdx"), "mdx");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "file.txt"), "txt");

            await manager.CopyFilesAsync(sourceDir, destDir, "*.md", skipExisting: true);

            File.Exists(Path.Combine(destDir, "file.md")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "file.mdx")).Should().BeFalse();
            File.Exists(Path.Combine(destDir, "file.txt")).Should().BeFalse();
        }

        #endregion

        #region CopyDirectoryRecursiveAsync Additional Tests

        [TestMethod]
        public async Task CopyDirectoryRecursiveAsync_SourceDirectoryMissing_ReturnsWithoutError()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "nonexistent");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            await manager.CopyDirectoryRecursiveAsync(sourceDir, destDir, "*", skipExisting: true);

            Directory.Exists(destDir).Should().BeFalse();
        }

        [TestMethod]
        public async Task CopyDirectoryRecursiveAsync_SkipExistingFalse_OverwritesFiles()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(Path.Combine(sourceDir, "sub"));
            Directory.CreateDirectory(Path.Combine(destDir, "sub"));

            await File.WriteAllTextAsync(Path.Combine(sourceDir, "sub", "file.md"), "New content");
            await File.WriteAllTextAsync(Path.Combine(destDir, "sub", "file.md"), "Original content");

            await manager.CopyDirectoryRecursiveAsync(sourceDir, destDir, "*", skipExisting: false);

            var content = await File.ReadAllTextAsync(Path.Combine(destDir, "sub", "file.md"));
            content.Should().Be("New content");
        }

        [TestMethod]
        public async Task CopyDirectoryRecursiveAsync_EmptyDirectory_CreatesStructure()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(Path.Combine(sourceDir, "empty1"));
            Directory.CreateDirectory(Path.Combine(sourceDir, "empty2", "nested"));

            await manager.CopyDirectoryRecursiveAsync(sourceDir, destDir, "*", skipExisting: true);

            Directory.Exists(Path.Combine(destDir, "empty1")).Should().BeTrue();
            Directory.Exists(Path.Combine(destDir, "empty2", "nested")).Should().BeTrue();
        }

        [TestMethod]
        public async Task CopyDirectoryRecursiveAsync_PatternFilter_CopiesOnlyMatchingFiles()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(Path.Combine(sourceDir, "sub"));
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "test.md"), "md");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "test.txt"), "txt");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "sub", "nested.md"), "nested md");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "sub", "nested.txt"), "nested txt");

            await manager.CopyDirectoryRecursiveAsync(sourceDir, destDir, "*.md", skipExisting: true);

            File.Exists(Path.Combine(destDir, "test.md")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "test.txt")).Should().BeFalse();
            File.Exists(Path.Combine(destDir, "sub", "nested.md")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "sub", "nested.txt")).Should().BeFalse();
        }

        [TestMethod]
        public async Task CopyDirectoryRecursiveAsync_DeepNesting_PreservesStructure()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            var deepPath = Path.Combine(sourceDir, "level1", "level2", "level3", "level4");
            Directory.CreateDirectory(deepPath);
            await File.WriteAllTextAsync(Path.Combine(deepPath, "deep.md"), "deep content");

            await manager.CopyDirectoryRecursiveAsync(sourceDir, destDir, "*", skipExisting: true);

            File.Exists(Path.Combine(destDir, "level1", "level2", "level3", "level4", "deep.md")).Should().BeTrue();
        }

        [TestMethod]
        public async Task CopyDirectoryRecursiveAsync_MixedContentTypes_CopiesAll()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(Path.Combine(sourceDir, "docs"));
            Directory.CreateDirectory(Path.Combine(sourceDir, "images"));
            Directory.CreateDirectory(Path.Combine(sourceDir, "scripts"));

            await File.WriteAllTextAsync(Path.Combine(sourceDir, "docs", "readme.md"), "readme");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "images", "logo.png"), "logo");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "scripts", "build.js"), "build");

            await manager.CopyDirectoryRecursiveAsync(sourceDir, destDir, "*", skipExisting: true);

            File.Exists(Path.Combine(destDir, "docs", "readme.md")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "images", "logo.png")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "scripts", "build.js")).Should().BeTrue();
        }

        [TestMethod]
        public async Task CopyDirectoryRecursiveAsync_OnlyRootFiles_CopiesWithoutSubdirectories()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(sourceDir);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "file1.md"), "content1");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "file2.md"), "content2");

            await manager.CopyDirectoryRecursiveAsync(sourceDir, destDir, "*", skipExisting: true);

            File.Exists(Path.Combine(destDir, "file1.md")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "file2.md")).Should().BeTrue();
            Directory.GetDirectories(destDir).Should().BeEmpty();
        }

        [TestMethod]
        public async Task CopyDirectoryRecursiveAsync_SkipExistingTrue_PreservesExistingSubdirectoryFiles()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");

            Directory.CreateDirectory(Path.Combine(sourceDir, "sub"));
            Directory.CreateDirectory(Path.Combine(destDir, "sub"));

            await File.WriteAllTextAsync(Path.Combine(sourceDir, "sub", "new.md"), "new");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "sub", "existing.md"), "source version");
            await File.WriteAllTextAsync(Path.Combine(destDir, "sub", "existing.md"), "dest version");

            await manager.CopyDirectoryRecursiveAsync(sourceDir, destDir, "*", skipExisting: true);

            var newContent = await File.ReadAllTextAsync(Path.Combine(destDir, "sub", "new.md"));
            var existingContent = await File.ReadAllTextAsync(Path.Combine(destDir, "sub", "existing.md"));

            newContent.Should().Be("new");
            existingContent.Should().Be("dest version", "existing file should not be overwritten");
        }

        #endregion

        #region CopyReferencedDocumentationAsync Additional Tests

        [TestMethod]
        public async Task CopyReferencedDocumentationAsync_NoReferences_CompletesWithoutError()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.DocumentationReferences.Clear();

            await manager.CopyReferencedDocumentationAsync();

            context.DocumentationReferences.Should().BeEmpty();
        }

        [TestMethod]
        public async Task CopyReferencedDocumentationAsync_MultipleReferences_CopiesAll()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.DocumentationReferences.Clear();

            var source1 = Path.Combine(_tempDirectory!, "ref1");
            var source2 = Path.Combine(_tempDirectory!, "ref2");
            Directory.CreateDirectory(source1);
            Directory.CreateDirectory(source2);

            await File.WriteAllTextAsync(Path.Combine(source1, "test1.md"), "ref1");
            await File.WriteAllTextAsync(Path.Combine(source2, "test2.md"), "ref2");

            context.DocumentationReferences.Add(new DocumentationReference
            {
                DocumentationRoot = source1,
                DestinationPath = "dest1",
                DocumentationType = "Mintlify"
            });

            context.DocumentationReferences.Add(new DocumentationReference
            {
                DocumentationRoot = source2,
                DestinationPath = "dest2",
                DocumentationType = "Mintlify"
            });

            await manager.CopyReferencedDocumentationAsync();

            File.Exists(Path.Combine(context.DocumentationRootPath, "dest1", "test1.md")).Should().BeTrue();
            File.Exists(Path.Combine(context.DocumentationRootPath, "dest2", "test2.md")).Should().BeTrue();
        }

        [TestMethod]
        public async Task CopyReferencedDocumentationAsync_DifferentDocumentationTypes_UsesCorrectPatterns()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.DocumentationReferences.Clear();

            var mintlifySource = Path.Combine(_tempDirectory!, "mintlify");
            var docfxSource = Path.Combine(_tempDirectory!, "docfx");

            Directory.CreateDirectory(mintlifySource);
            Directory.CreateDirectory(docfxSource);

            await File.WriteAllTextAsync(Path.Combine(mintlifySource, "test.mdx"), "mintlify");
            await File.WriteAllTextAsync(Path.Combine(docfxSource, "test.yml"), "docfx");

            context.DocumentationReferences.Add(new DocumentationReference
            {
                DocumentationRoot = mintlifySource,
                DestinationPath = "mintlify",
                DocumentationType = "Mintlify"
            });

            context.DocumentationReferences.Add(new DocumentationReference
            {
                DocumentationRoot = docfxSource,
                DestinationPath = "docfx",
                DocumentationType = "DocFX"
            });

            await manager.CopyReferencedDocumentationAsync();

            context.DocumentationReferences.Should().HaveCount(2);
        }

        [TestMethod]
        public async Task CopyReferencedDocumentationAsync_MissingSourceDirectory_ContinuesProcessing()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.DocumentationReferences.Clear();

            var validSource = Path.Combine(_tempDirectory!, "valid");
            Directory.CreateDirectory(validSource);
            await File.WriteAllTextAsync(Path.Combine(validSource, "test.md"), "content");

            context.DocumentationReferences.Add(new DocumentationReference
            {
                DocumentationRoot = Path.Combine(_tempDirectory!, "nonexistent"),
                DestinationPath = "missing",
                DocumentationType = "Mintlify"
            });

            context.DocumentationReferences.Add(new DocumentationReference
            {
                DocumentationRoot = validSource,
                DestinationPath = "valid",
                DocumentationType = "Mintlify"
            });

            await manager.CopyReferencedDocumentationAsync();

            File.Exists(Path.Combine(context.DocumentationRootPath, "valid", "test.md")).Should().BeTrue();
        }

        #endregion

        #region GetFilePatternsForDocumentationType Additional Tests

        [TestMethod]
        public void GetFilePatternsForDocumentationType_CaseSensitive_ReturnsCorrectPatterns()
        {
            var manager = GetDocumentationManager();

            var lowerCase = manager.GetFilePatternsForDocumentationType("mintlify");
            var mixedCase = manager.GetFilePatternsForDocumentationType("MiNtLiFy");

            lowerCase.Should().NotBeNull();
            mixedCase.Should().NotBeNull();
        }

        [TestMethod]
        public void GetFilePatternsForDocumentationType_NullOrEmpty_ReturnsDefaultPatterns()
        {
            var manager = GetDocumentationManager();

            var nullResult = manager.GetFilePatternsForDocumentationType(null!);
            var emptyResult = manager.GetFilePatternsForDocumentationType("");

            nullResult.Should().NotBeNull();
            emptyResult.Should().NotBeNull();
        }

        [TestMethod]
        public void GetFilePatternsForDocumentationType_AllKnownTypes_ReturnDistinctPatterns()
        {
            var manager = GetDocumentationManager();

            var mintlify = manager.GetFilePatternsForDocumentationType("Mintlify");
            var docfx = manager.GetFilePatternsForDocumentationType("DocFX");
            var mkdocs = manager.GetFilePatternsForDocumentationType("MkDocs");

            mintlify.Should().Contain("*.mdx");
            docfx.Should().Contain("*.yml");
            mkdocs.Should().Contain("mkdocs.yml");

            mintlify.Should().NotEqual(docfx);
            docfx.Should().NotEqual(mkdocs);
        }

        #endregion

        #region Dispose Tests

        [TestMethod]
        public void Dispose_WithCachedAssemblyManagers_DisposesAll()
        {
            var manager = GetDocumentationManager();

            manager.GetOrCreateAssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var tempAssemblyPath = Path.Combine(_tempDirectory!, "temp.dll");
            File.Copy(_testAssemblyPath!, tempAssemblyPath, overwrite: true);
            manager.GetOrCreateAssemblyManager(tempAssemblyPath, _testXmlPath!);

            manager.Dispose();

            manager.Should().NotBeNull("Dispose should complete without error");
        }

        [TestMethod]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            var manager = GetDocumentationManager();
            manager.GetOrCreateAssemblyManager(_testAssemblyPath!, _testXmlPath!);

            manager.Dispose();

            var act = () => manager.Dispose();
            act.Should().NotThrow("Dispose should be idempotent");
        }

        [TestMethod]
        public void Dispose_WithEmptyCache_CompletesSuccessfully()
        {
            var manager = GetDocumentationManager();

            var act = () => manager.Dispose();

            act.Should().NotThrow("Dispose should handle empty cache gracefully");
        }

        [TestMethod]
        public void Dispose_ClearsAssemblyManagerCache()
        {
            var manager = GetDocumentationManager();
            manager.GetOrCreateAssemblyManager(_testAssemblyPath!, _testXmlPath!);

            manager.Dispose();

            var secondManager = manager.GetOrCreateAssemblyManager(_testAssemblyPath!, _testXmlPath!);
            secondManager.Should().NotBeNull("GetOrCreateAssemblyManager should work after Dispose");
        }

        [TestMethod]
        public void Dispose_WithNullManagerInCache_HandlesGracefully()
        {
            var manager = GetDocumentationManager();
            manager.GetOrCreateAssemblyManager(_testAssemblyPath!, _testXmlPath!);

            var act = () => manager.Dispose();

            act.Should().NotThrow("Dispose should handle null values in cache gracefully");
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        public async Task ProcessAsync_EndToEndWithConceptual_CompletesSuccessfully()
        {
            var context = GetService<ProjectContext>();
            context.ConceptualDocsEnabled = true;

            var renderer = new TestRenderer("TestRenderer", context);
            var manager = new DocumentationManager(context, renderers: [renderer]);

            await manager.ProcessAsync(_testAssemblyPath!, _testXmlPath!);

            Directory.Exists(context.DocumentationRootPath).Should().BeTrue();
        }

        [TestMethod]
        public async Task ProcessAsync_MultipleAssembliesWithReferences_MergesCorrectly()
        {
            var context = GetService<ProjectContext>();

            var renderer = new TestRenderer("TestRenderer", context);
            var manager = new DocumentationManager(context, renderers: [renderer]);

            var assemblies = new[]
            {
                (_testAssemblyPath!, _testXmlPath!),
                (_testAssemblyPath!, _testXmlPath!)
            };

            await manager.ProcessAsync(assemblies);

            Directory.Exists(context.DocumentationRootPath).Should().BeTrue();
        }

        [TestMethod]
        public async Task ProcessAsync_WithDocumentationReferences_CopiesFiles()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();

            var sourceDir = Path.Combine(_tempDirectory!, "reference-source");
            Directory.CreateDirectory(sourceDir);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "test.mdx"), "# Test");

            context.DocumentationReferences.Add(new DocumentationReference
            {
                DocumentationRoot = sourceDir,
                DestinationPath = "references",
                DocumentationType = "Mintlify"
            });

            await manager.ProcessAsync(_testAssemblyPath!, _testXmlPath!);

            var destFile = Path.Combine(context.DocumentationRootPath, "references", "test.mdx");
            File.Exists(destFile).Should().BeTrue("referenced documentation should be copied");
        }

        [TestMethod]
        public async Task CreateConceptualFilesAsync_ThenProcessAsync_DoesNotDuplicate()
        {
            var context = GetService<ProjectContext>();
            context.ConceptualDocsEnabled = true;

            var renderer = new TestRenderer("TestRenderer", context);
            var manager = new DocumentationManager(context, renderers: [renderer]);

            await manager.CreateConceptualFilesAsync(_testAssemblyPath!, _testXmlPath!);
            await manager.ProcessAsync(_testAssemblyPath!, _testXmlPath!);

            Directory.Exists(context.DocumentationRootPath).Should().BeTrue();
        }

        [TestMethod]
        public async Task ProcessAsync_WithConceptualDisabled_SkipsConceptual()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualDocsEnabled = false;

            await manager.ProcessAsync(_testAssemblyPath!, _testXmlPath!);

            manager.Should().NotBeNull("ProcessAsync should complete even with conceptual disabled");
        }

        #endregion

        #region Edge Case Tests

        [TestMethod]
        public async Task MergeDocAssembliesAsync_WithIdenticalAssemblies_ProducesCorrectMerge()
        {
            var manager = GetDocumentationManager();
            var assembly1 = await GetSingleTestAssembly();
            var assembly2 = await GetSingleTestAssembly();

            var merged = await manager.MergeDocAssembliesAsync(new List<DocAssembly> { assembly1, assembly2 });

            merged.Should().NotBeNull();
            merged.Namespaces.Should().NotBeEmpty();
            merged.Namespaces.Count.Should().Be(assembly1.Namespaces.Count, "identical assemblies should not duplicate namespaces");
        }

        [TestMethod]
        public async Task CopyFilesAsync_WithSymlinksInSource_HandlesCorrectly()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "symlink-source");
            var destDir = Path.Combine(_tempDirectory!, "symlink-dest");
            Directory.CreateDirectory(sourceDir);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "test.md"), "content");

            await manager.CopyFilesAsync(sourceDir, destDir, "*.md", skipExisting: true);

            File.Exists(Path.Combine(destDir, "test.md")).Should().BeTrue();
        }

        [TestMethod]
        public async Task CopyDirectoryRecursiveAsync_WithCircularReferences_DoesNotInfiniteLoop()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "circular-source");
            var destDir = Path.Combine(_tempDirectory!, "circular-dest");
            Directory.CreateDirectory(sourceDir);
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "test.md"), "content");

            await manager.CopyDirectoryRecursiveAsync(sourceDir, destDir, "*.md", skipExisting: true);

            File.Exists(Path.Combine(destDir, "test.md")).Should().BeTrue();
        }

        [TestMethod]
        public async Task LoadConceptualFileAsync_WithBOMAndWhitespace_ParsesCorrectly()
        {
            var manager = GetDocumentationManager();
            var directory = Path.Combine(_tempDirectory!, "bom-test");
            Directory.CreateDirectory(directory);

            var content = "\uFEFF   \r\n  Test Content  \r\n   ";
            await File.WriteAllTextAsync(Path.Combine(directory, "test.md"), content, new System.Text.UTF8Encoding(true));

            string? result = null;
            await manager.LoadConceptualFileAsync(directory, "test.md", s => result = s, showPlaceholders: false);

            result.Should().NotBeNullOrWhiteSpace("content should be parsed despite BOM and whitespace");
            result.Should().Be("Test Content", "BOM and surrounding whitespace should be trimmed");
        }

        [TestMethod]
        public void GetFilePatternsForDocumentationType_WithUnknownType_ReturnsDefaultPatterns()
        {
            var manager = GetDocumentationManager();

            var patterns = manager.GetFilePatternsForDocumentationType("UnknownDocType123");

            patterns.Should().NotBeEmpty();
            patterns.Should().Contain("*.md", "default patterns should include markdown");
            patterns.Should().Contain("*.html", "default patterns should include html");
        }

        [TestMethod]
        public async Task CopyReferencedDocumentationAsync_WithOverlappingDestinations_HandlesCorrectly()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();

            var source1 = Path.Combine(_tempDirectory!, "ref1");
            var source2 = Path.Combine(_tempDirectory!, "ref2");
            Directory.CreateDirectory(source1);
            Directory.CreateDirectory(source2);
            await File.WriteAllTextAsync(Path.Combine(source1, "file1.md"), "content1");
            await File.WriteAllTextAsync(Path.Combine(source2, "file2.md"), "content2");

            context.DocumentationReferences.Add(new DocumentationReference
            {
                DocumentationRoot = source1,
                DestinationPath = "shared",
                DocumentationType = "Mintlify"
            });
            context.DocumentationReferences.Add(new DocumentationReference
            {
                DocumentationRoot = source2,
                DestinationPath = "shared",
                DocumentationType = "Mintlify"
            });

            await manager.CopyReferencedDocumentationAsync();

            File.Exists(Path.Combine(context.DocumentationRootPath, "shared", "file1.md")).Should().BeTrue();
            File.Exists(Path.Combine(context.DocumentationRootPath, "shared", "file2.md")).Should().BeTrue();
        }

        #endregion

        #region Error Handling Tests

        [TestMethod]
        public async Task CopyFilesAsync_WithReadOnlyDestinationFile_OverwritesFails()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "readonly-source");
            var destDir = Path.Combine(_tempDirectory!, "readonly-dest");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destDir);

            await File.WriteAllTextAsync(Path.Combine(sourceDir, "test.md"), "new content");
            var destFile = Path.Combine(destDir, "test.md");
            await File.WriteAllTextAsync(destFile, "old content");
            File.SetAttributes(destFile, FileAttributes.ReadOnly);

            var act = async () => await manager.CopyFilesAsync(sourceDir, destDir, "*.md", skipExisting: false);

            await act.Should().ThrowAsync<UnauthorizedAccessException>("cannot overwrite read-only file");

            File.SetAttributes(destFile, FileAttributes.Normal);
        }

        [TestMethod]
        public async Task CopyDirectoryRecursiveAsync_WithLockedFile_ContinuesWithOtherFiles()
        {
            var manager = GetDocumentationManager();
            var sourceDir = Path.Combine(_tempDirectory!, "locked-source");
            var destDir = Path.Combine(_tempDirectory!, "locked-dest");
            Directory.CreateDirectory(sourceDir);

            await File.WriteAllTextAsync(Path.Combine(sourceDir, "file1.md"), "content1");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "file2.md"), "content2");

            await manager.CopyDirectoryRecursiveAsync(sourceDir, destDir, "*.md", skipExisting: true);

            File.Exists(Path.Combine(destDir, "file1.md")).Should().BeTrue();
            File.Exists(Path.Combine(destDir, "file2.md")).Should().BeTrue();
        }

        [TestMethod]
        public async Task ProcessAsync_WithMissingXmlFile_ContinuesWithoutXmlDocs()
        {
            var context = GetService<ProjectContext>();
            var tempXmlPath = Path.Combine(_tempDirectory!, "nonexistent.xml");

            var renderer = new TestRenderer("TestRenderer", context);
            var manager = new DocumentationManager(context, renderers: [renderer]);

            await manager.ProcessAsync(_testAssemblyPath!, tempXmlPath);

            Directory.Exists(context.DocumentationRootPath).Should().BeTrue("ProcessAsync should complete even without XML docs");
        }

        [TestMethod]
        public async Task CopyReferencedDocumentationAsync_WithMissingSource_SkipsGracefully()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();

            context.DocumentationReferences.Add(new DocumentationReference
            {
                DocumentationRoot = Path.Combine(_tempDirectory!, "does-not-exist"),
                DestinationPath = "missing",
                DocumentationType = "Mintlify"
            });

            var act = async () => await manager.CopyReferencedDocumentationAsync();

            await act.Should().NotThrowAsync("missing source should be handled gracefully");
        }

        [TestMethod]
        public async Task LoadConceptualFileAsync_WithCorruptedFile_SetsEmptyContent()
        {
            var manager = GetDocumentationManager();
            var directory = Path.Combine(_tempDirectory!, "corrupted-test");
            Directory.CreateDirectory(directory);

            var filePath = Path.Combine(directory, "corrupted.md");
            await using (var stream = File.Create(filePath))
            {
                stream.WriteByte(0xFF);
                stream.WriteByte(0xFE);
                stream.WriteByte(0x00);
            }

            string? result = null;
            await manager.LoadConceptualFileAsync(directory, "corrupted.md", s => result = s, showPlaceholders: false);

            result.Should().NotBeNull("corrupted file should not crash the loader");
        }

        [TestMethod]
        public async Task MergeDocAssembliesAsync_WithConflictingTypes_PrefersFirst()
        {
            var manager = GetDocumentationManager();
            var assembly1 = await GetSingleTestAssembly();
            var assembly2 = await GetSingleTestAssembly();

            var merged = await manager.MergeDocAssembliesAsync(new List<DocAssembly> { assembly1, assembly2 });

            merged.Namespaces.Should().NotBeEmpty();
            foreach (var ns in merged.Namespaces)
            {
                var typeNames = ns.Types.Select(t => t.Name).ToList();
                typeNames.Should().OnlyHaveUniqueItems("conflicting types should be deduplicated");
            }
        }

        #endregion

        #region Concurrency Tests

        [TestMethod]
        public async Task GetOrCreateAssemblyManager_ConcurrentCalls_ReturnsSameInstance()
        {
            var manager = GetDocumentationManager();

            var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
            {
                return manager.GetOrCreateAssemblyManager(_testAssemblyPath!, _testXmlPath!);
            }));

            var results = await Task.WhenAll(tasks);

            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(instance =>
            {
                instance.Should().BeSameAs(results[0], "concurrent calls should return the same cached instance");
            });
        }

        [TestMethod]
        public async Task ProcessAsync_ConcurrentCallsMultipleAssemblies_CompletesSuccessfully()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();

            var tasks = Enumerable.Range(0, 5).Select(async i =>
            {
                var tempAssemblyPath = Path.Combine(_tempDirectory!, $"concurrent{i}.dll");
                File.Copy(_testAssemblyPath!, tempAssemblyPath, overwrite: true);
                var tempXmlPath = Path.Combine(_tempDirectory!, $"concurrent{i}.xml");
                File.Copy(_testXmlPath!, tempXmlPath, overwrite: true);

                await manager.ProcessAsync(tempAssemblyPath, tempXmlPath);
            });

            var act = async () => await Task.WhenAll(tasks);

            await act.Should().NotThrowAsync("concurrent ProcessAsync calls should complete successfully");
        }

        [TestMethod]
        public async Task CopyFilesAsync_ConcurrentCalls_AllComplete()
        {
            var manager = GetDocumentationManager();

            var tasks = Enumerable.Range(0, 10).Select(async i =>
            {
                var sourceDir = Path.Combine(_tempDirectory!, $"concurrent-source-{i}");
                var destDir = Path.Combine(_tempDirectory!, $"concurrent-dest-{i}");
                Directory.CreateDirectory(sourceDir);
                await File.WriteAllTextAsync(Path.Combine(sourceDir, "test.md"), $"content-{i}");

                await manager.CopyFilesAsync(sourceDir, destDir, "*.md", skipExisting: true);
            });

            await Task.WhenAll(tasks);

            for (int i = 0; i < 10; i++)
            {
                var destFile = Path.Combine(_tempDirectory!, $"concurrent-dest-{i}", "test.md");
                File.Exists(destFile).Should().BeTrue($"file {i} should be copied");
            }
        }

        [TestMethod]
        public async Task CreateConceptualFilesAsync_ConcurrentCallsSameAssembly_HandlesCorrectly()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualDocsEnabled = true;

            var tasks = Enumerable.Range(0, 5).Select(_ =>
                manager.CreateConceptualFilesAsync(_testAssemblyPath!, _testXmlPath!)
            );

            var act = async () => await Task.WhenAll(tasks);

            await act.Should().NotThrowAsync("concurrent CreateConceptualFilesAsync calls should be handled");
        }

        [TestMethod]
        public async Task MergeDocAssembliesAsync_ConcurrentMerges_ProduceConsistentResults()
        {
            var manager = GetDocumentationManager();

            var tasks = Enumerable.Range(0, 3).Select(async _ =>
            {
                var assembly1 = await GetSingleTestAssembly();
                var assembly2 = await GetSingleTestAssembly();
                return await manager.MergeDocAssembliesAsync(new List<DocAssembly> { assembly1, assembly2 });
            });

            var results = await Task.WhenAll(tasks);

            results.Should().AllSatisfy(r => r.Should().NotBeNull());
            results.Should().AllSatisfy(r => r.Namespaces.Should().NotBeEmpty());
        }

        [TestMethod]
        public async Task LoadConceptualAsync_ConcurrentLoads_CompleteSuccessfully()
        {
            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();

            var tasks = Enumerable.Range(0, 3).Select(async _ =>
            {
                var assembly = await GetSingleTestAssembly();
                await manager.LoadConceptualAsync(assembly);
                return assembly;
            });

            var results = await Task.WhenAll(tasks);

            results.Should().AllSatisfy(a => a.Should().NotBeNull());
        }

        #endregion

        #region Helper Methods

        private DocumentationManager GetDocumentationManager()
        {
            var manager = GetService<DocumentationManager>();
            manager.Should().NotBeNull("DocumentationManager should be registered in DI");
            return manager!;
        }

        private async Task<DocAssembly> GetSingleTestAssembly()
        {
            var context = GetService<ProjectContext>();
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            return await assemblyManager.DocumentAsync(context);
        }

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            Setup();

            // Configure services for DI
            TestHostBuilder.ConfigureServices((context, services) =>
            {
                services.AddDotNetDocsCore(ctx =>
                {
                    ctx.DocumentationRootPath = Path.Combine(_tempDirectory ?? Path.GetTempPath(), "output");
                });
            });

            TestSetup();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestTearDown();
        }

        #endregion

        #region Test Methods

        [TestMethod]
        public async Task ProcessAsync_WithConceptualPath_LoadsConceptualContent()
        {
            // Arrange
            await CreateConceptualContentAsync();

            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");

            // Since ProcessAsync runs the full pipeline, we'll test LoadConceptual directly
            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading directly
            await manager.LoadConceptualAsync(model);

            // Assert
            var testClass = model.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SimpleClass");

            testClass.Should().NotBeNull();
            testClass!.Usage.Should().Be("This is conceptual usage documentation");
            testClass.Examples.Should().Be("This is conceptual examples documentation");
            testClass.BestPractices.Should().Be("This is conceptual best practices");
            testClass.Patterns.Should().Be("This is conceptual patterns");
            testClass.Considerations.Should().Be("This is conceptual considerations");
        }

        [TestMethod]
        public async Task ProcessAsync_WithMemberConceptual_LoadsMemberContent()
        {
            // Arrange
            await CreateConceptualContentAsync();

            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading directly
            await manager.LoadConceptualAsync(model);

            // Assert
            var method = model.Namespaces
                .SelectMany(ns => ns.Types)
                .SelectMany(t => t.Members)
                .FirstOrDefault(m => m.Name == "DoWork");

            method.Should().NotBeNull();
            method!.Usage.Should().Be("This is conceptual member usage");
            method.Examples.Should().Be("This is conceptual member examples");
        }

        [TestMethod]
        public async Task ProcessAsync_WithPipeline_ExecutesAllSteps()
        {
            // Arrange
            await CreateConceptualContentAsync();

            var context = GetService<ProjectContext>();

            var enricher = new TestEnricher("Enricher");
            var transformer = new TestTransformer("Transformer");
            var renderer = new TestRenderer("Renderer", context);
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");

            var manager = new DocumentationManager(
                context,
                [enricher],
                [transformer],
                [renderer]
            );

            // Act
            await manager.ProcessAsync(_testAssemblyPath!, _testXmlPath!);

            // Assert
            enricher.Executed.Should().BeTrue();
            transformer.Executed.Should().BeTrue();
            renderer.Executed.Should().BeTrue();
        }

        [TestMethod]
        public async Task ProcessAsync_WithShowPlaceholdersFalse_SkipsPlaceholderContent()
        {
            // Arrange
            await CreateConceptualContentWithPlaceholdersAsync();

            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            context.ShowPlaceholders = false; // Hide placeholders

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading
            await manager.LoadConceptualAsync(model);

            // Assert - properties that had placeholder content should not contain the placeholder text
            var testClass = model.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SimpleClass");

            testClass.Should().NotBeNull();
            // The Usage property should remain unchanged from XML docs since placeholder was skipped
            testClass!.Usage.Should().NotContain("TODO: REMOVE THIS COMMENT");
            testClass.Usage.Should().NotContain("This is placeholder usage");
            
            // BestPractices should be null since it had placeholder content and was skipped
            testClass.BestPractices.Should().BeNull();

            // Examples was set to real content, should have that content
            testClass.Examples.Should().Be("This is real conceptual examples");
        }

        [TestMethod]
        public async Task ProcessAsync_WithShowPlaceholdersTrue_IncludesPlaceholderContent()
        {
            // Arrange
            await CreateConceptualContentWithPlaceholdersAsync();

            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            context.ShowPlaceholders = true; // Show placeholders

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading
            await manager.LoadConceptualAsync(model);

            // Assert - placeholder content should be included
            var testClass = model.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SimpleClass");

            testClass.Should().NotBeNull();
            // Usage should have the placeholder content
            testClass!.Usage.Should().Be("<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nThis is placeholder usage");
            
            // BestPractices should have the placeholder content
            testClass.BestPractices.Should().Be("<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nThis is placeholder best practices");

            // Examples was set to real content, should have that content
            testClass.Examples.Should().Be("This is real conceptual examples");
        }

        [TestMethod]
        public async Task ProcessAsync_WithMixedPlaceholderContent_HandlesCorrectly()
        {
            // Arrange
            await CreateMixedPlaceholderContentAsync();

            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            context.ShowPlaceholders = false; // Hide placeholders

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading
            await manager.LoadConceptualAsync(model);

            // Assert
            var testClass = model.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SimpleClass");

            testClass.Should().NotBeNull();
            // Usage had placeholder, should not contain placeholder text
            testClass!.Usage.Should().NotContain("TODO: REMOVE THIS COMMENT");
            
            // BestPractices had placeholder marker, should be skipped entirely
            testClass.BestPractices.Should().BeNull();
            
            // Patterns had no placeholder, should have full content
            testClass.Patterns.Should().Be("This is real patterns content");
        }

        [TestMethod]
        public async Task ProcessAsync_WithNestedNamespacePlaceholders_HandlesCorrectly()
        {
            // Arrange
            await CreateNestedNamespacePlaceholderContentAsync();

            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            context.ShowPlaceholders = false; // Hide placeholders

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading
            await manager.LoadConceptualAsync(model);

            // Assert - namespace level documentation
            var ns = model.Namespaces.FirstOrDefault(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");
            
            ns.Should().NotBeNull();
            // Namespace usage had placeholder, should not contain placeholder text
            ns!.Usage.Should().NotContain("TODO: REMOVE THIS COMMENT");
            ns.Usage.Should().NotContain("placeholder namespace usage");
            
            // The Usage property should remain unchanged from XML docs since placeholder was skipped
            ns.Examples.Should().Be("This is real namespace examples");
        }

        [TestMethod]
        public async Task ProcessAsync_WithMemberPlaceholders_HandlesCorrectly()
        {
            // Arrange
            await CreateMemberPlaceholderContentAsync();

            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            context.ShowPlaceholders = false; // Hide placeholders

            // Get the model first from AssemblyManager
            using var assemblyComponent = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyComponent.DocumentAsync(context);

            // Apply conceptual loading
            await manager.LoadConceptualAsync(model);

            // Assert - member level documentation
            var method = model.Namespaces
                .SelectMany(ns => ns.Types)
                .SelectMany(t => t.Members)
                .FirstOrDefault(m => m.Name == "DoWork");

            method.Should().NotBeNull();
            // The Usage property should remain unchanged from XML docs since placeholder was skipped
            method!.Usage.Should().NotContain("TODO: REMOVE THIS COMMENT");
            method.Usage.Should().NotContain("placeholder member usage");
            
            // Examples had real content, should have it
            method.Examples.Should().Be("This is real member examples");
        }

        [TestMethod]
        public async Task ProcessAsync_WithParameterPlaceholders_HandlesCorrectly()
        {
            // Arrange
            await CreateParameterPlaceholderContentAsync();

            var manager = GetDocumentationManager();
            var context = GetService<ProjectContext>();
            context.ConceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            context.ShowPlaceholders = false; // Hide placeholders

            // Get the model first from AssemblyManager
            using var assemblyManager = new AssemblyManager(_testAssemblyPath!, _testXmlPath!);
            var model = await assemblyManager.DocumentAsync(context);

            // Apply conceptual loading
            await manager.LoadConceptualAsync(model);

            // Assert - parameter level documentation
            var method = model.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithMethods")
                ?.Members
                .FirstOrDefault(m => m.Symbol.Name == "Calculate");

            method.Should().NotBeNull();

            var parameter = method?.Parameters.FirstOrDefault(p => p.Symbol.Name == "a");

            parameter.Should().NotBeNull();
            // The Usage property should remain unchanged from XML docs since placeholder was skipped
            // The XML doc for the parameter is "The first number."
            parameter!.Usage.Should().Be("The first number.");
        }

        [TestMethod]
        public void GetExclusionPatternsForDocumentationType_Mintlify_ReturnsCorrectPatterns()
        {
            // Arrange
            var manager = GetDocumentationManager();

            // Act
            var patterns = manager.GetExclusionPatternsForDocumentationType("Mintlify");

            // Assert
            patterns.Should().NotBeNull();
            patterns.Should().Contain("**/*.mdz");
            patterns.Should().Contain("conceptual/**/*");
            patterns.Should().Contain("**/*.css");
            patterns.Should().Contain("docs.json");
            patterns.Should().Contain("assembly-list.txt");
            patterns.Should().Contain("*.docsproj");
        }

        [TestMethod]
        public void GetExclusionPatternsForDocumentationType_UnknownType_ReturnsEmptyList()
        {
            // Arrange
            var manager = GetDocumentationManager();

            // Act
            var patterns = manager.GetExclusionPatternsForDocumentationType("UnknownType");

            // Assert
            patterns.Should().NotBeNull();
            patterns.Should().BeEmpty();
        }

        [TestMethod]
        public void ShouldExcludeFile_WithMdzPattern_ExcludesMdzFiles()
        {
            // Arrange
            var manager = GetDocumentationManager();
            var exclusionPatterns = new List<string> { "**/*.mdz" };

            // Act & Assert
            manager.ShouldExcludeFile("file.mdz", exclusionPatterns).Should().BeTrue();
            manager.ShouldExcludeFile("api-reference/test.mdz", exclusionPatterns).Should().BeTrue();
            manager.ShouldExcludeFile("deeply/nested/path/file.mdz", exclusionPatterns).Should().BeTrue();
            manager.ShouldExcludeFile("file.md", exclusionPatterns).Should().BeFalse();
            manager.ShouldExcludeFile("file.mdx", exclusionPatterns).Should().BeFalse();
        }

        [TestMethod]
        public void ShouldExcludeFile_WithConceptualPattern_ExcludesConceptualFolder()
        {
            // Arrange
            var manager = GetDocumentationManager();
            var exclusionPatterns = new List<string> { "conceptual/**/*" };

            // Act & Assert
            manager.ShouldExcludeFile("conceptual/guide.md", exclusionPatterns).Should().BeTrue();
            manager.ShouldExcludeFile("conceptual/nested/file.mdx", exclusionPatterns).Should().BeTrue();
            manager.ShouldExcludeFile("api-reference/file.md", exclusionPatterns).Should().BeFalse();
            manager.ShouldExcludeFile("guide.md", exclusionPatterns).Should().BeFalse();
        }

        [TestMethod]
        public void ShouldExcludeFile_WithCssPattern_ExcludesCssFiles()
        {
            // Arrange
            var manager = GetDocumentationManager();
            var exclusionPatterns = new List<string> { "**/*.css" };

            // Act & Assert
            manager.ShouldExcludeFile("style.css", exclusionPatterns).Should().BeTrue();
            manager.ShouldExcludeFile("assets/main.css", exclusionPatterns).Should().BeTrue();
            manager.ShouldExcludeFile("deeply/nested/theme.css", exclusionPatterns).Should().BeTrue();
            manager.ShouldExcludeFile("style.scss", exclusionPatterns).Should().BeFalse();
            manager.ShouldExcludeFile("file.css.map", exclusionPatterns).Should().BeFalse();
        }

        [TestMethod]
        public void ShouldExcludeFile_WithDocsJsonPattern_ExcludesDocsJson()
        {
            // Arrange
            var manager = GetDocumentationManager();
            var exclusionPatterns = new List<string> { "docs.json" };

            // Act & Assert
            manager.ShouldExcludeFile("docs.json", exclusionPatterns).Should().BeTrue();
            manager.ShouldExcludeFile("nested/docs.json", exclusionPatterns).Should().BeFalse(); // Exact match only
            manager.ShouldExcludeFile("docs-template.json", exclusionPatterns).Should().BeFalse();
        }

        [TestMethod]
        public void ShouldExcludeFile_WithAssemblyListPattern_ExcludesAssemblyList()
        {
            // Arrange
            var manager = GetDocumentationManager();
            var exclusionPatterns = new List<string> { "assembly-list.txt" };

            // Act & Assert
            manager.ShouldExcludeFile("assembly-list.txt", exclusionPatterns).Should().BeTrue();
            manager.ShouldExcludeFile("nested/assembly-list.txt", exclusionPatterns).Should().BeFalse(); // Exact match only
            manager.ShouldExcludeFile("assembly-list-backup.txt", exclusionPatterns).Should().BeFalse();
        }

        [TestMethod]
        public void ShouldExcludeFile_WithDocsprojPattern_ExcludesDocsprojFiles()
        {
            // Arrange
            var manager = GetDocumentationManager();
            var exclusionPatterns = new List<string> { "*.docsproj" };

            // Act & Assert
            manager.ShouldExcludeFile("MyProject.docsproj", exclusionPatterns).Should().BeTrue();
            manager.ShouldExcludeFile("Documentation.docsproj", exclusionPatterns).Should().BeTrue();
            manager.ShouldExcludeFile("nested/Project.docsproj", exclusionPatterns).Should().BeTrue();
            manager.ShouldExcludeFile("MyProject.csproj", exclusionPatterns).Should().BeFalse();
        }

        [TestMethod]
        public void ShouldExcludeDirectory_WithConceptualPattern_ExcludesConceptualDirectory()
        {
            // Arrange
            var manager = GetDocumentationManager();
            var exclusionPatterns = new List<string> { "conceptual/**/*" };

            // Act & Assert
            manager.ShouldExcludeDirectory("conceptual", exclusionPatterns).Should().BeTrue();
            manager.ShouldExcludeDirectory("conceptual/guides", exclusionPatterns).Should().BeTrue();
            manager.ShouldExcludeDirectory("conceptual/guides/nested", exclusionPatterns).Should().BeTrue();
            manager.ShouldExcludeDirectory("api-reference", exclusionPatterns).Should().BeFalse();
            manager.ShouldExcludeDirectory("guides", exclusionPatterns).Should().BeFalse();
        }

        [TestMethod]
        public void ShouldExcludeFile_NonMatchingFile_DoesNotExclude()
        {
            // Arrange
            var manager = GetDocumentationManager();
            var exclusionPatterns = new List<string> { "**/*.mdz", "conceptual/**/*", "**/*.css", "docs.json", "assembly-list.txt", "*.docsproj" };

            // Act & Assert - These should NOT be excluded
            manager.ShouldExcludeFile("index.mdx", exclusionPatterns).Should().BeFalse();
            manager.ShouldExcludeFile("api-reference/class.md", exclusionPatterns).Should().BeFalse();
            manager.ShouldExcludeFile("snippets/example.jsx", exclusionPatterns).Should().BeFalse();
            manager.ShouldExcludeFile("images/logo.png", exclusionPatterns).Should().BeFalse();
        }

        [TestMethod]
        public async Task CopyDirectoryWithExclusionsAsync_SkipsExcludedFiles()
        {
            // Arrange
            var sourceDir = Path.Combine(_tempDirectory!, "source");
            var destDir = Path.Combine(_tempDirectory!, "dest");
            Directory.CreateDirectory(sourceDir);

            // Create test files
            File.WriteAllText(Path.Combine(sourceDir, "index.mdx"), "content");
            File.WriteAllText(Path.Combine(sourceDir, "test.mdz"), "content");
            File.WriteAllText(Path.Combine(sourceDir, "style.css"), "content");
            File.WriteAllText(Path.Combine(sourceDir, "docs.json"), "content");

            var manager = GetDocumentationManager();
            var exclusionPatterns = new List<string> { "**/*.mdz", "**/*.css", "docs.json" };

            // Act
            await manager.CopyDirectoryWithExclusionsAsync(sourceDir, destDir, exclusionPatterns);

            // Assert
            File.Exists(Path.Combine(destDir, "index.mdx")).Should().BeTrue("index.mdx should be copied");
            File.Exists(Path.Combine(destDir, "test.mdz")).Should().BeFalse("test.mdz should be excluded");
            File.Exists(Path.Combine(destDir, "style.css")).Should().BeFalse("style.css should be excluded");
            File.Exists(Path.Combine(destDir, "docs.json")).Should().BeFalse("docs.json should be excluded");
        }

        [TestMethod]
        public async Task CopyDirectoryWithExclusionsAsync_CopiesNestedDirectories()
        {
            // Arrange
            var sourceDir = Path.Combine(_tempDirectory!, "source2");
            var destDir = Path.Combine(_tempDirectory!, "dest2");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(Path.Combine(sourceDir, "api-reference"));
            Directory.CreateDirectory(Path.Combine(sourceDir, "conceptual"));

            // Create test files
            File.WriteAllText(Path.Combine(sourceDir, "index.mdx"), "content");
            File.WriteAllText(Path.Combine(sourceDir, "api-reference", "class.md"), "content");
            File.WriteAllText(Path.Combine(sourceDir, "conceptual", "guide.md"), "content");

            var manager = GetDocumentationManager();
            var exclusionPatterns = new List<string> { "conceptual/**/*" };

            // Act
            await manager.CopyDirectoryWithExclusionsAsync(sourceDir, destDir, exclusionPatterns);

            // Assert
            File.Exists(Path.Combine(destDir, "index.mdx")).Should().BeTrue("index.mdx should be copied");
            File.Exists(Path.Combine(destDir, "api-reference", "class.md")).Should().BeTrue("api-reference/class.md should be copied");
            Directory.Exists(Path.Combine(destDir, "conceptual")).Should().BeFalse("conceptual directory should not be copied");
            File.Exists(Path.Combine(destDir, "conceptual", "guide.md")).Should().BeFalse("conceptual/guide.md should be excluded");
        }

        [TestMethod]
        public void MatchesGlobPattern_WithVariousPatterns_MatchesCorrectly()
        {
            // Arrange
            var manager = GetDocumentationManager();

            // Act & Assert
            // Test **/*.ext pattern
            manager.MatchesGlobPattern("file.mdz", "**/*.mdz").Should().BeTrue();
            manager.MatchesGlobPattern("path/file.mdz", "**/*.mdz").Should().BeTrue();
            manager.MatchesGlobPattern("deep/nested/file.mdz", "**/*.mdz").Should().BeTrue();
            manager.MatchesGlobPattern("file.md", "**/*.mdz").Should().BeFalse();

            // Test directory/**/* pattern
            manager.MatchesGlobPattern("conceptual/file.md", "conceptual/**/*").Should().BeTrue();
            manager.MatchesGlobPattern("conceptual/nested/file.md", "conceptual/**/*").Should().BeTrue();
            manager.MatchesGlobPattern("api-reference/file.md", "conceptual/**/*").Should().BeFalse();

            // Test exact match
            manager.MatchesGlobPattern("docs.json", "docs.json").Should().BeTrue();
            manager.MatchesGlobPattern("nested/docs.json", "docs.json").Should().BeFalse();

            // Test *.ext pattern
            manager.MatchesGlobPattern("file.css", "*.css").Should().BeTrue();
            manager.MatchesGlobPattern("path/file.css", "*.css").Should().BeTrue();
            manager.MatchesGlobPattern("file.scss", "*.css").Should().BeFalse();
        }

        #endregion

        #region Setup and Cleanup

        // TestInitialize is already defined in the Test Lifecycle region above
        // This method is now called from TestInitialize
        private void Setup()
        {
            // Use the real Tests.Shared assembly and its XML documentation
            _testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            _testXmlPath = Path.ChangeExtension(_testAssemblyPath, ".xml");

            _tempDirectory = Path.Combine(Path.GetTempPath(), $"DocManagerTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDirectory);
        }

        #endregion

        #region Private Methods

        private async Task CreateConceptualContentAsync()
        {
            var conceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            
            // Create namespace folder structure
            var namespacePath = Path.Combine(conceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios");
            Directory.CreateDirectory(namespacePath);

            // Create conceptual content for SimpleClass
            var classPath = Path.Combine(namespacePath, "SimpleClass");
            Directory.CreateDirectory(classPath);

            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.UsageFileName),
                "This is conceptual usage documentation");
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.ExamplesFileName),
                "This is conceptual examples documentation");
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.BestPracticesFileName),
                "This is conceptual best practices");
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.PatternsFileName),
                "This is conceptual patterns");
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.ConsiderationsFileName),
                "This is conceptual considerations");

            // Create member documentation
            var memberPath = Path.Combine(classPath, "DoWork");
            Directory.CreateDirectory(memberPath);

            await File.WriteAllTextAsync(Path.Combine(memberPath, DocConstants.UsageFileName),
                "This is conceptual member usage");
            await File.WriteAllTextAsync(Path.Combine(memberPath, DocConstants.ExamplesFileName),
                "This is conceptual member examples");
        }

        private async Task CreateConceptualContentWithPlaceholdersAsync()
        {
            var conceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            
            // Create namespace folder structure
            var namespacePath = Path.Combine(conceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios");
            Directory.CreateDirectory(namespacePath);

            // Create conceptual content for SimpleClass with placeholders
            var classPath = Path.Combine(namespacePath, "SimpleClass");
            Directory.CreateDirectory(classPath);

            // Usage has placeholder
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.UsageFileName),
                "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nThis is placeholder usage");
            
            // Examples has real content
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.ExamplesFileName),
                "This is real conceptual examples");
            
            // BestPractices has placeholder
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.BestPracticesFileName),
                "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nThis is placeholder best practices");
        }

        private async Task CreateMixedPlaceholderContentAsync()
        {
            var conceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            
            // Create namespace folder structure
            var namespacePath = Path.Combine(conceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios");
            Directory.CreateDirectory(namespacePath);

            // Create conceptual content for SimpleClass with mixed content
            var classPath = Path.Combine(namespacePath, "SimpleClass");
            Directory.CreateDirectory(classPath);

            // Usage has placeholder
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.UsageFileName),
                "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nThis is placeholder usage");
            
            // BestPractices has placeholder followed by real content
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.BestPracticesFileName),
                "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nThis is placeholder best practices\n\nThis is real best practices after placeholder");
            
            // Patterns has only real content
            await File.WriteAllTextAsync(Path.Combine(classPath, DocConstants.PatternsFileName),
                "This is real patterns content");
        }

        private async Task CreateNestedNamespacePlaceholderContentAsync()
        {
            var conceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            
            // Create namespace folder structure
            var namespacePath = Path.Combine(conceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios");
            Directory.CreateDirectory(namespacePath);

            // Create namespace-level documentation
            await File.WriteAllTextAsync(Path.Combine(namespacePath, DocConstants.UsageFileName),
                "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nThis is placeholder namespace usage");
            
            await File.WriteAllTextAsync(Path.Combine(namespacePath, DocConstants.ExamplesFileName),
                "This is real namespace examples");
        }

        private async Task CreateMemberPlaceholderContentAsync()
        {
            var conceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            
            // Create namespace folder structure
            var namespacePath = Path.Combine(conceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios");
            Directory.CreateDirectory(namespacePath);

            // Create conceptual content for SimpleClass
            var classPath = Path.Combine(namespacePath, "SimpleClass");
            Directory.CreateDirectory(classPath);

            // Create member documentation with placeholders
            var memberPath = Path.Combine(classPath, "DoWork");
            Directory.CreateDirectory(memberPath);

            await File.WriteAllTextAsync(Path.Combine(memberPath, DocConstants.UsageFileName),
                "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nThis is placeholder member usage");
            
            await File.WriteAllTextAsync(Path.Combine(memberPath, DocConstants.ExamplesFileName),
                "This is real member examples");
        }

        private async Task CreateParameterPlaceholderContentAsync()
        {
            var conceptualPath = Path.Combine(_tempDirectory!, "conceptual");
            
            // Create namespace folder structure
            var namespacePath = Path.Combine(conceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios");
            Directory.CreateDirectory(namespacePath);

            // Create conceptual content for ClassWithMethods
            var classPath = Path.Combine(namespacePath, "ClassWithMethods");
            Directory.CreateDirectory(classPath);

            // Create member documentation
            var memberPath = Path.Combine(classPath, "Calculate");
            Directory.CreateDirectory(memberPath);

            // Create parameter documentation with placeholder
            var paramPath = Path.Combine(memberPath, "a");
            Directory.CreateDirectory(paramPath);

            await File.WriteAllTextAsync(Path.Combine(paramPath, DocConstants.UsageFileName),
                "<!-- TODO: REMOVE THIS COMMENT AFTER YOU CUSTOMIZE THIS CONTENT -->\nThis is placeholder parameter usage");
        }

        #endregion

        #region Test Support Classes

        private class TestEnricher : IDocEnricher
        {
            public string Name { get; }
            public bool Executed { get; private set; }

            public TestEnricher(string name)
            {
                Name = name;
            }

            public Task EnrichAsync(DocEntity entity)
            {
                Executed = true;
                return Task.CompletedTask;
            }
        }

        private class TestTransformer : IDocTransformer
        {
            public string Name { get; }
            public bool Executed { get; private set; }

            public TestTransformer(string name)
            {
                Name = name;
            }

            public Task TransformAsync(DocEntity entity)
            {
                Executed = true;
                return Task.CompletedTask;
            }
        }

        private class TestRenderer : IDocRenderer
        {
            private readonly ProjectContext projectContext;

            public string Name { get; }
            public bool Executed { get; private set; }

            public TestRenderer(string name, ProjectContext projectContext)
            {
                Name = name;
                this.projectContext = projectContext;
            }

            public Task RenderAsync(DocAssembly model)
            {
                Executed = true;

                if (!string.IsNullOrWhiteSpace(projectContext.DocumentationRootPath))
                {
                    Directory.CreateDirectory(projectContext.DocumentationRootPath);
                }

                return Task.CompletedTask;
            }

            public Task RenderPlaceholdersAsync(DocAssembly model)
            {
                return Task.CompletedTask;
            }

            public Task CombineReferencedNavigationAsync(List<DocumentationReference> references)
            {
                return Task.CompletedTask;
            }
        }

        #endregion

    }

}