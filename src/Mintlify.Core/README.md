# Mintlify.Core

[![NuGet](https://img.shields.io/nuget/v/Mintlify.Core.svg)](https://www.nuget.org/packages/Mintlify.Core/)
[![Downloads](https://img.shields.io/nuget/dt/Mintlify.Core.svg)](https://www.nuget.org/packages/Mintlify.Core/)
[![License](https://img.shields.io/github/license/cloudnimble/dotnetdocs.svg)](https://github.com/CloudNimble/DotNetDocs/blob/master/LICENSE)

Mintlify.Core is a comprehensive .NET library for working with Mintlify documentation configuration files (`docs.json`). This package provides robust loading, 
validation, manipulation, and generation capabilities for Mintlify documentation projects.

## Brought to you by:

<a href="https://dotnetdocs.com">
  <img src="https://raw.githubusercontent.com/CloudNimble/DotNetDocs/refs/heads/dev/src/CloudNimble.DotNetDocs.Docs/images/logos/dotnetdocs.light.svg" alt="DotNetDocs Logo" width="450" />
</a>

## Features

- **Complete Configuration Management**: Load, validate, save, and manipulate `docs.json` files
- **Intelligent Navigation Merging**: Merge multiple documentation configurations with smart deduplication
- **Directory-Based Navigation Generation**: Automatically build navigation from folder structures
- **URL Management**: Apply prefixes and transformations to navigation URLs
- **Type-Safe Models**: Strongly-typed classes for all Mintlify configuration options
- **Comprehensive Validation**: Built-in validation with detailed error reporting
- **Multiple Target Frameworks**: Supports .NET 8+, .NET 9+, .NET 10+, and .NET Standard 2.0

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package Mintlify.Core
```

Or via Package Manager Console:

```powershell
Install-Package Mintlify.Core
```

## Quick Start

### Loading and Working with docs.json

```csharp
using Mintlify.Core;

// Load from file path
var manager = new DocsJsonManager("path/to/docs.json");
manager.Load();

// Or load from string content
var manager2 = new DocsJsonManager();
manager2.Load(jsonString);

// Access configuration
if (manager.IsLoaded)
{
    Console.WriteLine($"Site name: {manager.Configuration.Name}");
    Console.WriteLine($"Theme: {manager.Configuration.Theme}");
}

// Check for errors
if (manager.ConfigurationErrors.Any())
{
    foreach (var error in manager.ConfigurationErrors)
    {
        Console.WriteLine($"{error.ErrorNumber}: {error.ErrorText}");
    }
}
```

### Creating Default Configuration

```csharp
// Create a new documentation configuration
var config = DocsJsonManager.CreateDefault("My Documentation", "mint");

var manager = new DocsJsonManager();
manager.Configuration = config;
manager.Save("docs.json");
```

### Merging Configurations

```csharp
var mainManager = new DocsJsonManager("main-docs.json");
mainManager.Load();

var additionalManager = new DocsJsonManager("additional-docs.json");
additionalManager.Load();

// Merge configurations with intelligent navigation combining
mainManager.Merge(additionalManager.Configuration);

// Or merge only navigation (skip base properties)
mainManager.Merge(additionalManager.Configuration, combineBaseProperties: false);
```

### Auto-Generating Navigation from Directory Structure

```csharp
var manager = new DocsJsonManager();
manager.Configuration = DocsJsonManager.CreateDefault("My Docs");

// Scan directory and build navigation automatically
manager.PopulateNavigationFromPath("./docs", new[] { ".md", ".mdx" });

// Apply URL prefix to all navigation items
manager.ApplyUrlPrefix("/v2");

manager.Save("docs.json");
```

## Key Features

### Configuration Management

Load, validate, save, and manipulate `docs.json` files with intelligent error reporting.

### Navigation Merging

Merge multiple documentation configurations with smart deduplication - perfect for multi-project solutions.

### Directory-Based Generation

Automatically build navigation from your folder structure.

### URL Transformation

Apply prefixes and transformations to navigation URLs for versioning or multi-tenancy.

## Requirements

- .NET 8.0+ (for modern C# features and performance)
- .NET 9.0+ (fully supported)
- .NET 10.0+ (fully supported)
- .NET Standard 2.0 (for broader compatibility)

## Dependencies

- System.Text.Json (for JSON serialization)
- System.CodeDom (for error reporting)

## See Also

- **[Full Documentation](https://dotnetdocs.com)** - Complete guides and examples
- **[DotNetDocs CLI](https://www.nuget.org/packages/DotNetDocs/)** - Get up and running fast with our easy CLI
- **[DotNetDocs.Sdk](https://www.nuget.org/packages/DotNetDocs.Sdk/)** - MSBuild SDK for .docsproj projects
- **[DotNetDocs.Core](https://www.nuget.org/packages/DotNetDocs.Core/)** - Core documentation engine
- **[DotNetDocs.Mintlify](https://www.nuget.org/packages/DotNetDocs.Mintlify/)** - Enhanced [Mintlify.com](https://mintlify.com) support

## License

MIT License - see [LICENSE](https://github.com/CloudNimble/DotNetDocs/blob/main/LICENSE) for details.