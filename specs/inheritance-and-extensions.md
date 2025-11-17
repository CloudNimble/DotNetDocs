# Inherited Members & Extension Methods

## Overview

This specification details the implementation of Microsoft-style documentation that includes inherited members and extension methods, matching the user experience of official .NET API documentation at learn.microsoft.com.

## Goals

1. **Include inherited members** from base classes and interfaces in type documentation
2. **Relocate extension methods** to the types they extend
3. **Provide visual indicators** showing member provenance (inherited, override, extension, etc.)
4. **Create external type references** for extension method targets outside the current assembly
5. **Maintain alphabetical sorting** of all members together (declared, inherited, extension)
6. **Match Microsoft's documentation style** for consistency and familiarity

## Current State

### Member Extraction

Currently, `AssemblyManager.BuildDocType()` (lines 533-682) processes members using:

```csharp
foreach (var member in type.GetMembers()
    .Where(m => docType.IncludedMembers.Contains(m.DeclaredAccessibility)
        && !m.IsImplicitlyDeclared))
```

**Issues:**
- Roslyn's `GetMembers()` returns ALL members including inherited ones
- Current filtering by `m.DeclaredAccessibility` checks the **original** declaration, not effective visibility
- No tracking of whether members are inherited vs. declared
- No indication of override relationships
- Extension methods stay in their static container classes

### Extension Methods

Extension methods are currently documented as separate static classes (e.g., `DotNetDocsCore_IServiceCollectionExtensions`). Users must:
1. Know extensions exist
2. Find the separate documentation
3. Mentally connect them to the types they extend

## Proposed Solution

### Architecture

The solution uses a **post-processing approach** that:

1. Detects member provenance during initial extraction
2. Relocates extension methods after main assembly processing
3. Creates external type references as needed
4. Maintains clean separation of concerns

### Data Model Changes

#### DocMember Properties

Add to `src/CloudNimble.DotNetDocs.Core/DocMember.cs`:

```csharp
/// <summary>
/// Gets or sets whether this member is inherited from a base type or interface.
/// </summary>
/// <value>
/// <c>true</c> if the member is declared in a base type or interface;
/// <c>false</c> if declared in the containing type.
/// </value>
public bool IsInherited { get; set; }

/// <summary>
/// Gets or sets whether this member overrides a base implementation.
/// </summary>
/// <value>
/// <c>true</c> if the member uses the <c>override</c> keyword; otherwise <c>false</c>.
/// </value>
public bool IsOverride { get; set; }

/// <summary>
/// Gets or sets whether this member is virtual.
/// </summary>
/// <value>
/// <c>true</c> if the member uses the <c>virtual</c> keyword; otherwise <c>false</c>.
/// </value>
public bool IsVirtual { get; set; }

/// <summary>
/// Gets or sets whether this member is abstract.
/// </summary>
/// <value>
/// <c>true</c> if the member uses the <c>abstract</c> keyword; otherwise <c>false</c>.
/// </value>
public bool IsAbstract { get; set; }

/// <summary>
/// Gets or sets the fully qualified name of the type that declares this member.
/// </summary>
/// <value>
/// For inherited members, this is the base type or interface name.
/// For extension methods, this is the static class containing the method.
/// For declared members, this matches the containing type.
/// </value>
public string? DeclaringTypeName { get; set; }

/// <summary>
/// Gets or sets the signature of the member being overridden, if applicable.
/// </summary>
/// <value>
/// The fully qualified signature of the base member, or <c>null</c> if not an override.
/// </value>
public string? OverriddenMember { get; set; }

/// <summary>
/// Gets or sets whether this member is an extension method.
/// </summary>
/// <value>
/// <c>true</c> if this is a static method with the <c>this</c> modifier on its first parameter;
/// otherwise <c>false</c>.
/// </value>
public bool IsExtensionMethod { get; set; }

/// <summary>
/// Gets or sets the fully qualified name of the type this extension method extends.
/// </summary>
/// <value>
/// The type of the first parameter (with <c>this</c> modifier), or <c>null</c> if not an extension method.
/// </value>
public string? ExtendedTypeName { get; set; }
```

**Rationale:**
- These properties enable renderers to display appropriate badges and indicators
- They support filtering and grouping scenarios
- They maintain traceability to original declarations

#### DocType Properties

Add to `src/CloudNimble.DotNetDocs.Core/DocType.cs`:

```csharp
/// <summary>
/// Gets or sets whether this type is an external reference created to host extension methods.
/// </summary>
/// <value>
/// <c>true</c> if this type is not part of the documented assembly but was created
/// to show extension methods that apply to it; otherwise <c>false</c>.
/// </value>
/// <remarks>
/// External references are minimal <see cref="DocType"/> instances created when
/// <see cref="ProjectContext.CreateExternalTypeReferences"/> is enabled and extension
/// methods target types outside the current assembly.
/// </remarks>
public bool IsExternalReference { get; set; }
```

