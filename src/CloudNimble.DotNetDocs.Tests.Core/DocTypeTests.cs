using System;
using System.Linq;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    [TestClass]
    public class DocTypeTests : DotNetDocsTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Constructor_WithNullSymbol_ThrowsArgumentNullException()
        {
            Action act = () => new DocType(null!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("symbol");
        }

        [TestMethod]
        public void Constructor_WithValidSymbol_SetsProperties()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");
            
            type.Should().NotBeNull("SampleClass should exist in test assembly");

            // The type is already a DocType from AssemblyManager
            type!.Symbol.Name.Should().Be("SampleClass");
            // Note: Usage is populated from XML documentation
            type.Usage.Should().NotBeEmpty(); // Has XML doc comment
            type.Examples.Should().BeEmpty();
            type.BestPractices.Should().BeEmpty();
            type.Patterns.Should().BeEmpty();
            type.Considerations.Should().BeEmpty();
            type.RelatedApis.Should().BeEmpty();
            // Note: Members may be populated from AssemblyManager
            type.BaseType.Should().NotBeNull(); // SampleClass inherits from DotNetDocsTestBase
        }

        [TestMethod]
        public void Properties_CanBeSetAndRetrieved()
        {
            var assembly = GetTestsDotSharedAssembly();
            var existingType = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");
            
            existingType.Should().NotBeNull("SampleClass should exist in test assembly");

            // Create a new DocType to test property setting
            var docType = new DocType(existingType!.Symbol)
            {
                Usage = "This is how you use it",
                Examples = "Example code here",
                BestPractices = "Best practices content",
                Patterns = "Pattern documentation",
                Considerations = "Important considerations"
            };

            docType.RelatedApis.Add("System.String");
            docType.RelatedApis.Add("System.Object");

            docType.Usage.Should().Be("This is how you use it");
            docType.Examples.Should().Be("Example code here");
            docType.BestPractices.Should().Be("Best practices content");
            docType.Patterns.Should().Be("Pattern documentation");
            docType.Considerations.Should().Be("Important considerations");
            docType.RelatedApis.Should().HaveCount(2);
            docType.RelatedApis.Should().Contain("System.String");
        }

        #endregion

    }

}