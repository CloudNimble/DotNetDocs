# ClassWithSpecialCharacters

## Definition

**Assembly:** CloudNimble.DotNetDocs.Tests.Shared.dll

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.EdgeCases

**Inheritance:** System.Object

## Syntax

```csharp
CloudNimble.DotNetDocs.Tests.Shared.EdgeCases.ClassWithSpecialCharacters
```

## Summary

A class with special characters in documentation: &lt;, &gt;, &amp;, ", '.

## Remarks

This tests handling of XML special characters like &lt;tag&gt; and &amp;entity;.
            Also tests "quotes" and 'apostrophes'.

## Examples

<code>
// Using generics: List&lt;string&gt;
var list = new List&lt;string&gt;();
if (x &gt; 0 &amp;&amp; y &lt; 10) { }
</code>

## Constructors

### .ctor

#### Syntax

```csharp
public ClassWithSpecialCharacters()
```

### .ctor

#### Syntax

```csharp
public Object()
```

## Methods

### Equals

#### Syntax

```csharp
public virtual bool Equals(object obj)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `obj` | `object?` | - |

#### Returns

Type: `bool`

### Equals

#### Syntax

```csharp
public static bool Equals(object objA, object objB)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `objA` | `object?` | - |
| `objB` | `object?` | - |

#### Returns

Type: `bool`

### GetHashCode

#### Syntax

```csharp
public virtual int GetHashCode()
```

#### Returns

Type: `int`

### GetType

#### Syntax

```csharp
public System.Type GetType()
```

#### Returns

Type: `System.Type`

### MemberwiseClone

#### Syntax

```csharp
protected internal object MemberwiseClone()
```

#### Returns

Type: `object`

### MethodWithSpecialChars

A method with special characters in docs: &lt;T&gt; generics.

#### Syntax

```csharp
public string MethodWithSpecialChars(string input)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `input` | `string` | An input with "quotes" and 'apostrophes'. |

#### Returns

Type: `string`
A string with &amp; ampersands.

#### Remarks

This method handles &lt;, &gt;, &amp; characters properly.

### ReferenceEquals

#### Syntax

```csharp
public static bool ReferenceEquals(object objA, object objB)
```

#### Parameters

| Name | Type | Description |
|------|------|-------------|
| `objA` | `object?` | - |
| `objB` | `object?` | - |

#### Returns

Type: `bool`

### ToString

#### Syntax

```csharp
public virtual string ToString()
```

#### Returns

Type: `string?`

