# FileNamingOptions

## Definition

**Namespace:** CloudNimble.DotNetDocs.Core.Configuration
**Assembly:** CloudNimble.DotNetDocs.Core
**Inheritance:** System.Object

## Syntax

```csharp
public class FileNamingOptions : System.Object
```

## Description

Configuration options for file naming strategies in documentation generation.

## Properties

### NamespaceMode

#### Syntax

```csharp
public NamespaceMode NamespaceMode { get; set; }
```

#### Description

Gets or sets the namespace organization mode (File or Folder).

### NamespaceSeparator

#### Syntax

```csharp
public char NamespaceSeparator { get; set; }
```

#### Description

Gets or sets the character used to separate namespace parts in File mode.

## Constructors

### FileNamingOptions

#### Syntax

```csharp
public FileNamingOptions(NamespaceMode mode = NamespaceMode.File, char separator = '-')
```

#### Description

Initializes a new instance of the FileNamingOptions class.