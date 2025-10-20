# DotNetDocs.Sdk

[![NuGet](https://img.shields.io/nuget/v/DotNetDocs.Sdk.svg)](https://www.nuget.org/packages/DotNetDocs.Sdk/)
[![Downloads](https://img.shields.io/nuget/dt/DotNetDocs.Sdk.svg)](https://www.nuget.org/packages/DotNetDocs.Sdk/)
[![License](https://img.shields.io/github/license/cloudnimble/dotnetdocs.svg)](https://github.com/CloudNimble/DotNetDocs/blob/main/LICENSE)

<a href="https://dotnetdocs.com">
  <img src="https://raw.githubusercontent.com/CloudNimble/DotNetDocs/refs/heads/dev/src/CloudNimble.DotNetDocs.Docs/images/logos/dotnetdocs.light.svg" alt="DotNetDocs Logo" width="450" />
</a>

The MSBuild SDK for documentation projects (`.docsproj`) that provides a clean way to include documentation in .NET solutions.

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
<Project Sdk="DotNetDocs.Sdk/1.0.1">
  <!-- That's it! Everything is configured automatically -->
</Project>
```

### 2. Add to solution

**For .slnx files:**
```xml
<Project Path="docs/MyProject.Docs.docsproj" Type="C#" />
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
<Project Sdk="DotNetDocs.Sdk/1.0.1">
  <PropertyGroup>
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
<Project Sdk="DotNetDocs.Sdk/1.0.1">
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
<Project Sdk="DotNetDocs.Sdk/1.0.1">
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

### DotNetDocs Integration

The SDK automatically generates documentation when `GenerateMintlifyDocs` is enabled. Documentation is built from your project's assemblies and XML documentation files.

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
<Project Sdk="DotNetDocs.Sdk/1.0.1">
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
<Project Sdk="DotNetDocs.Sdk/1.0.1">
  <PropertyGroup>
    <!-- Override detection -->
    <DocumentationType>Generic</DocumentationType>
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

- .NET SDK 8.0 or later
- MSBuild 16.0 or later
- Visual Studio 2019 or later (for full IDE support)

## See Also

- **[Full Documentation](https://dotnetdocs.com)** - Complete guides and examples
- **[DotNetDocs CLI](https://www.nuget.org/packages/DotNetDocs/)** - Get up and running fast with our easy CLI
- **[DotNetDocs.Core](https://www.nuget.org/packages/DotNetDocs.Core/)** - Core documentation engine
- **[DotNetDocs.Mintlify](https://www.nuget.org/packages/DotNetDocs.Mintlify/)** - Enhanced [Mintlify.com](https://mintlify.com) support

## License

MIT License - see [LICENSE](https://github.com/CloudNimble/DotNetDocs/blob/main/LICENSE) for details.