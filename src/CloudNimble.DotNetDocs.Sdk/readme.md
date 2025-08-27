# CloudNimble.DotNetDocs.Sdk

MSBuild SDK for documentation projects (`.docsproj`) - provides a clean way to include documentation in .NET solutions.

## Features

- 🚀 **Zero Configuration** - Works out of the box for common documentation types
- 📁 **Clean Folders** - No `bin/obj` folders cluttering your documentation directories
- 🔍 **Auto-Detection** - Automatically detects documentation type (Mintlify, DocFX, MkDocs, Jekyll, Hugo)
- 📝 **Smart Includes** - Automatically includes relevant files based on documentation type
- 🛠️ **Integration** - Optional build targets for linting, preview, and deployment
- 🎯 **Visual Studio** - Full IntelliSense and editing support in Visual Studio

## Quick Start
<!--
### 1. Add to global.json

```json
{
  "msbuild-sdks": {
    "DotNetDocs.Sdk": "1.0.0"
  }
}
```
-->
### 1. Create a .docsproj file

```xml
<Project Sdk="DotNetDocs.Sdk/1.0.0">
  <!-- That's it! Everything is configured automatically -->
</Project>
```

### 2. Add to solution

**For .slnx files:**
```xml
<Project Path="docs/MyProject.Docs.docsproj" Type="{9A19103F-16F7-4668-BE54-9A1E7A4F7556}" projectTypeName="SharedProject" />
```

**For .sln files:**
```
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "MyProject.Docs", "docs\MyProject.Docs.docsproj", "{GUID}"
EndProject
```

## Documentation Type Detection

The SDK automatically detects your documentation type based on configuration files:

| Type | Detection File | Auto-Included Files |
|------|---------------|-------------------|
| **Mintlify** | `docs.json` | `*.mdx`, `*.md`, `api-reference/**`, `conceptual/**`, `overrides/**`, `logo/**` |
| **DocFX** | `docfx.json` | `*.md`, `*.yml`, `articles/**`, `api/**`, `templates/**` |
| **MkDocs** | `mkdocs.yml` | `docs/**/*.md`, `requirements.txt`, `overrides/**` |
| **Jekyll** | `_config.yml` | `*.md`, `*.html`, `_posts/**`, `_layouts/**`, `_includes/**` |
| **Hugo** | `hugo.toml` | `content/**`, `layouts/**`, `static/**`, `themes/**` |
| **Generic** | *fallback* | `*.md`, `*.rst`, `docs/**`, common formats |

## Configuration Options

### Basic Options

```xml
<Project Sdk="DotNetDocs.Sdk/1.0.0">
  <PropertyGroup>
    <!-- Keep bin/obj locally instead of redirecting to temp -->
    <KeepLocalOutput>true</KeepLocalOutput>
    
    <!-- Override auto-detected documentation type -->
    <DocumentationType>Mintlify</DocumentationType>
    
    <!-- Enable build-time features -->
    <GenerateMintlifyDocs>true</GenerateMintlifyDocs>
    <LintMarkdown>true</LintMarkdown>
    <ShowDocumentationStats>true</ShowDocumentationStats>
  </PropertyGroup>
</Project>
```

### Advanced Options

```xml
<Project Sdk="DotNetDocs.Sdk/1.0.0">
  <PropertyGroup>
    <!-- Preview and deployment -->
    <PreviewDocumentation>true</PreviewDocumentation>
    <DeployDocumentation>true</DeployDocumentation>
    
    <!-- Quality assurance -->
    <ValidateLinks>true</ValidateLinks>
    <GeneratePdf>true</GeneratePdf>
  </PropertyGroup>
</Project>
```

### Separate Documentation Location

If your `.docsproj` file is in a different location than your documentation files (e.g., project in `src/` but docs in `docs/`), use the `DocumentationRoot` property:

