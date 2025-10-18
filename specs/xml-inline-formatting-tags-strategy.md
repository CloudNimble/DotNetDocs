# XML Inline Formatting Tags Strategy

## The Challenge

Inline XML tags appear within text content and need to be handled differently by each renderer. Unlike structural tags that map to properties, these tags are embedded in strings and must be processed at render time.

## Inline XML Tags

### Reference Tags
1. **`<see cref="Type"/>`** - Inline reference to another type/member
2. **`<see href="url"/>`** - Inline hyperlink
3. **`<see langword="keyword"/>`** - Language keyword reference (null, true, false, etc.)
4. **`<paramref name="param"/>`** - Reference to a parameter
5. **`<typeparamref name="T"/>`** - Reference to a type parameter

### Code Formatting Tags
6. **`<c>text</c>`** - Inline code formatting
7. **`<code>block</code>`** - Code block formatting

### Text Formatting Tags
8. **`<para>text</para>`** - Paragraph separator
9. **`<b>text</b>`** - Bold text
10. **`<i>text</i>`** - Italic text
11. **`<u>text</u>`** - Underlined text
12. **`<br/>`** - Line break

### List Structure Tags
13. **`<list type="bullet|number|table">`** - List container
14. **`<item>`** - List item
15. **`<term>`** - Term in definition list
16. **`<description>`** - Description in definition list

## Current State

**All inline tags are currently preserved in the extracted text** but not parsed or processed. For example:
- Summary might contain: `"Gets the <see cref="ILogger"/> instance"`
- Returns might contain: `"Returns <c>null</c> if not found"`

## Rendering Challenges by Format

### JSON Renderer
- **Cannot support**: Hyperlinks, formatting
- **Strategy**: Strip all inline tags, preserve text content only
- **Example**: `"Gets the <see cref="ILogger"/> instance"` → `"Gets the ILogger instance"`

### YAML Renderer  
- **Cannot support**: Complex formatting, hyperlinks
- **Strategy**: Strip all inline tags, preserve text content only
- **Example**: Same as JSON

### Markdown Renderer
- **Can support**: All formatting via conversion
- **Strategy**: Convert XML tags to Markdown equivalents
- **Examples**:
  - `<see cref="Type"/>` → `[Type](Type.md)` or `` `Type` ``
  - `<see href="url"/>` → `[text](url)`
  - `<c>code</c>` → `` `code` ``
  - `<code>block</code>` → ` ```\nblock\n``` `
  - `<b>text</b>` → `**text**`
  - `<i>text</i>` → `*text*`
  - `<para>` → `\n\n`
  - Lists → Markdown list syntax

### HTML Renderer (Future)
- **Can support**: Everything natively
- **Strategy**: Convert to appropriate HTML

## Proposed Solution

### Option 1: Preserve Raw XML (Current State) ✅
**What we have now - XML tags are preserved in string properties**

**Pros:**
- No information loss
- Renderers can decide how to handle
- Simple extraction

**Cons:**
- Each renderer needs XML parsing logic
- Duplicated effort across renderers
- Raw XML in JSON/YAML output looks unprofessional

### Option 2: Pre-Process at Extraction
**Extract into a rich text model during AssemblyManager processing**

**Pros:**
- Single parsing implementation
- Consistent handling
- Clean data model

**Cons:**
- Complex data structure
- May lose flexibility
- Significant refactoring needed

### Option 3: Utility Methods for Renderers
**Provide shared utilities that renderers can call**

**Pros:**
- Reusable logic
- Renderers control when/how to use
- Incremental implementation

**Cons:**
- Still need to call from each renderer
- Not enforced consistently

## Recommended Approach

### Phase 1: Create Shared Utilities (Quick Win)
```csharp
public static class XmlDocumentationFormatter
{
    // Strip all XML tags
    public static string StripXmlTags(string text)
    {
        // Remove all XML tags, preserve content
        // <see cref="X"/> becomes X
        // <c>code</c> becomes code
    }
    
    // Convert to Markdown
    public static string ConvertToMarkdown(string text)
    {
        // <see cref="Type"/> → `Type` or [Type](Type.md)
        // <c>code</c> → `code`
        // <b>bold</b> → **bold**
        // <para> → \n\n
        // etc.
    }
    
    // Extract references (for link generation)
    public static IList<DocReference> ExtractReferences(string text)
    {
        // Find all <see cref="X"/> and <see href="Y"/>
        // Return structured data for link generation
    }
}
```

### Phase 2: Update Renderers
1. **JsonRenderer**: Call `StripXmlTags()` on all string properties during serialization
2. **YamlRenderer**: Call `StripXmlTags()` on all string properties during serialization  
3. **MarkdownRenderer**: Call `ConvertToMarkdown()` on all string properties during rendering

### Phase 3: Enhanced Model (Future)
Consider creating a `FormattedText` type that:
- Stores original XML
- Provides PlainText property (stripped)
- Provides Markdown property (converted)
- Provides References collection

## Implementation Priority

### Must Have (Phase 5 Completion)
1. ✅ Keep current extraction (preserves all XML)
2. ❌ Create `XmlDocumentationFormatter.StripXmlTags()` method
3. ❌ Update JsonRenderer to strip tags
4. ❌ Update YamlRenderer to strip tags

### Should Have (Phase 5.1)
5. ❌ Create `XmlDocumentationFormatter.ConvertToMarkdown()` method
6. ❌ Update MarkdownRenderer to convert tags
7. ❌ Handle `<see cref=""/>` link generation

### Nice to Have (Future)
8. ❌ Extract references for cross-referencing
9. ❌ Support `<list>` structures
10. ❌ Create FormattedText model

## Testing Strategy

### Test Cases for Tag Stripping
```csharp
[TestMethod]
public void StripXmlTags_RemovesSeeReferences()
{
    var input = "Gets the <see cref=\"ILogger\"/> instance";
    var result = XmlDocumentationFormatter.StripXmlTags(input);
    result.Should().Be("Gets the ILogger instance");
}

[TestMethod]
public void StripXmlTags_RemovesInlineCode()
{
    var input = "Returns <c>null</c> if not found";
    var result = XmlDocumentationFormatter.StripXmlTags(input);
    result.Should().Be("Returns null if not found");
}
```

### Test Cases for Markdown Conversion
```csharp
[TestMethod]
public void ConvertToMarkdown_ConvertsSeeToCode()
{
    var input = "Gets the <see cref=\"ILogger\"/> instance";
    var result = XmlDocumentationFormatter.ConvertToMarkdown(input);
    result.Should().Be("Gets the `ILogger` instance");
}

[TestMethod]
public void ConvertToMarkdown_ConvertsBoldTags()
{
    var input = "This is <b>important</b> text";
    var result = XmlDocumentationFormatter.ConvertToMarkdown(input);
    result.Should().Be("This is **important** text");
}
```

## Conclusion

The inline formatting tags represent a rendering challenge, not an extraction challenge. We're already extracting them correctly (embedded in the text). The solution is to:

1. **Short term**: Provide utilities to strip/convert tags at render time
2. **Long term**: Consider a richer text model that pre-processes formatting

This approach maintains backward compatibility while providing a path to cleaner output.