# Race Condition Fix: External Type Placeholder Generation

## Problem Statement

When multiple assemblies define extension methods for the same external type (e.g., `Microsoft.Extensions.DependencyInjection.IServiceCollection`), each assembly creates its own "shadow class" `DocType` instance for that external type during the `RelocateExtensionMethods()` process in `AssemblyManager.cs`.

During placeholder generation in `DocumentationManager.ProcessAsync()` (lines 117-129), these assemblies are processed in parallel:

```csharp
var assemblyTasks = docAssemblies.Select(async assembly =>
{
    // STEP 1: Generate placeholder files for this assembly with all renderers
    var placeholderTasks = renderers.Select(renderer => renderer.RenderPlaceholdersAsync(assembly));
    await Task.WhenAll(placeholderTasks);

    // STEP 2: Load conceptual content for this assembly (after placeholders exist)
    await LoadConceptualAsync(assembly);
});

await Task.WhenAll(assemblyTasks);
```

### Race Condition Details

**Scenario**:
- Assembly A defines extension method `IServiceCollection.AddMyServiceA()`
- Assembly B defines extension method `IServiceCollection.AddMyServiceB()`

**What Happens**:
1. Assembly A's `AssemblyManager` creates shadow class for `IServiceCollection` with `AddMyServiceA()` member
2. Assembly B's `AssemblyManager` creates separate shadow class for `IServiceCollection` with `AddMyServiceB()` member
3. Both assemblies processed in parallel via `Task.WhenAll(assemblyTasks)`
4. Assembly A's renderer writes conceptual placeholders to `conceptual/Microsoft/Extensions/DependencyInjection/IServiceCollection/`
5. Assembly B's renderer writes to SAME path simultaneously
6. **Result**: File system conflict, last-write-wins, extension methods lost

### Why It Occurs

- **Shadow classes are created per-assembly**: Each `AssemblyManager.RelocateExtensionMethods()` creates its own external type references in a local `Dictionary<string, DocType> externalTypes` (line 963 in AssemblyManager.cs)
- **No cross-assembly deduplication**: Each assembly has its own `DocAssembly` model with separate shadow classes
- **Parallel placeholder generation**: Multiple assemblies write to same file paths concurrently
- **No file write synchronization**: Renderers have no awareness of other assemblies writing to same paths

## Solution: Pre-Merge External Types Before Placeholder Generation

### Approach

Move the `MergeDocAssembliesAsync()` call to execute BEFORE placeholder generation, ensuring only one shadow class exists per external type when placeholders are created.

### Implementation Changes

#### 1. Modify `DocumentationManager.ProcessAsync()` Method

**File**: `src/CloudNimble.DotNetDocs.Core/DocumentationManager.cs`

**Current Processing Order** (lines 104-157):
1. Collect all DocAssembly models
2. Generate placeholders per-assembly (PARALLEL)
3. Load conceptual content per-assembly (PARALLEL)
4. **Merge all DocAssembly models**
5. Apply enrichers and transformers
6. Apply renderers
7. Copy referenced documentation

**New Processing Order**:
1. Collect all DocAssembly models
2. **Merge all DocAssembly models** (MOVED UP)
3. Generate placeholders for merged model (ONCE)
4. Load conceptual content for merged model (ONCE)
5. Apply enrichers and transformers
6. Apply renderers
7. Copy referenced documentation

**Code Changes**:

```csharp
public async Task ProcessAsync(IEnumerable<(string assemblyPath, string xmlPath)> assemblies)
{
    // STEP 1: Collect all DocAssembly models
    var docAssemblies = new List<DocAssembly>();
    foreach (var (assemblyPath, xmlPath) in assemblies)
    {
        var manager = GetOrCreateAssemblyManager(assemblyPath, xmlPath);
        var model = await manager.DocumentAsync(projectContext);
        docAssemblies.Add(model);
    }

    // STEP 2: Merge all DocAssembly models (MOVED UP - was after conceptual processing)
    var mergedModel = await MergeDocAssembliesAsync(docAssemblies);

    if (projectContext.ConceptualDocsEnabled)
    {
        // STEP 3: Generate placeholder files for merged model with all renderers
        // No longer per-assembly, so shadow classes are deduplicated
        var placeholderTasks = renderers.Select(renderer => renderer.RenderPlaceholdersAsync(mergedModel));
        await Task.WhenAll(placeholderTasks);

        // STEP 4: Load conceptual content for merged model (after placeholders exist)
        await LoadConceptualAsync(mergedModel);
    }

    // STEP 5: Apply enrichers, transformers, and renderers
    foreach (var enricher in enrichers)
    {
        await enricher.EnrichAsync(mergedModel);
    }

    foreach (var transformer in transformers)
    {
        await transformer.TransformAsync(mergedModel);
    }

    foreach (var renderer in renderers)
    {
        await renderer.RenderAsync(mergedModel);
    }

    // STEP 6: Copy referenced documentation files if any references exist
    if (projectContext.DocumentationReferences.Any())
    {
        await CopyReferencedDocumentationAsync();
    }
}
```

### Why This Works

1. **Single Shadow Class Per External Type**: The existing `MergeDocAssembliesAsync()` method (lines 169-195) already handles merging types with the same symbol:
   - Line 206-207: Finds existing namespace by symbol comparison
   - Line 241-242: Finds existing type by symbol comparison
   - Line 252-254: Merges members from source type into existing type

2. **No File Conflicts**: After merge, only ONE `DocType` instance exists for `IServiceCollection`, containing extension methods from ALL assemblies. Placeholder generation writes to each file path exactly once.

3. **All Extension Methods Preserved**: The merge logic (lines 252-254) adds all members from source types into the existing type, so no extension methods are lost.

4. **Minimal Code Changes**: Just reorder operations - no new synchronization primitives, no locking, no architectural changes.

### Benefits

- **Eliminates race condition**: Only one file write per external type
- **Preserves all extension methods**: Existing merge logic handles member combination
- **Clean architecture**: Merged model represents "complete documentation", placeholders reflect that
- **No performance loss**: Parallel processing during assembly documentation phase still occurs
- **No new complexity**: No locks, semaphores, or concurrent collections needed

### Potential Issues and Mitigations

**Issue**: Conceptual loading expects assembly-specific directory structure
- **Mitigation**: `LoadConceptualAsync()` already works with any `DocAssembly` model, merge doesn't change directory structure

**Issue**: Placeholder generation might be slower (no longer parallel per-assembly)
- **Mitigation**: Placeholder generation is I/O bound, parallelism at file level (within renderer) more efficient than assembly level

## Testing Strategy

After implementation, verify:

1. **Multiple assemblies with same external type**:
   - Create two test assemblies both extending `IServiceCollection`
   - Verify single set of conceptual placeholder files generated
   - Verify all extension methods appear in placeholders

2. **No file conflicts**:
   - Monitor for file access exceptions during placeholder generation
   - Verify no "file in use" or "access denied" errors

3. **Conceptual content loads correctly**:
   - Verify `LoadConceptualAsync()` works with merged model
   - Verify shadow classes can load conceptual content

4. **Final rendered output**:
   - Verify all extension methods from all assemblies appear in final documentation
   - Verify shadow class pages include all extensions

## Related Files

- `src/CloudNimble.DotNetDocs.Core/AssemblyManager.cs` (lines 937-1095): Where shadow classes are created
- `src/CloudNimble.DotNetDocs.Core/DocumentationManager.cs` (lines 104-157): Processing pipeline to be modified
- `src/CloudNimble.DotNetDocs.Core/DocumentationManager.cs` (lines 169-291): Merge logic that enables this fix
- `specs/inheritance-and-extensions.md`: Original specification for extension method handling

## References

- Original extension method relocation feature: `specs/inheritance-and-extensions.md`
- AssemblyManager detailed analysis from investigation (see code analysis in discussion)
