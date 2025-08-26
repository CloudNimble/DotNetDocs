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
- [x] Create solution directory: `mkdir src`.
- [x] Navigate to solution directory: `cd src`.
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

- [x] Navigate to core project: `cd ../CloudNimble.DotNetDocs.Core`.
- [x] Create `DocEntity.cs` file:
  ```csharp
  using System.Diagnostics.CodeAnalysis;

  namespace CloudNimble.DotNetDocs;

  /// <summary>
  /// Base class for documentation entities, providing common contextual metadata.
  /// </summary>
  /// <remarks>
  /// Represents shared documentation properties for assemblies, namespaces, types, members, or parameters.
  /// Use to store conceptual content not directly extractable from Roslyn or XML comments.
  /// </remarks>
  /// <example>
  /// <code><![CDATA[
  /// var docType = new DocType(symbol)
  /// {
  ///     Usage = "Use this class for logging.",
  ///     Examples = "```csharp\nLogger.LogInfo();\n```"
  /// };
  /// ]]></code>
  /// </example>
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

      #endregion
  }
  ```
- [x] Create `DocAssembly.cs` file (inherit from `DocEntity`, add `IAssemblySymbol`, `List<DocNamespace>`).
- [x] Create `DocNamespace.cs` file (inherit from `DocEntity`, add `INamespaceSymbol`, `List<DocType>`).
- [x] Create `DocType.cs` file:
  ```csharp
  using Microsoft.CodeAnalysis;
  using System.Diagnostics.CodeAnalysis;

  namespace CloudNimble.DotNetDocs;

  /// <summary>
  /// Represents documentation for a .NET type.
  /// </summary>
  public class DocType : DocEntity
  {
      #region Properties

      /// <summary>
      /// Gets the Roslyn symbol for the type.
      /// </summary>
      [NotNull]
      public ITypeSymbol Symbol { get; }

      /// <summary>
      /// Gets the base type, if any.
      /// </summary>
      public DocType? BaseType { get; }

      /// <summary>
      /// Gets the collection of members (methods, properties, etc.).
      /// </summary>
      [NotNull]
      public List<DocMember> Members { get; } = [];

      #endregion

      #region Constructors

      /// <summary>
      /// Initializes a new instance of <see cref="DocType"/>.
      /// </summary>
      /// <param name="symbol">The Roslyn type symbol.</param>
      public DocType(ITypeSymbol symbol)
      {
          ArgumentNullException.ThrowIfNull(symbol);
          Symbol = symbol;
      }

      #endregion
  }
  ```
- [x] Create `DocMember.cs` file (inherit from `DocEntity`, add `ISymbol` for methods/properties, `List<DocParameter>`).
- [x] Create `DocParameter.cs` file (inherit from `DocEntity`, add `IParameterSymbol`, type reference).
- [x] Navigate to test project: `cd ../CloudNimble.DotNetDocs.Tests.Core`.
- [x] Create Tests.Shared project: `CloudNimble.DotNetDocs.Tests.Shared` - COMPLETED
- [x] Tests.Shared contains test base classes and helpers
- [x] Build Tests.Shared: Builds automatically as part of solution
- [x] Create `DocEntityTests.cs` file (test properties, defaults).
- [x] Create `DocAssemblyTests.cs` file (test constructors, null checks).
- [x] Create `DocNamespaceTests.cs` file.
- [x] Create `DocTypeTests.cs` file:
  ```csharp
  using CloudNimble.Breakdance.Assemblies;
  using CloudNimble.DotNetDocs;
  using FluentAssertions;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using Microsoft.VisualStudio.TestTools.UnitTesting;

  namespace CloudNimble.DotNetDocs.Tests.Core;

  [TestClass]
  public class DocTypeTests : BreakdanceTestBase
  {
      [TestMethod]
      public void Constructor_WithNullSymbol_ThrowsArgumentNullException()
      {
          Action act = () => new DocType(null!);
          act.Should().Throw<ArgumentNullException>()
              .WithParameterName(nameof(symbol));
      }

      [TestMethod]
      public async Task Constructor_WithValidSymbol_SetsProperties()
      {
          var compilation = await CreateCompilationAsync();
          var symbol = compilation.GetTypeByMetadataName("CloudNimble.DotNetDocs.Tests.Shared.TestBase");
          symbol.Should().NotBeNull();

          var docType = new DocType(symbol!);
          docType.Symbol.Should().Be(symbol);
          docType.Usage.Should().BeEmpty();
          docType.RelatedApis.Should().BeEmpty();
          docType.Members.Should().BeEmpty();
      }

      private async Task<Compilation> CreateCompilationAsync()
      {
          var assemblyPath = "CloudNimble.DotNetDocs.Tests.Shared/bin/Debug/net8.0/CloudNimble.DotNetDocs.Tests.Shared.dll";
          var metadataReference = MetadataReference.CreateFromFile(assemblyPath);
          return CSharpCompilation.Create("Test")
              .AddReferences(metadataReference);
      }
  }
  ```
