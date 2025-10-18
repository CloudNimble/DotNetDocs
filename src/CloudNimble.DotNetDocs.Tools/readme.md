# DotNetDocs CLI

[![NuGet](https://img.shields.io/nuget/v/DotNetDocs.svg)](https://www.nuget.org/packages/DotNetDocs/)
[![Downloads](https://img.shields.io/nuget/dt/DotNetDocs.svg)](https://www.nuget.org/packages/DotNetDocs/)
[![License](https://img.shields.io/github/license/cloudnimble/dotnetdocs.svg)](https://github.com/CloudNimble/DotNetDocs/blob/main/LICENSE)

<a href="https://dotnetdocs.com">
  <img src="https://raw.githubusercontent.com/CloudNimble/DotNetDocs/refs/heads/dev/src/CloudNimble.DotNetDocs.Docs/images/logos/dotnetdocs.light.svg" alt="DotNetDocs Logo" width="450" />
</a>

The command-line tool that helps you manage your .NET Documentation Projects (.docsproj) and generate beautiful documentation from your XML Doc Comments.

## Installation

Install as a global tool:

```bash
dotnet tool install --global DotNetDocs
```

Or update an existing installation:

```bash
dotnet tool update --global DotNetDocs
```

## Commands

### dotnet docs add

Create and add a documentation project (.docsproj) to your solution.

**Usage:**
```bash
dotnet docs add [options]
```

**Options:**
- `--solution|-s <PATH>` - Path to solution file (.sln or .slnx). If not specified, searches current directory
- `--name <NAME>` - Name for the docs project. Defaults to `{SolutionName}.Docs`
- `--output|-o <PATH>` - Output directory for the docs project. Defaults to project folder

**Examples:**

```bash
# Create docs project in current directory's solution
dotnet docs add

# Specify solution file
dotnet docs add --solution ../MyApp.sln

# Custom project name
dotnet docs add --name MyCustomDocs

# Custom output directory
dotnet docs add --output ./documentation
```

**What it does:**
1. Creates a new `.docsproj` file with Mintlify configuration
2. Adds the project to your solution in a "Docs" solution folder
3. Configures default Mintlify theme and settings

### dotnet docs build

Build documentation from .NET assemblies.

**Usage:**
```bash
dotnet docs build [options]
```

**Options:**
- `--assembly-list|-a <PATH>` - **(Required)** Path to file containing list of assemblies to document (one per line)
- `--output|-o <PATH>` - **(Required)** Output path for generated documentation
- `--type|-t <TYPE>` - Documentation type: `Default`, `Mintlify`, `Json`, or `Yaml`. Default is `Default` (Markdown)
- `--namespace-mode|-n <MODE>` - Namespace organization: `File` or `Folder`. Default is `File`
- `--api-reference-path <PATH>` - API reference subfolder path. Default is `api-reference`

**Examples:**

```bash
# Basic build with assembly list
dotnet docs build --assembly-list assemblies.txt --output ./docs

# Build Mintlify documentation
dotnet docs build -a assemblies.txt -o ./docs --type Mintlify

# Use folder mode for namespaces
dotnet docs build -a assemblies.txt -o ./docs --namespace-mode Folder

# Build JSON format
dotnet docs build -a assemblies.txt -o ./docs --type Json
```

**Assembly List File Format:**

Create a text file with one assembly path per line:

```text
bin/Release/net8.0/MyProject.Core.dll
bin/Release/net8.0/MyProject.Extensions.dll
bin/Release/net8.0/MyProject.Utilities.dll
```

**Note:** Only assemblies with corresponding XML documentation files (`.xml`) will be processed.

## Documentation Types

- **Default/Markdown**: Clean Markdown files with frontmatter
- **Mintlify**: MDX files with enhanced frontmatter, icons, and `docs.json` navigation
- **Json**: JSON representation of the documentation model
- **Yaml**: YAML representation of the documentation model

## Namespace Modes

- **File Mode**: Each namespace gets a single file (e.g., `MyNamespace.SubNamespace.md`)
- **Folder Mode**: Each namespace becomes a folder with separate files per type (e.g., `MyNamespace/SubNamespace/MyClass.md`)

## Workflow Example

```bash
# 1. Add a docs project to your solution
dotnet docs add

# 2. Build your projects to generate assemblies and XML docs
dotnet build --configuration Release

# 3. Create an assembly list file
echo "bin/Release/net8.0/MyProject.dll" > assemblies.txt

# 4. Generate documentation
dotnet docs build -a assemblies.txt -o ./docs --type Mintlify

# 5. Preview with Mintlify CLI
npm i mint -g
cd docs
mint dev
```

## See Also

- **[Full Documentation](https://dotnetdocs.com/guides/cli-reference)** - Complete CLI reference and examples
- **[DotNetDocs.Sdk](https://www.nuget.org/packages/DotNetDocs.Sdk/)** - MSBuild SDK for .docsproj projects
- **[DotNetDocs.Core](https://www.nuget.org/packages/DotNetDocs.Core/)** - Core documentation engine
- **[DotNetDocs.Mintlify](https://www.nuget.org/packages/DotNetDocs.Mintlify/)** - Enhanced [Mintlify.com](https://mintlify.com) support

## Requirements

- .NET 8.0+, .NET 9.0+, or .NET 10.0+

## License

MIT License - see [LICENSE](https://github.com/CloudNimble/DotNetDocs/blob/main/LICENSE) for details.
