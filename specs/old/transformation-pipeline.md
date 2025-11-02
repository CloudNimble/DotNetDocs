# Transformation Pipeline for CloudNimble.DotNetDocs Rendering

This document outlines the transformation pipeline for `CloudNimble.DotNetDocs`, which processes the in-memory documentation model (`DocAssembly`, `DocType`, etc.) to produce customized outputs (Markdown, JSON, YAML, Mintlify, Docusaurus). The pipeline enables flexible customizations—insertions, overrides, exclusions, transformations, and conditions—while keeping the rendering process simple and maintainable.

## Purpose
The pipeline transforms the in-memory model (populated by `AssemblyManager` with Roslyn/XML and `DocEntity` properties like `Usage`, `Examples`) into user-specified outputs. It supports:
- **Insertions**: Add content (e.g., `Usage` from `conceptual/MyNamespace/usage.md` or `conceptual/MyClass/usage.md`).
- **Overrides**: Replace elements (e.g., frontmatter `title` with custom text).
- **Exclusions**: Omit items (e.g., specific members via conditions).
- **Transformations**: Modify content (e.g., format `Examples` as a table).
- **Conditions**: Apply rules contextually (e.g., add warnings for `[Obsolete]` members).

The pipeline ensures flexibility (via JSON rules or plugins) without overengineering, with defaults for straightforward API docs. Member filtering is handled during extraction via `DocEntity.IncludedMembers` (configurable via `ProjectContext.IncludedMembers`, default `[Accessibility.Public]`), reducing transformation overhead.

## Pipeline Structure
The pipeline is orchestrated by `DocumentationManager`, which manages the full lifecycle: enrichment (conceptual), transformation (structural), and rendering (output) for one or more assemblies. It handles `AssemblyManager` creation, usage, and disposal.

### Core Components
- **IDocEnricher**:
  ```csharp
  public interface IDocEnricher
  {
      Task EnrichAsync(DocEntity entity, ProjectContext context);
  }
  ```
  For conceptual augmentation (e.g., AI-generated `Examples`) in `CloudNimble.DotNetDocs.Core`, with implementations in `CloudNimble.DotNetDocs.Plugins.AI`.
- **IDocTransformer**:
  ```csharp
  public interface IDocTransformer
  {
      Task TransformAsync(DocEntity entity, ProjectContext context);
  }
  ```
  Defines a transformation step (e.g., insert `Usage`, override `title`). Applied recursively to all entities like enrichers.
- **IDocRenderer**:
  ```csharp
  public interface IDocRenderer
  {
      Task RenderAsync(DocAssembly model, string outputPath, ProjectContext context);
  }
  ```
  For format-specific output (e.g., Markdown files).
- **DocumentationManager**:
  ```csharp
  public class DocumentationManager
  {
      private readonly IEnumerable<IDocEnricher> enrichers;
      private readonly IEnumerable<IDocTransformer> transformers;
      private readonly IEnumerable<IDocRenderer> renderers;

      public DocumentationManager(IEnumerable<IDocEnricher> enrichers, IEnumerable<IDocTransformer> transformers, IEnumerable<IDocRenderer> renderers)
      {
          this.enrichers = enrichers ?? throw new ArgumentNullException(nameof(enrichers));
          this.transformers = transformers ?? throw new ArgumentNullException(nameof(transformers));
          this.renderers = renderers ?? throw new ArgumentNullException(nameof(renderers));
      }

      public async Task ProcessAsync(string assemblyPath, string xmlPath, ProjectContext? projectContext = null)
      {
          using var manager = new AssemblyManager(assemblyPath, xmlPath);
          var model = await manager.DocumentAsync(projectContext);

          // Load conceptual content if path provided
          if (!string.IsNullOrWhiteSpace(projectContext?.ConceptualPath))
          {
              await LoadConceptualAsync(model, projectContext.ConceptualPath);
          }

          // Apply enrichers recursively
          foreach (var enricher in enrichers)
          {
              await EnrichModelAsync(model, enricher, projectContext);
          }

          // Apply transformers recursively
          foreach (var transformer in transformers)
          {
              await TransformModelAsync(model, transformer, projectContext);
          }

          // Apply renderers
          foreach (var renderer in renderers)
          {
              await renderer.RenderAsync(model, projectContext?.OutputPath ?? "docs", projectContext ?? new ProjectContext());
          }
      }

      public async Task ProcessAsync(IEnumerable<(string assemblyPath, string xmlPath)> assemblies, ProjectContext? projectContext = null)
      {
          var tasks = assemblies.Select(async pair =>
          {
              using var manager = new AssemblyManager(pair.assemblyPath, pair.xmlPath);
              var model = await manager.DocumentAsync(projectContext);

              // Load conceptual content if path provided
              if (!string.IsNullOrWhiteSpace(projectContext?.ConceptualPath))
              {
                  await LoadConceptualAsync(model, projectContext.ConceptualPath);
              }

              // Apply enrichers recursively
              foreach (var enricher in enrichers)
              {
                  await EnrichModelAsync(model, enricher, projectContext);
              }

              // Apply transformers recursively
              foreach (var transformer in transformers)
              {
                  await TransformModelAsync(model, transformer, projectContext);
              }

              // Apply renderers
              foreach (var renderer in renderers)
              {
                  await renderer.RenderAsync(model, projectContext?.OutputPath ?? "docs", projectContext ?? new ProjectContext());
              }
          });
          await Task.WhenAll(tasks);
      }
  }
  ```
  Orchestrates the pipeline, managing `AssemblyManager` lifecycle for single or multiple assemblies.