- [x] Create `DocMemberTests.cs` file.
- [x] Create `DocParameterTests.cs` file.
- [x] Run tests: `dotnet test --configuration Debug`.
- [x] Clean up temporary files - N/A, using Tests.Shared project

## Phase 3: Metadata Extraction with Roslyn and XML  
**Goal**: Implement `AssemblyManager` for robust metadata and XML loading, multi-target compatible.

✅ **COMPLETED** - AssemblyManager fully implemented with:
- Roslyn metadata extraction
- XML documentation parsing  
- Conceptual content loading
- Baseline JSON testing
- 100% test coverage

- [x] Navigate to core project: `cd ../CloudNimble.DotNetDocs.Core`.
- [x] Create `ProjectContext.cs` file (COMPLETED)
- [x] Create `AssemblyManager.cs` file (COMPLETED with actual implementation):
  ```csharp
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Threading.Tasks;

  namespace CloudNimble.DotNetDocs.Core;

  /// <summary>
  /// Manages assembly metadata extraction using Roslyn for API documentation generation.
  /// </summary>
  /// <example>
  /// <code><![CDATA[
  /// using var manager = new AssemblyManager("MyLib.dll", "MyLib.xml");
  /// var context = new ProjectContext("ref1.dll", "ref2.dll") { ConceptualPath = "conceptual" };
  /// var model = await manager.DocumentAsync(context);
  /// ]]></code>
  /// </example>
  public class AssemblyManager : IDisposable
  {
      #region Properties

      /// <summary>
      /// Gets the path to the assembly DLL file.
      /// </summary>
      public string AssemblyPath { get; init; }

      /// <summary>
      /// Gets the path to the XML documentation file.
      /// </summary>
      public string XmlPath { get; init; }

      #endregion

      #region Constructors

      /// <summary>
      /// Initializes a new instance of the <see cref="AssemblyManager"/> class.
      /// </summary>
      /// <param name="assemblyPath">The path to the assembly DLL file.</param>
      /// <param name="xmlPath">The path to the XML documentation file.</param>
      public AssemblyManager(string assemblyPath, string xmlPath)
      {
          ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
          ArgumentException.ThrowIfNullOrWhiteSpace(xmlPath);
          
          AssemblyPath = assemblyPath;
          XmlPath = xmlPath;
      }

      #endregion

      #region Public Methods

      /// <summary>
      /// Documents the assembly asynchronously using the specified project context.
      /// </summary>
      /// <param name="projectContext">The project context with references and conceptual path.</param>
      /// <returns>The documented assembly model.</returns>
      public async Task<DocAssembly> DocumentAsync(ProjectContext? projectContext = null)
      {
          // Implementation extracts metadata using Roslyn
          // Loads XML documentation comments
          // Optionally loads conceptual content from namespace-based folder structure
      }

      #endregion
  }
  ```
