using CloudNimble.DotNetDocs.Core;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace CloudNimble.DotNetDocs.Mintlify;

/// <summary>
/// Provides FontAwesome 7 icon mappings for different .NET documentation entities in Mintlify.
/// </summary>
public static class MintlifyIcons
{

    #region Fields

    /// <summary>
    /// FontAwesome icon for assembly documentation.
    /// </summary>
    public const string Assembly = "cubes";
    
    /// <summary>
    /// FontAwesome icon for namespace documentation.
    /// </summary>
    public const string Namespace = "folder-tree";

    /// <summary>
    /// FontAwesome icon for class documentation.
    /// </summary>
    public const string Class = "file-brackets-curly";
    
    /// <summary>
    /// FontAwesome icon for interface documentation.
    /// </summary>
    public const string Interface = "plug";
    
    /// <summary>
    /// FontAwesome icon for struct documentation.
    /// </summary>
    public const string Struct = "cubes";
    
    /// <summary>
    /// FontAwesome icon for enum documentation.
    /// </summary>
    public const string Enum = "list-ol";
    
    /// <summary>
    /// FontAwesome icon for delegate documentation.
    /// </summary>
    public const string Delegate = "arrow-right-arrow-left";
    
    /// <summary>
    /// FontAwesome icon for record documentation.
    /// </summary>
    public const string Record = "database";

    /// <summary>
    /// FontAwesome icon for abstract class documentation.
    /// </summary>
    public const string AbstractClass = "shapes";
    
    /// <summary>
    /// FontAwesome icon for sealed class documentation.
    /// </summary>
    public const string SealedClass = "lock";
    
    /// <summary>
    /// FontAwesome icon for static class documentation.
    /// </summary>
    public const string StaticClass = "bolt";
    
    /// <summary>
    /// FontAwesome icon for generic type documentation.
    /// </summary>
    public const string GenericType = "code-branch";

    /// <summary>
    /// FontAwesome icon for constructor documentation.
    /// </summary>
    public const string Constructor = "hammer";
    
    /// <summary>
    /// FontAwesome icon for method documentation.
    /// </summary>
    public const string Method = "function";
    
    /// <summary>
    /// FontAwesome icon for property documentation.
    /// </summary>
    public const string Property = "tag";
    
    /// <summary>
    /// FontAwesome icon for field documentation.
    /// </summary>
    public const string Field = "box";
    
    /// <summary>
    /// FontAwesome icon for event documentation.
    /// </summary>
    public const string Event = "bell";
    
    /// <summary>
    /// FontAwesome icon for constant field documentation.
    /// </summary>
    public const string Constant = "anchor";
    
    /// <summary>
    /// FontAwesome icon for indexer documentation.
    /// </summary>
    public const string Indexer = "table-cells";
    
    /// <summary>
    /// FontAwesome icon for operator documentation.
    /// </summary>
    public const string Operator = "calculator";

    /// <summary>
    /// FontAwesome icon for async method documentation.
    /// </summary>
    public const string AsyncMethod = "rotate";
    
    /// <summary>
    /// FontAwesome icon for extension method documentation.
    /// </summary>
    public const string ExtensionMethod = "puzzle-piece";
    
    /// <summary>
    /// FontAwesome icon for virtual method documentation.
    /// </summary>
    public const string VirtualMethod = "code-fork";
    
    /// <summary>
    /// FontAwesome icon for override method documentation.
    /// </summary>
    public const string OverrideMethod = "code-merge";
    
    /// <summary>
    /// FontAwesome icon for static member documentation.
    /// </summary>
    public const string StaticMember = "thumbtack";

    /// <summary>
    /// FontAwesome icon for public access modifier.
    /// </summary>
    public const string Public = "globe";
    
    /// <summary>
    /// FontAwesome icon for protected access modifier.
    /// </summary>
    public const string Protected = "shield";
    
    /// <summary>
    /// FontAwesome icon for internal access modifier.
    /// </summary>
    public const string Internal = "building";
    
    /// <summary>
    /// FontAwesome icon for private access modifier.
    /// </summary>
    public const string Private = "lock";
    
    /// <summary>
    /// FontAwesome icon for protected internal access modifier.
    /// </summary>
    public const string ProtectedInternal = "shield-halved";

    /// <summary>
    /// FontAwesome icon for empty namespace documentation.
    /// </summary>
    public const string Namespace_Empty = "folder-open";
    
