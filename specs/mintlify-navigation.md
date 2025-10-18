# Multi-Assembly Navigation Support for Mintlify Documentation

## Overview

This specification describes the implementation of multi-assembly navigation support in the DotNetDocs Mintlify renderer. The feature allows documentation generation from multiple .NET assemblies with configurable navigation structures.

### Goals

1. **Support Multiple Assemblies**: Process multiple assemblies in a single documentation generation run
2. **Flexible Navigation**: Offer both unified and per-assembly navigation modes
3. **Backward Compatibility**: Maintain existing behavior as default
4. **Configuration Flexibility**: Support both inline XML and external JSON templates
5. **Clean Output**: Generate well-organized navigation structures for Mintlify

## Design Decisions

### Navigation Modes

Two navigation modes will be supported:

- **Unified** (default): All assemblies' content merged under a single "API Reference" group
- **ByAssembly**: Each assembly gets its own top-level group in the navigation

### Configuration Approach

The implementation will support three configuration methods:

1. **Inline XML Template**: Define Mintlify configuration directly in .docsproj
2. **External JSON File**: Reference an existing docs.json template file
3. **Default Values**: Use sensible defaults when no configuration provided

### Processing Strategy

Instead of processing assemblies individually (which causes overwriting), all assemblies will be collected and processed together, allowing proper navigation merging.

## Implementation Details

### 1. NavigationMode Enumeration

**File**: `src/CloudNimble.DotNetDocs.Mintlify/NavigationMode.cs`

```csharp
namespace CloudNimble.DotNetDocs.Mintlify
{
    /// <summary>
    /// Specifies how navigation should be organized when generating documentation from multiple assemblies.
    /// </summary>
    public enum NavigationMode
    {
        /// <summary>
        /// All assemblies are merged into a single unified navigation structure.
        /// This is the default mode for backward compatibility.
        /// </summary>
        Unified = 0,

        /// <summary>
        /// Each assembly gets its own top-level group in the navigation.
        /// Useful for large solutions with distinct assembly boundaries.
        /// </summary>
        ByAssembly = 1
    }
}
```

### 2. MintlifyRendererOptions Updates

**File**: `src/CloudNimble.DotNetDocs.Mintlify/MintlifyRendererOptions.cs`

Add the following properties:

```csharp
/// <summary>
/// Gets or sets the navigation mode for multi-assembly documentation.
/// </summary>
/// <value>
/// The navigation organization mode. Default is NavigationMode.Unified.
/// </value>
public NavigationMode NavigationMode { get; set; } = NavigationMode.Unified;

/// <summary>
/// Gets or sets the group name used when NavigationMode is Unified.
/// </summary>
/// <value>
/// The name of the unified API reference group. Default is "API Reference".
/// </value>
public string UnifiedGroupName { get; set; } = "API Reference";
```

### 3. MintlifyRenderer Updates

**File**: `src/CloudNimble.DotNetDocs.Mintlify/MintlifyRenderer.cs`

Update `BuildNavigationStructure` method to handle navigation modes:

```csharp
internal void BuildNavigationStructure(DocsJsonConfig config, DocAssembly model)
{
    config.Navigation ??= new NavigationConfig();
    config.Navigation.Pages = ["index"];

    if (_options.NavigationMode == NavigationMode.Unified)
    {
        // Existing behavior: single API Reference group
        var apiReferenceGroup = new GroupConfig
        {
            Group = _options.UnifiedGroupName,
            Icon = _options.IncludeIcons ? "code" : null,
            Pages = []
        };

        BuildNavigationForAssembly(apiReferenceGroup.Pages, model);
        
        if (apiReferenceGroup.Pages.Count != 0)
        {
            config.Navigation.Pages.Add(apiReferenceGroup);
        }
    }
    else // NavigationMode.ByAssembly
    {
        // Group by assembly name
        var assembliesByName = model.Namespaces
            .GroupBy(ns => ns.Types.FirstOrDefault()?.AssemblyName ?? "Unknown")
            .OrderBy(g => g.Key);

        foreach (var assemblyGroup in assembliesByName)
        {
            var assemblyNav = new GroupConfig
            {
                Group = assemblyGroup.Key,
                Icon = _options.IncludeIcons ? "package" : null,
                Pages = []
            };

            // Build navigation for this assembly's namespaces
            var assemblyModel = new DocAssembly
            {
                AssemblyName = assemblyGroup.Key,
                Namespaces = assemblyGroup.ToList()
            };

            BuildNavigationForAssembly(assemblyNav.Pages, assemblyModel);

            if (assemblyNav.Pages.Count != 0)
            {
                config.Navigation.Pages.Add(assemblyNav);
            }
        }
    }
}
```

