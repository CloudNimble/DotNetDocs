# Plan: Unified Documentation Portals via DocumentationReference

## Overview
Implement support for creating unified documentation portals from completely separate `.docsproj` files by introducing a `<DocumentationReference>` ItemGroup that works like `<ProjectReference>`. This enables scenarios like:
- **Microservices**: Single portal for multiple services in one repository
- **easyaf.dev**: Unified experience across separate open-source products

## Core Concept: "Easy As Fuck™️"

The collection `.docsproj` is a **normal, first-class documentation project** that:
1. Has its own assemblies to document (or not)
2. Has its own conceptual content (guides, tutorials, etc.)
3. Has its own `MintlifyTemplate` with custom branding
4. Generates its own `docs.json` using `DocumentationManager` + `ProjectContext`

**After** the collection project completes its normal documentation generation, we simply:
1. **Copy** markdown files from referenced projects into the collection's folder structure
2. **Load** each referenced project's `docs.json` using `DocsJsonManager`
3. **Apply** URL prefix to their navigation using existing `ApplyUrlPrefixToPages()`
4. **Insert** their navigation into the collection's `docs.json` (Tabs or Products arrays)

**No merging logic, no merge priorities, no complex decisions** - just copy files and combine navigation.

## Key Design Decisions

1. **Collection project builds FIRST** - Normal documentation generation completes before processing references
2. **NO compilation** - Assume referenced projects are already built with valid outputs
3. **Validation only** - Check if documentation outputs exist (e.g., `/api-reference` folder, `docs.json` for Mintlify)
4. **Path-based composition** - Use `Path` property to specify where docs are deposited
5. **Type-based integration** - Use `Type` property (Tabs/Products) to control Mintlify navigation insertion
6. **Non-destructive** - Source documents and `docs.json` files remain unchanged
7. **Leverage existing infrastructure** - Use `DocsJsonManager.ApplyUrlPrefixToPages()` that already exists

## Architecture: DocumentationManager Orchestrates, Renderers Handle Navigation

### Key Design:
1. **MSBuild Task**: Resolves `<DocumentationReference>` and populates `ProjectContext`
2. **DocumentationManager**: Copies files based on `DocumentationType` after rendering
3. **Renderer**: Handles navigation combining (Mintlify uses `DocsJsonManager`, others use their own format)

### Why This Works:
- ✅ **Reusable**: CLI, MSBuild, programmatic usage all use same code path
- ✅ **Testable**: No MSBuild required for unit tests
- ✅ **Renderer Polymorphism**: Each renderer knows its navigation format
- ✅ **Clean Separation**: Validation → Generation → File Copying → Navigation Combining

---

## Implementation Plan

### 1. **Define DocumentationReference ItemGroup Schema** (SDK)

**File**: `src/CloudNimble.DotNetDocs.Sdk/Sdk/Sdk.props`

Add new ItemGroup definition:
```xml
<ItemGroup>
  <!-- Example usage in .docsproj:
       <DocumentationReference Include="../ServiceA/ServiceA.docsproj"
                              Path="services/service-a"
                              Type="Tabs" />
  -->
  <DocumentationReference />
</ItemGroup>
```

**Metadata Properties**:
- `Include` (required) - Path to referenced `.docsproj` file
- `Path` (required) - URL path where docs will be deposited (e.g., `services/service-a`)
- `Type` (optional) - Integration type: `Tabs` or `Products` (default: `Tabs`)

---

### 2. **Create DocumentationReferenceResolver Task** (SDK Tasks)

**File**: `src/CloudNimble.DotNetDocs.Sdk.Tasks/DocumentationReferenceResolverTask.cs`

**Purpose**: Validate and resolve `<DocumentationReference>` items

**Responsibilities**:
1. Read each `<DocumentationReference>` item
2. Verify the referenced `.docsproj` file exists
3. Load the referenced project to extract properties:
   - `DocumentationRoot` - Base path for docs
   - `DocumentationType` - Type of docs (Mintlify, etc.)
   - `ApiReferencePath` - API reference folder
