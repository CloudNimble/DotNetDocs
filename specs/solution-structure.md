# Project Structure for CloudNimble.DotNetDocs

This document outlines the project structure for `CloudNimble.DotNetDocs`, a flexible .NET API documentation generator supporting command-line (`dotnet docs generate`) and build-time (MSBuild task) execution. It maximizes metadata extraction, with build-time mode leveraging `.csproj` for source intent and validation. The design emphasizes modularity, extensibility, and rich contextual documentation via a shared model, with a transformation pipeline for customized rendering.

## Design Goals
- **Command-Line Mode**: Run `dotnet docs generate --assembly MyLib.dll --xml MyLib.xml --output docs` for standalone metadata extraction, with default outputs (Markdown, JSON, YAML).
- **Build-Time Mode**: Integrate as an MSBuild task post-build, adding source intent (e.g., file paths, `<ConceptualPath>`), incremental builds, and validation.
- **Shared Logic**: Reuse core extraction and model building across modes.
- **Extensibility**: Support plugins and separate Mintlify/Docusaurus projects to manage dependencies. Use a transformation pipeline (`RenderPipeline`) for customizations (insertions, overrides, exclusions, transformations, conditions).
- **Rich Documentation**: Include contextual sections (Usage, Examples, Best Practices, Patterns, Considerations, Related APIs) beyond Roslyn/XML, sourced from `/conceptual` folder or plugins.
- **NuGet Packaging**: Projects use “CloudNimble” in names/namespaces; NuGet packages strip it (e.g., `DotNetDocs.Core`).

## Solution Structure
- **Solution**: `CloudNimble.DotNetDocs.slnx` (modern .NET solution format).
  - `CloudNimble.DotNetDocs.Core/`: Shared logic (class library).
  - `CloudNimble.DotNetDocs.Tools/`: CLI for `dotnet docs generate`.
  - `CloudNimble.DotNetDocs.MSBuild/`: MSBuild task for build-time integration.
  - `CloudNimble.DotNetDocs.Plugins/`: Extensibility plugins.
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
    - Derived: `DocAssembly`, `DocNamespace`, `DocType`, `DocMember`, `DocParameter` wrapping Roslyn `ISymbol` (e.g., `ITypeSymbol`), with specific fields (e.g., `DocType.BaseType`).
  - **Metadata Extraction**:
    - `AssemblyManager`: Loads assemblies (`MetadataReference`), XML (`XmlDocumentationProvider`), and `.csproj` for source intent.
    - Constructor: `AssemblyManager(assemblyPath, xmlPath)` - paths specified at construction for incremental build support.
    - Method: `DocumentAsync(ProjectContext?)` - uses paths from constructor, returns `DocAssembly`.
    - Implements `IDisposable` for memory management.
    - Populates `DocEntity` properties via `/conceptual` files organized by namespace (e.g., `conceptual/System/Text/Json/JsonSerializer/usage.md`) or plugins.
  - **Augmentation**:
    - `IAugmentor` interface for plugins to populate `DocEntity` properties (e.g., LLM-driven `Examples`).
  - **Rendering**:
    - `RenderPipeline`: Chains `ITransformer` implementations to apply customizations (insertions, overrides, exclusions, transformations, conditions). See `RenderPipeline.md`.
    - `IOutputRenderer`: Invokes `RenderPipeline.TransformAsync` for default Markdown, JSON, YAML, including `DocEntity` properties.
    - Tool-specific logic in `CloudNimble.DotNetDocs.Mintlify`, `CloudNimble.DotNetDocs.Docusaurus`.
- **Dependencies**: `Microsoft.CodeAnalysis.CSharp`, `System.Text.Json`.
- **NuGet**: `DotNetDocs.Core`.

### 2. CloudNimble.DotNetDocs.Tools (Console App)
- **Purpose**: Standalone CLI for manual runs.
- **Features**:
  - Command: `dotnet docs generate` using `McMaster.Extensions.CommandLineUtils`.
  - Args: `--assembly`, `--xml`, `--output`, `--output-format` (markdown/json/yaml), `--conceptual-path`, `--customizations-path`.
  - Example: `dotnet docs generate --assembly MyLib.dll --xml MyLib.xml --output docs --conceptual-path conceptual/ --customizations-path custom.yaml`.
  - Uses `CloudNimble.DotNetDocs.Core` for extraction, augmentation, rendering.
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

### 4. CloudNimble.DotNetDocs.Plugins (Class Library)
- **Purpose**: Extensibility for custom augmentation and transformation.
- **Examples**: Populate `DocEntity.Usage` via LLM; custom `ITransformer` for `RenderPipeline`.
- **Dependencies**: `CloudNimble.DotNetDocs.Core`.
- **NuGet**: `DotNetDocs.Plugins`.

### 5. CloudNimble.DotNetDocs.Mintlify, CloudNimble.DotNetDocs.Docusaurus
- **Purpose**: Tool-specific logic (e.g., Mintlify `docs.json`, Docusaurus sidebar configs), mapping `DocEntity` properties via `RenderPipeline`.
- **Dependencies**: `CloudNimble.DotNetDocs.Core`, respective tool SDKs.
- **NuGet**: `DotNetDocs.Mintlify`, `DotNetDocs.Docusaurus`.

### 6. Tests
- **Projects**: `CloudNimble.DotNetDocs.Tests.Core`, `CloudNimble.DotNetDocs.Tests.Tools`, `CloudNimble.DotNetDocs.Tests.MSBuild`.
- **Setup**: MSTest v3, FluentAssertions, Breakdance.
- **Tests**: Model accuracy, CLI args, MSBuild outputs, `DocEntity` property handling, `RenderPipeline` transformations.
- **Naming**: `CloudNimble.DotNetDocs.Tests.SubjectMatter`.

## Why It Works
- **Flexibility**: `CloudNimble.DotNetDocs.Core` ensures CLI/MSBuild consistency. CLI uses `/conceptual` files and `customizations.yaml`; MSBuild adds source intent, validation, incremental builds.
- **Rich Documentation**: `DocEntity` supports contextual sections (Usage, Examples, etc.), populated via `/conceptual` (e.g., `conceptual/MyClass/usage.md`) or plugins.
- **Extensibility**: `RenderPipeline` (see `RenderPipeline.md`) enables customizations (insertions, overrides, etc.) via YAML or plugins. Mintlify/Docusaurus projects manage tool-specific dependencies.
- **Maintainability**: `Directory.Build.props` automates `<PackageId>`; modular, testable, aligns with C# 12/13, nullable types.

## Next Steps
- Define `/conceptual` file format (e.g., YAML vs. Markdown, mapping to `DocEntity`).
- Refine `RenderPipeline` YAML schema and transformer order (see `RenderPipeline.md`).
- Test CLI (`dotnet docs generate`) and MSBuild modes with sample `/conceptual` files.

