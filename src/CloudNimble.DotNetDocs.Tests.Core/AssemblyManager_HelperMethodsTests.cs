using System.Linq;
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
            var refs = new System.Collections.Generic.List<MetadataReference>
            {
                // Add reference to mscorlib/System.Private.CoreLib
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };

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
                new[] { CSharpSyntaxTree.ParseText(derivedSource, cancellationToken: TestContext.CancellationToken) },
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
                new[] { CSharpSyntaxTree.ParseText(derivedSource, cancellationToken: TestContext.CancellationToken) },
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

        #region Tests - GetLearnTypeName

        [TestMethod]
        public void GetLearnTypeName_NonGenericType_ReturnsFullName()
        {
            var compilation = CreateCompilation("public class Test { }");
            var stringType = compilation.GetSpecialType(SpecialType.System_String);

            AssemblyManager.GetLearnTypeName(stringType).Should().Be("System.String");
        }

        [TestMethod]
        [DataRow("System.Collections.Generic.List<int>", "System.Collections.Generic.List`1")]
        [DataRow("System.Collections.Generic.Dictionary<string, int>", "System.Collections.Generic.Dictionary`2")]
        [DataRow("System.Collections.Generic.Dictionary<string, int>.KeyCollection", "System.Collections.Generic.Dictionary`2.KeyCollection")]
        public void GetLearnTypeName_ConstructedGenericType_UsesArityOfDefinition(string propertyType, string expected)
        {
            var compilation = CreateCompilation($"public class Test {{ public {propertyType} Items {{ get; set; }} }}");
            var itemsType = ((IPropertySymbol)compilation.GetTypeByMetadataName("Test")!.GetMembers("Items").First()).Type;

            AssemblyManager.GetLearnTypeName(itemsType).Should().Be(expected, "Microsoft Learn pages are named by generic arity, so type arguments in braces would link to a page that does not exist");
        }

        [TestMethod]
        public void GetLearnTypeName_UnresolvedType_StripsErrorPrefix()
        {
            var compilation = CSharpCompilation.Create("NoReferences", [CSharpSyntaxTree.ParseText("public class Test { public List<int> Items { get; set; } }")]);
            var itemsType = ((IPropertySymbol)compilation.GetTypeByMetadataName("Test")!.GetMembers("Items").First()).Type;

            itemsType.TypeKind.Should().Be(TypeKind.Error);
            AssemblyManager.GetLearnTypeName(itemsType).Should().Be("List`1", "unresolved types have the '!:' prefix instead of 'T:'");
        }

        [TestMethod]
        public void GetLearnTypeName_UnresolvedNestedTypeOfConstructedType_ReturnsNull()
        {
            var compilation = CSharpCompilation.Create("NoReferences", [CSharpSyntaxTree.ParseText("public class Test { public Dictionary<string, int>.KeyCollection Keys { get; set; } }")]);
            var keysType = ((IPropertySymbol)compilation.GetTypeByMetadataName("Test")!.GetMembers("Keys").First()).Type;

            AssemblyManager.GetLearnTypeName(keysType).Should().BeNull("an ID that still contains type arguments cannot be turned into a Microsoft Learn page name");
        }

        #endregion

    }

}
