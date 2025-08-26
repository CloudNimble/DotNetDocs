# Mintlify.Core

[![NuGet](https://img.shields.io/nuget/v/Mintlify.Core.svg)](https://www.nuget.org/packages/Mintlify.Core/)
[![Downloads](https://img.shields.io/nuget/dt/Mintlify.Core.svg)](https://www.nuget.org/packages/Mintlify.Core/)
[![License](https://img.shields.io/github/license/cloudnimble/easyaf.svg)](https://github.com/CloudNimble/EasyAF/blob/master/LICENSE)

A comprehensive .NET library for working with Mintlify documentation configuration files (`docs.json`). This package provides robust loading, validation, manipulation, and generation capabilities for Mintlify documentation projects.

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

## Advanced Usage

### Custom Validation

```csharp
var manager = new DocsJsonManager("docs.json");
manager.Load();

// Apply additional defaults
manager.ApplyDefaults();

// Validate and get detailed feedback
if (!manager.IsLoaded)
{
    var errors = manager.ConfigurationErrors.Where(e => !e.IsWarning);
    var warnings = manager.ConfigurationErrors.Where(e => e.IsWarning);
    
    Console.WriteLine($"Found {errors.Count()} errors and {warnings.Count()} warnings");
}
```

### Working with Navigation Structures

```csharp
// Access navigation elements
var navigation = manager.Configuration.Navigation;

if (navigation.Groups?.Any() == true)
{
    foreach (var group in navigation.Groups)
    {
        Console.WriteLine($"Group: {group.Group}");
        if (group.Pages?.Any() == true)
        {
            foreach (var page in group.Pages.OfType<string>())
            {
                Console.WriteLine($"  Page: {page}");
            }
        }
    }
}
```

### Error Handling

```csharp
try
{
    var manager = new DocsJsonManager("docs.json");
    manager.Load();
    
    if (!manager.IsLoaded)
    {
        throw new InvalidOperationException("Failed to load configuration");
    }
}
catch (FileNotFoundException)
{
    // Handle missing file
    var defaultConfig = DocsJsonManager.CreateDefault("New Documentation");
    // ... initialize with defaults
}
catch (ArgumentException ex)
{
    // Handle invalid arguments (e.g., invalid file paths)
    Console.WriteLine($"Invalid argument: {ex.Message}");
}
```

## Configuration Model

The library provides strongly-typed models for all Mintlify configuration options:

- **DocsJsonConfig**: Root configuration object
- **NavigationConfig**: Navigation structure with pages, groups, tabs, anchors
- **GroupConfig**: Documentation groups with nested pages
- **TabConfig**: Tab-based navigation sections  
- **ColorsConfig**: Theme color customization
- **LogoConfig**: Logo configuration for light/dark themes
- **FooterConfig**: Footer links and social media
- And many more...

## URL Management

Transform and prefix navigation URLs:

```csharp
// Apply prefix to all URLs in navigation
manager.ApplyUrlPrefix("/api/v1");

// This transforms:
// "getting-started" → "/api/v1/getting-started"
// "guides/authentication" → "/api/v1/guides/authentication"
// Group roots, tab hrefs, anchor hrefs are all updated recursively
```

## Navigation Generation

Build navigation from directory structure:

```csharp
// Given directory structure:
// docs/
//   getting-started.md
//   guides/
//     authentication.md
//     deployment.md
//   api/
//     endpoints.md

manager.PopulateNavigationFromPath("./docs");

// Generates navigation:
// {
//   "pages": [
//     "getting-started",
//     {
//       "group": "Guides", 
//       "pages": ["guides/authentication", "guides/deployment"]
//     },
//     {
//       "group": "Api",
//       "pages": ["api/endpoints"] 
//     }
//   ]
// }
```

## Requirements

- .NET 8.0+ (for modern C# features and performance)
- .NET 9.0+ (fully supported)
- .NET 10.0+ (fully supported)
- .NET Standard 2.0 (for broader compatibility)

## Dependencies

- System.Text.Json (for JSON serialization)
- System.CodeDom (for error reporting)
- CloudNimble.EasyAF.Core (internal utilities)

## Contributing

Contributions are welcome! This library is part of the [EasyAF Framework](https://github.com/CloudNimble/EasyAF).

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/CloudNimble/EasyAF/blob/master/LICENSE) file for details.

## Support

- [GitHub Issues](https://github.com/CloudNimble/EasyAF/issues)
- [Documentation](https://docs.nimbleapps.cloud)
- [NuGet Package](https://www.nuget.org/packages/Mintlify.Core/)