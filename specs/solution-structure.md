# Project Structure for CloudNimble.DotNetDocs

This document outlines the project structure for `CloudNimble.DotNetDocs`, a flexible .NET API documentation generator supporting command-line (`dotnet docs generate`) and build-time (MSBuild task) execution. It maximizes metadata extraction, with build-time mode leveraging `.csproj` for source intent and validation. The design emphasizes modularity, extensibility, and rich contextual documentation via a shared model, with a transformation pipeline for customized rendering.

## Design Goals
- **Command-Line Mode**: Run `dotnet docs generate --assembly MyLib.dll --xml MyLib.xml --output docs` for standalone metadata extraction, with default outputs (Markdown, JSON, YAML).
- **Build-Time Mode**: Integrate as an MSBuild task post-build, adding source intent (e.g., file paths, `<ConceptualPath>`), incremental builds, and validation.
- **Shared Logic**: Reuse core extraction and model building across modes.
- **Extensibility**: Support plugins and separate Mintlify/Docusaurus projects to manage dependencies. Use a transformation pipeline (`DocumentationManager`) for customizations (insertions, overrides, exclusions, transformations, conditions).
- **Rich Documentation**: Include contextual sections (Usage, Examples, Best Practices, Patterns, Considerations, Related APIs) beyond Roslyn/XML, sourced from `/conceptual` folder or plugins, with namespace and type/member support.
- **NuGet Packaging**: Projects use “CloudNimble” in names/namespaces; NuGet packages strip it (e.g., `DotNetDocs.Core`).

## Solution Structure
- **Solution**: `CloudNimble.DotNetDocs.slnx` (modern .NET solution format).
  - `CloudNimble.DotNetDocs.Core/`: Shared logic (class library).
  - `CloudNimble.DotNetDocs.Tools/`: CLI for `dotnet docs generate`.
  - `CloudNimble.DotNetDocs.MSBuild/`: MSBuild task for build-time integration.
  - `CloudNimble.DotNetDocs.Plugins.AI/`: AI-powered extensibility plugins using Semantic Kernel.
  - `CloudNimble.DotNetDocs.Mintlify/`, `CloudNimble.DotNetDocs.Docusaurus/`: Tool-specific logic.
  - `CloudNimble.DotNetDocs.Tests.Core/`, `CloudNimble.DotNetDocs.Tests.Tools/`, `CloudNimble.DotNetDocs.Tests.MSBuild/`: MSTest projects.
- **Directory.Build.props**:
  ```xml
  <PropertyGroup>
    <PackageId Condition="'$(IsPackable)'=='true'">$(MSBuildProjectName.Replace('CloudNimble.', ''))</PackageId>
  </PropertyGroup>
  ```

## Project Breakdown

### 1. CloudNimble.DotNetDocs.Core (Class Library)
- **Purpose**: Core logic for metadata extraction, model building, augmentation, and rendering.
- **Components**:
  - **Models** (root namespace: `CloudNimble.DotNetDocs`):
    - `DocEntity`: Base class with contextual properties.
      - `Usage`: Markdown for how to use the API.
      - `Examples`: Markdown for code snippets/demos.
      - `BestPractices`: Markdown for recommendations.
      - `Patterns`: Markdown for architectural guidance.
      - `Considerations`: Markdown for gotchas, performance, security.
      - `RelatedApis`: List of names/URLs for related APIs.
      - `IncludedMembers`: List of Accessibility enums (default: Public) for granular member filtering during extraction.
    - Derived: `DocAssembly`, `DocNamespace`, `DocType`, `DocMember`, `DocParameter` wrapping Roslyn `ISymbol` (e.g., `ITypeSymbol`), with specific fields (e.g., `DocType.BaseType`).
- **Metadata Extraction**:
  - `AssemblyManager`: Loads assemblies (`MetadataReference`), XML (`XmlDocumentationProvider`), and `.csproj` for source intent.
  - Method: `DocumentAsync(Compilation, ProjectContext?)`.
  - Populates `DocEntity` properties via `/conceptual` files (e.g., `conceptual/MyNamespace/usage.md`, `conceptual/MyClass/usage.md`) or plugins, including namespace-level conceptual documentation.
  - Filters members using `IncludedMembers` during recursive processing (configurable via `ProjectContext.IncludedMembers`).
  - **Augmentation**:
    - `IDocEnricher`: Interface in Core for conceptual enrichment (e.g., LLM-driven `Examples`).
  - **Rendering**:
    - `DocumentationManager`: Orchestrates the pipeline (enrichment, transformation, rendering) for one or more assemblies, managing `AssemblyManager` lifecycle.
    - `IDocTransformer`: For structural changes (e.g., insertions/overrides/exclusions from JSON rules).
    - `IDocRenderer`: For format-specific output, invoking `DocumentationManager.ProcessAsync`.
    - Tool-specific logic in `CloudNimble.DotNetDocs.Mintlify`, `CloudNimble.DotNetDocs.Docusaurus`.
- **Dependencies**: `Microsoft.CodeAnalysis.CSharp`, `System.Text.Json`.
- **NuGet**: `DotNetDocs.Core`.

### 2. CloudNimble.DotNetDocs.Tools (Console App)
- **Purpose**: Standalone CLI for manual runs.
- **Features**:
  - Command: `dotnet docs generate` using `McMaster.Extensions.CommandLineUtils`.
  - Args: `--assembly`, `--xml`, `--output`, `--output-format` (markdown/json/yaml), `--conceptual-path`, `--customizations-path`.
  - Example: `dotnet docs generate --assembly MyLib.dll --xml MyLib.xml --output docs --conceptual-path conceptual/ --customizations-path custom.json`.
  - Uses `CloudNimble.DotNetDocs.Core` for extraction, augmentation, output via `DocumentationManager`.
