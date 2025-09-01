using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Core.Renderers;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core.Configuration
{

    /// <summary>
    /// Tests for <see cref="DotNetDocsBuilder"/>.
    /// </summary>
    [TestClass]
    public class DotNetDocsBuilderTests : DotNetDocsTestBase
    {

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            // Reset TestHostBuilder for each test - handled by base class
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestTearDown();
        }

        #endregion

        #region Pipeline Builder Tests

        [TestMethod]
        public void AddDotNetDocsPipeline_ConfiguresBasicPipeline()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices((context, services) => 
                services.AddDotNetDocsPipeline(pipeline =>
                {
                    pipeline.UseMarkdownRenderer();
                }));
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().HaveCount(1);
            renderers.Should().AllBeOfType<MarkdownRenderer>();

            var manager = TestHost.Services.GetService<DocumentationManager>();
            manager.Should().NotBeNull();

            var context = TestHost.Services.GetService<ProjectContext>();
            context.Should().NotBeNull();
        }

        [TestMethod]
        public void AddDotNetDocsPipeline_ConfiguresMultipleRenderers()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices((context, services) =>
                services.AddDotNetDocsPipeline(pipeline =>
                {
                    pipeline
                        .UseMarkdownRenderer()
                        .UseJsonRenderer()
                        .UseYamlRenderer();
                }));
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().HaveCount(3);
            renderers.Should().Contain(r => r is MarkdownRenderer);
            renderers.Should().Contain(r => r is JsonRenderer);
            renderers.Should().Contain(r => r is YamlRenderer);
        }

        [TestMethod]
        public void AddDotNetDocsPipeline_ConfiguresJsonRendererOptions()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices((context, services) =>
                services.AddDotNetDocsPipeline(pipeline =>
                {
                    pipeline.UseJsonRenderer(options =>
                    {
                        options.SerializerOptions = new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };
                    });
                }));
            TestSetup();

            // Assert
            var options = TestHost.Services.GetService<IOptions<JsonRendererOptions>>();
            options.Should().NotBeNull();
            options!.Value.SerializerOptions.WriteIndented.Should().BeTrue();
            options.Value.SerializerOptions.DefaultIgnoreCondition.Should().Be(JsonIgnoreCondition.WhenWritingNull);
        }

        [TestMethod]
        public void AddDotNetDocsPipeline_ConfiguresProjectContext()
        {
            // Arrange
            const string expectedPath = "pipeline/docs";
            const bool expectedPlaceholders = false;

            // Act
            TestHostBuilder.ConfigureServices((context, services) =>
                services.AddDotNetDocsPipeline(pipeline =>
                {
                    pipeline.ConfigureContext(ctx =>
                    {
                        ctx.OutputPath = expectedPath;
                        ctx.ShowPlaceholders = expectedPlaceholders;
                    });
                }));
            TestSetup();

            // Assert
            var context = TestHost.Services.GetRequiredService<ProjectContext>();
            context.OutputPath.Should().Be(expectedPath);
            context.ShowPlaceholders.Should().Be(expectedPlaceholders);
        }

        [TestMethod]
        public void AddDotNetDocsPipeline_AddsCustomComponents()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices((context, services) =>
                services.AddDotNetDocsPipeline(pipeline =>
                {
                    pipeline
                        .AddRenderer<TestRenderer>()
                        .AddEnricher<TestEnricher>()
                        .AddTransformer<TestTransformer>();
                }));
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().HaveCount(1);
            renderers.Should().AllBeOfType<TestRenderer>();

            var enrichers = TestHost.Services.GetServices<IDocEnricher>().ToList();
            enrichers.Should().HaveCount(1);
            enrichers.Should().AllBeOfType<TestEnricher>();

            var transformers = TestHost.Services.GetServices<IDocTransformer>().ToList();
            transformers.Should().HaveCount(1);
            transformers.Should().AllBeOfType<TestTransformer>();
        }

        [TestMethod]
        public void AddDotNetDocsPipeline_ComplexConfiguration()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices((context, services) =>
                services.AddDotNetDocsPipeline(pipeline =>
                {
                    pipeline
                        .UseMarkdownRenderer()
                        .UseJsonRenderer(options => options.SerializerOptions = new JsonSerializerOptions { WriteIndented = true })
                        .AddEnricher<TestEnricher>()
                        .AddTransformer<TestTransformer>()
                        .ConfigureContext(ctx =>
                        {
                            ctx.OutputPath = "complex/output";
                            ctx.ShowPlaceholders = false;
                        });
                }));
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().HaveCount(2);

            var enrichers = TestHost.Services.GetServices<IDocEnricher>().ToList();
            enrichers.Should().HaveCount(1);

            var transformers = TestHost.Services.GetServices<IDocTransformer>().ToList();
            transformers.Should().HaveCount(1);

            var context = TestHost.Services.GetRequiredService<ProjectContext>();
            context.OutputPath.Should().Be("complex/output");
        }

        [TestMethod]
        public void AddDotNetDocsPipeline_ThrowsOnNullConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            Action act = () => services.AddDotNetDocsPipeline(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void DotNetDocsPipeline_MultipleRenderers_NoDuplicates()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices((context, services) =>
                services.AddDotNetDocsPipeline(pipeline =>
                {
                    pipeline
                        .UseMarkdownRenderer()
                        .UseMarkdownRenderer() // Should not duplicate
                        .UseJsonRenderer()
                        .UseJsonRenderer() // Should not duplicate
                        .UseYamlRenderer()
                        .UseYamlRenderer(); // Should not duplicate
                }));
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().HaveCount(3, "each renderer should only be registered once");
            renderers.OfType<MarkdownRenderer>().Should().HaveCount(1);
            renderers.OfType<JsonRenderer>().Should().HaveCount(1);
            renderers.OfType<YamlRenderer>().Should().HaveCount(1);
        }

        [TestMethod]
        public void DotNetDocsPipeline_MultipleCustomComponents_NoDuplicates()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices((context, services) =>
                services.AddDotNetDocsPipeline(pipeline =>
                {
                    pipeline
                        .AddRenderer<TestRenderer>()
                        .AddRenderer<TestRenderer>() // Should not duplicate
                        .AddEnricher<TestEnricher>()
                        .AddEnricher<TestEnricher>() // Should not duplicate
                        .AddTransformer<TestTransformer>()
                        .AddTransformer<TestTransformer>(); // Should not duplicate
                }));
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().HaveCount(1);
            renderers.Should().AllBeOfType<TestRenderer>();

            var enrichers = TestHost.Services.GetServices<IDocEnricher>().ToList();
            enrichers.Should().HaveCount(1);
            enrichers.Should().AllBeOfType<TestEnricher>();

            var transformers = TestHost.Services.GetServices<IDocTransformer>().ToList();
            transformers.Should().HaveCount(1);
            transformers.Should().AllBeOfType<TestTransformer>();
        }

        #endregion

        #region Builder Instance Tests

        [TestMethod]
        public void DotNetDocsBuilder_ThrowsOnNullServices()
        {
            // Arrange & Act
            Action act = () => new DotNetDocsBuilder(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void DotNetDocsBuilder_ConfigureContext_ThrowsOnNullAction()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new DotNetDocsBuilder(services);

            // Act
            Action act = () => builder.ConfigureContext(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void DotNetDocsBuilder_Build_EnsuresRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new DotNetDocsBuilder(services);

            // Act
            builder.Build();
            using var serviceProvider = services.BuildServiceProvider();

            // Assert
            var manager = serviceProvider.GetService<DocumentationManager>();
            manager.Should().NotBeNull();

            var context = serviceProvider.GetService<ProjectContext>();
            context.Should().NotBeNull();
        }

        [TestMethod]
        public void DotNetDocsBuilder_ConfigureContext_ReplacesExistingContext()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(new ProjectContext { OutputPath = "original" });
            var builder = new DotNetDocsBuilder(services);

            // Act
            builder.ConfigureContext(ctx => ctx.OutputPath = "replaced");
            builder.Build();
            using var serviceProvider = services.BuildServiceProvider();

            // Assert
            var context = serviceProvider.GetRequiredService<ProjectContext>();
            context.OutputPath.Should().Be("replaced");
        }

        #endregion

        #region Fluent API Tests

        [TestMethod]
        public void DotNetDocsBuilder_SupportsMethodChaining()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new DotNetDocsBuilder(services);

            // Act
            var result = builder
                .UseMarkdownRenderer()
                .UseJsonRenderer()
                .UseYamlRenderer()
                .AddEnricher<TestEnricher>()
                .AddTransformer<TestTransformer>()
                .ConfigureContext(ctx => ctx.OutputPath = "chained");

            // Assert
            result.Should().BeSameAs(builder);
        }

        #endregion

        #region Test Helpers

        private class TestRenderer : IDocRenderer
        {
            public string OutputFormat => "Test";
            public Task RenderAsync(DocAssembly model, string outputPath, ProjectContext context) => Task.CompletedTask;
        }

        private class TestEnricher : IDocEnricher
        {
            public Task EnrichAsync(DocEntity entity, ProjectContext context) => Task.CompletedTask;
        }

        private class TestTransformer : IDocTransformer
        {
            public Task TransformAsync(DocEntity entity, ProjectContext context) => Task.CompletedTask;
        }

        #endregion

    }

}