4. Validate outputs exist:
   - Check if `{DocumentationRoot}/{ApiReferencePath}` exists
   - For Mintlify: Check if `{DocumentationRoot}/docs.json` exists
5. If outputs missing AND `GenerateDocumentation=true` on referenced project:
   - Log warning: "Referenced project {name} has no documentation outputs. Build the project first."
6. Output resolved metadata as `<ResolvedDocumentationReference>` items

**Task Parameters**:
- Input: `ITaskItem[] DocumentationReferences`
- Input: `string Configuration` (for resolving bin paths)
- Output: `ITaskItem[] ResolvedDocumentationReferences`

**Item Metadata** (on ResolvedDocumentationReferences):
- `ProjectPath` - Full path to `.docsproj`
- `DocumentationRoot` - Resolved documentation root
- `DocsJsonPath` - Full path to `docs.json` (Mintlify only)
- `ApiReferencePath` - API reference folder path
- `DestinationPath` - Where to deposit merged docs (from `Path` attribute)
- `IntegrationType` - `Tabs` or `Products` (from `Type` attribute)

---

### 3. **Add DocumentationReference to ProjectContext** (Core)

**File**: `src/CloudNimble.DotNetDocs.Core/ProjectContext.cs`

Add new property and class:
```csharp
/// <summary>
/// Gets or sets the collection of documentation references to combine.
/// </summary>
/// <value>
/// A collection of external documentation projects to copy and combine into this project.
/// </value>
public List<DocumentationReference> DocumentationReferences { get; set; } = [];
```

**New Class**: `DocumentationReference.cs`
```csharp
namespace CloudNimble.DotNetDocs.Core
{
    /// <summary>
    /// Represents a reference to external documentation to be combined.
    /// </summary>
    public class DocumentationReference
    {
        /// <summary>
        /// Gets or sets the path to the referenced .docsproj file.
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// Gets or sets the documentation root path of the referenced project.
        /// </summary>
        public string DocumentationRoot { get; set; }

        /// <summary>
        /// Gets or sets the destination path where docs will be copied.
        /// </summary>
        public string DestinationPath { get; set; }

        /// <summary>
        /// Gets or sets the integration type (Tabs, Products, etc.).
        /// </summary>
        public string IntegrationType { get; set; } = "Tabs";

        /// <summary>
        /// Gets or sets the documentation type (Mintlify, DocFX, etc.).
        /// </summary>
        public string DocumentationType { get; set; }

        /// <summary>
        /// Gets or sets the path to the navigation file (docs.json, toc.yml, etc.).
        /// </summary>
        public string NavigationFilePath { get; set; }
    }
}
```

---

### 4. **Extend DocumentationManager to Copy Referenced Files** (Core)

**File**: `src/CloudNimble.DotNetDocs.Core/DocumentationManager.cs`

Add new method called **after** all renderers complete:

```csharp
/// <summary>
/// Processes multiple assemblies through the documentation pipeline.
/// </summary>
public async Task ProcessAsync(IEnumerable<(string assemblyPath, string xmlPath)> assemblies)
{
    // ... existing STEPS 1-4 (document, enrich, transform, render) ...

    // STEP 5: Copy referenced documentation files
    await CopyReferencedDocumentationAsync();

    // STEP 6: Combine navigation (renderer-specific)
    foreach (var renderer in renderers)
    {
        await renderer.CombineReferencedNavigationAsync(projectContext.DocumentationReferences);
    }
}

/// <summary>
/// Copies documentation files from referenced projects based on their DocumentationType.
/// </summary>
private async Task CopyReferencedDocumentationAsync()
{
    foreach (var reference in projectContext.DocumentationReferences)
    {
        var sourcePath = reference.DocumentationRoot;
        var destPath = Path.Combine(projectContext.DocumentationRootPath, reference.DestinationPath);

        // Get file patterns based on DocumentationType
        var patterns = GetFilePatternsForDocumentationType(reference.DocumentationType);

        foreach (var pattern in patterns)
        {
            await CopyFilesAsync(sourcePath, destPath, pattern, skipExisting: true);
        }
    }
}

/// <summary>
/// Gets the file patterns to copy based on documentation type.
/// </summary>
private string[] GetFilePatternsForDocumentationType(string documentationType)
{
    return documentationType?.ToLowerInvariant() switch
    {
        "mintlify" => new[] { "*.md", "*.mdx", "*.mdz", "images/**/*", "logo/**/*" },
        "docfx" => new[] { "*.md", "*.yml", "toc.yml", "images/**/*" },
        "mkdocs" => new[] { "*.md", "docs/**/*" },
        _ => new[] { "*.md", "*.html", "images/**/*" }
    };
}

/// <summary>
/// Copies files from source to destination, preserving folder structure.
/// </summary>
private async Task CopyFilesAsync(string sourcePath, string destPath, string pattern, bool skipExisting)
{
    // Implementation: glob source files, copy to dest with same relative path
    // Skip if file exists in destination (collection wins)
}
```

