# Markdown Formatting Specification

## Overview

This document specifies how XML documentation tags are transformed into Markdown format by the `MarkdownXmlTransformer` class. The transformation occurs after XML extraction but before rendering, allowing different renderers to work with clean Markdown-formatted text.

## Architecture

XML documentation conversion is split into a format-neutral base class and one subclass per output format:

| Class | Responsibility |
|-------|----------------|
| `XmlTransformerBase` | Walks the documentation model, decides which strings contain tags, normalizes XML file indentation, indexes every entity so `cref` references resolve, resolves `seealso` references, and builds Microsoft Learn URLs for framework types and C# keywords. |
| `MarkdownXmlTransformer` | Writes each tag as Markdown. It is the only class that knows the output syntax. |

A transformer for another format, such as HTML, derives from `XmlTransformerBase` and implements `ConvertXml(string text, bool skipEscaping)`. It gets reference resolution, traversal, and Learn URLs without reimplementing them.

The model is transformed in place once per run, before any renderer runs, so a run produces one markup format. Data renderers carry whatever format the run's transformer produced:

- **JSON or YAML next to a Markdown or Mintlify renderer:** their string fields contain Markdown.
- **JSON or YAML on their own:** they register no transformer, so their string fields contain the original XML documentation tags.

### Registration

Each renderer's registration method registers the transformer and reference handler it needs. The `DotNetDocsBuilder` methods delegate to the same `IServiceCollection` methods, so both ways of adding a renderer register the same services.

| Registration | Renderer | Transformers | Reference handler |
|--------------|----------|--------------|-------------------|
| `AddMarkdownRenderer` / `UseMarkdownRenderer` | `MarkdownRenderer` | `MarkdownXmlTransformer` | `MarkdownDocReferenceHandler` |
| `AddMintlifyRenderer` / `UseMintlifyRenderer` | `MintlifyRenderer` | `MarkdownXmlTransformer`, `MintlifyMarkdownTransformer` | `MintlifyDocReferenceHandler` |
| `AddJsonRenderer` / `UseJsonRenderer` | `JsonRenderer` | none | none |
| `AddYamlRenderer` / `UseYamlRenderer` | `YamlRenderer` | none | none |
| `AddDotNetDocs` | Markdown, JSON, and YAML | whatever `AddMarkdownRenderer`, `AddJsonRenderer`, and `AddYamlRenderer` register | the same |

A new output format follows the same pattern: its registration method registers its renderer together with its `XmlTransformerBase` subclass.

## Performance Optimization

The transformer uses a performance-optimized approach:

1. **Pre-check Strategy**: Uses compiled regex to detect if any XML tags are present before processing
2. **Skip Rate**: Expected to skip ~80% of strings that contain no XML documentation tags
3. **Single Pass**: Indexes the assembly into a reference map once, then transforms every entity in a single recursive traversal
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

`XmlTransformerBase.GetFrameworkDocumentationUrl` is the only builder of Microsoft Learn type URLs. It takes a documentation ID without its prefix, so generic types are named by arity (`List`1`), which Learn writes as `-1`. `AssemblyManager` uses the same method for the remarks of external type references, which it writes as a `<see href>` tag so each format's transformer decides how the link is written.

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

### `XmlTransformerBase`

| Member | Kind | Purpose |
|--------|------|---------|
| `ConvertXml(string text, bool skipEscaping)` | `protected abstract` | Converts a string that contains tags into the output format. The text is never null or whitespace and its indentation is already normalized. |
| `ConvertText(string? text, bool skipEscaping)` | `protected virtual` | Skips null, whitespace, and tag-free strings, normalizes indentation, then calls `ConvertXml`. |
| `TransformEntityRecursive(DocEntity entity)` | `protected virtual` | Decides which properties are converted and walks the children. |
| `UpdateCurrentPath(DocEntity entity)` | `protected virtual` | Tracks the page of the entity being transformed, exposed as `CurrentPath`. |
| `GetLanguageKeywordUrl(string keyword)` | `protected virtual` | Microsoft Learn URL for a C# keyword. |
| `GetRelativePath(string fromPath, string toPath)` | `protected internal virtual` | Link path to another page. Override it for document-relative links or file extensions. |
| `ResolveReference(string rawReference, string currentPath)` | `protected internal` | Turns a `cref` into a `DocReference` with its path, anchor, and display name. |
| `RemoveCommonIndentation(string code)` | `protected internal static` | Removes the indentation every line of a block shares. |

### `MarkdownXmlTransformer`

Each Markdown conversion step is `protected virtual`, so a derived class can change one tag without reimplementing the rest:

- `ConvertXml` - Runs the steps below in order
- `ConvertSeeReferences` - Handle `<see>` tags
- `ConvertCodeTags` - Handle `<c>` and `<code>`
- `ConvertReferenceParams` - Handle parameter references
- `ConvertFormattingTags` - Handle text formatting
- `ConvertLists` - Handle list structures
- `EscapeRemainingXmlTags` - Final cleanup

Transformers that post-process Markdown rather than XML, such as `MintlifyMarkdownTransformer`, implement `IDocTransformer` directly and run after `MarkdownXmlTransformer`.

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

`XmlTransformerBase` counts the strings it processes and the strings it skips because they have nothing to convert, and writes them to the console after each transformation:

```
XML Transform Stats: 823/1029 strings skipped (80.0%)
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