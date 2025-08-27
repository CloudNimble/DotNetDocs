# Execution Plan for CloudNimble.DotNetDocs

This document provides a detailed, phased execution plan to implement `CloudNimble.DotNetDocs`, a robust, high-performance .NET API documentation generator. The plan ensures simplicity, maximum test coverage (100% for new code using MSTest v3, FluentAssertions, Breakdance), and leverages .NET 10 Preview 7 SDK (C# 14 features like params collections, alias any type) while multi-targeting .NET 10.0, 9.0, and 8.0. Tests use Breakdance’s real Dependency Injection (DI) approach for authentic behavior, avoiding mocks per its design ([GitHub - CloudNimble/Breakdance](https://github.com/CloudNimble/Breakdance)).

Assumptions:
- Use .NET 10 Preview 7 SDK (verify with `dotnet --version`; no global.json changes).
- Multi-target: `<TargetFrameworks>net10.0;net9.0;net8.0</TargetFrameworks>`.
- SDK-style projects, .editorconfig for formatting (newlines before braces, alphabetical ordering).
- XML comments with `<example>`/`code`; Mintlify output via `dotnet easyaf mintlify`.
- Commands specify `Configuration=Debug` or `Release`; clean temp files at phase end.
- Checkboxes track each file/task for LLM clarity.

## Phase 1: Solution Setup and Core Project
**Goal**: Establish solution, core library, and test projects with minimal dependencies. Verify with smoke tests using Breakdance.

- [x] Install .NET 10 Preview 7 SDK (if not present; verify with `dotnet --version`).
- [x] Create solution directory: `mkdir CloudNimble.DotNetDocs`.
- [x] Navigate to solution directory: `cd CloudNimble.DotNetDocs`.
- [x] Create solution file: `dotnet new slnx -n CloudNimble.DotNetDocs.slnx --configuration Debug`.
- [x] Create `.editorconfig` file in root:
  ```
  [*.cs]
  csharp_new_line_before_open_brace = all
  csharp_prefer_braces = true
  dotnet_sort_system_directives_first = true
  ```
- [x] Create `Directory.Build.props` file in root:
  ```xml
  <Project>
    <PropertyGroup>
      <PackageId Condition="'$(IsPackable)'=='true'">$(MSBuildProjectName.Replace('CloudNimble.', ''))</PackageId>
      <Nullable>enable</Nullable>
      <LangVersion>14.0</LangVersion>
    </PropertyGroup>
  </Project>
  ```
- [x] Create core project directory: `mkdir CloudNimble.DotNetDocs.Core`.
- [x] Navigate to core project: `cd CloudNimble.DotNetDocs.Core`.
- [x] Create core project file: `dotnet new classlib --framework net10.0 --configuration Debug`.
- [x] Update `CloudNimble.DotNetDocs.Core.csproj` to multi-target:
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <TargetFrameworks>net10.0;net9.0;net8.0</TargetFrameworks>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
      <IsPackable>true</IsPackable>
    </PropertyGroup>
  </Project>
  ```
- [x] Add Roslyn NuGet to core: `dotnet add package Microsoft.CodeAnalysis.CSharp --version 4.11.0-preview1`.
- [x] Add System.Text.Json NuGet to core: `dotnet add package System.Text.Json --version 8.0.4`.
- [x] Create test project directory: `mkdir ../CloudNimble.DotNetDocs.Tests.Core`.
- [x] Navigate to test project: `cd ../CloudNimble.DotNetDocs.Tests.Core`.
- [x] Create test project file: `dotnet new mstest --framework net10.0 --configuration Debug`.
- [x] Update `CloudNimble.DotNetDocs.Tests.Core.csproj` to multi-target:
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <TargetFrameworks>net10.0;net9.0;net8.0</TargetFrameworks>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
      <IsPackable>false</IsPackable>
    </PropertyGroup>
  </Project>
  ```
- [x] Add FluentAssertions NuGet to test project: `dotnet add package FluentAssertions --version 6.12.0`.
- [x] Add Breakdance NuGet to test project: `dotnet add package Breakdance.Assemblies --version 7.0.0`.
- [x] Add core reference to test project: `dotnet add reference ../CloudNimble.DotNetDocs.Core/CloudNimble.DotNetDocs.Core.csproj`.
- [x] Create `SetupTests.cs` file in test project:
  ```csharp
  using CloudNimble.Breakdance.Assemblies;
  using Microsoft.VisualStudio.TestTools.UnitTesting;

  namespace CloudNimble.DotNetDocs.Tests.Core;

  [TestClass]
  public class SetupTests : BreakdanceTestBase
  {
      [TestMethod]
      public void SmokeTest()
      {
          Assert.IsTrue(true);
      }
  }
  ```
- [x] Run tests: `dotnet test --configuration Debug`.
- [x] Clean up temporary files from Phase 1.

## Phase 2: In-Memory Model Implementation
**Goal**: Define `DocEntity` and derived classes (`DocAssembly`, `DocNamespace`, `DocType`, `DocMember`, `DocParameter`). Use C# 14 (e.g., params collections for `RelatedApis`), ensure nullable-safe design. Test with Breakdance's real DI.

✅ **COMPLETED** - All model classes implemented with full test coverage:
- DocEntity base class with all properties (Usage, Examples, BestPractices, Patterns, Considerations, RelatedApis, IncludedMembers)
- DocAssembly, DocNamespace, DocType, DocMember, DocParameter derived classes
- IncludedMembers property added to DocEntity with default [Accessibility.Public]
- Complete test suite with 100% passing tests

- [x] Navigate to core project: `cd ../CloudNimble.DotNetDocs.Core`.
- [x] Create `DocEntity.cs` file:
  ```csharp
  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;
  using Microsoft.CodeAnalysis;

  namespace CloudNimble.DotNetDocs;

  /// <summary>
  /// Base class for documentation entities, providing common contextual metadata.
  /// </summary>
  /// <remarks>
  /// Represents shared documentation properties for assemblies, namespaces, types, members, or parameters.
  /// Use to store conceptual content not directly extractable from Roslyn or XML comments.
  /// </remarks>
  public abstract class DocEntity
  {
      #region Properties

      /// <summary>
      /// Gets or sets the best practices documentation content.
      /// </summary>
      /// <value>Markdown content with best practices, recommendations, and guidelines.</value>
      [NotNull]
      public string BestPractices { get; set; } = string.Empty;

      /// <summary>
      /// Gets or sets the considerations or notes related to the current context.
      /// </summary>
      /// <value>Markdown content with gotchas, performance, or security notes.</value>
      [NotNull]
      public string Considerations { get; set; } = string.Empty;

      /// <summary>
      /// Gets or sets the examples documentation content.
      /// </summary>
      /// <value>Markdown content containing code examples and demonstrations.</value>
      [NotNull]
      public string Examples { get; set; } = string.Empty;

      /// <summary>
      /// Gets or sets the patterns documentation content.
      /// </summary>
      /// <value>Markdown content explaining common usage patterns and architectural guidance.</value>
      [NotNull]
      public string Patterns { get; set; } = string.Empty;

      /// <summary>
      /// Gets or sets a list of related API names.
      /// </summary>
      /// <value>List of fully qualified names or URLs for related APIs.</value>
      [NotNull]
      public List<string> RelatedApis { get; set; } = [];

      /// <summary>
      /// Gets or sets the usage documentation content.
      /// </summary>
      /// <value>Markdown content explaining how to use the API element.</value>
      [NotNull]
      public string Usage { get; set; } = string.Empty;

      /// <summary>
      /// Gets or sets the list of member accessibilities to include (default: Public).
      /// </summary>
      [NotNull]
      public List<Accessibility> IncludedMembers { get; set; } = [Accessibility.Public];

      #endregion
  }
  ```
- [x] Create `DocAssembly.cs` file (inherit from `DocEntity`, add `IAssemblySymbol`, `List<DocNamespace>`).
- [x] Create `DocNamespace.cs` file (inherit from `DocEntity`, add `INamespaceSymbol`, `List<DocType>`).
- [x] Create `DocType.cs` file.
- [x] Create `DocMember.cs` file (inherit from `DocEntity`, add `ISymbol` for methods/properties, `List<DocParameter>`).
- [x] Create `DocParameter.cs` file (inherit from `DocEntity`, add `IParameterSymbol`, type reference).
- [x] Navigate to test project: `cd ../CloudNimble.DotNetDocs.Tests.Core`.
- [x] Create `DocEntityTests.cs` file (test properties, defaults, IncludedMembers).
- [x] Create `DocAssemblyTests.cs` file (test constructors, null checks).
- [x] Create `DocNamespaceTests.cs` file.
- [x] Create `DocTypeTests.cs` file.
- [x] Create `DocMemberTests.cs` file.
- [x] Create `DocParameterTests.cs` file.
- [x] Run tests: `dotnet test --configuration Debug` - All tests passing.
- [x] Clean up temporary files from Phase 2.

## Phase 3: Metadata Extraction with Roslyn and XML
**Goal**: Implement `AssemblyManager` for loading, resolving, and XML integration. Multi-target compatible.

✅ **COMPLETED** - AssemblyManager implemented with IncludedMembers filtering:
- ProjectContext.cs created with IncludedMembers property and improved constructor API
- AssemblyManager.cs implemented with Roslyn/XML extraction
- IncludedMembers filtering applied to types and members throughout the hierarchy
- DocEntity.IncludedMembers cascades from assembly → namespace → type → member
- All model classes updated with proper XML parsing
- Comprehensive test suite with 100% passing tests
- IncludedMembers defaults to [Accessibility.Public] but configurable via ProjectContext constructor or property

## Phase 4: Augmentation and /conceptual Loading
**Goal**: Load conceptual content from `/conceptual` into `DocEntity` properties, including namespaces.

✅ **COMPLETED** - Conceptual loading fully implemented with namespace-based folder structure:
- LoadConceptual method implemented in AssemblyManager
- Namespace-based paths (e.g., /conceptual/System/Text/Json/JsonSerializer/)
- Support for all DocEntity properties (Usage, Examples, BestPractices, Patterns, Considerations, RelatedApis)
- Member and parameter-specific documentation loading
- Comprehensive test coverage with AugmentationTests (6 test methods)
- All 135 tests passing

- [x] Navigate to core project: `cd ../CloudNimble.DotNetDocs.Core`.
- [x] Update `AssemblyManager.cs` to call `LoadConceptual` in `BuildModel`.
- [x] Implement `LoadConceptual` method in `AssemblyManager.cs` to parse /conceptual files (support namespace-level, e.g., `conceptual/MyNamespace/usage.md`, and type/member levels).
- [x] Update `DocNamespace.cs` to handle conceptual mapping.
- [x] Update `DocType.cs` to handle conceptual mapping.
- [x] Update `DocMember.cs` to handle conceptual mapping.
- [x] Navigate to test project: `cd ../CloudNimble.DotNetDocs.Tests.Core`.
- [x] Create `/conceptual/MyNamespace/usage.md` sample file (created dynamically in tests).
- [x] Create `/conceptual/SampleClass/usage.md` sample file (created dynamically in tests).
- [x] Create `AugmentationTests.cs` file with tests for loading namespaces, types, and members, missing files (empty strings).
- [x] Run tests: `dotnet test --configuration Debug` - All 135 tests passing.
- [x] Clean up /conceptual samples from Phase 4 (tests clean up automatically).

## Phase 4.5: Adjustments for DocumentationManager and IncludedMembers
**Goal**: Implement `DocumentationManager` as pipeline orchestrator, add `IncludedMembers` to `DocEntity`, switch to JSON for settings.

✅ **COMPLETED** - DocumentationManager pipeline fully implemented:
- Created IDocEnricher, IDocTransformer, IDocRenderer interfaces
- Implemented DocumentationManager as pipeline orchestrator
- Moved LoadConceptual logic from AssemblyManager to DocumentationManager
- Made all file I/O operations async for better performance
- Created DotNetDocsConstants class to eliminate magic strings
- Added OutputPath and CustomSettings to ProjectContext
- Created comprehensive DocumentationManagerTests with 100% coverage
- All 132 tests passing

- [x] Navigate to core project: `cd ../CloudNimble.DotNetDocs.Core`.
- [x] Create `IDocEnricher.cs` file:
  ```csharp
  using System.Threading.Tasks;

  namespace CloudNimble.DotNetDocs;

  /// <summary>
  /// Defines an enricher for conceptual documentation augmentation.
  /// </summary>
  public interface IDocEnricher
  {
      Task EnrichAsync(DocEntity entity, EnrichmentContext context);
  }
  ```
- [x] Create `EnrichmentContext.cs` file:
  ```csharp
  using System.Diagnostics.CodeAnalysis;

  namespace CloudNimble.DotNetDocs;

  /// <summary>
  /// Context for enrichment, including conceptual path and settings.
  /// </summary>
  public class EnrichmentContext
  {
      public string ConceptualPath { get; init; } = string.Empty;
      public object Settings { get; init; } = new();
  }
  ```
- [x] Create `DocumentationManager.cs` file:
  ```csharp
  using System.Collections.Generic;
  using System.Threading.Tasks;

  namespace CloudNimble.DotNetDocs;

  /// <summary>
  /// Orchestrates the documentation pipeline for one or more assemblies.
  /// </summary>
  public class DocumentationManager
  {
      private readonly IEnumerable<IDocEnricher> _enrichers;
      private readonly IEnumerable<IDocTransformer> _transformers;
      private readonly IEnumerable<IDocRenderer> _renderers;

      public DocumentationManager(IEnumerable<IDocEnricher> enrichers, IEnumerable<IDocTransformer> transformers, IEnumerable<IDocRenderer> renderers)
      {
          _enrichers = enrichers ?? throw new ArgumentNullException(nameof(enrichers));
          _transformers = transformers ?? throw new ArgumentNullException(nameof(transformers));
          _renderers = renderers ?? throw new ArgumentNullException(nameof(renderers));
      }

      public async Task ProcessAsync(string assemblyPath, string xmlPath, ProjectContext? projectContext = null)
      {
          using var manager = new AssemblyManager(assemblyPath, xmlPath);
          var model = await manager.DocumentAsync(projectContext);

          foreach (var enricher in _enrichers)
              await enricher.EnrichAsync(model, new EnrichmentContext { ConceptualPath = projectContext?.ConceptualPath });

          foreach (var transformer in _transformers)
          {
              await TransformModelAsync(model, transformer, projectContext);
          }

          foreach (var renderer in _renderers)
              await renderer.RenderAsync(model, projectContext?.OutputPath ?? "docs", new TransformationContext { CustomSettings = projectContext?.CustomSettings });
      }

      public async Task ProcessAsync(IEnumerable<(string assemblyPath, string xmlPath)> assemblies, ProjectContext? projectContext = null)
      {
          var tasks = assemblies.Select(async pair =>
          {
              using var manager = new AssemblyManager(pair.assemblyPath, pair.xmlPath);
              var model = await manager.DocumentAsync(projectContext);
              foreach (var enricher in _enrichers)
                  await enricher.EnrichAsync(model, new EnrichmentContext { ConceptualPath = projectContext?.ConceptualPath });
              foreach (var transformer in _transformers)
              {
                  await TransformModelAsync(model, transformer, projectContext);
              }
              foreach (var renderer in _renderers)
                  await renderer.RenderAsync(model, projectContext?.OutputPath ?? "docs", new TransformationContext { CustomSettings = projectContext?.CustomSettings });
          });
          await Task.WhenAll(tasks);
      }
  }
  ```
- [x] Create `TransformationContext.cs` file:
  ```csharp
  using System.Collections.Generic;

  namespace CloudNimble.DotNetDocs;

  /// <summary>
  /// Context for transformation, including configuration and output settings.
  /// </summary>
  public class TransformationContext
  {
      public object CustomSettings { get; init; } = new();
  }
  ```
- [x] Update `DocEntity.cs` to add `IncludedMembers`:
  ```csharp
  // Add to existing DocEntity properties
  /// <summary>
  /// Gets or sets the list of member accessibilities to include (default: Public).
  /// </summary>
  [NotNull]
  public List<Accessibility> IncludedMembers { get; set; } = [Accessibility.Public];
  ```
- [x] Update `AssemblyManager.cs` to use `IncludedMembers` in `BuildDocType` for filtering `GetMembers()`:
  ```csharp
  // Update BuildDocType method
  private DocType BuildDocType(ITypeSymbol type, Compilation compilation, Dictionary<string, DocType> typeMap)
  {
      var docType = new DocType(type)
      {
          Usage = GetXmlComment(type.GetDocumentationCommentXml()),
          Examples = GetXmlComment(type.GetDocumentationCommentXml(), "example"),
          BestPractices = GetXmlComment(type.GetDocumentationCommentXml(), "remarks")
      };

      typeMap[type.ToDisplayString()] = docType;

      if (type.BaseType is not null && type.BaseType.SpecialType != SpecialType.System_Object)
      {
          var baseTypeKey = type.BaseType.ToDisplayString();
          if (typeMap.TryGetValue(baseTypeKey, out var baseDocType))
              docType.BaseType = baseDocType;
          else
          {
              var baseDocType = new DocType(type.BaseType);
              typeMap[baseTypeKey] = baseDocType;
              docType.BaseType = baseDocType;
          }
      }

      foreach (var member in type.GetMembers().Where(m => docType.IncludedMembers.Contains(m.DeclaredAccessibility)))
      {
          var docMember = member switch
          {
              IMethodSymbol method => new DocMember(method)
              {
                  Usage = GetXmlComment(method.GetDocumentationCommentXml()),
                  Examples = GetXmlComment(method.GetDocumentationCommentXml(), "example"),
                  Parameters = method.Parameters.Select(p => new DocParameter(p)
                  {
                      Usage = GetXmlComment(p.GetDocumentationCommentXml())
                  }).ToList()
              },
              IPropertySymbol property => new DocMember(property)
              {
                  Usage = GetXmlComment(property.GetDocumentationCommentXml())
              },
              _ => null
          };

          if (docMember is not null)
              docType.Members.Add(docMember);
      }

      docType.RelatedApis = type.AllInterfaces.Select(i => i.ToDisplayString()).ToList();

      return docType;
  }
  ```
- [x] Update `AssemblyManager.cs` to support namespace conceptual (e.g., `conceptual/MyNamespace/usage.md`):
  ```csharp
  // Update LoadConceptual method
  private void LoadConceptual(DocAssembly assembly, string conceptualPath)
  {
      ArgumentException.ThrowIfNullOrWhiteSpace(conceptualPath);

      if (!Directory.Exists(conceptualPath))
          return;

      foreach (var ns in assembly.Namespaces)
      {
          var nsDir = Path.Combine(conceptualPath, ns.Symbol.Name.Replace(".", "/"));
          if (Directory.Exists(nsDir))
          {
              if (File.Exists(Path.Combine(nsDir, "usage.md")))
                  ns.Usage = File.ReadAllText(Path.Combine(nsDir, "usage.md"));
              if (File.Exists(Path.Combine(nsDir, "examples.md")))
                  ns.Examples = File.ReadAllText(Path.Combine(nsDir, "examples.md"));
              if (File.Exists(Path.Combine(nsDir, "best-practices.md")))
                  ns.BestPractices = File.ReadAllText(Path.Combine(nsDir, "best-practices.md"));
              if (File.Exists(Path.Combine(nsDir, "patterns.md")))
                  ns.Patterns = File.ReadAllText(Path.Combine(nsDir, "patterns.md"));
              if (File.Exists(Path.Combine(nsDir, "considerations.md")))
                  ns.Considerations = File.ReadAllText(Path.Combine(nsDir, "considerations.md"));
              if (File.Exists(Path.Combine(nsDir, "related-apis.yaml")))
              {
                  ns.RelatedApis = ParseRelatedApisYaml(Path.Combine(nsDir, "related-apis.yaml"));
              }
          }

          foreach (var type in ns.Types)
          {
              var typeDir = Path.Combine(nsDir, type.Symbol.Name);
              if (Directory.Exists(typeDir))
              {
                  if (File.Exists(Path.Combine(typeDir, "usage.md")))
                      type.Usage = File.ReadAllText(Path.Combine(typeDir, "usage.md"));
                  if (File.Exists(Path.Combine(typeDir, "examples.md")))
                      type.Examples = File.ReadAllText(Path.Combine(typeDir, "examples.md"));
                  if (File.Exists(Path.Combine(typeDir, "best-practices.md")))
                      type.BestPractices = File.ReadAllText(Path.Combine(typeDir, "best-practices.md"));
                  if (File.Exists(Path.Combine(typeDir, "patterns.md")))
                      type.Patterns = File.ReadAllText(Path.Combine(typeDir, "patterns.md"));
                  if (File.Exists(Path.Combine(typeDir, "considerations.md")))
                      type.Considerations = File.ReadAllText(Path.Combine(typeDir, "considerations.md"));
                  if (File.Exists(Path.Combine(typeDir, "related-apis.yaml")))
                  {
                      type.RelatedApis = ParseRelatedApisYaml(Path.Combine(typeDir, "related-apis.yaml"));
                  }

                  foreach (var member in type.Members)
                  {
                      var memberDir = Path.Combine(typeDir, member.Symbol.Name);
                      if (Directory.Exists(memberDir))
                      {
                          if (File.Exists(Path.Combine(memberDir, "usage.md")))
                              member.Usage = File.ReadAllText(Path.Combine(memberDir, "usage.md"));
                          if (File.Exists(Path.Combine(memberDir, "examples.md")))
                              member.Examples = File.ReadAllText(Path.Combine(memberDir, "examples.md"));
                      }
                  }
              }
          }
      }
  }
  ```
- [ ] Create `CustomizationSettings.cs` file:
  ```csharp
  using System.Text.Json.Serialization;

  namespace CloudNimble.DotNetDocs;

  /// <summary>
  /// Represents settings for customization rules in JSON format.
  /// </summary>
  public class CustomizationSettings
  {
      [JsonPropertyName("root")]
      public RootSettings? Root { get; set; }

      [JsonPropertyName("namespaces")]
      public Dictionary<string, NamespaceSettings>? Namespaces { get; set; }

      [JsonPropertyName("pages")]
      public Dictionary<string, PageSettings>? Pages { get; set; }
  }

  public class RootSettings
  {
      [JsonPropertyName("excludePrivate")]
      public bool? ExcludePrivate { get; set; }

      [JsonPropertyName("transformations")]
      public Dictionary<string, string>? Transformations { get; set; }
  }

  public class NamespaceSettings
  {
      [JsonPropertyName("title")]
      public string? Title { get; set; }

      [JsonPropertyName("icon")]
      public string? Icon { get; set; }

      [JsonPropertyName("exclusions")]
      public ExclusionSettings? Exclusions { get; set; }
  }

  public class PageSettings
  {
      [JsonPropertyName("overrides")]
      public Dictionary<string, string>? Overrides { get; set; }

      [JsonPropertyName("conditions")]
      public Dictionary<string, Dictionary<string, string>>? Conditions { get; set; }
  }

  public class ExclusionSettings
  {
      [JsonPropertyName("members")]
      public List<string>? Members { get; set; }
  }
  ```
- [x] Navigate to test project: `cd ../CloudNimble.DotNetDocs.Tests.Core`.
- [x] Create `DocumentationManagerTests.cs` file with tests for orchestration, multi-assembly support.
- [x] Create `DocEntityIncludedMembersTests.cs` file with tests for filtering.
- [x] Run tests: `dotnet test --configuration Debug`.
- [x] Clean up temporary files from Phase 4.5.

## Phase 5: Transformer Implementation
**Goal**: Implement recursive transformation pipeline for customizations (insertions, overrides, exclusions, transformations, conditions).

- [ ] Navigate to core project: `cd ../CloudNimble.DotNetDocs.Core`.
- [ ] Create `IDocTransformer.cs` file (updated to accept DocEntity instead of DocAssembly).
- [ ] Create `InsertConceptualTransformer.cs` file.
- [ ] Create `OverrideTitleTransformer.cs` file.
- [ ] Create `ExcludePrivateTransformer.cs` file.
- [ ] Create `TransformExamplesTransformer.cs` file.
- [ ] Create `ConditionalObsoleteTransformer.cs` file.
- [ ] Create `IDocRenderer.cs` file.
- [ ] Create `MarkdownRenderer.cs` file.
- [ ] Create `JsonRenderer.cs` file.
- [ ] Create `YamlRenderer.cs` file.
- [ ] Update `DocumentationManager.cs` to apply transformers recursively like enrichers.
- [ ] Navigate to test project.
- [ ] Create transformer and renderer tests.
- [ ] Run tests: `dotnet test --configuration Debug`.

## Phase 6: CLI and MSBuild Integration
**Goal**: Implement CLI (`dotnet docs generate`) and MSBuild task.

- [ ] Create Tools project.
- [ ] Implement CLI commands.
- [ ] Create MSBuild project.
- [ ] Implement MSBuild tasks.
- [ ] Create tests.
- [ ] Run tests.

## Phase 7: Plugins.AI, Mintlify/Docusaurus, and Optimization
**Goal**: Add AI-powered extensibility using Semantic Kernel and tool-specific outputs.

### Mintlify.Core Foundation ✅ **COMPLETED**
- [x] Create Mintlify.Core project - COMPLETED (foundational library for docs.json)
- [x] Implement DocsJsonConfig model with full schema support - COMPLETED
- [x] Implement DocsJsonManager with merge functionality - COMPLETED
- [x] Implement DocsJsonValidator for schema validation - COMPLETED
- [x] Add MergeOptions with CombineEmptyGroups feature - COMPLETED
- [x] Create comprehensive test suite with 100% coverage - COMPLETED
- [x] Add custom JSON converters for complex navigation types - COMPLETED

### CloudNimble.DotNetDocs.Mintlify Renderer (Still Needed)
- [ ] Create MintlifyRenderer.cs - Transform DocAssembly to Mintlify MDX format
- [ ] Create MintlifyNavigationBuilder.cs - Build navigation from DocAssembly structure
- [ ] Create MintlifyPageGenerator.cs - Generate individual MDX pages
- [ ] Create tests for Mintlify renderer

### AI Plugins with Semantic Kernel
- [ ] Create Plugins.AI project directory: `mkdir ../CloudNimble.DotNetDocs.Plugins.AI`.
- [ ] Navigate to Plugins.AI project: `cd ../CloudNimble.DotNetDocs.Plugins.AI`.
- [ ] Create Plugins.AI project file: `dotnet new classlib --framework net10.0 --configuration Debug`.
- [ ] Update `CloudNimble.DotNetDocs.Plugins.AI.csproj` to multi-target.
- [ ] Add Semantic Kernel NuGet: `dotnet add package Microsoft.SemanticKernel`.
- [ ] Add Kernel Memory NuGet (optional): `dotnet add package Microsoft.KernelMemory.Core`.
- [ ] Create `IDocEnricher.cs` file.
- [ ] Create `SemanticKernelEnricher.cs` file - Allows users to configure:
  - AI model selection (OpenAI, Azure OpenAI, local models, etc.)
  - Embedding model selection for semantic search
  - Custom prompts for documentation generation
  - Temperature and other parameters
- [ ] Create `DocumentationEnhancer.cs` - Uses SK to:
  - Generate missing examples from method signatures
  - Enhance usage documentation with best practices
  - Create related API suggestions using embeddings
  - Generate considerations based on code patterns
- [ ] Create `KernelMemoryIndexer.cs` (optional) - Uses Kernel Memory to:
  - Index existing documentation for semantic search
  - Find similar code patterns across assemblies
  - Suggest documentation based on similar APIs

### Docusaurus Support  
- [ ] Create Docusaurus project directory: `mkdir ../CloudNimble.DotNetDocs.Docusaurus`.
- [ ] Navigate to Docusaurus project: `cd ../CloudNimble.DotNetDocs.Docusaurus`.
- [ ] Create Docusaurus project file: `dotnet new classlib --framework net10.0 --configuration Debug`.
- [ ] Update `CloudNimble.DotNetDocs.Docusaurus.csproj` to multi-target.
- [ ] Create `DocusaurusRenderer.cs` file.

### Performance Optimization
- [ ] Add BenchmarkDotNet NuGet to test project.
- [ ] Create performance tests for extraction, rendering.
- [ ] Run tests: `dotnet test --configuration Debug`.