---

### 5. **Add CombineReferencedNavigationAsync to IDocRenderer** (Core)

**File**: `src/CloudNimble.DotNetDocs.Core/IDocRenderer.cs`

Add new method to interface:
```csharp
/// <summary>
/// Combines navigation from referenced documentation projects.
/// </summary>
/// <param name="references">The collection of documentation references to combine.</param>
/// <returns>A task representing the asynchronous operation.</returns>
Task CombineReferencedNavigationAsync(List<DocumentationReference> references);
```

**Default Implementation** in `RendererBase.cs`:
```csharp
/// <summary>
/// Default implementation does nothing (no navigation to combine).
/// </summary>
public virtual Task CombineReferencedNavigationAsync(List<DocumentationReference> references)
{
    // Most renderers don't have navigation files to combine
    return Task.CompletedTask;
}
```

---

### 6. **Implement Navigation Combining in MintlifyRenderer** (Mintlify)

**File**: `src/CloudNimble.DotNetDocs.Mintlify/MintlifyRenderer.cs`

Add new method:
```csharp
/// <summary>
/// Combines navigation from referenced Mintlify projects.
/// </summary>
public async Task CombineReferencedNavigationAsync(List<DocumentationReference> references)
{
    if (!references.Any()) return;

    // Load collection's docs.json (already generated by RenderAsync)
    var collectionDocsJsonPath = Path.Combine(Context.DocumentationRootPath, "docs.json");
    if (!File.Exists(collectionDocsJsonPath)) return;

    var collectionManager = new DocsJsonManager(collectionDocsJsonPath);
    collectionManager.Load();

    foreach (var reference in references)
    {
        // Skip non-Mintlify projects
        if (!reference.DocumentationType.Equals("Mintlify", StringComparison.OrdinalIgnoreCase))
            continue;

        // Load reference's docs.json
        var refManager = new DocsJsonManager(reference.NavigationFilePath);
        refManager.Load();

        if (!refManager.IsLoaded || refManager.Configuration?.Navigation?.Pages is null)
            continue;

        // Apply URL prefix to navigation
        refManager.ApplyUrlPrefixToPages(refManager.Configuration.Navigation.Pages, reference.DestinationPath);

        // Add to Tabs or Products based on IntegrationType
        if (reference.IntegrationType.Equals("Products", StringComparison.OrdinalIgnoreCase))
        {
            AddToProducts(collectionManager, refManager, reference);
        }
        else // Default to Tabs
        {
            AddToTabs(collectionManager, refManager, reference);
        }
    }

    // Save updated collection docs.json
    collectionManager.Save(collectionDocsJsonPath);
}

private void AddToTabs(DocsJsonManager collection, DocsJsonManager source, DocumentationReference reference)
{
    collection.Configuration.Tabs ??= new List<TabConfig>();

    collection.Configuration.Tabs.Add(new TabConfig
    {
        Name = GetProjectName(reference.ProjectPath),
        Url = reference.DestinationPath,
        Pages = source.Configuration.Navigation.Pages
    });
}

private void AddToProducts(DocsJsonManager collection, DocsJsonManager source, DocumentationReference reference)
{
    // Similar to AddToTabs but uses Products array (if supported)
    // Fallback to Tabs if Products not available
    collection.Configuration.Products ??= new List<ProductConfig>();

    collection.Configuration.Products.Add(new ProductConfig
    {
        Name = GetProjectName(reference.ProjectPath),
        Url = reference.DestinationPath,
        Pages = source.Configuration.Navigation.Pages
    });
}

private string GetProjectName(string projectPath)
{
    return Path.GetFileNameWithoutExtension(projectPath);
}
```

