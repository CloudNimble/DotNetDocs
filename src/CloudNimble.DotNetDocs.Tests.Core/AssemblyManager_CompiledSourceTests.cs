using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Tests <see cref="AssemblyManager"/> against assemblies compiled from source for the test, covering shapes the shared test assembly does not contain.
    /// </summary>
    [TestClass]
    public class AssemblyManager_CompiledSourceTests : DotNetDocsTestBase
    {

        #region Test Classes

        private sealed class ObservableAssemblyManager(string assemblyPath, string xmlPath) : AssemblyManager(assemblyPath, xmlPath)
        {
            public void ReleaseUnmanagedOnly() => Dispose(false);
        }

        #endregion

        #region Fields

        private string _tempDirectory = string.Empty;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"CompiledSourceTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDirectory);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        #endregion

        #region Event Tests

        [TestMethod]
        public async Task DocumentAsync_DocumentsEvents()
        {
            var (assemblyPath, xmlPath) = EmitAssembly("Events", """
                using System;

                namespace Coverage.Events
                {
                    /// <summary>A publisher.</summary>
                    public class Publisher
                    {
                        /// <summary>Raised when the value changes.</summary>
                        /// <remarks>Raised after the new value is stored.</remarks>
                        /// <example>publisher.Changed += OnChanged;</example>
                        /// <seealso cref="Publisher"/>
                        public event EventHandler? Changed;

                        public event EventHandler<string>? Undocumented;

                        /// <summary>Raises both events.</summary>
                        public void Raise()
                        {
                            Changed?.Invoke(this, EventArgs.Empty);
                            Undocumented?.Invoke(this, string.Empty);
                        }
                    }
                }
                """);
            using var manager = new AssemblyManager(assemblyPath, xmlPath);

            var assembly = await manager.DocumentAsync(new ProjectContext());

            var publisher = assembly.Namespaces.SelectMany(n => n.Types).Single(t => t.Name == "Publisher");
            var changed = publisher.Members.Single(m => m.Name == "Changed");
            changed.MemberKind.Should().Be(SymbolKind.Event);
            changed.Accessibility.Should().Be(Accessibility.Public);
            changed.ReturnTypeName.Should().Be("System.EventHandler?");
            changed.Signature.Should().Be("public event System.EventHandler Changed");
            changed.Summary.Should().Be("Raised when the value changes.");
            changed.Remarks.Should().Be("Raised after the new value is stored.");
            changed.Examples.Should().Be("publisher.Changed += OnChanged;");
            changed.SeeAlso.Should().ContainSingle().Which.RawReference.Should().Be("T:Coverage.Events.Publisher");
            changed.IsInherited.Should().BeFalse();
            changed.DeclaringTypeName.Should().Be("Coverage.Events.Publisher");

            var undocumented = publisher.Members.Single(m => m.Name == "Undocumented");
            undocumented.MemberKind.Should().Be(SymbolKind.Event);
            undocumented.ReturnTypeName.Should().Be("System.EventHandler<string>?");
            undocumented.Summary.Should().BeNull();
            undocumented.Remarks.Should().BeNull();
            undocumented.Examples.Should().BeNull();
        }

        [TestMethod]
        public async Task DocumentAsync_MarksEventsFromBaseClassAsInherited()
        {
            var (assemblyPath, xmlPath) = EmitAssembly("InheritedEvents", """
                using System;

                namespace Coverage.Events
                {
                    /// <summary>The base publisher.</summary>
                    public class BasePublisher
                    {
                        /// <summary>Raised when the base changes.</summary>
                        public event EventHandler? BaseChanged;

                        /// <summary>Raises the event.</summary>
                        protected void OnBaseChanged() => BaseChanged?.Invoke(this, EventArgs.Empty);
                    }

                    /// <summary>A derived publisher.</summary>
                    public class DerivedPublisher : BasePublisher
                    {
                    }
                }
                """);
            using var manager = new AssemblyManager(assemblyPath, xmlPath);

            var assembly = await manager.DocumentAsync(new ProjectContext());

            var derived = assembly.Namespaces.SelectMany(n => n.Types).Single(t => t.Name == "DerivedPublisher");
            var inherited = derived.Members.Single(m => m.Name == "BaseChanged");
            inherited.IsInherited.Should().BeTrue();
            inherited.DeclaringTypeName.Should().Be("Coverage.Events.BasePublisher");
            inherited.Summary.Should().Be("Raised when the base changes.");
        }

        #endregion

        #region Extension Method Tests

        [TestMethod]
        public async Task DocumentAsync_WithoutExtensionMethods_AddsNoExternalTypes()
        {
            var (assemblyPath, xmlPath) = EmitAssembly("NoExtensions", """
                namespace Coverage.Plain
                {
                    /// <summary>A static helper with no extension methods.</summary>
                    public static class Helper
                    {
                        /// <summary>Doubles a value.</summary>
                        public static int Double(int value) => value * 2;
                    }
                }
                """);
            using var manager = new AssemblyManager(assemblyPath, xmlPath);

            var assembly = await manager.DocumentAsync(new ProjectContext());

            assembly.Namespaces.Select(n => n.Name).Should().Equal("Coverage.Plain");
            assembly.Namespaces.Single().Types.Select(t => t.Name).Should().Equal("Helper");
            assembly.Namespaces.Single().Types.Single().Members.Should().ContainSingle(m => m.Name == "Double" && !m.IsExtensionMethod);
        }

        [TestMethod]
        public async Task DocumentAsync_WithExtensionsOnArraysAndTypeParameters_KeepsThemOnTheirStaticClass()
        {
            var (assemblyPath, xmlPath) = EmitAssembly("ArrayExtensions", """
                namespace Coverage.Extensions
                {
                    /// <summary>Extensions on types that have no page of their own.</summary>
                    public static class ShapeExtensions
                    {
                        /// <summary>Totals the values.</summary>
                        public static int Total(this int[] values) => values.Length;

                        /// <summary>Wraps any value.</summary>
                        public static T[] Wrap<T>(this T value) => [value];

                        /// <summary>Shouts the text.</summary>
                        public static string Shout(this string text) => text.ToUpperInvariant();
                    }
                }
                """);
            using var manager = new AssemblyManager(assemblyPath, xmlPath);

            var assembly = await manager.DocumentAsync();

            var types = assembly.Namespaces.SelectMany(n => n.Types).ToList();
            types.Single(t => t.Name == "ShapeExtensions").Members.Where(m => m.IsExtensionMethod).Select(m => m.Name)
                .Should().BeEquivalentTo(["Total", "Wrap"], "an array or type parameter has no page to move the method to");

            var stringType = types.Should().ContainSingle(t => t.IsExternalReference).Subject;
            stringType.FullName.Should().Be("string");
            stringType.AssemblyName.Should().Be("System.Private.CoreLib");
            stringType.Summary.Should().Be("This type is defined in System.Private.CoreLib.");
            stringType.Remarks.Should().Be("See <see href=\"https://learn.microsoft.com/dotnet/api/system.string\">Microsoft documentation</see> for more information about the rest of the API.");
            stringType.Members.Select(m => m.Name).Should().Equal("Shout");
            stringType.IncludedMembers.Should().Equal(Accessibility.Public);
        }

        [TestMethod]
        public async Task DocumentAsync_ExtensionOnThirdPartyLookalikeAssembly_GetsNoLearnLink()
        {
            var (libraryPath, _) = EmitAssembly("SystemsCorp.Tools", """
                namespace SystemsCorp.Tools
                {
                    /// <summary>A third-party type.</summary>
                    public class Gadget
                    {
                    }
                }
                """);
            var (assemblyPath, xmlPath) = EmitAssembly("GadgetExtensions", """
                namespace Coverage.Extensions
                {
                    /// <summary>Extensions on a third-party type.</summary>
                    public static class GadgetExtensions
                    {
                        /// <summary>Starts the gadget.</summary>
                        public static void Start(this SystemsCorp.Tools.Gadget gadget)
                        {
                        }
                    }
                }
                """, libraryPath);
            using var manager = new AssemblyManager(assemblyPath, xmlPath);

            var assembly = await manager.DocumentAsync(new ProjectContext { References = [libraryPath] });

            var gadget = assembly.Namespaces.SelectMany(n => n.Types).Single(t => t.IsExternalReference);
            gadget.FullName.Should().Be("SystemsCorp.Tools.Gadget");
            gadget.Summary.Should().Be("This type is defined in SystemsCorp.Tools.");
            gadget.Remarks.Should().BeNull("SystemsCorp.Tools is not a Microsoft assembly, so there is no Microsoft Learn page for it");
        }

        [TestMethod]
        public async Task DocumentAsync_ExtensionOnTypeFromMissingReference_NamesTheMissingAssembly()
        {
            var (libraryPath, _) = EmitAssembly("Missing.Library", """
                namespace Missing.Library
                {
                    /// <summary>A type whose assembly is not passed to the documentation run.</summary>
                    public class Part
                    {
                    }
                }
                """);
            var (assemblyPath, xmlPath) = EmitAssembly("PartExtensions", """
                namespace Coverage.Extensions
                {
                    /// <summary>Extensions on a type from another assembly.</summary>
                    public static class PartExtensions
                    {
                        /// <summary>Inspects the part.</summary>
                        public static void Inspect(this Missing.Library.Part part)
                        {
                        }
                    }
                }
                """, libraryPath);
            using var manager = new AssemblyManager(assemblyPath, xmlPath);

            var assembly = await manager.DocumentAsync();

            var part = assembly.Namespaces.SelectMany(n => n.Types).Single(t => t.IsExternalReference);
            part.TypeKind.Should().Be(TypeKind.Error, "the reference was not passed in, so Roslyn cannot resolve the type");
            part.FullName.Should().Be("Missing.Library.Part");
            part.AssemblyName.Should().Be("Missing.Library");
            part.Summary.Should().Be("This type is defined in Missing.Library.");
            part.Remarks.Should().BeNull();
            part.Members.Select(m => m.Name).Should().Equal("Inspect");
            assembly.Namespaces.Single(n => n.Name == "Missing.Library").IncludedMembers.Should().Equal(Accessibility.Public);
        }

        #endregion

        #region Shape Tests

        [TestMethod]
        public async Task DocumentAsync_ReturnTypesWithoutAssembly_HaveNoAssemblyName()
        {
            var (assemblyPath, xmlPath) = EmitAssembly("ReturnShapes", """
                namespace Coverage.Shapes
                {
                    /// <summary>Returns values of several shapes.</summary>
                    public class Factory
                    {
                        /// <summary>Returns an array.</summary>
                        public int[] Numbers() => [];

                        /// <summary>Returns a type parameter.</summary>
                        public T Echo<T>(T value) => value;

                        /// <summary>Returns a named type.</summary>
                        public string Name() => string.Empty;
                    }
                }
                """);
            using var manager = new AssemblyManager(assemblyPath, xmlPath);

            var assembly = await manager.DocumentAsync(new ProjectContext());

            var factory = assembly.Namespaces.SelectMany(n => n.Types).Single(t => t.Name == "Factory");
            factory.Members.Single(m => m.Name == "Numbers").ReturnType!.AssemblyName.Should().BeNull();
            factory.Members.Single(m => m.Name == "Echo").ReturnType!.AssemblyName.Should().Be("ReturnShapes", "a type parameter belongs to the assembly that declares it");
            factory.Members.Single(m => m.Name == "Name").ReturnType!.AssemblyName.Should().Be("System.Private.CoreLib");
        }

        [TestMethod]
        public async Task DocumentAsync_Record_SkipsCompilerGeneratedMembers()
        {
            var (assemblyPath, xmlPath) = EmitAssembly("Records", """
                namespace Coverage.Records
                {
                    /// <summary>A package reference.</summary>
                    /// <param name="Id">The package ID.</param>
                    /// <param name="Version">The package version.</param>
                    public sealed record Package(string Id, string Version);

                    /// <summary>A point.</summary>
                    public record struct Point(int X, int Y);
                }
                """);
            using var manager = new AssemblyManager(assemblyPath, xmlPath);

            var assembly = await manager.DocumentAsync(new ProjectContext([Accessibility.Public, Accessibility.Protected, Accessibility.Internal, Accessibility.Private]));

            var memberNames = assembly.Namespaces.SelectMany(n => n.Types).SelectMany(t => t.Members).Select(m => m.Name).ToList();
            memberNames.Should().NotContain(name => name.Contains('<') || name.Contains('>'),"the compiler's own members, such as <Clone>$ and property backing fields, can't be called by name");
            var package = assembly.Namespaces.SelectMany(n => n.Types).Single(t => t.Name == "Package");
            package.Members.Should().Contain(m => m.Name == "Id");
            package.Members.Should().Contain(m => m.Name == "Version");
            package.Members.Should().Contain(m => m.Name == "Deconstruct");
            package.Members.Should().Contain(m => m.Name == "EqualityContract");
            package.Members.Should().Contain(m => m.Name == ".ctor");
            package.Members.Should().Contain(m => m.Name == "op_Equality");
        }

        [TestMethod]
        public async Task DocumentAsync_WithFileThatIsNotAnAssembly_Throws()
        {
            var assemblyPath = Path.Combine(_tempDirectory, "Corrupt.dll");
            var xmlPath = Path.Combine(_tempDirectory, "Corrupt.xml");
            File.WriteAllText(assemblyPath, "This is not an assembly.");
            File.WriteAllText(xmlPath, "<doc><members/></doc>");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);

            var act = () => manager.DocumentAsync(new ProjectContext());

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Could not get assembly symbol from compilation.");
        }

        [TestMethod]
        public void ShouldDocumentType_ReturnsFalse_ForImplicitlyDeclaredType()
        {
            var compilation = CSharpCompilation.CreateScriptCompilation(
                "Script",
                CSharpSyntaxTree.ParseText("var answer = 42;", new CSharpParseOptions(kind: SourceCodeKind.Script)),
                AssemblyManager.GetFrameworkReferencePaths().Select(path => MetadataReference.CreateFromFile(path)));
            var scriptClass = compilation.ScriptClass!;

            scriptClass.IsImplicitlyDeclared.Should().BeTrue();
            AssemblyManager.ShouldDocumentType(scriptClass).Should().BeFalse();
        }

        [TestMethod]
        public void BuildDocType_ForTypeWithoutAssembly_LeavesAssemblyNameEmpty()
        {
            var compilation = CreateCompilation();
            var arrayType = compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Int32));
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            using var manager = new AssemblyManager(assemblyPath, Path.ChangeExtension(assemblyPath, ".xml"));

            var docType = manager.BuildDocType(arrayType, compilation, [], [Accessibility.Public], null);

            docType.Name.Should().BeEmpty();
            docType.FullName.Should().Be("int[]");
            docType.AssemblyName.Should().BeNull();
            docType.TypeKind.Should().Be(TypeKind.Array);
        }

        #endregion

        #region GetFriendlyTypeName Tests

        [TestMethod]
        [DataRow(SpecialType.System_Boolean, "bool")]
        [DataRow(SpecialType.System_Byte, "byte")]
        [DataRow(SpecialType.System_SByte, "sbyte")]
        [DataRow(SpecialType.System_Char, "char")]
        [DataRow(SpecialType.System_Decimal, "decimal")]
        [DataRow(SpecialType.System_Double, "double")]
        [DataRow(SpecialType.System_Single, "float")]
        [DataRow(SpecialType.System_Int32, "int")]
        [DataRow(SpecialType.System_UInt32, "uint")]
        [DataRow(SpecialType.System_Int64, "long")]
        [DataRow(SpecialType.System_UInt64, "ulong")]
        [DataRow(SpecialType.System_Int16, "short")]
        [DataRow(SpecialType.System_UInt16, "ushort")]
        [DataRow(SpecialType.System_Object, "object")]
        [DataRow(SpecialType.System_String, "string")]
        [DataRow(SpecialType.System_Void, "void")]
        [DataRow(SpecialType.System_DateTime, "System.DateTime")]
        [DataRow(SpecialType.System_IntPtr, "nint")]
        public void GetFriendlyTypeName_ReturnsKeywordForBuiltInTypes(SpecialType specialType, string expected)
        {
            var compilation = CreateCompilation();

            AssemblyManager.GetFriendlyTypeName(compilation.GetSpecialType(specialType)).Should().Be(expected);
        }

        [TestMethod]
        public void GetFriendlyTypeName_ReturnsDisplayNameForOtherTypes()
        {
            var compilation = CreateCompilation();
            var listOfInt = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")!
                .Construct(compilation.GetSpecialType(SpecialType.System_Int32));

            AssemblyManager.GetFriendlyTypeName(listOfInt).Should().Be("System.Collections.Generic.List<int>");
            AssemblyManager.GetFriendlyTypeName(compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Int32))).Should().Be("int[]");
        }

        #endregion

        #region GetLearnTypeName Tests

        [TestMethod]
        public void GetLearnTypeName_ReturnsNull_ForTypesWithoutDocumentationId()
        {
            var compilation = CreateCompilation();
            var intType = compilation.GetSpecialType(SpecialType.System_Int32);

            AssemblyManager.GetLearnTypeName(compilation.CreateArrayTypeSymbol(intType)).Should().BeNull();
            AssemblyManager.GetLearnTypeName(compilation.CreatePointerTypeSymbol(intType)).Should().BeNull();
        }

        [TestMethod]
        public void GetLearnTypeName_ReturnsNull_ForUnresolvedTypeWithoutName()
        {
            var compilation = CreateCompilation();

            AssemblyManager.GetLearnTypeName(compilation.CreateErrorTypeSymbol(null, string.Empty, 0)).Should().BeNull();
        }

        #endregion

        #region GetManagedAssemblyPaths Tests

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void GetManagedAssemblyPaths_ReturnsEmpty_WhenDirectoryIsUnknown(string? directory)
        {
            AssemblyManager.GetManagedAssemblyPaths(directory).Should().BeEmpty();
        }

        [TestMethod]
        public void GetManagedAssemblyPaths_ReturnsEmpty_WhenDirectoryDoesNotExist()
        {
            AssemblyManager.GetManagedAssemblyPaths(Path.Combine(_tempDirectory, "Missing")).Should().BeEmpty();
        }

        [TestMethod]
        public void GetManagedAssemblyPaths_ReturnsOnlyManagedDllsInPathOrder()
        {
            var managedSource = typeof(SampleClass).Assembly.Location;
            File.Copy(managedSource, Path.Combine(_tempDirectory, "B.Managed.dll"));
            File.Copy(managedSource, Path.Combine(_tempDirectory, "a.managed.dll"));
            File.WriteAllText(Path.Combine(_tempDirectory, "NotAnAssembly.dll"), "text");
            File.Copy(managedSource, Path.Combine(_tempDirectory, "Managed.exe"));

            var paths = AssemblyManager.GetManagedAssemblyPaths(_tempDirectory);

            paths.Select(Path.GetFileName).Should().Equal("a.managed.dll", "B.Managed.dll");
        }

        #endregion

        #region Dispose Tests

        [TestMethod]
        public async Task Dispose_ReleasesTheModel_AndCanBeCalledTwice()
        {
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var manager = new AssemblyManager(assemblyPath, Path.ChangeExtension(assemblyPath, ".xml"));
            await manager.DocumentAsync(new ProjectContext());
            manager.Document.Should().NotBeNull();

            manager.Dispose();
            manager.Dispose();

            manager.Document.Should().BeNull();
        }

        [TestMethod]
        public async Task Dispose_FromFinalizerPath_KeepsManagedState()
        {
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var manager = new ObservableAssemblyManager(assemblyPath, Path.ChangeExtension(assemblyPath, ".xml"));
            await manager.DocumentAsync(new ProjectContext());

            manager.ReleaseUnmanagedOnly();
            manager.Dispose();

            manager.Document.Should().NotBeNull("the finalizer path must not touch managed objects, and a later Dispose is a no-op");
        }

        #endregion

        #region Helper Methods

        private static CSharpCompilation CreateCompilation(string? source = null, string assemblyName = "Coverage", params string[] additionalReferences)
        {
            var references = AssemblyManager.GetFrameworkReferencePaths().Concat(additionalReferences).Select(path => MetadataReference.CreateFromFile(path));
            var syntaxTrees = source is null
                ? Array.Empty<SyntaxTree>()
                : [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose))];

            return CSharpCompilation.Create(
                assemblyName,
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        }

        private (string AssemblyPath, string XmlPath) EmitAssembly(string assemblyName, string source, params string[] additionalReferences)
        {
            var assemblyPath = Path.Combine(_tempDirectory, $"{assemblyName}.dll");
            var xmlPath = Path.Combine(_tempDirectory, $"{assemblyName}.xml");
            var compilation = CreateCompilation(source, assemblyName, additionalReferences);

            using (var peStream = File.Create(assemblyPath))
            using (var xmlStream = File.Create(xmlPath))
            {
                var result = compilation.Emit(peStream, xmlDocumentationStream: xmlStream);
                result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
            }

            return (assemblyPath, xmlPath);
        }

        #endregion

    }

}