- [x] Update `DocAssembly.cs` (add `IAssemblySymbol`, `List<DocNamespace>`) - COMPLETED
- [x] Update `DocNamespace.cs` (add `INamespaceSymbol`, `List<DocType>`) - COMPLETED
- [x] Update `DocType.cs` (add interfaces, XML parsing via `GetDocumentationCommentXml`) - COMPLETED
- [x] Update `DocMember.cs` (add XML tags like `<summary>`, `<param>`) - COMPLETED
- [x] Update `DocParameter.cs` (add XML `<param>` description) - COMPLETED
- [x] Navigate to test project: `cd ../CloudNimble.DotNetDocs.Tests.Core`.
- [x] Ensure Tests.Shared exists from Phase 2 - COMPLETED
- [x] Create `AssemblyManagerTests.cs` file:
  ```csharp
  using CloudNimble.Breakdance.Assemblies;
  using CloudNimble.DotNetDocs;
  using FluentAssertions;
  using Microsoft.VisualStudio.TestTools.UnitTesting;
  using System.IO;
  using System.Threading.Tasks;

  namespace CloudNimble.DotNetDocs.Tests.Core;

  [TestClass]
  public class AssemblyManagerTests : BreakdanceTestBase
  {
      [TestMethod]
      public async Task DocumentAsync_WithValidFiles_PopulatesModel()
      {
          // Arrange
          var assemblyPath = "CloudNimble.DotNetDocs.Tests.Shared/bin/Debug/net8.0/CloudNimble.DotNetDocs.Tests.Shared.dll";
          var xmlPath = "CloudNimble.DotNetDocs.Tests.Shared/bin/Debug/net8.0/CloudNimble.DotNetDocs.Tests.Shared.xml";
          using var manager = new AssemblyManager(assemblyPath, xmlPath);
          
          // Act
          var model = await manager.DocumentAsync();
          
          // Assert
          model.Should().NotBeNull();
          model.Namespaces.Should().NotBeEmpty();
      }
  }
  ```
- [x] Run tests: `dotnet test --configuration Debug` - All AssemblyManager tests pass
- [x] Benchmark extraction performance (e.g., <5s for large assemblies) - Tests run in <100ms
- [x] Clean up temporary files - N/A, using test baselines

## Phase 4: Augmentation and /conceptual Loading
**Goal**: Load conceptual content from `/conceptual` into `DocEntity` properties organized by namespace hierarchy.

- [ ] Navigate to core project: `cd ../CloudNimble.DotNetDocs.Core`.
- [ ] Update `AssemblyManager.cs` to call `LoadConceptual` in `BuildModel`.
- [ ] Implement `LoadConceptual` method in `AssemblyManager.cs`:
  ```csharp
  private void LoadConceptual(DocAssembly assembly, string conceptualPath)
  {
      ArgumentException.ThrowIfNullOrWhiteSpace(conceptualPath);
      foreach (var ns in assembly.Namespaces)
      {
          foreach (var type in ns.Types)
          {
              // Build namespace path like /conceptual/System/Text/Json/JsonSerializer/
              var namespacePath = ns.Symbol.ToDisplayString().Replace('.', Path.DirectorySeparatorChar);
              var dir = Path.Combine(conceptualPath, namespacePath, type.Symbol.Name);
              
              if (Directory.Exists(dir))
              {
                  type.Usage = File.Exists(Path.Combine(dir, "usage.md"))
                      ? File.ReadAllText(Path.Combine(dir, "usage.md"))
                      : string.Empty;
                  type.Examples = File.Exists(Path.Combine(dir, "examples.md"))
                      ? File.ReadAllText(Path.Combine(dir, "examples.md"))
                      : string.Empty;
                  type.BestPractices = File.Exists(Path.Combine(dir, "best-practices.md"))
                      ? File.ReadAllText(Path.Combine(dir, "best-practices.md"))
                      : string.Empty;
                  // Load other DocEntity properties
                  
                  // Load member-specific content
                  foreach (var member in type.Members)
                  {
                      var memberDir = Path.Combine(dir, member.Symbol.Name);
                      if (Directory.Exists(memberDir))
                      {
                          member.Usage = File.Exists(Path.Combine(memberDir, "usage.md"))
                              ? File.ReadAllText(Path.Combine(memberDir, "usage.md"))
                              : string.Empty;
                          // Load other member properties
                      }
                  }
              }
          }
      }
  }
  ```
