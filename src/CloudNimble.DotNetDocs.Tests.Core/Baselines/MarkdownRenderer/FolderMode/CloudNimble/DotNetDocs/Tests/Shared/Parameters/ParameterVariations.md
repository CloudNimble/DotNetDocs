# ParameterVariations

## Definition

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.Parameters
**Assembly:** CloudNimble.DotNetDocs.Tests.Shared
**Inheritance:** System.Object

## Syntax

```csharp
CloudNimble.DotNetDocs.Tests.Shared.Parameters.ParameterVariations
```

## Summary

A class demonstrating various parameter types and patterns.

## Remarks

This class contains methods with different parameter modifiers and types.

## Constructors

### .ctor

#### Syntax

```csharp
public ParameterVariations()
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
The string representation of the value.

#### Type Parameters

- `T` - The type of the value.

#### Examples

var result1 = GenericMethod<int>(42);
            var result2 = GenericMethod("hello");

### GenericMethodWithMultipleTypes

A method with multiple generic type parameters.

#### Syntax

```csharp
public KeyValuePair<TKey, TValue> GenericMethodWithMultipleTypes<TKey, TValue>(TKey key, TValue value)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `key` | `TKey` | The key. |
| `value` | `TValue` | The value. |

#### Returns

Type: `System.Collections.Generic.KeyValuePair<TKey, TValue>`
A key-value pair.

#### Type Parameters

- `TKey` - The type of the key.
- `TValue` - The type of the value.

### MethodWithConstraints

A method demonstrating parameter constraints.

#### Syntax

```csharp
public string MethodWithConstraints<T>(T item) where T : class
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `item` | `T` | The item to process. |

#### Returns

Type: `string`
The type name of the item.

#### Type Parameters

- `T` - The type parameter constrained to class types.

### MethodWithNullables

A method with nullable parameters.

#### Syntax

```csharp
public string MethodWithNullables(Nullable<int> nullableInt, string nullableString)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `nullableInt` | `System.Nullable<int>` | An optional nullable integer. |
| `nullableString` | `string?` | An optional nullable string. |

#### Returns

Type: `string`
A description of the provided values.

### MethodWithOptionalParam

A method with an optional parameter.

#### Syntax

```csharp
public string MethodWithOptionalParam(string required, int optional = 42)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `required` | `string` | The required string parameter. |
| `optional` | `int` | The optional integer parameter with a default value. |

#### Returns

Type: `string`
A formatted string combining both parameters.

#### Examples

var result1 = MethodWithOptionalParam("test");      // Uses default value 42
            var result2 = MethodWithOptionalParam("test", 100); // Uses provided value

### MethodWithOut

A method with an out parameter.

#### Syntax

```csharp
public bool MethodWithOut(string input, out int value)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `input` | `string` | The input string to parse. |
| `value` | `int` | The output integer value if parsing succeeds. |

#### Returns

Type: `bool`
true if the parsing was successful; otherwise, false.

### MethodWithParams

A method with a params array.

#### Syntax

```csharp
public int MethodWithParams(params int[] values)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `values` | `int[]` | Variable number of integer values. |

#### Returns

Type: `int`
The sum of all provided values.

#### Examples

var sum1 = MethodWithParams(1, 2, 3);        // Returns 6
            var sum2 = MethodWithParams(new[] { 1, 2 }); // Returns 3

### MethodWithRef

A method with a ref parameter.

#### Syntax

```csharp
public void MethodWithRef(ref int value)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `value` | `int` | The value to be modified by reference. |

#### Remarks

This method doubles the input value.

