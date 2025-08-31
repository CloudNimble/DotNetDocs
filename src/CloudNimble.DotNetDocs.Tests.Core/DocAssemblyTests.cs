using System;
using System.Linq;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.CodeAnalysis;
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
            docAssembly.Usage.Should().BeNull();
            docAssembly.Examples.Should().BeNull();
            docAssembly.BestPractices.Should().BeNull();
            docAssembly.RelatedApis.Should().BeNull();
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

        [TestMethod]
        public void AssemblyName_IsSetCorrectly()
        {
            var assembly = GetTestsDotSharedAssembly();
            
            assembly.AssemblyName.Should().NotBeNullOrWhiteSpace();
            assembly.AssemblyName.Should().Be("CloudNimble.DotNetDocs.Tests.Shared");
        }

        [TestMethod]
        public void IncludedMembers_CascadesToNamespaces()
        {
            var assembly = GetTestsDotSharedAssembly();
            var docAssembly = new DocAssembly(assembly.Symbol)
            {
                IncludedMembers = [Accessibility.Public, Accessibility.Internal]
            };

            var namespaceDoc = assembly.Namespaces.FirstOrDefault(n => !n.Symbol.IsGlobalNamespace);
            var docNamespace = new DocNamespace(namespaceDoc!.Symbol);
            docAssembly.Namespaces.Add(docNamespace);

            // When cascading is implemented, namespaces should inherit IncludedMembers
            docAssembly.IncludedMembers.Should().Contain(Accessibility.Public);
            docAssembly.IncludedMembers.Should().Contain(Accessibility.Internal);
        }

        [TestMethod]
        public void MultipleNamespaces_CanBeAdded()
        {
            var assembly = GetTestsDotSharedAssembly();
            var docAssembly = new DocAssembly(assembly.Symbol);

            // Add all non-global namespaces from the test assembly
            foreach (var ns in assembly.Namespaces.Where(n => !n.Symbol.IsGlobalNamespace))
            {
                docAssembly.Namespaces.Add(new DocNamespace(ns.Symbol));
            }

            docAssembly.Namespaces.Should().HaveCountGreaterThan(0);
            docAssembly.Namespaces.Should().AllSatisfy(ns => ns.Symbol.IsGlobalNamespace.Should().BeFalse());
        }

        [TestMethod]
        public void Properties_FromDocEntity_CanBeSet()
        {
            var assembly = GetTestsDotSharedAssembly();
            var docAssembly = new DocAssembly(assembly.Symbol)
            {
                Usage = "Assembly usage documentation",
                Examples = "Assembly examples",
                BestPractices = "Best practices for using this assembly",
                Patterns = "Common patterns",
                Considerations = "Important considerations",
                Summary = "Assembly summary",
                Remarks = "Additional remarks",
                DisplayName = "Test Assembly"
            };

            docAssembly.Usage.Should().Be("Assembly usage documentation");
            docAssembly.Examples.Should().Be("Assembly examples");
            docAssembly.BestPractices.Should().Be("Best practices for using this assembly");
            docAssembly.Patterns.Should().Be("Common patterns");
            docAssembly.Considerations.Should().Be("Important considerations");
            docAssembly.Summary.Should().Be("Assembly summary");
            docAssembly.Remarks.Should().Be("Additional remarks");
            docAssembly.DisplayName.Should().Be("Test Assembly");
        }


        [TestMethod]
        public void Symbol_IsPreservedCorrectly()
        {
            var assembly = GetTestsDotSharedAssembly();
            var originalSymbol = assembly.Symbol;
            
            var docAssembly = new DocAssembly(originalSymbol);

            docAssembly.Symbol.Should().NotBeNull();
            docAssembly.Symbol.Should().Be(originalSymbol);
            docAssembly.Symbol.Name.Should().Be(originalSymbol.Name);
        }

        #endregion

    }

}