### Example Transformers
- **InsertUsageTransformer**: Loads `Usage` from `conceptual/MyNamespace/usage.md` or `conceptual/MyClass/usage.md` if empty.
- **OverrideTitleTransformer**: Replaces frontmatter `title` (e.g., `MyClass` to "Custom Logger").
- **TransformExamplesTransformer**: Converts `Examples` to a table for Markdown.
- **ConditionalObsoleteTransformer**: Adds warnings to `Considerations` for `[Obsolete]` members.
  - Note: `ExcludePrivateTransformer` is replaced by `IncludedMembers` filtering in `AssemblyManager`.

Transformers are applied recursively to each `DocEntity` in the model tree (assembly → namespaces → types → members → parameters), allowing fine-grained transformations at any level.

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
  - Transformers are applied recursively like enrichers, allowing transformations at any entity level.
- **CLI (`CloudNimble.DotNetDocs.Tools`)**:
  - Args: `--customizations-path custom.json`.
  - Example: `dotnet docs generate --assembly MyLib.dll --xml MyLib.xml --output docs --conceptual-path conceptual/ --customizations-path custom.json`.
- **MSBuild (`CloudNimble.DotNetDocs.MSBuild`)**:
  - Task input: `<CustomizationsPath>custom.json</CustomizationsPath>`.
  - Uses `.csproj` for context (e.g., `<ConceptualPath>`).
  - Example: `<GenerateDocsTask ConceptualPath="conceptual/" CustomizationsPath="custom.json" OutputPath="$(DocsOutputPath)">`.
- **Plugins.AI (`CloudNimble.DotNetDocs.Plugins.AI`)**:
  - Custom `IDocEnricher` implementations (e.g., `SemanticKernelEnricher` for LLM generation).
  - Custom `IDocTransformer` implementations for AI-powered transformations.
  - Loaded via `--enrich-plugin` and `--transform-plugin` (CLI) or `<EnrichPlugin>` and `<TransformPlugin>` (MSBuild).
- **Tool-Specific Projects**:
  - `CloudNimble.DotNetDocs.Mintlify`: Maps `DocEntity` properties to `docs.json`.
  - `CloudNimble.DotNetDocs.Docusaurus`: Transforms `RelatedApis` into sidebar links.

## Why It Works
- **Flexibility**: JSON rules and plugins allow custom insertions, overrides, etc., without modifying core logic.
- **Simplicity**: Default pipeline (no rules) renders Roslyn/XML data directly.
- **Recursive Processing**: Both enrichers and transformers work recursively, enabling fine-grained control at any entity level.
- **Unified Context**: Single `ProjectContext` eliminates redundancy and simplifies the API.
- **Extensibility**: Plugins add custom enrichers/transformers; tool-specific projects manage format-specific dependencies.
- **Maintainability**: Testable components, human-readable JSON, async execution for performance.

## Next Steps
- Define JSON schema for rules (e.g., support for conditional logic).
- Determine transformer order and conflict resolution (applied sequentially like enrichers).
- Specify default enrichers/transformers (e.g., `LoadConceptualEnricher`, `OverrideTitleTransformer`).
- Add validation for customization rules (e.g., warn on invalid `RelatedApis`).
- Test recursive transformer application and `IncludedMembers` filtering.
- Implement concrete transformer classes (InsertUsageTransformer, OverrideTitleTransformer, etc.).

