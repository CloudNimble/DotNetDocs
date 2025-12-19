using System;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Tests for the <see cref="AssemblyManager"/> class focusing on extension method relocation.
    /// </summary>
    [TestClass]
    public class AssemblyManager_ExtensionMethodsTests : DotNetDocsTestBase
    {

        #region Tests - Extension Method Detection

        [TestMethod]
        public async Task RelocateExtensionMethods_DetectsExtensionMethods_InTraditionalPattern()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext();

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            // Extension methods from StringExtensions should be moved to System.String
            var allTypes = result.Namespaces.SelectMany(ns => ns.Types).ToList();

            // The StringExtensions class should be removed (empty after relocation)
            var stringExtensionsClass = allTypes.FirstOrDefault(t => t.Name == "StringExtensions");
            stringExtensionsClass.Should().BeNull("Empty extension classes should be removed");
        }

        [TestMethod]
        public async Task RelocateExtensionMethods_DetectsExtensionMethods_InDiscoverablePattern()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext();

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            // The TestsShared_SimpleClassExtensions class should be removed (empty after relocation)
            var extensionsClass = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "TestsShared_SimpleClassExtensions");

            extensionsClass.Should().BeNull("Empty extension classes should be removed");
        }

        [TestMethod]
        public async Task RelocateExtensionMethods_MarksExtensionMethods_WithIsExtensionMethod()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext();

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var simpleClass = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "SimpleClass");

            simpleClass.Should().NotBeNull();

            // Should have extension methods relocated to it
            var extensionMethod = simpleClass!.Members.FirstOrDefault(m => m.Name == "ToDisplayString");
            extensionMethod.Should().NotBeNull("Extension method should be relocated to SimpleClass");
            extensionMethod!.IsExtensionMethod.Should().BeTrue();
            extensionMethod.DeclaringTypeName.Should().Contain("TestsShared_SimpleClassExtensions");
            extensionMethod.ExtendedTypeName.Should().Contain("SimpleClass");
        }

        #endregion

        #region Tests - Extension Method Relocation

        [TestMethod]
        public async Task RelocateExtensionMethods_MovesExtensionMethods_ToTargetType()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext();

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var simpleClass = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "SimpleClass");

            simpleClass.Should().NotBeNull();

            // Should have extension methods from TestsShared_SimpleClassExtensions
            simpleClass!.Members.Should().Contain(m => m.Name == "ToDisplayString" && m.IsExtensionMethod);
            simpleClass.Members.Should().Contain(m => m.Name == "IsValid" && m.IsExtensionMethod);
        }

        [TestMethod]
        public async Task RelocateExtensionMethods_RemovesEmptyExtensionClasses()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext();

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var allTypes = result.Namespaces.SelectMany(ns => ns.Types).ToList();

            // All extension classes should be removed
            allTypes.Should().NotContain(t => t.Name == "StringExtensions");
            allTypes.Should().NotContain(t => t.Name == "TestsShared_SimpleClassExtensions");
            allTypes.Should().NotContain(t => t.Name == "TestsShared_ITestInterfaceExtensions");
            allTypes.Should().NotContain(t => t.Name == "TestsShared_ListExtensions");
        }

        [TestMethod]
        public async Task RelocateExtensionMethods_HandlesInterfaceExtensions()
        {
            // Arrange
            var testAssemblyPath = typeof(ITestInterface).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext();

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var testInterface = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "ITestInterface");

            testInterface.Should().NotBeNull();

            // Should have extension methods relocated to the interface
            testInterface!.Members.Should().Contain(m => m.Name == "GetFormattedValue" && m.IsExtensionMethod);
            testInterface.Members.Should().Contain(m => m.Name == "Validate" && m.IsExtensionMethod);
        }

        [TestMethod]
        public async Task RelocateExtensionMethods_HandlesNoExtensionMethods()
        {
            // Arrange
            var testAssemblyPath = typeof(BaseClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext();

            // Act - Should not throw
            Func<Task> act = async () => await manager.DocumentAsync(context);

            // Assert
            await act.Should().NotThrowAsync("Should handle assemblies with no extension methods");
        }

        #endregion

        #region Tests - External Type References

        [TestMethod]
        public async Task RelocateExtensionMethods_CreatesExternalTypeReference_ForSystemTypes()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext
            {
                CreateExternalTypeReferences = true
            };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var allTypes = result.Namespaces.SelectMany(ns => ns.Types).ToList();

            // Should create external reference for List<T>
            var listType = allTypes.FirstOrDefault(t => t.Name == "List" && t.IsExternalReference);
            listType.Should().NotBeNull("Should create external reference for List<T>");
            listType!.IsExternalReference.Should().BeTrue();
            listType.Remarks.Should().Contain("Microsoft documentation", "Should link to MS docs");

            // Should have extension methods on List<T>
            listType.Members.Should().Contain(m => m.Name == "IsNullOrEmpty" && m.IsExtensionMethod);
            listType.Members.Should().Contain(m => m.Name == "AddMultiple" && m.IsExtensionMethod);
            listType.Members.Should().Contain(m => m.Name == "Shuffle" && m.IsExtensionMethod);
        }

        [TestMethod]
        public async Task RelocateExtensionMethods_WithCreateExternalTypeReferences_Disabled_SkipsExternalTypes()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext
            {
                CreateExternalTypeReferences = false
            };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var allTypes = result.Namespaces.SelectMany(ns => ns.Types).ToList();

            // Should NOT create external reference for List<T>
            var listType = allTypes.FirstOrDefault(t => t.Name == "List");
            listType.Should().BeNull("Should not create external references when disabled");
        }

        [TestMethod]
        public async Task CreateExternalTypeReference_CreatesNamespace_WhenMissing()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext
            {
                CreateExternalTypeReferences = true
            };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            // System.Collections.Generic namespace should be created for List<T>
            var systemNamespace = result.Namespaces.FirstOrDefault(ns => ns.Name == "System.Collections.Generic");
            systemNamespace.Should().NotBeNull("Should create namespace for external type");

            var listType = systemNamespace!.Types.FirstOrDefault(t => t.Name == "List");
            listType.Should().NotBeNull();
            listType!.IsExternalReference.Should().BeTrue();
        }

        [TestMethod]
        public async Task CreateExternalTypeReference_ReusesSameExternalType_ForMultipleExtensions()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext
            {
                CreateExternalTypeReferences = true
            };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var systemNamespace = result.Namespaces.FirstOrDefault(ns => ns.Name == "System.Collections.Generic");
            systemNamespace.Should().NotBeNull();

            // Should only have ONE List type even though multiple extensions target it
            var listTypes = systemNamespace!.Types.Where(t => t.Name == "List").ToList();
            listTypes.Should().ContainSingle("Should reuse same external type for multiple extensions");

            // But it should have all three extension methods
            var listType = listTypes.First();
            listType.Members.Count(m => m.IsExtensionMethod).Should().BeGreaterOrEqualTo(3);
        }

        #endregion

    }

}
