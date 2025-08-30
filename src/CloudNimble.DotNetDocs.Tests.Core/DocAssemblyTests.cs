using System;
using System.Linq;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    [TestClass]
    public class DocAssemblyTests : DotNetDocsTestBase
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
        public void Constructor_WithValidSymbol_SetsProperties()
        {
            var assembly = GetTestsDotSharedAssembly();
            
            // The assembly is already a DocAssembly from AssemblyManager
            // Create a new one to test the constructor
            var docAssembly = new DocAssembly(assembly.Symbol);

            docAssembly.Symbol.Should().Be(assembly.Symbol);
            docAssembly.Namespaces.Should().BeEmpty();
            docAssembly.Usage.Should().BeEmpty();
            docAssembly.Examples.Should().BeEmpty();
            docAssembly.BestPractices.Should().BeEmpty();
            docAssembly.RelatedApis.Should().BeEmpty();
        }

        [TestMethod]
        public void Namespaces_CanBeAdded()
        {
            var assembly = GetTestsDotSharedAssembly();
            
            // Create a new DocAssembly to test adding namespaces
            var docAssembly = new DocAssembly(assembly.Symbol);

            var namespaceDoc = assembly.Namespaces.FirstOrDefault(n => !n.Symbol.IsGlobalNamespace);
            namespaceDoc.Should().NotBeNull("Test assembly should contain at least one non-global namespace");
            
            var docNamespace = new DocNamespace(namespaceDoc!.Symbol);

            docAssembly.Namespaces.Add(docNamespace);

            docAssembly.Namespaces.Should().HaveCount(1);
            docAssembly.Namespaces.Should().Contain(docNamespace);
        }

        #endregion

    }

}