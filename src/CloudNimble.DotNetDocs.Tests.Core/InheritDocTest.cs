using System.Linq;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    [Ignore("We're not using these right now. Revisit later.")]
    [TestClass]
    public class InheritDocTest : DotNetDocsTestBase
    {

        #region Public Methods

        [TestMethod]
        public void InheritDoc_Should_Be_Resolved_By_Roslyn()
        {
            // Arrange
            var source = @"
namespace TestNamespace
{
    /// <summary>Base class summary</summary>
    /// <remarks>Base class remarks</remarks>
    public class BaseClass
    {
        /// <summary>Base method summary</summary>
        /// <returns>Base method returns</returns>
        public virtual string Method() => ""base"";
    }

    /// <inheritdoc/>
    public class DerivedClass : BaseClass
    {
        /// <inheritdoc/>
        public override string Method() => ""derived"";
    }

    public class AnotherClass
    {
        /// <summary>Original documentation</summary>
        /// <param name=""value"">The value parameter</param>
        public void MethodA(string value) { }
        
        /// <inheritdoc cref=""MethodA""/>
        public void MethodB(string value) { }
    }
}";

            var compilation = CSharpCompilation.Create("TestAssembly")
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(source));

            var manager = new AssemblyManager("", "");
            
            // Act - Get symbols
            var baseClass = compilation.GetTypeByMetadataName("TestNamespace.BaseClass");
            var derivedClass = compilation.GetTypeByMetadataName("TestNamespace.DerivedClass");
            var anotherClass = compilation.GetTypeByMetadataName("TestNamespace.AnotherClass");
            
            baseClass.Should().NotBeNull();
            derivedClass.Should().NotBeNull();
            anotherClass.Should().NotBeNull();
            
            // Test class-level inheritdoc
            var derivedClassDocs = manager.ExtractDocumentationXml(derivedClass!);
            var derivedClassSummary = manager.ExtractSummary(derivedClassDocs);
            
            // Test method-level inheritdoc
            var baseMethod = baseClass!.GetMembers("Method").First();
            var derivedMethod = derivedClass!.GetMembers("Method").First();
            var baseMethodDocs = manager.ExtractDocumentationXml(baseMethod);
            var derivedMethodDocs = manager.ExtractDocumentationXml(derivedMethod);
            
            var baseMethodSummary = manager.ExtractSummary(baseMethodDocs);
            var derivedMethodSummary = manager.ExtractSummary(derivedMethodDocs);
            var derivedMethodReturns = manager.ExtractReturns(derivedMethodDocs);
            
            // Test cref inheritdoc
            var methodA = anotherClass!.GetMembers("MethodA").First();
            var methodB = anotherClass.GetMembers("MethodB").First();
            var methodADocs = manager.ExtractDocumentationXml(methodA);
            var methodBDocs = manager.ExtractDocumentationXml(methodB);
            
            var methodASummary = manager.ExtractSummary(methodADocs);
            var methodBSummary = manager.ExtractSummary(methodBDocs);
            
            // Assert
            
            // Without XML documentation file, Roslyn won't resolve <inheritdoc/>
            // The inherited documentation would be null/empty
            derivedClassSummary.Should().BeNull("Roslyn doesn't resolve inheritdoc without XML file");
            derivedMethodSummary.Should().BeNull("Roslyn doesn't resolve inheritdoc without XML file");
            derivedMethodReturns.Should().BeNull("Roslyn doesn't resolve inheritdoc without XML file");
            methodBSummary.Should().BeNull("Roslyn doesn't resolve inheritdoc cref without XML file");
            
            // The base documentation should be present
            baseMethodSummary.Should().Be("Base method summary");
            methodASummary.Should().Be("Original documentation");
        }

        [TestMethod]
        public void Include_Tag_Should_Be_Handled()
        {
            // Arrange
            var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        /// <include file='docs.xml' path='doc/member[@name=""TestMethod""]/*'/>
        public void TestMethod() { }
    }
}";

            var compilation = CSharpCompilation.Create("TestAssembly")
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(source));

            var manager = new AssemblyManager("", "");
            
            // Act
            var testClass = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
            testClass.Should().NotBeNull();
            
            var testMethod = testClass!.GetMembers("TestMethod").First();
            var docs = manager.ExtractDocumentationXml(testMethod);
            
            // Assert
            // Without the external file, the documentation should be null/empty
            // Roslyn doesn't automatically load external files
            docs.Should().BeNull("Roslyn doesn't resolve include tags without processing");
        }

        #endregion

    }

}