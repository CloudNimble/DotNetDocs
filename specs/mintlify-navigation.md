# Mintlify Navigation Integration Specification

## Overview

This document outlines the integration of Mintlify's `docs.json` navigation generation into the `MintlifyRenderer`. The implementation leverages the existing `DocsJsonManager` from `Mintlify.Core` to automatically generate navigation structures alongside MDX documentation files.

## Architecture

### Core Components

1. **MintlifyRenderer** - Generates MDX files with frontmatter and manages docs.json generation
2. **DocsJsonManager** - Manages `docs.json` creation and navigation structure
3. **FileNamingOptions** - Controls File vs Folder mode organization
4. **MintlifyIcons** - Provides icon mappings for navigation elements
5. **MintlifyRendererOptions** - Configuration options for the renderer

### Dependency Injection

The Mintlify project extends the Core DI infrastructure without modifying it:

- `DotNetDocsMintlify_IServiceCollectionExtensions.cs` - Service registration
- `DotNetDocsMintlify_DotNetDocsBuilderExtensions.cs` - Pipeline builder extensions

**Important**: The MintlifyRenderer constructor requires all dependencies (no optional parameters) to enforce proper DI patterns.

## Navigation Structure Mapping

### File Mode (Flat Structure)

When `FileNamingOptions.NamespaceMode = File`:

```
output/
├── index.mdx                           # Assembly overview
├── docs.json                            # Navigation configuration
├── CloudNimble-Common.mdx              # Namespace documentation
├── CloudNimble-Common.MyClass.mdx      # Type documentation
├── CloudNimble-Common.MyInterface.mdx  # Type documentation
└── System-Collections-Generic.mdx      # Another namespace
```

Navigation structure:
```json
{
  "navigation": [
    {
      "pages": [
        "index",
        {
          "group": "API Reference",
          "icon": "code",
          "pages": [
            {
              "group": "CloudNimble.Common",
              "icon": "folder",
              "pages": [
                "CloudNimble-Common",
                "CloudNimble-Common.MyClass",
                "CloudNimble-Common.MyInterface"
              ]
            },
            {
              "group": "System.Collections.Generic",
              "icon": "folder",
              "pages": [
                "System-Collections-Generic"
              ]
            }
          ]
        }
      ]
    }
  ]
}
```

### Folder Mode (Hierarchical Structure)

When `FileNamingOptions.NamespaceMode = Folder`:

```
output/
├── index.mdx                    # Assembly overview
├── docs.json                     # Navigation configuration
├── CloudNimble/
│   └── Common/
│       ├── index.mdx            # Namespace documentation
│       ├── MyClass.mdx          # Type documentation
│       └── MyInterface.mdx      # Type documentation
└── System/
    └── Collections/
        └── Generic/
            └── index.mdx        # Namespace documentation
```

Navigation structure:
```json
{
  "navigation": [
    {
      "pages": [
        "index",
        {
          "group": "API Reference",
          "icon": "code",
          "pages": [
            {
              "group": "CloudNimble",
              "icon": "folder",
              "pages": [
                {
                  "group": "Common",
                  "icon": "folder",
                  "pages": [
                    "CloudNimble/Common/index",
                    "CloudNimble/Common/MyClass",
                    "CloudNimble/Common/MyInterface"
                  ]
                }
              ]
            },
            {
              "group": "System",
              "icon": "folder",
              "pages": [
                {
                  "group": "Collections",
                  "icon": "folder",
                  "pages": [
                    {
                      "group": "Generic",
                      "icon": "folder",
                      "pages": [
                        "System/Collections/Generic/index"
                      ]
                    }
                  ]
                }
              ]
            }
          ]
        }
      ]
    }
  ]
}
```

## Implementation Details

### Navigation Building from DocAssembly

The `MintlifyRenderer` builds navigation directly from the `DocAssembly` structure, maintaining semantic hierarchy:

```csharp
// Navigation is built from the DocAssembly model, not from generated files
internal void BuildNavigationStructure(DocsJsonConfig config, DocAssembly model, string outputPath)
{
    // Add index page
    config.Navigation.Pages.Add("index");
    
    // Create "API Reference" root group
    var apiReferenceGroup = new GroupConfig
    {
        Group = "API Reference",
        Icon = "code",
        Pages = new List<object>()
    };
    
    // Build navigation based on file/folder mode
    if (Context.FileNamingOptions.NamespaceMode == NamespaceMode.Folder)
    {
        BuildFolderModeNavigation(apiReferenceGroup.Pages, model, outputPath);
    }
    else
    {
        BuildFileModeNavigation(apiReferenceGroup.Pages, model, outputPath);
    }
    
    config.Navigation.Pages.Add(apiReferenceGroup);
}
```

### Navigation Building Algorithm

1. **Initialize**: Create default `DocsJsonConfig` with assembly metadata
2. **Build from Model**: Use the `DocAssembly` structure to create navigation hierarchy
3. **Apply Structure**: 
   - File Mode: Group by full namespace names
   - Folder Mode: Create nested groups matching folder structure
   - Apply appropriate icons from `MintlifyIcons`