### 4. GenerateDocumentationTask Updates

**File**: `src/CloudNimble.DotNetDocs.Sdk/Tasks/GenerateDocumentationTask.cs`

Add new properties:

```csharp
/// <summary>
/// Gets or sets the navigation mode for Mintlify documentation.
/// </summary>
public string? MintlifyNavigationMode { get; set; }

/// <summary>
/// Gets or sets the unified group name for Mintlify documentation.
/// </summary>
public string? MintlifyUnifiedGroupName { get; set; }

/// <summary>
/// Gets or sets the path to an external docs.json template file.
/// </summary>
public string? DocsJsonTemplatePath { get; set; }

/// <summary>
/// Gets or sets the inline Mintlify template XML.
/// </summary>
public string? MintlifyTemplate { get; set; }
```

Update the Mintlify configuration section:

```csharp
case "mintlify":
    services.AddMintlifyServices(options =>
    {
        options.GenerateDocsJson = true;
        options.GenerateNamespaceIndex = true;
        options.IncludeIcons = true;

        // Parse navigation mode
        if (!string.IsNullOrWhiteSpace(MintlifyNavigationMode))
        {
            if (Enum.TryParse<NavigationMode>(MintlifyNavigationMode, true, out var navMode))
            {
                options.NavigationMode = navMode;
            }
        }

        // Set unified group name
        if (!string.IsNullOrWhiteSpace(MintlifyUnifiedGroupName))
        {
            options.UnifiedGroupName = MintlifyUnifiedGroupName;
        }

        // Load template from XML or file
        DocsJsonConfig? template = null;
        
        // First try inline XML template
        if (!string.IsNullOrWhiteSpace(MintlifyTemplate))
        {
            template = ParseMintlifyTemplate(MintlifyTemplate);
        }
        // Fall back to external file
        else if (!string.IsNullOrWhiteSpace(DocsJsonTemplatePath) && File.Exists(DocsJsonTemplatePath))
        {
            var json = File.ReadAllText(DocsJsonTemplatePath);
            template = JsonSerializer.Deserialize<DocsJsonConfig>(json, MintlifyConstants.JsonSerializerOptions);
        }

        if (template != null)
        {
            options.Template = template;
        }
    });
    break;
```

Add helper method to parse XML template:

```csharp
private DocsJsonConfig? ParseMintlifyTemplate(string xmlTemplate)
{
    try
    {
        var doc = XDocument.Parse($"<root>{xmlTemplate}</root>");
        var root = doc.Root;
        
        var config = new DocsJsonConfig
        {
            Name = root.Element("Name")?.Value,
            Description = root.Element("Description")?.Value,
            Theme = root.Element("Theme")?.Value ?? "mint"
        };

        // Parse Colors
        var colorsElement = root.Element("Colors");
        if (colorsElement != null)
        {
            config.Colors = new ColorsConfig
            {
                Primary = colorsElement.Element("Primary")?.Value
            };
        }

        // Parse Logo
        var logoElement = root.Element("Logo");
        if (logoElement != null)
        {
            config.Logo = new LogoConfig
            {
                Light = logoElement.Element("Light")?.Value,
                Dark = logoElement.Element("Dark")?.Value,
                Href = logoElement.Element("Href")?.Value
            };
        }

        return config;
    }
    catch (Exception ex)
    {
        Log.LogWarning($"Failed to parse MintlifyTemplate XML: {ex.Message}");
        return null;
    }
}
```

### 5. SDK Properties Updates

**File**: `src/CloudNimble.DotNetDocs.Sdk/Sdk/Sdk.props`

Add new properties with defaults:

```xml
<!-- Mintlify-specific configuration -->
<PropertyGroup Condition="'$(DocumentationType)' == 'Mintlify'">
    <!-- Navigation mode: Unified (default) or ByAssembly -->
    <MintlifyNavigationMode Condition="'$(MintlifyNavigationMode)' == ''">Unified</MintlifyNavigationMode>
    
    <!-- Group name for unified navigation mode -->
    <MintlifyUnifiedGroupName Condition="'$(MintlifyUnifiedGroupName)' == ''">API Reference</MintlifyUnifiedGroupName>
    
    <!-- Optional path to external docs.json template -->
    <DocsJsonTemplatePath Condition="'$(DocsJsonTemplatePath)' == '' AND Exists('$(DocumentationRoot)docs-template.json')">$(DocumentationRoot)docs-template.json</DocsJsonTemplatePath>
</PropertyGroup>
```

### 6. SDK Targets Updates

**File**: `src/CloudNimble.DotNetDocs.Sdk/Sdk/Sdk.targets`

