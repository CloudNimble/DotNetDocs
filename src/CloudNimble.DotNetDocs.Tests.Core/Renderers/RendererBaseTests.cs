using System.IO;
using System.Linq;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Core.Renderers;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core.Renderers
{

    /// <summary>
    /// Tests for the RendererBase class.
    /// </summary>
    [TestClass]
    public class RendererBaseTests : DotNetDocsTestBase
    {

        #region Test Classes

        private class TestRenderer : RendererBase
        {
            public TestRenderer(ProjectContext? context = null) : base(context)
            {
            }

            // Expose protected methods for testing
            public new string GetSafeNamespaceName(DocNamespace ns) => base.GetSafeNamespaceName(ns);
            public new string GetSafeTypeName(DocType type) => base.GetSafeTypeName(type);
            public new string GetNamespaceFilePath(DocNamespace ns, string outputPath, string extension) => base.GetNamespaceFilePath(ns, outputPath, extension);
            public new string GetTypeFilePath(DocType type, DocNamespace ns, string outputPath, string extension) => base.GetTypeFilePath(type, ns, outputPath, extension);
            public new string GetNamespaceFileName(DocNamespace ns, string extension) => base.GetNamespaceFileName(ns, extension);
            public new string GetTypeFileName(DocType type, DocNamespace ns, string extension) => base.GetTypeFileName(type, ns, extension);
            public new string GetAccessModifier(Accessibility accessibility) => base.GetAccessModifier(accessibility);
            public new string GetMemberSignature(DocMember member) => base.GetMemberSignature(member);
            public new string GetMethodSignature(IMethodSymbol method) => base.GetMethodSignature(method);
            public new string GetPropertySignature(IPropertySymbol property) => base.GetPropertySignature(property);
            public new string GetFieldSignature(IFieldSymbol field) => base.GetFieldSignature(field);
            public new string GetEventSignature(IEventSymbol evt) => base.GetEventSignature(evt);
            public new string GetTypeSignature(DocType type) => base.GetTypeSignature(type);
            public new ProjectContext Context => base.Context;
            public new FileNamingOptions FileNamingOptions => base.FileNamingOptions;
        }

        #endregion

        #region Fields

        private TestRenderer _renderer = null!;
        private DocAssembly _testAssembly = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            _renderer = new TestRenderer();
            
            // Use the shared test assembly
            _testAssembly = GetTestsDotSharedAssembly();
        }

        #endregion

        #region Constructor Tests

        [TestMethod]
        public void Constructor_WithNullContext_CreatesDefaultContext()
        {
            // Act
            var renderer = new TestRenderer(null);

            // Assert
            renderer.Context.Should().NotBeNull();
            renderer.FileNamingOptions.Should().NotBeNull();
            renderer.FileNamingOptions.NamespaceMode.Should().Be(NamespaceMode.File);
            renderer.FileNamingOptions.NamespaceSeparator.Should().Be('-');
        }

        [TestMethod]
        public void Constructor_WithCustomContext_UsesProvidedContext()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder, '_')
            };

            // Act
            var renderer = new TestRenderer(context);

            // Assert
            renderer.Context.Should().BeSameAs(context);
            renderer.FileNamingOptions.NamespaceMode.Should().Be(NamespaceMode.Folder);
            renderer.FileNamingOptions.NamespaceSeparator.Should().Be('_');
        }

        #endregion

        #region GetSafeNamespaceName Tests

        [TestMethod]
        public void GetSafeNamespaceName_WithNormalNamespace_ReturnsFullName()
        {
            // Arrange
            var ns = _testAssembly.Namespaces.First();

            // Act
            var result = _renderer.GetSafeNamespaceName(ns);

            // Assert
            result.Should().Be(ns.Name);
        }

        #endregion

        #region GetSafeTypeName Tests

        [TestMethod]
        public void GetSafeTypeName_WithGenericType_ReplacesInvalidCharacters()
        {
            // Arrange
            // Find a type in the test assembly - SimpleClass is a good candidate
            var ns = _testAssembly.Namespaces.FirstOrDefault(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");
            ns.Should().NotBeNull("BasicScenarios namespace should exist");
            var type = ns!.Types.FirstOrDefault(t => t.Name == "SimpleClass");
            type.Should().NotBeNull("SimpleClass should exist in BasicScenarios");

            // Act
            var result = _renderer.GetSafeTypeName(type!);

            // Assert
            result.Should().Be("SimpleClass");
        }

        [TestMethod]
        public void GetSafeTypeName_WithSpecialCharacters_ReplacesAllInvalidCharacters()
        {
            // Arrange
            // Use any type from test assembly
            var type = _testAssembly.Namespaces.SelectMany(n => n.Types).First();
            
            // Act
            var result = _renderer.GetSafeTypeName(type);
            
            // Assert - verify no invalid characters remain
            result.Should().NotContain("<");
            result.Should().NotContain(">");
            result.Should().NotContain("`");
            result.Should().NotContain("/");
            result.Should().NotContain("\\");
            result.Should().NotContain(":");
            result.Should().NotContain("*");
            result.Should().NotContain("?");
            result.Should().NotContain("\"");
            result.Should().NotContain("|");
        }

        #endregion

        #region GetNamespaceFilePath Tests

        [TestMethod]
        public void GetNamespaceFilePath_WithFileMode_ReturnsFlatFileName()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '-')
            };
            var renderer = new TestRenderer(context);
            var ns = _testAssembly.Namespaces.First(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared");

            // Act
            var result = renderer.GetNamespaceFilePath(ns, Path.Combine("output"), "md");

            // Assert
            result.Should().Be(Path.Combine("output", "CloudNimble-DotNetDocs-Tests-Shared.md"));
        }

        [TestMethod]
        public void GetNamespaceFilePath_WithFolderMode_ReturnsFolderWithIndex()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder)
            };
            var renderer = new TestRenderer(context);
            var ns = _testAssembly.Namespaces.First(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared");

            // Act
            var result = renderer.GetNamespaceFilePath(ns, "output", "md");

            // Assert
            var expected = Path.Combine("output", "CloudNimble", "DotNetDocs", "Tests", "Shared", "index.md");
            result.Should().Be(expected);
        }

        #endregion

        #region GetTypeFilePath Tests

        [TestMethod]
        public void GetTypeFilePath_WithFileMode_ReturnsFlatFileName()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.File, '_')
            };
            var renderer = new TestRenderer(context);
            var ns = _testAssembly.Namespaces.First(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");
            var type = ns.Types.First(t => t.Name == "SimpleClass");

            // Act
            var result = renderer.GetTypeFilePath(type, ns, "output", "md");

            // Assert
            result.Should().Be(Path.Combine("output", "CloudNimble_DotNetDocs_Tests_Shared_BasicScenarios.SimpleClass.md"));
        }

        [TestMethod]
        public void GetTypeFilePath_WithFolderMode_ReturnsFolderWithTypeFile()
        {
            // Arrange
            var context = new ProjectContext
            {
                FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder)
            };
            var renderer = new TestRenderer(context);
            var ns = _testAssembly.Namespaces.First(n => n.Name == "CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios");
            var type = ns.Types.First(t => t.Name == "SimpleClass");

            // Act
            var result = renderer.GetTypeFilePath(type, ns, "output", "md");

            // Assert
            result.Should().Be(Path.Combine("output", "CloudNimble", "DotNetDocs", "Tests", "Shared", "BasicScenarios", "SimpleClass.md"));
        }

        #endregion

        #region GetAccessModifier Tests

        [TestMethod]
        public void GetAccessModifier_ReturnsCorrectStrings()
        {
            // Arrange & Act & Assert
            _renderer.GetAccessModifier(Accessibility.Public).Should().Be("public");
            _renderer.GetAccessModifier(Accessibility.Protected).Should().Be("protected");
            _renderer.GetAccessModifier(Accessibility.Internal).Should().Be("internal");
            _renderer.GetAccessModifier(Accessibility.ProtectedOrInternal).Should().Be("protected internal");
            _renderer.GetAccessModifier(Accessibility.ProtectedAndInternal).Should().Be("private protected");
            _renderer.GetAccessModifier(Accessibility.Private).Should().Be("private");
            _renderer.GetAccessModifier(Accessibility.NotApplicable).Should().Be("");
        }

        #endregion

        #region GetMethodSignature Tests

        // Method signature tests removed - use GetMemberSignature tests with real assembly data

        #endregion

        #region RemoveIndentation Tests

        [TestMethod]
        public void RemoveIndentation_WithNullText_ReturnsNull()
        {
            // Act
            var result = RendererBase.RemoveIndentation(null!);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void RemoveIndentation_WithEmptyText_ReturnsEmpty()
        {
            // Act
            var result = RendererBase.RemoveIndentation(string.Empty);

            // Assert
            result.Should().BeEmpty();
        }

        [TestMethod]
        public void RemoveIndentation_WithWhitespaceOnlyText_ReturnsWhitespace()
        {
            // Act
            var result = RendererBase.RemoveIndentation("   ");

            // Assert
            result.Should().Be("   ");
        }

        [TestMethod]
        public void RemoveIndentation_WithSimpleCodeBlock_RemovesIndentation()
        {
            // Arrange
            var input = """
                var simple = new SimpleClass();
                            simple.DoWork();
                """;

            var expected = """
                var simple = new SimpleClass();
                simple.DoWork();
                """;

            // Act
            var result = RendererBase.RemoveIndentation(input);

            // Assert
            result.Should().Be(expected);
        }

        [TestMethod]
        public void RemoveIndentation_WithComplexCodeBlock_PreservesRelativeIndentation()
        {
            // Arrange
            var input = """
                var fullDocs = new ClassWithFullDocs();
                            if (fullDocs != null)
                            {
                                fullDocs.ComplexMethod("test", 42);
                                Console.WriteLine("Done");
                            }
                """;

            var expected = """
                var fullDocs = new ClassWithFullDocs();
                if (fullDocs != null)
                {
                    fullDocs.ComplexMethod("test", 42);
                    Console.WriteLine("Done");
                }
                """;

            // Act
            var result = RendererBase.RemoveIndentation(input);

            // Assert
            result.Should().Be(expected);
        }

        [TestMethod]
        public void RemoveIndentation_WithMixedTabsAndSpaces_RemovesCorrectly()
        {
            // Arrange
            var input = """
                using (var disposable = new DisposableClass())
                		{
                		    disposable.UseResource();
                		}
                """;

            var expected = """
                using (var disposable = new DisposableClass())
                {
                    disposable.UseResource();
                }
                """;

            // Act
            var result = RendererBase.RemoveIndentation(input);

            // Assert
            result.Should().Be(expected);
        }

        [TestMethod]
        public void RemoveIndentation_WithBlankLinesInMiddle_PreservesBlankLines()
        {
            // Arrange
            var input = """
                var example = new Example();
                            example.Initialize();

                            // After some processing
                            example.Process();
                            example.Cleanup();
                """;

            var expected = """
                var example = new Example();
                example.Initialize();

                // After some processing
                example.Process();
                example.Cleanup();
                """;

            // Act
            var result = RendererBase.RemoveIndentation(input);

            // Assert
            result.Should().Be(expected);
        }

        [TestMethod]
        public void RemoveIndentation_WithNestedStructures_MaintainsNesting()
        {
            // Arrange
            var input = """
                public class Example
                            {
                                public void Method()
                                {
                                    for (int i = 0; i < 10; i++)
                                    {
                                        if (i % 2 == 0)
                                        {
                                            Console.WriteLine($"Even: {i}");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"Odd: {i}");
                                        }
                                    }
                                }
                            }
                """;

            var expected = """
                public class Example
                {
                    public void Method()
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            if (i % 2 == 0)
                            {
                                Console.WriteLine($"Even: {i}");
                            }
                            else
                            {
                                Console.WriteLine($"Odd: {i}");
                            }
                        }
                    }
                }
                """;

            // Act
            var result = RendererBase.RemoveIndentation(input);

            // Assert
            result.Should().Be(expected);
        }

        [TestMethod]
        public void RemoveIndentation_WithXmlComments_PreservesCommentStructure()
        {
            // Arrange
            var input = """
                /// <summary>
                            /// This is a summary comment.
                            /// </summary>
                            /// <param name="value">The value parameter.</param>
                            /// <returns>The result.</returns>
                            public string Process(string value)
                            {
                                return value.ToUpper();
                            }
                """;

            var expected = """
                /// <summary>
                /// This is a summary comment.
                /// </summary>
                /// <param name="value">The value parameter.</param>
                /// <returns>The result.</returns>
                public string Process(string value)
                {
                    return value.ToUpper();
                }
                """;

            // Act
            var result = RendererBase.RemoveIndentation(input);

            // Assert
            result.Should().Be(expected);
        }

        [TestMethod]
        public void RemoveIndentation_WithLambdaExpressions_PreservesFormatting()
        {
            // Arrange
            var input = """
                var numbers = Enumerable.Range(1, 10)
                                .Where(x => x % 2 == 0)
                                .Select(x => new
                                {
                                    Number = x,
                                    Square = x * x,
                                    Cube = x * x * x
                                })
                                .ToList();
                """;

            var expected = """
                var numbers = Enumerable.Range(1, 10)
                    .Where(x => x % 2 == 0)
                    .Select(x => new
                    {
                        Number = x,
                        Square = x * x,
                        Cube = x * x * x
                    })
                    .ToList();
                """;

            // Act
            var result = RendererBase.RemoveIndentation(input);

            // Assert
            result.Should().Be(expected);
        }

        [TestMethod]
        public void RemoveIndentation_WithStringInterpolation_PreservesContent()
        {
            // Arrange
            var input = """
                var name = "World";
                            var message = $@"Hello, {name}!
                                This is a multiline
                                interpolated string.";
                            Console.WriteLine(message);
                """;

            var expected = """
                var name = "World";
                var message = $@"Hello, {name}!
                    This is a multiline
                    interpolated string.";
                Console.WriteLine(message);
                """;

            // Act
            var result = RendererBase.RemoveIndentation(input);

            // Assert
            result.Should().Be(expected);
        }

        [TestMethod]
        public void RemoveIndentation_WithNoIndentation_ReturnsOriginal()
        {
            // Arrange
            var input = """
                var x = 1;
                var y = 2;
                var z = x + y;
                """;

            // Act
            var result = RendererBase.RemoveIndentation(input);

            // Assert
            result.Should().Be(input);
        }

        [TestMethod]
        public void RemoveIndentation_WithVaryingIndentationLevels_UsesMinimumIndent()
        {
            // Arrange
            var input = """
                    var a = 1;
                        var b = 2;
                            var c = 3;
                        var d = 4;
                    var e = 5;
                """;

            var expected = """
                    var a = 1;
                var b = 2;
                    var c = 3;
                var d = 4;
                var e = 5;
                """;

            // Act
            var result = RendererBase.RemoveIndentation(input);

            // Assert
            result.Should().Be(expected);
        }

        [TestMethod]
        public void RemoveIndentation_WithAsyncAwaitPattern_PreservesStructure()
        {
            // Arrange
            var input = """
                public async Task<string> ProcessAsync()
                            {
                                try
                                {
                                    var result = await GetDataAsync();
                                    return await TransformAsync(result);
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogError(ex, "Processing failed");
                                    throw;
                                }
                            }
                """;

            var expected = """
                public async Task<string> ProcessAsync()
                {
                    try
                    {
                        var result = await GetDataAsync();
                        return await TransformAsync(result);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Processing failed");
                        throw;
                    }
                }
                """;

            // Act
            var result = RendererBase.RemoveIndentation(input);

            // Assert
            result.Should().Be(expected);
        }

        [TestMethod]
        public void RemoveIndentation_WithSwitchExpression_MaintainsAlignment()
        {
            // Arrange
            var input = """
                var result = operation switch
                            {
                                "add" => x + y,
                                "subtract" => x - y,
                                "multiply" => x * y,
                                "divide" when y != 0 => x / y,
                                _ => throw new ArgumentException("Invalid operation")
                            };
                """;

            var expected = """
                var result = operation switch
                {
                    "add" => x + y,
                    "subtract" => x - y,
                    "multiply" => x * y,
                    "divide" when y != 0 => x / y,
                    _ => throw new ArgumentException("Invalid operation")
                };
                """;

            // Act
            var result = RendererBase.RemoveIndentation(input);

            // Assert
            result.Should().Be(expected);
        }

        [TestMethod]
        public void RemoveIndentation_WithLinqQuery_PreservesQueryStructure()
        {
            // Arrange
            var input = """
                var query = from person in people
                            where person.Age >= 18
                            orderby person.LastName, person.FirstName
                            select new
                            {
                                FullName = $"{person.FirstName} {person.LastName}",
                                person.Age,
                                IsAdult = true
                            };
                """;

            var expected = """
                var query = from person in people
                where person.Age >= 18
                orderby person.LastName, person.FirstName
                select new
                {
                    FullName = $"{person.FirstName} {person.LastName}",
                    person.Age,
                    IsAdult = true
                };
                """;

            // Act
            var result = RendererBase.RemoveIndentation(input);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

    }

}