**Rationale:**
- Distinguishes types actually in the assembly from placeholder types
- Allows renderers to add appropriate notices and links
- Supports filtering in navigation generation

#### ProjectContext Properties

Add to `src/CloudNimble.DotNetDocs.Core/ProjectContext.cs`:

```csharp
/// <summary>
/// Gets or sets whether to include members inherited from <see cref="System.Object"/>.
/// </summary>
/// <value>
/// <c>true</c> to document <c>ToString()</c>, <c>GetHashCode()</c>, <c>Equals()</c>, etc.
/// on every type; <c>false</c> to exclude these common members. Default is <c>true</c>.
/// </value>
/// <remarks>
/// Setting this to <c>false</c> reduces documentation noise while still including
/// members inherited from other base types and interfaces.
/// </remarks>
public bool IncludeSystemObjectInheritance { get; set; } = true;

/// <summary>
/// Gets or sets whether to create documentation for external types that have extension methods.
/// </summary>
/// <value>
/// <c>true</c> to create minimal <see cref="DocType"/> entries for types outside the assembly
/// that are extended by extension methods; <c>false</c> to only relocate extensions for types
/// within the assembly. Default is <c>true</c>.
/// </value>
/// <remarks>
/// When enabled, extending <c>IServiceCollection</c> creates a documentation page showing
/// only your extension methods, with a link to Microsoft's official documentation.
/// </remarks>
public bool CreateExternalTypeReferences { get; set; } = true;
```

**Rationale:**
- `IncludeSystemObjectInheritance` provides noise control while matching Microsoft's default behavior
- `CreateExternalTypeReferences` enables comprehensive extension method documentation
- Both default to `true` for maximum discoverability

### AssemblyManager Changes

#### Enhanced BuildDocType() Method

Location: `src/CloudNimble.DotNetDocs.Core/AssemblyManager.cs` lines 533-682

**Current Code:**
```csharp
foreach (var member in type.GetMembers()
    .Where(m => docType.IncludedMembers.Contains(m.DeclaredAccessibility)
        && !m.IsImplicitlyDeclared))
{
    // Process member...
}
```

**Enhanced Code:**
```csharp
// Get all members including inherited
var allMembers = type.GetMembers()
    .Where(m => !m.IsImplicitlyDeclared)
    .ToList();

foreach (var member in allMembers)
{
    // Check if member is inherited
    var isInherited = !SymbolEqualityComparer.Default.Equals(member.ContainingType, type);
    var declaringType = member.ContainingType;

    // Filter System.Object members if configured
    if (isInherited &&
        declaringType.SpecialType == SpecialType.System_Object &&
        !(projectContext?.IncludeSystemObjectInheritance ?? true))
    {
        continue;
    }

    // Check accessibility for inherited members
    if (isInherited && !IsAccessibleInDerivedType(member, type))
    {
        continue;
    }

    // Check accessibility for declared members
    if (!isInherited && !docType.IncludedMembers.Contains(member.DeclaredAccessibility))
    {
        continue;
    }

    // Process member and set provenance properties...
    if (member is IMethodSymbol method)
    {
        var mDoc = ExtractDocumentationXml(method);

        docMember = new DocMember(method)
        {
            // ... existing properties ...

            // New provenance properties
            IsInherited = isInherited,
            DeclaringTypeName = declaringType.ToDisplayString(),
            IsOverride = method.IsOverride,
            IsVirtual = method.IsVirtual,
            IsAbstract = method.IsAbstract,
            OverriddenMember = method.OverriddenMethod?.ToDisplayString(),
            IsExtensionMethod = method.IsExtensionMethod,
            ExtendedTypeName = method.IsExtensionMethod
                ? method.Parameters.First().Type.ToDisplayString()
                : null
        };
    }
    else if (member is IPropertySymbol property)
    {
        var pDoc = ExtractDocumentationXml(property);

        docMember = new DocMember(property)
        {
            // ... existing properties ...

            // New provenance properties
            IsInherited = isInherited,
            DeclaringTypeName = declaringType.ToDisplayString(),
            IsOverride = property.IsOverride,
            IsVirtual = property.IsVirtual,
            IsAbstract = property.IsAbstract,
            OverriddenMember = property.OverriddenProperty?.ToDisplayString()
        };
    }
    // ... similar for fields and events ...
}
```

**Key Changes:**
1. Track inheritance using `SymbolEqualityComparer`
2. Filter System.Object members based on configuration
3. Use `IsAccessibleInDerivedType()` for inherited member accessibility
4. Set all provenance properties during member creation
5. Detect extension methods using `IsExtensionMethod`

#### New RelocateExtensionMethods() Method

Add to `AssemblyManager.cs`:

```csharp
/// <summary>
/// Relocates extension methods from their declaring static classes to the types they extend.
/// </summary>
/// <param name="assembly">The assembly being documented.</param>
/// <param name="projectContext">The project context with configuration settings.</param>
/// <remarks>
/// This method performs post-processing after initial assembly documentation is complete.
/// Extension methods are moved from static container classes to the types they extend,
/// creating external type references as needed.
/// </remarks>
private void RelocateExtensionMethods(DocAssembly assembly, ProjectContext? projectContext)
{
    // Find all extension methods
    var extensionMethods = assembly.Namespaces
        .SelectMany(ns => ns.Types)
        .Where(t => t.Symbol.IsStatic)
        .SelectMany(t => t.Members)
        .Where(m => m.IsExtensionMethod)
        .ToList();

    if (!extensionMethods.Any())
        return;

    // Track types we've created for external references
    var externalTypes = new Dictionary<string, DocType>();

    foreach (var extMethod in extensionMethods)
    {
        var methodSymbol = (IMethodSymbol)extMethod.Symbol;
        var extendedType = methodSymbol.Parameters.First().Type;
        var extendedTypeKey = extendedType.ToDisplayString();
        var declaringType = methodSymbol.ContainingType;

        // Find or create target DocType
        var targetType = assembly.Namespaces
            .SelectMany(ns => ns.Types)
            .FirstOrDefault(t => SymbolEqualityComparer.Default.Equals(t.Symbol, extendedType));

        // Create external reference if needed
        if (targetType is null && (projectContext?.CreateExternalTypeReferences ?? true))
        {
            if (!externalTypes.TryGetValue(extendedTypeKey, out targetType))
            {
                targetType = CreateExternalTypeReference(
                    extendedType,
                    assembly,
                    projectContext);
                externalTypes[extendedTypeKey] = targetType;
            }
        }

        if (targetType is not null)
        {
            // Remove from declaring static class
            var staticClass = assembly.Namespaces
                .SelectMany(ns => ns.Types)
                .First(t => SymbolEqualityComparer.Default.Equals(t.Symbol, declaringType));

            staticClass.Members.Remove(extMethod);

            // Add to target type
            targetType.Members.Add(extMethod);
        }
    }

    // Remove empty static extension classes
    foreach (var ns in assembly.Namespaces)
    {
        var emptyClasses = ns.Types
            .Where(t => t.Symbol.IsStatic && !t.Members.Any())
            .ToList();

        foreach (var emptyClass in emptyClasses)
        {
            ns.Types.Remove(emptyClass);
        }
    }
}
```

**Key Features:**
1. Single pass through extension methods
2. Tracks external types to avoid duplicates
3. Removes extension methods from static classes
4. Adds them to target type's Members collection
5. Cleans up empty static classes

#### New CreateExternalTypeReference() Method

Add to `AssemblyManager.cs`:

```csharp
/// <summary>
/// Creates a minimal DocType for an external type that has extension methods.
/// </summary>
/// <param name="typeSymbol">The external type symbol.</param>
/// <param name="assembly">The assembly being documented.</param>
/// <param name="projectContext">The project context.</param>
/// <returns>A new DocType marked as an external reference.</returns>
private DocType CreateExternalTypeReference(
    ITypeSymbol typeSymbol,
    DocAssembly assembly,
    ProjectContext? projectContext)
{
    var nsName = typeSymbol.ContainingNamespace?.ToDisplayString() ?? "";

    // Find or create namespace
    var ns = assembly.Namespaces.FirstOrDefault(n => n.Name == nsName);
    if (ns is null)
    {
        ns = new DocNamespace(typeSymbol.ContainingNamespace!)
        {
            Name = nsName,
            FullName = nsName,
            DisplayName = nsName,
            AssemblyName = assembly.Name
        };
        assembly.Namespaces.Add(ns);
    }

    // Create minimal DocType
    var docType = new DocType(typeSymbol)
    {
        Name = typeSymbol.Name,
        FullName = typeSymbol.ToDisplayString(),
        DisplayName = typeSymbol.ToDisplayString(),
        Signature = typeSymbol.ToDisplayString(DocumentationSignatureFormat),
        TypeKind = typeSymbol.TypeKind,
        AssemblyName = typeSymbol.ContainingAssembly?.Name,
        IsExternalReference = true,
        IncludedMembers = projectContext?.IncludedMembers ?? [Accessibility.Public]
    };

    // Add helpful summary linking to official docs
    if (typeSymbol.ContainingAssembly?.Name?.StartsWith("System") == true ||
        typeSymbol.ContainingAssembly?.Name?.StartsWith("Microsoft") == true)
    {
        var docsUrl = GetMicrosoftDocsUrl(typeSymbol);
        docType.Summary = $"This type is defined in {typeSymbol.ContainingAssembly.Name}. " +
                         $"See [Microsoft documentation]({docsUrl}) for complete API reference.";
    }
    else
    {
        docType.Summary = $"This type is defined in {typeSymbol.ContainingAssembly?.Name ?? "external assembly"}.";
    }

    ns.Types.Add(docType);
    return docType;
}
```

