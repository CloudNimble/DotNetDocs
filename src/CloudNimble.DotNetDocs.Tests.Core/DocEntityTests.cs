using System;
using CloudNimble.Breakdance.Assemblies;
using CloudNimble.DotNetDocs.Core;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    [TestClass]
    public class DocEntityTests : BreakdanceTestBase
    {

        #region Private Classes

        private class TestDocEntity : DocEntity
        {
            public ISymbol? Symbol { get; set; }
        }

        #endregion

        #region Public Methods

        [TestMethod]
        public void DocEntity_DefaultProperties_AreEmptyStrings()
        {
            var entity = new TestDocEntity();

            entity.Usage.Should().BeEmpty();
            entity.Examples.Should().BeEmpty();
            entity.BestPractices.Should().BeEmpty();
            entity.Patterns.Should().BeEmpty();
            entity.Considerations.Should().BeEmpty();
            entity.RelatedApis.Should().NotBeNull();
            entity.RelatedApis.Should().BeEmpty();
        }

        [TestMethod]
        public void DocEntity_Properties_CanBeSetAndRetrieved()
        {
            var entity = new TestDocEntity
            {
                Usage = "Usage documentation",
                Examples = "Example code",
                BestPractices = "Best practice guidelines",
                Patterns = "Pattern documentation",
                Considerations = "Important notes"
            };

            entity.RelatedApis.Add("System.String");
            entity.RelatedApis.Add("System.Object");

            entity.Usage.Should().Be("Usage documentation");
            entity.Examples.Should().Be("Example code");
            entity.BestPractices.Should().Be("Best practice guidelines");
            entity.Patterns.Should().Be("Pattern documentation");
            entity.Considerations.Should().Be("Important notes");
            entity.RelatedApis.Should().HaveCount(2);
            entity.RelatedApis.Should().ContainInOrder("System.String", "System.Object");
        }

        [TestMethod]
        public void DocEntity_RelatedApis_SupportsMultipleEntries()
        {
            var entity = new TestDocEntity();

            entity.RelatedApis.Add("https://docs.microsoft.com/api1");
            entity.RelatedApis.Add("System.Collections.Generic.List");
            entity.RelatedApis.Add("MyNamespace.MyClass.MyMethod");

            entity.RelatedApis.Should().HaveCount(3);
            entity.RelatedApis[0].Should().Be("https://docs.microsoft.com/api1");
            entity.RelatedApis[1].Should().Be("System.Collections.Generic.List");
            entity.RelatedApis[2].Should().Be("MyNamespace.MyClass.MyMethod");
        }

        #endregion

    }

}