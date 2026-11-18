using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using CloudNimble.DotNetDocs.Tests.Shared.Enums;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Tests that <see cref="AssemblyManager"/> references the framework assemblies, so framework types and enums loaded from metadata resolve.
    /// </summary>
    [TestClass]
    public class AssemblyManager_FrameworkReferencesTests : DotNetDocsTestBase
    {

        #region Fields

        private string _tempDirectory = string.Empty;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"FrameworkReferencesTests_{Guid.NewGuid():N}");
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

        #region CreateCompilationAsync Tests

        [TestMethod]
        public async Task CreateCompilationAsync_ResolvesFrameworkTypesReferencedByTheTarget()
        {
            var assemblyPath = typeof(SimpleEnum).Assembly.Location;
            using var manager = new AssemblyManager(assemblyPath, Path.ChangeExtension(assemblyPath, ".xml"));

            var compilation = await manager.CreateCompilationAsync([]);

            compilation.GetTypeByMetadataName("System.Enum")!.TypeKind.Should().Be(TypeKind.Class);
            compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")!.TypeKind.Should().Be(TypeKind.Class);
            compilation.GetTypeByMetadataName(typeof(SimpleEnum).FullName!)!.TypeKind.Should().Be(TypeKind.Enum);
            compilation.GetDiagnostics().Should().NotContain(d => d.Id == "CS0009", "native libraries must not be referenced");
        }

        [TestMethod]
        public async Task CreateCompilationAsync_ProvidedReference_ReplacesRuntimeCopyWithTheSameName()
        {
            var runtimeCollections = AssemblyManager.GetFrameworkReferencePaths()
                .Single(path => Path.GetFileName(path).Equals("System.Collections.dll", StringComparison.OrdinalIgnoreCase));
            var providedCollections = Path.Combine(_tempDirectory, "System.Collections.dll");
            File.Copy(runtimeCollections, providedCollections);
            var assemblyPath = typeof(SimpleEnum).Assembly.Location;
            using var manager = new AssemblyManager(assemblyPath, Path.ChangeExtension(assemblyPath, ".xml"));

            var compilation = await manager.CreateCompilationAsync([providedCollections]);

            var collectionsReferences = compilation.References
                .OfType<PortableExecutableReference>()
                .Where(r => string.Equals(Path.GetFileName(r.FilePath), "System.Collections.dll", StringComparison.OrdinalIgnoreCase))
                .Select(r => r.FilePath)
                .ToList();
            collectionsReferences.Should().ContainSingle().Which.Should().Be(providedCollections);
        }

        [TestMethod]
        public async Task CreateCompilationAsync_SkipsMissingProvidedReference_AndKeepsRuntimeCopy()
        {
            var missingReference = Path.Combine(_tempDirectory, "System.Collections.dll");
            var assemblyPath = typeof(SimpleEnum).Assembly.Location;
            using var manager = new AssemblyManager(assemblyPath, Path.ChangeExtension(assemblyPath, ".xml"));

            var compilation = await manager.CreateCompilationAsync([missingReference]);

            compilation.References
                .OfType<PortableExecutableReference>()
                .Should().ContainSingle(r => string.Equals(Path.GetFileName(r.FilePath), "System.Collections.dll", StringComparison.OrdinalIgnoreCase))
                .Which.FilePath.Should().NotBe(missingReference);
        }

        [TestMethod]
        public async Task CreateCompilationAsync_DoesNotReferenceTheTargetAssemblyTwice()
        {
            var assemblyPath = typeof(SimpleEnum).Assembly.Location;
            using var manager = new AssemblyManager(assemblyPath, Path.ChangeExtension(assemblyPath, ".xml"));

            var compilation = await manager.CreateCompilationAsync([]);

            compilation.References
                .OfType<PortableExecutableReference>()
                .Should().ContainSingle(r => Path.GetFileName(r.FilePath) == Path.GetFileName(assemblyPath));
        }

        #endregion

        #region GetFrameworkReferencePaths Tests

        [TestMethod]
        public void GetFrameworkReferencePaths_ReturnsOnlyManagedAssembliesFromTheRuntimeDirectory()
        {
            var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);

            var paths = AssemblyManager.GetFrameworkReferencePaths();

            paths.Should().NotBeEmpty();
            paths.Should().OnlyContain(path => Path.GetDirectoryName(path) == runtimeDirectory);
            paths.Should().OnlyContain(path => AssemblyManager.IsManagedAssembly(path));
            paths.Select(Path.GetFileName).Should().Contain(["System.Runtime.dll", "System.Collections.dll", "System.Private.CoreLib.dll"]);
            paths.Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void GetFrameworkReferencePaths_ExcludesNativeLibraries()
        {
            var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            var nativeLibraries = Directory.EnumerateFiles(runtimeDirectory, "*.dll")
                .Where(path => !AssemblyManager.IsManagedAssembly(path))
                .ToList();

            var paths = AssemblyManager.GetFrameworkReferencePaths();

            paths.Should().NotIntersectWith(nativeLibraries);
            paths.Count.Should().Be(Directory.EnumerateFiles(runtimeDirectory, "*.dll").Count() - nativeLibraries.Count);
        }

        #endregion

        #region IsManagedAssembly Tests

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void IsManagedAssembly_ThrowsForMissingPath(string? path)
        {
            Action act = () => AssemblyManager.IsManagedAssembly(path!);

            act.Should().Throw<ArgumentException>().WithParameterName("path");
        }

        [TestMethod]
        public void IsManagedAssembly_ReturnsTrue_ForManagedAssembly()
        {
            AssemblyManager.IsManagedAssembly(typeof(SimpleEnum).Assembly.Location).Should().BeTrue();
        }

        [TestMethod]
        public void IsManagedAssembly_ReturnsFalse_ForMissingFile()
        {
            AssemblyManager.IsManagedAssembly(Path.Combine(_tempDirectory, "Missing.dll")).Should().BeFalse();
        }

        [TestMethod]
        public void IsManagedAssembly_ReturnsFalse_ForEmptyFile()
        {
            var path = Path.Combine(_tempDirectory, "Empty.dll");
            File.WriteAllBytes(path, []);

            AssemblyManager.IsManagedAssembly(path).Should().BeFalse();
        }

        [TestMethod]
        public void IsManagedAssembly_ReturnsFalse_ForFileThatIsNotAPortableExecutable()
        {
            var path = Path.Combine(_tempDirectory, "NotAnAssembly.dll");
            File.WriteAllText(path, "This is a text file with a .dll extension.");

            AssemblyManager.IsManagedAssembly(path).Should().BeFalse();
        }

        [TestMethod]
        public void IsManagedAssembly_ReturnsFalse_ForNativeLibrary()
        {
            var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            var nativeLibrary = new[] { "coreclr.dll", "libcoreclr.so", "libcoreclr.dylib" }
                .Select(name => Path.Combine(runtimeDirectory, name))
                .First(File.Exists);

            AssemblyManager.IsManagedAssembly(nativeLibrary).Should().BeFalse();
        }

        #endregion

        #region Enum Tests

        [TestMethod]
        [DataRow(nameof(SimpleEnum), "int", false)]
        [DataRow(nameof(ByteEnum), "byte", false)]
        [DataRow(nameof(LongEnum), "long", false)]
        [DataRow(nameof(FlagsEnum), "int", true)]
        public void DocumentAsync_ModelsMetadataEnumAsDocEnum(string enumName, string underlyingType, bool isFlags)
        {
            var assembly = GetTestsDotSharedAssembly();

            var type = assembly.Namespaces.SelectMany(n => n.Types).Single(t => t.Name == enumName);

            var docEnum = type.Should().BeOfType<DocEnum>().Subject;
            docEnum.TypeKind.Should().Be(TypeKind.Enum);
            docEnum.UnderlyingType!.DisplayName.Should().Be(underlyingType);
            docEnum.IsFlags.Should().Be(isFlags);
            docEnum.Members.Should().BeEmpty("enum values are modeled in Values, not as members");
            docEnum.BaseType.Should().Be("System.Enum");
        }

        [TestMethod]
        public void DocumentAsync_ModelsEnumValuesWithTheirConstantValues()
        {
            var assembly = GetTestsDotSharedAssembly();

            var simpleEnum = (DocEnum)assembly.Namespaces.SelectMany(n => n.Types).Single(t => t.Name == nameof(SimpleEnum));

            simpleEnum.Values.Select(v => (v.Name, v.NumericValue)).Should().Equal(
                ("None", "0"),
                ("First", "1"),
                ("Second", "2"),
                ("Third", "10"),
                ("Fourth", "11"));
        }

        [TestMethod]
        public void DocumentAsync_ModelsLongEnumValuesWithoutOverflow()
        {
            var assembly = GetTestsDotSharedAssembly();

            var longEnum = (DocEnum)assembly.Namespaces.SelectMany(n => n.Types).Single(t => t.Name == nameof(LongEnum));

            longEnum.Values.Single(v => v.Name == nameof(LongEnum.VeryLarge)).NumericValue.Should().Be(long.MaxValue.ToString());
        }

        #endregion

    }

}
