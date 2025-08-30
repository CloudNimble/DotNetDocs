# DerivedClass

## Definition

**Namespace:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios
**Assembly:** CloudNimble.DotNetDocs.Tests.Shared
**Inheritance:** CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.BaseClass

## Syntax

```csharp
public class DerivedClass : CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios.BaseClass
```

## Description

A derived class for testing inheritance documentation.

## Examples

var derived = new DerivedClass();
            var result = derived.VirtualMethod();

## Constructors

### .ctor

#### Syntax

```csharp
public .ctor()
```

## Properties

### BaseProperty

Gets or sets the base property with overridden behavior.

#### Syntax

```csharp
public override string BaseProperty { get; set; }
```

#### Property Value

Type: `string`

### DerivedProperty

Gets or sets the derived property.

#### Syntax

```csharp
public string DerivedProperty { get; set; }
```

#### Property Value

Type: `string`

## Methods

### DerivedMethod

An additional method in the derived class.

#### Syntax

```csharp
public void DerivedMethod()
```

### VirtualMethod

Overrides the virtual method from the base class.

#### Syntax

```csharp
public override string VirtualMethod()
```

#### Returns

Type: `string`