---

### 7. **Update GenerateDocumentationTask** (SDK Tasks)

**File**: `src/CloudNimble.DotNetDocs.Sdk.Tasks/GenerateDocumentationTask.cs`

Update to populate `ProjectContext.DocumentationReferences`:

```csharp
public override bool Execute()
{
    try
    {
        // ... existing code to create services and ProjectContext ...

        // NEW: Populate DocumentationReferences from resolved items
        if (ResolvedDocumentationReferences?.Length > 0)
        {
            foreach (var item in ResolvedDocumentationReferences)
            {
                context.DocumentationReferences.Add(new DocumentationReference
                {
                    ProjectPath = item.GetMetadata("ProjectPath"),
                    DocumentationRoot = item.GetMetadata("DocumentationRoot"),
                    DestinationPath = item.GetMetadata("DestinationPath"),
                    IntegrationType = item.GetMetadata("IntegrationType"),
                    DocumentationType = item.GetMetadata("DocumentationType"),
                    NavigationFilePath = item.GetMetadata("DocsJsonPath") // or toc.yml, etc.
                });
            }
        }

        // Configure services...
        services.AddDotNetDocsCore(context =>
        {
            // ... existing configuration ...
        });

        // ... rest of existing code ...

        // DocumentationManager.ProcessAsync() now handles:
        // 1. Normal documentation generation
        // 2. File copying from references
        // 3. Navigation combining via renderers
        var task = documentationManager.ProcessAsync(assemblyPairs);
        task.GetAwaiter().GetResult();

        return true;
    }
    catch (Exception ex)
    {
        Log.LogErrorFromException(ex);
        return false;
    }
}
```

**Add new task parameter**:
```csharp
/// <summary>
/// Gets or sets the resolved documentation references.
/// </summary>
public ITaskItem[]? ResolvedDocumentationReferences { get; set; }
```

---

### 8. **Update SDK Targets** (SDK)

**File**: `src/CloudNimble.DotNetDocs.Sdk/Sdk/Sdk.targets`

**Add new target BEFORE `GenerateDocumentation`**:

```xml
<!-- Resolve documentation references (validation only) -->
<Target Name="ResolveDocumentationReferences"
        Condition="'$(GenerateDocumentation)' == 'true' AND '@(DocumentationReference)' != ''"
        BeforeTargets="GenerateDocumentation">

    <Message Text="🔗 Resolving @(DocumentationReference->Count()) documentation references..." Importance="high" />

    <DocumentationReferenceResolverTask
        DocumentationReferences="@(DocumentationReference)"
        Configuration="$(Configuration)">
        <Output TaskParameter="ResolvedDocumentationReferences" ItemName="ResolvedDocumentationReferences" />
    </DocumentationReferenceResolverTask>

    <Message Text="✅ Resolved @(ResolvedDocumentationReferences->Count()) documentation references" Importance="high" />
</Target>
```

**Update `GenerateDocumentation` target**:
```xml
<CloudNimble.DotNetDocs.Sdk.Tasks.GenerateDocumentationTask
    Assemblies="@(AssemblyPaths)"
    OutputPath="$(DocumentationRootPath)"
    DocumentationType="$(DocumentationType)"
    ... (existing parameters) ...
    ResolvedDocumentationReferences="@(ResolvedDocumentationReferences)">
    <Output TaskParameter="GeneratedFiles" ItemName="GeneratedDocumentationFiles" />
</CloudNimble.DotNetDocs.Sdk.Tasks.GenerateDocumentationTask>
```

