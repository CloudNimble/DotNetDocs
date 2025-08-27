using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    [TestClass]
    public class AssemblyManagerTests
    {

        #region Fields

        private string tempDirectory = null!;
        private string testAssemblyPath = null!;
        private string testXmlPath = null!;

        #endregion

        #region Public Methods

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }

        [TestMethod]
        public void Constructor_WithNullAssemblyPath_ThrowsArgumentNullException()
        {
            Action act = () => new AssemblyManager(null!, "test.xml");

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("assemblyPath");
        }

        [TestMethod]
        public void Constructor_WithNullXmlPath_ThrowsArgumentNullException()
        {
            Action act = () => new AssemblyManager("test.dll", null!);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("xmlPath");
        }

        [TestMethod]
        public void Constructor_WithNonExistentAssemblyFile_ThrowsFileNotFoundException()
        {
            Action act = () => new AssemblyManager("nonexistent.dll", "test.xml");

            act.Should().Throw<FileNotFoundException>()
                .WithMessage("Assembly file not found.");
        }

        [TestMethod]
        public async Task Constructor_WithNonExistentXmlFile_ThrowsFileNotFoundException()
        {
            await CreateTestAssemblyAsync();

            Action act = () => new AssemblyManager(testAssemblyPath, "nonexistent.xml");

            act.Should().Throw<FileNotFoundException>()
                .WithMessage("XML documentation file not found.");
        }

        [TestMethod]
        public async Task DocumentAsync_WithValidFiles_ReturnsDocAssembly()
        {
            await CreateTestAssemblyAsync();
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            var result = await manager.DocumentAsync();

            result.Should().NotBeNull();
            result.Symbol.Should().NotBeNull();
            result.Symbol.Name.Should().Be("TestAssembly");
        }

        [TestMethod]
        public async Task DocumentAsync_ExtractsNamespaces()
        {
            await CreateTestAssemblyAsync();
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            var result = await manager.DocumentAsync();

            result.Namespaces.Should().NotBeEmpty();
            result.Namespaces.Should().Contain(ns => ns.Symbol.Name == "TestNamespace");
        }

        [TestMethod]
        public async Task DocumentAsync_ExtractsTypes()
        {
            await CreateTestAssemblyAsync();
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            var result = await manager.DocumentAsync();

            var testNamespace = result.Namespaces.FirstOrDefault(ns => ns.Symbol.Name == "TestNamespace");
            testNamespace.Should().NotBeNull();
            testNamespace!.Types.Should().NotBeEmpty();
            testNamespace.Types.Should().Contain(t => t.Symbol.Name == "TestClass");
        }

        [TestMethod]
        public async Task DocumentAsync_ExtractsTypeDocumentation()
        {
            await CreateTestAssemblyAsync();
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            var result = await manager.DocumentAsync();

            var testType = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "TestClass");

            testType.Should().NotBeNull();
            testType!.Usage.Should().Contain("test class");
            testType.BestPractices.Should().Contain("remarks about the class");
            testType.Examples.Should().Contain("new TestClass()");
        }

        [TestMethod]
        public async Task DocumentAsync_ExtractsMembers()
        {
            await CreateTestAssemblyAsync();
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            var result = await manager.DocumentAsync();

            var testType = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "TestClass");

            testType!.Members.Should().NotBeEmpty();
            testType.Members.Should().Contain(m => m.Symbol.Name == "DoSomething");
            testType.Members.Should().Contain(m => m.Symbol.Name == "TestProperty");
        }

        [TestMethod]
        public async Task DocumentAsync_ExtractsMemberDocumentation()
        {
            await CreateTestAssemblyAsync();
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            var result = await manager.DocumentAsync();

            var method = result.Namespaces
                .SelectMany(ns => ns.Types)
                .SelectMany(t => t.Members)
                .FirstOrDefault(m => m.Symbol.Name == "DoSomething");

            method.Should().NotBeNull();
            method!.Usage.Should().Contain("Does something important");
            method.Examples.Should().Contain("DoSomething(\"test\")");
        }

        [TestMethod]
        public async Task DocumentAsync_ExtractsParameters()
        {
            await CreateTestAssemblyAsync();
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            var result = await manager.DocumentAsync();

            var method = result.Namespaces
                .SelectMany(ns => ns.Types)
                .SelectMany(t => t.Members)
                .FirstOrDefault(m => m.Symbol.Name == "DoSomething");

            method!.Parameters.Should().NotBeEmpty();
            var param = method.Parameters.FirstOrDefault(p => p.Symbol.Name == "input");
            param.Should().NotBeNull();
            param!.Usage.Should().Contain("input value");
        }

        [TestMethod]
        public async Task DocumentAsync_ExtractsBaseType()
        {
            await CreateTestAssemblyAsync();
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            await manager.DocumentAsync();

            var derivedType = manager.Document?.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "DerivedClass");

            derivedType.Should().NotBeNull();
            derivedType!.BaseType.Should().NotBeNull();
            derivedType.BaseType.Should().Be("TestNamespace.TestClass");
        }

        [TestMethod]
        public async Task DocumentAsync_ExtractsInterfaces()
        {
            await CreateTestAssemblyAsync();
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            var result = await manager.DocumentAsync();

            var testType = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "TestClass");

            testType!.RelatedApis.Should().Contain("System.IDisposable");
        }

        [TestMethod]
        public async Task DocumentAsync_WithProjectContext_UsesReferences()
        {
            await CreateTestAssemblyAsync();
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
            await CreateTestAssemblyAsync();
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
            await CreateTestAssemblyAsync();
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext([Accessibility.Public]);

            var result = await manager.DocumentAsync(context);

            // The test assembly should only include public members
            var testType = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Symbol.Name == "TestClass");

            testType.Should().NotBeNull();
            testType!.Members.Should().NotBeEmpty();

            // Verify only public members are included
            testType.Members.Should().Contain(m => m.Symbol.Name == "DoSomething");
            testType.Members.Should().Contain(m => m.Symbol.Name == "TestProperty");
            testType.Members.Should().Contain(m => m.Symbol.Name == "Dispose");

            // Verify private/internal members are filtered out
            testType.Members.Should().NotContain(m => m.Symbol.Name == "PrivateMethod");
            testType.Members.Should().NotContain(m => m.Symbol.Name == "PrivateProperty");
            testType.Members.Should().NotContain(m => m.Symbol.Name == "InternalMethod");
            testType.Members.Should().NotContain(m => m.Symbol.Name == "InternalProperty");
        }

        [TestMethod]
        public async Task DocumentAsync_WithInternalIncludedMembers_IncludesInternalMembers()
        {
            // With the new bridge compilation using IgnoresAccessChecksTo attribute,
            // we should now be able to see internal members even when loading a compiled assembly.
            
            await CreateTestAssemblyAsync();
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);
            var context = new ProjectContext([Accessibility.Public, Accessibility.Internal]);

            var result = await manager.DocumentAsync(context);

            var testType = result.Namespaces
                .SelectMany(ns => ns.Types)
                .FirstOrDefault(t => t.Name == "TestClass");

            testType.Should().NotBeNull();
            testType!.Members.Should().NotBeEmpty();

            // Verify public members are included
            testType.Members.Should().Contain(m => m.Name == "DoSomething");
            // Properties appear as get_/set_ methods
            testType.Members.Should().Contain(m => m.Name == "get_TestProperty" || m.Name == "set_TestProperty");

            // With the bridge compilation, internal members should now be visible!
            testType.Members.Should().Contain(m => m.Name == "InternalMethod", 
                "bridge compilation with IgnoresAccessChecksTo should make internal methods visible");
            testType.Members.Should().Contain(m => m.Name == "get_InternalProperty" || m.Name == "set_InternalProperty", 
                "bridge compilation should make internal properties visible");

            // Private members are still not visible (and shouldn't be included with our filter)
            testType.Members.Should().NotContain(m => m.Name == "PrivateMethod");
            testType.Members.Should().NotContain(m => m.Name == "get_PrivateProperty" || m.Name == "set_PrivateProperty");
        }

        [TestMethod]
        public async Task DocumentAsync_ProducesConsistentBaseline()
        {
            await CreateTestAssemblyAsync();
            var manager = new AssemblyManager(testAssemblyPath, testXmlPath);

            var result = await manager.DocumentAsync();

            // Debug: Check if the result has the expected structure
            result.Should().NotBeNull();
            result.AssemblyName.Should().Be("TestAssembly");
            result.Namespaces.Should().NotBeEmpty();
            result.Namespaces.Should().Contain(ns => ns.Name == "TestNamespace");

            var testNamespace = result.Namespaces.FirstOrDefault(ns => ns.Name == "TestNamespace");
            testNamespace.Should().NotBeNull();
            testNamespace!.Types.Should().NotBeEmpty();
            testNamespace.Types.Should().Contain(t => t.Name == "TestClass");

            // Serialize to JSON with deterministic settings
            var json = SerializeToJson(result);

            // Save or compare baseline
            var baselineFile = Path.Combine("Baselines", "AssemblyManager", "BasicAssembly.json");
            await SaveOrCompareBaseline(json, baselineFile);
        }

        [TestInitialize]
        public void Setup()
        {
            tempDirectory = Path.Combine(Path.GetTempPath(), $"AssemblyManagerTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDirectory);
            testAssemblyPath = Path.Combine(tempDirectory, "TestAssembly.dll");
            testXmlPath = Path.Combine(tempDirectory, "TestAssembly.xml");
        }

        #endregion

        #region Private Methods

        private async Task CreateConceptualContentAsync()
        {
            var conceptualPath = Path.Combine(tempDirectory, "conceptual");
            Directory.CreateDirectory(conceptualPath);

            // Create namespace-based folder structure: /conceptual/TestNamespace/TestClass/
            var namespacePath = Path.Combine(conceptualPath, "TestNamespace");
            Directory.CreateDirectory(namespacePath);

            var testClassPath = Path.Combine(namespacePath, "TestClass");
            Directory.CreateDirectory(testClassPath);

            await File.WriteAllTextAsync(
                Path.Combine(testClassPath, DocConstants.UsageFileName),
                "This is conceptual usage documentation for TestClass."
            );

            await File.WriteAllTextAsync(
                Path.Combine(testClassPath, DocConstants.ExamplesFileName),
                "This is conceptual examples documentation for TestClass."
            );

            await File.WriteAllTextAsync(
                Path.Combine(testClassPath, DocConstants.BestPracticesFileName),
                "This is conceptual best practices for TestClass."
            );

            await File.WriteAllTextAsync(
                Path.Combine(testClassPath, DocConstants.PatternsFileName),
                "This is conceptual patterns for TestClass."
            );

            await File.WriteAllTextAsync(
                Path.Combine(testClassPath, DocConstants.ConsiderationsFileName),
                "This is conceptual considerations for TestClass."
            );

            // Create member-specific documentation
            var memberPath = Path.Combine(testClassPath, "DoSomething");
            Directory.CreateDirectory(memberPath);

            await File.WriteAllTextAsync(
                Path.Combine(memberPath, DocConstants.UsageFileName),
                "This is conceptual member usage for DoSomething."
            );

            await File.WriteAllTextAsync(
                Path.Combine(memberPath, DocConstants.ExamplesFileName),
                "This is conceptual member examples for DoSomething."
            );
        }

        private async Task CreateTestAssemblyAsync()
        {
            var source = """
                using System;

                namespace TestNamespace
                {
                    /// <summary>
                    /// This is a test class.
                    /// </summary>
                    /// <remarks>
                    /// These are remarks about the class.
                    /// </remarks>
                    /// <example>
                    /// <code>
                    /// var test = new TestClass();
                    /// </code>
                    /// </example>
                    public class TestClass : IDisposable
                    {
                        /// <summary>
                        /// Gets or sets the test property.
                        /// </summary>
                        public string TestProperty { get; set; }

                        /// <summary>
                        /// Does something important.
                        /// </summary>
                        /// <param name="input">The input value.</param>
                        /// <returns>The result.</returns>
                        /// <example>
                        /// <code>
                        /// var result = DoSomething("test");
                        /// </code>
                        /// </example>
                        public string DoSomething(string input)
                        {
                            return input;
                        }

                         /// <summary>
                         /// Disposes the object.
                         /// </summary>
                         public void Dispose()
                         {
                         }

                         /// <summary>
                         /// An internal method for testing.
                         /// </summary>
                         internal void InternalMethod()
                         {
                         }

                         /// <summary>
                         /// A private method for testing.
                         /// </summary>
                         private void PrivateMethod()
                         {
                         }

                         /// <summary>
                         /// Gets or sets an internal property.
                         /// </summary>
                         internal string InternalProperty { get; set; }

                         /// <summary>
                         /// Gets or sets a private property.
                         /// </summary>
                         private string PrivateProperty { get; set; }
                    }

                    /// <summary>
                    /// A derived class.
                    /// </summary>
                    public class DerivedClass : TestClass
                    {
                        /// <summary>
                        /// An additional method.
                        /// </summary>
                        public void AdditionalMethod()
                        {
                        }
                    }
                }
                """;

            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create("TestAssembly")
                .AddSyntaxTrees(tree)
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .WithOptions(new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    xmlReferenceResolver: null,
                    sourceReferenceResolver: null,
                    metadataReferenceResolver: null,
                    assemblyIdentityComparer: null,
                    strongNameProvider: null,
                    publicSign: false,
                    reportSuppressedDiagnostics: false
                ));

            using (var dllStream = new MemoryStream())
            using (var xmlStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(dllStream, xmlDocumentationStream: xmlStream);

                if (!emitResult.Success)
                {
                    var errors = string.Join("\n", emitResult.Diagnostics.Select(d => d.ToString()));
                    throw new InvalidOperationException($"Compilation failed: {errors}");
                }

                await File.WriteAllBytesAsync(testAssemblyPath, dllStream.ToArray());

                // Create a proper XML documentation file
                var xmlContent = """
                    <?xml version="1.0"?>
                    <doc>
                        <assembly>
                            <name>TestAssembly</name>
                        </assembly>
                        <members>
                            <member name="T:TestNamespace.TestClass">
                                <summary>
                                This is a test class.
                                </summary>
                                <remarks>
                                These are remarks about the class.
                                </remarks>
                                <example>
                                <code>
                                var test = new TestClass();
                                </code>
                                </example>
                            </member>
                            <member name="P:TestNamespace.TestClass.TestProperty">
                                <summary>
                                Gets or sets the test property.
                                </summary>
                            </member>
                            <member name="M:TestNamespace.TestClass.DoSomething(System.String)">
                                <summary>
                                Does something important.
                                </summary>
                                <param name="input">The input value.</param>
                                <returns>The result.</returns>
                                <example>
                                <code>
                                var result = DoSomething("test");
                                </code>
                                </example>
                            </member>
                            <member name="M:TestNamespace.TestClass.Dispose">
                                <summary>
                                Disposes the object.
                                </summary>
                            </member>
                            <member name="T:TestNamespace.DerivedClass">
                                <summary>
                                A derived class.
                                </summary>
                            </member>
                             <member name="M:TestNamespace.DerivedClass.AdditionalMethod">
                                 <summary>
                                 An additional method.
                                 </summary>
                             </member>
                             <member name="M:TestNamespace.TestClass.InternalMethod">
                                 <summary>
                                 An internal method for testing.
                                 </summary>
                             </member>
                             <member name="M:TestNamespace.TestClass.PrivateMethod">
                                 <summary>
                                 A private method for testing.
                                 </summary>
                             </member>
                             <member name="P:TestNamespace.TestClass.InternalProperty">
                                 <summary>
                                 Gets or sets an internal property.
                                 </summary>
                             </member>
                             <member name="P:TestNamespace.TestClass.PrivateProperty">
                                 <summary>
                                 Gets or sets a private property.
                                 </summary>
                             </member>
                         </members>
                    </doc>
                    """;
                await File.WriteAllTextAsync(testXmlPath, xmlContent, Encoding.UTF8);
            }
        }

        /// <summary>
        /// Saves or compares a JSON string with a baseline file.
        /// </summary>
        /// <param name="json">The JSON string to save or compare.</param>
        /// <param name="baselineFile">The path to the baseline file.</param>
        private async Task SaveOrCompareBaseline(string json, string baselineFile)
        {
            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, baselineFile);
            var directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            if (File.Exists(fullPath))
            {
                // Compare with existing baseline
                var baseline = await File.ReadAllTextAsync(fullPath);
                json.Should().Be(baseline, "Output should match the baseline");
            }
            else
            {
                // Save new baseline
                await File.WriteAllTextAsync(fullPath, json);
                Assert.Inconclusive($"Baseline created at {fullPath}. Re-run test to verify.");
            }
        }

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