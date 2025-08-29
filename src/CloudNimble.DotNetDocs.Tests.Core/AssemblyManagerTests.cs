using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Assemblies;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Tests for the <see cref="AssemblyManager"/> class.
    /// </summary>
    [TestClass]
    public class AssemblyManagerTests
    {

        #region Fields

        private const string projectPath = "..//..//..//";
        private string tempDirectory = string.Empty;
        private string testAssemblyPath = string.Empty;
        private string testXmlPath = string.Empty;

        #endregion

        #region Test Cleanup

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }

        #endregion

        #region Tests

        [TestMethod]
        public async Task AssemblyManager_Constructor_ThrowsExceptionWhenAssemblyPathIsNull()
        {
            await Task.CompletedTask;
            
            Action action = () => new AssemblyManager(null!, "test.xml");

            action.Should().Throw<ArgumentNullException>()
                .WithMessage("*assemblyPath*");
        }

        [TestMethod]
        public async Task AssemblyManager_Constructor_ThrowsExceptionWhenAssemblyPathIsWhitespace()
        {
            await Task.CompletedTask;

            Action action = () => new AssemblyManager("  ", "test.xml");

            action.Should().Throw<ArgumentException>()
                .WithMessage("*assemblyPath*");
        }

        [TestMethod]
        public async Task DocumentAsync_ReturnsDocAssembly()
        {
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            var result = await manager.DocumentAsync();

            result.Should().NotBeNull();
            result.Should().BeOfType<DocAssembly>();
            result.AssemblyName.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public async Task DocumentAsync_ExtractsNamespaces()
        {
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            var result = await manager.DocumentAsync();

            result.Namespaces.Should().NotBeEmpty();
            result.Namespaces.Should().Contain(ns => ns.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");
        }

        [TestMethod]
        public async Task DocumentAsync_ExtractsTypes()
        {
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            var result = await manager.DocumentAsync();

            var testNamespace = result.Namespaces
                .FirstOrDefault(ns => ns.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");

            testNamespace.Should().NotBeNull();
            testNamespace!.Types.Should().NotBeEmpty();
            testNamespace.Types.Should().Contain(t => t.Symbol.Name == "SimpleClass");
            testNamespace.Types.Should().Contain(t => t.Symbol.Name == "ClassWithMethods");
        }

        [TestMethod]
        public async Task DocumentAsync_HandlesXmlDocumentationFile()
        {
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            var result = await manager.DocumentAsync();

            // Check that XML documentation was loaded
            var simpleClass = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SimpleClass");

            simpleClass.Should().NotBeNull();
            simpleClass!.Usage.Should().Contain("simple class for testing basic documentation extraction");
        }

        [TestMethod]
        public async Task DocumentAsync_ExtractsDocumentation()
        {
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            var result = await manager.DocumentAsync();

            var testType = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "SimpleClass");

            testType.Should().NotBeNull();
            testType!.Usage.Should().Contain("simple class");
            testType.Remarks.Should().Contain("remarks about the SimpleClass");
            testType.Examples.Should().Contain("new SimpleClass()");
        }

        [TestMethod]
        public async Task DocumentAsync_ExtractsMembers()
        {
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            var result = await manager.DocumentAsync();

            var testType = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "ClassWithMethods");

            testType!.Members.Should().NotBeEmpty();
            testType.Members.Should().Contain(m => m.Symbol.Name == "Calculate");
            testType.Members.Should().Contain(m => m.Symbol.Name == "Process");
        }

        [TestMethod]
        public async Task DocumentAsync_ExtractsMemberDocumentation()
        {
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            var result = await manager.DocumentAsync();

            var method = result.Namespaces
                .SelectMany(ns => ns.Types)
                .SelectMany(t => t.Members)
                .FirstOrDefault(m => m.Symbol.Name == "Calculate");

            method.Should().NotBeNull();
            method!.Usage.Should().Contain("Calculates the sum");
            method.Examples.Should().Contain("Calculate(3, 4)");
        }

        [TestMethod]
        public async Task DocumentAsync_ExtractsInheritanceRelationships()
        {
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            var result = await manager.DocumentAsync();

            var testType = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "DisposableClass");

            testType.Should().NotBeNull();
            testType!.RelatedApis.Should().Contain("System.IDisposable");
        }

        [TestMethod]
        public async Task DocumentAsync_WithProjectContext_UsesReferences()
        {
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext(null, typeof(object).Assembly.Location);

            var result = await manager.DocumentAsync(context);

            result.Should().NotBeNull();
            // Verify that references were loaded (compilation should be more complete)
            result.Symbol.Should().NotBeNull();
        }

        [TestMethod]
        public async Task DocumentAsync_WithIncludedMembers_FiltersMembers()
        {
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext([Accessibility.Public, Accessibility.Internal]);

            var result = await manager.DocumentAsync(context);

            // Verify that IncludedMembers is set on the assembly
            result.IncludedMembers.Should().Contain(Accessibility.Public);
            result.IncludedMembers.Should().Contain(Accessibility.Internal);

            // Verify cascading to namespaces
            foreach (var ns in result.Namespaces)
            {
                ns.IncludedMembers.Should().Contain(Accessibility.Public);
                ns.IncludedMembers.Should().Contain(Accessibility.Internal);

                // Verify cascading to types
                foreach (var type in ns.Types)
                {
                    type.IncludedMembers.Should().Contain(Accessibility.Public);
                    type.IncludedMembers.Should().Contain(Accessibility.Internal);
                }
            }
        }

        [TestMethod]
        public async Task DocumentAsync_WithRestrictedIncludedMembers_FiltersOutPrivateMembers()
        {
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext([Accessibility.Public]);

            var result = await manager.DocumentAsync(context);

            // The test assembly should only include public members
            var testType = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "MixedAccessClass");

            testType.Should().NotBeNull();
            testType!.Members.Should().NotBeEmpty();

            // Verify only public members are included
            testType.Members.Should().Contain(m => m.Name == "PublicMethod");
            testType.Members.Should().Contain(m => m.Name == "PublicField");
            // Properties appear as get_/set_ methods in Roslyn
            testType.Members.Should().Contain(m => m.Name == "get_PublicProperty" || m.Name == "set_PublicProperty");

            // Verify private/internal members are filtered out
            testType.Members.Should().NotContain(m => m.Name == "PrivateMethod");
            testType.Members.Should().NotContain(m => m.Name == "get_PrivateProperty" || m.Name == "set_PrivateProperty");
            testType.Members.Should().NotContain(m => m.Name == "InternalMethod");
            testType.Members.Should().NotContain(m => m.Name == "get_InternalProperty" || m.Name == "set_InternalProperty");
        }

        [TestMethod]
        public async Task DocumentAsync_WithInternalIncludedMembers_IncludesInternalMembers()
        {
            // With the new bridge compilation using IgnoresAccessChecksTo attribute,
            // we should now be able to see internal members even when loading a compiled assembly.
            
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext([Accessibility.Public, Accessibility.Internal]);

            var result = await manager.DocumentAsync(context);

            var testType = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "MixedAccessClass");

            testType.Should().NotBeNull();
            testType!.Members.Should().NotBeEmpty();

            // Verify public members are included
            testType.Members.Should().Contain(m => m.Name == "PublicMethod");
            // Properties appear as get_/set_ methods
            testType.Members.Should().Contain(m => m.Name == "get_PublicProperty" || m.Name == "set_PublicProperty");

            // With the bridge compilation, internal members should now be visible!
            testType.Members.Should().Contain(m => m.Name == "InternalMethod", 
                "bridge compilation with IgnoresAccessChecksTo should make internal methods visible");
            testType.Members.Should().Contain(m => m.Name == "get_InternalProperty" || m.Name == "set_InternalProperty", 
                "bridge compilation with IgnoresAccessChecksTo should make internal properties visible");
        }

        [TestMethod]
        public async Task DocumentAsync_ProducesConsistentBaseline()
        {
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            var result = await manager.DocumentAsync();

            // Debug: Check if the result has the expected structure
            result.Should().NotBeNull();
            result.AssemblyName.Should().Be("CloudNimble.DotNetDocs.Tests.Shared");
            result.Namespaces.Should().NotBeEmpty();
            result.Namespaces.Should().Contain(ns => ns.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");

            var testNamespace = result.Namespaces.FirstOrDefault(ns => ns.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");
            testNamespace.Should().NotBeNull();
            testNamespace!.Types.Should().NotBeEmpty();
            testNamespace.Types.Should().Contain(t => t.Name == "SimpleClass");

            // Serialize to JSON with deterministic settings
            var json = SerializeToJson(result);

            // Compare against baseline
            var projectPath = "..//..//..//";
            var baselinePath = Path.Combine(projectPath, "Baselines", "AssemblyManager", "BasicAssembly.json");
            
            if (File.Exists(baselinePath))
            {
                var baseline = await File.ReadAllTextAsync(baselinePath);
                json.Should().Be(baseline, 
                    "Assembly documentation has changed. If this is intentional, regenerate baselines using 'dotnet breakdance generate'");
            }
            else
            {
                Assert.Inconclusive($"Baseline not found at {baselinePath}. Run 'dotnet breakdance generate' to create baselines.");
            }
        }

        //[Ignore]
        //[TestMethod]
        //[DataRow(projectPath)]
        [BreakdanceManifestGenerator]
        public async Task GenerateAssemblyBaseline(string projectPath)
        {
            // Use the real Tests.Shared assembly and its XML documentation
            var assemblyPath = typeof(SimpleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            
            var manager = new AssemblyManager(assemblyPath, xmlPath);
            var result = await manager.DocumentAsync();

            // Serialize to JSON with deterministic settings
            var json = SerializeToJson(result);

            // Write baseline
            var baselinePath = Path.Combine(projectPath, "Baselines", "AssemblyManager", "BasicAssembly.json");
            var directory = Path.GetDirectoryName(baselinePath);
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }
            
            await File.WriteAllTextAsync(baselinePath, json);
        }

        [TestInitialize]
        public void Setup()
        {
            // Use the real Tests.Shared assembly and its XML documentation
            testAssemblyPath = typeof(SimpleClass).Assembly.Location;
            testXmlPath = Path.ChangeExtension(testAssemblyPath, ".xml");
            
            // Create temp directory for conceptual content tests
            tempDirectory = Path.Combine(Path.GetTempPath(), $"AssemblyManagerTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDirectory);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Serializes a DocAssembly to JSON with deterministic settings.
        /// </summary>
        /// <param name="assembly">The assembly to serialize.</param>
        /// <returns>The JSON string.</returns>
        private string SerializeToJson(DocAssembly assembly)
        {
            return assembly.ToJson();
        }

        #endregion

    }

}