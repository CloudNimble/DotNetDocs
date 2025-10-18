# DotNetDocs.Core

[![NuGet](https://img.shields.io/nuget/v/DotNetDocs.Core.svg)](https://www.nuget.org/packages/DotNetDocs.Core/)
[![Downloads](https://img.shields.io/nuget/dt/DotNetDocs.Core.svg)](https://www.nuget.org/packages/DotNetDocs.Core/)
[![License](https://img.shields.io/github/license/cloudnimble/dotnetdocs.svg)](https://github.com/CloudNimble/DotNetDocs/blob/main/LICENSE)

<a href="https://dotnetdocs.com">
  <img src="https://raw.githubusercontent.com/CloudNimble/DotNetDocs/refs/heads/dev/src/CloudNimble.DotNetDocs.Docs/images/logos/dotnetdocs.light.svg" alt="DotNetDocs Logo" width="450" />
</a>

The core documentation generation engine for DotNetDocs that transforms .NET assemblies and XML Doc Comments into rich, multi-format documentation.

## Features

- **Assembly Extraction**: Leverages Roslyn to extract API metadata from compiled assemblies
- **XML Documentation Processing**: Parses and integrates XML documentation comments
- **Conceptual Documentation**: Enriches API docs with conceptual content from `.mdz` files
- **Extensible Pipeline**: Modular architecture with enrichers, transformers, and renderers
- **Multi-Assembly Support**: Merge documentation from multiple assemblies
- **Multiple Output Formats**: Built-in renderers for Markdown, JSON, and YAML

## Installation

```bash
dotnet add package DotNetDocs.Core
```

## Quick Start

```csharp
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        services.AddDotNetDocs(ctx =>
        {
            ctx.DocumentationRootPath = "docs";
            ctx.ApiReferencePath = "api-reference";
        });
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var manager = scope.ServiceProvider.GetRequiredService<DocumentationManager>();
    await manager.ProcessAsync("MyLibrary.dll", "MyLibrary.xml");
}
```

## See Also

- **[Full Documentation](https://dotnetdocs.com)** - Complete guides, API reference, and examples
- **[DotNetDocs CLI](https://www.nuget.org/packages/DotNetDocs/)** - Get up and running fast with our easy CLI
- **[DotNetDocs.Sdk](https://www.nuget.org/packages/DotNetDocs.Sdk/)** - MSBuild SDK for .docsproj projects
- **[DotNetDocs.Mintlify](https://www.nuget.org/packages/DotNetDocs.Mintlify/)** - Enhanced [Mintlify.com](https://mintlify.com) support

## Requirements

- .NET 8.0+, .NET 9.0+, or .NET 10.0+

## License

MIT License - see [LICENSE](https://github.com/CloudNimble/DotNetDocs/blob/main/LICENSE) for details.