Update the GenerateDocumentationTask invocation:

```xml
<CloudNimble.DotNetDocs.Sdk.Tasks.GenerateDocumentationTask
    Assemblies="@(AssemblyPaths)"
    OutputPath="$(DocumentationRootPath)"
    DocumentationType="$(DocumentationType)"
    NamespaceMode="$(NamespaceMode)"
    ApiReferencePath="$(ApiReferencePath)"
    MintlifyNavigationMode="$(MintlifyNavigationMode)"
    MintlifyUnifiedGroupName="$(MintlifyUnifiedGroupName)"
    DocsJsonTemplatePath="$(DocsJsonTemplatePath)"
    MintlifyTemplate="$(MintlifyTemplate)">
    <Output TaskParameter="GeneratedFiles" ItemName="GeneratedDocumentationFiles" />
</CloudNimble.DotNetDocs.Sdk.Tasks.GenerateDocumentationTask>
```

## Configuration Examples

### Example 1: Unified Navigation (Default)

```xml
<Project Sdk="Microsoft.Build.NoTargets/3.7.0">
    <PropertyGroup>
        <DocumentationType>Mintlify</DocumentationType>
        <!-- Uses default Unified mode with "API Reference" group -->
    </PropertyGroup>
</Project>
```

### Example 2: Per-Assembly Navigation

```xml
<Project Sdk="Microsoft.Build.NoTargets/3.7.0">
    <PropertyGroup>
        <DocumentationType>Mintlify</DocumentationType>
        <MintlifyNavigationMode>ByAssembly</MintlifyNavigationMode>
    </PropertyGroup>
</Project>
```

### Example 3: Custom Unified Group Name

```xml
<Project Sdk="Microsoft.Build.NoTargets/3.7.0">
    <PropertyGroup>
        <DocumentationType>Mintlify</DocumentationType>
        <MintlifyNavigationMode>Unified</MintlifyNavigationMode>
        <MintlifyUnifiedGroupName>Framework APIs</MintlifyUnifiedGroupName>
    </PropertyGroup>
</Project>
```

### Example 4: Inline Template Configuration

```xml
<Project Sdk="Microsoft.Build.NoTargets/3.7.0">
    <PropertyGroup>
        <DocumentationType>Mintlify</DocumentationType>
        <MintlifyNavigationMode>ByAssembly</MintlifyNavigationMode>
        <MintlifyTemplate>
            <Name>DotNetDocs API</Name>
            <Description>Comprehensive API documentation for DotNetDocs</Description>
            <Theme>mint</Theme>
            <Colors>
                <Primary>#0D9373</Primary>
            </Colors>
            <Logo>
                <Light>assets/logo-light.png</Light>
                <Dark>assets/logo-dark.png</Dark>
                <Href>https://github.com/CloudNimble/DotNetDocs</Href>
            </Logo>
        </MintlifyTemplate>
    </PropertyGroup>
</Project>
```

### Example 5: External Template File

```xml
<Project Sdk="Microsoft.Build.NoTargets/3.7.0">
    <PropertyGroup>
        <DocumentationType>Mintlify</DocumentationType>
        <MintlifyNavigationMode>ByAssembly</MintlifyNavigationMode>
        <DocsJsonTemplatePath>$(MSBuildProjectDirectory)\mintlify-template.json</DocsJsonTemplatePath>
    </PropertyGroup>
</Project>
```

## Expected Output

### Unified Mode Output

```json
{
  "navigation": {
    "pages": [
      "index",
      {
        "group": "API Reference",
        "icon": "code",
        "pages": [
          // All namespaces from all assemblies
        ]
      }
    ]
  }
}
```

### ByAssembly Mode Output

```json
{
  "navigation": {
    "pages": [
      "index",
      {
        "group": "CloudNimble.DotNetDocs.Core",
        "icon": "package",
        "pages": [
          // Namespaces from Core assembly
        ]
      },
      {
        "group": "CloudNimble.DotNetDocs.Mintlify",
        "icon": "package",
        "pages": [
          // Namespaces from Mintlify assembly
        ]
      }
    ]
  }
}
```

## Testing Strategy

1. **Unit Tests**: Test navigation building logic with mock DocAssembly models
2. **Integration Tests**: Test full pipeline with sample assemblies
3. **Configuration Tests**: Verify all configuration methods work correctly

## Future Enhancements

1. **Custom Grouping**: Allow arbitrary grouping strategies beyond assembly boundaries
2. **Navigation Ordering**: Support custom ordering of assemblies and namespaces
3. **Conditional Navigation**: Include/exclude assemblies based on conditions
4. **Navigation Merging**: Support merging with existing docs.json files