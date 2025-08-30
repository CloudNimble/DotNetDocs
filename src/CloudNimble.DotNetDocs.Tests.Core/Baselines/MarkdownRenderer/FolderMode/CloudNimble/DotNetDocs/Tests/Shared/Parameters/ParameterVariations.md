# ParameterVariations

## Definition

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.Parameters
**Assembly:** CloudNimble.DotNetDocs.Tests.Shared
**Inheritance:** System.Object

## Syntax

```csharp
public class ParameterVariations : System.Object
```

## Description

A class demonstrating various parameter types and patterns.

## Constructors

### .ctor

#### Syntax

```csharp
public .ctor()
```

## Methods

### GenericMethod

A generic method with a type parameter.

#### Syntax

```csharp
public string GenericMethod<T>(T value)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `value` | `T` | The value to process. |

#### Returns

Type: `string`

#### Examples

var result1 = GenericMethod<int>(42);
            var result2 = GenericMethod("hello");

### GenericMethodWithMultipleTypes

A method with multiple generic type parameters.

#### Syntax

```csharp
public System.Collections.Generic.KeyValuePair<TKey, TValue> GenericMethodWithMultipleTypes<TKey, TValue>(TKey key, TValue value)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `key` | `TKey` | The key. |
| `value` | `TValue` | The value. |

#### Returns

Type: `System.Collections.Generic.KeyValuePair<TKey, TValue>`

### MethodWithConstraints

A method demonstrating parameter constraints.

#### Syntax

```csharp
public string MethodWithConstraints<T>(T item)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `item` | `T` | The item to process. |

#### Returns

Type: `string`

### MethodWithNullables

A method with nullable parameters.

#### Syntax

```csharp
public string MethodWithNullables(System.Nullable<int> nullableInt, string? nullableString)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `nullableInt` | `System.Nullable<int>` | An optional nullable integer. |
| `nullableString` | `string?` | An optional nullable string. |

#### Returns

Type: `string`

### MethodWithOptionalParam

A method with an optional parameter.

#### Syntax

```csharp
public string MethodWithOptionalParam(string required, int optional)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `required` | `string` | The required string parameter. |
| `optional` | `int` | The optional integer parameter with a default value. |

#### Returns

Type: `string`

#### Examples

var result1 = MethodWithOptionalParam("test");      // Uses default value 42
            var result2 = MethodWithOptionalParam("test", 100); // Uses provided value

### MethodWithOut

A method with an out parameter.

#### Syntax

```csharp
public bool MethodWithOut(string input, int value)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `input` | `string` | The input string to parse. |
| `value` | `int` | The output integer value if parsing succeeds. |

#### Returns

Type: `bool`

### MethodWithParams

A method with a params array.

#### Syntax

```csharp
public int MethodWithParams(int[] values)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `values` | `int[]` | Variable number of integer values. |

#### Returns

Type: `int`

#### Examples

var sum1 = MethodWithParams(1, 2, 3);        // Returns 6
            var sum2 = MethodWithParams(new[] { 1, 2 }); // Returns 3

### MethodWithRef

A method with a ref parameter.

#### Syntax

```csharp
public void MethodWithRef(int value)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `value` | `int` | The value to be modified by reference. |

