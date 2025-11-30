# .NET Documentation Expert Plugin

A comprehensive Claude plugin that provides expert guidance for writing high-quality .NET API documentation and conceptual content.

## Overview

The .NET Documentation Expert plugin transforms Claude into a specialized assistant for creating, reviewing, and improving .NET documentation. It provides deep expertise in XML documentation comments, conceptual documentation, and integration with the DotNetDocs documentation pipeline.

## Progressive Disclosure Architecture

This plugin uses a **three-level progressive disclosure** pattern to efficiently manage context:

### Level 1: Startup Context (Always Loaded)
The plugin's YAML frontmatter (`name` and `description`) is loaded into Claude's system prompt at startup. This allows Claude to understand when the plugin should be activated without loading the full content.

```yaml
---
name: dotnet-documentation-expert
description: Expert guidance for .NET API documentation. Use when writing or reviewing XML documentation comments, creating conceptual .mdz files, or working with DotNetDocs/Mintlify documentation.
---
```

### Level 2: Core Guidance (Loaded When Activated)
When the plugin is triggered, the main `plugin.md` content is loaded, providing:
- Core principles (Clarity First, Show Don't Tell, Document the Why, Think Like a User)
- Quick-start templates for common scenarios (class, method, property documentation)
- Essential guidelines for summaries, remarks, examples, parameters, returns, and exceptions
- Project standards from CLAUDE.md
- References to supplementary resources

This level gives you what you need for most documentation tasks without overloading context.

### Level 3+: Detailed Resources (Loaded As Needed)
When you need specific, detailed guidance, the plugin references supplementary files:

- **[xml-templates.md](xml-templates.md)** - Complete templates for:
  - Generic types with constraints
  - Async methods and CancellationToken
  - Extension methods
  - Events, delegates, and EventArgs
  - Enumerations (simple and flags)
  - Interfaces with multiple members
  - Indexers, structs, records, operators
  - IDisposable implementation

- **[best-practices.md](best-practices.md)** - Detailed guidance on:
  - Microsoft .NET documentation standards
  - Modern C# features (nullable reference types, pattern matching, records, init-only properties)
  - Cross-referencing with `<see>`, `<seealso>`, `<paramref>`, `<typeparamref>`, `<see langword>`
  - Thread safety documentation
  - Performance characteristics
  - Lists and formatting with `<para>` and `<list>` tags
  - Common pitfalls to avoid

- **[EXAMPLES.md](EXAMPLES.md)** - Comprehensive real-world examples of fully documented classes

These files are only loaded when you explicitly need that level of detail, keeping the plugin efficient while maintaining access to comprehensive guidance.

### Why This Matters

Progressive disclosure provides several benefits:
- **Faster activation**: Minimal context loaded at startup
- **Efficient operation**: Only load what you need for each task
- **Comprehensive coverage**: Full detailed guidance available when needed
- **Better performance**: Reduced token usage for routine documentation tasks

## Features

### 📝 XML Documentation Comments
- Complete templates for all .NET member types (classes, methods, properties, events, enums)
- Best practices for writing clear, accurate, and maintainable documentation
- Guidance on all XML tags: `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<exception>`, `<example>`, etc.
- Project-specific standards compliance (CLAUDE.md)

### 📚 Conceptual Documentation
- Structure and organization for `.mdz` conceptual documentation files
- Templates for usage guides, examples, best practices, patterns, and considerations
- Integration with DotNetDocs documentation pipeline
- Mintlify-enhanced markdown features

### 🎯 .NET Best Practices
- Microsoft documentation standards
- Null reference type documentation patterns
- Modern C# feature documentation (async, generics, pattern matching)
- Thread safety and performance documentation

### 🔍 Code Review & Quality
- Review existing documentation for completeness and quality
- Identify missing or inadequate documentation
- Suggest improvements based on best practices
- Ensure consistency across codebases

## Installation

### In This Repository

The plugin is automatically available when working in the DotNetDocs repository.

### Manual Installation in Other Projects

Copy the plugin directory to your project:
```bash
cp -r .claude/plugins/dotnet-documentation-expert /path/to/your/project/.claude/plugins/
```

The plugin will be automatically available in Claude Code.

### From Marketplace

This plugin is distributed through the DotNetDocs plugin marketplace located at `.claude-plugin/marketplace.json` in the repository root.

## Usage

### Automatic Activation

The plugin activates automatically when:
- You mention "documentation", "XML comments", or "API docs"
- You're working with .NET/C# code
- You ask about documenting code
- You mention DotNetDocs, Mintlify, or documentation generation

### Explicit Invocation

Reference the plugin in your conversation:
```
Using the .NET Documentation Expert plugin, help me document this UserService class.
```

### Common Use Cases

#### 1. Document a New Class
```
Can you help me write documentation for this new OrderProcessor class?
```

The plugin will provide comprehensive XML documentation including summary, remarks, examples, and any necessary conceptual documentation structure.

#### 2. Review Existing Documentation
```
Please review the documentation for this PaymentService class and suggest improvements.
```

The plugin will analyze the documentation for completeness, clarity, and adherence to best practices.

#### 3. Create Conceptual Documentation
```
I need to create usage documentation for the MintlifyRenderer. What structure should I use?
```

The plugin will suggest appropriate .mdz file organization and provide content templates.

#### 4. Document Specific Patterns
```
How should I document this async method that returns a nullable Task<User>?
```

The plugin will provide specific guidance for the pattern in question.

## What You Get

### For Classes
```csharp
/// <summary>
/// Provides user management operations including authentication, authorization, and profile management.
/// </summary>
/// <remarks>
/// This service coordinates between the user repository, authentication provider, and email service
/// to provide a unified interface for user-related operations.
/// <para>
/// All methods are thread-safe and can be safely called from multiple threads concurrently.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var userService = serviceProvider.GetRequiredService&lt;UserService&gt;();
/// var user = await userService.AuthenticateAsync("username", "password");
/// if (user is not null)
/// {
///     Console.WriteLine($"Welcome, {user.DisplayName}!");
/// }
/// </code>
/// </example>
public class UserService { }
```

### For Methods
```csharp
/// <summary>
/// Asynchronously authenticates a user with their credentials.
/// </summary>
/// <param name="username">The username. Cannot be <see langword="null"/> or whitespace.</param>
/// <param name="password">The password. Cannot be <see langword="null"/> or whitespace.</param>
/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
/// <returns>
/// A task representing the asynchronous operation. The task result contains the authenticated
/// <see cref="User"/> if credentials are valid; otherwise, <see langword="null"/>.
/// </returns>
/// <exception cref="ArgumentException">
/// Thrown when <paramref name="username"/> or <paramref name="password"/> is <see langword="null"/> or whitespace.
/// </exception>
/// <remarks>
/// This method validates credentials against the configured authentication provider.
/// Failed authentication attempts are logged for security monitoring.
/// </remarks>
public async Task<User?> AuthenticateAsync(
    string username,
    string password,
    CancellationToken cancellationToken = default)
```

### For Conceptual Documentation

Structured `.mdz` files with:
- Clear organization by namespace/type/member
- Task-oriented usage guides
- Comprehensive examples
- Best practices and patterns
- Important considerations
- Related APIs and cross-references

## Configuration

The plugin respects project-specific standards defined in `CLAUDE.md`:
- Extensive XML doc comments for all public APIs
- Include `<example>` and `<code>` tags where applicable
- `<param>` tags on same line as content
- `<remarks>` tag last before member declaration
- Null reference type documentation patterns

## Integration with DotNetDocs

The plugin understands the DotNetDocs documentation pipeline:
- XML documentation extraction
- Conceptual content loading from `.mdz` files
- Markdown transformation
- Mintlify rendering
- Navigation generation

This ensures documentation guidance considers how content flows through the pipeline and appears in final output.

## Examples

See [EXAMPLES.md](EXAMPLES.md) for comprehensive examples of:
- Service class documentation
- Configuration class documentation
- Extension method documentation
- Async method patterns
- Generic type documentation
- Event and delegate documentation

## Version History

See [CHANGELOG.md](CHANGELOG.md) for version history and changes.

## License

MIT License - See the main DotNetDocs repository for details.

## Contributing

Contributions are welcome! Please:
1. Follow the existing documentation structure
2. Include examples for new patterns
3. Test documentation output with DotNetDocs
4. Update CHANGELOG.md with your changes

## Support

- **Issues**: [GitHub Issues](https://github.com/CloudNimble/DotNetDocs/issues)
- **Documentation**: [DotNetDocs Documentation](https://dotnetdocs.dev)
- **Repository**: [CloudNimble/DotNetDocs](https://github.com/CloudNimble/DotNetDocs)

## Author

**CloudNimble**
- GitHub: [@CloudNimble](https://github.com/CloudNimble)
- Website: [https://cloudnimble.com](https://cloudnimble.com)

---

**Keywords**: .NET, C#, documentation, XML comments, API documentation, technical writing, DotNetDocs, Mintlify, DocFX
