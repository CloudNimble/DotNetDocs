using System;
using System.IO;
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
    /// Verifies that code samples and nested lists keep their indentation all the way through the pipeline to the rendered Markdown.
    /// </summary>
    /// <remarks>
    /// Indentation passes through three stages: extraction from the XML documentation file, the transformer, and the renderer.
    /// These tests run all three with real content and compare the rendered page exactly.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public class MarkdownRendererIndentationTests : DotNetDocsTestBase
    {

        #region Fields

        private const string BasicScenariosNamespace = "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios";

        private string _testOutputPath = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            _testOutputPath = Path.Combine(Path.GetTempPath(), $"MDIndentationTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testOutputPath);

            TestHostBuilder.ConfigureServices((context, services) =>
            {
                services.AddDotNetDocsPipeline(pipeline =>
                {
                    pipeline.UseMarkdownRenderer()
                        .ConfigureContext(ctx =>
                        {
                            ctx.DocumentationRootPath = _testOutputPath;
                            ctx.ApiReferencePath = "api-reference";
                            ctx.FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder);
                        });
                });
            });

            TestSetup();
        }

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
        public async Task ProcessAsync_ConceptualMemberExample_RendersCodeWithEveryIndentationLevel()
        {
            var context = GetService<ProjectContext>()!;
            await WriteConceptualFileAsync(context.GetConceptualMemberPath(BasicScenariosNamespace, nameof(SimpleClass), nameof(SimpleClass.DoWork)), DocConstants.ExamplesFileName, IndentationSamples.ComplexCodeExample);

            await RunPipelineAsync();

            (await ReadTypePageAsync(context, nameof(SimpleClass))).Should().Contain(IndentationSamples.ComplexCodeExample.ReplaceLineEndings());
        }

        [TestMethod]
        public async Task ProcessAsync_ConceptualTypeExampleWithNestedList_RendersItUnchanged()
        {
            var context = GetService<ProjectContext>()!;
            await WriteConceptualFileAsync(context.GetConceptualTypePath(BasicScenariosNamespace, nameof(SimpleClass)), DocConstants.ExamplesFileName, IndentationSamples.ProseWithNestedListExample);

            await RunPipelineAsync();

            (await ReadTypePageAsync(context, nameof(SimpleClass))).Should().Contain(IndentationSamples.ProseWithNestedListExample.ReplaceLineEndings(),
                "an indented second line must not change how much indentation is removed from the other lines");
        }

        [TestMethod]
        public async Task ProcessAsync_ConceptualConsiderationsWithNestedBullets_RendersThemNested()
        {
            var context = GetService<ProjectContext>()!;
            await WriteConceptualFileAsync(context.GetConceptualTypePath(BasicScenariosNamespace, nameof(SimpleClass)), DocConstants.ConsiderationsFileName, IndentationSamples.NestedConsiderations);

            await RunPipelineAsync();

            (await ReadTypePageAsync(context, nameof(SimpleClass))).Should().Contain(IndentationSamples.NestedConsiderations.ReplaceLineEndings());
        }

        [TestMethod]
        public async Task ProcessAsync_XmlExampleWithChainedCalls_RendersTheRelativeIndentation()
        {
            var context = GetService<ProjectContext>()!;

            await RunPipelineAsync();

            var page = await ReadTypePageAsync(context, "ClassWithFullDocs", "CloudNimble.DotNetDocs.Tests.Shared.EdgeCases");
            page.Should().Contain("""
                ```csharp
                var numbers = Enumerable.Range(1, 10)
                    .Where(x => x % 2 == 0)
                    .Select(x => new
                    {
                        Number = x,
                        Square = x * x,
                        Cube = x * x * x
                    })
                    .ToList();
                ```
                """.ReplaceLineEndings(), "only the indentation the XML file adds to every line is removed");
        }

        #endregion

        #region Private Methods

        private async Task<string> ReadTypePageAsync(ProjectContext context, string typeName, string namespaceName = BasicScenariosNamespace)
        {
            // Conceptual content keeps the line endings of its file, so compare text with normalized line endings.
            var pagePath = Path.Combine(_testOutputPath, context.ApiReferencePath, $"{context.GetTypePagePath(namespaceName, typeName)}.md");
            return (await File.ReadAllTextAsync(pagePath)).ReplaceLineEndings();
        }

        private async Task RunPipelineAsync()
        {
            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var manager = GetService<DocumentationManager>()!;

            await manager.ProcessAsync(assemblyPath, Path.ChangeExtension(assemblyPath, ".xml"));
        }

        private static async Task WriteConceptualFileAsync(string folder, string fileName, string content)
        {
            Directory.CreateDirectory(folder);
            await File.WriteAllTextAsync(Path.Combine(folder, fileName), content);
        }

        #endregion

    }

}
