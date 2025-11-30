using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    [TestClass]
    public class DocEntityTests : DotNetDocsTestBase
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

            entity.Usage.Should().BeNull();
            entity.Examples.Should().BeNull();
            entity.BestPractices.Should().BeNull();
            entity.Patterns.Should().BeNull();
            entity.Considerations.Should().BeNull();
            entity.RelatedApis.Should().BeNull();
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

            entity.RelatedApis ??= [];
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

            entity.RelatedApis ??= [];
            entity.RelatedApis.Add("https://docs.microsoft.com/api1");
            entity.RelatedApis.Add("System.Collections.Generic.List");
            entity.RelatedApis.Add("MyNamespace.MyClass.MyMethod");

            entity.RelatedApis.Should().HaveCount(3);
            var apiList = entity.RelatedApis.ToList();
            apiList[0].Should().Be("https://docs.microsoft.com/api1");
            apiList[1].Should().Be("System.Collections.Generic.List");
            apiList[2].Should().Be("MyNamespace.MyClass.MyMethod");
        }

        [TestMethod]
        public void DocEntity_IncludedMembers_DefaultsToPublic()
        {
            var entity = new TestDocEntity();

            entity.IncludedMembers.Should().NotBeNull();
            entity.IncludedMembers.Should().ContainSingle();
            entity.IncludedMembers.Should().Contain(Accessibility.Public);
        }

        [TestMethod]
        public void DocEntity_IncludedMembers_CanBeModified()
        {
            var entity = new TestDocEntity();

            entity.IncludedMembers = [Accessibility.Public, Accessibility.Internal, Accessibility.Protected];

            entity.IncludedMembers.Should().HaveCount(3);
            entity.IncludedMembers.Should().Contain(Accessibility.Public);
            entity.IncludedMembers.Should().Contain(Accessibility.Internal);
            entity.IncludedMembers.Should().Contain(Accessibility.Protected);
        }

        [TestMethod]
        public void DocEntity_AllXmlDocProperties_CanBeSet()
        {
            var entity = new TestDocEntity
            {
                Summary = "Brief description",
                Remarks = "Additional remarks",
                Returns = "Return value description",
                Value = "Property value description",
                DisplayName = "Full.Display.Name"
            };

            entity.Exceptions =
            [
                new() { Type = "ArgumentException", Description = "Invalid argument" }
            ];

            entity.TypeParameters =
            [
                new() { Name = "T", Description = "Type parameter" }
            ];

            entity.SeeAlso =
            [
                new DocReference("T:RelatedType"),
                new DocReference("T:AnotherType")
            ];

            entity.Summary.Should().Be("Brief description");
            entity.Remarks.Should().Be("Additional remarks");
            entity.Returns.Should().Be("Return value description");
            entity.Value.Should().Be("Property value description");
            entity.DisplayName.Should().Be("Full.Display.Name");
            entity.Exceptions.Should().ContainSingle();
            entity.TypeParameters.Should().ContainSingle();
            entity.SeeAlso.Should().HaveCount(2);
        }


        [TestMethod]
        public void DocEntity_OriginalSymbol_CanBeSetInConstructor()
        {
            // Get a real symbol from test assembly
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");

            type.Should().NotBeNull();

            // Create new entity with symbol
            var entity = new TestDocEntityWithSymbolConstructor(type!.Symbol);

            entity.OriginalSymbol.Should().NotBeNull();
            entity.OriginalSymbol.Should().Be(type.Symbol);
        }

        #endregion

        #region Private Classes

        private class TestDocEntityWithSymbolConstructor : DocEntity
        {
            public TestDocEntityWithSymbolConstructor(ISymbol symbol) : base(symbol)
            {
            }
        }

        #endregion

    }

}