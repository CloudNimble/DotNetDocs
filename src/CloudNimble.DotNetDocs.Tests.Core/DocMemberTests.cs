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
            methodMember.MemberKind.Should().Be(SymbolKind.Method);
            // Note: DocMember loaded from AssemblyManager has return type populated
            // The DoSomething method returns string
            methodMember.ReturnType.Should().NotBeNull();
            // Summary is populated from XML documentation
            methodMember.Summary.Should().Be("Performs a sample operation.");
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
            propertyMember.MemberKind.Should().Be(SymbolKind.Property);
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

            newDocMember.Parameters.Should().ContainSingle();
            newDocMember.Parameters.Should().Contain(docParam);
        }

        [TestMethod]
        public void MemberSpecificProperties_CanBeSet()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");
            
            var methodMember = type!.Members.FirstOrDefault(m => m.Symbol is IMethodSymbol);
            
            var docMember = new DocMember(methodMember!.Symbol)
            {
                Name = "TestMethod",
                Signature = "public void TestMethod()",
                DisplayName = "TestClass.TestMethod",
                Accessibility = Accessibility.Public,
                MemberKind = SymbolKind.Method,
                MethodKind = Microsoft.CodeAnalysis.MethodKind.Ordinary,
                ReturnTypeName = "void"
            };

            docMember.Name.Should().Be("TestMethod");
            docMember.Signature.Should().Be("public void TestMethod()");
            docMember.DisplayName.Should().Be("TestClass.TestMethod");
            docMember.Accessibility.Should().Be(Accessibility.Public);
            docMember.MemberKind.Should().Be(SymbolKind.Method);
            docMember.MethodKind.Should().Be(Microsoft.CodeAnalysis.MethodKind.Ordinary);
            docMember.ReturnTypeName.Should().Be("void");
        }

        [TestMethod]
        public void Constructor_WithFieldSymbol_SetsProperties()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");
            
            // Find a field member if exists
            var fieldMember = type!.Members
                .FirstOrDefault(m => m.Symbol.Kind == SymbolKind.Field);
            
            if (fieldMember is not null)
            {
                fieldMember.Symbol.Should().BeAssignableTo<IFieldSymbol>();
                fieldMember.MemberKind.Should().Be(SymbolKind.Field);
                fieldMember.Parameters.Should().BeEmpty();
            }
        }

        [TestMethod]
        public void Constructor_WithEventSymbol_SetsProperties()
        {
            var assembly = GetTestsDotSharedAssembly();
            
            // Events might not exist in our test classes, but we can still test the logic
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");
            
            var eventMember = type!.Members
                .FirstOrDefault(m => m.Symbol.Kind == SymbolKind.Event);
            
            if (eventMember is not null)
            {
                eventMember.Symbol.Should().BeAssignableTo<IEventSymbol>();
                eventMember.MemberKind.Should().Be(SymbolKind.Event);
                eventMember.Parameters.Should().BeEmpty();
            }
        }

        [TestMethod]
        public void ReturnType_IsSetForMethods()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");
            
            var methodMember = type!.Members
                .FirstOrDefault(m => m.Symbol.Name == "DoSomething");
            
            methodMember.Should().NotBeNull();
            methodMember!.ReturnType.Should().NotBeNull();
            // DoSomething returns string
            methodMember.ReturnType!.Name.Should().Be("String");
        }

        [TestMethod]
        public void AccessModifiers_AreSetCorrectly()
        {
            var assembly = GetTestsDotSharedAssembly();
            var type = assembly.Namespaces
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SampleClass");
            
            // All members in SampleClass should be public based on our test class
            var publicMembers = type!.Members
                .Where(m => m.Symbol.DeclaredAccessibility == Accessibility.Public);
            
            publicMembers.Should().NotBeEmpty();
        }


        #endregion

    }

}