**Note**: No separate "CombineDocumentationReferences" target needed - it all happens inside `DocumentationManager.ProcessAsync()`!

---

### 9. **Add Tests**

**Test Projects**:
- `CloudNimble.DotNetDocs.Tests.Core` - DocumentationManager and file copying tests
- `CloudNimble.DotNetDocs.Tests.Sdk` - MSBuild task tests
- `CloudNimble.DotNetDocs.Tests.Mintlify` - Navigation combining tests
- `Mintlify.Tests.Core` - DocsJsonManager tests (extend existing)

**Test Scenarios**:

#### DocumentationReferenceResolverTask Tests (SDK)
1. ✅ Resolve single DocumentationReference with valid outputs
2. ✅ Resolve multiple DocumentationReferences
3. ✅ Warn when referenced .docsproj doesn't exist
4. ✅ Warn when documentation outputs are missing (no api-reference folder)
5. ✅ Warn when docs.json is missing for Mintlify projects
6. ✅ Correctly extract DocumentationRoot, DocumentationType, ApiReferencePath from referenced project
7. ✅ Populate all metadata on ResolvedDocumentationReference items
8. ✅ Handle relative and absolute paths correctly

#### DocumentationManager Tests (Core)
1. ✅ GetFilePatternsForDocumentationType returns correct patterns for Mintlify
2. ✅ GetFilePatternsForDocumentationType returns correct patterns for DocFX
3. ✅ GetFilePatternsForDocumentationType returns correct patterns for MkDocs
4. ✅ GetFilePatternsForDocumentationType returns default patterns for unknown types
5. ✅ CopyFilesAsync copies files with path prefix
6. ✅ CopyFilesAsync preserves folder structure
7. ✅ CopyFilesAsync skips existing files when skipExisting=true
8. ✅ CopyFilesAsync handles glob patterns correctly
9. ✅ CopyReferencedDocumentationAsync processes all references
10. ✅ ProcessAsync calls CopyReferencedDocumentationAsync after rendering
11. ✅ ProcessAsync calls renderer.CombineReferencedNavigationAsync for each renderer

#### MintlifyRenderer Tests (Mintlify)
1. ✅ CombineReferencedNavigationAsync skips when no references
2. ✅ CombineReferencedNavigationAsync loads collection's docs.json
3. ✅ CombineReferencedNavigationAsync skips non-Mintlify references
4. ✅ CombineReferencedNavigationAsync applies URL prefix to navigation
5. ✅ AddToTabs adds navigation to Tabs array correctly
6. ✅ AddToProducts adds navigation to Products array correctly
7. ✅ GetProjectName extracts name from .docsproj path
8. ✅ CombineReferencedNavigationAsync handles missing docs.json gracefully
9. ✅ CombineReferencedNavigationAsync saves updated collection docs.json
10. ✅ Nested navigation structures preserved with correct prefixes

#### DocsJsonManager Tests (extend existing in Mintlify.Tests.Core)
11. ✅ ApplyUrlPrefixToPages handles nested groups correctly
12. ✅ ApplyUrlPrefixToPages handles string pages
13. ✅ ApplyUrlPrefixToPages handles GroupConfig pages
14. ✅ ApplyUrlPrefixToPages handles TabConfig pages
15. ✅ ApplyUrlPrefixToPages handles AnchorConfig pages

#### Integration Tests (End-to-End)
16. ✅ Collection project with 3 service references (Tabs)
17. ✅ Collection project with 3 product references (Products)
18. ✅ Mixed Tabs and Products in same collection
19. ✅ Collection project with its own assemblies + references
20. ✅ Collection with only references (no assemblies)
21. ✅ Verify markdown files copied with correct paths
22. ✅ Verify navigation combined correctly with URL prefixes
23. ✅ Verify collection's existing files not overwritten

---

## Example Usage

