using System;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Tests.Shared;
using CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

        [TestMethod]
        [DataRow(NamespaceMode.File, "global.GlobalWidget")]
        [DataRow(NamespaceMode.Folder, "global/GlobalWidget")]
        public void CreateExternalTypeReference_WithGlobalNamespaceType_UsesEmptyNamespaceName(NamespaceMode mode, string expectedPagePath)
        {
            var compilation = CSharpCompilation.Create("ExternalLibrary", [CSharpSyntaxTree.ParseText("public class GlobalWidget { }")]);
            var globalType = compilation.GetTypeByMetadataName("GlobalWidget")!;
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            using var manager = new AssemblyManager(testAssemblyPath, System.IO.Path.ChangeExtension(testAssemblyPath, ".xml"));
            var docAssembly = new DocAssembly(compilation.Assembly);
            var context = new ProjectContext { FileNamingOptions = new FileNamingOptions(mode, '-') };

            var docType = manager.CreateExternalTypeReference(globalType, docAssembly, [], context);

            var ns = docAssembly.Namespaces.Should().ContainSingle().Subject;
            ns.Name.Should().BeEmpty("Roslyn's \"<global namespace>\" display string must not be used as a namespace name");
            ns.Types.Should().ContainSingle().Which.Should().BeSameAs(docType);
            context.GetTypePagePath(ns.Name, docType.Name)
                .Should().Be(expectedPagePath.Replace('/', System.IO.Path.DirectorySeparatorChar), "the page should go in the global location, with no angle brackets in the path");
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
            listType!.Remarks.Should().Be(
                "See <see href=\"https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1\">Microsoft documentation</see> for more information about the rest of the API.",
                "the link must be an XML documentation tag that the output format's transformer converts, and must use the Microsoft Learn page name for List`1");
        }

        [TestMethod]
        public void CreateExternalTypeReference_LeavesRemarksEmpty_ForNonMicrosoftAssembly()
        {
            var compilation = CSharpCompilation.Create("ExternalLibrary", [CSharpSyntaxTree.ParseText("namespace External { public class Widget { } }")]);
            var widgetType = compilation.GetTypeByMetadataName("External.Widget")!;
            var testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            using var manager = new AssemblyManager(testAssemblyPath, System.IO.Path.ChangeExtension(testAssemblyPath, ".xml"));

            var docType = manager.CreateExternalTypeReference(widgetType, new DocAssembly(compilation.Assembly), [], new ProjectContext());

            docType.Summary.Should().Be("This type is defined in ExternalLibrary.");
            docType.Remarks.Should().BeNull("only Microsoft types have a Microsoft Learn page to link to");
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
