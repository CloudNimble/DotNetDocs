# ParameterVariations

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.Parameters

**Base Type:** System.Object

## Overview

A class demonstrating various parameter types and patterns.

## Constructors

### .ctor

```csharp
public .ctor()
```

## Methods

### GenericMethod

```csharp
public string GenericMethod<T>(T value)
```

A generic method with a type parameter.

**Parameters:**

- `value`: The value to process.

**Examples:**

var result1 = GenericMethod<int>(42);
            var result2 = GenericMethod("hello");

### GenericMethodWithMultipleTypes

```csharp
public System.Collections.Generic.KeyValuePair<TKey, TValue> GenericMethodWithMultipleTypes<TKey, TValue>(TKey key, TValue value)
```

A method with multiple generic type parameters.

**Parameters:**

- `key`: The key.
- `value`: The value.

### MethodWithConstraints

```csharp
public string MethodWithConstraints<T>(T item)
```

A method demonstrating parameter constraints.

**Parameters:**

- `item`: The item to process.

### MethodWithNullables

```csharp
public string MethodWithNullables(System.Nullable<int> nullableInt, string? nullableString)
```

A method with nullable parameters.

**Parameters:**

- `nullableInt`: An optional nullable integer.
- `nullableString`: An optional nullable string.

### MethodWithOptionalParam

```csharp
public string MethodWithOptionalParam(string required, int optional)
```

A method with an optional parameter.

**Parameters:**

- `required`: The required string parameter.
- `optional`: The optional integer parameter with a default value.

**Examples:**

var result1 = MethodWithOptionalParam("test");      // Uses default value 42
            var result2 = MethodWithOptionalParam("test", 100); // Uses provided value

### MethodWithOut

```csharp
public bool MethodWithOut(string input, int value)
```

A method with an out parameter.

**Parameters:**

- `input`: The input string to parse.
- `value`: The output integer value if parsing succeeds.

### MethodWithParams

```csharp
public int MethodWithParams(int[] values)
```

A method with a params array.

**Parameters:**

- `values`: Variable number of integer values.

**Examples:**

var sum1 = MethodWithParams(1, 2, 3);        // Returns 6
            var sum2 = MethodWithParams(new[] { 1, 2 }); // Returns 3

### MethodWithRef

```csharp
public void MethodWithRef(int value)
```

A method with a ref parameter.

**Parameters:**

- `value`: The value to be modified by reference.

