# Agent Guidelines for DotNetDocs

## Build/Test Commands
- **Build**: `dotnet build src/CloudNimble.DotNetDocs.slnx --configuration Release`
- **Test All**: `dotnet test src/CloudNimble.DotNetDocs.slnx --configuration Release --no-build`
- **Test Single**: `dotnet test src/CloudNimble.DotNetDocs.slnx --configuration Release --filter "FullyQualifiedName~TestMethodName"`
- **Restore**: `dotnet restore src/CloudNimble.DotNetDocs.slnx`
- **Pack**: `dotnet pack src/CloudNimble.DotNetDocs.slnx --configuration Release --output ./artifacts`

## Code Style Guidelines
- **C# Version**: Use C# 14 (global.json targets .NET 10.0)
- **Formatting**: Follow .editorconfig - 4-space indent, newlines before braces, block-scoped namespaces
- **Imports**: Single-line using directives, sorted system-first
- **Regions**: #regions in order: Fields, Properties, Constructors, Public Methods, Private Methods
- **Null Handling**: Prefer `ArgumentException.ThrowIfNullOrWhiteSpace()` and `ArgumentNullException.ThrowIfNull()` (.NET 8+)
- **Strings**: Use `.IsNullOrWhiteSpace()` over `.IsNullOrEmpty()`
- **Patterns**: Use pattern matching, switch expressions, collection initializers
- **Naming**: Use `nameof` instead of string literals
- **Organization**: Public members first, then protected, internal, private (alphabetical within groups)

## Testing
- **Framework**: MSTest v3 with FluentAssertions and Breakdance
- **Style**: No Arrange/Act/Assert comments, no mocking
- **Assertions**: Prefer `.NotBeNullOrWhiteSpace()` for strings
- **Project Format**: `BaseNamespace.Tests.SubjectMatter`

## Documentation
- **XML Comments**: Extensive comments required for APIs with `<example>`, `<code>`, `<remarks>`
- **Process**: Use `dotnet easyaf mintlify` to generate Mintlify documentation

## Additional Rules
- Always specify Configuration for dotnet commands
- Use defense-in-depth programming
- Trust C# null annotations
- No interfaces for DI unless absolutely necessary</content>
</xai:function_call/>
</xai:function_call name="read">
<parameter name="filePath">D:\GitHub\DotNetDocs\AGENTS.md