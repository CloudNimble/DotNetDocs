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
            if (element == null)
                return null;

            // Get all nodes and concatenate them, preserving XML tags
            var innerXml = string.Concat(element.Nodes().Select(n => n.ToString()));
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
        /// Extracts the remarks/best practices text from XML documentation, preserving inner XML tags.
        /// </summary>
        /// <param name="doc">The parsed XML documentation.</param>
        /// <returns>The remarks text with preserved XML tags, or empty string if not found.</returns>
        internal string? ExtractRemarks(XDocument? doc) =>
            ExtractInnerXml(doc?.Descendants("remarks").FirstOrDefault());

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

            return docAssembly;
        }

        /// <summary>
        /// Builds a <see cref="DocType"/> from a Roslyn type symbol, resolving members and base types.
        /// </summary>
        /// <param name="type">The Roslyn type symbol.</param>
        /// <param name="compilation">The Roslyn compilation for symbol resolution.</param>
        /// <param name="typeMap">Cache of resolved types for linking.</param>
        /// <param name="includedMembers">List of member accessibilities to include.</param>
        /// <returns>The <see cref="DocType"/> instance.</returns>
        internal DocType BuildDocType(ITypeSymbol type, Compilation compilation, Dictionary<string, DocType> typeMap, List<Accessibility> includedMembers)
        {
            var doc = ExtractDocumentationXml(type);

            var docType = new DocType(type)
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
            docType.IncludedMembers = includedMembers;

            typeMap[type.ToDisplayString()] = docType;

            // Resolve base type
            if (type.BaseType is not null && type.BaseType.SpecialType != SpecialType.System_Object)
            {
                docType.BaseType = type.BaseType.ToDisplayString();
            }

            // Set related APIs (e.g., all interfaces as strings)
            docType.RelatedApis = type.AllInterfaces.Select(i => i.ToDisplayString()).ToList();

            // Resolve members
            // The bridge compilation with IgnoresAccessChecksTo allows us to see all members including internals
            foreach (var member in type.GetMembers().Where(m => docType.IncludedMembers.Contains(m.DeclaredAccessibility) && !m.IsImplicitlyDeclared))
            {
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

                                 // Parameter type
                                 var pTypeKey = p.Type.ToDisplayString();
                                 if (!typeMap.TryGetValue(pTypeKey, out var pTypeDoc))
                                 {
                                     pTypeDoc = new DocType(p.Type)
                                     {
                                         Name = p.Type.Name,
                                         FullName = p.Type.ToDisplayString(),
                                         DisplayName = p.Type.ToDisplayString(),
                                         Signature = p.Type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                                         TypeKind = p.Type.TypeKind,
                                         AssemblyName = p.Type.ContainingAssembly?.Name,
                                         IncludedMembers = docType.IncludedMembers
                                     };
                                     typeMap[pTypeKey] = pTypeDoc;
                                 }
                                 paramDoc.ParameterType = pTypeDoc;

                                 return paramDoc;
                             }).ToList()
                     };

                     // Return type
                     if (method.ReturnType.SpecialType != SpecialType.System_Void)
                     {
                         var rKey = method.ReturnType.ToDisplayString();
                         if (!typeMap.TryGetValue(rKey, out var rDoc))
                         {
                            rDoc = new DocType(method.ReturnType)
                            {
                                Name = method.ReturnType.Name,
                                FullName = method.ReturnType.ToDisplayString(),
                                DisplayName = method.ReturnType.ToDisplayString(),
                                Signature = method.ReturnType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                                TypeKind = method.ReturnType.TypeKind,
                                AssemblyName = method.ReturnType.ContainingAssembly?.Name,
                                IncludedMembers = docType.IncludedMembers
                            };
                            typeMap[rKey] = rDoc;
                         }
                         docMember.ReturnType = rDoc;
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
                         IncludedMembers = docType.IncludedMembers
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
                         IncludedMembers = docType.IncludedMembers
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
                         IncludedMembers = docType.IncludedMembers
                     };
                 }

                if (docMember is not null)
                {
                    docType.Members.Add(docMember);
                }
            }

            return docType;
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
            var documentationProvider = XmlPath != null && File.Exists(XmlPath) 
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
                        var docType = BuildDocType(type, compilation, typeMap, includedMembers);
                        docNs.Types.Add(docType);
                    }
                }

                // Recurse into nested namespaces regardless of whether this one had types
                ProcessNamespaceRecursive(ns, docAssembly, compilation, typeMap, includedMembers, projectContext);
            }
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