### Microservices Use Case

**File**: `docs/MicroservicesPortal.docsproj`
```xml
<Project Sdk="CloudNimble.DotNetDocs.Sdk/1.0.0">
  <PropertyGroup>
    <DocumentationType>Mintlify</DocumentationType>
    <NamespaceMode>Folder</NamespaceMode>
    <MintlifyNavigationMode>Unified</MintlifyNavigationMode>
  </PropertyGroup>

  <ItemGroup>
    <!-- Reference individual service documentation -->
    <DocumentationReference Include="../services/UserService/UserService.docsproj"
                           Path="services/users"
                           Type="Tabs" />
    <DocumentationReference Include="../services/OrderService/OrderService.docsproj"
                           Path="services/orders"
                           Type="Tabs" />
    <DocumentationReference Include="../services/PaymentService/PaymentService.docsproj"
                           Path="services/payments"
                           Type="Tabs" />
  </ItemGroup>
</Project>
```

**Generated `docs.json` structure**:
```json
{
  "name": "Microservices Portal",
  "tabs": [
    {
      "name": "User Service",
      "url": "services/users",
      "pages": [/* prefixed navigation from UserService */]
    },
    {
      "name": "Order Service",
      "url": "services/orders",
      "pages": [/* prefixed navigation from OrderService */]
    },
    {
      "name": "Payment Service",
      "url": "services/payments",
      "pages": [/* prefixed navigation from PaymentService */]
    }
  ]
}
```

### easyaf.dev Use Case

**File**: `docs/EasyAF.Portal.docsproj`
```xml
<Project Sdk="CloudNimble.DotNetDocs.Sdk/1.0.0">
  <PropertyGroup>
    <DocumentationType>Mintlify</DocumentationType>
  </PropertyGroup>

  <ItemGroup>
    <!-- Reference separate open-source products -->
    <DocumentationReference Include="../../EasyAF.Core/docs/EasyAF.Core.docsproj"
                           Path="core"
                           Type="Products" />
    <DocumentationReference Include="../../EasyAF.Http/docs/EasyAF.Http.docsproj"
                           Path="http"
                           Type="Products" />
    <DocumentationReference Include="../../EasyAF.Validation/docs/EasyAF.Validation.docsproj"
                           Path="validation"
                           Type="Products" />
  </ItemGroup>
</Project>
```

---

## Implementation Tasks

### Phase 1: Core Infrastructure (ProjectContext & DocumentationReference)
- [ ] Create `DocumentationReference.cs` class in Core
- [ ] Add `DocumentationReferences` property to `ProjectContext.cs`
- [ ] Add `<DocumentationReference>` ItemGroup definition to `Sdk.props`
- [ ] Write unit tests for `DocumentationReference` class

### Phase 2: MSBuild Validation (DocumentationReferenceResolverTask)
- [ ] Create `DocumentationReferenceResolverTask.cs` with validation logic
- [ ] Register task in `Sdk.targets` with `UsingTask`
- [ ] Add `ResolveDocumentationReferences` target (before `GenerateDocumentation`)
- [ ] Write unit tests for `DocumentationReferenceResolverTask`
  - [ ] Test: Resolve single reference with valid outputs
  - [ ] Test: Resolve multiple references
  - [ ] Test: Warn on missing .docsproj file
  - [ ] Test: Warn on missing documentation outputs
  - [ ] Test: Correctly extract all metadata
  - [ ] Test: Handle relative/absolute paths

### Phase 3: DocumentationManager Extensions (File Copying)
- [ ] Add `GetFilePatternsForDocumentationType()` method
- [ ] Add `CopyFilesAsync()` method with glob support
- [ ] Add `CopyReferencedDocumentationAsync()` method
- [ ] Update `ProcessAsync()` to call copy method after rendering
- [ ] Write unit tests for DocumentationManager
  - [ ] Test: GetFilePatternsForDocumentationType for all types
  - [ ] Test: CopyFilesAsync with various patterns
  - [ ] Test: CopyFilesAsync preserves folder structure
  - [ ] Test: CopyFilesAsync skips existing files
  - [ ] Test: CopyReferencedDocumentationAsync processes all references

