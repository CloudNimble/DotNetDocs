using System;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    [TestClass]
    public class DocTypeTests : TestBase
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
        public async Task Constructor_WithValidSymbol_SetsProperties()
        {
            var compilation = await CreateCompilationAsync();
            var symbol = compilation.GetTypeByMetadataName("CloudNimble.DotNetDocs.Tests.Shared.SampleClass");
            symbol.Should().NotBeNull();

            var docType = new DocType(symbol!);

            docType.Symbol.Should().Be(symbol);
            docType.Usage.Should().BeEmpty();
            docType.Examples.Should().BeEmpty();
            docType.BestPractices.Should().BeEmpty();
            docType.Patterns.Should().BeEmpty();
            docType.Considerations.Should().BeEmpty();
            docType.RelatedApis.Should().BeEmpty();
            docType.Members.Should().BeEmpty();
            docType.BaseType.Should().BeNull();
        }

        [TestMethod]
        public async Task Properties_CanBeSetAndRetrieved()
        {
            var compilation = await CreateCompilationAsync();
            var symbol = compilation.GetTypeByMetadataName("CloudNimble.DotNetDocs.Tests.Shared.SampleClass");

            var docType = new DocType(symbol!)
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