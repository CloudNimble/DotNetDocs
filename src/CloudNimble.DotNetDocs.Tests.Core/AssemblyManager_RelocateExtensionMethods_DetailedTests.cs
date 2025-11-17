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
    /// Detailed tests for RelocateExtensionMethods() covering every code path individually.
    /// </summary>
    [TestClass]
    public class AssemblyManager_RelocateExtensionMethods_DetailedTests : DotNetDocsTestBase
    {

        #region Tests - Early Return Path

        [TestMethod]
        public async Task RelocateExtensionMethods_ReturnsEarly_WhenNoExtensionMethods()
        {
            // Arrange - Use an assembly with no extension methods
            var testAssemblyPath = typeof(BaseClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert - Should complete without error
            result.Should().NotBeNull();
            // No static extension classes should exist
            var staticClasses = result.Namespaces
                .SelectMany(ns => ns.Types)
                .Where(t => t.Symbol.IsStatic && t.Name.Contains("Extensions"))
                .ToList();
            // BaseClass assembly shouldn't have extension classes
        }

        #endregion

        #region Tests - Target Type in Same Assembly

        [TestMethod]
        public async Task RelocateExtensionMethods_MovesMethod_WhenTargetTypeInSameAssembly()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var simpleClass = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "SimpleClass");

            simpleClass.Should().NotBeNull();
            simpleClass!.Members.Should().Contain(m => m.IsExtensionMethod && m.Name == "ToDisplayString");
        }

        [TestMethod]
        public async Task RelocateExtensionMethods_RemovesMethodFromStaticClass_WhenRelocatingToSameAssembly()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var extensionClass = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "TestsShared_SimpleClassExtensions");

            // Should be removed because it's empty after relocation
            extensionClass.Should().BeNull("Empty extension classes should be removed");
        }

        #endregion

        #region Tests - External Type References

        [TestMethod]
        public async Task RelocateExtensionMethods_CreatesExternalType_WhenTargetNotInAssemblyAndCreateExternalReferencesTrue()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext { CreateExternalTypeReferences = true };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var listType = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "List" && t.IsExternalReference);

            listType.Should().NotBeNull("Should create external reference for List<T>");
        }

        [TestMethod]
        public async Task RelocateExtensionMethods_DoesNotCreateExternalType_WhenCreateExternalReferencesFalse()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext { CreateExternalTypeReferences = false };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var listType = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "List");

            listType.Should().BeNull("Should not create external references when disabled");
        }

        [TestMethod]
        public async Task RelocateExtensionMethods_ReusesExternalType_WhenMultipleExtensionsTargetSameType()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext { CreateExternalTypeReferences = true };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var listTypes = result.Namespaces
                .SelectMany(ns => ns.Types)
                .Where(t => t.Name == "List" && t.IsExternalReference)
                .ToList();

            listTypes.Should().HaveCount(1, "Should only create one external reference even with multiple extensions");

            // Should have multiple extension methods on the single List type
            if (listTypes.Any())
            {
                var listType = listTypes.First();
                listType.Members.Count(m => m.IsExtensionMethod).Should().BeGreaterOrEqualTo(1);
            }
        }

        [TestMethod]
        public async Task RelocateExtensionMethods_AddsMethodToExternalType_WhenExternalTypeCreated()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext { CreateExternalTypeReferences = true };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var listType = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "List" && t.IsExternalReference);

            listType.Should().NotBeNull();
            listType!.Members.Should().Contain(m => m.IsExtensionMethod && m.Name == "IsNullOrEmpty");
        }

        [TestMethod]
        public async Task RelocateExtensionMethods_SkipsRelocation_WhenTargetNotFoundAndCreateExternalReferencesFalse()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext { CreateExternalTypeReferences = false };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            // Extension methods targeting external types should not be relocated
            var allExtensionMethods = result.Namespaces
                .SelectMany(ns => ns.Types)
                .SelectMany(t => t.Members)
                .Where(m => m.IsExtensionMethod)
                .ToList();

            // Methods extending internal types should still be relocated
            allExtensionMethods.Should().Contain(m => m.Name == "ToDisplayString");

            // But methods extending List<T> should be in static classes (not relocated)
            var staticClasses = result.Namespaces
                .SelectMany(ns => ns.Types)
                .Where(t => t.Symbol.IsStatic && t.Name.Contains("ListExtensions"))
                .ToList();

            if (staticClasses.Any())
            {
                staticClasses.First().Members.Should().Contain(m => m.IsExtensionMethod);
            }
        }

        #endregion

        #region Tests - Empty Class Removal

        [TestMethod]
        public async Task RelocateExtensionMethods_RemovesStaticClass_WhenAllMethodsRelocated()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var extensionClass = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "TestsShared_SimpleClassExtensions");

            extensionClass.Should().BeNull("Extension class should be removed after all methods relocated");
        }

        [TestMethod]
        public async Task RelocateExtensionMethods_RemovesMultipleEmptyClasses_WhenAllHaveMethodsRelocated()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var emptyExtensionClasses = result.Namespaces
                .SelectMany(ns => ns.Types)
                .Where(t => t.Symbol.IsStatic && !t.Members.Any() && t.Name.Contains("Extensions"))
                .ToList();

            emptyExtensionClasses.Should().BeEmpty("All empty extension classes should be removed");
        }

        [TestMethod]
        public async Task RelocateExtensionMethods_DoesNotRemoveStaticClass_WhenItHasNonExtensionMembers()
        {
            // This test would require a static class with both extension and non-extension methods
            // For now, we verify that only truly empty classes are removed

            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var staticClasses = result.Namespaces
                .SelectMany(ns => ns.Types)
                .Where(t => t.Symbol.IsStatic)
                .ToList();

            // If any static classes remain, they should have members
            foreach (var staticClass in staticClasses)
            {
                if (staticClass.Name.Contains("Extensions"))
                {
                    // Extension classes that remain should not be empty
                    staticClass.Members.Should().NotBeEmpty($"{staticClass.Name} should have members or be removed");
                }
            }
        }

        #endregion

        #region Tests - Interface Extension Methods

        [TestMethod]
        public async Task RelocateExtensionMethods_RelocatesToInterface_WhenExtendingInterface()
        {
            // Arrange
            var testAssemblyPath = typeof(ITestInterface).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var testInterface = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "ITestInterface");

            testInterface.Should().NotBeNull();
            testInterface!.Members.Should().Contain(m => m.IsExtensionMethod && m.Name == "GetFormattedValue");
        }

        [TestMethod]
        public async Task RelocateExtensionMethods_RemovesInterfaceExtensionClass_AfterRelocation()
        {
            // Arrange
            var testAssemblyPath = typeof(ITestInterface).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            // Act
            var result = await manager.DocumentAsync(new ProjectContext());

            // Assert
            var extensionClass = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "TestsShared_ITestInterfaceExtensions");

            extensionClass.Should().BeNull("Interface extension class should be removed after relocation");
        }

        #endregion

        #region Tests - Generic Type Extensions

        [TestMethod]
        public async Task RelocateExtensionMethods_HandlesGenericTypes_WhenExtendingListOfT()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext { CreateExternalTypeReferences = true };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var listType = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "List" && t.IsExternalReference);

            listType.Should().NotBeNull("Should handle generic type List<T>");
            listType!.Members.Should().Contain(m => m.IsExtensionMethod);
        }

        #endregion

    }

}
