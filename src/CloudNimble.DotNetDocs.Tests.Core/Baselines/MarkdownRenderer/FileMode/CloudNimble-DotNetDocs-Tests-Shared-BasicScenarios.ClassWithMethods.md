# ClassWithMethods

## Definition

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios
**Assembly:** CloudNimble.DotNetDocs.Tests.Shared
**Inheritance:** System.Object

## Syntax

```csharp
public class ClassWithMethods : System.Object
```

## Description

A class demonstrating various method documentation scenarios.

## Examples

var obj = new ClassWithMethods();
            var result = obj.Calculate(5, 10);

## Constructors

### .ctor

#### Syntax

```csharp
public .ctor()
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

#### Examples

var result = Calculate(3, 4); // Returns 7

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

### PerformAction

A void method that performs an action.

#### Syntax

```csharp
public void PerformAction()
```

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

