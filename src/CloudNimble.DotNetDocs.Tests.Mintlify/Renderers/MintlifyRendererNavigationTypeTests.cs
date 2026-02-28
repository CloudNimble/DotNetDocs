using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Mintlify;
using CloudNimble.DotNetDocs.Tests.Shared;
using CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace CloudNimble.DotNetDocs.Tests.Mintlify.Renderers
{

    /// <summary>
    /// Tests for the MintlifyRenderer.ApplyNavigationType functionality.
    /// These tests are isolated from the main MintlifyRendererTests to allow proper DI configuration
    /// of the Template property before services are built.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MintlifyRendererNavigationTypeTests : DotNetDocsTestBase
    {

        #region Fields

        private string _testOutputPath = null!;

        #endregion

        #region Helper Methods

        /// <summary>
        /// Configures test host with a specific template and navigation configuration.
        /// This must be called before TestSetup() to ensure the configuration is set before services are built.
        /// </summary>
        /// <param name="template">The DocsJsonConfig template to use, or null for no template.</param>
        /// <param name="navConfig">Optional navigation configuration to override defaults.</param>
        /// <param name="contentOnly">
        /// When <see langword="true"/>, sets <c>HasMintlifyTemplate = true</c> on the project context so that
        /// <see cref="DocumentationManager.ProcessAsync(System.Collections.Generic.IEnumerable{System.ValueTuple{string,string}})"/>
        /// with an empty assembly list still runs the renderer pipeline (content-only mode).
        /// </param>
        private void ConfigureTestWithTemplate(DocsJsonConfig? template, DocsNavigationConfig? navConfig = null, bool contentOnly = false)
        {
            _testOutputPath = Path.Combine(Path.GetTempPath(), $"MintlifyNavigationTypeTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testOutputPath);

            TestHostBuilder.ConfigureServices((context, services) =>
            {
                services.AddDotNetDocsPipeline(pipeline =>
                {
                    pipeline.UseMintlifyRenderer(options =>
                    {
                        options.Template = template;
                        if (navConfig is not null)
                        {
                            options.Navigation = navConfig;
                        }
                    })
                    .ConfigureContext(ctx =>
                    {
                        ctx.DocumentationRootPath = _testOutputPath;
                        ctx.ApiReferencePath = string.Empty;
                        if (contentOnly)
                        {
                            ctx.HasMintlifyTemplate = true;
                        }
                    });
                });
            });

            TestSetup();
        }

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

        #region ApplyNavigationType Tests

        [TestMethod]
        public async Task ApplyNavigationType_WithPagesDefault_DoesNotMoveNavigation()
        {
            // Arrange
            ConfigureTestWithTemplate(
                new DocsJsonConfig
                {
                    Name = "Test",
                    Theme = "mint",
                    Colors = new ColorsConfig { Primary = "#000000" }
                },
                new DocsNavigationConfig
                {
                    Type = NavigationType.Pages // Default
                });

            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            var docsJsonPath = Path.Combine(_testOutputPath, "docs.json");
            File.Exists(docsJsonPath).Should().BeTrue();

            var json = File.ReadAllText(docsJsonPath);
            var config = JsonSerializer.Deserialize<DocsJsonConfig>(json, MintlifyConstants.JsonSerializerOptions);

            config.Should().NotBeNull();
            config!.Navigation.Pages.Should().NotBeNullOrEmpty("Pages should contain the navigation");
            config.Navigation.Tabs.Should().BeNullOrEmpty("Tabs should be empty with Pages navigation type");
        }

        [TestMethod]
        public async Task ApplyNavigationType_WithTabs_MovesNavigationToTabs()
        {
            // Arrange
            ConfigureTestWithTemplate(
                new DocsJsonConfig
                {
                    Name = "Test API",
                    Theme = "mint",
                    Colors = new ColorsConfig { Primary = "#000000" }
                },
                new DocsNavigationConfig
                {
                    Type = NavigationType.Tabs
                });

            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            var docsJsonPath = Path.Combine(_testOutputPath, "docs.json");
            File.Exists(docsJsonPath).Should().BeTrue();

            var json = File.ReadAllText(docsJsonPath);
            var config = JsonSerializer.Deserialize<DocsJsonConfig>(json, MintlifyConstants.JsonSerializerOptions);

            config.Should().NotBeNull();
            config!.Navigation.Pages.Should().BeNull("Pages should be empty when using Tabs");
            config.Navigation.Tabs.Should().NotBeNullOrEmpty("Tabs should contain the navigation");
            config.Navigation.Tabs![0].Tab.Should().Be("Test API");
            config.Navigation.Tabs[0].Pages.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task ApplyNavigationType_WithTabsAndCustomName_UsesCustomName()
        {
            // Arrange
            ConfigureTestWithTemplate(
                new DocsJsonConfig
                {
                    Name = "Test API",
                    Theme = "mint",
                    Colors = new ColorsConfig { Primary = "#000000" }
                },
                new DocsNavigationConfig
                {
                    Type = NavigationType.Tabs,
                    Name = "Custom Tab Name"
                });

            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            var docsJsonPath = Path.Combine(_testOutputPath, "docs.json");
            var json = File.ReadAllText(docsJsonPath);
            var config = JsonSerializer.Deserialize<DocsJsonConfig>(json, MintlifyConstants.JsonSerializerOptions);

            config.Should().NotBeNull();
            config!.Navigation.Tabs.Should().NotBeNullOrEmpty();
            config.Navigation.Tabs![0].Tab.Should().Be("Custom Tab Name", "NavigationName should be used when provided");
        }

        [TestMethod]
        public async Task ApplyNavigationType_WithProducts_MovesNavigationToProducts()
        {
            // Arrange
            ConfigureTestWithTemplate(
                new DocsJsonConfig
                {
                    Name = "Test Product",
                    Theme = "mint",
                    Colors = new ColorsConfig { Primary = "#000000" }
                },
                new DocsNavigationConfig
                {
                    Type = NavigationType.Products
                });

            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            var docsJsonPath = Path.Combine(_testOutputPath, "docs.json");
            File.Exists(docsJsonPath).Should().BeTrue();

            var json = File.ReadAllText(docsJsonPath);
            var config = JsonSerializer.Deserialize<DocsJsonConfig>(json, MintlifyConstants.JsonSerializerOptions);

            config.Should().NotBeNull();
            config!.Navigation.Pages.Should().BeNull("Pages should be null when using Products");
            config.Navigation.Products.Should().NotBeNullOrEmpty("Products should contain the navigation");
            config.Navigation.Products![0].Product.Should().Be("Test Product");
            config.Navigation.Products[0].Pages.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task ApplyNavigationType_WithNullTemplate_DoesNotThrow()
        {
            // Arrange
            ConfigureTestWithTemplate(null); // No template

            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            // Act
            Func<Task> act = async () => await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            await act.Should().NotThrowAsync("ApplyNavigationType should handle null template gracefully");
        }

        [TestMethod]
        public async Task ApplyNavigationType_WithEmptyNavigationType_UsesDefaultBehavior()
        {
            // Arrange - Use default NavigationType.Pages
            ConfigureTestWithTemplate(new DocsJsonConfig
            {
                Name = "Test",
                Theme = "mint",
                Colors = new ColorsConfig { Primary = "#000000" }
            });

            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            var docsJsonPath = Path.Combine(_testOutputPath, "docs.json");
            var json = File.ReadAllText(docsJsonPath);
            var config = JsonSerializer.Deserialize<DocsJsonConfig>(json, MintlifyConstants.JsonSerializerOptions);

            config.Should().NotBeNull();
            config!.Navigation.Pages.Should().NotBeNullOrEmpty("Empty NavigationType should default to Pages behavior");
        }

        [TestMethod]
        public async Task ApplyNavigationType_WithCaseInsensitiveType_Works()
        {
            // Arrange - Enum is already case-insensitive by design
            ConfigureTestWithTemplate(
                new DocsJsonConfig
                {
                    Name = "Test",
                    Theme = "mint",
                    Colors = new ColorsConfig { Primary = "#000000" }
                },
                new DocsNavigationConfig
                {
                    Type = NavigationType.Tabs
                });

            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            var docsJsonPath = Path.Combine(_testOutputPath, "docs.json");
            var json = File.ReadAllText(docsJsonPath);
            var config = JsonSerializer.Deserialize<DocsJsonConfig>(json, MintlifyConstants.JsonSerializerOptions);

            config.Should().NotBeNull();
            config!.Navigation.Tabs.Should().NotBeNullOrEmpty("Lowercase 'tabs' should be recognized");
        }

        [TestMethod]
        public async Task ApplyNavigationType_WithInvalidType_UsesDefaultBehavior()
        {
            // Arrange - Enums prevent invalid values at compile time, so this test uses default
            ConfigureTestWithTemplate(new DocsJsonConfig
            {
                Name = "Test",
                Theme = "mint",
                Colors = new ColorsConfig { Primary = "#000000" }
            });

            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            var docsJsonPath = Path.Combine(_testOutputPath, "docs.json");
            var json = File.ReadAllText(docsJsonPath);
            var config = JsonSerializer.Deserialize<DocsJsonConfig>(json, MintlifyConstants.JsonSerializerOptions);

            config.Should().NotBeNull();
            config!.Navigation.Pages.Should().NotBeNullOrEmpty("Invalid NavigationType should default to Pages behavior");
        }

        #endregion

        #region Explicit Template Navigation Section Tests

        /// <summary>
        /// Regression test: when a template defines explicit Tabs in the Navigation config, the renderer
        /// must not auto-generate a parallel pages block from disk discovery (content-only mode).
        /// </summary>
        [TestMethod]
        public async Task ExplicitTemplateTabs_ContentOnly_HasTabsAndNoPages()
        {
            // Arrange
            ConfigureTestWithTemplate(
                new DocsJsonConfig
                {
                    Name = "Test Docs",
                    Theme = "mint",
                    Colors = new ColorsConfig { Primary = "#000000" },
                    Navigation = new NavigationConfig
                    {
                        Tabs =
                        [
                            new TabConfig { Tab = "Guides", Href = "/guides", Pages = ["guides/index"] },
                            new TabConfig { Tab = "API Reference", Href = "/api", Pages = ["api/index"] }
                        ]
                    }
                },
                contentOnly: true);

            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync([]);

            // Assert
            var docsJsonPath = Path.Combine(_testOutputPath, "docs.json");
            File.Exists(docsJsonPath).Should().BeTrue();

            var json = File.ReadAllText(docsJsonPath);
            var config = JsonSerializer.Deserialize<DocsJsonConfig>(json, MintlifyConstants.JsonSerializerOptions);

            config.Should().NotBeNull();
            config!.Navigation.Tabs.Should().HaveCount(2, "both template-defined tabs should be present");
            config.Navigation.Tabs![0].Tab.Should().Be("Guides");
            config.Navigation.Tabs![1].Tab.Should().Be("API Reference");
            config.Navigation.Pages.Should().BeNull("explicit tabs navigation must not produce a spurious pages block");
        }

        /// <summary>
        /// Regression test: when a template defines explicit Tabs, auto-discovery must not inject a pages block
        /// even when a real assembly is also being processed.
        /// </summary>
        [TestMethod]
        public async Task ExplicitTemplateTabs_WithAssembly_HasOnlyTabsAndNoPages()
        {
            // Arrange
            ConfigureTestWithTemplate(new DocsJsonConfig
            {
                Name = "Test API",
                Theme = "mint",
                Colors = new ColorsConfig { Primary = "#000000" },
                Navigation = new NavigationConfig
                {
                    Tabs =
                    [
                        new TabConfig { Tab = "Getting Started", Href = "/start", Pages = ["index"] },
                        new TabConfig { Tab = "API Reference", Href = "/api", Pages = ["api/index"] }
                    ]
                }
            });

            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync(assemblyPath, xmlPath);

            // Assert
            var docsJsonPath = Path.Combine(_testOutputPath, "docs.json");
            var json = File.ReadAllText(docsJsonPath);
            var config = JsonSerializer.Deserialize<DocsJsonConfig>(json, MintlifyConstants.JsonSerializerOptions);

            config.Should().NotBeNull();
            config!.Navigation.Tabs.Should().HaveCount(2, "template-defined tabs should be preserved");
            config.Navigation.Pages.Should().BeNull("auto-discovery must not inject pages alongside explicit tabs even when an assembly is present");
        }

        /// <summary>
        /// Verifies that a template with explicit Anchors produces anchors and no pages in content-only mode.
        /// </summary>
        [TestMethod]
        public async Task ExplicitTemplateAnchors_ContentOnly_HasAnchorsAndNoPages()
        {
            // Arrange
            ConfigureTestWithTemplate(
                new DocsJsonConfig
                {
                    Name = "Test Docs",
                    Theme = "mint",
                    Colors = new ColorsConfig { Primary = "#000000" },
                    Navigation = new NavigationConfig
                    {
                        Anchors =
                        [
                            new AnchorConfig { Anchor = "Documentation", Href = "/docs", Icon = "book", Pages = [] },
                            new AnchorConfig { Anchor = "API", Href = "/api", Icon = "code", Pages = [] }
                        ]
                    }
                },
                contentOnly: true);

            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync([]);

            // Assert
            var docsJsonPath = Path.Combine(_testOutputPath, "docs.json");
            var json = File.ReadAllText(docsJsonPath);
            var config = JsonSerializer.Deserialize<DocsJsonConfig>(json, MintlifyConstants.JsonSerializerOptions);

            config.Should().NotBeNull();
            config!.Navigation.Anchors.Should().HaveCount(2, "both template-defined anchors should be present");
            config.Navigation.Anchors![0].Anchor.Should().Be("Documentation");
            config.Navigation.Anchors![1].Anchor.Should().Be("API");
            config.Navigation.Pages.Should().BeNull("explicit anchors navigation must not produce a spurious pages block");
        }

        /// <summary>
        /// Verifies that a template with explicit Products produces products and no pages in content-only mode.
        /// </summary>
        [TestMethod]
        public async Task ExplicitTemplateProducts_ContentOnly_HasProductsAndNoPages()
        {
            // Arrange
            ConfigureTestWithTemplate(
                new DocsJsonConfig
                {
                    Name = "Test Platform",
                    Theme = "mint",
                    Colors = new ColorsConfig { Primary = "#000000" },
                    Navigation = new NavigationConfig
                    {
                        Products =
                        [
                            new ProductConfig { Product = "Core SDK", Href = "/core", Pages = ["core/index"] },
                            new ProductConfig { Product = "Extensions", Href = "/ext", Pages = ["ext/index"] }
                        ]
                    }
                },
                contentOnly: true);

            var documentationManager = GetService<DocumentationManager>();

            // Act
            await documentationManager.ProcessAsync([]);

            // Assert
            var docsJsonPath = Path.Combine(_testOutputPath, "docs.json");
            var json = File.ReadAllText(docsJsonPath);
            var config = JsonSerializer.Deserialize<DocsJsonConfig>(json, MintlifyConstants.JsonSerializerOptions);

            config.Should().NotBeNull();
            config!.Navigation.Products.Should().HaveCount(2, "both template-defined products should be present");
            config.Navigation.Products![0].Product.Should().Be("Core SDK");
            config.Navigation.Products![1].Product.Should().Be("Extensions");
            config.Navigation.Pages.Should().BeNull("explicit products navigation must not produce a spurious pages block");
        }

        #endregion

    }

}
