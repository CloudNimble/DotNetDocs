# Markdown Formatting Specification

## Overview

This document specifies how XML documentation tags are transformed into Markdown format by the `MarkdownXmlTransformer` class. The transformation occurs after XML extraction but before rendering, allowing different renderers to work with clean Markdown-formatted text.

## Performance Optimization

The transformer uses a performance-optimized approach:

1. **Pre-check Strategy**: Uses compiled regex to detect if any XML tags are present before processing
2. **Skip Rate**: Expected to skip ~80% of strings that contain no XML documentation tags
3. **Single Pass**: Builds reference dictionary while transforming in a single recursive traversal
4. **Compiled Regex**: All patterns are pre-compiled for maximum performance

### Detection Pattern

```regex
<(?:see|c|code|para|b|i|br|list|item|paramref|typeparamref|exception|returns|summary|remarks|example|value)
```

This pattern quickly identifies strings that need transformation, avoiding unnecessary processing of plain text.

## Transformation Rules

### Reference Tags

| XML Tag | Markdown Output | Notes |
|---------|-----------------|-------|
| `<see cref="Type"/>` | `[Type](../path/to/Type.md)` | Internal type with relative path |
| `<see cref="System.String"/>` | [`String`](https://learn.microsoft.com/dotnet/api/system.string) | .NET Framework type |
| `<see cref="T:System.String"/>` | [`String`](https://learn.microsoft.com/dotnet/api/system.string) | Fully qualified reference |
| `<see cref="UnknownType"/>` | `` `UnknownType` `` | Fallback to inline code |
| `<see href="https://example.com">text</see>` | `[text](https://example.com)` | External link with text |
| `<see href="https://example.com"/>` | `[link](https://example.com)` | External link without text |
| `<see langword="null"/>` | [`null`](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) | Language keyword |
| `<see langword="true"/>` | [`true`](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool) | Boolean literal |
| `<paramref name="param"/>` | *param* | Parameter reference |
| `<typeparamref name="T"/>` | *T* | Type parameter reference |

### Code Formatting Tags

| XML Tag | Markdown Output | Notes |
|---------|-----------------|-------|
| `<c>inline code</c>` | `` `inline code` `` | Inline code |
| `<code>block</code>` | ````csharp`<br>`block`<br>` ``` `` | Code block (default to C#) |
| `<code language="xml">block</code>` | ````xml`<br>`block`<br>` ``` `` | Code block with language |

### Text Formatting Tags

| XML Tag | Markdown Output | Notes |
|---------|-----------------|-------|
| `<para>text</para>` | `\n\ntext\n\n` | Paragraph separator |
| `<b>bold</b>` | `**bold**` | Bold text |
| `<i>italic</i>` | `*italic*` | Italic text |
| `<br/>` | `  \n` | Line break (two spaces + newline) |

### List Structure Tags

#### Bullet List
```xml
<list type="bullet">
  <item><description>First item</description></item>
  <item><description>Second item</description></item>
</list>
```
**Converts to:**
```markdown
- First item
- Second item
```

#### Numbered List
```xml
<list type="number">
  <item><description>First item</description></item>
  <item><description>Second item</description></item>
</list>
```
**Converts to:**
```markdown
1. First item
2. Second item
```

#### Definition List
```xml
<list type="table">
  <item>
    <term>Term 1</term>
    <description>Description 1</description>
  </item>
  <item>
    <term>Term 2</term>
    <description>Description 2</description>
  </item>
</list>
```
**Converts to:**
```markdown
**Term 1**
Description 1

**Term 2**
Description 2
```

#### Table List
```xml
<list type="table">
  <listheader>
    <term>Column 1</term>
    <description>Column 2</description>
  </listheader>
  <item>
    <term>Row 1, Col 1</term>
    <description>Row 1, Col 2</description>
  </item>
</list>
```
**Converts to:**
```markdown
| Column 1 | Column 2 |
|----------|----------|
| Row 1, Col 1 | Row 1, Col 2 |
```

## Microsoft Learn URL Patterns

### Type Resolution

The transformer generates Microsoft Learn documentation URLs for .NET Framework types:

| Type | Generated URL |
|------|---------------|
| `System.String` | `https://learn.microsoft.com/dotnet/api/system.string` |
| `System.Collections.Generic.List<T>` | `https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1` |
| `System.Tuple<T1,T2>` | `https://learn.microsoft.com/dotnet/api/system.tuple-2` |
| `System.Action` | `https://learn.microsoft.com/dotnet/api/system.action` |
| `System.IDisposable` | `https://learn.microsoft.com/dotnet/api/system.idisposable` |

### Language Keywords

| Keyword | Generated URL |
|---------|---------------|
| `null` | `https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null` |
| `true`/`false` | `https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool` |
| `void` | `https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/void` |
| `async` | `https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/async` |
| `await` | `https://learn.microsoft.com/dotnet/csharp/language-reference/operators/await` |

## Edge Cases and Error Handling

### Malformed XML

| Input | Output | Notes |
|-------|--------|-------|
| `<see cref="Type"` | `&lt;see cref="Type"` | Unclosed tag is escaped |
| `<unknown>text</unknown>` | `&lt;unknown&gt;text&lt;/unknown&gt;` | Unknown tags are escaped |
| `<c>code` | `&lt;c&gt;code` | Unclosed inline tag is escaped |

### Nested Tags

| Input | Output |
|-------|--------|
| `<b>Bold with <c>code</c></b>` | `**Bold with `code`**` |
| `<para>Text with <see cref="Type"/></para>` | `\n\nText with [Type](../Type.md)\n\n` |

### Empty Tags

| Input | Output |
|-------|--------|
| `<c></c>` | (removed) |
| `<see cref=""/>` | (removed) |
| `<para></para>` | `\n\n\n\n` |

## Extensibility

All transformation methods in `MarkdownXmlTransformer` are `protected virtual` to allow override in derived classes:

- `ConvertXmlToMarkdown` - Main orchestrator
- `ConvertSeeReferences` - Handle `<see>` tags
- `ConvertCodeTags` - Handle `<c>` and `<code>`
- `ConvertFormattingTags` - Handle text formatting
- `ConvertReferenceParams` - Handle parameter references
- `ConvertLists` - Handle list structures
- `ResolveTypeReference` - Resolve type to URL
- `GetMicrosoftDocsUrl` - Generate Microsoft Learn URLs
- `EscapeRemainingXmlTags` - Final cleanup

This allows creation of specialized transformers like `MintlifyXmlTransformer` that can override specific behaviors while reusing the base implementation.

## Implementation Notes

### Properties Transformed

The transformer processes these DocEntity properties:
- `Summary`
- `Remarks`
- `Returns`
- `Usage`
- `Examples`
- `Value`
- `BestPractices`
- `Patterns`
- `Considerations`

And these collection items:
- `Exceptions[].Description`
- `TypeParameters[].Description`
- `Parameters[].Usage`

### Performance Metrics

The transformer can optionally collect metrics:
- Total strings processed
- Strings with XML tags detected
- Strings skipped (no transformation needed)
- Average transformation time per string

Example output:
```
XML Transform Stats: 823/1029 strings skipped (80.0%)
Average transform time: 0.12ms per string with tags
```

## Testing Coverage

### Unit Tests

1. **Performance Tests**
   - Verify plain text bypasses transformation
   - Benchmark transformation speed
   - Validate compiled regex performance

2. **Tag Conversion Tests**
   - Each XML tag type individually
   - Nested tag combinations
   - Malformed XML handling
   - Empty tag handling

3. **Reference Resolution Tests**
   - Internal type resolution
   - External (.NET) type resolution
   - Unknown type fallback
   - Relative path generation

4. **Integration Tests**
   - Full DocAssembly transformation
   - Cross-reference validation
   - Real assembly documentation

### Test Data

Test cases should cover:
- Simple tags: `<c>code</c>`
- Complex tags: `<see cref="System.Collections.Generic.List{T}"/>`
- Nested tags: `<b>Bold with <c>code</c> and <see cref="Type"/></b>`
- Mixed content: Paragraphs with lists, code blocks, and references
- Edge cases: Malformed XML, empty tags, unknown tags

## Future Enhancements

1. **Caching**: Cache resolved type references for repeated lookups
2. **Configurable Output**: Allow configuration of output format (e.g., use bold instead of italic for parameters)
3. **Custom URL Patterns**: Support custom documentation sites beyond Microsoft Learn
4. **Localization**: Support localized Microsoft Learn URLs
5. **Validation**: Validate that internal cross-references actually exist