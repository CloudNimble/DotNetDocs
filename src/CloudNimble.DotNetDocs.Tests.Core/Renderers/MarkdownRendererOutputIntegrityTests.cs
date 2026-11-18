using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Core.Renderers;
using CloudNimble.DotNetDocs.Tests.Core.Helpers;
using CloudNimble.DotNetDocs.Tests.Shared;
using CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core.Renderers
{

    /// <summary>
    /// Verifies that the files <see cref="MarkdownRenderer"/> writes agree with each other in every <see cref="NamespaceMode"/>.
    /// </summary>
    /// <remarks>
    /// These tests run the full documentation pipeline and check the output on disk: every link must resolve to a rendered file,
    /// every entity must get its own path, and the files written must be exactly the ones the shared path helpers predict.
    /// Unlike baselines, they don't depend on previously recorded output being correct.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public class MarkdownRendererOutputIntegrityTests : DotNetDocsTestBase
    {

        #region Fields

        private string _testOutputPath = null!;

        #endregion

        #region Test Lifecycle

        [TestCleanup]
        public void TestCleanup()
        {
            TestTearDown();

            if (Directory.Exists(_testOutputPath))
            {
                Directory.Delete(_testOutputPath, true);
            }
        }

        #endregion

        #region Tests

        [TestMethod]
        [DataRow(NamespaceMode.File, '-', "api-reference")]
        [DataRow(NamespaceMode.File, '_', "")]
        [DataRow(NamespaceMode.Folder, '-', "api-reference")]
        [DataRow(NamespaceMode.Folder, '-', "")]
        public async Task ProcessAsync_EveryLocalLink_ResolvesToAnotherRenderedFile(NamespaceMode mode, char separator, string apiReferencePath)
        {
            ConfigureTest(mode, separator, apiReferencePath);

            await RunPipelineAsync();

            // Plain Markdown viewers don't resolve extensionless or folder links, so relative links must name a real file.
            var brokenLinks = RenderedOutputInspector.FindBrokenLinks(_testOutputPath, GetApiReferenceOutputPath(apiReferencePath), "md", allowSiteStyleLinks: false);
            brokenLinks.Should().BeEmpty("every link in the rendered Markdown should point at a different file that was written");
        }

        [TestMethod]
        [DataRow(NamespaceMode.File, '-', "api-reference")]
        [DataRow(NamespaceMode.File, '_', "")]
        [DataRow(NamespaceMode.Folder, '-', "api-reference")]
        [DataRow(NamespaceMode.Folder, '-', "")]
        public async Task ProcessAsync_RenderedFiles_MatchPredictedPaths(NamespaceMode mode, char separator, string apiReferencePath)
        {
            ConfigureTest(mode, separator, apiReferencePath);
            var predictor = new PathPredictor(GetService<ProjectContext>()!);
            var model = await DocumentTestAssemblyAsync();
            var outputPath = GetApiReferenceOutputPath(apiReferencePath);

            await RunPipelineAsync();

            var expected = predictor.GetPagePaths(model, outputPath, "md").Select(Path.GetFullPath).ToList();
            var actual = Directory.EnumerateFiles(outputPath, "*.md", SearchOption.AllDirectories).Select(Path.GetFullPath);
            actual.Should().BeEquivalentTo(expected, "the renderer should write exactly the pages the shared path helpers predict, with no orphans or gaps");

            var namespacePages = model.Namespaces.Select(ns => Path.GetFullPath(predictor.GetNamespacePagePath(ns, outputPath, "md")));
            var indexLinks = RenderedOutputInspector.GetLinkedFiles(expected[0], _testOutputPath, "md", allowSiteStyleLinks: false);
            indexLinks.Should().Contain(namespacePages, "the assembly index should link to every namespace page");
        }

        [TestMethod]
        [DataRow(NamespaceMode.File, '-')]
        [DataRow(NamespaceMode.File, '_')]
        [DataRow(NamespaceMode.Folder, '-')]
        public async Task GetPagePaths_EveryNamespaceAndType_GetsAUniquePath(NamespaceMode mode, char separator)
        {
            ConfigureTest(mode, separator, "api-reference");
            var predictor = new PathPredictor(GetService<ProjectContext>()!);
            var model = await DocumentTestAssemblyAsync();

            var paths = predictor.GetPagePaths(model, GetApiReferenceOutputPath("api-reference"), "md");

            // Compare case-insensitively, because Windows and macOS file systems would merge paths that differ only by case.
            paths.GroupBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .Should().BeEmpty("no two namespaces or types should be rendered to the same file");
        }

        [TestMethod]
        [DataRow(NamespaceMode.File, '-')]
        [DataRow(NamespaceMode.Folder, '-')]
        public async Task ProcessAsync_EditedConceptualFiles_AppearOnTheirOwnPages(NamespaceMode mode, char separator)
        {
            ConfigureTest(mode, separator, "api-reference");
            var context = GetService<ProjectContext>()!;
            var predictor = new PathPredictor(context);
            var model = await DocumentTestAssemblyAsync();
            var outputPath = GetApiReferenceOutputPath("api-reference");
            var conceptualRoot = context.GetFullConceptualPath();

            await RunPipelineAsync();
            var markers = ConceptualRoundTrip.WriteUsageMarkers(conceptualRoot);

            // No renderer generates member placeholders, but DocumentationManager loads them, so write one by hand.
            var memberMarker = ConceptualRoundTrip.WriteUsageMarker(conceptualRoot,
                ConceptualRoundTrip.GetExpectedFolder("CloudNimble.DotNetDocs.Tests.Shared.EdgeCases", "ClassWithNoDocs", "UndocumentedMethod"));
            await RunPipelineAsync();

            var renderedContent = string.Join(Environment.NewLine,
                Directory.EnumerateFiles(outputPath, "*.md", SearchOption.AllDirectories).Select(File.ReadAllText));
            markers.Should().NotBeEmpty("placeholders should be generated for every namespace and type");
            markers.Where(marker => !renderedContent.Contains(marker))
                .Should().BeEmpty("every edited conceptual file should be loaded and rendered");

            foreach (var ns in model.Namespaces)
            {
                File.ReadAllText(predictor.GetNamespacePagePath(ns, outputPath, "md"))
                    .Should().Contain(ConceptualRoundTrip.GetMarker(ConceptualRoundTrip.GetExpectedFolder(ns.Name)),
                        $"namespace '{ns.Name}' should show the usage file from its own conceptual folder");

                foreach (var type in ns.Types)
                {
                    File.ReadAllText(predictor.GetTypePagePath(type, ns, outputPath, "md"))
                        .Should().Contain(ConceptualRoundTrip.GetMarker(ConceptualRoundTrip.GetExpectedFolder(ns.Name, type.Name)),
                            $"type '{type.FullName}' should show the usage file from its own conceptual folder");
                }
            }

            renderedContent.Should().Contain(memberMarker, "member conceptual files should be loaded from inside their type's folder");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Configures the documentation pipeline for the specified naming options and builds the services.
        /// </summary>
        /// <param name="mode">The namespace mode to render with.</param>
        /// <param name="separator">The namespace separator used in <see cref="NamespaceMode.File"/>.</param>
        /// <param name="apiReferencePath">The API reference folder, relative to the documentation root.</param>
        private void ConfigureTest(NamespaceMode mode, char separator, string apiReferencePath)
        {
            _testOutputPath = Path.Combine(Path.GetTempPath(), $"MDIntegrityTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testOutputPath);

            TestHostBuilder.ConfigureServices((context, services) =>
            {
                services.AddDotNetDocsPipeline(pipeline =>
                {
                    pipeline.UseMarkdownRenderer()
                        .ConfigureContext(ctx =>
                        {
                            ctx.DocumentationRootPath = _testOutputPath;
                            ctx.ApiReferencePath = apiReferencePath;
                            ctx.FileNamingOptions = new FileNamingOptions(mode, separator);
                        });
                });
            });

            TestSetup();
        }

        /// <summary>
        /// Builds the documentation model for the shared test assembly with the configured project context.
        /// </summary>
        /// <returns>The documentation model the pipeline renders.</returns>
        private async Task<DocAssembly> DocumentTestAssemblyAsync()
        {
            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            using var manager = new AssemblyManager(assemblyPath, Path.ChangeExtension(assemblyPath, ".xml"));

            return await manager.DocumentAsync(GetService<ProjectContext>()!);
        }

        private string GetApiReferenceOutputPath(string apiReferencePath) => Path.Combine(_testOutputPath, apiReferencePath);

        /// <summary>
        /// Runs the full documentation pipeline over the shared test assembly.
        /// </summary>
        private async Task RunPipelineAsync()
        {
            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var manager = GetService<DocumentationManager>()!;

            await manager.ProcessAsync(assemblyPath, Path.ChangeExtension(assemblyPath, ".xml"));
        }

        #endregion

        #region Test Classes

        /// <summary>
        /// Exposes the shared <see cref="RendererBase"/> path helpers so tests can predict where pages should be written.
        /// </summary>
        private sealed class PathPredictor(ProjectContext context) : RendererBase(context)
        {

            /// <summary>
            /// Gets the path of a namespace's page.
            /// </summary>
            public string GetNamespacePagePath(DocNamespace ns, string outputPath, string extension) => GetNamespaceFilePath(ns, outputPath, extension);

            /// <summary>
            /// Gets the path of a type's page.
            /// </summary>
            public string GetTypePagePath(DocType type, DocNamespace ns, string outputPath, string extension) => GetTypeFilePath(type, ns, outputPath, extension);

            /// <summary>
            /// Gets the path of every page a renderer should write: the assembly index first, then each namespace and type page.
            /// </summary>
            public List<string> GetPagePaths(DocAssembly model, string outputPath, string extension)
            {
                List<string> paths = [Path.Combine(outputPath, $"index.{extension}")];

                foreach (var ns in model.Namespaces)
                {
                    paths.Add(GetNamespaceFilePath(ns, outputPath, extension));
                    paths.AddRange(ns.Types.Select(type => GetTypeFilePath(type, ns, outputPath, extension)));
                }

                return paths;
            }

        }

        #endregion

    }

}
