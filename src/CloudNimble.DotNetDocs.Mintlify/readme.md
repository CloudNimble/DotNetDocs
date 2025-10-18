# DotNetDocs.Mintlify

[![NuGet](https://img.shields.io/nuget/v/DotNetDocs.Mintlify.svg)](https://www.nuget.org/packages/DotNetDocs.Mintlify/)
[![Downloads](https://img.shields.io/nuget/dt/DotNetDocs.Mintlify.svg)](https://www.nuget.org/packages/DotNetDocs.Mintlify/)
[![License](https://img.shields.io/github/license/cloudnimble/dotnetdocs.svg)](https://github.com/CloudNimble/DotNetDocs/blob/main/LICENSE)

<a href="https://dotnetdocs.com">
  <img src="https://raw.githubusercontent.com/CloudNimble/DotNetDocs/refs/heads/dev/src/CloudNimble.DotNetDocs.Docs/images/logos/dotnetdocs.light.svg" alt="DotNetDocs Logo" width="450" />
</a>

Extensions for DotNetDocs that transform your .NET XML Doc Comments into beautiful Mintlify websites with smart navigation, context-aware icons, and rich MDX features.

## Features

- **MDX with Frontmatter**: Auto-generated frontmatter with icons, tags, SEO metadata, and keywords
- **Smart Navigation**: Automatic docs.json generation with hierarchical namespace organization
- **Context-Aware Icons**: 50+ FontAwesome icons automatically assigned based on type characteristics
- **React Components**: Embed custom React components directly in your MDX files
- **Flexible Output Modes**: Choose between File and Folder namespace organization

## Installation

```bash
dotnet add package DotNetDocs.Mintlify
```

## Quick Start

```csharp
using CloudNimble.DotNetDocs.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = new DotNetDocsBuilder()
    .AddDefaultPipeline()
    .AddMintlifyRenderer(options =>
    {
        options.NavigationMode = NavigationMode.Unified;
        options.NamespaceMode = NamespaceMode.Folder;
        options.GenerateDocsJson = true;
        options.IncludeIcons = true;
    });

var result = await builder.BuildAsync();
```

## Navigation Modes

- **Unified**: Single navigation tree with all namespaces (best for single-project solutions)
- **ByAssembly**: Separate groups per assembly (best for multi-project solutions)

## Output Modes

- **File Mode**: One MDX file per namespace (simpler file structure)
- **Folder Mode**: Folder per namespace with separate type files (better for large projects)

## Preview Your Docs

```bash
npm install -g mintlify
cd docs
mintlify dev
```

Your documentation will be available at `http://localhost:3000`.

## See Also

- **[Full Documentation](https://dotnetdocs.com/providers/mintlify)** - Complete guides, examples, and icon reference
- **[DotNetDocs CLI](https://www.nuget.org/packages/DotNetDocs/)** - Get up and running fast with our easy CLI
- **[DotNetDocs.Core](https://www.nuget.org/packages/DotNetDocs.Core/)** - Core documentation engine
- **[DotNetDocs.Sdk](https://www.nuget.org/packages/DotNetDocs.Sdk/)** - MSBuild SDK for .docsproj projects
- **[Mintlify Documentation](https://mintlify.com/docs)** - Official Mintlify docs and component reference

## Requirements

- .NET 8.0+, .NET 9.0+, or .NET 10.0+
- Mintlify CLI (for local preview)

## License

MIT License - see [LICENSE](https://github.com/CloudNimble/DotNetDocs/blob/main/LICENSE) for details.
