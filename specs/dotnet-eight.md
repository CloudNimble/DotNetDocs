# .NET Framework-Specific Documentation Differences

## Problem

`System.Object.MemberwiseClone()` has different signatures across .NET versions, causing baseline test failures:

- **.NET 8**: `protected object MemberwiseClone()` (non-virtual)
- **.NET 9+**: `protected internal virtual object MemberwiseClone()`

This difference affects:
1. **Accessibility**: `Protected` vs `ProtectedOrInternal`
2. **Virtuality**: `IsVirtual = false` vs `IsVirtual = true`
3. **Signature string**: Different modifiers in the formatted signature

## Root Cause

Roslyn's `IMethodSymbol.DeclaredAccessibility` reports the actual runtime behavior for each framework:
- .NET 8 correctly reports `Accessibility.Protected`
- .NET 9+ correctly reports `Accessibility.ProtectedOrInternal`

The metadata actually changed between framework versions - this is not a Roslyn bug.

## Solution Approach

**Do NOT** try to force all frameworks to generate identical output. Instead, normalize baselines during test comparison to account for known framework differences.

### Implementation Strategy

Since we maintain a **single set of baselines** (not framework-specific copies), we generate them from .NET 8 and normalize during comparison on .NET 9+:

1. **Baselines**: Generated from .NET 8 (show `protected` non-virtual)
2. **Test Normalization**: On .NET 9+, adjust baseline objects before comparison

### Files Modified

#### 1. AssemblyManager.cs
- **Reverted** the universal fix that forced `Protected` on all frameworks
- Now reports actual framework-specific accessibility via `method.DeclaredAccessibility`

#### 2. MarkdownRendererTests.cs - `CompareWithFolderBaseline()`
```csharp
#if !NET8_0
    // Baselines were generated from .NET 8, so normalize for .NET 9+ comparison
    normalizedBaseline = normalizedBaseline
        .Replace("protected object MemberwiseClone()",
                 "protected internal virtual object MemberwiseClone()");
#endif
```

#### 3. YamlRendererTests.cs - `CompareYamlWithFolderBaseline()`
```csharp
// Parse YAML into DocType objects
var actualYaml = _yamlDeserializer.Deserialize<List<DocType>>(actualContent);
var baselineYaml = _yamlDeserializer.Deserialize<List<DocType>>(baselineContent);

#if !NET8_0
    // Find and fix MemberwiseClone members in baseline objects
    foreach (var type in baselineYaml)
    {
        var memberwiseClone = type.Members?.FirstOrDefault(m =>
            m.Name == "MemberwiseClone" &&
            m.DeclaringTypeName == "object" &&
            m.Accessibility == Accessibility.Protected);

        if (memberwiseClone != null)
        {
            memberwiseClone.Accessibility = Accessibility.ProtectedOrInternal;
            memberwiseClone.IsVirtual = true;
            memberwiseClone.Signature = "protected internal virtual object MemberwiseClone()";
        }
    }
#endif

// Compare objects (not strings!)
actualYaml.Should().BeEquivalentTo(baselineYaml, ...);
```

#### 4. AssemblyManagerTests.cs - `DocumentAsync_ProducesConsistentBaseline()`
```csharp
#if !NET8_0
    // Baselines were generated from .NET 8, so normalize for .NET 9+ comparison
    baseline = baseline
        .Replace("\"signature\": \"protected object MemberwiseClone()\"",
                 "\"signature\": \"protected internal virtual object MemberwiseClone()\"");
#endif
```

## Current Status

- **Code changes**: Complete
- **Test status**: Still failing (3-4 tests per framework)
- **Issue**: The normalization may not be working correctly, or there are additional differences beyond MemberwiseClone

## Next Steps

1. **Debug YAML deserialization**: The `List<DocType>` deserialization may be failing due to missing parameterless constructors
2. **Verify normalization works**: Add debug output to confirm the baseline objects are being modified correctly
3. **Check for other differences**: There may be additional framework-specific differences beyond MemberwiseClone
4. **Consider alternative**: If object deserialization fails, fall back to string-based regex replacement

## Key Insights

- **Don't fight the framework**: Let each version generate accurate documentation for its runtime
- **Single source of truth**: One set of baselines, normalized at test time
- **Object comparison > String comparison**: Working with deserialized objects is cleaner than regex on strings
- **Conditional compilation**: Use `#if !NET8_0` to apply normalization only where needed