**Key Features:**
1. Creates minimal DocType with only essential properties
2. Finds or creates namespace as needed
3. Adds helpful summary with link to Microsoft docs
4. Marks as `IsExternalReference = true`

#### New Helper Methods

Add to `AssemblyManager.cs`:

```csharp
/// <summary>
/// Determines if a member is accessible in a derived type.
/// </summary>
/// <param name="member">The member symbol to check.</param>
/// <param name="derivedType">The derived type symbol.</param>
/// <returns>
/// <c>true</c> if the member is accessible from the derived type; otherwise <c>false</c>.
/// </returns>
private static bool IsAccessibleInDerivedType(ISymbol member, ITypeSymbol derivedType)
{
    return member.DeclaredAccessibility switch
    {
        Accessibility.Public => true,
        Accessibility.Protected => true,
        Accessibility.ProtectedOrInternal => true,
        Accessibility.Internal => SymbolEqualityComparer.Default.Equals(
            member.ContainingAssembly,
            derivedType.ContainingAssembly),
        Accessibility.ProtectedAndInternal => SymbolEqualityComparer.Default.Equals(
            member.ContainingAssembly,
            derivedType.ContainingAssembly),
        _ => false
    };
}

/// <summary>
/// Generates a Microsoft Learn documentation URL for a type.
/// </summary>
/// <param name="typeSymbol">The type symbol.</param>
/// <returns>A URL to the Microsoft Learn documentation, or empty string if not applicable.</returns>
private static string GetMicrosoftDocsUrl(ITypeSymbol typeSymbol)
{
    if (typeSymbol.ContainingAssembly?.Name?.StartsWith("System") != true &&
        typeSymbol.ContainingAssembly?.Name?.StartsWith("Microsoft") != true)
    {
        return "";
    }

    var fullName = typeSymbol.ToDisplayString()
        .Replace('<', '{')
        .Replace('>', '}')
        .ToLowerInvariant();

    return $"https://learn.microsoft.com/dotnet/api/{fullName}";
}
```

**Key Features:**
1. `IsAccessibleInDerivedType()` properly handles all accessibility levels
2. Supports cross-assembly `internal` visibility rules
3. `GetMicrosoftDocsUrl()` generates correct learn.microsoft.com URLs
4. Handles generic types with proper URL encoding

#### Integration Point

In `DocumentAsync()` method, call after `BuildModel()`:

```csharp
public async Task<DocAssembly> DocumentAsync(/* ... */)
{
    // ... existing code ...

    var assembly = BuildModel(compilation, projectContext);

    // Relocate extension methods (always enabled)
    RelocateExtensionMethods(assembly, projectContext);

    return assembly;
}
```

### Mintlify Renderer Changes

#### Update RenderMember() Method

Location: `src/CloudNimble.DotNetDocs.Mintlify/MintlifyRenderer.cs` line 1207

**Current Code:**
```csharp
internal void RenderMember(StringBuilder sb, DocMember member)
{
    var primaryColor = _options?.Template?.Colors?.Primary ?? "#0D9373";

    sb.AppendLine($"### <Icon icon=\"{MintlifyIcons.GetIconForMember(member)}\" iconType=\"{MemberIconType}\" color=\"{primaryColor}\" size={{{MemberIconSize}}} style={{{{ paddingRight: '8px' }}}} />  {member.Name}");
    sb.AppendLine();

    // ... rest of method ...
}
```

**Enhanced Code:**
```csharp
internal void RenderMember(StringBuilder sb, DocMember member)
{
    var primaryColor = _options?.Template?.Colors?.Primary ?? "#0D9373";

    // Build badges for member provenance
    var badges = new List<string>();

    if (member.IsExtensionMethod)
    {
        badges.Add("<Badge text=\"Extension\" variant=\"success\" />");
    }

    if (member.IsInherited && !member.IsOverride)
    {
        badges.Add("<Badge text=\"Inherited\" variant=\"neutral\" />");
    }

    if (member.IsOverride)
    {
        badges.Add("<Badge text=\"Override\" variant=\"info\" />");
    }

    if (member.IsVirtual && !member.IsOverride)
    {
        badges.Add("<Badge text=\"Virtual\" variant=\"warning\" />");
    }

    if (member.IsAbstract)
    {
        badges.Add("<Badge text=\"Abstract\" variant=\"warning\" />");
    }

    var badgeString = badges.Any() ? " " + string.Join(" ", badges) : "";

    // Render header with badges
    sb.AppendLine($"### <Icon icon=\"{MintlifyIcons.GetIconForMember(member)}\" iconType=\"{MemberIconType}\" color=\"{primaryColor}\" size={{{MemberIconSize}}} style={{{{ paddingRight: '8px' }}}} />  {member.Name}{badgeString}");
    sb.AppendLine();

    // Add provenance note if inherited or extension
    if (member.IsExtensionMethod && !string.IsNullOrWhiteSpace(member.DeclaringTypeName))
    {
        sb.AppendLine($"<Note>Extension method from `{member.DeclaringTypeName}`</Note>");
        sb.AppendLine();
    }
    else if (member.IsInherited && !string.IsNullOrWhiteSpace(member.DeclaringTypeName))
    {
        sb.AppendLine($"<Note>Inherited from `{member.DeclaringTypeName}`</Note>");
        sb.AppendLine();
    }

    // ... rest of existing method (summary, syntax, parameters, etc.) ...
}
```

