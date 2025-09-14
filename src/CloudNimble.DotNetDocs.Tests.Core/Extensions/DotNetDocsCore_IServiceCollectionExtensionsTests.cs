using System.Linq;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Renderers;
using CloudNimble.DotNetDocs.Core.Transformers;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core.Extensions
{

    /// <summary>
    /// Tests for <see cref="DotNetDocsCore_IServiceCollectionExtensions"/>.
    /// </summary>
    [TestClass]
    public class DotNetDocsCore_IServiceCollectionExtensionsTests : DotNetDocsTestBase
    {

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            // Reset the TestHostBuilder for each test
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestTearDown(); 
        }

        #endregion

        #region AddDotNetDocs Tests

        [TestMethod]
        public void AddDotNetDocs_RegistersAllCoreServices()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => services.AddDotNetDocs());
            TestSetup();

            // Assert
            var projectContext = TestHost.Services.GetService<ProjectContext>();
            projectContext.Should().NotBeNull();

            var documentationManager = TestHost.Services.GetService<DocumentationManager>();
            documentationManager.Should().NotBeNull();
        }

        [TestMethod]
        public void AddDotNetDocs_RegistersAllBuiltInRenderers()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => services.AddDotNetDocs());
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().HaveCount(3);
            renderers.Should().Contain(r => r is MarkdownRenderer);
            renderers.Should().Contain(r => r is JsonRenderer);
            renderers.Should().Contain(r => r is YamlRenderer);
        }

        [TestMethod]
        public void AddDotNetDocs_ConfiguresProjectContext()
        {
            // Arrange
            const string expectedPath = "custom/docs";
            const bool expectedShowPlaceholders = false;

            // Act
            TestHostBuilder.ConfigureServices(services => 
                services.AddDotNetDocs(context =>
                {
                    context.DocumentationRootPath = expectedPath;
                    context.ShowPlaceholders = expectedShowPlaceholders;
                }));
            TestSetup();

            // Assert
            var projectContext = TestHost.Services.GetRequiredService<ProjectContext>();
            projectContext.DocumentationRootPath.Should().Be(expectedPath);
            projectContext.ShowPlaceholders.Should().Be(expectedShowPlaceholders);
        }

        [TestMethod]
        public void AddDotNetDocs_ProjectContextIsSingleton()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => services.AddDotNetDocs());
            TestSetup();

            // Assert
            var context1 = TestHost.Services.GetRequiredService<ProjectContext>();
            var context2 = TestHost.Services.GetRequiredService<ProjectContext>();
            context1.Should().BeSameAs(context2);
        }

        [TestMethod]
        public void AddDotNetDocs_DocumentationManagerIsScoped()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => services.AddDotNetDocs());
            TestSetup();

            // Assert
            DocumentationManager manager1, manager2;
            using (var scope = TestHost.Services.CreateScope())
            {
                manager1 = scope.ServiceProvider.GetRequiredService<DocumentationManager>();
            }
            using (var scope = TestHost.Services.CreateScope())
            {
                manager2 = scope.ServiceProvider.GetRequiredService<DocumentationManager>();
            }
            manager1.Should().NotBeSameAs(manager2);
        }

        #endregion

        #region AddDotNetDocsCore Tests

        [TestMethod]
        public void AddDotNetDocsCore_RegistersOnlyCoreServices()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => services.AddDotNetDocsCore());
            TestSetup();

            // Assert
            var projectContext = TestHost.Services.GetService<ProjectContext>();
            projectContext.Should().NotBeNull();

            var documentationManager = TestHost.Services.GetService<DocumentationManager>();
            documentationManager.Should().NotBeNull();

            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().BeEmpty();
        }

        [TestMethod]
        public void AddDotNetDocsCore_ConfiguresProjectContext()
        {
            // Arrange
            const string expectedPath = "api/docs";

            // Act
            TestHostBuilder.ConfigureServices(services => 
                services.AddDotNetDocsCore(context =>
                {
                    context.DocumentationRootPath = expectedPath;
                }));
            TestSetup();

            // Assert
            var projectContext = TestHost.Services.GetRequiredService<ProjectContext>();
            projectContext.DocumentationRootPath.Should().Be(expectedPath);
        }

        #endregion

        #region Individual Renderer Tests

        [TestMethod]
        public void AddMarkdownRenderer_RegistersOnlyMarkdownRenderer()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => 
            {
                services.AddDotNetDocsCore();
                services.AddMarkdownRenderer();
            });
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().HaveCount(1);
            renderers.Should().AllBeOfType<MarkdownRenderer>();
        }

        [TestMethod]
        public void AddJsonRenderer_RegistersOnlyJsonRenderer()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => 
            {
                services.AddDotNetDocsCore();
                services.AddJsonRenderer();
            });
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().HaveCount(1);
            renderers.Should().AllBeOfType<JsonRenderer>();
        }

        [TestMethod]
        public void AddJsonRenderer_ConfiguresOptions()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => 
            {
                services.AddDotNetDocsCore();
                services.AddJsonRenderer(options =>
                {
                    options.SerializerOptions = new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    };
                });
            });
            TestSetup();

            // Assert
            var options = TestHost.Services.GetService<IOptions<JsonRendererOptions>>();
            options.Should().NotBeNull();
            options!.Value.SerializerOptions.WriteIndented.Should().BeTrue();
            options.Value.SerializerOptions.DefaultIgnoreCondition.Should().Be(System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull);
        }

        [TestMethod]
        public void AddYamlRenderer_RegistersOnlyYamlRenderer()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => 
            {
                services.AddDotNetDocsCore();
                services.AddYamlRenderer();
            });
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().HaveCount(1);
            renderers.Should().AllBeOfType<YamlRenderer>();
        }

        #endregion

        #region Generic Registration Tests

        [TestMethod]
        public void AddDocRenderer_RegistersCustomRenderer()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => 
            {
                services.AddDotNetDocsCore();
                services.AddDocRenderer<TestRenderer>();
            });
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().HaveCount(1);
            renderers.Should().AllBeOfType<TestRenderer>();
        }

        [TestMethod]
        public void AddDocEnricher_RegistersCustomEnricher()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => 
            {
                services.AddDotNetDocsCore();
                services.AddDocEnricher<TestEnricher>();
            });
            TestSetup();

            // Assert
            var enrichers = TestHost.Services.GetServices<IDocEnricher>().ToList();
            enrichers.Should().HaveCount(1);
            enrichers.Should().AllBeOfType<TestEnricher>();
        }

        [TestMethod]
        public void AddDocTransformer_RegistersCustomTransformer()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => 
            {
                services.AddDotNetDocsCore();
                services.AddDocTransformer<TestTransformer>();
            });
            TestSetup();

            // Assert
            var transformers = TestHost.Services.GetServices<IDocTransformer>().ToList();
            transformers.Should().HaveCount(1);
            transformers.Should().AllBeOfType<TestTransformer>();
        }

        #endregion

        #region TryAdd Behavior Tests

        [TestMethod]
        public void MultipleAddDotNetDocs_DoesNotDuplicateServices()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => 
            {
                services.AddDotNetDocs();
                services.AddDotNetDocs(); // Second call should not duplicate
            });
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().HaveCount(3, "each renderer type should only be registered once");
            
            // Verify each specific renderer type appears exactly once
            renderers.OfType<MarkdownRenderer>().Should().HaveCount(1);
            renderers.OfType<JsonRenderer>().Should().HaveCount(1);
            renderers.OfType<YamlRenderer>().Should().HaveCount(1);
        }

        [TestMethod]
        public void AddRenderer_AfterAddDotNetDocs_DoesNotDuplicate()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => 
            {
                services.AddDotNetDocs();
                services.AddMarkdownRenderer(); // Try to add again - should not duplicate
            });
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            var markdownRenderers = renderers.OfType<MarkdownRenderer>().ToList();
            markdownRenderers.Should().HaveCount(1, "MarkdownRenderer should only be registered once");
        }

        [TestMethod]
        public void MultipleAddMarkdownRenderer_DoesNotDuplicate()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => 
            {
                services.AddDotNetDocsCore();
                services.AddMarkdownRenderer();
                services.AddMarkdownRenderer(); // Second call should not duplicate
                services.AddMarkdownRenderer(); // Third call should not duplicate
            });
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().HaveCount(1);
            renderers.OfType<MarkdownRenderer>().Should().HaveCount(1);
        }

        [TestMethod]
        public void MultipleAddJsonRenderer_DoesNotDuplicate()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => 
            {
                services.AddDotNetDocsCore();
                services.AddJsonRenderer();
                services.AddJsonRenderer(); // Second call should not duplicate
            });
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().HaveCount(1);
            renderers.OfType<JsonRenderer>().Should().HaveCount(1);
        }

        [TestMethod]
        public void MultipleAddYamlRenderer_DoesNotDuplicate()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => 
            {
                services.AddDotNetDocsCore();
                services.AddYamlRenderer();
                services.AddYamlRenderer(); // Second call should not duplicate
            });
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().HaveCount(1);
            renderers.OfType<YamlRenderer>().Should().HaveCount(1);
        }

        [TestMethod]
        public void MixedRendererRegistrations_NoDuplicates()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => 
            {
                services.AddDotNetDocs(); // Adds all 3 renderers
                services.AddMarkdownRenderer(); // Should not duplicate
                services.AddJsonRenderer(); // Should not duplicate
                services.AddYamlRenderer(); // Should not duplicate
                services.AddDotNetDocs(); // Should not duplicate any
            });
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().HaveCount(3, "should have exactly 3 renderers total");
            renderers.OfType<MarkdownRenderer>().Should().HaveCount(1);
            renderers.OfType<JsonRenderer>().Should().HaveCount(1);
            renderers.OfType<YamlRenderer>().Should().HaveCount(1);
        }

        [TestMethod]
        public void MultipleAddDocRenderer_SameType_DoesNotDuplicate()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => 
            {
                services.AddDotNetDocsCore();
                services.AddDocRenderer<TestRenderer>();
                services.AddDocRenderer<TestRenderer>(); // Should not duplicate
                services.AddDocRenderer<TestRenderer>(); // Should not duplicate
            });
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().HaveCount(1, "custom renderer should only be registered once");
            renderers.Should().AllBeOfType<TestRenderer>();
        }

        [TestMethod]
        public void MultipleAddDocEnricher_SameType_DoesNotDuplicate()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => 
            {
                services.AddDotNetDocsCore();
                services.AddDocEnricher<TestEnricher>();
                services.AddDocEnricher<TestEnricher>(); // Should not duplicate
            });
            TestSetup();

            // Assert
            var enrichers = TestHost.Services.GetServices<IDocEnricher>().ToList();
            enrichers.Should().HaveCount(1, "enricher should only be registered once");
            enrichers.Should().AllBeOfType<TestEnricher>();
        }

        [TestMethod]
        public void MultipleAddDocTransformer_SameType_DoesNotDuplicate()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => 
            {
                services.AddDotNetDocsCore();
                services.AddDocTransformer<TestTransformer>();
                services.AddDocTransformer<TestTransformer>(); // Should not duplicate
            });
            TestSetup();

            // Assert
            var transformers = TestHost.Services.GetServices<IDocTransformer>().ToList();
            transformers.Should().HaveCount(1, "transformer should only be registered once");
            transformers.Should().AllBeOfType<TestTransformer>();
        }

        [TestMethod]
        public void AddDotNetDocsCore_MultipleCalls_DoesNotDuplicateCore()
        {
            // Arrange & Act
            TestHostBuilder.ConfigureServices(services => 
            {
                services.AddDotNetDocsCore();
                services.AddDotNetDocsCore(); // Should not duplicate
                services.AddDotNetDocsCore(); // Should not duplicate
            });
            TestSetup();

            // Assert
            // Verify ProjectContext is still singleton
            var context1 = TestHost.Services.GetRequiredService<ProjectContext>();
            var context2 = TestHost.Services.GetRequiredService<ProjectContext>();
            context1.Should().BeSameAs(context2);

            // Verify DocumentationManager is registered only once (scoped)
            DocumentationManager manager1, manager2;
            using (var scope1 = TestHost.Services.CreateScope())
            {
                manager1 = scope1.ServiceProvider.GetRequiredService<DocumentationManager>();
            }
            using (var scope2 = TestHost.Services.CreateScope())
            {
                manager2 = scope2.ServiceProvider.GetRequiredService<DocumentationManager>();
            }
            manager1.Should().NotBeSameAs(manager2, "DocumentationManager should be scoped");
        }

        [TestMethod]
        public void ComplexRegistrationScenario_NoDuplicates()
        {
            // Arrange & Act - Complex scenario with many overlapping registrations
            TestHostBuilder.ConfigureServices(services => 
            {
                // First wave
                services.AddDotNetDocsCore();
                services.AddMarkdownRenderer();
                
                // Second wave - try to duplicate
                services.AddDotNetDocs(); // Should not duplicate existing Markdown
                
                // Third wave - individual renderers again
                services.AddMarkdownRenderer();
                services.AddJsonRenderer();
                services.AddYamlRenderer();
                
                // Fourth wave - custom types
                services.AddDocRenderer<TestRenderer>();
                services.AddDocEnricher<TestEnricher>();
                services.AddDocTransformer<TestTransformer>();
                
                // Fifth wave - try to duplicate custom types
                services.AddDocRenderer<TestRenderer>();
                services.AddDocEnricher<TestEnricher>();
                services.AddDocTransformer<TestTransformer>();
                
                // Final wave - try AddDotNetDocs again
                services.AddDotNetDocs();
            });
            TestSetup();

            // Assert
            var renderers = TestHost.Services.GetServices<IDocRenderer>().ToList();
            renderers.Should().HaveCount(4, "should have 3 built-in + 1 custom renderer");
            renderers.OfType<MarkdownRenderer>().Should().HaveCount(1);
            renderers.OfType<JsonRenderer>().Should().HaveCount(1);
            renderers.OfType<YamlRenderer>().Should().HaveCount(1);
            renderers.OfType<TestRenderer>().Should().HaveCount(1);

            var enrichers = TestHost.Services.GetServices<IDocEnricher>().ToList();
            enrichers.Should().HaveCount(1);
            enrichers.Should().AllBeOfType<TestEnricher>();

            var transformers = TestHost.Services.GetServices<IDocTransformer>().ToList();
            transformers.Should().HaveCount(2, "should have MarkdownXmlTransformer (auto-added with MarkdownRenderer) + TestTransformer");
            transformers.Should().Contain(t => t is MarkdownXmlTransformer);
            transformers.Should().Contain(t => t is TestTransformer);
        }

        #endregion

        #region DocumentationManager DI Integration Tests

        [TestMethod]
        public void DocumentationManager_ReceivesInjectedRenderers()
        {
            // Arrange
            TestHostBuilder.ConfigureServices(services => services.AddDotNetDocs());
            TestSetup();

            // Act
            using var scope = TestHost.Services.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<DocumentationManager>();
            var renderers = scope.ServiceProvider.GetServices<IDocRenderer>().ToList();

            // Assert
            manager.Should().NotBeNull();
            renderers.Should().HaveCount(3);
        }

        [TestMethod]
        public void DocumentationManager_WorksWithNoRenderers()
        {
            // Arrange
            TestHostBuilder.ConfigureServices(services => services.AddDotNetDocsCore()); // Core only, no renderers
            TestSetup();

            // Act
            using var scope = TestHost.Services.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<DocumentationManager>();

            // Assert
            manager.Should().NotBeNull();
        }

        #endregion

        #region Test Helpers

        private class TestRenderer : IDocRenderer
        {
            public string OutputFormat => "Test";
            public Task RenderAsync(DocAssembly model) => Task.CompletedTask;
        }

        private class TestEnricher : IDocEnricher
        {
            public Task EnrichAsync(DocEntity entity) => Task.CompletedTask;
        }

        private class TestTransformer : IDocTransformer
        {
            public Task TransformAsync(DocEntity entity) => Task.CompletedTask;
        }

        #endregion

    }

}