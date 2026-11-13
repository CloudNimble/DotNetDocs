using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CloudNimble.DotNetDocs.Sdk.Tasks;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Sdk.Tasks
{

    /// <summary>
    /// Evaluates a real <c>.docsproj</c> against the SDK's <c>Sdk.props</c> and <c>Sdk.targets</c> to verify that the SDK's
    /// MSBuild properties actually reach <see cref="GenerateDocumentationTask"/>.
    /// </summary>
    /// <remarks>
    /// The task's own tests can't catch a property the targets file forgets to pass, because they set task parameters directly.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public class SdkTargetsTests : DotNetDocsTestBase
    {

        #region Fields

        private ProjectCollection _projectCollection = null!;
        private string _testProjectPath = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            _testProjectPath = Path.Combine(Path.GetTempPath(), $"SdkTargetsTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testProjectPath);
            _projectCollection = new ProjectCollection();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _projectCollection.UnloadAllProjects();
            _projectCollection.Dispose();

            if (Directory.Exists(_testProjectPath))
            {
                Directory.Delete(_testProjectPath, true);
            }
        }

        #endregion

        #region Tests

        [TestMethod]
        public void GenerateDocumentation_PassesEverySdkPropertyThatMatchesATaskParameter()
        {
            var project = LoadDocsProject(string.Empty);
            var task = GetGenerateDocumentationTask(project);

            // A task parameter with an SDK property of the same name is a setting users expect to work from the .docsproj.
            var unwired = GetSettableTaskParameters()
                .Where(name => !string.IsNullOrWhiteSpace(project.GetPropertyValue(name)))
                .Where(name => !task.Parameters.ContainsKey(name))
                .ToList();

            unwired.Should().BeEmpty("every SDK property with a matching GenerateDocumentationTask parameter must be passed to the task");
        }

        [TestMethod]
        [DataRow("", "conceptual")]
        [DataRow("<ConceptualPath>content/concepts</ConceptualPath>", "content/concepts")]
        public void GenerateDocumentation_PassesConceptualPath(string propertyXml, string expected)
        {
            var project = LoadDocsProject(propertyXml);
            var task = GetGenerateDocumentationTask(project);

            task.Parameters.Should().ContainKey(nameof(GenerateDocumentationTask.ConceptualPath));
            project.ExpandString(task.Parameters[nameof(GenerateDocumentationTask.ConceptualPath)])
                .Should().Be(expected, "the task should receive the ConceptualPath set in the .docsproj, or the SDK default");
        }

        [TestMethod]
        public void SdkProps_WithCustomConceptualPath_IncludesConceptualFilesInProject()
        {
            // Use a .json file, because the SDK already globs .mdz, .md, .mdx, and image files anywhere in the project.
            var conceptualFile = Path.Combine(_testProjectPath, "content", "concepts", "MyNamespace", "metadata.json");
            Directory.CreateDirectory(Path.GetDirectoryName(conceptualFile)!);
            File.WriteAllText(conceptualFile, "{}");
            File.WriteAllText(Path.Combine(_testProjectPath, "docs.json"), "{}");

            var project = LoadDocsProject("<ConceptualPath>content/concepts</ConceptualPath>");

            project.GetItems("None")
                .Select(item => Path.GetFullPath(Path.Combine(_testProjectPath, item.EvaluatedInclude)))
                .Should().Contain(Path.GetFullPath(conceptualFile), "files in a custom ConceptualPath should appear in the project");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the <see cref="GenerateDocumentationTask"/> invocation from the <c>GenerateDocumentation</c> target.
        /// </summary>
        /// <param name="project">The evaluated project instance.</param>
        /// <returns>The task invocation with its unevaluated parameters.</returns>
        private static ProjectTaskInstance GetGenerateDocumentationTask(ProjectInstance project)
        {
            project.Targets.Should().ContainKey("GenerateDocumentation");

            return project.Targets["GenerateDocumentation"].Tasks
                .Single(task => task.Name == typeof(GenerateDocumentationTask).FullName);
        }

        /// <summary>
        /// Gets the names of the task parameters that MSBuild can set.
        /// </summary>
        /// <returns>The public settable properties declared by <see cref="GenerateDocumentationTask"/>, excluding outputs.</returns>
        private static IEnumerable<string> GetSettableTaskParameters()
        {
            return typeof(GenerateDocumentationTask)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(property => property.CanWrite && property.GetCustomAttribute<OutputAttribute>() is null)
                .Select(property => property.Name);
        }

        /// <summary>
        /// Copies the SDK's props and targets from this repository into a staging folder next to the test project.
        /// </summary>
        /// <returns>The staging folder containing <c>Sdk.props</c> and <c>Sdk.targets</c>.</returns>
        /// <remarks>
        /// The NoTargets SDK is bundled into the package at pack time, so it isn't next to the source files. These tests check only
        /// this SDK's own wiring, so the NoTargets imports are replaced with empty projects.
        /// </remarks>
        private string StageSdk()
        {
            var sourcePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, projectPath, "..", "CloudNimble.DotNetDocs.Sdk", "Sdk"));
            File.Exists(Path.Combine(sourcePath, "Sdk.props")).Should().BeTrue($"the SDK files should be at '{sourcePath}'");

            var sdkPath = Path.Combine(_testProjectPath, ".sdk");
            Directory.CreateDirectory(Path.Combine(sdkPath, "NoTargets"));
            File.Copy(Path.Combine(sourcePath, "Sdk.props"), Path.Combine(sdkPath, "Sdk.props"));
            File.Copy(Path.Combine(sourcePath, "Sdk.targets"), Path.Combine(sdkPath, "Sdk.targets"));
            File.WriteAllText(Path.Combine(sdkPath, "NoTargets", "Sdk.props"), "<Project />");
            File.WriteAllText(Path.Combine(sdkPath, "NoTargets", "Sdk.targets"), "<Project />");

            return sdkPath;
        }

        /// <summary>
        /// Writes and evaluates a <c>.docsproj</c> that imports the SDK files from this repository.
        /// </summary>
        /// <param name="propertyXml">Property elements to set in the project body, between the SDK imports.</param>
        /// <returns>The evaluated project instance.</returns>
        private ProjectInstance LoadDocsProject(string propertyXml)
        {
            var sdkPath = StageSdk();

            var projectFile = Path.Combine(_testProjectPath, "Test.docsproj");
            File.WriteAllText(projectFile, $"""
                <Project>
                    <Import Project="{Path.Combine(sdkPath, "Sdk.props")}" />
                    <PropertyGroup>
                        {propertyXml}
                    </PropertyGroup>
                    <Import Project="{Path.Combine(sdkPath, "Sdk.targets")}" />
                </Project>
                """);

            return new Project(projectFile, null, null, _projectCollection).CreateProjectInstance();
        }

        #endregion

    }

}