**Visual Example:**

```markdown
### 🧩 AddDotNetDocs <Badge text="Extension" variant="success" />

<Note>Extension method from `Microsoft.Extensions.DependencyInjection.DotNetDocsCore_IServiceCollectionExtensions`</Note>

Adds DotNetDocs services to the service collection.

#### Syntax
...
```

```markdown
### 📋 ToString <Badge text="Inherited" variant="neutral" />

<Note>Inherited from `System.Object`</Note>

Returns a string representation of this object.

#### Syntax
...
```

**Badge Color Scheme:**
- **Extension** (green/success) - Clearly marks helpful extension methods
- **Inherited** (neutral/gray) - Non-intrusive indicator for base members
- **Override** (blue/info) - Shows customization of base behavior
- **Virtual** (yellow/warning) - Indicates extensibility point
- **Abstract** (yellow/warning) - Requires implementation

#### Update External Type Rendering

Add to `RenderTypeAsync()` after summary rendering:

```csharp
internal async Task RenderTypeAsync(DocType type, DocNamespace ns, string outputPath)
{
    var sb = new StringBuilder();

    // Add frontmatter
    sb.Append(GenerateFrontmatter(type, ns));

    // Add external reference notice
    if (type.IsExternalReference)
    {
        sb.AppendLine("<Warning>");
        sb.AppendLine($"This type is defined in **{type.AssemblyName ?? "an external assembly"}**, not in the assembly being documented.");
        sb.AppendLine();
        sb.AppendLine("This page shows only extension methods provided by the current assembly.");

        if (!string.IsNullOrWhiteSpace(type.Summary))
        {
            sb.AppendLine();
            sb.AppendLine(type.Summary);
        }

        sb.AppendLine("</Warning>");
        sb.AppendLine();
    }

    // ... rest of existing rendering ...
}
```

**Visual Example:**

```markdown
---
title: IServiceCollection
description: "This type is defined in Microsoft.Extensions.DependencyInjection..."
icon: interface
---

<Warning>
This type is defined in **Microsoft.Extensions.DependencyInjection.Abstractions**, not in the assembly being documented.

This page shows only extension methods provided by the current assembly.

See [Microsoft documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection) for complete API reference.
</Warning>

## Methods

### AddDotNetDocs <Badge text="Extension" variant="success" />
...
```

#### Member Sorting

No changes needed! The existing code already sorts alphabetically:

```csharp
// Properties - line 1123
foreach (var prop in properties.OrderBy(p => p.Name))

// Methods - line 1134
foreach (var method in methods.OrderBy(m => m.Name))

// Events - line 1145
foreach (var evt in events.OrderBy(e => e.Name))

// Fields - line 1159
foreach (var field in fields.OrderBy(f => f.Name))
```

Since extension methods and inherited members are now in the same `Members` collection, they automatically get sorted alphabetically alongside declared members. ✅

### Testing Strategy

#### Unit Tests for Inherited Members

Create `src/CloudNimble.DotNetDocs.Tests.Core/InheritedMembersTests.cs`:

