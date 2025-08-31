# XML Structural Tags Analysis
## Top-Level/Section Tags vs Inline Formatting

## Currently Extracted Structural Tags ✅

These are top-level tags that define sections of documentation and are extracted into dedicated properties:

### Fully Implemented
1. **`<summary>`** → `Summary` property
2. **`<remarks>`** → `Remarks` property  
3. **`<example>`** → `Examples` property
4. **`<param name="x">`** → `Usage` property (DocParameter)
5. **`<returns>`** → `Returns` property
6. **`<exception cref="x">`** → `Exceptions` collection
7. **`<typeparam name="x">`** → `TypeParameters` collection
8. **`<value>`** → `Value` property
9. **`<seealso cref="x">`** → `SeeAlso` collection

## Potentially Missing Structural Tags

### `<inheritdoc>` - Likely Handled by Roslyn
- **Status**: Need to verify
- **Expected Behavior**: Roslyn's IDocumentationCommentXmlSource should resolve inherited documentation
- **Test Needed**: Create a derived class with `<inheritdoc/>` and verify if ExtractDocumentationXml returns the inherited content
- **Action**: No extraction needed if Roslyn handles it; just need to verify

### `<include file="x" path="y">` - External Documentation
- **Status**: Not currently handled
- **Expected Behavior**: Should load external XML file and merge documentation
- **Current Impact**: External documentation files are ignored
- **Priority**: LOW - Rarely used in practice
- **Implementation**: Would need to:
  1. Parse the include tag
  2. Load the external file
  3. Apply the XPath to extract relevant nodes
  4. Merge into the documentation

### `<overloads>` - Method Overload Group Documentation
- **Status**: Not extracted
- **Purpose**: Provides documentation for a group of method overloads
- **Current Impact**: Overload group documentation is lost
- **Priority**: MEDIUM - Useful for methods with multiple overloads
- **Implementation**: Need a property to store overload documentation

## Verification Needed

### Test Roslyn's Handling of Special Cases

```csharp
// Test 1: Does Roslyn resolve <inheritdoc/>?
public class BaseClass
{
    /// <summary>Base method description</summary>
    public virtual void Method() { }
}

public class DerivedClass : BaseClass
{
    /// <inheritdoc/>
    public override void Method() { }
}
// Expected: ExtractDocumentationXml for DerivedClass.Method should return "Base method description"

// Test 2: Does Roslyn resolve <inheritdoc cref="SpecificMember"/>?
public class MyClass
{
    /// <summary>Original documentation</summary>
    public void MethodA() { }
    
    /// <inheritdoc cref="MethodA"/>
    public void MethodB() { }
}
// Expected: ExtractDocumentationXml for MethodB should return "Original documentation"

// Test 3: Does Roslyn handle <include/>?
/// <include file='docs.xml' path='doc/member[@name="MyMethod"]/*'/>
public void MyMethod() { }
// Expected: Need to check if Roslyn loads external file or if we need to handle it
```

## Structural Tags Completeness Assessment

| Tag | Purpose | Currently Extracted | Roslyn Handles? | Action Needed |
|-----|---------|-------------------|-----------------|---------------|
| `<summary>` | Brief description | ✅ Yes | N/A | None |
| `<remarks>` | Additional info | ✅ Yes | N/A | None |
| `<returns>` | Return value | ✅ Yes | N/A | None |
| `<param>` | Parameter desc | ✅ Yes | N/A | None |
| `<typeparam>` | Type param desc | ✅ Yes | N/A | None |
| `<exception>` | Exceptions thrown | ✅ Yes | N/A | None |
| `<value>` | Property value | ✅ Yes | N/A | None |
| `<example>` | Usage examples | ✅ Yes | N/A | None |
| `<seealso>` | Related items | ✅ Yes | N/A | None |
| `<inheritdoc>` | Inherit docs | ❓ Unknown | ❓ Likely | Verify |
| `<include>` | External docs | ❌ No | ❓ Unknown | Verify & Implement if needed |
| `<overloads>` | Overload group | ❌ No | No | Consider adding |

## Recommendations

### High Priority
1. **Verify `<inheritdoc>` handling** - Create test to confirm Roslyn resolves this
2. **Test `<include>` handling** - Check if Roslyn loads external files

### Medium Priority  
3. **Add `<overloads>` support** if tests show it's not handled by Roslyn

### Low Priority
4. **Implement `<include>` support** if Roslyn doesn't handle it (rarely used)

## Next Steps

1. Create unit tests to verify Roslyn's handling of `<inheritdoc>` and `<include>`
2. Based on test results, determine what additional extraction is needed
3. Focus on inline formatting tags (separate document) as the bigger challenge