4. **Generate**: Save `docs.json` at the end of rendering

### Icon Mapping

Icons are assigned based on entity type:

- Assembly: `cube`
- Namespace: `folder` or specialized (e.g., `database` for Data namespaces)
- Class: `rectangle-code`
- Interface: `brackets-curly`
- Struct: `box`
- Enum: `list`
- Delegate: `function`

## Dependency Injection Integration

### Service Registration

```csharp
// Direct registration
services.AddMintlifyServices();

// With configuration
services.AddMintlifyServices(options => 
{
    options.IncludeIcons = true;
    options.GenerateDocsJson = true;
});

// Via pipeline builder
services.AddDotNetDocsPipeline(pipeline => 
{
    pipeline.UseMintlifyRenderer()
           .ConfigureContext(ctx => 
           {
               ctx.OutputPath = "docs/api";
               ctx.FileNamingOptions.NamespaceMode = NamespaceMode.Folder;
           });
});
```

### Extension Methods

1. **IServiceCollection Extensions**:
   - `AddMintlifyRenderer()` - Registers `MintlifyRenderer` as `IDocRenderer`
   - `AddMintlifyServices()` - Registers renderer and supporting services

2. **DotNetDocsBuilder Extensions**:
   - `UseMintlifyRenderer()` - Adds Mintlify renderer to the pipeline

## Testing Strategy

### Unit Tests

1. **Navigation Generation Tests**:
   - Verify correct structure for File mode
   - Verify correct structure for Folder mode
   - Test nested namespace handling
   - Test empty namespace handling

2. **Icon Assignment Tests**:
   - Verify icons match entity types
   - Test specialized namespace icons

3. **Integration Tests**:
   - Generate documentation with docs.json
   - Verify navigation matches generated files
   - Test with real assemblies

### Baseline Tests

Create baseline files for:
- File mode docs.json
- Folder mode docs.json
- Complex namespace hierarchies
- Mixed entity types

## Implementation Checklist

### Phase 1: Infrastructure ✅
- [x] Create specs/mintlify-navigation.md
- [x] Create Extensions folder structure
- [x] Create DotNetDocsMintlify_IServiceCollectionExtensions.cs
- [x] Create DotNetDocsMintlify_DotNetDocsBuilderExtensions.cs

### Phase 2: Core Implementation ✅
- [x] Add DocsJsonManager to MintlifyRenderer
- [x] ~~Implement file tracking mechanism~~ (Replaced with DocAssembly-based approach)
- [x] Build navigation directly from DocAssembly structure
- [x] Add navigation building logic
- [x] Implement docs.json generation

### Phase 3: Navigation Building ✅
- [x] Implement File mode navigation structure
- [x] Implement Folder mode navigation structure
- [x] Handle nested namespaces
- [x] Apply icons from MintlifyIcons
- [x] Add "API Reference" root group
- [x] Fix namespace hierarchy in folder mode

### Phase 4: Testing ✅
- [x] Add unit tests for navigation generation
- [x] Test docs.json creation with options
- [x] Update tests to use proper DI patterns
- [x] Test both File and Folder modes
- [x] Verify DI registration works correctly

### Phase 5: Documentation 
- [ ] Update README with Mintlify usage
- [x] Add examples for different configurations
- [x] Document navigation customization options

## Configuration Options

Current configuration options:

```csharp
public class MintlifyRendererOptions
{
    /// <summary>
    /// Whether to generate docs.json navigation file
    /// </summary>
    public bool GenerateDocsJson { get; set; } = true;

    /// <summary>
    /// Whether to include icons in navigation
    /// </summary>
    public bool IncludeIcons { get; set; } = true;

    /// <summary>
    /// Custom navigation group order
    /// </summary>
    public List<string>? NamespaceOrder { get; set; }

    /// <summary>
    /// Custom docs.json template
    /// </summary>
    public DocsJsonConfig? Template { get; set; }
}
```

## Examples

### Basic Usage

```csharp
var services = new ServiceCollection();
services.AddMintlifyServices();

var provider = services.BuildServiceProvider();
var renderer = provider.GetRequiredService<IDocRenderer>();

var assembly = await AssemblyManager.LoadAsync("MyAssembly.dll");
await renderer.RenderAsync(assembly, "output", new ProjectContext());
```

### Advanced Configuration

```csharp
services.AddDotNetDocsPipeline(pipeline =>
{
    pipeline
        .UseMintlifyRenderer()
        .ConfigureContext(ctx =>
        {
            ctx.OutputPath = "docs/api";
            ctx.FileNamingOptions = new FileNamingOptions
            {
                NamespaceMode = NamespaceMode.Folder,
                NamespaceSeparator = '-'
            };
        });
});
```

## Future Enhancements

1. **Custom Navigation Templates**: Allow users to provide custom docs.json templates
2. **Navigation Sorting**: Configurable sorting strategies for navigation items
3. **Multi-Version Support**: Generate navigation for multiple API versions
4. **Localization**: Support for multi-language navigation
5. **Search Integration**: Add search configuration to docs.json
6. **Custom Grouping**: Allow custom grouping strategies beyond namespace