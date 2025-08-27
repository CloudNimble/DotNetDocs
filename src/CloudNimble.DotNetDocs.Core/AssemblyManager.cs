using System;
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
    /// <remarks>
    /// Extracts metadata from a single .NET assembly and its XML documentation file, building an in-memory model
    /// (<see cref="DocAssembly"/>) with interconnected types, members, and parameters. Supports conceptual content
    /// loading from a specified folder. Designed for multi-targeting .NET 10.0, 9.0, and 8.0. One instance per assembly
    /// is required, with paths specified at construction for incremental build support. Implements <see cref="IDisposable"/>
    /// to release memory used by the compilation and model.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using var manager = new AssemblyManager("MyLib.dll", "MyLib.xml");
    /// var context = new ProjectContext("ref1.dll", "ref2.dll") { ConceptualPath = "conceptual" };
    /// var model = await manager.DocumentAsync(context);
    /// ]]>
    /// </code>
    /// </example>
    public class AssemblyManager : IDisposable
    {

        #region Fields

        private Compilation? _compilation;
        private bool _disposed;

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
        /// Gets the last modified timestamp of the assembly file for incremental builds.
        /// </summary>
        public DateTime LastModified { get; private set; }

        /// <summary>
        /// Gets the path to the XML documentation file.
        /// </summary>
        public string XmlPath { get; init; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="AssemblyManager"/> for a specific assembly.
        /// </summary>
        /// <param name="assemblyPath">Path to the assembly DLL file.</param>
        /// <param name="xmlPath">Path to the XML documentation file.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="assemblyPath"/> or <paramref name="xmlPath"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="assemblyPath"/> or <paramref name="xmlPath"/> is empty or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown when <paramref name="assemblyPath"/> or <paramref name="xmlPath"/> does not exist.</exception>
        public AssemblyManager(string assemblyPath, string xmlPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
            ArgumentException.ThrowIfNullOrWhiteSpace(xmlPath);

            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException("Assembly file not found.", nameof(assemblyPath));
            }
            if (!File.Exists(xmlPath))
            {
                throw new FileNotFoundException("XML documentation file not found.", nameof(xmlPath));
            }

            AssemblyPath = assemblyPath;
            XmlPath = xmlPath;
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
            if (Document is null || currentModified > LastModified)
            {
                _compilation = await CreateCompilationAsync(projectContext?.References ?? []);
                Document = BuildModel(_compilation, projectContext?.ConceptualPath, projectContext?.IncludedMembers ?? [Accessibility.Public]);
                LastModified = currentModified;
            }
            return Document;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Builds the in-memory documentation model from the Roslyn compilation.
        /// </summary>
        /// <param name="compilation">The Roslyn compilation containing assembly metadata.</param>
        /// <param name="conceptualPath">Optional path to conceptual documentation files.</param>
        /// <param name="includedMembers">List of member accessibilities to include.</param>
        /// <returns>The <see cref="DocAssembly"/> model.</returns>
        private DocAssembly BuildModel(Compilation compilation, string? conceptualPath, List<Accessibility> includedMembers)
        {
            var targetRef = compilation.References.OfType<PortableExecutableReference>().FirstOrDefault(r => string.Equals(r.FilePath, AssemblyPath, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Target assembly reference not found in compilation.");

            var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(targetRef) as IAssemblySymbol
                ?? throw new InvalidOperationException("Could not get assembly symbol from compilation.");

            var assemblyXml = assemblySymbol.GetDocumentationCommentXml() ?? string.Empty;
            var assemblyDoc = string.IsNullOrWhiteSpace(assemblyXml) ? null : XDocument.Parse(assemblyXml);

            var docAssembly = new DocAssembly(assemblySymbol)
            {
                Usage = assemblyDoc?.Descendants("summary").FirstOrDefault()?.Value.Trim() ?? string.Empty,
                IncludedMembers = includedMembers
            };

            var typeMap = new Dictionary<string, DocType>(); // Cache for type resolutions

            // Process all namespaces recursively
            ProcessNamespace(assemblySymbol.GlobalNamespace, docAssembly, compilation, typeMap, includedMembers);

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
        private DocType BuildDocType(ITypeSymbol type, Compilation compilation, Dictionary<string, DocType> typeMap, List<Accessibility> includedMembers)
        {
            var xml = type.GetDocumentationCommentXml() ?? string.Empty;
            var doc = string.IsNullOrWhiteSpace(xml) ? null : XDocument.Parse(xml);

            var docType = new DocType(type)
            {
                Usage = doc?.Descendants("summary").FirstOrDefault()?.Value.Trim() ?? string.Empty,
                Examples = doc?.Descendants("example").FirstOrDefault()?.Value.Trim() ?? string.Empty,
                BestPractices = doc?.Descendants("remarks").FirstOrDefault()?.Value.Trim() ?? string.Empty
            };
            docType.IncludedMembers = includedMembers;

            typeMap[type.ToDisplayString()] = docType;

            // Resolve base type
            if (type.BaseType is not null && type.BaseType.SpecialType != SpecialType.System_Object)
            {
                var baseTypeKey = type.BaseType.ToDisplayString();
                if (!typeMap.TryGetValue(baseTypeKey, out var baseDocType))
                {
                    baseDocType = new DocType(type.BaseType);
                    typeMap[baseTypeKey] = baseDocType;
                }
                docType.BaseType = baseDocType;
            }

            // Resolve implemented interfaces
            foreach (var iface in type.Interfaces)
            {
                var ifaceKey = iface.ToDisplayString();
                if (!typeMap.TryGetValue(ifaceKey, out var ifaceDoc))
                {
                    ifaceDoc = new DocType(iface);
                    typeMap[ifaceKey] = ifaceDoc;
                }
                docType.ImplementedInterfaces.Add(ifaceDoc);
            }

            // Set related APIs (e.g., all interfaces as strings)
            docType.RelatedApis = type.AllInterfaces.Select(i => i.ToDisplayString()).ToList();

            // Resolve members
            foreach (var member in type.GetMembers().Where(m => docType.IncludedMembers.Contains(m.DeclaredAccessibility) && !m.IsImplicitlyDeclared))
            {
                DocMember? docMember = null;

                if (member is IMethodSymbol method)
                {
                    var mXml = method.GetDocumentationCommentXml() ?? string.Empty;
                    var mDoc = string.IsNullOrWhiteSpace(mXml) ? null : XDocument.Parse(mXml);

                    docMember = new DocMember(method)
                    {
                        Usage = mDoc?.Descendants("summary").FirstOrDefault()?.Value.Trim() ?? string.Empty,
                        Examples = mDoc?.Descendants("example").FirstOrDefault()?.Value.Trim() ?? string.Empty,
                        BestPractices = mDoc?.Descendants("remarks").FirstOrDefault()?.Value.Trim() ?? string.Empty,
                        // Parameters
                        Parameters = method.Parameters.Select(p =>
                            {
                                var paramDoc = new DocParameter(p);
                                var paramElem = mDoc?.Descendants("param").FirstOrDefault(e => e.Attribute("name")?.Value == p.Name);
                                paramDoc.Usage = paramElem?.Value.Trim() ?? string.Empty;

                                // Parameter type
                                var pTypeKey = p.Type.ToDisplayString();
                                if (!typeMap.TryGetValue(pTypeKey, out var pTypeDoc))
                                {
                                    pTypeDoc = new DocType(p.Type);
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
                            rDoc = new DocType(method.ReturnType);
                            typeMap[rKey] = rDoc;
                        }
                        docMember.ReturnType = rDoc;
                    }
                }
                else if (member is IPropertySymbol property)
                {
                    var pXml = property.GetDocumentationCommentXml() ?? string.Empty;
                    var pDoc = string.IsNullOrWhiteSpace(pXml) ? null : XDocument.Parse(pXml);

                    docMember = new DocMember(property)
                    {
                        Usage = pDoc?.Descendants("summary").FirstOrDefault()?.Value.Trim() ?? string.Empty,
                        Examples = pDoc?.Descendants("example").FirstOrDefault()?.Value.Trim() ?? string.Empty,
                        BestPractices = pDoc?.Descendants("remarks").FirstOrDefault()?.Value.Trim() ?? string.Empty
                    };
                }
                // Add handling for other member kinds (fields, events) if needed

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
        private async Task<Compilation> CreateCompilationAsync(IEnumerable<string> references)
        {
            var metadataReference = MetadataReference.CreateFromFile(AssemblyPath, documentation: XmlDocumentationProvider.CreateFromFile(XmlPath));

            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create(AssemblyName)
                .WithOptions(compilationOptions)
                .AddReferences(metadataReference);

            foreach (var refPath in references)
            {
                if (File.Exists(refPath))
                {
                    compilation = compilation.AddReferences(MetadataReference.CreateFromFile(refPath));
                }
            }

            // Add common .NET references (e.g., System.Runtime) for resolution
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
        private void ProcessNamespace(INamespaceSymbol namespaceSymbol, DocAssembly docAssembly, Compilation compilation, Dictionary<string, DocType> typeMap, List<Accessibility> includedMembers)
        {
            // Process types in the global namespace
            if (namespaceSymbol.IsGlobalNamespace)
            {
                var hasTypesToDocument = namespaceSymbol.GetTypeMembers().Any(t => includedMembers.Contains(t.DeclaredAccessibility) || t.Name == "<Module>");
                if (hasTypesToDocument)
                {
                    var nsXml = namespaceSymbol.GetDocumentationCommentXml() ?? string.Empty;
                    var nsDoc = string.IsNullOrWhiteSpace(nsXml) ? null : XDocument.Parse(nsXml);

                    var globalNs = new DocNamespace(namespaceSymbol)
                    {
                        Usage = nsDoc?.Descendants("summary").FirstOrDefault()?.Value.Trim() ?? string.Empty,
                        IncludedMembers = includedMembers
                    };
                    docAssembly.Namespaces.Add(globalNs);

                    foreach (var type in namespaceSymbol.GetTypeMembers().Where(t => includedMembers.Contains(t.DeclaredAccessibility) || t.Name == "<Module>"))
                    {
                        var docType = BuildDocType(type, compilation, typeMap, includedMembers);
                        globalNs.Types.Add(docType);
                    }
                }
            }

            // Process nested namespaces
            foreach (var ns in namespaceSymbol.GetNamespaceMembers())
            {
                // Skip empty namespaces (check for types or nested namespaces)
                if (!ns.GetTypeMembers().Any() && !ns.GetNamespaceMembers().Any())
                {
                    continue;
                }

                var nsXml = ns.GetDocumentationCommentXml() ?? string.Empty;
                var nsDoc = string.IsNullOrWhiteSpace(nsXml) ? null : XDocument.Parse(nsXml);

                var docNs = new DocNamespace(ns)
                {
                    Usage = nsDoc?.Descendants("summary").FirstOrDefault()?.Value.Trim() ?? string.Empty,
                    IncludedMembers = includedMembers
                };
                docAssembly.Namespaces.Add(docNs);

                // Process types in this namespace
                foreach (var type in ns.GetTypeMembers().Where(t => includedMembers.Contains(t.DeclaredAccessibility)))
                {
                    var docType = BuildDocType(type, compilation, typeMap, includedMembers);
                    docNs.Types.Add(docType);
                }

                // Recurse into nested namespaces
                ProcessNamespace(ns, docAssembly, compilation, typeMap, includedMembers);
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