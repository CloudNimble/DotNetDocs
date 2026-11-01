# ITestInterface

## Definition

**Assembly:** CloudNimble.DotNetDocs.Tests.Shared.dll

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios

## Syntax

```csharp
CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.ITestInterface
```

## Summary

A test interface for demonstrating interface inheritance and extension methods.

## Remarks

This interface is used to test extension methods on interfaces and to verify
            that inherited members from interfaces are properly documented.

## Properties

### TestValue

Gets the test value.

#### Syntax

```csharp
string TestValue { get; }
```

#### Property Value

Type: `string`

## Methods

### GetFormattedValue

Gets a formatted string from the interface.

#### Syntax

```csharp
public static string GetFormattedValue(CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.ITestInterface instance)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `instance` | `CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.ITestInterface` | The interface instance. |

#### Returns

Type: `string`
A formatted string containing the test value.

#### Examples

<code>
ITestInterface test = new TestImplementation();
var formatted = test.GetFormattedValue();
</code>

### TestMethod

Performs a test operation.

#### Syntax

```csharp
void TestMethod()
```

### Validate

Validates the interface instance.

#### Syntax

```csharp
public static bool Validate(CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.ITestInterface instance)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `instance` | `CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.ITestInterface` | The interface instance. |

#### Returns

Type: `bool`
True if the instance is valid, otherwise false.

#### Remarks

This extension provides common validation logic for all ITestInterface implementers.

