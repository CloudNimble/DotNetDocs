using System;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Tests for the <see cref="AssemblyManager"/> helper methods.
    /// </summary>
    [TestClass]
    public class AssemblyManager_HelperMethodsTests : DotNetDocsTestBase
    {

        #region Helper Methods

        private CSharpCompilation CreateCompilation(string source, string assemblyName = "TestAssembly")
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var references = GetMetadataReferences();

            return CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private System.Collections.Generic.IEnumerable<MetadataReference> GetMetadataReferences()
        {
            var refs = new System.Collections.Generic.List<MetadataReference>();

            // Add reference to mscorlib/System.Private.CoreLib
            refs.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

            // Add reference to System.Runtime
            var systemRuntimePath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!,
                "System.Runtime.dll");
            if (System.IO.File.Exists(systemRuntimePath))
            {
                refs.Add(MetadataReference.CreateFromFile(systemRuntimePath));
            }

            // Add reference to System.Collections
            var collectionsPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!,
                "System.Collections.dll");
            if (System.IO.File.Exists(collectionsPath))
            {
                refs.Add(MetadataReference.CreateFromFile(collectionsPath));
            }

            return refs;
        }

        #endregion

        #region Tests - IsAccessibleInDerivedType

        [TestMethod]
        public void IsAccessibleInDerivedType_PublicMember_ReturnsTrue()
        {
            // Arrange
            var source = @"
namespace Test
{
    public class Base
    {
        public void PublicMethod() { }
    }
    public class Derived : Base { }
}";
            var compilation = CreateCompilation(source);
            var baseType = compilation.GetTypeByMetadataName("Test.Base")!;
            var derivedType = compilation.GetTypeByMetadataName("Test.Derived")!;
            var publicMethod = baseType.GetMembers("PublicMethod").First();

            // Act
            var result = AssemblyManager.IsAccessibleInDerivedType(publicMethod, derivedType);

            // Assert
            result.Should().BeTrue("Public members are always accessible in derived types");
        }

        [TestMethod]
        public void IsAccessibleInDerivedType_ProtectedMember_ReturnsTrue()
        {
            // Arrange
            var source = @"
namespace Test
{
    public class Base
    {
        protected void ProtectedMethod() { }
    }
    public class Derived : Base { }
}";
            var compilation = CreateCompilation(source);
            var baseType = compilation.GetTypeByMetadataName("Test.Base")!;
            var derivedType = compilation.GetTypeByMetadataName("Test.Derived")!;
            var protectedMethod = baseType.GetMembers("ProtectedMethod").First();

            // Act
            var result = AssemblyManager.IsAccessibleInDerivedType(protectedMethod, derivedType);

            // Assert
            result.Should().BeTrue("Protected members are accessible in derived types");
        }

        [TestMethod]
        public void IsAccessibleInDerivedType_ProtectedOrInternalMember_ReturnsTrue()
        {
            // Arrange
            var source = @"
namespace Test
{
    public class Base
    {
        protected internal void ProtectedInternalMethod() { }
    }
    public class Derived : Base { }
}";
            var compilation = CreateCompilation(source);
            var baseType = compilation.GetTypeByMetadataName("Test.Base")!;
            var derivedType = compilation.GetTypeByMetadataName("Test.Derived")!;
            var protectedInternalMethod = baseType.GetMembers("ProtectedInternalMethod").First();

            // Act
            var result = AssemblyManager.IsAccessibleInDerivedType(protectedInternalMethod, derivedType);

            // Assert
            result.Should().BeTrue("Protected internal members are accessible in derived types");
        }

        [TestMethod]
        public void IsAccessibleInDerivedType_InternalMember_SameAssembly_ReturnsTrue()
        {
            // Arrange
            var source = @"
namespace Test
{
    public class Base
    {
        internal void InternalMethod() { }
    }
    public class Derived : Base { }
}";
            var compilation = CreateCompilation(source);
            var baseType = compilation.GetTypeByMetadataName("Test.Base")!;
            var derivedType = compilation.GetTypeByMetadataName("Test.Derived")!;
            var internalMethod = baseType.GetMembers("InternalMethod").First();

            // Act
            var result = AssemblyManager.IsAccessibleInDerivedType(internalMethod, derivedType);

            // Assert
            result.Should().BeTrue("Internal members are accessible in derived types within the same assembly");
        }

        [TestMethod]
        public void IsAccessibleInDerivedType_InternalMember_DifferentAssembly_ReturnsFalse()
        {
            // Arrange
            var baseSource = @"
namespace Test
{
    public class Base
    {
        internal void InternalMethod() { }
    }
}";
            var derivedSource = @"
namespace Test
{
    public class Derived : Base { }
}";

            var baseCompilation = CreateCompilation(baseSource, "BaseAssembly");
            var baseReference = baseCompilation.ToMetadataReference();
            var derivedCompilation = CSharpCompilation.Create(
                "DerivedAssembly",
                new[] { CSharpSyntaxTree.ParseText(derivedSource) },
                new[] { baseReference }.Concat(GetMetadataReferences()),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var baseType = baseCompilation.GetTypeByMetadataName("Test.Base")!;
            var derivedType = derivedCompilation.GetTypeByMetadataName("Test.Derived")!;
            var internalMethod = baseType.GetMembers("InternalMethod").First();

            // Act
            var result = AssemblyManager.IsAccessibleInDerivedType(internalMethod, derivedType);

            // Assert
            result.Should().BeFalse("Internal members are not accessible across assemblies");
        }

        [TestMethod]
        public void IsAccessibleInDerivedType_PrivateMember_ReturnsFalse()
        {
            // Arrange
            var source = @"
namespace Test
{
    public class Base
    {
        private void PrivateMethod() { }
    }
    public class Derived : Base { }
}";
            var compilation = CreateCompilation(source);
            var baseType = compilation.GetTypeByMetadataName("Test.Base")!;
            var derivedType = compilation.GetTypeByMetadataName("Test.Derived")!;
            var privateMethod = baseType.GetMembers("PrivateMethod").First();

            // Act
            var result = AssemblyManager.IsAccessibleInDerivedType(privateMethod, derivedType);

            // Assert
            result.Should().BeFalse("Private members are not accessible in derived types");
        }

        [TestMethod]
        public void IsAccessibleInDerivedType_ProtectedAndInternalMember_SameAssembly_ReturnsTrue()
        {
            // Arrange
            var source = @"
namespace Test
{
    public class Base
    {
        private protected void ProtectedAndInternalMethod() { }
    }
    public class Derived : Base { }
}";
            var compilation = CreateCompilation(source);
            var baseType = compilation.GetTypeByMetadataName("Test.Base")!;
            var derivedType = compilation.GetTypeByMetadataName("Test.Derived")!;
            var protectedAndInternalMethod = baseType.GetMembers("ProtectedAndInternalMethod").First();

            // Act
            var result = AssemblyManager.IsAccessibleInDerivedType(protectedAndInternalMethod, derivedType);

            // Assert
            result.Should().BeTrue("Protected and internal members are accessible in derived types within the same assembly");
        }

        [TestMethod]
        public void IsAccessibleInDerivedType_ProtectedAndInternalMember_DifferentAssembly_ReturnsFalse()
        {
            // Arrange
            var baseSource = @"
namespace Test
{
    public class Base
    {
        private protected void ProtectedAndInternalMethod() { }
    }
}";
            var derivedSource = @"
namespace Test
{
    public class Derived : Base { }
}";

            var baseCompilation = CreateCompilation(baseSource, "BaseAssembly");
            var baseReference = baseCompilation.ToMetadataReference();
            var derivedCompilation = CSharpCompilation.Create(
                "DerivedAssembly",
                new[] { CSharpSyntaxTree.ParseText(derivedSource) },
                new[] { baseReference }.Concat(GetMetadataReferences()),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var baseType = baseCompilation.GetTypeByMetadataName("Test.Base")!;
            var derivedType = derivedCompilation.GetTypeByMetadataName("Test.Derived")!;
            var protectedAndInternalMethod = baseType.GetMembers("ProtectedAndInternalMethod").First();

            // Act
            var result = AssemblyManager.IsAccessibleInDerivedType(protectedAndInternalMethod, derivedType);

            // Assert
            result.Should().BeFalse("Protected and internal members are not accessible across assemblies");
        }

        #endregion

        #region Tests - GetMicrosoftDocsUrl

        [TestMethod]
        public void GetMicrosoftDocsUrl_SystemType_ReturnsCorrectUrl()
        {
            // Arrange
            var source = "public class Test { }";
            var compilation = CreateCompilation(source);
            var stringType = compilation.GetSpecialType(SpecialType.System_String);

            // Act
            var url = AssemblyManager.GetMicrosoftDocsUrl(stringType);

            // Assert
            url.Should().NotBeNullOrEmpty();
            url.Should().StartWith("https://learn.microsoft.com/dotnet/api/");
            url.Should().Contain("system.string");
        }

        [TestMethod]
        public void GetMicrosoftDocsUrl_MicrosoftType_ReturnsCorrectUrl()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var source = @"
using System.Collections.Generic;
public class Test
{
    public List<int> Items { get; set; }
}";
            var compilation = CreateCompilation(source);
            var testType = compilation.GetTypeByMetadataName("Test")!;
            var itemsProperty = (IPropertySymbol)testType.GetMembers("Items").First();
            var listType = itemsProperty.Type;

            // Act
            var url = AssemblyManager.GetMicrosoftDocsUrl((ITypeSymbol)listType);

            // Assert
            url.Should().NotBeNullOrEmpty();
            url.Should().StartWith("https://learn.microsoft.com/dotnet/api/");
            url.Should().Contain("system.collections.generic.list");
        }

        [TestMethod]
        public void GetMicrosoftDocsUrl_GenericType_ConvertsAngleBrackets()
        {
            // Arrange
            var source = @"
using System.Collections.Generic;
public class Test
{
    public List<int> Items { get; set; }
}";
            var compilation = CreateCompilation(source);
            var testType = compilation.GetTypeByMetadataName("Test")!;
            var itemsProperty = (IPropertySymbol)testType.GetMembers("Items").First();
            var listType = itemsProperty.Type;

            // Act
            var url = AssemblyManager.GetMicrosoftDocsUrl((ITypeSymbol)listType);

            // Assert
            url.Should().NotContain("<", "Angle brackets should be converted");
            url.Should().NotContain(">", "Angle brackets should be converted");
            url.Should().Contain("{", "Should use curly braces for generics");
            url.Should().Contain("}", "Should use curly braces for generics");
        }

        [TestMethod]
        public void GetMicrosoftDocsUrl_NonMicrosoftType_ReturnsEmptyString()
        {
            // Arrange
            var source = "namespace Custom { public class MyType { } }";
            var compilation = CreateCompilation(source);
            var customType = compilation.GetTypeByMetadataName("Custom.MyType")!;

            // Act
            var url = AssemblyManager.GetMicrosoftDocsUrl(customType);

            // Assert
            url.Should().BeEmpty("Non-Microsoft types should return empty string");
        }

        [TestMethod]
        public void GetMicrosoftDocsUrl_UrlIsLowerCase()
        {
            // Arrange
            var source = "public class Test { }";
            var compilation = CreateCompilation(source);
            var stringType = compilation.GetSpecialType(SpecialType.System_String);

            // Act
            var url = AssemblyManager.GetMicrosoftDocsUrl(stringType);

            // Assert
            url.Should().Be(url.ToLowerInvariant(), "URLs should be lowercase for MS docs");
        }

        #endregion

    }

}