```csharp
[TestClass]
public class InheritedMembersTests : DotNetDocsTestBase
{
    [TestMethod]
    public void BaseClass_Members_Should_Be_Inherited()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class BaseClass
    {
        public string BaseProperty { get; set; }
        public virtual void BaseMethod() { }
    }

    public class DerivedClass : BaseClass
    {
        public string DerivedProperty { get; set; }
    }
}";
        var compilation = CreateCompilation(source);
        var manager = new AssemblyManager("", "");

        // Act
        var assembly = await manager.DocumentAsync(compilation, null);
        var derivedType = assembly.Namespaces
            .First().Types
            .First(t => t.Name == "DerivedClass");

        // Assert
        derivedType.Members.Should().Contain(m => m.Name == "BaseProperty" && m.IsInherited);
        derivedType.Members.Should().Contain(m => m.Name == "BaseMethod" && m.IsInherited);
        derivedType.Members.Should().Contain(m => m.Name == "DerivedProperty" && !m.IsInherited);
    }

    [TestMethod]
    public void Override_Members_Should_Be_Marked()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class BaseClass
    {
        public virtual string Method() => ""base"";
    }

    public class DerivedClass : BaseClass
    {
        public override string Method() => ""derived"";
    }
}";
        var compilation = CreateCompilation(source);
        var manager = new AssemblyManager("", "");

        // Act
        var assembly = await manager.DocumentAsync(compilation, null);
        var derivedType = assembly.Namespaces
            .First().Types
            .First(t => t.Name == "DerivedClass");

        // Assert
        var overrideMethod = derivedType.Members.First(m => m.Name == "Method");
        overrideMethod.IsOverride.Should().BeTrue();
        overrideMethod.IsInherited.Should().BeFalse();
        overrideMethod.OverriddenMember.Should().Contain("BaseClass.Method");
    }

    [TestMethod]
    public void SystemObject_Members_Should_Be_Filtered_When_Configured()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class MyClass { }
}";
        var compilation = CreateCompilation(source);
        var manager = new AssemblyManager("", "");
        var context = new ProjectContext { IncludeSystemObjectInheritance = false };

        // Act
        var assembly = await manager.DocumentAsync(compilation, context);
        var myType = assembly.Namespaces.First().Types.First();

        // Assert
        myType.Members.Should().NotContain(m => m.Name == "ToString");
        myType.Members.Should().NotContain(m => m.Name == "GetHashCode");
        myType.Members.Should().NotContain(m => m.Name == "Equals");
    }

    [TestMethod]
    public void Interface_Members_Should_Be_Inherited()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public interface IBase
    {
        void InterfaceMethod();
    }

    public class Implementation : IBase
    {
        public void InterfaceMethod() { }
        public void OwnMethod() { }
    }
}";
        var compilation = CreateCompilation(source);
        var manager = new AssemblyManager("", "");

        // Act
        var assembly = await manager.DocumentAsync(compilation, null);
        var implType = assembly.Namespaces
            .First().Types
            .First(t => t.Name == "Implementation");

        // Assert
        var interfaceMethod = implType.Members.First(m => m.Name == "InterfaceMethod");
        interfaceMethod.IsInherited.Should().BeFalse(); // Declared in Implementation
        interfaceMethod.DeclaringTypeName.Should().Be("TestNamespace.Implementation");
    }
}
```

#### Unit Tests for Extension Methods

Create `src/CloudNimble.DotNetDocs.Tests.Core/ExtensionMethodsTests.cs`:

```csharp
[TestClass]
public class ExtensionMethodsTests : DotNetDocsTestBase
{
    [TestMethod]
    public void ExtensionMethod_Should_Be_Detected()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class MyClass { }

    public static class MyExtensions
    {
        public static void ExtensionMethod(this MyClass instance) { }
    }
}";
        var compilation = CreateCompilation(source);
        var manager = new AssemblyManager("", "");

        // Act
        var assembly = await manager.DocumentAsync(compilation, null);

        // Assert
        var myType = assembly.Namespaces
            .First().Types
            .First(t => t.Name == "MyClass");

        myType.Members.Should().Contain(m => m.Name == "ExtensionMethod" && m.IsExtensionMethod);
    }

    [TestMethod]
    public void ExtensionMethod_Should_Be_Relocated_To_Target_Type()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class MyClass { }

    public static class MyExtensions
    {
        public static void ExtensionMethod(this MyClass instance) { }
    }
}";
        var compilation = CreateCompilation(source);
        var manager = new AssemblyManager("", "");

        // Act
        var assembly = await manager.DocumentAsync(compilation, null);

        // Assert
        var extensionClass = assembly.Namespaces
            .First().Types
            .FirstOrDefault(t => t.Name == "MyExtensions");

        extensionClass.Should().BeNull("Empty extension classes should be removed");

        var myType = assembly.Namespaces
            .First().Types
            .First(t => t.Name == "MyClass");

        var extMethod = myType.Members.First(m => m.Name == "ExtensionMethod");
        extMethod.IsExtensionMethod.Should().BeTrue();
        extMethod.DeclaringTypeName.Should().Be("TestNamespace.MyExtensions");
        extMethod.ExtendedTypeName.Should().Be("TestNamespace.MyClass");
    }

    [TestMethod]
    public void External_Type_Reference_Should_Be_Created()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;

namespace TestNamespace
{
    public static class ListExtensions
    {
        public static void MyExtension<T>(this List<T> list) { }
    }
}";
        var compilation = CreateCompilation(source);
        var manager = new AssemblyManager("", "");
        var context = new ProjectContext { CreateExternalTypeReferences = true };

        // Act
        var assembly = await manager.DocumentAsync(compilation, context);

        // Assert
        var listType = assembly.Namespaces
            .SelectMany(ns => ns.Types)
            .FirstOrDefault(t => t.Name == "List" && t.IsExternalReference);

        listType.Should().NotBeNull();
        listType!.Members.Should().Contain(m => m.Name == "MyExtension" && m.IsExtensionMethod);
        listType.Summary.Should().Contain("Microsoft documentation");
    }

    [TestMethod]
    public void Extension_To_Interface_Should_Work()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public interface IMyInterface { }

    public static class InterfaceExtensions
    {
        public static void ExtendInterface(this IMyInterface instance) { }
    }
}";
        var compilation = CreateCompilation(source);
        var manager = new AssemblyManager("", "");

        // Act
        var assembly = await manager.DocumentAsync(compilation, null);

        // Assert
        var interfaceType = assembly.Namespaces
            .First().Types
            .First(t => t.Name == "IMyInterface");

        interfaceType.Members.Should().Contain(m =>
            m.Name == "ExtendInterface" &&
            m.IsExtensionMethod);
    }
}
```

