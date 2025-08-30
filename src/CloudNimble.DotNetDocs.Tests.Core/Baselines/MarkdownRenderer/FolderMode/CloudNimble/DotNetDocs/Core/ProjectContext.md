# ProjectContext

## Definition

**Namespace:** CloudNimble.DotNetDocs.Core
**Assembly:** CloudNimble.DotNetDocs.Core
**Inheritance:** System.Object

## Syntax

```csharp
public class ProjectContext : System.Object
```

## Description

Represents MSBuild project context for source intent in documentation generation.

## Properties

### ConceptualPath

#### Syntax

```csharp
public string? ConceptualPath { get; init; }
```

#### Description

Gets or sets the path to the conceptual documentation folder.

### FileNamingOptions

#### Syntax

```csharp
public FileNamingOptions FileNamingOptions { get; init; }
```

#### Description

Gets or sets the file naming options for documentation generation.

### OutputPath

#### Syntax

```csharp
public string OutputPath { get; init; }
```

#### Description

Gets or sets the output path for generated documentation.

## Methods

### GetNamespaceFolderPath

#### Syntax

```csharp
public string GetNamespaceFolderPath(string namespaceName)
```

#### Description

Converts a namespace string to a folder path based on the configured file naming options.

### GetTypeFilePath

#### Syntax

```csharp
public string GetTypeFilePath(string fullyQualifiedTypeName, string extension)
```

#### Description

Gets the full file path for a type, including namespace folder structure if in Folder mode.

### EnsureOutputDirectoryStructure

#### Syntax

```csharp
public void EnsureOutputDirectoryStructure(DocAssembly assemblyModel, string outputPath)
```

#### Description

Ensures that the output directory structure exists for all namespaces in the assembly model.