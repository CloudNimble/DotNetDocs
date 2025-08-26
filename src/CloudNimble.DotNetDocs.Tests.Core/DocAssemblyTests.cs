using System;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    [TestClass]
    public class DocAssemblyTests : TestBase
    {

        #region Public Methods

        [TestMethod]
        public void Constructor_WithNullSymbol_ThrowsArgumentNullException()
        {
            Action act = () => new DocAssembly(null!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("symbol");
        }

        [TestMethod]
        public async Task Constructor_WithValidSymbol_SetsProperties()
        {
            var compilation = await CreateCompilationAsync();
            var assemblySymbol = compilation.Assembly;
            assemblySymbol.Should().NotBeNull();

            var docAssembly = new DocAssembly(assemblySymbol);

            docAssembly.Symbol.Should().Be(assemblySymbol);
            docAssembly.Namespaces.Should().BeEmpty();
            docAssembly.Usage.Should().BeEmpty();
            docAssembly.Examples.Should().BeEmpty();
            docAssembly.BestPractices.Should().BeEmpty();
            docAssembly.RelatedApis.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Namespaces_CanBeAdded()
        {
            var compilation = await CreateCompilationAsync();
            var assemblySymbol = compilation.Assembly;
            var docAssembly = new DocAssembly(assemblySymbol);

            var namespaceSymbol = compilation.GlobalNamespace;
            var docNamespace = new DocNamespace(namespaceSymbol);

            docAssembly.Namespaces.Add(docNamespace);

            docAssembly.Namespaces.Should().HaveCount(1);
            docAssembly.Namespaces.Should().Contain(docNamespace);
        }

        #endregion

    }

}