- **Dependencies**: `CloudNimble.DotNetDocs.Core`, `McMaster.Extensions.CommandLineUtils`.
- **NuGet**: `DotNetDocs.Tools` (global tool).

### 3. CloudNimble.DotNetDocs.MSBuild (MSBuild Task)
- **Purpose**: Build-time integration with source context.
- **Implementation**:
  - Task: `GenerateDocsTask` with inputs (`Assembly`, `XmlDocFile`, `ProjectFile`, `References`, `ConceptualPath`, `CustomizationsPath`).
  - MSBuild: `<Target Name="GenerateDocs" AfterTargets="Build">`.
  - Outputs: Docs or model to `$(DocsOutputPath)`.
- **Extra Coolness**:
  - Incremental builds via MSBuild.
  - Source intent from `.csproj` (e.g., `<Compile>` paths, `<ConceptualPath>`).
  - Validation: XML comment and `DocEntity` property consistency.
  - Git integration for versioning notes.
- **Dependencies**: `CloudNimble.DotNetDocs.Core`, `Microsoft.Build.Tasks.Core`.
- **NuGet**: `DotNetDocs.MSBuild`.

### 4. CloudNimble.DotNetDocs.Plugins.AI (Class Library)
- **Purpose**: AI-powered extensibility for custom augmentation and transformation using Semantic Kernel.
- **Features**:
  - **Model Flexibility**: Users configure AI models (OpenAI, Azure OpenAI, local).
  - **Embedding Support**: Choose embedding models for semantic search.
  - **Semantic Kernel Integration**: Skills, planners, and memory for generating conceptual content.
  - **Kernel Memory (Optional)**: Index and search documentation semantically.
- **Components**:
  - `IDocEnricher`: Interface for AI-powered enrichment (implemented here for LLM generation).
  - `SemanticKernelEnricher`: SK-based implementation with model selection.
  - `DocumentationEnhancer`: Generates examples, usage, considerations.
  - `KernelMemoryIndexer`: Semantic search across documentation.
- **Dependencies**: `CloudNimble.DotNetDocs.Core`, `Microsoft.SemanticKernel`, optional `Microsoft.KernelMemory.Core`.
- **NuGet**: `DotNetDocs.Plugins.AI`.

### 5. CloudNimble.DotNetDocs.Mintlify, CloudNimble.DotNetDocs.Docusaurus
- **Purpose**: Tool-specific logic (e.g., Mintlify `docs.json`, Docusaurus sidebar configs), mapping `DocEntity` properties via `DocumentationManager`.
- **Dependencies**: `CloudNimble.DotNetDocs.Core`, respective tool SDKs.
- **NuGet**: `DotNetDocs.Mintlify`, `DotNetDocs.Docusaurus`.

### 6. Tests
- **Projects**: `CloudNimble.DotNetDocs.Tests.Core`, `CloudNimble.DotNetDocs.Tests.Tools`, `CloudNimble.DotNetDocs.Tests.MSBuild`.
- **Setup**: MSTest v3, FluentAssertions, Breakdance.
- **Tests**: Model accuracy, CLI args, MSBuild outputs, `DocEntity` property handling, `DocumentationManager` transformations.
- **Naming**: `CloudNimble.DotNetDocs.Tests.SubjectMatter`.

## Internal Member Documentation

### Current Approach
To document internal members, assemblies must include:
```csharp
[assembly: InternalsVisibleTo("CloudNimble.DotNetDocs.Core")]
```

This allows the documentation generator to access internal members when `IncludedMembers` contains `Accessibility.Internal`.

### Future Enhancements
For situations where modifying the source assembly isn't possible, we may explore:

1. **Mono.Cecil Integration**: Dynamically inject `[InternalsVisibleTo]` attributes into assemblies at documentation time. See [Stack Overflow example](https://stackoverflow.com/a/44329684).

2. **Roslyn Compilation Bypass**: Use the technique described in [StrathWeb's article](https://www.strathweb.com/2018/10/no-internalvisibleto-no-problem-bypassing-c-visibility-rules-with-roslyn/) to compile an intermediate assembly that can access internals without the attribute.

### Error Reporting
`AssemblyManager` includes an `Errors` property (`List<CompilerError>`) to report issues such as:
- Requested internal members but assembly lacks `InternalsVisibleTo` attribute
- Missing XML documentation for public APIs
- Compilation or metadata extraction failures

## Why It Works
- **Flexibility**: `CloudNimble.DotNetDocs.Core` ensures CLI/MSBuild consistency. CLI uses `/conceptual` files and `custom.json`; MSBuild adds source intent, validation, incremental builds.
- **Rich Documentation**: `DocEntity` supports contextual sections (Usage, Examples, etc.), populated via `/conceptual` (e.g., `conceptual/MyNamespace/usage.md`, `conceptual/MyClass/usage.md`) or plugins, including namespace support.
- **Extensibility**: `DocumentationManager` orchestrates enrich/transform/render with DI-injected lists (e.g., `IEnumerable<IDocEnricher>`). AI plugins enhance with Semantic Kernel; Mintlify/Docusaurus projects manage tool-specific dependencies.
- **Maintainability**: `Directory.Build.props` automates `<PackageId>`; modular, testable, aligns with C# 12/13, nullable types.
- **Internal Documentation**: Support for internal members via `InternalsVisibleTo` attribute, with error reporting when access is restricted.

## Next Steps
- Define `/conceptual` file format (e.g., Markdown, JSON mapping to `DocEntity`).
- Refine `DocumentationManager` JSON schema and transformer order (see `transformation-pipeline.md`).
- Test CLI (`dotnet docs generate`) and MSBuild modes with sample `/conceptual` files.