```xml
<Project Sdk="DotNetDocs.Sdk/1.0.0">
  <PropertyGroup>
    <!-- Point to where your documentation files actually live -->
    <DocumentationRoot>$(MSBuildThisFileDirectory)..\..\docs\</DocumentationRoot>
    
    <!-- All features work with the specified root -->
    <GenerateMintlifyDocs>true</GenerateMintlifyDocs>
    <ShowDocumentationStats>true</ShowDocumentationStats>
  </PropertyGroup>
</Project>
```

This is particularly useful when you want to:
- Keep your `.docsproj` file with other project files in the `src/` folder
- Have all documentation content in a separate `docs/` folder at the repository root
- Maintain a clean separation between code and documentation

## Available Build Targets

| Target | Description |
|--------|-------------|
| `DocumentationHelp` | Show available options and current configuration |
| `DocumentationStats` | Display statistics about your documentation |
| `GenerateMintlifyDocs` | Generate Mintlify documentation (requires EasyAF.Tools) |
| `LintMarkdown` | Lint markdown files for issues |
| `PreviewDocumentation` | Start local preview server |
| `ValidateLinks` | Check for broken links |
| `GeneratePdf` | Generate PDF output |
| `DeployDocumentation` | Deploy to hosting platform |

### EasyAF.Tools Integration

The SDK automatically checks for and attempts to install [CloudNimble.EasyAF.Tools](https://github.com/CloudNimble/EasyAF) when using Mintlify features. If automatic installation fails, you'll see instructions to visit [this GitHub issue](https://github.com/CloudNimble/EasyAF.Docs/issues/1) and leave a reaction emoji to get access to the tools.

### Usage Examples

```bash
# Show help and current configuration
dotnet build -t:DocumentationHelp

# Show documentation statistics  
dotnet build -t:DocumentationStats

# Generate Mintlify docs manually
dotnet build -t:GenerateMintlifyDocs

# Start preview server
dotnet build -t:PreviewDocumentation
```

## Integration Examples

### Mintlify Project

```xml
<Project Sdk="DotNetDocs.Sdk/1.0.0">
  <PropertyGroup>
    <!-- Auto-generate on build -->
    <GenerateMintlifyDocs>true</GenerateMintlifyDocs>
    
    <!-- Quality checks -->
    <LintMarkdown>true</LintMarkdown>
    <ShowDocumentationStats>true</ShowDocumentationStats>
  </PropertyGroup>
</Project>
```

### Multi-Format Project

```xml
<Project Sdk="DotNetDocs.Sdk/1.0.0">
  <PropertyGroup>
    <!-- Override detection -->
    <DocumentationType>Generic</DocumentationType>
    
    <!-- Keep output local for complex builds -->
    <KeepLocalOutput>true</KeepLocalOutput>
  </PropertyGroup>
  
  <!-- Custom file includes -->
  <ItemGroup>
    <None Include="custom-docs/**/*.rst" />
    <None Include="api-specs/**/*.yaml" />
  </ItemGroup>
</Project>
```

### CI/CD Integration

```yaml
# GitHub Actions example
- name: Build Documentation
  run: dotnet build docs/MyProject.Docs.docsproj -p:DeployDocumentation=true
```

## Benefits Over Manual Configuration

| Feature | Manual Setup | CloudNimble.DotNetDocs.Sdk |
|---------|-------------|--------------------------------|
| Configuration | 20+ lines of MSBuild | 2 lines |
| File Includes | Manual specification | Automatic by type |
| Output Cleanup | Custom Directory.Build.props | Built-in |
| Type Detection | None | Automatic |
| Build Integration | Custom targets | Ready-to-use targets |
| Maintenance | High | Zero |

## Requirements

- .NET SDK 6.0 or later
- MSBuild 16.0 or later
- Visual Studio 2019 or later (for full IDE support)

## Contributing

This SDK is part of the [CloudNimble EasyAF](https://github.com/CloudNimble/EasyAF) framework. Contributions welcome!

## License

MIT License - see [LICENSE](LICENSE) for details.