    /// <summary>
    /// FontAwesome icon for populated namespace documentation.
    /// </summary>
    public const string Namespace_Populated = "folder-tree";
    
    /// <summary>
    /// FontAwesome icon for example documentation sections.
    /// </summary>
    public const string Example = "brackets-curly";
    
    /// <summary>
    /// FontAwesome icon for note documentation sections.
    /// </summary>
    public const string Note = "circle-info";
    
    /// <summary>
    /// FontAwesome icon for warning documentation sections.
    /// </summary>
    public const string Warning = "triangle-exclamation";
    
    /// <summary>
    /// FontAwesome icon for error documentation sections.
    /// </summary>
    public const string Error = "circle-xmark";
    
    /// <summary>
    /// FontAwesome icon for reference documentation.
    /// </summary>
    public const string Reference = "link";
    
    /// <summary>
    /// FontAwesome icon for package documentation.
    /// </summary>
    public const string Package = "box-isometric";

    /// <summary>
    /// Default FontAwesome icon for miscellaneous documentation.
    /// </summary>
    public const string Globe = "globe";

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the appropriate icon for a DocAssembly.
    /// </summary>
    /// <param name="assembly">The documentation assembly.</param>
    /// <returns>The FontAwesome icon name.</returns>
    public static string GetIconForAssembly(DocAssembly assembly)
    {
        return Assembly;
    }

    /// <summary>
    /// Gets the appropriate icon for a DocEntity based on its type.
    /// </summary>
    /// <param name="entity">The documentation entity.</param>
    /// <returns>The FontAwesome icon name.</returns>
    public static string GetIconForEntity(DocEntity entity)
    {
        return entity switch
        {
            DocAssembly => Assembly,
            DocNamespace ns => GetIconForNamespace(ns),
            DocType type => GetIconForType(type),
            DocMember member => GetIconForMember(member),
            _ => Globe
        };
    }

    /// <summary>
    /// Gets the icon for a DocMember based on its kind and characteristics.
    /// </summary>
    /// <param name="member">The documentation member.</param>
    /// <returns>The FontAwesome icon name.</returns>
    public static string GetIconForMember(DocMember member)
    {
        // Check for constructor
        if (member.MethodKind == MethodKind.Constructor)
            return Constructor;

        // Check member kind
        return member.MemberKind switch
        {
            SymbolKind.Method => GetIconForMethod(member),
            SymbolKind.Property => Property,
            SymbolKind.Field => member.Symbol is IFieldSymbol fieldSymbol && fieldSymbol.IsConst ? Constant : Field,
            SymbolKind.Event => Event,
            _ => Method
        };
    }

    /// <summary>
    /// Gets the icon for a DocMember method, considering special characteristics.
    /// </summary>
    /// <param name="docMember">The documentation member (should be a method).</param>
    /// <returns>The FontAwesome icon name.</returns>
    public static string GetIconForMethod(DocMember docMember)
    {
        // For methods, check the Symbol properties
        if (docMember.Symbol is IMethodSymbol methodSymbol)
        {
            if (methodSymbol.IsExtensionMethod)
                return ExtensionMethod;

            if (methodSymbol.IsOverride)
                return OverrideMethod;

            if (methodSymbol.IsVirtual)
                return VirtualMethod;

            if (methodSymbol.IsStatic)
                return StaticMember;

            if (methodSymbol.IsAsync)
                return AsyncMethod;
        }

        return Method;
    }

    /// <summary>
    /// Gets the icon for a DocNamespace based on whether it has types.
    /// </summary>
    /// <param name="ns">The documentation namespace.</param>
    /// <returns>The FontAwesome icon name.</returns>
    public static string GetIconForNamespace(DocNamespace ns)
    {
        return ns.Types.Any() ? Namespace_Populated : Namespace_Empty;
    }

    /// <summary>
    /// Gets the icon for a DocType based on its kind and characteristics.
    /// </summary>
    /// <param name="docType">The documentation type.</param>
    /// <returns>The FontAwesome icon name.</returns>
    public static string GetIconForType(DocType docType)
    {
        // Check type kind first for non-class types
        if (docType.TypeKind != TypeKind.Class)
        {
            return docType.TypeKind switch
            {
                TypeKind.Interface => Interface,
                TypeKind.Struct => Struct,
                TypeKind.Enum => Enum,
                TypeKind.Delegate => Delegate,
                _ => Class
            };
        }

        // For classes, check for special characteristics
        // Check for generic types
        if (docType.Symbol is INamedTypeSymbol namedType && namedType.IsGenericType)
            return GenericType;

        // Check for static class
        if (docType.Symbol.IsStatic)
            return StaticClass;

        // Check for abstract class
        if (docType.Symbol.IsAbstract)
            return AbstractClass;

        // Check for sealed class
        if (docType.Symbol.IsSealed && !docType.Symbol.IsStatic)
            return SealedClass;

        return Class;
    }

