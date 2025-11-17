using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CloudNimble.DotNetDocs.Core
{


    /// <summary>
    /// Manages assembly metadata extraction using Roslyn for API documentation generation.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using var manager = new AssemblyManager("MyLib.dll", "MyLib.xml");
    /// var context = new ProjectContext("ref1.dll", "ref2.dll") { ConceptualPath = "conceptual" };
    /// var model = await manager.DocumentAsync(context);
    /// ]]>
    /// </code>
    /// </example>
    /// <remarks>
    /// Extracts metadata from a single .NET assembly and its XML documentation file, building an in-memory model
    /// (<see cref="DocAssembly"/>) with interconnected types, members, and parameters. Supports conceptual content
    /// loading from a specified folder. Designed for multi-targeting .NET 10.0, 9.0, and 8.0. One instance per assembly
    /// is required, with paths specified at construction for incremental build support. Implements <see cref="IDisposable"/>
    /// to release memory used by the compilation and model.
    /// </remarks>
    public class AssemblyManager : IDisposable
    {

        #region Fields

        private Compilation? _compilation;
        private bool _disposed;

        /// <summary>
        /// SymbolDisplayFormat for generating member signatures with access modifiers.
        /// </summary>
        private static readonly SymbolDisplayFormat DocumentationSignatureFormat = new SymbolDisplayFormat(
            memberOptions: SymbolDisplayMemberOptions.IncludeAccessibility |
                           SymbolDisplayMemberOptions.IncludeModifiers |
                           SymbolDisplayMemberOptions.IncludeType |
                           SymbolDisplayMemberOptions.IncludeParameters |
                           SymbolDisplayMemberOptions.IncludeRef,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType |
                              SymbolDisplayParameterOptions.IncludeName |
                              SymbolDisplayParameterOptions.IncludeDefaultValue |
                              SymbolDisplayParameterOptions.IncludeParamsRefOut,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters |
                             SymbolDisplayGenericsOptions.IncludeTypeConstraints,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        /// <summary>
        /// SymbolDisplayFormat for generating property signatures with accessors.
        /// </summary>
        private static readonly SymbolDisplayFormat PropertySignatureFormat = new SymbolDisplayFormat(
            memberOptions: SymbolDisplayMemberOptions.IncludeAccessibility |
                           SymbolDisplayMemberOptions.IncludeModifiers |
                           SymbolDisplayMemberOptions.IncludeType,
            propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        #endregion

        #region Properties

        /// <summary>
        /// Gets the name of the assembly (without extension).
        /// </summary>
        public string AssemblyName { get; init; }

        /// <summary>
        /// Gets the path to the assembly DLL file.
        /// </summary>
        public string AssemblyPath { get; init; }

        /// <summary>
        /// Gets the current documentation model for the processed assembly.
        /// </summary>
        public DocAssembly? Document { get; private set; }

        /// <summary>
        /// Gets the collection of errors that occurred during Since  processing.
        /// </summary>
        /// <remarks>
        /// Includes warnings about inaccessible internal members when requested but not available.
        /// </remarks>
        public List<CompilerError> Errors { get; } = [];

        /// <summary>
        /// Gets the last modified timestamp of the assembly file for incremental builds.
        /// </summary>
        public DateTime LastModified { get; private set; }

        /// <summary>
        /// Gets the path to the XML documentation file.
        /// </summary>
        /// <remarks>
        /// May be null if no XML documentation file is available.
        /// </remarks>
        public string? XmlPath { get; init; }

        /// <summary>
        /// Gets the previously used included members for caching.
        /// </summary>
        private List<Accessibility> PreviousIncludedMembers { get; set; } = [];

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="AssemblyManager"/> for a specific assembly.
        /// </summary>
        /// <param name="assemblyPath">Path to the assembly DLL file.</param>
        /// <param name="xmlPath">Path to the XML documentation file.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="assemblyPath"/> or <paramref name="xmlPath"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="assemblyPath"/> or <paramref name="xmlPath"/> is empty or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown when <paramref name="assemblyPath"/> does not exist.</exception>
        /// <remarks>
        /// If the XML documentation file does not exist, processing will continue without XML documentation.
        /// A warning will be added to the Errors collection.
        /// </remarks>
        public AssemblyManager(string assemblyPath, string xmlPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
            ArgumentException.ThrowIfNullOrWhiteSpace(xmlPath);

            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException("Assembly file not found.", nameof(assemblyPath));
            }
            
            // Handle missing XML documentation gracefully
            if (!File.Exists(xmlPath))
            {
                // Add a warning but continue processing
                Errors.Add(new CompilerError(xmlPath, 0, 0, "DOC001", 
                    $"XML documentation file not found: {Path.GetFileName(xmlPath)}. Processing will continue without XML documentation."));
                XmlPath = null; // Mark as null to indicate no XML is available
            }
            else
            {
                XmlPath = xmlPath;
            }

            AssemblyPath = assemblyPath;
            AssemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
            LastModified = DateTime.MinValue;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Documents the assembly asynchronously, building or rebuilding an in-memory model with metadata and documentation if necessary.
        /// </summary>
        /// <param name="projectContext">Optional project context for referenced assemblies and conceptual content path.</param>
        /// <returns>A task representing the asynchronous operation, containing the <see cref="DocAssembly"/> model.</returns>
        public async Task<DocAssembly> DocumentAsync(ProjectContext? projectContext = null)
        {
            var currentModified = File.GetLastWriteTimeUtc(AssemblyPath);
            var includedMembers = projectContext?.IncludedMembers ?? [Accessibility.Public];

            // Check if we need to rebuild: file modified, no document yet, or included members changed
            var needsRebuild = Document is null ||
                              currentModified > LastModified ||
                              !includedMembers.SequenceEqual(PreviousIncludedMembers);

            if (needsRebuild)
            {
                _compilation = await CreateCompilationAsync(projectContext?.References ?? []);
                Document = BuildModel(_compilation, projectContext);
                LastModified = currentModified;
                PreviousIncludedMembers = includedMembers.ToList();
            }
            return Document!;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Extracts and parses XML documentation from a symbol.
        /// </summary>
        /// <param name="symbol">The symbol to extract documentation from.</param>
        /// <returns>The parsed XML document, or null if no documentation exists.</returns>
        internal XDocument? ExtractDocumentationXml(ISymbol symbol)
        {
            var xml = symbol.GetDocumentationCommentXml() ?? string.Empty;
            return string.IsNullOrWhiteSpace(xml) ? null : XDocument.Parse(xml);
        }

        /// <summary>
        /// Extracts inner XML content from an XElement, preserving nested XML tags.
        /// </summary>
        /// <param name="element">The XML element to extract content from.</param>
        /// <returns>The inner XML as a string, or null if element is null.</returns>
        internal string? ExtractInnerXml(XElement? element)
        {
            if (element is null)
                return null;

            // Get all nodes and concatenate them, preserving XML tags
            var innerXml = string.Concat(element.Nodes().Select(n => n.ToString()));
            var trimmed = innerXml?.Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }

        /// <summary>
        /// Extracts inner XML content from an XElement, preserving nested XML tags but excluding specified tags and their content.
        /// </summary>
        /// <param name="element">The XML element to extract content from.</param>
        /// <param name="excludeTags">Tag names to exclude from the output (including all their content).</param>
        /// <returns>The inner XML as a string with excluded tags and their content removed, or null if element is null.</returns>
        internal string? ExtractInnerXmlExcluding(XElement? element, params string[] excludeTags)
        {
            if (element is null)
                return null;

            // Clone the element to avoid modifying the original
            var cloned = new XElement(element);

            // Remove all excluded tags and their content
            foreach (var tag in excludeTags)
            {
                cloned.Descendants(tag).Remove();
            }

            // Get all nodes and concatenate them, preserving XML tags
            var innerXml = string.Concat(cloned.Nodes().Select(n => n.ToString()));
            var trimmed = innerXml?.Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }

        /// <summary>
        /// Extracts the summary text from XML documentation, preserving inner XML tags.
        /// </summary>
        /// <param name="doc">The parsed XML documentation.</param>
        /// <returns>The summary text with preserved XML tags, or empty string if not found.</returns>
        internal string? ExtractSummary(XDocument? doc) =>
            ExtractInnerXml(doc?.Descendants("summary").FirstOrDefault());

        /// <summary>
        /// Extracts the examples text from XML documentation, preserving inner XML tags.
        /// </summary>
        /// <param name="doc">The parsed XML documentation.</param>
        /// <returns>The examples text with preserved XML tags, or empty string if not found.</returns>
        internal string? ExtractExamples(XDocument? doc) =>
            ExtractInnerXml(doc?.Descendants("example").FirstOrDefault());

        /// <summary>
        /// Extracts the remarks/best practices text from XML documentation, preserving inner XML tags but excluding example tags.
        /// </summary>
        /// <param name="doc">The parsed XML documentation.</param>
        /// <returns>The remarks text with preserved XML tags (excluding example tags and their content), or empty string if not found.</returns>
        internal string? ExtractRemarks(XDocument? doc) =>
            ExtractInnerXmlExcluding(doc?.Descendants("remarks").FirstOrDefault(), "example");

        /// <summary>
        /// Extracts parameter documentation from XML documentation, preserving inner XML tags.
        /// </summary>
        /// <param name="doc">The parsed XML documentation.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <returns>The parameter documentation with preserved XML tags, or empty string if not found.</returns>
        internal string ExtractParameterDocumentation(XDocument? doc, string parameterName) =>
            ExtractInnerXml(doc?.Descendants("param")
                .FirstOrDefault(e => e.Attribute("name")?.Value == parameterName)) ?? string.Empty;

        /// <summary>
        /// Extracts the returns documentation from XML documentation, preserving inner XML tags.
        /// </summary>
        /// <param name="doc">The parsed XML documentation.</param>
        /// <returns>The returns text with preserved XML tags, or null if not found.</returns>
        internal string? ExtractReturns(XDocument? doc) =>
            ExtractInnerXml(doc?.Descendants("returns").FirstOrDefault());

        /// <summary>
        /// Extracts exception documentation from XML documentation, preserving inner XML tags.
        /// </summary>
        /// <param name="doc">The parsed XML documentation.</param>
        /// <returns>Collection of exception documentation, or null if none found.</returns>
        internal ICollection<DocException>? ExtractExceptions(XDocument? doc)
        {
            var exceptions = doc?.Descendants("exception")
                .Select(e => new DocException
                {
                    Type = e.Attribute("cref")?.Value?.Replace("T:", "")?.Split('.').LastOrDefault(),
                    Description = ExtractInnerXml(e) ?? string.Empty
                })
                .Where(e => !string.IsNullOrWhiteSpace(e.Type))
                .ToList();

            return exceptions?.Any() == true ? exceptions : null;
        }

        /// <summary>
        /// Extracts type parameter documentation from XML documentation, preserving inner XML tags.
        /// </summary>
        /// <param name="doc">The parsed XML documentation.</param>
        /// <returns>Collection of type parameter documentation, or null if none found.</returns>
        internal ICollection<DocTypeParameter>? ExtractTypeParameters(XDocument? doc)
        {
            var typeParams = doc?.Descendants("typeparam")
                .Select(e => new DocTypeParameter
                {
                    Name = e.Attribute("name")?.Value,
                    Description = ExtractInnerXml(e) ?? string.Empty
                })
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .ToList();

            return typeParams?.Any() == true ? typeParams : null;
        }

        /// <summary>
        /// Extracts the value documentation from XML documentation (for properties), preserving inner XML tags.
        /// </summary>
        /// <param name="doc">The parsed XML documentation.</param>
        /// <returns>The value text with preserved XML tags, or null if not found.</returns>
        internal string? ExtractValue(XDocument? doc) =>
            ExtractInnerXml(doc?.Descendants("value").FirstOrDefault());

        /// <summary>
        /// Extracts see-also references from XML documentation.
        /// </summary>
        /// <param name="doc">The parsed XML documentation.</param>
        /// <returns>Collection of see-also references, or null if none found.</returns>
        internal ICollection<DocReference>? ExtractSeeAlso(XDocument? doc)
        {
            var seeAlso = doc?.Descendants("seealso")
                .Select(e => e.Attribute("cref")?.Value)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => new DocReference(s!))
                .ToList();

            return seeAlso?.Any() == true ? seeAlso : null;
        }

        /// <summary>
        /// Builds the in-memory documentation model from the Roslyn compilation.
        /// </summary>
        /// <param name="compilation">The Roslyn compilation containing assembly metadata.</param>
        /// <param name="projectContext">Optional project context containing configuration and filters.</param>
        /// <returns>The <see cref="DocAssembly"/> model.</returns>
        internal DocAssembly BuildModel(Compilation compilation, ProjectContext? projectContext)
        {
            var targetRef = compilation.References.OfType<PortableExecutableReference>().FirstOrDefault(r => string.Equals(r.FilePath, AssemblyPath, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Target assembly reference not found in compilation.");

            var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(targetRef) as IAssemblySymbol
                ?? throw new InvalidOperationException("Could not get assembly symbol from compilation.");

            var assemblyDoc = ExtractDocumentationXml(assemblySymbol);
            var includedMembers = projectContext?.IncludedMembers ?? [Accessibility.Public];

            var docAssembly = new DocAssembly(assemblySymbol)
            {
                AssemblyName = assemblySymbol.Name,
                Version = assemblySymbol.Identity.Version.ToString(),
                DisplayName = assemblySymbol.ToDisplayString(),
                Summary = ExtractSummary(assemblyDoc),
                Returns = ExtractReturns(assemblyDoc),
                Exceptions = ExtractExceptions(assemblyDoc),
                TypeParameters = ExtractTypeParameters(assemblyDoc),
                Value = ExtractValue(assemblyDoc),
                SeeAlso = ExtractSeeAlso(assemblyDoc),
                IncludedMembers = includedMembers
            };

            var typeMap = new Dictionary<string, DocType>(); // Cache for type resolutions

            // Process all namespaces recursively
            ProcessNamespace(assemblySymbol.GlobalNamespace, docAssembly, compilation, typeMap, includedMembers, projectContext);

            // Relocate extension methods to target types (always enabled)
            RelocateExtensionMethods(docAssembly, compilation, typeMap, projectContext);

            return docAssembly;
        }

        /// <summary>
        /// Builds a <see cref="DocType"/> from a Roslyn type symbol, resolving members and base types.
        /// </summary>
        /// <param name="type">The Roslyn type symbol.</param>
        /// <param name="compilation">The Roslyn compilation for symbol resolution.</param>
        /// <param name="typeMap">Cache of resolved types for linking.</param>
        /// <param name="includedMembers">List of member accessibilities to include.</param>
        /// <param name="projectContext">The project context with configuration settings.</param>
        /// <returns>The <see cref="DocType"/> instance.</returns>
        internal DocType BuildDocType(ITypeSymbol type, Compilation compilation, Dictionary<string, DocType> typeMap, List<Accessibility> includedMembers, ProjectContext? projectContext)
        {
            var doc = ExtractDocumentationXml(type);

            DocType docType;

            // Create DocEnum for enum types
            // Note: When loading from metadata (compiled DLL), enums appear as sealed classes with const fields
            // We need to detect this pattern since TypeKind.Enum is not preserved in metadata
            var isEnum = type.TypeKind == TypeKind.Enum ||
                         (type.TypeKind == TypeKind.Class && type.IsSealed && type.BaseType?.Name == "Enum");

            if (isEnum)
            {
                var docEnum = new DocEnum(type)
                {
                    Name = type.Name,
                    FullName = type.ToDisplayString(),
                    DisplayName = type.ToDisplayString(),
                    Signature = type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    TypeKind = type.TypeKind,
                    AssemblyName = type.ContainingAssembly?.Name,
                    Summary = ExtractSummary(doc),
                    Returns = ExtractReturns(doc),
                    Exceptions = ExtractExceptions(doc),
                    TypeParameters = ExtractTypeParameters(doc),
                    Value = ExtractValue(doc),
                    SeeAlso = ExtractSeeAlso(doc),
                    Examples = ExtractExamples(doc),
                    Remarks = ExtractRemarks(doc)
                };

                // Check for Flags attribute
                docEnum.IsFlags = type.GetAttributes()
                    .Any(a => a.AttributeClass?.ToDisplayString() == "System.FlagsAttribute");

                // Get underlying type
                if (type is INamedTypeSymbol namedType)
                {
                    ITypeSymbol? underlyingType = namedType.EnumUnderlyingType;

                    // Fallback: For metadata-loaded enums, infer from constant value type
                    if (underlyingType is null)
                    {
                        var firstConstField = type.GetMembers()
                            .OfType<IFieldSymbol>()
                            .FirstOrDefault(f => f.IsConst && f.HasConstantValue);

                        if (firstConstField?.ConstantValue is not null)
                        {
                            // Determine underlying type from the constant value's actual type
                            underlyingType = firstConstField.ConstantValue switch
                            {
                                byte => compilation.GetSpecialType(SpecialType.System_Byte),
                                sbyte => compilation.GetSpecialType(SpecialType.System_SByte),
                                short => compilation.GetSpecialType(SpecialType.System_Int16),
                                ushort => compilation.GetSpecialType(SpecialType.System_UInt16),
                                int => compilation.GetSpecialType(SpecialType.System_Int32),
                                uint => compilation.GetSpecialType(SpecialType.System_UInt32),
                                long => compilation.GetSpecialType(SpecialType.System_Int64),
                                ulong => compilation.GetSpecialType(SpecialType.System_UInt64),
                                _ => compilation.GetSpecialType(SpecialType.System_Int32) // Default to int
                            };
                        }
                    }

                    if (underlyingType is not null)
                    {
                        docEnum.UnderlyingType = new DocReference
                        {
                            RawReference = $"T:{underlyingType.ToDisplayString()}",
                            DisplayName = GetFriendlyTypeName(underlyingType),
                            IsResolved = true,
                            ReferenceType = ReferenceType.Framework
                        };
                    }
                }

                // Extract enum values
                foreach (var member in type.GetMembers().OfType<IFieldSymbol>().Where(f => f.IsConst))
                {
                    var memberDoc = ExtractDocumentationXml(member);
                    var enumValue = new DocEnumValue(member)
                    {
                        Name = member.Name,
                        DisplayName = member.ToDisplayString(),
                        NumericValue = member.ConstantValue?.ToString(),
                        Summary = ExtractSummary(memberDoc),
                        Remarks = ExtractRemarks(memberDoc),
                        Examples = ExtractExamples(memberDoc),
                        SeeAlso = ExtractSeeAlso(memberDoc)
                    };
                    docEnum.Values.Add(enumValue);
                }

                docType = docEnum;
            }
            else
            {
                docType = new DocType(type)
                {
                    Name = type.Name,
                    FullName = type.ToDisplayString(),
                    DisplayName = type.ToDisplayString(),
                    Signature = type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    TypeKind = type.TypeKind,
                    AssemblyName = type.ContainingAssembly?.Name,
                    Summary = ExtractSummary(doc),
                    Returns = ExtractReturns(doc),
                    Exceptions = ExtractExceptions(doc),
                    TypeParameters = ExtractTypeParameters(doc),
                    Value = ExtractValue(doc),
                    SeeAlso = ExtractSeeAlso(doc),
                    Examples = ExtractExamples(doc),
                    Remarks = ExtractRemarks(doc)
                };
            }

            docType.IncludedMembers = includedMembers;

            typeMap[type.ToDisplayString()] = docType;

            // Resolve base type
            if (type.BaseType is not null && type.BaseType.SpecialType != SpecialType.System_Object)
            {
                docType.BaseType = type.BaseType.ToDisplayString();
            }

            // Set related APIs (e.g., all interfaces as strings)
            docType.RelatedApis = type.AllInterfaces.Select(i => i.ToDisplayString()).ToList();

            // Resolve members (skip for enums as they use Values collection instead)
            // The bridge compilation with IgnoresAccessChecksTo allows us to see all members including internals
            if (type.TypeKind != TypeKind.Enum)
            {
                // Get all members including inherited from base types
                // Replace any error System.Object types with the properly-resolved one from compilation
                // For static classes, only get members from the class itself (not inherited) since static classes
                // can't use inherited instance members and we need to detect empty extension classes for removal
                var allTypesInChain = type.IsStatic
                    ? [type]
                    : GetBaseTypesAndThis(type)
                    .Select(t =>
                    {
                        // If this is System.Object but it's an error type, replace it with the compilation's special type
                        if (t.Name == "Object" && t.TypeKind == TypeKind.Error && _compilation is not null)
                        {
                            return _compilation.GetSpecialType(SpecialType.System_Object);
                        }
                        return t;
                    })
                    .ToList();

                var allMembers = allTypesInChain
                    .SelectMany(t => t.GetMembers())
                    .Where(m => !m.IsImplicitlyDeclared)
                    .ToList();

                foreach (var member in allMembers)
                {
                    // Check if member is inherited
                    var isInherited = !SymbolEqualityComparer.Default.Equals(member.ContainingType, type);
                    var declaringType = member.ContainingType;

                    // Filter System.Object members if configured
                    if (isInherited &&
                        declaringType.SpecialType == SpecialType.System_Object &&
                        !(projectContext?.IncludeSystemObjectInheritance ?? true))
                    {
                        continue;
                    }

                    // Check accessibility for inherited members
                    if (isInherited && !IsAccessibleInDerivedType(member, type))
                    {
                        continue;
                    }

                    // Check accessibility for declared members
                    if (!isInherited && !docType.IncludedMembers.Contains(member.DeclaredAccessibility))
                    {
                        continue;
                    }

                    DocMember? docMember = null;

                       if (member is IMethodSymbol method)
                    {
                        var mDoc = ExtractDocumentationXml(method);

                        docMember = new DocMember(method)
                        {
                            Name = method.Name,
                            DisplayName = method.ToDisplayString(),
                            Signature = method.ToDisplayString(DocumentationSignatureFormat),
                            MemberKind = method.Kind,
                            MethodKind = method.MethodKind,
                            Accessibility = method.DeclaredAccessibility,
                            ReturnTypeName = method.ReturnsVoid ? "void" : method.ReturnType.ToDisplayString(),
                            Summary = ExtractSummary(mDoc),
                            Returns = ExtractReturns(mDoc),
                            Exceptions = ExtractExceptions(mDoc),
                            TypeParameters = ExtractTypeParameters(mDoc),
                            Value = ExtractValue(mDoc),
                            SeeAlso = ExtractSeeAlso(mDoc),
                            Examples = ExtractExamples(mDoc),
                            Remarks = ExtractRemarks(mDoc),
                            IncludedMembers = docType.IncludedMembers,
                            // Provenance tracking
                            IsInherited = isInherited,
                            DeclaringTypeName = declaringType.ToDisplayString(),
                            IsOverride = method.IsOverride,
                            IsVirtual = method.IsVirtual,
                            IsAbstract = method.IsAbstract,
                            OverriddenMember = method.OverriddenMethod?.ToDisplayString(),
                            IsExtensionMethod = method.IsExtensionMethod,
                            ExtendedTypeName = method.IsExtensionMethod
                                ? method.Parameters.First().Type.ToDisplayString()
                                : null,
                         // Parameters
                         Parameters = method.Parameters.Select(p =>
                             {
                                 var paramDoc = new DocParameter(p)
                                 {
                                     Name = p.Name,
                                     TypeName = p.Type.ToDisplayString(),
                                     DisplayName = p.ToDisplayString(),
                                     IsOptional = p.IsOptional,
                                     HasDefaultValue = p.HasExplicitDefaultValue,
                                     DefaultValue = p.HasExplicitDefaultValue ? p.ExplicitDefaultValue?.ToString() : null,
                                     IsParams = p.IsParams,
                                     Usage = ExtractParameterDocumentation(mDoc, p.Name)
                                 };

                                 // Parameter type - create minimal standalone instance to avoid circular references
                                 // Do NOT add to typeMap or reuse instances, as that causes circular references when members are populated
                                 paramDoc.ParameterType = new DocType(p.Type)
                                 {
                                     Name = p.Type.Name,
                                     FullName = p.Type.ToDisplayString(),
                                     DisplayName = p.Type.ToDisplayString(),
                                     Signature = p.Type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                                     TypeKind = p.Type.TypeKind,
                                     AssemblyName = p.Type.ContainingAssembly?.Name,
                                     IncludedMembers = docType.IncludedMembers,
                                     IsExternalReference = true // Mark as external to prevent member population
                                 };

                                 return paramDoc;
                             }).ToList()
                     };

                     // Return type - create minimal standalone instance to avoid circular references
                     // Do NOT add to typeMap or reuse instances, as that causes circular references when members are populated
                     if (method.ReturnType.SpecialType != SpecialType.System_Void)
                     {
                         docMember.ReturnType = new DocType(method.ReturnType)
                         {
                             Name = method.ReturnType.Name,
                             FullName = method.ReturnType.ToDisplayString(),
                             DisplayName = method.ReturnType.ToDisplayString(),
                             Signature = method.ReturnType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                             TypeKind = method.ReturnType.TypeKind,
                             AssemblyName = method.ReturnType.ContainingAssembly?.Name,
                             IncludedMembers = docType.IncludedMembers,
                             IsExternalReference = true // Mark as external to prevent member population
                         };
                     }
                 }
                    else if (member is IPropertySymbol property)
                    {
                        var pDoc = ExtractDocumentationXml(property);

                        docMember = new DocMember(property)
                        {
                            Name = property.Name,
                            DisplayName = property.ToDisplayString(),
                            Signature = property.ToDisplayString(PropertySignatureFormat),
                            MemberKind = property.Kind,
                            Accessibility = property.DeclaredAccessibility,
                            ReturnTypeName = property.Type.ToDisplayString(),
                            Summary = ExtractSummary(pDoc),
                            Value = ExtractValue(pDoc),
                            Exceptions = ExtractExceptions(pDoc),
                            SeeAlso = ExtractSeeAlso(pDoc),
                            Examples = ExtractExamples(pDoc),
                            Remarks = ExtractRemarks(pDoc),
                            IncludedMembers = docType.IncludedMembers,
                            // Provenance tracking
                            IsInherited = isInherited,
                            DeclaringTypeName = declaringType.ToDisplayString(),
                            IsOverride = property.IsOverride,
                            IsVirtual = property.IsVirtual,
                            IsAbstract = property.IsAbstract,
                            OverriddenMember = property.OverriddenProperty?.ToDisplayString()
                        };
                    }
                    else if (member is IFieldSymbol field)
                    {
                        var fDoc = ExtractDocumentationXml(field);

                        docMember = new DocMember(field)
                        {
                            Name = field.Name,
                            DisplayName = field.ToDisplayString(),
                            Signature = field.ToDisplayString(DocumentationSignatureFormat),
                            MemberKind = field.Kind,
                            Accessibility = field.DeclaredAccessibility,
                            ReturnTypeName = field.Type.ToDisplayString(),
                            Summary = ExtractSummary(fDoc),
                            Value = ExtractValue(fDoc),
                            SeeAlso = ExtractSeeAlso(fDoc),
                            Examples = ExtractExamples(fDoc),
                            Remarks = ExtractRemarks(fDoc),
                            IncludedMembers = docType.IncludedMembers,
                            // Provenance tracking
                            IsInherited = isInherited,
                            DeclaringTypeName = declaringType.ToDisplayString()
                        };
                    }
                    else if (member is IEventSymbol eventSymbol)
                    {
                        var eDoc = ExtractDocumentationXml(eventSymbol);

                        docMember = new DocMember(eventSymbol)
                        {
                            Name = eventSymbol.Name,
                            DisplayName = eventSymbol.ToDisplayString(),
                            Signature = eventSymbol.ToDisplayString(DocumentationSignatureFormat),
                            MemberKind = eventSymbol.Kind,
                            Accessibility = eventSymbol.DeclaredAccessibility,
                            ReturnTypeName = eventSymbol.Type.ToDisplayString(),
                            Summary = ExtractSummary(eDoc),
                            SeeAlso = ExtractSeeAlso(eDoc),
                            Examples = ExtractExamples(eDoc),
                            Remarks = ExtractRemarks(eDoc),
                            IncludedMembers = docType.IncludedMembers,
                            // Provenance tracking
                            IsInherited = isInherited,
                            DeclaringTypeName = declaringType.ToDisplayString()
                        };
                    }

                    if (docMember is not null)
                    {
                        docType.Members.Add(docMember);
                    }
                }
            }

            return docType;
        }

        /// <summary>
        /// Gets a friendly display name for a type (e.g., "int" instead of "System.Int32").
        /// </summary>
        /// <param name="type">The type symbol to get a friendly name for.</param>
        /// <returns>A friendly type name suitable for display.</returns>
        internal static string GetFriendlyTypeName(ITypeSymbol type)
        {
            return type.SpecialType switch
            {
                SpecialType.System_Boolean => "bool",
                SpecialType.System_Byte => "byte",
                SpecialType.System_SByte => "sbyte",
                SpecialType.System_Char => "char",
                SpecialType.System_Decimal => "decimal",
                SpecialType.System_Double => "double",
                SpecialType.System_Single => "float",
                SpecialType.System_Int32 => "int",
                SpecialType.System_UInt32 => "uint",
                SpecialType.System_Int64 => "long",
                SpecialType.System_UInt64 => "ulong",
                SpecialType.System_Int16 => "short",
                SpecialType.System_UInt16 => "ushort",
                SpecialType.System_Object => "object",
                SpecialType.System_String => "string",
                SpecialType.System_Void => "void",
                _ => type.ToDisplayString()
            };
        }

        /// <summary>
        /// Creates a Roslyn compilation from the assembly and XML documentation.
        /// </summary>
        /// <param name="references">Paths to referenced assemblies.</param>
        /// <returns>A task representing the asynchronous operation, containing the <see cref="Compilation"/>.</returns>
        /// <remarks>
        /// Uses a bridge compilation technique with IgnoresAccessChecksTo attribute to enable
        /// visibility of internal members for documentation purposes.
        /// </remarks>
        internal async Task<Compilation> CreateCompilationAsync(IEnumerable<string> references)
        {
            // Generate the IgnoresAccessChecksTo attribute dynamically
            // This is a compiler trick to allow seeing internal members during compilation
            // RWM: This technique is a modification of:
            // https://www.strathweb.com/2018/10/no-internalvisibleto-no-problem-bypassing-c-visibility-rules-with-roslyn/
            var ignoresAccessChecksSource = @"
                namespace System.Runtime.CompilerServices
                {
                    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
                    internal sealed class IgnoresAccessChecksToAttribute : System.Attribute
                    {
                        public string AssemblyName { get; }
                        public IgnoresAccessChecksToAttribute(string assemblyName)
                        {
                            AssemblyName = assemblyName;
                        }
                    }
                }";

            // Apply the attribute to our bridge compilation to see internals of the target assembly
            var assemblyName = Path.GetFileNameWithoutExtension(AssemblyPath);
            var bridgeSource = $@"
                using System.Runtime.CompilerServices;
                [assembly: IgnoresAccessChecksTo(""{assemblyName}"")]";

            // Parse the source trees
            var syntaxTrees = new[]
            {
                CSharpSyntaxTree.ParseText(ignoresAccessChecksSource),
                CSharpSyntaxTree.ParseText(bridgeSource)
            };

            // Create compilation with metadata import options to see all members
            var compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                metadataImportOptions: MetadataImportOptions.All);

            // Create the compilation with our bridge that can see internals
            var compilation = CSharpCompilation.Create($"{AssemblyName}.DocumentationBridge")
                .WithOptions(compilationOptions)
                .AddSyntaxTrees(syntaxTrees);

            // Add the target assembly with its XML documentation (if available)
            var documentationProvider = XmlPath is not null && File.Exists(XmlPath) 
                ? XmlDocumentationProvider.CreateFromFile(XmlPath)
                : null;
            var targetReference = MetadataReference.CreateFromFile(
                AssemblyPath, 
                documentation: documentationProvider);
            compilation = compilation.AddReferences(targetReference);

            // Add all provided references
            foreach (var refPath in references)
            {
                if (File.Exists(refPath))
                {
                    compilation = compilation.AddReferences(MetadataReference.CreateFromFile(refPath));
                }
            }

            // Add common .NET references for resolution
            var netRefs = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            };
            compilation = compilation.AddReferences(netRefs);

            return await Task.FromResult(compilation);
        }



        /// <summary>
        /// Processes a namespace and its nested namespaces recursively.
        /// </summary>
        /// <param name="namespaceSymbol">The namespace symbol to process.</param>
        /// <param name="docAssembly">The documentation assembly being built.</param>
        /// <param name="compilation">The Roslyn compilation.</param>
        /// <param name="typeMap">Cache for type resolutions.</param>
        /// <param name="includedMembers">List of member accessibilities to include.</param>
        /// <param name="projectContext">Optional project context containing configuration and filters.</param>
        internal void ProcessNamespace(INamespaceSymbol namespaceSymbol, DocAssembly docAssembly, Compilation compilation, Dictionary<string, DocType> typeMap, List<Accessibility> includedMembers, ProjectContext? projectContext)
        {
            // Process nested namespaces recursively, collecting all namespaces with types
            // We always skip the global namespace to avoid documenting compiler-generated <Module> types
            ProcessNamespaceRecursive(namespaceSymbol, docAssembly, compilation, typeMap, includedMembers, projectContext);
        }

        /// <summary>
        /// Recursively processes namespaces, only adding those that contain types.
        /// </summary>
        /// <param name="namespaceSymbol">The namespace symbol to process.</param>
        /// <param name="docAssembly">The documentation assembly being built.</param>
        /// <param name="compilation">The Roslyn compilation.</param>
        /// <param name="typeMap">Cache for type resolutions.</param>
        /// <param name="includedMembers">List of member accessibilities to include.</param>
        /// <param name="projectContext">Optional project context containing configuration and filters.</param>
        internal void ProcessNamespaceRecursive(INamespaceSymbol namespaceSymbol, DocAssembly docAssembly, Compilation compilation, Dictionary<string, DocType> typeMap, List<Accessibility> includedMembers, ProjectContext? projectContext)
        {
            foreach (var ns in namespaceSymbol.GetNamespaceMembers())
            {
                // Check if this namespace has any types with the required accessibility
                var typesInNamespace = ns.GetTypeMembers()
                    .Where(t => includedMembers.Contains(t.DeclaredAccessibility))
                    .ToList();
                
                // Filter out excluded types if we have a project context
                if (projectContext is not null)
                {
                    typesInNamespace = typesInNamespace
                        .Where(t => !projectContext.IsTypeExcluded(t.ToDisplayString()))
                        .ToList();
                }
                
                if (typesInNamespace.Any())
                {
                    // This namespace has types, so add it to the assembly
                    var nsDoc = ExtractDocumentationXml(ns);

                    var docNs = new DocNamespace(ns)
                    {
                        Name = ns.IsGlobalNamespace ? string.Empty : ns.ToDisplayString(),
                        DisplayName = ns.ToDisplayString(),
                        Summary = ExtractSummary(nsDoc),
                        SeeAlso = ExtractSeeAlso(nsDoc),
                        IncludedMembers = includedMembers
                    };
                    docAssembly.Namespaces.Add(docNs);

                    // Process types in this namespace
                    foreach (var type in typesInNamespace)
                    {
                        var docType = BuildDocType(type, compilation, typeMap, includedMembers, projectContext);
                        docNs.Types.Add(docType);
                    }
                }

                // Recurse into nested namespaces regardless of whether this one had types
                ProcessNamespaceRecursive(ns, docAssembly, compilation, typeMap, includedMembers, projectContext);
            }
        }

        /// <summary>
        /// Relocates extension methods from their declaring static classes to the types they extend.
        /// </summary>
        /// <param name="assembly">The assembly being documented.</param>
        /// <param name="compilation">The Roslyn compilation.</param>
        /// <param name="typeMap">Map of type names to DocType instances for type resolution.</param>
        /// <param name="projectContext">The project context with configuration settings.</param>
        /// <remarks>
        /// This method performs post-processing after initial assembly documentation is complete.
        /// Extension methods are moved from static container classes to the types they extend,
        /// creating external type references as needed.
        /// </remarks>
        internal void RelocateExtensionMethods(DocAssembly assembly, Compilation compilation, Dictionary<string, DocType> typeMap, ProjectContext? projectContext)
        {
            // Find all extension methods
            var extensionMethods = assembly.Namespaces
                .SelectMany(ns => ns.Types)
                .Where(t => t.Symbol.IsStatic)
                .SelectMany(t => t.Members)
                .Where(m => m.IsExtensionMethod)
                .ToList();

            if (!extensionMethods.Any())
                return;

            // Track types we've created for external references
            var externalTypes = new Dictionary<string, DocType>();

            foreach (var extMethod in extensionMethods)
            {
                var methodSymbol = (IMethodSymbol)extMethod.Symbol;
                var extendedType = methodSymbol.Parameters.First().Type;
                var extendedTypeKey = extendedType.ToDisplayString();
                var declaringType = methodSymbol.ContainingType;

                // Find or create target DocType
                var targetType = assembly.Namespaces
                    .SelectMany(ns => ns.Types)
                    .FirstOrDefault(t => SymbolEqualityComparer.Default.Equals(t.Symbol, extendedType));

                // Create external reference if needed
                if (targetType is null && (projectContext?.CreateExternalTypeReferences ?? true))
                {
                    if (!externalTypes.TryGetValue(extendedTypeKey, out targetType))
                    {
                        targetType = CreateExternalTypeReference(
                            extendedType,
                            assembly,
                            typeMap,
                            projectContext);
                        externalTypes[extendedTypeKey] = targetType;
                    }
                }

                if (targetType is not null)
                {
                    // Remove from declaring static class
                    var staticClass = assembly.Namespaces
                        .SelectMany(ns => ns.Types)
                        .First(t => SymbolEqualityComparer.Default.Equals(t.Symbol, declaringType));

                    staticClass.Members.Remove(extMethod);

                    // Add to target type
                    targetType.Members.Add(extMethod);
                }
            }

            // Remove empty static extension classes
            foreach (var ns in assembly.Namespaces)
            {
                var emptyClasses = ns.Types
                    .Where(t => t.Symbol.IsStatic && !t.Members.Any())
                    .ToList();

                foreach (var emptyClass in emptyClasses)
                {
                    ns.Types.Remove(emptyClass);
                }
            }

            // Remove namespaces that are now empty after class removal
            var emptyNamespaces = assembly.Namespaces
                .Where(ns => !ns.Types.Any())
                .ToList();

            foreach (var emptyNs in emptyNamespaces)
            {
                assembly.Namespaces.Remove(emptyNs);
            }
        }

        /// <summary>
        /// Creates a minimal DocType for an external type that has extension methods.
        /// </summary>
        /// <param name="typeSymbol">The external type symbol.</param>
        /// <param name="assembly">The assembly being documented.</param>
        /// <param name="typeMap">Map of type names to DocType instances for type resolution.</param>
        /// <param name="projectContext">The project context.</param>
        /// <returns>A new DocType marked as an external reference.</returns>
        internal DocType CreateExternalTypeReference(
            ITypeSymbol typeSymbol,
            DocAssembly assembly,
            Dictionary<string, DocType> typeMap,
            ProjectContext? projectContext)
        {
            // If we have an error type, we can't create a proper external reference
            // This typically happens due to reference assembly conflicts (e.g., System.Runtime vs System.Private.CoreLib)
            // For now, we'll use the error type as-is, but note this in the summary
            var resolvedSymbol = typeSymbol;

            var nsName = resolvedSymbol.ContainingNamespace?.ToDisplayString() ?? "";

            // Find or create namespace
            var ns = assembly.Namespaces.FirstOrDefault(n => n.Name == nsName);
            if (ns is null)
            {
                ns = new DocNamespace(resolvedSymbol.ContainingNamespace!)
                {
                    Name = nsName,
                    DisplayName = nsName,
                    IncludedMembers = projectContext?.IncludedMembers ?? [Accessibility.Public]
                };
                assembly.Namespaces.Add(ns);
            }

            // Create minimal DocType using the resolved symbol
            var docType = new DocType(resolvedSymbol)
            {
                Name = resolvedSymbol.Name,
                FullName = resolvedSymbol.ToDisplayString(),
                DisplayName = resolvedSymbol.ToDisplayString(),
                Signature = resolvedSymbol.ToDisplayString(DocumentationSignatureFormat),
                TypeKind = resolvedSymbol.TypeKind,
                AssemblyName = resolvedSymbol.ContainingAssembly?.Name,
                IsExternalReference = true,
                IncludedMembers = projectContext?.IncludedMembers ?? [Accessibility.Public]
            };

            // Add helpful summary linking to official docs
            if (resolvedSymbol.ContainingAssembly?.Name?.StartsWith("System") == true ||
                resolvedSymbol.ContainingAssembly?.Name?.StartsWith("Microsoft") == true)
            {
                var docsUrl = GetMicrosoftDocsUrl(resolvedSymbol);
                docType.Summary = $"This type is defined in {resolvedSymbol.ContainingAssembly.Name}.";
                docType.Remarks = $"See [Microsoft documentation]({docsUrl}) for more information about the rest of the API.";
            }
            else
            {
                docType.Summary = $"This type is defined in {resolvedSymbol.ContainingAssembly?.Name ?? "external assembly"}.";
            }

            ns.Types.Add(docType);

            // Do NOT add to typeMap - external types with members would create circular references
            // when used as parameter/return types. Parameter/return types should use minimal DocType instances.

            return docType;
        }

        /// <summary>
        /// Determines if a member is accessible in a derived type.
        /// </summary>
        /// <param name="member">The member symbol to check.</param>
        /// <param name="derivedType">The derived type symbol.</param>
        /// <returns>
        /// <c>true</c> if the member is accessible from the derived type; otherwise <c>false</c>.
        /// </returns>
        internal static bool IsAccessibleInDerivedType(ISymbol member, ITypeSymbol derivedType)
        {
            return member.DeclaredAccessibility switch
            {
                Accessibility.Public => true,
                Accessibility.Protected => true,
                Accessibility.ProtectedOrInternal => true,
                Accessibility.Internal => SymbolEqualityComparer.Default.Equals(
                    member.ContainingAssembly,
                    derivedType.ContainingAssembly),
                Accessibility.ProtectedAndInternal => SymbolEqualityComparer.Default.Equals(
                    member.ContainingAssembly,
                    derivedType.ContainingAssembly),
                _ => false
            };
        }

        /// <summary>
        /// Gets the inheritance chain for a type, including the type itself and all base types.
        /// </summary>
        /// <param name="type">The type to get the inheritance chain for.</param>
        /// <returns>An enumerable of type symbols from the current type up through all base types.</returns>
        /// <remarks>
        /// This walks the inheritance chain from the current type up to System.Object,
        /// allowing access to inherited members from all levels of the hierarchy.
        /// </remarks>
        internal static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(ITypeSymbol type)
        {
            var current = type;
            while (current is not null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        /// <summary>
        /// Generates a Microsoft Learn documentation URL for a type.
        /// </summary>
        /// <param name="typeSymbol">The type symbol.</param>
        /// <returns>A URL to the Microsoft Learn documentation, or empty string if not applicable.</returns>
        internal static string GetMicrosoftDocsUrl(ITypeSymbol typeSymbol)
        {
            if (typeSymbol.ContainingAssembly?.Name?.StartsWith("System") != true &&
                typeSymbol.ContainingAssembly?.Name?.StartsWith("Microsoft") != true)
            {
                return "";
            }

            // Use a format that shows full metadata names (System.String instead of string)
            var format = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

            var fullName = typeSymbol.ToDisplayString(format)
                .Replace('<', '{')
                .Replace('>', '}')
                .ToLowerInvariant();

            return $"https://learn.microsoft.com/dotnet/api/{fullName}";
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Releases unmanaged resources and optionally releases managed resources.
        /// </summary>
        /// <param name="disposing">True to release managed and unmanaged resources; false for unmanaged only.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _compilation = null;
                Document = null;
            }

            _disposed = true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer to ensure resources are released if Dispose is not called.
        /// </summary>
        ~AssemblyManager()
        {
            Dispose(false);
        }

        #endregion

    }

}