### Phase 4: Renderer Interface Extension
- [ ] Add `CombineReferencedNavigationAsync()` to `IDocRenderer`
- [ ] Add default implementation in `RendererBase.cs`
- [ ] Update all existing renderers to implement new method (empty implementation)

### Phase 5: Mintlify Navigation Combining
- [ ] Implement `CombineReferencedNavigationAsync()` in `MintlifyRenderer`
- [ ] Implement `AddToTabs()` helper method
- [ ] Implement `AddToProducts()` helper method
- [ ] Implement `GetProjectName()` helper method
- [ ] Write unit tests for MintlifyRenderer
  - [ ] Test: CombineReferencedNavigationAsync skips when no references
  - [ ] Test: Loads and saves collection's docs.json
  - [ ] Test: Skips non-Mintlify references
  - [ ] Test: Applies URL prefix correctly
  - [ ] Test: AddToTabs/AddToProducts work correctly
  - [ ] Test: Handles missing docs.json gracefully

### Phase 6: GenerateDocumentationTask Integration
- [ ] Add `ResolvedDocumentationReferences` parameter to task
- [ ] Add logic to populate `ProjectContext.DocumentationReferences`
- [ ] Update `GenerateDocumentation` target to pass resolved references
- [ ] Write unit tests for task integration

### Phase 7: DocsJsonManager Validation
- [ ] Verify `ApplyUrlPrefixToPages()` works for all navigation types
- [ ] Add unit tests if missing:
  - [ ] Test: Nested groups
  - [ ] Test: String pages
  - [ ] Test: GroupConfig/TabConfig/AnchorConfig pages

### Phase 8: Integration Tests
- [ ] Create test .docsproj files in `/test` folder
  - [ ] `test/CollectionPortal.Tabs.docsproj` - 3 service references with Tabs
  - [ ] `test/CollectionPortal.Products.docsproj` - 3 product references with Products
  - [ ] `test/ServiceA.docsproj` - Sample service documentation
  - [ ] `test/ServiceB.docsproj` - Sample service documentation
  - [ ] `test/ServiceC.docsproj` - Sample service documentation
- [ ] Write integration tests
  - [ ] Test: Build collection with service references (Tabs)
  - [ ] Test: Build collection with product references (Products)
  - [ ] Test: Mixed Tabs and Products
  - [ ] Test: Collection with own assemblies + references
  - [ ] Test: Collection with only references (no assemblies)
  - [ ] Test: Verify markdown files copied with correct paths
  - [ ] Test: Verify navigation combined correctly
  - [ ] Test: Verify collection's files not overwritten

### Phase 9: Documentation & Examples
- [ ] Update this spec with any implementation learnings
- [ ] Add code examples showing actual implementations
- [ ] Create example collection projects showing both use cases
- [ ] Document any edge cases or limitations discovered

---

## Benefits

✅ **Easy As Fuck™️** - Leverages existing infrastructure, no complex merging logic
✅ **Collection is first-class** - Normal documentation project with own content/branding
✅ **No compilation** - Assumes referenced projects are already built
✅ **Validation only** - Warns when outputs are missing
✅ **Flexible composition** - Path-based organization
✅ **Type-safe integration** - Tabs vs Products explicit
✅ **Non-destructive** - Source files unchanged
✅ **Simple implementation** - Just copy files + combine navigation
✅ **Testable** - Clear separation: validation → generation → combining
✅ **MSBuild native** - Familiar `<ItemGroup>` pattern

---

## Notes

- **Collection project builds FIRST** - Normal `GenerateDocumentation` target completes before combining
- **Mintlify-only initially** - Only renderer with navigation generation
- **ApplyUrlPrefixToPages exists** - Already in `DocsJsonManager` (line 1569)
- **No services/DI** - Tasks are simple, self-contained
- **Collection wins conflicts** - If same file exists in both, collection's file is kept
