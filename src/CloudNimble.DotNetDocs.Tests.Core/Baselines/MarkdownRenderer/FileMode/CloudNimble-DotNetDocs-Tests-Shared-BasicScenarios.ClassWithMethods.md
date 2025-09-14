# ClassWithMethods

## Definition

**Assembly:** CloudNimble.DotNetDocs.Tests.Shared.dll

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios

**Inheritance:** System.Object

## Syntax

```csharp
CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.ClassWithMethods
```

## Summary

A class demonstrating various method documentation scenarios.

## Remarks

Contains methods with different signatures, parameters, and return types.

## Examples

<code>
var obj = new ClassWithMethods();
var result = obj.Calculate(5, 10);
</code>

## Constructors

### .ctor

#### Syntax

```csharp
public ClassWithMethods()
```

## Methods

### Calculate

Calculates the sum of two numbers.

#### Syntax

```csharp
public int Calculate(int a, int b)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `a` | `int` | The first number. |
| `b` | `int` | The second number. |

#### Returns

Type: `int`
The sum of a and b.

#### Examples

<code>
var result = Calculate(3, 4); // Returns 7
</code>

### GetConditionalValue

Gets a value based on a condition.

#### Syntax

```csharp
public string GetConditionalValue(bool condition)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `condition` | `bool` | The condition to evaluate. |

#### Returns

Type: `string`
Returns "Yes" if condition is true, "No" otherwise.

### PerformAction

A void method that performs an action.

#### Syntax

```csharp
public void PerformAction()
```

#### Remarks

This method doesn't return anything.

### Process

Processes the input string.

#### Syntax

```csharp
public string Process(string input)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `input` | `string` | The string to process. |

#### Returns

Type: `string`
The processed string in uppercase.

#### Exceptions

| Exception | Description |
|-----------|-------------|
| `ArgumentNullException` | Thrown when input is null. |

#### Remarks

This method performs a simple transformation for testing purposes.