    /// <summary>
    /// Gets the icon for an access modifier.
    /// </summary>
    /// <param name="accessModifier">The access modifier string.</param>
    /// <returns>The FontAwesome icon name.</returns>
    public static string GetIconForAccessModifier(string accessModifier)
    {
        return accessModifier?.ToLowerInvariant() switch
        {
            "public" => Public,
            "protected" => Protected,
            "internal" => Internal,
            "private" => Private,
            "protected internal" => ProtectedInternal,
            "private protected" => Private,
            _ => Globe
        };
    }

    /// <summary>
    /// Gets the icon for an Accessibility enum value.
    /// </summary>
    /// <param name="accessibility">The accessibility level.</param>
    /// <returns>The FontAwesome icon name.</returns>
    public static string GetIconForAccessibility(Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.Public => Public,
            Accessibility.Protected => Protected,
            Accessibility.Internal => Internal,
            Accessibility.Private => Private,
            Accessibility.ProtectedAndInternal => Protected,
            Accessibility.ProtectedOrInternal => ProtectedInternal,
            _ => Globe
        };
    }

    /// <summary>
    /// Gets an icon based on the namespace segment for organization-specific icons.
    /// </summary>
    /// <param name="namespaceName">The full namespace name.</param>
    /// <returns>The FontAwesome icon name based on common patterns.</returns>
    public static string GetIconForNamespaceSegment(string namespaceName)
    {
        if (string.IsNullOrWhiteSpace(namespaceName))
            return Namespace;

        var lastSegment = namespaceName.Split('.').Last().ToLowerInvariant();

        return lastSegment switch
        {
            "models" => "database",
            "services" => "server",
            "controllers" => "gamepad",
            "views" => "eye",
            "viewmodels" => "layer-group",
            "utilities" or "helpers" or "utils" => "wrench",
            "interfaces" => "plug",
            "extensions" => "puzzle-piece",
            "configuration" or "config" => "gears",
            "data" => "database",
            "entities" => "cubes",
            "exceptions" => "triangle-exclamation",
            "handlers" => "hand",
            "factories" => "industry",
            "builders" => "hammer",
            "validators" or "validation" => "check-circle",
            "attributes" => "tag",
            "enums" or "enumerations" => "list-ol",
            "constants" => "anchor",
            "tests" or "test" => "flask",
            "shared" or "common" => "share-nodes",
            "core" => "circle",
            "api" => "plug",
            "web" => "globe",
            "infrastructure" => "building",
            _ => Namespace
        };
    }

    /// <summary>
    /// Gets an appropriate icon based on common type naming patterns.
    /// </summary>
    /// <param name="typeName">The type name.</param>
    /// <returns>A FontAwesome icon based on the type name pattern.</returns>
    public static string GetIconForTypeByName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return Class;

        var lowerName = typeName.ToLowerInvariant();

        // Check for interface pattern
        if (typeName.StartsWith("I") && typeName.Length > 1 && char.IsUpper(typeName[1]))
            return Interface;

        // Check for common suffixes
        if (lowerName.EndsWith("exception"))
            return "triangle-exclamation";
        if (lowerName.EndsWith("attribute"))
            return "tag";
        if (lowerName.EndsWith("handler"))
            return "hand";
        if (lowerName.EndsWith("factory"))
            return "industry";
        if (lowerName.EndsWith("builder"))
            return "hammer";
        if (lowerName.EndsWith("validator"))
            return "check-circle";
        if (lowerName.EndsWith("service"))
            return "server";
        if (lowerName.EndsWith("controller"))
            return "gamepad";
        if (lowerName.EndsWith("model") || lowerName.EndsWith("entity"))
            return "database";
        if (lowerName.EndsWith("dto"))
            return "exchange";
        if (lowerName.EndsWith("viewmodel"))
            return "layer-group";
        if (lowerName.EndsWith("helper") || lowerName.EndsWith("utility"))
            return "wrench";
        if (lowerName.EndsWith("manager"))
            return "briefcase";
        if (lowerName.EndsWith("provider"))
            return "plug";

        return Class;
    }

    #endregion

}