# List

## Definition

**Assembly:** System.Collections.dll

**Namespace:** System.Collections.Generic

## Syntax

```csharp
System.Collections.Generic.List<T>
```

## Summary

This type is defined in System.Collections.

## Remarks

See [Microsoft documentation](https://learn.microsoft.com/dotnet/api/system.collections.generic.list{t}) for more information about the rest of the API.

## Methods

### AddMultiple

Adds multiple items to a list in one call.

#### Syntax

```csharp
public static System.Collections.Generic.List<T> AddMultiple<T>(System.Collections.Generic.List<T> list, params T[] items)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `list` | `System.Collections.Generic.List&lt;T&gt;` | The list to add items to. |
| `items` | `T[]` | The items to add. |

#### Returns

Type: `System.Collections.Generic.List&lt;T&gt;`
The list for fluent chaining.

#### Type Parameters

- `T` - The type of elements in the list.

#### Examples

<code>
var numbers = new List&lt;int&gt;()
    .AddMultiple(1, 2, 3, 4, 5);
</code>

### IsNullOrEmpty

Checks if a list is null or empty.

#### Syntax

```csharp
public static bool IsNullOrEmpty<T>(System.Collections.Generic.List<T> list)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `list` | `System.Collections.Generic.List&lt;T&gt;` | The list to check. |

#### Returns

Type: `bool`
True if the list is null or empty, otherwise false.

#### Type Parameters

- `T` - The type of elements in the list.

#### Examples

<code>
var numbers = new List&lt;int&gt;();
if (numbers.IsNullOrEmpty())
{
    // Handle empty list
}
</code>

### Shuffle

Shuffles the elements in a list randomly.

#### Syntax

```csharp
public static System.Collections.Generic.List<T> Shuffle<T>(System.Collections.Generic.List<T> list)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `list` | `System.Collections.Generic.List&lt;T&gt;` | The list to shuffle. |

#### Returns

Type: `System.Collections.Generic.List&lt;T&gt;`
The shuffled list.

#### Type Parameters

- `T` - The type of elements in the list.

#### Remarks

This uses the Fisher-Yates shuffle algorithm for randomization.

