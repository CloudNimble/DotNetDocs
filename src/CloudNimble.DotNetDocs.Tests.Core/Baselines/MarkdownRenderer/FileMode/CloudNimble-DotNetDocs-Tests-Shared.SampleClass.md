# SampleClass

## Definition

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared
**Assembly:** CloudNimble.DotNetDocs.Tests.Shared
**Inheritance:** System.Object

## Syntax

```csharp
public class SampleClass : System.Object
```

## Description

A sample class for testing documentation generation.

## Constructors

### .ctor

#### Syntax

```csharp
public .ctor()
```

## Properties

### Name

Gets or sets the name.

#### Syntax

```csharp
public string Name { get; set; }
```

#### Property Value

Type: `string`

### Value

Gets or sets the value.

#### Syntax

```csharp
public int Value { get; set; }
```

#### Property Value

Type: `int`

## Methods

### DoSomething

Performs a sample operation.

#### Syntax

```csharp
public string DoSomething(string input)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `input` | `string` | The input parameter. |

#### Returns

Type: `string`

### GetDisplay

Gets the display value.

#### Syntax

```csharp
public string GetDisplay()
```

#### Returns

Type: `string`

### MethodWithOptional

Method with optional parameter.

#### Syntax

```csharp
public string MethodWithOptional(string required, int optional)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `required` | `string` | Required parameter. |
| `optional` | `int` | Optional parameter with default value. |

#### Returns

Type: `string`

### MethodWithParams

Method with params array.

#### Syntax

```csharp
public int MethodWithParams(int[] values)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `values` | `int[]` | Variable number of values. |

#### Returns

Type: `int`

