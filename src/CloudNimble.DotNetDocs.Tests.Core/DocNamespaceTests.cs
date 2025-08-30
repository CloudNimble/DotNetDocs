using System;
using System.Linq;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    [TestClass]
    public class DocNamespaceTests : DotNetDocsTestBase
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
        public void Constructor_WithValidSymbol_SetsProperties()
        {
            var assembly = GetTestsDotSharedAssembly();
            var namespaceDoc = assembly.Namespaces.FirstOrDefault(n => !n.Symbol.IsGlobalNamespace);
            
            namespaceDoc.Should().NotBeNull("Test assembly should contain at least one non-global namespace");

            // The namespace is already a DocNamespace from AssemblyManager
            // Create a new one to test the constructor
            var docNamespace = new DocNamespace(namespaceDoc!.Symbol);

            docNamespace.Symbol.Should().Be(namespaceDoc.Symbol);
            docNamespace.Types.Should().BeEmpty();
            docNamespace.Usage.Should().BeEmpty();
            docNamespace.Examples.Should().BeEmpty();
            docNamespace.RelatedApis.Should().BeEmpty();
        }

        [TestMethod]
        public void Types_CanBeAdded()
        {
            var assembly = GetTestsDotSharedAssembly();
            var namespaceDoc = assembly.Namespaces.FirstOrDefault(n => !n.Symbol.IsGlobalNamespace);
            
            namespaceDoc.Should().NotBeNull("Test assembly should contain at least one non-global namespace");
            
            // Create a new DocNamespace to test adding types
            var docNamespace = new DocNamespace(namespaceDoc!.Symbol);

            var existingType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");
            
            existingType.Should().NotBeNull("SampleClass should exist in test assembly");
            
            var docType = new DocType(existingType!.Symbol);

            docNamespace.Types.Add(docType);

            docNamespace.Types.Should().ContainSingle();
            docNamespace.Types.Should().Contain(docType);
        }

        #endregion

    }

}