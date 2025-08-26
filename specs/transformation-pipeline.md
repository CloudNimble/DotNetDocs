# Transformation Pipeline for CloudNimble.DotNetDocs Rendering

This document outlines the transformation pipeline for `CloudNimble.DotNetDocs`, which processes the in-memory documentation model (`DocAssembly`, `DocType`, etc.) to produce customized outputs (Markdown, JSON, YAML, Mintlify, Docusaurus). The pipeline enables flexible customizations—insertions, overrides, exclusions, transformations, and conditions—while keeping the rendering process simple and maintainable.

## Purpose
The pipeline transforms the in-memory model (populated by `AssemblyManager` with Roslyn/XML and `DocEntity` properties like `Usage`, `Examples`) into user-specified outputs. It supports:
- **Insertions**: Add content (e.g., `Usage` from `conceptual/MyNamespace/usage.md` or `conceptual/MyClass/usage.md`).
- **Overrides**: Replace elements (e.g., frontmatter `title` with custom text).
- **Exclusions**: Omit items (e.g., specific members via conditions).
- **Transformations**: Modify content (e.g., format `Examples` as a table).
- **Conditions**: Apply rules contextually (e.g., add warnings for `[Obsolete]` members).

The pipeline ensures flexibility (via JSON rules or plugins) without overengineering, with defaults for straightforward API docs. Member filtering is handled during extraction via `DocEntity.IncludedMembers` (default `[Accessibility.Public]`), reducing transformation overhead.

## Pipeline Structure
The pipeline is orchestrated by `DocumentationManager`, which manages the full lifecycle: enrichment (conceptual), transformation (structural), and rendering (output) for one or more assemblies. It handles `AssemblyManager` creation, usage, and disposal.

### Core Components
- **IDocEnricher**:
  ```csharp
  public interface IDocEnricher
  {
      Task EnrichAsync(DocEntity entity, EnrichmentContext context);
  }
  ```
  For conceptual augmentation (e.g., AI-generated `Examples`) in `CloudNimble.DotNetDocs.Core`, with implementations in `CloudNimble.DotNetDocs.Plugins.AI`.
- **IDocTransformer**:
  ```csharp
  public interface IDocTransformer
  {
      Task TransformAsync(DocAssembly model, TransformationContext context);
  }
  ```
  Defines a transformation step (e.g., insert `Usage`, override `title`).
- **IDocRenderer**:
  ```csharp
  public interface IDocRenderer
  {
      Task RenderAsync(DocAssembly model, string outputPath, TransformationContext context);
  }
  ```
  For format-specific output (e.g., Markdown files).
- **DocumentationManager**:
  ```csharp
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

          var pipeline = new RenderPipeline(_transformers.ToArray());
          await pipeline.TransformAsync(model, new TransformationContext { CustomSettings = projectContext?.CustomSettings });

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
              var pipeline = new RenderPipeline(_transformers.ToArray());
              await pipeline.TransformAsync(model, new TransformationContext { CustomSettings = projectContext?.CustomSettings });
              foreach (var renderer in _renderers)
                  await renderer.RenderAsync(model, projectContext?.OutputPath ?? "docs", new TransformationContext { CustomSettings = projectContext?.CustomSettings });
          });
          await Task.WhenAll(tasks);
      }
  }
  ```
  Orchestrates the pipeline, managing `AssemblyManager` lifecycle for single or multiple assemblies.
- **EnrichmentContext**:
  ```csharp
  public class EnrichmentContext
  {
      public string ConceptualPath { get; init; } = string.Empty;
      public object Settings { get; init; } = new();
  }
  ```
  For enrichers (e.g., conceptual path, AI configs).
- **TransformationContext**:
  ```csharp
  public class TransformationContext
  {
      public object CustomSettings { get; init; } = new();
  }
  ```
  For transformers (e.g., output format, JSON rules from `custom.json`).

### Example Transformers
- **InsertUsageTransformer**: Loads `Usage` from `conceptual/MyNamespace/usage.md` or `conceptual/MyClass/usage.md` if empty.
- **OverrideTitleTransformer**: Replaces frontmatter `title` (e.g., `MyClass` to "Custom Logger").
- **TransformExamplesTransformer**: Converts `Examples` to a table for Markdown.
- **ConditionalObsoleteTransformer**: Adds warnings to `Considerations` for `[Obsolete]` members.
  - Note: `ExcludePrivateTransformer` is replaced by `IncludedMembers` filtering in `AssemblyManager`.

### Customization Rules
Users define customizations in a JSON file (e.g., `custom.json`), loaded via `--customizations-path` (CLI) or `<CustomizationsPath>` (MSBuild). Example:
```json
{
  "root": {
    "excludePrivate": true,
    "transformations": {
      "examples": "table"
    }
  },
  "namespaces": {
    "MyNamespace": {
      "title": "Core Utilities",
      "icon": "utils-icon.png"
    }
  },
  "pages": {
    "MyClass.DoWork": {
      "overrides": {
        "title": "Perform Work Operation"
      },
      "conditions": {
        "ifObsolete": { "insert": "Deprecated; use DoWorkAsync." }
      }
    }
  }
}
```

## Integration
- **Core (`CloudNimble.DotNetDocs.Core`)**:
  - `DocumentationManager` and interfaces (`IDocEnricher`, `IDocTransformer`, `IDocRenderer`) manage the pipeline.
  - `AssemblyManager.DocumentAsync` invokes `IDocEnricher` for initial conceptual population, including namespace support.
- **CLI (`CloudNimble.DotNetDocs.Tools`)**:
  - Args: `--customizations-path custom.json`.
  - Example: `dotnet docs generate --assembly MyLib.dll --xml MyLib.xml --output docs --conceptual-path conceptual/ --customizations-path custom.json`.
- **MSBuild (`CloudNimble.DotNetDocs.MSBuild`)**:
  - Task input: `<CustomizationsPath>custom.json</CustomizationsPath>`.
  - Uses `.csproj` for context (e.g., `<ConceptualPath>`).
  - Example: `<GenerateDocsTask ConceptualPath="conceptual/" CustomizationsPath="custom.json" OutputPath="$(DocsOutputPath)">`.
- **Plugins.AI (`CloudNimble.DotNetDocs.Plugins.AI`)**:
  - Custom `IDocEnricher` implementations (e.g., `SemanticKernelEnricher` for LLM generation).
  - Loaded via `--enrich-plugin` (CLI) or `<EnrichPlugin>` (MSBuild).
- **Tool-Specific Projects**:
  - `CloudNimble.DotNetDocs.Mintlify`: Maps `DocEntity` properties to `docs.json`.
  - `CloudNimble.DotNetDocs.Docusaurus`: Transforms `RelatedApis` into sidebar links.

## Why It Works
- **Flexibility**: JSON rules and plugins allow custom insertions, overrides, etc., without modifying core logic.
- **Simplicity**: Default pipeline (no rules) renders Roslyn/XML data directly.
- **Extensibility**: Plugins add custom enrichers/transformers; tool-specific projects handle format-specific needs.
- **Maintainability**: Testable components, human-readable JSON, async execution for performance.

## Next Steps
- Define JSON schema for rules (e.g., support for conditional logic).
- Determine transformer order and conflict resolution.
- Specify default enrichers/transformers (e.g., `LoadConceptualEnricher`, `OverrideTitleTransformer`).
- Add validation for customization rules (e.g., warn on invalid `RelatedApis`).
- Test namespace-level conceptual loading and `IncludedMembers` filtering.