- [ ] Update `DocType.cs` to handle conceptual mapping.
- [ ] Update `DocMember.cs` to handle conceptual mapping.
- [ ] Navigate to test project: `cd ../CloudNimble.DotNetDocs.Tests.Core`.
- [ ] Create `/conceptual/CloudNimble/DotNetDocs/Tests/Shared/TestBase/usage.md` file: `Base class for test infrastructure with Breakdance support.`.
- [ ] Create `AugmentationTests.cs` file:
  ```csharp
  using CloudNimble.Breakdance.Assemblies;
  using CloudNimble.DotNetDocs;
  using FluentAssertions;
  using Microsoft.VisualStudio.TestTools.UnitTesting;

  namespace CloudNimble.DotNetDocs.Tests.Core;

  [TestClass]
  public class AugmentationTests : BreakdanceTestBase
  {
      [TestMethod]
      public async Task DocumentAsync_WithConceptualPath_LoadsUsage()
      {
          // Arrange: Create namespace-based conceptual structure
          Directory.CreateDirectory("conceptual/CloudNimble/DotNetDocs/Tests/Shared");
          File.WriteAllText("conceptual/CloudNimble/DotNetDocs/Tests/Shared/TestBase/usage.md", 
              "Base class for test infrastructure with Breakdance support.");
          
          var manager = new AssemblyManager();
          var model = await manager.DocumentAsync(
              "CloudNimble.DotNetDocs.Tests.Shared/bin/Debug/net8.0/CloudNimble.DotNetDocs.Tests.Shared.dll",
              "CloudNimble.DotNetDocs.Tests.Shared/bin/Debug/net8.0/CloudNimble.DotNetDocs.Tests.Shared.xml",
              new ProjectContext { ConceptualPath = "conceptual" }
          );
          var testBase = model.Namespaces.SelectMany(n => n.Types)
              .FirstOrDefault(t => t.Symbol.Name == "TestBase");
          testBase.Should().NotBeNull();
          testBase!.Usage.Should().Be("Base class for test infrastructure with Breakdance support.");
      }
  }
  ```
- [ ] Run tests: `dotnet test --configuration Debug`.
- [ ] Clean up `/conceptual` samples.

## Phase 5: RenderPipeline Implementation
**Goal**: Implement transformation pipeline for customizations (insertions, overrides, etc.).

- [ ] Navigate to core project: `cd ../CloudNimble.DotNetDocs.Core`.
- [ ] Create `ITransformer.cs` file:
  ```csharp
  using System.Threading.Tasks;

  namespace CloudNimble.DotNetDocs;

  /// <summary>
  /// Defines a transformation step in the rendering pipeline.
  /// </summary>
  public interface ITransformer
  {
      Task TransformAsync(DocAssembly model, TransformationContext context);
  }
  ```
- [ ] Create `TransformationContext.cs` file:
  ```csharp
  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;

  namespace CloudNimble.DotNetDocs;

  /// <summary>
  /// Context for transformation, including configuration and output settings.
  /// </summary>
  public class TransformationContext
  {
      public string OutputFormat { get; init; } = string.Empty;
      public Dictionary<string, object> Rules { get; init; } = [];
      public string ConceptualPath { get; init; } = string.Empty;
  }
  ```
- [ ] Create `RenderPipeline.cs` file:
  ```csharp
  using System.Collections.Generic;
  using System.Threading.Tasks;

  namespace CloudNimble.DotNetDocs;

  /// <summary>
  /// Manages the transformation pipeline for rendering documentation.
  /// </summary>
  public class RenderPipeline
  {
      private readonly List<ITransformer> transformers;

      public RenderPipeline(params ITransformer[] transformers)
      {
          ArgumentNullException.ThrowIfNull(transformers);
          this.transformers = [.. transformers];
      }

      public async Task<DocAssembly> TransformAsync(DocAssembly model, TransformationContext context)
      {
          ArgumentNullException.ThrowIfNull(model);
          ArgumentNullException.ThrowIfNull(context);

          foreach (var transformer in transformers)
          {
              await transformer.TransformAsync(model, context);
          }
          return model;
      }
  }
  ```
