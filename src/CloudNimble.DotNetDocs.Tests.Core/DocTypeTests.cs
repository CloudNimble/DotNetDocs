using System;
using System.Collections.Generic;
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
            // Summary is populated from XML doc comment
            type.Summary.Should().NotBeNullOrEmpty();
            type.Summary.Should().Be("A sample class for testing documentation generation.");
            type.Examples.Should().BeNull();
            type.BestPractices.Should().BeNull();
            type.Patterns.Should().BeNull();
            type.Considerations.Should().BeNull();
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

            docType.RelatedApis ??= [];
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

        [TestMethod]
        public void TypeSpecificProperties_CanBeSet()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");
            
            var docType = new DocType(type!.Symbol)
            {
                Name = "TestType",
                FullName = "Namespace.TestType",
                AssemblyName = "TestAssembly",
                Signature = "public class TestType",
                TypeKind = Microsoft.CodeAnalysis.TypeKind.Class
            };

            docType.Name.Should().Be("TestType");
            docType.FullName.Should().Be("Namespace.TestType");
            docType.AssemblyName.Should().Be("TestAssembly");
            docType.Signature.Should().Be("public class TestType");
            docType.TypeKind.Should().Be(Microsoft.CodeAnalysis.TypeKind.Class);
        }

        [TestMethod]
        public void BaseType_IsExtractedCorrectly()
        {
            var assembly = GetTestsDotSharedAssembly();
            
            // SampleClass should have a base type
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");
            
            type.Should().NotBeNull();
            type!.BaseType.Should().NotBeNull();
        }

        [TestMethod]
        public void Members_CollectionCanBePopulated()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");

            // The type loaded from AssemblyManager should have members
            type!.Members.Should().NotBeEmpty();
            type.Members.Should().Contain(m => m.Name == "DoSomething");
            type.Members.Should().Contain(m => m.Name == "Name" || m.Name == "get_Name" || m.Name == "set_Name");
        }

        [TestMethod]
        public void Different_TypeKinds_AreHandledCorrectly()
        {
            var assembly = GetTestsDotSharedAssembly();
            
            // Find different type kinds in the test assembly
            var types = assembly.Namespaces.SelectMany(n => n.Types).ToList();
            
            // We should have at least classes
            var classTypes = types.Where(t => t.TypeKind == Microsoft.CodeAnalysis.TypeKind.Class);
            classTypes.Should().NotBeEmpty("Test assembly should contain classes");
        }


        [TestMethod]
        public void Symbol_IsPreservedCorrectly()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");
            
            var originalSymbol = type!.Symbol;
            var docType = new DocType(originalSymbol);

            docType.Symbol.Should().NotBeNull();
            docType.Symbol.Should().Be(originalSymbol);
            docType.Symbol.Name.Should().Be("SampleClass");
        }

        #endregion

    }

}