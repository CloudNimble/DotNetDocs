using System;
using System.IO;
using System.Linq;
using CloudNimble.DotNetDocs.Sdk.Tasks;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Sdk.Tasks
{

    /// <summary>
    /// Tests for the DiscoverDocumentedProjectsTask class, focusing on project discovery and filtering logic.
    /// </summary>
    [TestClass]
    public class DiscoverDocumentedProjectsTaskTests : DotNetDocsTestBase
    {

        #region Fields

        private DiscoverDocumentedProjectsTask _task = null!;
        private string _tempDirectory = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            TestSetup();
            _task = new DiscoverDocumentedProjectsTask();

            // Set up a minimal build engine to avoid logging errors
            var buildEngine = new TestBuildEngine();
            _task.BuildEngine = buildEngine;

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

        #region Project Discovery Tests

        /// <summary>
        /// Tests that DiscoverProjectFiles finds all project types.
        /// </summary>
        [TestMethod]
        public void DiscoverProjectFiles_WithValidDirectory_FindsAllProjectTypes()
        {
            // Create test project files
            File.WriteAllText(Path.Combine(_tempDirectory, "Test1.csproj"), "<Project />");
            File.WriteAllText(Path.Combine(_tempDirectory, "Test2.vbproj"), "<Project />");
            File.WriteAllText(Path.Combine(_tempDirectory, "Test3.fsproj"), "<Project />");
            File.WriteAllText(Path.Combine(_tempDirectory, "NotProject.txt"), "text file");

            var result = _task.DiscoverProjectFiles(_tempDirectory, null);

            result.Should().HaveCount(3);
            result.Should().Contain(p => p.EndsWith("Test1.csproj"));
            result.Should().Contain(p => p.EndsWith("Test2.vbproj"));
            result.Should().Contain(p => p.EndsWith("Test3.fsproj"));
        }

        /// <summary>
        /// Tests that DiscoverProjectFiles excludes projects based on patterns.
        /// </summary>
        [TestMethod]
        public void DiscoverProjectFiles_WithExcludePatterns_ExcludesMatchingProjects()
        {
            // Create test project files
            File.WriteAllText(Path.Combine(_tempDirectory, "MyProject.csproj"), "<Project />");
            File.WriteAllText(Path.Combine(_tempDirectory, "MyProject.Tests.csproj"), "<Project />");
            File.WriteAllText(Path.Combine(_tempDirectory, "MyProject.IntegrationTests.csproj"), "<Project />");

            var excludePatterns = new[] { "Tests", "IntegrationTests" };
            var result = _task.DiscoverProjectFiles(_tempDirectory, excludePatterns);

            result.Should().HaveCount(1);
            result.Should().Contain(p => p.EndsWith("MyProject.csproj"));
        }

        /// <summary>
        /// Tests that DiscoverProjectFiles finds projects in subdirectories.
        /// </summary>
        [TestMethod]
        public void DiscoverProjectFiles_WithSubdirectories_FindsProjectsRecursively()
        {
            // Create subdirectory and project
            var subDir = Path.Combine(_tempDirectory, "SubFolder");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "SubProject.csproj"), "<Project />");
            File.WriteAllText(Path.Combine(_tempDirectory, "RootProject.csproj"), "<Project />");

            var result = _task.DiscoverProjectFiles(_tempDirectory, null);

            result.Should().HaveCount(2);
            result.Should().Contain(p => p.EndsWith("RootProject.csproj"));
            result.Should().Contain(p => p.EndsWith("SubProject.csproj"));
        }

        #endregion

        #region Project Filtering Tests

        /// <summary>
        /// Tests that ShouldIncludeProject returns true when no exclude patterns are provided.
        /// </summary>
        [TestMethod]
        public void ShouldIncludeProject_WithNoExcludePatterns_ReturnsTrue()
        {
            var projectPath = Path.Combine(_tempDirectory, "TestProject.csproj");

            var result = _task.ShouldIncludeProject(projectPath, null);

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ShouldIncludeProject returns true when project doesn't match exclude patterns.
        /// </summary>
        [TestMethod]
        public void ShouldIncludeProject_WithNonMatchingPatterns_ReturnsTrue()
        {
            var projectPath = Path.Combine(_tempDirectory, "MyProject.csproj");
            var excludePatterns = new[] { "Tests", "Samples" };

            var result = _task.ShouldIncludeProject(projectPath, excludePatterns);

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ShouldIncludeProject returns false when project matches exclude patterns.
        /// </summary>
        [TestMethod]
        public void ShouldIncludeProject_WithMatchingPattern_ReturnsFalse()
        {
            var projectPath = Path.Combine(_tempDirectory, "MyProject.Tests.csproj");
            var excludePatterns = new[] { "Tests", "Samples" };

            var result = _task.ShouldIncludeProject(projectPath, excludePatterns);

            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that ShouldIncludeProject matching is case insensitive.
        /// </summary>
        [TestMethod]
        public void ShouldIncludeProject_WithCaseInsensitiveMatch_ReturnsFalse()
        {
            var projectPath = Path.Combine(_tempDirectory, "MyProject.TESTS.csproj");
            var excludePatterns = new[] { "tests" };

            var result = _task.ShouldIncludeProject(projectPath, excludePatterns);

            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that ShouldIncludeProject handles wildcard patterns correctly.
        /// </summary>
        [TestMethod]
        public void ShouldIncludeProject_WithWildcardPattern_HandlesCorrectly()
        {
            var projectPath = Path.Combine(_tempDirectory, "MyProject.Tests.csproj");
            var excludePatterns = new[] { "*Tests*" };

            var result = _task.ShouldIncludeProject(projectPath, excludePatterns);

            result.Should().BeFalse();
        }

        #endregion

        #region Properties Tests

        /// <summary>
        /// Tests that _propertiesToEvaluate contains the expected properties and values.
        /// </summary>
        [TestMethod]
        public void PropertiesToEvaluate_ContainsExpectedProperties()
        {
            _task._propertiesToEvaluate.Should().ContainKey("IsTestProject")
                .WhoseValue.Should().BeFalse();
            _task._propertiesToEvaluate.Should().ContainKey("IsPackable")
                .WhoseValue.Should().BeTrue();
            _task._propertiesToEvaluate.Should().ContainKey("ExcludeFromDocumentation")
                .WhoseValue.Should().BeFalse();
        }

        /// <summary>
        /// Tests that properties can be modified for testing scenarios.
        /// </summary>
        [TestMethod]
        public void PropertiesToEvaluate_CanBeModified()
        {
            _task._propertiesToEvaluate["IsTestProject"] = true;
            _task._propertiesToEvaluate.Should().ContainKey("IsTestProject")
                .WhoseValue.Should().BeTrue();
        }

        #endregion

        #region Task Property Tests

        /// <summary>
        /// Tests that task properties have correct default values.
        /// </summary>
        [TestMethod]
        public void TaskProperties_HaveCorrectDefaults()
        {
            _task.Configuration.Should().Be("Release");
            _task.TargetFramework.Should().Be("net8.0");
            _task.DocumentedProjects.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that task properties can be set.
        /// </summary>
        [TestMethod]
        public void TaskProperties_CanBeSet()
        {
            _task.SolutionDir = "C:\\TestSolution";
            _task.Configuration = "Debug";
            _task.TargetFramework = "net9.0";
            _task.ExcludePatterns = new[] { "Tests" };

            _task.SolutionDir.Should().Be("C:\\TestSolution");
            _task.Configuration.Should().Be("Debug");
            _task.TargetFramework.Should().Be("net9.0");
            _task.ExcludePatterns.Should().ContainSingle("Tests");
        }

        #endregion

        #region Argument Validation Tests

        /// <summary>
        /// Tests that DiscoverProjectFiles throws ArgumentException for null or whitespace solution directory.
        /// </summary>
        [TestMethod]
        public void DiscoverProjectFiles_WithNullSolutionDir_ThrowsArgumentException()
        {
            var action = () => _task.DiscoverProjectFiles(null!, null);

            action.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Tests that DiscoverProjectFiles throws ArgumentException for whitespace solution directory.
        /// </summary>
        [TestMethod]
        public void DiscoverProjectFiles_WithWhitespaceSolutionDir_ThrowsArgumentException()
        {
            var action = () => _task.DiscoverProjectFiles("   ", null);

            action.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Tests that ShouldIncludeProject throws ArgumentException for null or whitespace project file path.
        /// </summary>
        [TestMethod]
        public void ShouldIncludeProject_WithNullProjectPath_ThrowsArgumentException()
        {
            var action = () => _task.ShouldIncludeProject(null!, null);

            action.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// Tests that ShouldIncludeProject throws ArgumentException for whitespace project file path.
        /// </summary>
        [TestMethod]
        public void ShouldIncludeProject_WithWhitespaceProjectPath_ThrowsArgumentException()
        {
            var action = () => _task.ShouldIncludeProject("   ", null);

            action.Should().Throw<ArgumentException>();
        }

        #endregion


    }

}