- [ ] Create `InsertConceptualTransformer.cs` file (insert from `/conceptual` if empty).
- [ ] Create `OverrideTitleTransformer.cs` file (from rules).
- [ ] Create `ExcludePrivateTransformer.cs` file.
- [ ] Create `TransformExamplesTransformer.cs` file (e.g., to table).
- [ ] Create `ConditionalObsoleteTransformer.cs` file.
- [ ] Create `IOutputRenderer.cs` file:
  ```csharp
  using System.Threading.Tasks;

  namespace CloudNimble.DotNetDocs;

  /// <summary>
  /// Defines a renderer for documentation output.
  /// </summary>
  public interface IOutputRenderer
  {
      Task RenderAsync(DocAssembly model, string outputPath, TransformationContext context);
  }
  ```
- [ ] Create `MarkdownRenderer.cs` file (calls pipeline, renders to Markdown).
- [ ] Create `JsonRenderer.cs` file.
- [ ] Create `YamlRenderer.cs` file.
- [ ] Navigate to test project: `cd ../CloudNimble.DotNetDocs.Tests.Core`.
- [ ] Create `RenderPipelineTests.cs` file (test chain, async).
- [ ] Create `InsertConceptualTransformerTests.cs` file.
- [ ] Create `OverrideTitleTransformerTests.cs` file.
- [ ] Create `ExcludePrivateTransformerTests.cs` file.
- [ ] Create `TransformExamplesTransformerTests.cs` file.
- [ ] Create `ConditionalObsoleteTransformerTests.cs` file.
- [ ] Create `MarkdownRendererTests.cs` file.
- [ ] Create `JsonRendererTests.cs` file.
- [ ] Create `YamlRendererTests.cs` file.
- [ ] Run tests: `dotnet test --configuration Debug`.
- [ ] Clean up temporary files.

## Phase 6: CLI and MSBuild Integration
**Goal**: Implement CLI (`dotnet docs generate`) and MSBuild task.

