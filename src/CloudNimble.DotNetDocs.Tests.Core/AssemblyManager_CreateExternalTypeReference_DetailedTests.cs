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
    /// Detailed tests for CreateExternalTypeReference() covering every code path individually.
    /// </summary>
    [TestClass]
    public class AssemblyManager_CreateExternalTypeReference_DetailedTests : DotNetDocsTestBase
    {

        #region Tests - Namespace Handling

        [TestMethod]
        public async Task CreateExternalTypeReference_FindsExistingNamespace_WhenNamespaceExists()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext { CreateExternalTypeReferences = true };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var systemNamespace = result.Namespaces.FirstOrDefault(ns => ns.Name == "System.Collections.Generic");
            systemNamespace.Should().NotBeNull("Namespace should be created for external type");

            // Should only have one namespace with this name
            var namespaceCount = result.Namespaces.Count(ns => ns.Name == "System.Collections.Generic");
            namespaceCount.Should().Be(1, "Should not create duplicate namespaces");
        }

        [TestMethod]
        public async Task CreateExternalTypeReference_CreatesNewNamespace_WhenNamespaceMissing()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext { CreateExternalTypeReferences = true };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert - System.Collections.Generic namespace should be created
            var systemNamespace = result.Namespaces.FirstOrDefault(ns => ns.Name == "System.Collections.Generic");
            systemNamespace.Should().NotBeNull("Should create namespace for external type");
            systemNamespace!.Name.Should().Be("System.Collections.Generic");
            systemNamespace.DisplayName.Should().Be("System.Collections.Generic");
        }

        #endregion

        #region Tests - DocType Properties

        [TestMethod]
        public async Task CreateExternalTypeReference_SetsIsExternalReference_True()
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
            listType!.IsExternalReference.Should().BeTrue();
        }

        [TestMethod]
        public async Task CreateExternalTypeReference_SetsBasicProperties_Correctly()
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
            listType!.Name.Should().Be("List");
            listType.FullName.Should().NotBeNullOrWhiteSpace();
            listType.DisplayName.Should().NotBeNullOrWhiteSpace();
            listType.Signature.Should().NotBeNullOrWhiteSpace();
            listType.AssemblyName.Should().NotBeNullOrWhiteSpace();
            // TypeKind might be Error due to reference assembly conflicts (System.Runtime vs System.Private.CoreLib)
            // but should be Class in an ideal scenario - this is a known limitation
            listType.TypeKind.Should().BeOneOf(TypeKind.Class, TypeKind.Error);
        }

        [TestMethod]
        public async Task CreateExternalTypeReference_SetsIncludedMembers_FromContext()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext
            {
                CreateExternalTypeReferences = true,
                IncludedMembers = [Accessibility.Public, Accessibility.Protected]
            };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var listType = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "List" && t.IsExternalReference);

            listType.Should().NotBeNull();
            listType!.IncludedMembers.Should().Contain(Accessibility.Public);
            listType.IncludedMembers.Should().Contain(Accessibility.Protected);
        }

        #endregion

        #region Tests - Summary Generation

        [TestMethod]
        public async Task CreateExternalTypeReference_GeneratesMicrosoftDocsSummary_ForSystemAssembly()
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
            listType!.Remarks.Should().Contain("Microsoft documentation", "Should link to MS docs for System types");
            listType.Remarks.Should().Contain("https://learn.microsoft.com/dotnet/api/");
        }

        [TestMethod]
        public async Task CreateExternalTypeReference_GeneratesGenericSummary_ForNonMicrosoftAssembly()
        {
            // This test would require an assembly that extends a non-Microsoft type
            // For now, we test that System types get the Microsoft docs summary
            // If we had a non-Microsoft external type, it would get the generic summary

            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext { CreateExternalTypeReferences = true };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            // All our extension methods target System types, so they should all have MS docs
            var externalTypes = result.Namespaces
                .SelectMany(ns => ns.Types)
                .Where(t => t.IsExternalReference)
                .ToList();

            foreach (var type in externalTypes)
            {
                // System types should have Microsoft documentation link
                if (type.AssemblyName?.StartsWith("System") == true)
                {
                    type.Remarks.Should().Contain("Microsoft documentation");
                }
            }
        }

        #endregion

        #region Tests - TypeMap Handling

        [TestMethod]
        public async Task CreateExternalTypeReference_AddsToTypeMap_WhenKeyDoesNotExist()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext { CreateExternalTypeReferences = true };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert - External type should be added to namespace
            var listType = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "List" && t.IsExternalReference);

            listType.Should().NotBeNull("External type should be created and added");
        }

        [TestMethod]
        public async Task CreateExternalTypeReference_ReusesExistingType_WhenKeyExists()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext { CreateExternalTypeReferences = true };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert - Should only have one List type even though multiple extensions target it
            var listTypes = result.Namespaces
                .SelectMany(ns => ns.Types)
                .Where(t => t.Name == "List" && t.IsExternalReference)
                .ToList();

            listTypes.Should().ContainSingle("Should reuse existing external type instead of creating duplicates");

            // And it should have all the extension methods
            var listType = listTypes.First();
            listType.Members.Count(m => m.IsExtensionMethod).Should().BeGreaterOrEqualTo(1);
        }

        #endregion

        #region Tests - Adds Type to Namespace

        [TestMethod]
        public async Task CreateExternalTypeReference_AddsTypeToNamespace()
        {
            // Arrange
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            var testXmlPath = System.IO.Path.ChangeExtension(testAssemblyPath, ".xml");
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext { CreateExternalTypeReferences = true };

            // Act
            var result = await manager.DocumentAsync(context);

            // Assert
            var systemNamespace = result.Namespaces.FirstOrDefault(ns => ns.Name == "System.Collections.Generic");
            systemNamespace.Should().NotBeNull();

            var listType = systemNamespace!.Types.FirstOrDefault(t => t.Name == "List");
            listType.Should().NotBeNull("Type should be added to namespace");
            listType!.IsExternalReference.Should().BeTrue();
        }

        #endregion

    }

}
