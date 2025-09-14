using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Core.Transformers;
using CloudNimble.DotNetDocs.Tests.Shared;
using CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Tests for the CrossReferenceResolver class.
    /// </summary>
    [TestClass]
    public class CrossReferenceResolverTests : DotNetDocsTestBase
    {

        #region Fields

        private CrossReferenceResolver _resolver = null!;
        private ProjectContext _context = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            // Configure services for DI
            TestHostBuilder.ConfigureServices((context, services) =>
            {
                services.AddDotNetDocs(ctx =>
                {
                    ctx.FileNamingOptions.NamespaceMode = NamespaceMode.Folder;
                    ctx.FileNamingOptions.NamespaceSeparator = '-';
                });
            });

            TestSetup();
            _context = GetService<ProjectContext>();
            _resolver = new CrossReferenceResolver(_context);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestTearDown();
        }

        #endregion

        #region BuildReferenceMap Tests

        [TestMethod]
        public async Task BuildReferenceMap_IndexesAllEntities()
        {
            // Arrange
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            // Act
            _resolver.BuildReferenceMap(assembly);

            // Assert
            // Test that we can resolve a type reference that we know exists
            var typeRef = _resolver.ResolveReference("T:CloudNimble.DotNetDocs.Core.Configuration.NamespaceMode", "");
            typeRef.Should().NotBeNull();
            typeRef.IsResolved.Should().BeTrue();
            typeRef.DisplayName.Should().Be("NamespaceMode");
        }

        #endregion

        #region ResolveReference Tests

        [TestMethod]
        public async Task ResolveReference_WithEnumFieldPrefix_ResolvesCorrectly()
        {
            // Arrange
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();
            _resolver.BuildReferenceMap(assembly);

            // Act
            var reference = _resolver.ResolveReference("F:CloudNimble.DotNetDocs.Core.Configuration.NamespaceMode.File", "");

            // Assert
            reference.Should().NotBeNull();
            reference.IsResolved.Should().BeTrue();
            reference.DisplayName.Should().Be("NamespaceMode.File");
            reference.Anchor.Should().Be("file");
            reference.ReferenceType.Should().Be(ReferenceType.Field);
        }

        [TestMethod]
        public async Task ResolveReference_WithTypePrefix_ResolvesCorrectly()
        {
            // Arrange
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();
            _resolver.BuildReferenceMap(assembly);

            // Act
            var reference = _resolver.ResolveReference("T:CloudNimble.DotNetDocs.Core.Configuration.NamespaceMode", "");

            // Assert
            reference.Should().NotBeNull();
            reference.IsResolved.Should().BeTrue();
            reference.DisplayName.Should().Be("NamespaceMode");
            reference.Anchor.Should().BeNull();
            reference.ReferenceType.Should().Be(ReferenceType.Type);
        }

        [TestMethod]
        public void ResolveReference_WithExternalUrl_ResolvesAsExternal()
        {
            // Act
            var reference = _resolver.ResolveReference("https://learn.microsoft.com/dotnet", "");

            // Assert
            reference.Should().NotBeNull();
            reference.IsResolved.Should().BeTrue();
            reference.ReferenceType.Should().Be(ReferenceType.External);
            reference.RelativePath.Should().Be("https://learn.microsoft.com/dotnet");
            reference.DisplayName.Should().Be("link");
        }

        [TestMethod]
        public void ResolveReference_WithFrameworkType_ResolvesAsFramework()
        {
            // Act
            var reference = _resolver.ResolveReference("T:System.String", "");

            // Assert
            reference.Should().NotBeNull();
            reference.IsResolved.Should().BeTrue();
            reference.ReferenceType.Should().Be(ReferenceType.Framework);
            reference.DisplayName.Should().Be("String");
            reference.RelativePath.Should().StartWith("https://learn.microsoft.com/dotnet/api/");
        }

        [TestMethod]
        public void ResolveReference_WithUnknownType_ReturnsUnresolved()
        {
            // Act
            var reference = _resolver.ResolveReference("T:Some.Unknown.Type", "");

            // Assert
            reference.Should().NotBeNull();
            reference.IsResolved.Should().BeFalse();
            reference.DisplayName.Should().Be("Type");
        }

        #endregion

        #region ResolveReferences Tests

        [TestMethod]
        public async Task ResolveReferences_ResolvesMultipleReferences()
        {
            // Arrange
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();
            _resolver.BuildReferenceMap(assembly);

            var rawReferences = new List<string>
            {
                "T:CloudNimble.DotNetDocs.Core.Configuration.NamespaceMode",
                "F:CloudNimble.DotNetDocs.Core.Configuration.NamespaceMode.File",
                "T:System.String",
                "https://example.com"
            };

            // Act
            var resolved = _resolver.ResolveReferences(rawReferences, "");

            // Assert
            resolved.Should().NotBeNull();
            resolved.Should().HaveCount(4);

            resolved.ElementAt(0).IsResolved.Should().BeTrue();
            resolved.ElementAt(0).ReferenceType.Should().Be(ReferenceType.Type);

            resolved.ElementAt(1).IsResolved.Should().BeTrue();
            resolved.ElementAt(1).ReferenceType.Should().Be(ReferenceType.Field);

            resolved.ElementAt(2).IsResolved.Should().BeTrue();
            resolved.ElementAt(2).ReferenceType.Should().Be(ReferenceType.Framework);

            resolved.ElementAt(3).IsResolved.Should().BeTrue();
            resolved.ElementAt(3).ReferenceType.Should().Be(ReferenceType.External);
        }

        #endregion

        #region Helper Method Tests

        [TestMethod]
        public void StripPrefix_RemovesTypePrefix()
        {
            // Act
            var result = _resolver.StripPrefix("T:System.String");

            // Assert
            result.Should().Be("System.String");
        }

        [TestMethod]
        public void StripPrefix_HandlesNoPrefix()
        {
            // Act
            var result = _resolver.StripPrefix("System.String");

            // Assert
            result.Should().Be("System.String");
        }

        [TestMethod]
        public void GetSimpleTypeName_ExtractsSimpleName()
        {
            // Act
            var result = _resolver.GetSimpleTypeName("CloudNimble.DotNetDocs.Core.Configuration.NamespaceMode");

            // Assert
            result.Should().Be("NamespaceMode");
        }

        [TestMethod]
        public void IsFrameworkType_IdentifiesFrameworkTypes()
        {
            // Act & Assert
            _resolver.IsFrameworkType("System.String").Should().BeTrue();
            _resolver.IsFrameworkType("Microsoft.Extensions.DependencyInjection.IServiceCollection").Should().BeTrue();
            _resolver.IsFrameworkType("Windows.UI.Xaml.Controls.Button").Should().BeTrue();
            _resolver.IsFrameworkType("CloudNimble.DotNetDocs.Core.DocEntity").Should().BeFalse();
        }

        [TestMethod]
        public void GetFrameworkDocumentationUrl_GeneratesCorrectUrl()
        {
            // Act
            var url = _resolver.GetFrameworkDocumentationUrl("System.Collections.Generic.List`1");

            // Assert
            url.Should().Be("https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1");
        }

        #endregion

    }

}