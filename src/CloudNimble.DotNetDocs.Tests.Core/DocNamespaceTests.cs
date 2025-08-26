using System;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    [TestClass]
    public class DocNamespaceTests : TestBase
    {

        #region Public Methods

        [TestMethod]
        public void Constructor_WithNullSymbol_ThrowsArgumentNullException()
        {
            Action act = () => new DocNamespace(null!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("symbol");
        }

        [TestMethod]
        public async Task Constructor_WithValidSymbol_SetsProperties()
        {
            var compilation = await CreateCompilationAsync();
            var namespaceSymbol = compilation.GlobalNamespace;
            namespaceSymbol.Should().NotBeNull();

            var docNamespace = new DocNamespace(namespaceSymbol);

            docNamespace.Symbol.Should().Be(namespaceSymbol);
            docNamespace.Types.Should().BeEmpty();
            docNamespace.Usage.Should().BeEmpty();
            docNamespace.Examples.Should().BeEmpty();
            docNamespace.RelatedApis.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Types_CanBeAdded()
        {
            var compilation = await CreateCompilationAsync();
            var namespaceSymbol = compilation.GlobalNamespace;
            var docNamespace = new DocNamespace(namespaceSymbol);

            var typeSymbol = compilation.GetTypeByMetadataName("CloudNimble.DotNetDocs.Tests.Shared.SampleClass");
            var docType = new DocType(typeSymbol!);

            docNamespace.Types.Add(docType);

            docNamespace.Types.Should().HaveCount(1);
            docNamespace.Types.Should().Contain(docType);
        }

        #endregion

    }

}