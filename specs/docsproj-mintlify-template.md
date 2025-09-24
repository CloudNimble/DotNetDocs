# .docsproj Mintlify Template Processing

## Overview

This document captures important learnings from debugging the Mintlify template processing in `.docsproj` files, specifically around preserving template-defined navigation order and icons during the build process.

## Problem Statement

The build process was not respecting the group order and icons defined in the MintlifyTemplate XML within `.docsproj` files. Template-defined groups were losing their icons and appearing in alphabetical order rather than the template-specified order.

## Expected vs Actual Behavior

### Expected (Template Order)
```xml
<Group Name="Getting Started" Icon="stars">
<Group Name="Guides" Icon="dog-leashed">
<Group Name="Providers" Icon="books">
<Group Name="Plugins" Icon="outlet">
<Group Name="Learnings" Icon="">
```

### Actual (Generated Output)
```json
{
  "group": "Getting Started",  // ❌ Missing icon
  "pages": [...]
},
{
  "group": "Guides",          // ❌ Missing icon, wrong position
  "pages": [...]
},
{
  "group": "Learnings",       // ❌ Missing icon, wrong position
  "pages": [...]
}
```

## Root Cause Analysis

### Investigation Process

1. **Initial Hypothesis**: Template navigation was being cleared by `PopulateNavigationFromPath()`
2. **Fix Attempted**: Modified `PopulateNavigationFromPath()` to preserve existing navigation via `preserveExisting` parameter
3. **Discovery**: Both `preserveExisting: true` and `preserveExisting: false` produced identical results
4. **Conclusion**: The issue is deeper in the template loading/parsing chain

### Key Findings

1. **Template XML Parsing Works**: The XML template is correctly parsed with icons and proper order
2. **Navigation Merge Logic Is Correct**: The merge logic properly preserves template properties when merging with discovered content
3. **Template Loading Issue**: The template is either:
   - Not being loaded with expected groups and icons, OR
   - Being overridden after the JSON serialization/deserialization cycle

### Evidence

The fact that both preservation modes produce identical output indicates that the template groups are not making it through the loading process with their icons intact.

## Technical Implementation

### Template Processing Flow

```
.docsproj XML Template
    ↓ (ParseTemplate)
GenerateDocumentationTask.ParseGroupConfig()
    ↓ (options.Template = template)
MintlifyRenderer.Render()
    ↓ (JSON serialize/deserialize)
DocsJsonManager.Load()
    ↓ (PopulateNavigationFromPath)
File System Discovery & Merge
    ↓ (BuildNavigationStructure)
API Reference Addition
    ↓
Final docs.json
```

### Key Components

#### 1. PopulateNavigationFromPath Enhancement

```csharp
public void PopulateNavigationFromPath(string path, string[]? fileExtensions = null, bool includeApiReference = false, bool preserveExisting = true)
{
    if (preserveExisting)
    {
        // Create temporary config for discovered navigation
        var discoveredNavigation = new NavigationConfig { Pages = [] };
        PopulateNavigationFromDirectory(path, discoveredNavigation.Pages, path, fileExtensions, includeApiReference, true);

        // Merge discovered navigation into existing template
        MergeNavigation(Configuration.Navigation, discoveredNavigation);
    }
    else
    {
        // Legacy behavior: clear and repopulate
        Configuration.Navigation.Pages.Clear();
        PopulateNavigationFromDirectory(path, Configuration.Navigation.Pages, path, fileExtensions, includeApiReference, true);
    }
}
```

#### 2. Navigation Merge Logic

The merge logic correctly preserves template properties:

```csharp
internal static void MergeGroupConfig(GroupConfig target, GroupConfig source, MergeOptions? options = null)
{
    // Only overwrite if source has non-null values
    if (!string.IsNullOrWhiteSpace(source.Tag))
        target.Tag = source.Tag;
    if (source.Icon is not null)          // ✅ Preserves template icons
        target.Icon = source.Icon;
    // ... other properties
}
```

## Outstanding Issues

### Primary Issue: Template Loading Chain

The template groups are not making it through the complete loading process with their icons. Investigation needed:

1. **XML Parsing**: Verify `ParseGroupConfig()` correctly extracts icons
2. **Options Transfer**: Ensure `options.Template = template` preserves all properties
3. **JSON Serialization**: Check that serialize/deserialize cycle in `MintlifyRenderer.cs:94-95` maintains icons
4. **Configuration Loading**: Verify `DocsJsonManager.Load()` preserves template structure

### Secondary Issue: Order Preservation

Even with correct merge logic, the final order may not match template due to alphabetical directory processing.

## Best Practices Learned

### 1. Template-First Processing
The build process should:
- **START** from the template navigation structure
- **MERGE** discovered content into template groups
- **PRESERVE** template order and properties

### 2. Defensive Merging
```csharp
// Good: Preserve existing template properties
if (source.Property is not null)
    target.Property = source.Property;

// Bad: Always overwrite
target.Property = source.Property;
```

### 3. Navigation Structure Validation
Both merging modes should be tested to ensure template preservation:
```csharp
// Test both preservation modes during development
PopulateNavigationFromPath(..., preserveExisting: true);   // Template preservation
PopulateNavigationFromPath(..., preserveExisting: false);  // Legacy mode
```

### 4. Icon Handling
Icons are optional properties that must be explicitly preserved:
```xml
<Group Name="Learnings" Icon="">  <!-- Empty icon is valid -->
<Group Name="Guides" Icon="dog-leashed">  <!-- Named icon -->
```

## Debugging Strategies

### 1. Preservation Mode Testing
Testing both `preserveExisting` modes helps isolate whether issues are in:
- Template loading (both modes fail similarly)
- Merge logic (modes produce different results)

### 2. JSON Serialization Cycle Verification
The template goes through serialization in `MintlifyRenderer.cs`:
```csharp
var json = JsonSerializer.Serialize(docsConfig, MintlifyConstants.JsonSerializerOptions);
_docsJsonManager.Load(json);
```

### 3. Template vs Default Detection
Check if `_options.Template` is null, which would cause fallback to `CreateDefault()`:
```csharp
docsConfig = _options.Template ?? DocsJsonManager.CreateDefault(...);
```

## Future Improvements

1. **Template Validation**: Add validation to ensure templates are loaded with expected properties
2. **Debug Logging**: Add logging at each stage of template processing
3. **Icon Preservation Tests**: Unit tests specifically for icon preservation during merge
4. **Order Preservation**: Ensure template-defined order is maintained through the entire pipeline

## Related Files

- `src/Mintlify.Core/DocsJsonManager.cs` - Navigation merging logic
- `src/CloudNimble.DotNetDocs.Mintlify/MintlifyRenderer.cs` - Template loading and processing
- `src/CloudNimble.DotNetDocs.Sdk.Tasks/GenerateDocumentationTask.cs` - Template XML parsing
- `src/CloudNimble.DotNetDocs.Docs/CloudNimble.DotNetDocs.Docs.docsproj` - Template definition

## Resolution Status

- ✅ **PopulateNavigationFromPath**: Enhanced to preserve existing navigation
- ✅ **Merge Logic**: Correctly preserves template properties when merging
- ❌ **Template Loading**: Icons still not preserved - requires further investigation in template loading chain
- ❌ **Order Preservation**: Template order not maintained - linked to icon preservation issue

The foundation for proper template preservation is now in place. The remaining work involves debugging the template loading chain to ensure icons and order are preserved from XML parsing through to final navigation structure.