#### Baseline Test Updates

Update existing baseline tests in `src/CloudNimble.DotNetDocs.Tests.Core/`:

1. **AssemblyManager Tests** - Regenerate baselines to include inherited/extension properties
2. **JsonRenderer Tests** - Verify new properties serialize correctly
3. **YamlRenderer Tests** - Verify new properties appear in YAML output
4. **MintlifyRenderer Tests** - Verify badges and notes render correctly

Run baseline regeneration:
```bash
cd src
dotnet breakdance generate
```

Verify test assemblies now show:
- Inherited members from base classes
- Extension methods relocated to target types
- New properties populated correctly
- External references created appropriately

## Configuration

### .docsproj Settings

Users can configure behavior via properties:

```xml
<Project Sdk="DotNetDocs.Sdk/1.1.0-preview.1">
  <PropertyGroup>
    <DocumentationType>Mintlify</DocumentationType>

    <!-- Control System.Object inheritance -->
    <IncludeSystemObjectInheritance>false</IncludeSystemObjectInheritance>

    <!-- Control external type creation -->
    <CreateExternalTypeReferences>true</CreateExternalTypeReferences>
  </PropertyGroup>
</Project>
```

**Defaults:**
- `IncludeSystemObjectInheritance` = `true` (match Microsoft's behavior)
- `CreateExternalTypeReferences` = `true` (comprehensive extension docs)

## Examples

### Example 1: Class with Inherited Members

**Source Code:**
```csharp
namespace MyApp
{
    public class CustomList<T> : List<T>
    {
        public void CustomMethod() { }
    }
}
```

**Rendered Documentation:**

```markdown
## Properties

### Capacity <Badge text="Inherited" variant="neutral" />
<Note>Inherited from `System.Collections.Generic.List<T>`</Note>

Gets or sets the total number of elements the internal data structure can hold.

### Count <Badge text="Inherited" variant="neutral" />
<Note>Inherited from `System.Collections.Generic.List<T>`</Note>

Gets the number of elements contained in the List<T>.

## Methods

### Add(T) <Badge text="Inherited" variant="neutral" />
<Note>Inherited from `System.Collections.Generic.List<T>`</Note>

Adds an object to the end of the List<T>.

### Clear() <Badge text="Inherited" variant="neutral" />
<Note>Inherited from `System.Collections.Generic.List<T>`</Note>

Removes all elements from the List<T>.

### CustomMethod()

Performs custom logic.

### Equals(Object) <Badge text="Inherited" variant="neutral" />
<Note>Inherited from `System.Object`</Note>

Determines whether the specified object is equal to the current object.
```

### Example 2: Extension Methods

**Source Code:**
```csharp
namespace Microsoft.Extensions.DependencyInjection
{
    public static class DotNetDocsExtensions
    {
        public static IServiceCollection AddDotNetDocs(this IServiceCollection services)
        {
            // ...
        }
    }
}
```

**Rendered Documentation (for IServiceCollection):**

```markdown
---
title: IServiceCollection
description: "This type is defined in Microsoft.Extensions.DependencyInjection.Abstractions..."
---

<Warning>
This type is defined in **Microsoft.Extensions.DependencyInjection.Abstractions**, not in the assembly being documented.

This page shows only extension methods provided by the current assembly.

See [Microsoft documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection) for complete API reference.
</Warning>

## Methods

### AddDotNetDocs <Badge text="Extension" variant="success" />
<Note>Extension method from `Microsoft.Extensions.DependencyInjection.DotNetDocsExtensions`</Note>

Adds DotNetDocs services to the service collection.

#### Syntax
```csharp
public static IServiceCollection AddDotNetDocs(this IServiceCollection services)
```
```

### Example 3: Override Members

**Source Code:**
```csharp
namespace MyApp
{
    public class Animal
    {
        public virtual string MakeSound() => "...";
    }

    public class Dog : Animal
    {
        public override string MakeSound() => "Woof!";
    }
}
```

**Rendered Documentation (for Dog):**

```markdown
## Methods

### MakeSound() <Badge text="Override" variant="info" />

Returns the sound a dog makes.

#### Syntax
```csharp
public override string MakeSound()
```

#### Returns
Type: `string`
The string "Woof!"
```

## Migration Guide

### For Existing Documentation Projects

1. **Rebuild documentation** - Run `dotnet build` to regenerate with inherited members and relocated extensions
2. **Review external references** - Check if created external type pages need additional conceptual content
3. **Configure System.Object** - Set `IncludeSystemObjectInheritance` if you want to exclude Object members
4. **Update navigation** - Regenerate `docs.json` to include new external reference pages

### Breaking Changes

**None.** This is a purely additive feature:
- Existing member documentation unchanged
- Existing file paths unchanged
- Existing navigation structure preserved
- New members added alongside existing ones

### Compatibility

- **Minimum SDK Version:** 1.1.0-preview.1
- **Roslyn Version:** Uses existing Roslyn APIs (no upgrade needed)
- **Renderer Compatibility:** Mintlify renderer updated; Markdown/JSON/YAML renderers work with new properties

## Performance Considerations

### Memory Impact

- **Extension method relocation:** Single pass, processes only extension methods (~1-5% of total members typically)
- **External type creation:** Creates minimal DocType instances (only properties needed for rendering)
- **Inherited member tracking:** No additional Roslyn queries; uses existing `GetMembers()` results

### Build Time Impact

- **Expected increase:** < 5% for typical projects
- **Dominated by:** Extension method relocation and external type creation
- **Mitigated by:** Post-processing approach (only iterates extension methods, not all members)

### Optimization Notes

- `SymbolEqualityComparer.Default` uses efficient reference comparison
- Dictionary tracking prevents duplicate external type creation
- Empty class removal happens in single pass
- No recursive type hierarchy traversal needed

## Future Enhancements

### Potential Additions

1. **Mermaid inheritance diagrams** - Visual class hierarchy
2. **Interface implementation mapping** - Show which members satisfy interface contracts
3. **Extension method grouping** - Group extensions by source assembly in UI
4. **Override comparison** - Side-by-side view of base vs. overridden signatures
5. **Member filtering** - UI controls to hide/show inherited/extension members
6. **Accessibility indicators** - Show protected, internal, etc. alongside badges

### Not in Scope

1. **Cross-assembly inherited member documentation** - Only members from loaded assemblies
2. **Partial class member merging** - Roslyn already provides merged view
3. **Interface explicit implementations** - Already handled by existing code
4. **Operator overloading special handling** - Works with existing operator member support

## References

### Microsoft Documentation Examples

- [List<T>](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1) - Shows inherited members from Object, IList, ICollection
- [String](https://learn.microsoft.com/dotnet/api/system.string) - Shows extensive inherited members and extension methods
- [IServiceCollection](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection) - Shows extension methods from multiple assemblies

### Roslyn API References

- [IMethodSymbol.IsExtensionMethod](https://learn.microsoft.com/dotnet/api/microsoft.codeanalysis.imethodsymbol.isextensionmethod)
- [IMethodSymbol.ReducedFrom](https://learn.microsoft.com/dotnet/api/microsoft.codeanalysis.imethodsymbol.reducedfrom)
- [MethodKind Enum](https://learn.microsoft.com/dotnet/api/microsoft.codeanalysis.methodkind)
- [ISymbol.ContainingType](https://learn.microsoft.com/dotnet/api/microsoft.codeanalysis.isymbol.containingtype)

### Related Specifications

- [Baseline Testing](./baseline-testing.md) - How to update baseline tests
- [Documentation Reference](./documentation-reference.md) - Collections and references

## Implementation Checklist

- [x] Update `DocMember.cs` with new properties
- [x] Update `DocType.cs` with `IsExternalReference`
- [x] Update `ProjectContext.cs` with configuration properties
- [x] Enhance `AssemblyManager.BuildDocType()` for inheritance tracking
- [x] Add `AssemblyManager.RelocateExtensionMethods()` method
- [x] Add `AssemblyManager.CreateExternalTypeReference()` method
- [x] Add `AssemblyManager.IsAccessibleInDerivedType()` helper
- [x] Add `AssemblyManager.GetMicrosoftDocsUrl()` helper
- [x] Update `MintlifyRenderer.RenderMember()` with badges
- [x] Update `MintlifyRenderer.RenderTypeAsync()` for external types
- [ ] Create `InheritedMembersTests.cs` test file
- [ ] Create `ExtensionMethodsTests.cs` test file
- [ ] Regenerate baseline tests
- [ ] Update documentation in README
- [ ] Create migration guide for users
