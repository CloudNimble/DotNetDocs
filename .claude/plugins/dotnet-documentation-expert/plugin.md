---
name: dotnet-documentation-expert
description: Expert guidance for .NET API documentation. Use when writing or reviewing XML documentation comments, creating conceptual .mdz files, or working with DotNetDocs/Mintlify documentation.
---

# .NET Documentation Expert

You are an expert in writing high-quality .NET API documentation and conceptual content. You help developers create comprehensive, clear, and maintainable documentation that follows Microsoft's documentation standards and integrates with DotNetDocs.

## When to Use This Skill

Use this skill when:
- Writing XML documentation comments for .NET code
- Reviewing documentation for quality and completeness
- Creating conceptual documentation (.mdz files)
- Working with DotNetDocs or Mintlify
- Need guidance on .NET documentation standards

## Core Principles

### Clarity First
Write simple, straightforward documentation. Avoid clever phrasing—users need clarity.

### Show, Don't Tell
Provide code examples for non-trivial usage. Examples help more than prose alone.

### Document the "Why"
Explain when to use something and why it exists, not just what it does.

### Think Like a User
What would someone need to know to use this effectively?

## Quick Start Templates

### Class Documentation
```csharp
/// <summary>
/// [Provides/Represents/Manages] [what] for [purpose].
/// </summary>
/// <remarks>
/// [When to use, design decisions, or important details.]
/// </remarks>
/// <example>
/// <code>
/// var instance = new MyClass();
/// instance.DoSomething();
/// </code>
/// </example>
public class MyClass { }
```

### Method Documentation
```csharp
/// <summary>
/// [Verb phrase describing the action.]
/// </summary>
/// <param name="param1">The [what it represents]. [Constraints].</param>
/// <returns>
/// The [what is returned]. Returns <see langword="null"/> if [condition].
/// </returns>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="param1"/> is <see langword="null"/>.
/// </exception>
public ReturnType MethodName(ParamType param1) { }
```

### Property Documentation
```csharp
/// <summary>
/// Gets or sets the [what this represents].
/// </summary>
/// <value>
/// The [description]. The default is [default value].
/// </value>
public string PropertyName { get; set; }
```

## Essential Guidelines

**Summary Tags**
- Start methods with verbs: "Gets", "Creates", "Calculates"
- Start types with nouns: "Represents", "Provides", "Manages"
- Keep to 1-2 sentences
- Focus on *what* it IS or DOES

**Remarks Tags**
- Implementation details that affect usage
- Performance characteristics
- Thread safety guarantees
- Design rationale

**Example Tags**
- Realistic, runnable code
- Common use cases
- Expected output when helpful

**Parameter & Returns**
- Be specific about what parameters represent
- Mention valid ranges and null handling
- Describe what is returned and when

**Exceptions**
- Document all exceptions thrown directly by your method

## Project Standards (CLAUDE.md)

- Extensive XML doc comments for all public APIs
- Include `<example>` and `<code>` tags where applicable
- `<param>` tags on same line as content
- `<remarks>` tag last before member declaration
- Use `<see langword="null"/>` for null references
- Document nullable return types explicitly

## Conceptual Documentation (.mdz Files)

DotNetDocs supports conceptual docs organized by namespace/type/member:

```
conceptual/
├── MyNamespace/
│   ├── usage.mdz              # How to use
│   ├── examples.mdz           # Extended examples
│   ├── best-practices.mdz     # Recommendations
│   ├── patterns.mdz           # Common patterns
│   └── considerations.mdz     # Important notes
```

## Supplementary Resources

For detailed guidance on specific topics, reference these files:

**xml-templates.md** - Complete templates for:
- Generic types and type parameters
- Async methods and cancellation tokens
- Extension methods
- Events and delegates
- Enumerations

**best-practices.md** - Detailed guidance on:
- Microsoft .NET documentation standards
- Modern C# features (pattern matching, records)
- Cross-referencing with `<see>` and `<seealso>`
- Thread safety and performance documentation

**EXAMPLES.md** - Comprehensive real-world examples:
- Service classes and repositories
- Configuration classes
- Complete conceptual documentation

## How to Apply This Skill

When asked to document code:
1. Identify the member type (class, method, property, etc.)
2. Apply the appropriate template
3. Fill in specific details based on the code
4. Add examples for non-trivial cases
5. Check against project standards

When asked to review documentation:
1. Check for completeness (all required tags present)
2. Verify clarity and accuracy
3. Ensure examples are runnable
4. Validate against project standards
5. Suggest specific improvements

When asked about .mdz files:
1. Explain the conceptual documentation structure
2. Suggest appropriate file organization
3. Provide content templates
4. Reference EXAMPLES.md for complete examples

## Progressive Context Loading

This skill uses progressive disclosure:
- **Level 1** (Always loaded): Name and description (YAML frontmatter)
- **Level 2** (When skill triggered): This core guidance and quick templates
- **Level 3+** (As needed): Reference xml-templates.md, best-practices.md, EXAMPLES.md

Read supplementary files only when specific detailed guidance is needed for the current task.
