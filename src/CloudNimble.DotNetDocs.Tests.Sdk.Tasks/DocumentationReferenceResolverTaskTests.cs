using System;
using System.IO;
using System.Linq;
using CloudNimble.DotNetDocs.Sdk.Tasks;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Sdk.Tasks
{

    /// <summary>
    /// Tests for the DocumentationReferenceResolverTask class.
    /// </summary>
    [TestClass]
    public class DocumentationReferenceResolverTaskTests : DotNetDocsTestBase
    {

        #region Fields

        private DocumentationReferenceResolverTask _task = null!;
        private TestBuildEngine _buildEngine = null!;
        private string _tempDirectory = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            TestSetup();
            _task = new DocumentationReferenceResolverTask();
            _buildEngine = new TestBuildEngine();
            _task.BuildEngine = _buildEngine;

            // Create a temporary directory for test files
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up temporary files
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }

            TestTearDown();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a test .docsproj file with specified properties.
        /// </summary>
        private string CreateTestDocsproj(string filename, string documentationType, string? documentationRoot = null, string? docsJsonPath = null)
        {
            var projectPath = Path.Combine(_tempDirectory, filename);
            var docRoot = documentationRoot ?? Path.Combine(_tempDirectory, $"{Path.GetFileNameWithoutExtension(filename)}_docs");

            // Create the documentation root directory
            Directory.CreateDirectory(docRoot);

            // Create a navigation file if path is provided
            if (!string.IsNullOrWhiteSpace(docsJsonPath))
            {
                var navFilePath = Path.Combine(docRoot, docsJsonPath);
                var navDir = Path.GetDirectoryName(navFilePath);
                if (!string.IsNullOrWhiteSpace(navDir))
                {
                    Directory.CreateDirectory(navDir);
                }
                File.WriteAllText(navFilePath, "{ \"name\": \"Test Docs\" }");
            }

            var projectContent = $@"<Project>
  <PropertyGroup>
    <DocumentationType>{documentationType}</DocumentationType>
    <DocumentationRoot>{docRoot}</DocumentationRoot>
    <Configuration>Release</Configuration>
  </PropertyGroup>
</Project>";

            File.WriteAllText(projectPath, projectContent);
            return projectPath;
        }

        #endregion

        #region Basic Execution Tests

        [TestMethod]
        public void Execute_WithNoReferences_ReturnsEmptyArray()
        {
            // Arrange
            _task.DocumentationReferences = [];

            // Act
            var result = _task.Execute();

            // Assert
            result.Should().BeTrue();
            _task.ResolvedDocumentationReferences.Should().BeEmpty();
            _buildEngine.LoggedMessages.Where(m => m.Message != null).Should().Contain(m => m.Message!.Contains("No DocumentationReferences to resolve"));
        }

        [TestMethod]
        public void Execute_WithNullReferences_ReturnsEmptyArray()
        {
            // Arrange
            _task.DocumentationReferences = null!;

            // Act
            var result = _task.Execute();

            // Assert
            result.Should().BeTrue();
            _task.ResolvedDocumentationReferences.Should().BeEmpty();
        }

        #endregion

        #region Single Reference Resolution Tests

        [TestMethod]
        public void Execute_WithValidMintlifyReference_ResolvesSuccessfully()
        {
            // Arrange
            var projectPath = CreateTestDocsproj("ServiceA.docsproj", "Mintlify", docsJsonPath: "docs.json");
            var reference = new TaskItem(projectPath);
            reference.SetMetadata("DestinationPath", "services/service-a");
            reference.SetMetadata("IntegrationType", "Tabs");

            _task.DocumentationReferences = [reference];
            _task.Configuration = "Release";
            _task.DocumentationType = "Mintlify";

            // Change to temp directory so relative paths work correctly
            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeTrue();
                _task.ResolvedDocumentationReferences.Should().HaveCount(1);

                var resolved = _task.ResolvedDocumentationReferences[0];
                resolved.GetMetadata("ProjectPath").Should().EndWith("ServiceA.docsproj");
                resolved.GetMetadata("DocumentationType").Should().Be("Mintlify");
                resolved.GetMetadata("DestinationPath").Should().Be("services/service-a");
                resolved.GetMetadata("IntegrationType").Should().Be("Tabs");
                resolved.GetMetadata("DocumentationRoot").Should().NotBeNullOrWhiteSpace();
                resolved.GetMetadata("NavigationFilePath").Should().EndWith("docs.json");
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [TestMethod]
        public void Execute_WithMissingDestinationPath_DefaultsToProjectName()
        {
            // Arrange
            var projectPath = CreateTestDocsproj("MyService.docsproj", "Mintlify");
            var reference = new TaskItem(projectPath);

            _task.DocumentationReferences = [reference];
            _task.DocumentationType = "Mintlify";

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeTrue();
                _task.ResolvedDocumentationReferences.Should().HaveCount(1);
                _task.ResolvedDocumentationReferences[0].GetMetadata("DestinationPath").Should().Be("MyService");
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [TestMethod]
        public void Execute_WithMissingIntegrationType_DefaultsToTabs()
        {
            // Arrange
            var projectPath = CreateTestDocsproj("ServiceA.docsproj", "Mintlify");
            var reference = new TaskItem(projectPath);
            reference.SetMetadata("DestinationPath", "services/a");

            _task.DocumentationReferences = [reference];
            _task.DocumentationType = "Mintlify";

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeTrue();
                _task.ResolvedDocumentationReferences.Should().HaveCount(1);
                _task.ResolvedDocumentationReferences[0].GetMetadata("IntegrationType").Should().Be("Tabs");
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        #endregion

        #region Multiple Reference Resolution Tests

        [TestMethod]
        public void Execute_WithMultipleReferences_ResolvesAll()
        {
            // Arrange
            var serviceAPath = CreateTestDocsproj("ServiceA.docsproj", "Mintlify");
            var serviceBPath = CreateTestDocsproj("ServiceB.docsproj", "Mintlify");
            var serviceCPath = CreateTestDocsproj("ServiceC.docsproj", "Mintlify");

            var refA = new TaskItem(serviceAPath);
            refA.SetMetadata("DestinationPath", "services/a");

            var refB = new TaskItem(serviceBPath);
            refB.SetMetadata("DestinationPath", "services/b");

            var refC = new TaskItem(serviceCPath);
            refC.SetMetadata("DestinationPath", "services/c");

            _task.DocumentationReferences = [refA, refB, refC];
            _task.DocumentationType = "Mintlify";

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeTrue();
                _task.ResolvedDocumentationReferences.Should().HaveCount(3);
                _task.ResolvedDocumentationReferences[0].GetMetadata("DestinationPath").Should().Be("services/a");
                _task.ResolvedDocumentationReferences[1].GetMetadata("DestinationPath").Should().Be("services/b");
                _task.ResolvedDocumentationReferences[2].GetMetadata("DestinationPath").Should().Be("services/c");
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        #endregion

        #region Error Handling Tests

        [TestMethod]
        public void Execute_WithEmptyProjectPath_SkipsReferenceWithWarning()
        {
            // Arrange
            var reference = new TaskItem("");
            _task.DocumentationReferences = [reference];

            // Act
            var result = _task.Execute();

            // Assert
            result.Should().BeTrue();
            _task.ResolvedDocumentationReferences.Should().BeEmpty();
            _buildEngine.LoggedWarnings.Where(w => w.Message != null).Should().Contain(w => w.Message!.Contains("empty project path"));
        }

        [TestMethod]
        public void Execute_WithNonExistentProject_ReturnsError()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_tempDirectory, "DoesNotExist.docsproj");
            var reference = new TaskItem(nonExistentPath);
            _task.DocumentationReferences = [reference];

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeFalse();
                _buildEngine.LoggedErrors.Where(e => e.Message != null).Should().Contain(e => e.Message!.Contains("not found"));
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [TestMethod]
        public void Execute_WithMissingDocumentationRoot_ReturnsError()
        {
            // Arrange
            var projectPath = Path.Combine(_tempDirectory, "Invalid.docsproj");
            var projectContent = @"<Project>
  <PropertyGroup>
    <DocumentationType>Mintlify</DocumentationType>
    <!-- DocumentationRoot is missing -->
  </PropertyGroup>
</Project>";
            File.WriteAllText(projectPath, projectContent);

            var reference = new TaskItem(projectPath);
            _task.DocumentationReferences = [reference];

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeFalse();
                _buildEngine.LoggedErrors.Where(e => e.Message != null).Should().Contain(e => e.Message!.Contains("does not have a DocumentationRoot"));
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [TestMethod]
        public void Execute_WithMissingDocumentationType_ReturnsError()
        {
            // Arrange
            var projectPath = Path.Combine(_tempDirectory, "Invalid.docsproj");
            var docRoot = Path.Combine(_tempDirectory, "docs");
            Directory.CreateDirectory(docRoot);

            var projectContent = $@"<Project>
  <PropertyGroup>
    <DocumentationRoot>{docRoot}</DocumentationRoot>
    <!-- DocumentationType is missing -->
  </PropertyGroup>
</Project>";
            File.WriteAllText(projectPath, projectContent);

            var reference = new TaskItem(projectPath);
            _task.DocumentationReferences = [reference];

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeFalse();
                _buildEngine.LoggedErrors.Where(e => e.Message != null).Should().Contain(e => e.Message!.Contains("does not have a DocumentationType"));
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [TestMethod]
        public void Execute_WithNonExistentDocumentationRoot_ReturnsError()
        {
            // Arrange
            var projectPath = Path.Combine(_tempDirectory, "Invalid.docsproj");
            var nonExistentRoot = Path.Combine(_tempDirectory, "does-not-exist");

            var projectContent = $@"<Project>
  <PropertyGroup>
    <DocumentationType>Mintlify</DocumentationType>
    <DocumentationRoot>{nonExistentRoot}</DocumentationRoot>
  </PropertyGroup>
</Project>";
            File.WriteAllText(projectPath, projectContent);

            var reference = new TaskItem(projectPath);
            _task.DocumentationReferences = [reference];

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeFalse();
                _buildEngine.LoggedErrors.Where(e => e.Message != null).Should().Contain(e => e.Message!.Contains("Documentation root does not exist"));
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        #endregion

        #region Documentation Type Validation Tests

        [TestMethod]
        public void Execute_WithMismatchedDocumentationType_SkipsWithWarning()
        {
            // Arrange
            var projectPath = CreateTestDocsproj("DocFXService.docsproj", "DocFX");
            var reference = new TaskItem(projectPath);

            _task.DocumentationReferences = [reference];
            _task.DocumentationType = "Mintlify"; // Collection is Mintlify, but reference is DocFX

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeTrue();
                _task.ResolvedDocumentationReferences.Should().BeEmpty();
                _buildEngine.LoggedWarnings.Where(w => w.Message != null).Should().Contain(w =>
                    w.Message!.Contains("Skipping documentation reference") &&
                    w.Message.Contains("DocFX") &&
                    w.Message.Contains("Mintlify"));
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [TestMethod]
        public void Execute_WithMatchingDocumentationType_Resolves()
        {
            // Arrange
            var projectPath = CreateTestDocsproj("ServiceA.docsproj", "Mintlify");
            var reference = new TaskItem(projectPath);

            _task.DocumentationReferences = [reference];
            _task.DocumentationType = "Mintlify"; // Both use Mintlify

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeTrue();
                _task.ResolvedDocumentationReferences.Should().HaveCount(1);
                _buildEngine.LoggedWarnings.Where(w => w.Message != null).Should().NotContain(w => w.Message!.Contains("Skipping"));
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [TestMethod]
        public void Execute_WithNullCollectionDocumentationType_SkipsValidation()
        {
            // Arrange
            var projectPath = CreateTestDocsproj("ServiceA.docsproj", "Mintlify", docsJsonPath: "docs.json");
            var reference = new TaskItem(projectPath);

            _task.DocumentationReferences = [reference];
            _task.DocumentationType = null; // No type specified for collection

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeTrue();
                _task.ResolvedDocumentationReferences.Should().HaveCount(1);
                _buildEngine.LoggedWarnings.Should().BeEmpty();
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [TestMethod]
        public void Execute_WithCaseInsensitiveDocumentationType_Matches()
        {
            // Arrange
            var projectPath = CreateTestDocsproj("ServiceA.docsproj", "mintlify"); // lowercase
            var reference = new TaskItem(projectPath);

            _task.DocumentationReferences = [reference];
            _task.DocumentationType = "MINTLIFY"; // uppercase

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeTrue();
                _task.ResolvedDocumentationReferences.Should().HaveCount(1);
                _buildEngine.LoggedWarnings.Where(w => w.Message != null).Should().NotContain(w => w.Message!.Contains("Skipping"));
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        #endregion

        #region Navigation File Path Tests

        [TestMethod]
        public void Execute_WithMintlifyType_SetsDocsJsonNavigationPath()
        {
            // Arrange
            var projectPath = CreateTestDocsproj("ServiceA.docsproj", "Mintlify", docsJsonPath: "docs.json");
            var reference = new TaskItem(projectPath);

            _task.DocumentationReferences = [reference];
            _task.DocumentationType = "Mintlify";

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeTrue();
                _task.ResolvedDocumentationReferences[0].GetMetadata("NavigationFilePath").Should().EndWith("docs.json");
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [TestMethod]
        public void Execute_WithDocFXType_SetsTocYmlNavigationPath()
        {
            // Arrange
            var projectPath = CreateTestDocsproj("ServiceA.docsproj", "DocFX", docsJsonPath: "toc.yml");
            var reference = new TaskItem(projectPath);

            _task.DocumentationReferences = [reference];
            _task.DocumentationType = "DocFX";

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeTrue();
                _task.ResolvedDocumentationReferences[0].GetMetadata("NavigationFilePath").Should().EndWith("toc.yml");
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [TestMethod]
        public void Execute_WithMissingNavigationFile_LogsWarningAndContinues()
        {
            // Arrange
            var projectPath = CreateTestDocsproj("ServiceA.docsproj", "Mintlify"); // No navigation file created
            var reference = new TaskItem(projectPath);

            _task.DocumentationReferences = [reference];
            _task.DocumentationType = "Mintlify";

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeTrue();
                _task.ResolvedDocumentationReferences.Should().HaveCount(1);
                _task.ResolvedDocumentationReferences[0]!.GetMetadata("NavigationFilePath").Should().BeEmpty();
                _buildEngine.LoggedWarnings.Where(w => w.Message != null).Should().Contain(w => w.Message!.Contains("Navigation file not found"));
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [TestMethod]
        public void Execute_WithUnknownDocumentationType_SetsEmptyNavigationPath()
        {
            // Arrange
            var projectPath = CreateTestDocsproj("ServiceA.docsproj", "CustomDocSystem");
            var reference = new TaskItem(projectPath);

            _task.DocumentationReferences = [reference];
            _task.DocumentationType = "CustomDocSystem";

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeTrue();
                _task.ResolvedDocumentationReferences[0]!.GetMetadata("NavigationFilePath").Should().BeEmpty();
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        #endregion

        #region Relative and Absolute Path Tests

        [TestMethod]
        public void Execute_WithRelativePath_ConvertsToAbsolute()
        {
            // Arrange
            var projectPath = CreateTestDocsproj("ServiceA.docsproj", "Mintlify");
            var relativePath = Path.GetFileName(projectPath); // Just the filename
            var reference = new TaskItem(relativePath);

            _task.DocumentationReferences = [reference];
            _task.DocumentationType = "Mintlify";

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeTrue();
                _task.ResolvedDocumentationReferences.Should().HaveCount(1);
                Path.IsPathRooted(_task.ResolvedDocumentationReferences[0].GetMetadata("ProjectPath")).Should().BeTrue();
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [TestMethod]
        public void Execute_WithAbsolutePath_UsesAsIs()
        {
            // Arrange
            var projectPath = CreateTestDocsproj("ServiceA.docsproj", "Mintlify");
            var reference = new TaskItem(projectPath); // Already absolute

            _task.DocumentationReferences = [reference];
            _task.DocumentationType = "Mintlify";

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeTrue();
                _task.ResolvedDocumentationReferences.Should().HaveCount(1);
                _task.ResolvedDocumentationReferences[0].GetMetadata("ProjectPath").Should().Be(projectPath);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        #endregion

        #region Integration Type Tests

        [TestMethod]
        public void Execute_WithTabsIntegrationType_PreservesValue()
        {
            // Arrange
            var projectPath = CreateTestDocsproj("ServiceA.docsproj", "Mintlify");
            var reference = new TaskItem(projectPath);
            reference.SetMetadata("IntegrationType", "Tabs");

            _task.DocumentationReferences = [reference];
            _task.DocumentationType = "Mintlify";

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeTrue();
                _task.ResolvedDocumentationReferences[0].GetMetadata("IntegrationType").Should().Be("Tabs");
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [TestMethod]
        public void Execute_WithProductsIntegrationType_PreservesValue()
        {
            // Arrange
            var projectPath = CreateTestDocsproj("ProductA.docsproj", "Mintlify");
            var reference = new TaskItem(projectPath);
            reference.SetMetadata("IntegrationType", "Products");

            _task.DocumentationReferences = [reference];
            _task.DocumentationType = "Mintlify";

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeTrue();
                _task.ResolvedDocumentationReferences[0].GetMetadata("IntegrationType").Should().Be("Products");
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        #endregion

        #region Complex Scenario Tests

        [TestMethod]
        public void Execute_WithMixedValidAndInvalidReferences_ResolvesValidOnes()
        {
            // Arrange
            var validPath1 = CreateTestDocsproj("Valid1.docsproj", "Mintlify");
            var validPath2 = CreateTestDocsproj("Valid2.docsproj", "Mintlify");
            var invalidPath = Path.Combine(_tempDirectory, "DoesNotExist.docsproj");

            var ref1 = new TaskItem(validPath1);
            ref1.SetMetadata("DestinationPath", "services/valid1");

            var ref2 = new TaskItem(invalidPath); // This one will fail

            var ref3 = new TaskItem(validPath2);
            ref3.SetMetadata("DestinationPath", "services/valid2");

            _task.DocumentationReferences = [ref1, ref2, ref3];
            _task.DocumentationType = "Mintlify";

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeFalse(); // Should fail because of the invalid reference
                _buildEngine.LoggedErrors.Should().NotBeEmpty();
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [TestMethod]
        public void Execute_WithMultipleTypesMixedWithSkips_ResolvesMatchingTypes()
        {
            // Arrange
            var mintlifyPath1 = CreateTestDocsproj("Mintlify1.docsproj", "Mintlify");
            var mintlifyPath2 = CreateTestDocsproj("Mintlify2.docsproj", "Mintlify");
            var docfxPath = CreateTestDocsproj("DocFX1.docsproj", "DocFX"); // Will be skipped

            var ref1 = new TaskItem(mintlifyPath1);
            ref1.SetMetadata("DestinationPath", "services/m1");

            var ref2 = new TaskItem(docfxPath);
            ref2.SetMetadata("DestinationPath", "services/df1");

            var ref3 = new TaskItem(mintlifyPath2);
            ref3.SetMetadata("DestinationPath", "services/m2");

            _task.DocumentationReferences = [ref1, ref2, ref3];
            _task.DocumentationType = "Mintlify";

            var originalDir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_tempDirectory);

                // Act
                var result = _task.Execute();

                // Assert
                result.Should().BeTrue();
                _task.ResolvedDocumentationReferences.Should().HaveCount(2); // Only the 2 Mintlify references
                _task.ResolvedDocumentationReferences[0]!.GetMetadata("DestinationPath").Should().Be("services/m1");
                _task.ResolvedDocumentationReferences[1]!.GetMetadata("DestinationPath").Should().Be("services/m2");
                _buildEngine.LoggedWarnings.Where(w => w.Message != null).Should().Contain(w => w.Message!.Contains("Skipping") && w.Message.Contains("DocFX"));
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        #endregion

    }

}
