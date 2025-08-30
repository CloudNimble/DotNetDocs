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
    public class DocMemberTests : DotNetDocsTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Constructor_WithNullSymbol_ThrowsArgumentNullException()
        {
            Action act = () => new DocMember(null!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("symbol");
        }

        [TestMethod]
        public void Constructor_WithMethodSymbol_SetsProperties()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");
            
            type.Should().NotBeNull("SampleClass should exist in test assembly");
            
            var methodMember = type!.Members
                .FirstOrDefault(m => m.Symbol.Name == "DoSomething");
            
            methodMember.Should().NotBeNull("DoSomething method should exist in SampleClass");

            // The member is already a DocMember, not an IMethodSymbol
            methodMember!.Symbol.Should().BeAssignableTo<IMethodSymbol>();
            methodMember.Kind.Should().Be(SymbolKind.Method);
            // Note: DocMember loaded from AssemblyManager has return type populated
            // The DoSomething method returns string
            methodMember.ReturnType.Should().NotBeNull();
            methodMember.Usage.Should().BeEmpty();
        }

        [TestMethod]
        public void Constructor_WithPropertySymbol_SetsProperties()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");
            
            type.Should().NotBeNull("SampleClass should exist in test assembly");
            
            var propertyMember = type!.Members
                .FirstOrDefault(m => m.Symbol.Name == "Name");
            
            propertyMember.Should().NotBeNull("Name property should exist in SampleClass");

            // The member is already a DocMember, not an IPropertySymbol
            propertyMember!.Symbol.Should().BeAssignableTo<IPropertySymbol>();
            propertyMember.Kind.Should().Be(SymbolKind.Property);
            propertyMember.Parameters.Should().BeEmpty();
        }

        [TestMethod]
        public void Parameters_CanBeAdded()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");
            
            type.Should().NotBeNull("SampleClass should exist in test assembly");
            
            var methodMember = type!.Members
                .FirstOrDefault(m => m.Symbol.Name == "DoSomething");
            
            methodMember.Should().NotBeNull("DoSomething method should exist in SampleClass");

            // The member loaded from AssemblyManager should already have parameters
            var methodSymbol = methodMember!.Symbol as IMethodSymbol;
            methodSymbol.Should().NotBeNull("Symbol should be an IMethodSymbol");
            
            methodSymbol!.Parameters.Should().NotBeEmpty("DoSomething method should have parameters");
            
            // Create a new DocMember to test adding parameters
            var newDocMember = new DocMember(methodSymbol);
            var paramSymbol = methodSymbol.Parameters.First();
            var docParam = new DocParameter(paramSymbol);

            newDocMember.Parameters.Add(docParam);

            newDocMember.Parameters.Should().HaveCount(1);
            newDocMember.Parameters.Should().Contain(docParam);
        }

        #endregion

    }

}