using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Core.Transformers;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core.Transformers
{

    /// <summary>
    /// Tests for the cross-reference resolution and URL building in <see cref="XmlTransformerBase"/>.
    /// </summary>
    /// <remarks>
    /// The base class is abstract, so the tests run through <see cref="MarkdownXmlTransformer"/>, which does not override any of the members under test.
    /// </remarks>
    [TestClass]
    public class XmlTransformerBaseTests : DotNetDocsTestBase
    {

        #region Fields

        private MarkdownXmlTransformer _resolver = null!;
        private ProjectContext _context = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            // Configure services for DI
            TestHostBuilder.ConfigureServices((context, services) =>
            {
                services.AddDotNetDocs(ctx =>
                {
                    ctx.FileNamingOptions.NamespaceMode = NamespaceMode.Folder;
                    ctx.FileNamingOptions.NamespaceSeparator = '-';
                });
            });

            TestSetup();
            _context = GetService<ProjectContext>();
            _resolver = new MarkdownXmlTransformer(_context);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestTearDown();
        }

        #endregion

        #region BuildReferenceMap Tests

        [TestMethod]
        public async Task BuildReferenceMap_IndexesAllEntities()
        {
            // Arrange
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();

            // Act
            _resolver.BuildReferenceMap(assembly);

            // Assert
            // Test that we can resolve a type reference that we know exists
            var typeRef = _resolver.ResolveReference("T:CloudNimble.DotNetDocs.Core.Configuration.NamespaceMode", "");
            typeRef.Should().NotBeNull();
            typeRef.IsResolved.Should().BeTrue();
            typeRef.DisplayName.Should().Be("NamespaceMode");
        }

        #endregion

        #region ResolveReference Tests

        [TestMethod]
        public async Task ResolveReference_WithEnumFieldPrefix_ResolvesCorrectly()
        {
            // Arrange
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();
            _resolver.BuildReferenceMap(assembly);

            // Act
            var reference = _resolver.ResolveReference("F:CloudNimble.DotNetDocs.Core.Configuration.NamespaceMode.File", "");

            // Assert
            reference.Should().NotBeNull();
            reference.IsResolved.Should().BeTrue();
            reference.DisplayName.Should().Be("NamespaceMode.File");
            reference.Anchor.Should().Be("file");
            reference.ReferenceType.Should().Be(ReferenceType.Field);
        }

        [TestMethod]
        public async Task ResolveReference_WithTypePrefix_ResolvesCorrectly()
        {
            // Arrange
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();
            _resolver.BuildReferenceMap(assembly);

            // Act
            var reference = _resolver.ResolveReference("T:CloudNimble.DotNetDocs.Core.Configuration.NamespaceMode", "");

            // Assert
            reference.Should().NotBeNull();
            reference.IsResolved.Should().BeTrue();
            reference.DisplayName.Should().Be("NamespaceMode");
            reference.Anchor.Should().BeNull();
            reference.ReferenceType.Should().Be(ReferenceType.Type);
        }

        [TestMethod]
        public void ResolveReference_WithExternalUrl_ResolvesAsExternal()
        {
            // Act
            var reference = _resolver.ResolveReference("https://learn.microsoft.com/dotnet", "");

            // Assert
            reference.Should().NotBeNull();
            reference.IsResolved.Should().BeTrue();
            reference.ReferenceType.Should().Be(ReferenceType.External);
            reference.RelativePath.Should().Be("https://learn.microsoft.com/dotnet");
            reference.DisplayName.Should().Be("link");
        }

        [TestMethod]
        public void ResolveReference_WithFrameworkType_ResolvesAsFramework()
        {
            // Act
            var reference = _resolver.ResolveReference("T:System.String", "");

            // Assert
            reference.Should().NotBeNull();
            reference.IsResolved.Should().BeTrue();
            reference.ReferenceType.Should().Be(ReferenceType.Framework);
            reference.DisplayName.Should().Be("String");
            reference.RelativePath.Should().StartWith("https://learn.microsoft.com/dotnet/api/");
        }

        [TestMethod]
        public void ResolveReference_WithUnknownType_ReturnsUnresolved()
        {
            // Act
            var reference = _resolver.ResolveReference("T:Some.Unknown.Type", "");

            // Assert
            reference.Should().NotBeNull();
            reference.IsResolved.Should().BeFalse();
            reference.DisplayName.Should().Be("Type");
        }

        #endregion

        #region ResolveReferences Tests

        [TestMethod]
        public async Task ResolveReferences_ResolvesMultipleReferences()
        {
            // Arrange
            var assemblyPath = typeof(NamespaceMode).Assembly.Location;
            var xmlPath = System.IO.Path.ChangeExtension(assemblyPath, ".xml");
            using var manager = new AssemblyManager(assemblyPath, xmlPath);
            var assembly = await manager.DocumentAsync();
            _resolver.BuildReferenceMap(assembly);

            var rawReferences = new List<string>
            {
                "T:CloudNimble.DotNetDocs.Core.Configuration.NamespaceMode",
                "F:CloudNimble.DotNetDocs.Core.Configuration.NamespaceMode.File",
                "T:System.String",
                "https://example.com"
            };

            // Act
            var resolved = _resolver.ResolveReferences(rawReferences, "");

            // Assert
            resolved.Should().NotBeNull();
            resolved.Should().HaveCount(4);

            resolved.ElementAt(0).IsResolved.Should().BeTrue();
            resolved.ElementAt(0).ReferenceType.Should().Be(ReferenceType.Type);

            resolved.ElementAt(1).IsResolved.Should().BeTrue();
            resolved.ElementAt(1).ReferenceType.Should().Be(ReferenceType.Field);

            resolved.ElementAt(2).IsResolved.Should().BeTrue();
            resolved.ElementAt(2).ReferenceType.Should().Be(ReferenceType.Framework);

            resolved.ElementAt(3).IsResolved.Should().BeTrue();
            resolved.ElementAt(3).ReferenceType.Should().Be(ReferenceType.External);
        }

        #endregion

        #region GetRelativePath Tests

        [TestMethod]
        [DataRow("api-reference", "/api-reference/System/Text/Json/JsonSerializer")]
        [DataRow("/docs/api/", "/docs/api/System/Text/Json/JsonSerializer")]
        [DataRow("", "/System/Text/Json/JsonSerializer")]
        public void GetRelativePath_ReturnsSingleRootedPath(string apiReferencePath, string expected)
        {
            _context.ApiReferencePath = apiReferencePath;

            var result = _resolver.GetRelativePath(string.Empty, "System/Text/Json/JsonSerializer");

            result.Should().Be(expected, "an empty ApiReferencePath must not produce '//', which browsers treat as a host name");
        }

        #endregion

        #region Helper Method Tests

        [TestMethod]
        public void StripPrefix_RemovesTypePrefix()
        {
            // Act
            var result = _resolver.StripPrefix("T:System.String");

            // Assert
            result.Should().Be("System.String");
        }

        [TestMethod]
        public void StripPrefix_HandlesNoPrefix()
        {
            // Act
            var result = _resolver.StripPrefix("System.String");

            // Assert
            result.Should().Be("System.String");
        }

        [TestMethod]
        public void GetSimpleTypeName_ExtractsSimpleName()
        {
            // Act
            var result = _resolver.GetSimpleTypeName("CloudNimble.DotNetDocs.Core.Configuration.NamespaceMode");

            // Assert
            result.Should().Be("NamespaceMode");
        }

        [TestMethod]
        public void IsFrameworkType_IdentifiesFrameworkTypes()
        {
            // Act & Assert
            _resolver.IsFrameworkType("System.String").Should().BeTrue();
            _resolver.IsFrameworkType("Microsoft.Extensions.DependencyInjection.IServiceCollection").Should().BeTrue();
            _resolver.IsFrameworkType("Windows.UI.Xaml.Controls.Button").Should().BeTrue();
            _resolver.IsFrameworkType("CloudNimble.DotNetDocs.Core.DocEntity").Should().BeFalse();
        }

        [TestMethod]
        [DataRow("System.String", "https://learn.microsoft.com/dotnet/api/system.string")]
        [DataRow("System.Collections.Generic.List`1", "https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1")]
        [DataRow("System.Collections.Generic.Dictionary`2", "https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary-2")]
        [DataRow("System.Collections.Generic.Dictionary`2.KeyCollection", "https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary-2.keycollection")]
        [DataRow("System.Environment+SpecialFolder", "https://learn.microsoft.com/dotnet/api/system.environment.specialfolder")]
        public void GetFrameworkDocumentationUrl_GeneratesLearnUrl(string typeName, string expected)
        {
            var url = XmlTransformerBase.GetFrameworkDocumentationUrl(typeName);

            url.Should().Be(expected);
        }

        [TestMethod]
        public void GetFrameworkDocumentationUrl_WithNullTypeName_ThrowsArgumentNullException()
        {
            var act = () => XmlTransformerBase.GetFrameworkDocumentationUrl(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("typeName");
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        public void GetFrameworkDocumentationUrl_WithEmptyTypeName_ThrowsArgumentException(string typeName)
        {
            var act = () => XmlTransformerBase.GetFrameworkDocumentationUrl(typeName);

            act.Should().Throw<ArgumentException>().WithParameterName("typeName", "an empty name would link to the Microsoft Learn API root instead of a type page");
        }

        #endregion

        #region Constructor and TransformAsync Guard Tests

        [TestMethod]
        public void Constructor_WithNullProjectContext_ThrowsArgumentNullException()
        {
            var act = () => new MarkdownXmlTransformer(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("projectContext");
        }

        [TestMethod]
        public async Task TransformAsync_WithNullEntity_ThrowsArgumentNullException()
        {
            var act = () => _resolver.TransformAsync(null!);

            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("entity");
        }

        [TestMethod]
        public void BuildReferenceMap_WithNullAssembly_ThrowsArgumentNullException()
        {
            var act = () => _resolver.BuildReferenceMap(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("assembly");
        }

        [TestMethod]
        public void ResolveReferences_WithNullCollection_ThrowsArgumentNullException()
        {
            var act = () => _resolver.ResolveReferences(null!, string.Empty);

            act.Should().Throw<ArgumentNullException>().WithParameterName("rawReferences");
        }

        [TestMethod]
        public void GetRelativePath_WithNullTargetPath_ThrowsArgumentNullException()
        {
            var act = () => _resolver.GetRelativePath(string.Empty, null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("toPath");
        }

        #endregion

        #region Reference Resolution Edge Case Tests

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void ResolveReference_WithMissingReference_ReturnsUnresolvedEmptyReference(string? rawReference)
        {
            var reference = _resolver.ResolveReference(rawReference!, string.Empty);

            reference.IsResolved.Should().BeFalse();
            reference.RawReference.Should().Be(rawReference ?? string.Empty);
            reference.ReferenceType.Should().Be(ReferenceType.Unknown);
            reference.RelativePath.Should().BeNull();
            reference.DisplayName.Should().BeNull();
            reference.TargetEntity.Should().BeNull();
        }

        [TestMethod]
        [DataRow("http://example.com/page")]
        [DataRow("https://example.com/page")]
        public void ResolveReference_WithUrl_ReturnsExternalReferenceToThatUrl(string url)
        {
            var reference = _resolver.ResolveReference(url, string.Empty);

            reference.IsResolved.Should().BeTrue();
            reference.ReferenceType.Should().Be(ReferenceType.External);
            reference.RelativePath.Should().Be(url);
            reference.DisplayName.Should().Be("link");
        }

        [TestMethod]
        public void ResolveReference_WithPrefixOnly_ReturnsUnresolvedReference()
        {
            _resolver.BuildReferenceMap(GetTestsDotSharedAssembly());

            var reference = _resolver.ResolveReference("T:", string.Empty);

            reference.IsResolved.Should().BeFalse("a prefix without a name identifies nothing");
            reference.DisplayName.Should().BeEmpty();
            reference.RelativePath.Should().BeNull();
        }

        [TestMethod]
        public void ResolveReference_WithUnknownMember_UsesLastSegmentWithoutLinking()
        {
            _resolver.BuildReferenceMap(GetTestsDotSharedAssembly());

            var reference = _resolver.ResolveReference("M:Not.A.Real.Type.Method", string.Empty);

            reference.IsResolved.Should().BeFalse();
            reference.DisplayName.Should().Be("Method");
            reference.RelativePath.Should().BeNull();
            reference.Anchor.Should().BeNull();
        }

        [TestMethod]
        public void ResolveReference_WithAssemblyId_LinksToApiReferenceRootWithoutAnchor()
        {
            var assembly = GetTestsDotSharedAssembly();
            _resolver.BuildReferenceMap(assembly);

            var reference = _resolver.ResolveReference($"A:{assembly.AssemblyName}", string.Empty);

            reference.IsResolved.Should().BeTrue();
            reference.TargetEntity.Should().BeSameAs(assembly);
            reference.DisplayName.Should().Be(assembly.DisplayName);
            reference.RelativePath.Should().Be("/api-reference", "the assembly page is the root of the API reference");
            reference.Anchor.Should().BeNull();
        }

        [TestMethod]
        public void ResolveReference_WithAssemblyWithoutDisplayName_UsesLastSegmentOfReference()
        {
            var assembly = GetTestsDotSharedAssembly();
            assembly.DisplayName = null;
            _resolver.BuildReferenceMap(assembly);

            var reference = _resolver.ResolveReference($"A:{assembly.AssemblyName}", string.Empty);

            reference.IsResolved.Should().BeTrue();
            reference.DisplayName.Should().Be("Shared");
        }

        [TestMethod]
        public void ResolveReference_WithNamespaceId_LinksToNamespaceIndexPage()
        {
            _resolver.BuildReferenceMap(GetTestsDotSharedAssembly());

            var reference = _resolver.ResolveReference("N:CloudNimble.DotNetDocs.Tests.Shared.Enums", string.Empty);

            reference.IsResolved.Should().BeTrue();
            reference.TargetEntity.Should().BeOfType<DocNamespace>();
            reference.DisplayName.Should().Be("CloudNimble.DotNetDocs.Tests.Shared.Enums");
            reference.RelativePath.Should().Be("/api-reference/CloudNimble/DotNetDocs/Tests/Shared/Enums/index");
            reference.Anchor.Should().BeNull();
        }

        [TestMethod]
        public void ResolveReference_WithPropertyId_LinksToTypePageWithMemberAnchor()
        {
            _resolver.BuildReferenceMap(GetTestsDotSharedAssembly());

            var reference = _resolver.ResolveReference("P:CloudNimble.DotNetDocs.Tests.Shared.SampleClass.Name", string.Empty);

            reference.IsResolved.Should().BeTrue();
            reference.ReferenceType.Should().Be(ReferenceType.Property);
            reference.DisplayName.Should().Be("SampleClass.Name");
            reference.RelativePath.Should().Be("/api-reference/CloudNimble/DotNetDocs/Tests/Shared/SampleClass");
            reference.Anchor.Should().Be("name");
        }

        [TestMethod]
        public void ResolveReference_AfterRebuildingMapForAnotherAssembly_NoLongerResolvesPreviousAssembly()
        {
            _resolver.BuildReferenceMap(GetTestsDotSharedAssembly());
            _resolver.ResolveReference("T:CloudNimble.DotNetDocs.Tests.Shared.SampleClass", string.Empty).IsResolved.Should().BeTrue();

            _resolver.BuildReferenceMap(CreateWidgetAssembly(out _, out _));

            _resolver.ResolveReference("T:CloudNimble.DotNetDocs.Tests.Shared.SampleClass", string.Empty).IsResolved.Should().BeFalse("each assembly is resolved against its own map, without entries left over from the previous one");
            _resolver.ResolveReference("T:Coverage.Models.Widget", string.Empty).IsResolved.Should().BeTrue();
        }

        [TestMethod]
        public void ResolveReference_WithSimpleNameSharedByTwoTypes_ResolvesToFirstIndexedType()
        {
            _resolver.BuildReferenceMap(CreateWidgetAssembly(out var modelsWidget, out var otherWidget));

            _resolver.ResolveReference("Widget", string.Empty).TargetEntity.Should().BeSameAs(modelsWidget, "a later type must not take over a simple name that is already indexed");
            _resolver.ResolveReference("T:Coverage.Other.Widget", string.Empty).TargetEntity.Should().BeSameAs(otherWidget);
            _resolver.ResolveReference("T:Coverage.Other.Widget", string.Empty).RelativePath.Should().Be("/api-reference/Coverage/Other/Widget");
        }

        [TestMethod]
        public void ResolveReference_WithEventId_UsesEventPrefixAndAnchor()
        {
            var assembly = CreateWidgetAssembly(out var widget, out _);
            var changed = (IEventSymbol)widget.Symbol.GetMembers("Changed").Single();
            widget.Members.Add(new DocMember(changed) { Name = changed.Name, MemberKind = changed.Kind });
            _resolver.BuildReferenceMap(assembly);

            var reference = _resolver.ResolveReference("E:Coverage.Models.Widget.Changed", string.Empty);

            reference.IsResolved.Should().BeTrue();
            reference.ReferenceType.Should().Be(ReferenceType.Event);
            reference.DisplayName.Should().Be("Widget.Changed");
            reference.Anchor.Should().Be("changed");
        }

        [TestMethod]
        public void GetMemberDocumentationId_WithUnexpectedMemberKind_FallsBackToMethodPrefix()
        {
            CreateWidgetAssembly(out var widget, out _);
            var nested = widget.Symbol.GetTypeMembers("Nested").Single();
            var member = new DocMember(nested) { Name = nested.Name, MemberKind = nested.Kind };

            _resolver.GetMemberDocumentationId(member, widget).Should().Be("M:Coverage.Models.Widget.Nested");
        }

        [TestMethod]
        public void GetMemberDocumentationId_WithTypeWithoutFullName_UsesTypeName()
        {
            CreateWidgetAssembly(out var widget, out _);
            widget.FullName = null;
            var size = widget.Symbol.GetMembers("Size").Single();
            var member = new DocMember(size) { Name = size.Name, MemberKind = size.Kind };

            _resolver.GetMemberDocumentationId(member, widget).Should().Be("P:Widget.Size");
            _resolver.GetTypeDocumentationId(widget).Should().Be("T:Widget");
        }

        [TestMethod]
        public void GetDisplayName_WithMemberReferenceWithoutTypeQualifier_ReturnsMemberName()
        {
            CreateWidgetAssembly(out var widget, out _);
            var size = widget.Symbol.GetMembers("Size").Single();
            var member = new DocMember(size) { Name = size.Name, MemberKind = size.Kind };

            _resolver.GetDisplayName("P:Size", member).Should().Be("Size");
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        public void BuildReferenceMap_WithTypeWithoutName_ThrowsArgumentException(string typeName)
        {
            var assembly = CreateWidgetAssembly(out var widget, out _);
            widget.Name = typeName;

            var act = () => _resolver.BuildReferenceMap(assembly);

            act.Should().Throw<ArgumentException>().WithParameterName("typeName", "a type without a name has no page to link to");
        }

        [TestMethod]
        [DataRow("NoNamespace", "NoNamespace")]
        [DataRow("Trailing.", "")]
        [DataRow("", "")]
        public void GetSimpleTypeName_ReturnsTextAfterLastDot(string typeName, string expected)
        {
            _resolver.GetSimpleTypeName(typeName).Should().Be(expected);
        }

        [TestMethod]
        [DataRow("docs\\api\\", "/docs/api/Ns/Type")]
        [DataRow("/", "/Ns/Type")]
        public void GetRelativePath_NormalizesApiReferencePathSeparators(string apiReferencePath, string expected)
        {
            _context.ApiReferencePath = apiReferencePath;

            _resolver.GetRelativePath(string.Empty, "Ns\\Type").Should().Be(expected);
        }

        [TestMethod]
        public void GetRelativePath_WithEmptyTargetPath_ReturnsApiReferenceRoot()
        {
            _resolver.GetRelativePath(string.Empty, string.Empty).Should().Be("/api-reference");
        }

        #endregion

        #region Current Path Tests

        [TestMethod]
        [DataRow(NamespaceMode.Folder)]
        [DataRow(NamespaceMode.File)]
        public async Task TransformAsync_TracksPageOfEachNamespaceAndType(NamespaceMode mode)
        {
            _context.FileNamingOptions = new FileNamingOptions(mode, '-');
            var recorder = new CurrentPathRecorder(_context);
            var assembly = CreateWidgetAssembly(out var widget, out _);
            var size = widget.Symbol.GetMembers("Size").Single();
            widget.Members.Add(new DocMember(size) { Name = size.Name, MemberKind = size.Kind });

            await recorder.TransformAsync(assembly);

            var expected = mode == NamespaceMode.Folder
                ? new[] { string.Empty, Path.Combine("Coverage", "Models", "index"), "Coverage/Models/Widget", "Coverage/Models/Widget", Path.Combine("Coverage", "Other", "index"), "Coverage/Other/Widget" }
                : new[] { string.Empty, "Coverage-Models", "Widget", "Widget", "Coverage-Other", "Widget" };
            recorder.Paths.Should().Equal(expected, "members stay on their type's page");
        }

        [TestMethod]
        [DataRow(NamespaceMode.Folder)]
        [DataRow(NamespaceMode.File)]
        public async Task TransformAsync_WithGlobalNamespace_TracksGlobalPage(NamespaceMode mode)
        {
            _context.FileNamingOptions = new FileNamingOptions(mode, '-');
            var recorder = new CurrentPathRecorder(_context);
            var compilation = CreateCompilation("public class GlobalThing { }");
            var globalNamespace = new DocNamespace(compilation.GlobalNamespace) { Name = string.Empty };

            await recorder.TransformAsync(globalNamespace);

            recorder.Paths.Should().Equal(mode == NamespaceMode.Folder ? Path.Combine("global", "index") : string.Empty);
        }

        [TestMethod]
        public async Task TransformAsync_WithTypeOutsideAnyNamespace_KeepsCurrentPage()
        {
            _context.FileNamingOptions = new FileNamingOptions(NamespaceMode.Folder, '-');
            var recorder = new CurrentPathRecorder(_context);
            var compilation = CreateCompilation("namespace Coverage.Models { public class Widget { } }");
            var arrayType = new DocType(compilation.CreateArrayTypeSymbol(compilation.GetTypeByMetadataName("Coverage.Models.Widget")!)) { Name = "Widget[]" };
            var ns = new DocNamespace(compilation.GetTypeByMetadataName("Coverage.Models.Widget")!.ContainingNamespace.ContainingNamespace) { Name = "Coverage" };
            ns.Types.Add(arrayType);

            await recorder.TransformAsync(ns);

            arrayType.Symbol.ContainingNamespace.Should().BeNull("array types do not belong to a namespace");
            recorder.Paths.Should().Equal(Path.Combine("Coverage", "index"), Path.Combine("Coverage", "index"));
        }

        #endregion

        #region Enum Value Tests

        [TestMethod]
        public async Task TransformAsync_ConvertsXmlTagsInEnumValueDocumentation()
        {
            var assembly = GetTestsDotSharedAssembly();
            var simpleEnum = assembly.Namespaces.SelectMany(ns => ns.Types).OfType<DocEnum>().Single(type => type.Name == "SimpleEnum");
            var value = simpleEnum.Values.First();
            value.Summary = "The <c>first</c> value. See <see cref=\"T:CloudNimble.DotNetDocs.Tests.Shared.SampleClass\"/>.";
            value.Remarks = "Use <paramref name=\"input\"/> carefully.";

            await _resolver.TransformAsync(assembly);

            value.Summary.Should().Be("The `first` value. See [SampleClass](/api-reference/CloudNimble/DotNetDocs/Tests/Shared/SampleClass).",
                "the renderers write enum values from DocEnum.Values, so their documentation must be converted like every other entity's");
            value.Remarks.Should().Be("Use *input* carefully.");
        }

        #endregion

        #region Text Conversion Tests

        [TestMethod]
        [DataRow("null", "[`null`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null)")]
        [DataRow("true", "[`true`](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool)")]
        [DataRow("false", "[`false`](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool)")]
        [DataRow("void", "[`void`](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/void)")]
        [DataRow("async", "[`async`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/async)")]
        [DataRow("await", "[`await`](https://learn.microsoft.com/dotnet/csharp/language-reference/operators/await)")]
        [DataRow("static", "[`static`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/static)")]
        [DataRow("abstract", "[`abstract`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/abstract)")]
        [DataRow("virtual", "[`virtual`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/virtual)")]
        [DataRow("override", "[`override`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/override)")]
        [DataRow("sealed", "[`sealed`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/sealed)")]
        [DataRow("NULL", "[`null`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null)")]
        [DataRow("readonly", "`readonly`")]
        [DataRow("", "&lt;see langword=\"\"/&gt;")]
        public async Task TransformAsync_ConvertsLangwordToKeywordLink(string keyword, string expected)
        {
            CreateWidgetAssembly(out var widget, out _);
            widget.Summary = $"Returns <see langword=\"{keyword}\"/>.";

            await _resolver.TransformAsync(widget);

            widget.Summary.Should().Be($"Returns {expected}.");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public async Task TransformAsync_WithMissingText_LeavesTextUnchanged(string? summary)
        {
            CreateWidgetAssembly(out var widget, out _);
            widget.Summary = summary;

            await _resolver.TransformAsync(widget);

            widget.Summary.Should().Be(summary);
        }

        [TestMethod]
        public async Task TransformAsync_WithMultiLinePlainText_RemovesXmlFileIndentation()
        {
            CreateWidgetAssembly(out var widget, out _);
            widget.Summary = "First line\n\t\tsecond line\n\t\t\tthird line";

            await _resolver.TransformAsync(widget);

            widget.Summary.Should().Be(string.Join(Environment.NewLine, "First line", "second line", "\tthird line"), "the indentation the lines share is removed and the extra tab is kept");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("   single line")]
        public void NormalizeXmlDocIndentation_WithNothingToNormalize_ReturnsInputUnchanged(string? text)
        {
            XmlTransformerBase.NormalizeXmlDocIndentation(text!).Should().Be(text);
        }

        [TestMethod]
        public void NormalizeXmlDocIndentation_WithNoIndentedLines_TrimsText()
        {
            XmlTransformerBase.NormalizeXmlDocIndentation("first\nsecond\n").Should().Be("first\nsecond");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        public void RemoveCommonIndentation_WithNothingToRemove_ReturnsInputUnchanged(string? code)
        {
            XmlTransformerBase.RemoveCommonIndentation(code!).Should().Be(code);
        }

        [TestMethod]
        public void RemoveCommonIndentation_WithTabsAndBlankLines_RemovesSharedIndentationOnly()
        {
            var result = XmlTransformerBase.RemoveCommonIndentation("\t\tif (x)\n\n\t\t\treturn;");

            result.Should().Be(string.Join(Environment.NewLine, "if (x)", string.Empty, "\treturn;"));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Builds a documentation assembly from real Roslyn symbols with two types that share the simple name "Widget".
        /// </summary>
        private static DocAssembly CreateWidgetAssembly(out DocType modelsWidget, out DocType otherWidget)
        {
            var compilation = CreateCompilation("""
                namespace Coverage.Models
                {
                    public class Widget
                    {
                        public event System.EventHandler Changed;
                        public int Size { get; set; }
                        public class Nested { }
                    }
                }
                namespace Coverage.Other
                {
                    public class Widget { }
                }
                """);

            var assembly = new DocAssembly(compilation.Assembly) { AssemblyName = compilation.AssemblyName!, DisplayName = compilation.AssemblyName };
            modelsWidget = AddType(assembly, compilation, "Coverage.Models", "Coverage.Models.Widget");
            otherWidget = AddType(assembly, compilation, "Coverage.Other", "Coverage.Other.Widget");

            return assembly;
        }

        private static DocType AddType(DocAssembly assembly, CSharpCompilation compilation, string namespaceName, string typeName)
        {
            var typeSymbol = compilation.GetTypeByMetadataName(typeName)!;
            var ns = new DocNamespace(typeSymbol.ContainingNamespace) { Name = namespaceName };
            var type = new DocType(typeSymbol) { Name = typeSymbol.Name, FullName = typeName };
            ns.Types.Add(type);
            assembly.Namespaces.Add(ns);

            return type;
        }

        private static CSharpCompilation CreateCompilation(string source) =>
            CSharpCompilation.Create("CoverageModels", [CSharpSyntaxTree.ParseText(source)], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        #endregion

        #region Test Classes

        /// <summary>
        /// A real <see cref="MarkdownXmlTransformer"/> that records <see cref="XmlTransformerBase.CurrentPath"/> after every entity it visits.
        /// </summary>
        private sealed class CurrentPathRecorder(ProjectContext projectContext) : MarkdownXmlTransformer(projectContext)
        {
            public List<string> Paths { get; } = [];

            protected override void UpdateCurrentPath(DocEntity entity)
            {
                base.UpdateCurrentPath(entity);

                // Parameters have no page of their own, so only record the entities that can change the page.
                if (entity is DocAssembly or DocNamespace or DocType or DocMember)
                {
                    Paths.Add(CurrentPath);
                }
            }
        }

        #endregion

    }

}