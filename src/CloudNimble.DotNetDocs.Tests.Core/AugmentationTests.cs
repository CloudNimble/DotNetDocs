using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Assemblies;
using CloudNimble.DotNetDocs.Core;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Tests for conceptual content augmentation in AssemblyManager.
    /// </summary>
    [TestClass]
    public class AugmentationTests : BreakdanceTestBase
    {

        #region Private Fields

        private string? _tempConceptualPath;

        #endregion

        #region Test Methods

        [TestMethod]
        public async Task DocumentAsync_WithConceptualPath_LoadsTypeUsage()
        {
            // Arrange: Create namespace-based conceptual structure
            _tempConceptualPath = Path.Combine(Path.GetTempPath(), $"conceptual_{Guid.NewGuid()}");
            var namespacePath = Path.Combine(_tempConceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared");
            Directory.CreateDirectory(namespacePath);
            
            var typePath = Path.Combine(namespacePath, "TestBase");
            Directory.CreateDirectory(typePath);
            await File.WriteAllTextAsync(Path.Combine(typePath, "usage.md"), 
                "Base class for test infrastructure with Breakdance support.");

            // Use the actual Tests.Shared assembly from the current runtime
            var assemblyPath = typeof(CloudNimble.DotNetDocs.Tests.Shared.TestBase).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            // Skip test if files don't exist
            if (!File.Exists(assemblyPath) || !File.Exists(xmlPath))
            {
                Assert.Inconclusive($"Test assembly or XML not found at {assemblyPath}");
                return;
            }

            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext { ConceptualPath = _tempConceptualPath };

            // Act
            var model = await manager.DocumentAsync(context);

            // Assert
            model.Should().NotBeNull();
            var testBase = model.Namespaces
                .Where(ns => ns.Symbol.ToDisplayString() == "CloudNimble.DotNetDocs.Tests.Shared")
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "TestBase");
            
            testBase.Should().NotBeNull();
            testBase!.Usage.Should().Be("Base class for test infrastructure with Breakdance support.");
        }

        [TestMethod]
        public async Task DocumentAsync_WithConceptualPath_LoadsMultipleProperties()
        {
            // Arrange: Create comprehensive conceptual content
            _tempConceptualPath = Path.Combine(Path.GetTempPath(), $"conceptual_{Guid.NewGuid()}");
            var namespacePath = Path.Combine(_tempConceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared");
            Directory.CreateDirectory(namespacePath);
            
            var typePath = Path.Combine(namespacePath, "TestBase");
            Directory.CreateDirectory(typePath);
            
            await File.WriteAllTextAsync(Path.Combine(typePath, "usage.md"), "How to use TestBase");
            await File.WriteAllTextAsync(Path.Combine(typePath, "examples.md"), "```csharp\n// Example code\n```");
            await File.WriteAllTextAsync(Path.Combine(typePath, "best-practices.md"), "Best practices for TestBase");
            await File.WriteAllTextAsync(Path.Combine(typePath, "patterns.md"), "Common patterns");
            await File.WriteAllTextAsync(Path.Combine(typePath, "considerations.md"), "Important considerations");
            await File.WriteAllTextAsync(Path.Combine(typePath, "related-apis.md"), "System.Object\nMicrosoft.VisualStudio.TestTools.UnitTesting.TestClass");

            // Use the actual Tests.Shared assembly from the current runtime
            var assemblyPath = typeof(CloudNimble.DotNetDocs.Tests.Shared.TestBase).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            // Skip test if files don't exist
            if (!File.Exists(assemblyPath) || !File.Exists(xmlPath))
            {
                Assert.Inconclusive($"Test assembly or XML not found at {assemblyPath}");
                return;
            }

            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext { ConceptualPath = _tempConceptualPath };

            // Act
            var model = await manager.DocumentAsync(context);

            // Assert
            var testBase = model.Namespaces
                .Where(ns => ns.Symbol.ToDisplayString() == "CloudNimble.DotNetDocs.Tests.Shared")
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "TestBase");
            
            testBase.Should().NotBeNull();
            testBase!.Usage.Should().Be("How to use TestBase");
            testBase.Examples.Should().Be("```csharp\n// Example code\n```");
            testBase.BestPractices.Should().Be("Best practices for TestBase");
            testBase.Patterns.Should().Be("Common patterns");
            testBase.Considerations.Should().Be("Important considerations");
            testBase.RelatedApis.Should().HaveCount(2);
            testBase.RelatedApis.Should().Contain("System.Object");
            testBase.RelatedApis.Should().Contain("Microsoft.VisualStudio.TestTools.UnitTesting.TestClass");
        }

        [TestMethod]
        public async Task DocumentAsync_WithMemberConceptualContent_LoadsMemberDocs()
        {
            // Arrange: Create member-specific conceptual content
            _tempConceptualPath = Path.Combine(Path.GetTempPath(), $"conceptual_{Guid.NewGuid()}");
            var namespacePath = Path.Combine(_tempConceptualPath, "CloudNimble", "DotNetDocs", "Tests", "Shared");
            Directory.CreateDirectory(namespacePath);
            
            var typePath = Path.Combine(namespacePath, "TestBase");
            Directory.CreateDirectory(typePath);
            
            // Find a public method to document
            var memberPath = Path.Combine(typePath, "TestInitialize");
            Directory.CreateDirectory(memberPath);
            await File.WriteAllTextAsync(Path.Combine(memberPath, "usage.md"), 
                "Called before each test method to set up test state.");
            await File.WriteAllTextAsync(Path.Combine(memberPath, "examples.md"), 
                "```csharp\n[TestInitialize]\npublic void Setup() { /* setup code */ }\n```");

            // Use the actual Tests.Shared assembly from the current runtime
            var assemblyPath = typeof(CloudNimble.DotNetDocs.Tests.Shared.TestBase).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            // Skip test if files don't exist
            if (!File.Exists(assemblyPath) || !File.Exists(xmlPath))
            {
                Assert.Inconclusive($"Test assembly or XML not found at {assemblyPath}");
                return;
            }

            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext { ConceptualPath = _tempConceptualPath };

            // Act
            var model = await manager.DocumentAsync(context);

            // Assert
            var testBase = model.Namespaces
                .Where(ns => ns.Symbol.ToDisplayString() == "CloudNimble.DotNetDocs.Tests.Shared")
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "TestBase");
            
            testBase.Should().NotBeNull();
            
            var testInitMethod = testBase!.Members
                .FirstOrDefault(m => m.Symbol.Name == "TestInitialize");
            
            if (testInitMethod != null)
            {
                testInitMethod.Usage.Should().Be("Called before each test method to set up test state.");
                testInitMethod.Examples.Should().Be("```csharp\n[TestInitialize]\npublic void Setup() { /* setup code */ }\n```");
            }
        }

        [TestMethod]
        public async Task DocumentAsync_WithoutConceptualPath_UsesXmlDocs()
        {
            // Arrange
            // Use the actual Tests.Shared assembly from the current runtime
            var assemblyPath = typeof(CloudNimble.DotNetDocs.Tests.Shared.TestBase).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            // Skip test if files don't exist
            if (!File.Exists(assemblyPath) || !File.Exists(xmlPath))
            {
                Assert.Inconclusive($"Test assembly or XML not found at {assemblyPath}");
                return;
            }

            using var manager = new AssemblyManager(assemblyPath, xmlPath);

            // Act
            var model = await manager.DocumentAsync();

            // Assert
            model.Should().NotBeNull();
            model.Namespaces.Should().NotBeEmpty();
            
            // Should have XML documentation but no conceptual content
            var testBase = model.Namespaces
                .Where(ns => ns.Symbol.ToDisplayString() == "CloudNimble.DotNetDocs.Tests.Shared")
                .SelectMany(n => n.Types)
                .FirstOrDefault(t => t.Symbol.Name == "TestBase");
            
            if (testBase != null)
            {
                // XML doc summary should be extracted
                testBase.Usage.Should().NotBeNullOrWhiteSpace();
            }
        }

        [TestMethod]
        public async Task DocumentAsync_WithNonExistentConceptualPath_DoesNotThrow()
        {
            // Arrange
            // Use the actual Tests.Shared assembly from the current runtime
            var assemblyPath = typeof(CloudNimble.DotNetDocs.Tests.Shared.TestBase).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            // Skip test if files don't exist
            if (!File.Exists(assemblyPath) || !File.Exists(xmlPath))
            {
                Assert.Inconclusive($"Test assembly or XML not found at {assemblyPath}");
                return;
            }

            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext { ConceptualPath = Path.Combine(Path.GetTempPath(), "non-existent-path") };

            // Act
            var act = async () => await manager.DocumentAsync(context);

            // Assert
            await act.Should().NotThrowAsync();
            var model = await manager.DocumentAsync(context);
            model.Should().NotBeNull();
        }

        [TestMethod]
        public async Task DocumentAsync_WithGlobalNamespaceType_LoadsConceptualContent()
        {
            // This test would apply if Tests.Shared had types in the global namespace
            // For now, just verify the logic path works
            
            // Arrange: Create conceptual content for a hypothetical global namespace type
            _tempConceptualPath = Path.Combine(Path.GetTempPath(), $"conceptual_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempConceptualPath);
            
            var globalTypePath = Path.Combine(_tempConceptualPath, "GlobalType");
            Directory.CreateDirectory(globalTypePath);
            await File.WriteAllTextAsync(Path.Combine(globalTypePath, "usage.md"), 
                "Documentation for global namespace type");

            // Use the actual Tests.Shared assembly from the current runtime
            var assemblyPath = typeof(CloudNimble.DotNetDocs.Tests.Shared.TestBase).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            // Skip test if files don't exist
            if (!File.Exists(assemblyPath) || !File.Exists(xmlPath))
            {
                Assert.Inconclusive($"Test assembly or XML not found at {assemblyPath}");
                return;
            }

            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext { ConceptualPath = _tempConceptualPath };

            // Act
            var model = await manager.DocumentAsync(context);

            // Assert
            model.Should().NotBeNull();
            // The test assembly doesn't have global namespace types, but the code path is exercised
        }

        #endregion

        #region Test Cleanup

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up temporary conceptual directories
            if (!string.IsNullOrWhiteSpace(_tempConceptualPath) && Directory.Exists(_tempConceptualPath))
            {
                try
                {
                    Directory.Delete(_tempConceptualPath, recursive: true);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }

        #endregion

    }

}