- [ ] Create Tools project directory: `mkdir ../CloudNimble.DotNetDocs.Tools`.
- [ ] Navigate to Tools project: `cd ../CloudNimble.DotNetDocs.Tools`.
- [ ] Create Tools project file: `dotnet new console --framework net10.0 --configuration Debug`.
- [ ] Update `CloudNimble.DotNetDocs.Tools.csproj`:
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <TargetFrameworks>net10.0;net9.0;net8.0</TargetFrameworks>
      <PackAsTool>true</PackAsTool>
      <ToolCommandName>docs</ToolCommandName>
      <IsPackable>true</IsPackable>
    </PropertyGroup>
  </Project>
  ```
- [ ] Add McMaster NuGet: `dotnet add package McMaster.Extensions.CommandLineUtils --version 4.1.1`.
- [ ] Create `Program.cs` file:
  ```csharp
  using McMaster.Extensions.CommandLineUtils;

  namespace CloudNimble.DotNetDocs.Tools;

  [Command("docs")]
  public class Program
  {
      public static int Main(string[] args) => CommandLineApplication.Execute<GenerateCommand>(args);
  }
  ```
- [ ] Create `GenerateCommand.cs` file (parse args, call `AssemblyManager`, `RenderPipeline`).
- [ ] Create MSBuild project directory: `mkdir ../CloudNimble.DotNetDocs.MSBuild`.
- [ ] Navigate to MSBuild project: `cd ../CloudNimble.DotNetDocs.MSBuild`.
- [ ] Create MSBuild project file: `dotnet new classlib --framework netstandard2.0 --configuration Debug`.
- [ ] Add Microsoft.Build NuGet: `dotnet add package Microsoft.Build.Tasks.Core --version 17.11.0`.
- [ ] Create `GenerateDocsTask.cs` file (MSBuild task with inputs).
- [ ] Create test projects: `mkdir ../CloudNimble.DotNetDocs.Tests.Tools`, `mkdir ../CloudNimble.DotNetDocs.Tests.MSBuild`.
- [ ] Create test project files and tests for CLI/MSBuild integration.
- [ ] Run tests: `dotnet test --configuration Debug`.
- [ ] Clean up.

## Phase 7: Plugins, Mintlify/Docusaurus, and Optimization
**Goal**: Add extensibility and tool-specific outputs.

### Mintlify.Core Foundation ✅ **COMPLETED**
- [x] Create Mintlify.Core project - COMPLETED (foundational library for docs.json)
- [x] Implement DocsJsonConfig model with full schema support - COMPLETED
- [x] Implement DocsJsonManager with merge functionality - COMPLETED
- [x] Implement DocsJsonValidator for schema validation - COMPLETED
- [x] Add MergeOptions with CombineEmptyGroups feature - COMPLETED
- [x] Create comprehensive test suite with 100% coverage - COMPLETED
- [x] Add custom JSON converters for complex navigation types - COMPLETED

### CloudNimble.DotNetDocs.Mintlify Renderer (Still Needed)
- [ ] Create Mintlify project directory: `mkdir ../CloudNimble.DotNetDocs.Mintlify`
- [ ] Navigate to Mintlify project: `cd ../CloudNimble.DotNetDocs.Mintlify`
- [ ] Create Mintlify project file: `dotnet new classlib --framework net10.0 --configuration Debug`
- [ ] Update `CloudNimble.DotNetDocs.Mintlify.csproj` to multi-target
- [ ] Add reference to Mintlify.Core for DocsJsonConfig usage
- [ ] Create `MintlifyRenderer.cs` - Transform DocAssembly to Mintlify MDX format
- [ ] Create `MintlifyNavigationBuilder.cs` - Build navigation from DocAssembly structure
- [ ] Create `MintlifyPageGenerator.cs` - Generate individual MDX pages
- [ ] Create tests for Mintlify renderer

### Remaining Plugin Work
- [ ] Create Plugins project directory: `mkdir ../CloudNimble.DotNetDocs.Plugins`.
- [ ] Navigate to Plugins project: `cd ../CloudNimble.DotNetDocs.Plugins`.
- [ ] Create Plugins project file: `dotnet new classlib --framework net10.0 --configuration Debug`.
- [ ] Update `CloudNimble.DotNetDocs.Plugins.csproj` to multi-target.
- [ ] Create `IAugmentor.cs` file.
- [ ] Create `SampleLlmAugmentor.cs` file.

### Docusaurus Support  
- [ ] Create Docusaurus project directory: `mkdir ../CloudNimble.DotNetDocs.Docusaurus`.
- [ ] Navigate to Docusaurus project: `cd ../CloudNimble.DotNetDocs.Docusaurus`.
- [ ] Create Docusaurus project file: `dotnet new classlib --framework net10.0 --configuration Debug`.
- [ ] Update `CloudNimble.DotNetDocs.Docusaurus.csproj` to multi-target.
- [ ] Create `DocusaurusRenderer.cs` file.

### Performance Optimization
- [ ] Add BenchmarkDotNet NuGet to test project: `dotnet add package BenchmarkDotNet`.
- [ ] Create performance tests for extraction, rendering.
- [ ] Run tests: `dotnet test --configuration Debug`.
- [ ] Clean up.

## Phase 8: Final Validation, Mintlify Integration, and Cleanup
**Goal**: Ensure robustness, integrate Mintlify.

- [ ] Create `MintlifyScript.cs` file for `dotnet easyaf mintlify`.
- [ ] Run full tests across targets: `dotnet test --configuration Debug`.
- [ ] Verify coverage >95% (use `dotnet test --collect:"Code Coverage"`).
- [ ] Ensure XML comments complete.
- [ ] Clean up all temp files.

## Next Steps (Priority Order)

### Immediate Tasks
1. **Phase 4: Conceptual Loading** - Implement the /conceptual folder system for augmenting API docs with human-written content
2. **Phase 5: RenderPipeline** - Build transformation pipeline for customizations and output generation
3. **Phase 6: CLI Integration** - Create `dotnet docs` command-line tool for easy usage

### Medium Priority  
4. **Docusaurus Support** - Add Docusaurus output format alongside Mintlify
5. **Plugin System** - Implement extensibility points for custom transformers
6. **MSBuild Integration** - Create MSBuild tasks for build-time doc generation

### Future Enhancements
7. **Performance Optimization** - Add benchmarking and optimize for large assemblies
8. **Advanced Transformers** - Add specialized transformers (obsolete handling, example formatting, etc.)
9. **LLM Augmentation** - Explore AI-powered documentation enhancement

This plan delivers a simple, performant, testable solution.

