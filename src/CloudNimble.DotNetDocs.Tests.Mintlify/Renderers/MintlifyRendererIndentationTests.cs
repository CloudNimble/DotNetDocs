using System;
using System.IO;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Mintlify;
using CloudNimble.DotNetDocs.Tests.Core.Helpers;
using CloudNimble.DotNetDocs.Tests.Shared;
using CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core.Models;

namespace CloudNimble.DotNetDocs.Tests.Mintlify.Renderers
{

    /// <summary>
    /// Verifies that conceptual code samples and nested lists keep their indentation all the way to the rendered Mintlify page.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MintlifyRendererIndentationTests : DotNetDocsTestBase
    {

        #region Fields

        private const string BasicScenariosNamespace = "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios";

        private string _testOutputPath = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            _testOutputPath = Path.Combine(Path.GetTempPath(), $"MintlifyIndentationTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testOutputPath);

            TestHostBuilder.ConfigureServices((context, services) =>
            {
                services.AddDotNetDocsPipeline(pipeline =>
                {
                    pipeline.UseMintlifyRenderer(options =>
                        {
                            options.Template = new DocsJsonConfig
                            {
                                Name = "Indentation Test",
                                Theme = "mint",
                                Colors = new ColorsConfig { Primary = "#000000" }
                            };
                        })
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

            (await ReadTypePageAsync(context)).Should().Contain(IndentationSamples.ComplexCodeExample.ReplaceLineEndings());
        }

        [TestMethod]
        public async Task ProcessAsync_ConceptualTypeExampleWithNestedList_RendersItUnchanged()
        {
            var context = GetService<ProjectContext>()!;
            await WriteConceptualFileAsync(context.GetConceptualTypePath(BasicScenariosNamespace, nameof(SimpleClass)), DocConstants.ExamplesFileName, IndentationSamples.ProseWithNestedListExample);

            await RunPipelineAsync();

            (await ReadTypePageAsync(context)).Should().Contain(IndentationSamples.ProseWithNestedListExample.ReplaceLineEndings(),
                "an indented second line must not change how much indentation is removed from the other lines");
        }

        [TestMethod]
        public async Task ProcessAsync_ConceptualMemberConsiderationsWithNestedBullets_RendersThemNested()
        {
            var context = GetService<ProjectContext>()!;
            await WriteConceptualFileAsync(context.GetConceptualMemberPath(BasicScenariosNamespace, nameof(SimpleClass), nameof(SimpleClass.DoWork)), DocConstants.ConsiderationsFileName, IndentationSamples.NestedConsiderations);

            await RunPipelineAsync();

            (await ReadTypePageAsync(context)).Should().Contain(IndentationSamples.NestedConsiderations.ReplaceLineEndings());
        }

        #endregion

        #region Private Methods

        private async Task<string> ReadTypePageAsync(ProjectContext context)
        {
            // Conceptual content keeps the line endings of its file, so compare text with normalized line endings.
            var pagePath = Path.Combine(_testOutputPath, context.ApiReferencePath, $"{context.GetTypePagePath(BasicScenariosNamespace, nameof(SimpleClass))}.mdx");
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
