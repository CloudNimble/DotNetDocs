# Transformation Pipeline for CloudNimble.DotNetDocs Rendering

This document outlines the transformation pipeline for `CloudNimble.DotNetDocs`, which processes the in-memory documentation model (`DocAssembly`, `DocType`, etc.) to produce customized outputs (Markdown, JSON, YAML, Mintlify, Docusaurus). The pipeline enables flexible customizations—insertions, overrides, exclusions, transformations, and conditions—while keeping the rendering process simple and maintainable.

## Purpose
The pipeline transforms the in-memory model (populated by `AssemblyManager` with Roslyn/XML and `DocEntity` properties like `Usage`, `Examples`) into user-specified outputs. It supports:
- **Insertions**: Add content (e.g., `Usage` from `docs/MyClass/usage.md`).
- **Overrides**: Replace elements (e.g., frontmatter `title` with custom text).
- **Exclusions**: Omit items (e.g., private members).
- **Transformations**: Modify content (e.g., format `Examples` as a table).
- **Conditions**: Apply rules contextually (e.g., add warnings for `[Obsolete]` members).

The pipeline ensures flexibility (via YAML rules or plugins) without overengineering, with defaults for straightforward API docs.

## Pipeline Structure
The pipeline is a chain of `ITransformer` implementations, executed sequentially or conditionally, modifying the model before rendering.

### Core Components
- **ITransformer**:
  ```csharp
  public interface ITransformer
  {
      Task TransformAsync(DocAssembly model, TransformationContext context);
  }
  ```
  Defines a transformation step (e.g., insert `Usage`, override `title`).
- **TransformationContext**:
  ```csharp
  public class TransformationContext
  {
      public string OutputFormat { get; init; } // e.g., "markdown", "mintlify"
      public Dictionary<string, object> Rules { get; init; } // YAML rules
      public string DocsPath { get; init; } // Path to external docs
  }
  ```
  Carries configuration (e.g., output format, rules from `customizations.yaml`).
- **RenderPipeline**:
  ```csharp
  public class RenderPipeline
  {
      private readonly List<ITransformer> transformers;
      public RenderPipeline(IEnumerable<ITransformer> transformers)
      {
          ArgumentNullException.ThrowIfNull(transformers);
          this.transformers = transformers.ToList();
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
  Chains transformers, modifying the model in-place.

### Example Transformers
- **InsertUsageTransformer**: Loads `Usage` from `docs/MyClass/usage.md` if empty.
- **OverrideTitleTransformer**: Replaces frontmatter `title` (e.g., `MyClass` to "Custom Logger").
- **ExcludePrivateTransformer**: Removes private `DocMember` instances if `context.Rules["excludePrivate"] == true`.
- **TransformExamplesTransformer**: Converts `Examples` to a table for Markdown.
- **ConditionalObsoleteTransformer**: Adds warnings to `Considerations` for `[Obsolete]` members.

### Customization Rules
Users define customizations in a YAML file (e.g., `customizations.yaml`), loaded via `--customizations-path` (CLI) or `<CustomizationsPath>` (MSBuild). Example:
```yaml
insertions:
  MyClass:
    Usage: "Centralized logging for distributed apps."
    Examples: |
      ```csharp
      Logger.LogInfo(new { UserId = 123 });
      ```
overrides:
  MyClass.DoWork:
    title: "Perform Work Operation"
exclusions:
  privateMembers: true
transformations:
  Examples: table
conditions:
  obsoleteMembers:
    insert: { Considerations: "Deprecated; use newer alternative." }
```

## Integration
- **Core (`CloudNimble.DotNetDocs.Core`)**:
  - `RenderPipeline` and `ITransformer` manage transformations.
  - `IOutputRenderer` invokes `RenderPipeline.TransformAsync` before generating output.
- **CLI (`CloudNimble.DotNetDocs.Tools`)**:
  - Args: `--customizations-path custom.yaml`.
  - Example: `dotnet docs generate --assembly MyLib.dll --xml MyLib.xml --output docs --docs-path docs/ --customizations-path custom.yaml`.
- **MSBuild (`CloudNimble.DotNetDocs.MSBuild`)**:
  - Task input: `<CustomizationsPath>custom.yaml</CustomizationsPath>`.
  - Uses `.csproj` for context (e.g., `<DocsPath>`).
  - Example: `<GenerateDocsTask DocsPath="docs/" CustomizationsPath="custom.yaml" OutputPath="$(DocsOutputPath)">`.
- **Plugins (`CloudNimble.DotNetDocs.Plugins`)**:
  - Custom `ITransformer` implementations (e.g., `LlmExamplesTransformer`).
  - Loaded via `--augment-plugin` (CLI) or `<AugmentPlugin>` (MSBuild).
- **Tool-Specific Projects**:
  - `CloudNimble.DotNetDocs.Mintlify`: Maps `DocEntity` properties to `docs.json`.
  - `CloudNimble.DotNetDocs.Docusaurus`: Transforms `RelatedApis` into sidebar links.

## Why It Works
- **Flexibility**: YAML rules and plugins allow custom insertions, overrides, etc., without modifying core logic.
- **Simplicity**: Default pipeline (no rules) renders Roslyn/XML data directly.
- **Extensibility**: Plugins add custom transformers; tool-specific projects handle format-specific needs.
- **Maintainability**: Testable transformers, human-readable YAML, async execution for performance.

## Next Steps
- Define YAML schema for rules (e.g., support for conditional logic).
- Determine transformer order and conflict resolution.
- Specify default transformers (e.g., `InsertDocsTransformer`, `ExcludePrivateTransformer`).
- Add validation for customization rules (e.g., warn on invalid `RelatedApis`).

