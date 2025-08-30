using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CloudNimble.DotNetDocs.Core.Configuration;
using Microsoft.CodeAnalysis;

namespace CloudNimble.DotNetDocs.Core.Renderers
{

    /// <summary>
    /// Base class for documentation renderers providing common functionality.
    /// </summary>
    public abstract class RendererBase
    {

        #region Properties

        /// <summary>
        /// Gets the project context for this renderer.
        /// </summary>
        /// <value>The project context containing configuration and settings.</value>
        protected ProjectContext Context { get; }

        /// <summary>
        /// Gets the file naming options for this renderer.
        /// </summary>
        /// <value>The file naming configuration.</value>
        protected FileNamingOptions FileNamingOptions => Context.FileNamingOptions;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RendererBase"/> class.
        /// </summary>
        /// <param name="context">The project context. If null, a default context is created.</param>
        protected RendererBase(ProjectContext? context = null)
        {
            Context = context ?? new ProjectContext();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Gets a safe namespace name for use in file names and display.
        /// </summary>
        /// <param name="ns">The namespace to get the name for.</param>
        /// <returns>A safe namespace name, using "global" for the global namespace.</returns>
        internal string GetSafeNamespaceName(DocNamespace ns)
        {
            return ns.Symbol.IsGlobalNamespace ? "global" : ns.Symbol.ToDisplayString();
        }

        /// <summary>
        /// Gets a safe type name for use in file names, removing invalid characters.
        /// </summary>
        /// <param name="type">The type to get the name for.</param>
        /// <returns>A safe type name with invalid characters replaced.</returns>
        protected string GetSafeTypeName(DocType type)
        {
            // Replace angle brackets (from <Module> and generic types) and other invalid filename characters
            return type.Symbol.Name
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('`', '_')
                .Replace('/', '_')
                .Replace('\\', '_')
                .Replace(':', '_')
                .Replace('*', '_')
                .Replace('?', '_')
                .Replace('"', '_')
                .Replace('|', '_');
        }

        /// <summary>
        /// Gets the file path for a namespace documentation file.
        /// </summary>
        /// <param name="ns">The namespace to get the file path for.</param>
        /// <param name="outputPath">The base output path.</param>
        /// <param name="extension">The file extension (without the dot).</param>
        /// <returns>The full file path for the namespace documentation.</returns>
        protected string GetNamespaceFilePath(DocNamespace ns, string outputPath, string extension)
        {
            var namespaceName = GetSafeNamespaceName(ns);
            var folderPath = Context.GetNamespaceFolderPath(namespaceName);
            
            if (FileNamingOptions.NamespaceMode == NamespaceMode.Folder)
            {
                // Use folder structure with index file
                var fullPath = Path.Combine(outputPath, folderPath);
                return Path.Combine(fullPath, $"index.{extension}");
            }
            else
            {
                // Use flat file structure with separator
                var fileName = $"{namespaceName.Replace('.', FileNamingOptions.NamespaceSeparator)}.{extension}";
                return Path.Combine(outputPath, fileName);
            }
        }

        /// <summary>
        /// Gets the file path for a type documentation file.
        /// </summary>
        /// <param name="type">The type to get the file path for.</param>
        /// <param name="ns">The namespace containing the type.</param>
        /// <param name="outputPath">The base output path.</param>
        /// <param name="extension">The file extension (without the dot).</param>
        /// <returns>The full file path for the type documentation.</returns>
        protected string GetTypeFilePath(DocType type, DocNamespace ns, string outputPath, string extension)
        {
            var namespaceName = GetSafeNamespaceName(ns);
            var typeName = GetSafeTypeName(type);
            var folderPath = Context.GetNamespaceFolderPath(namespaceName);
            
            if (FileNamingOptions.NamespaceMode == NamespaceMode.Folder)
            {
                // Use folder structure based on namespace
                var fullPath = Path.Combine(outputPath, folderPath);
                return Path.Combine(fullPath, $"{typeName}.{extension}");
            }
            else
            {
                // Use flat file structure with separator
                var fileName = $"{namespaceName.Replace('.', FileNamingOptions.NamespaceSeparator)}.{typeName}.{extension}";
                return Path.Combine(outputPath, fileName);
            }
        }

        /// <summary>
        /// Gets a safe file name for a namespace, suitable for use in file systems.
        /// </summary>
        /// <param name="ns">The namespace to get the file name for.</param>
        /// <param name="extension">The file extension (without the dot).</param>
        /// <returns>A safe file name for the namespace.</returns>
        /// <remarks>This method is deprecated. Use GetNamespaceFilePath instead.</remarks>
        internal string GetNamespaceFileName(DocNamespace ns, string extension)
        {
            var namespaceName = GetSafeNamespaceName(ns);
            return $"{namespaceName.Replace('.', FileNamingOptions.NamespaceSeparator)}.{extension}";
        }

        /// <summary>
        /// Gets a safe file name for a type, suitable for use in file systems.
        /// </summary>
        /// <param name="type">The type to get the file name for.</param>
        /// <param name="ns">The namespace containing the type.</param>
        /// <param name="extension">The file extension (without the dot).</param>
        /// <returns>A safe file name for the type.</returns>
        /// <remarks>This method is deprecated. Use GetTypeFilePath instead.</remarks>
        protected string GetTypeFileName(DocType type, DocNamespace ns, string extension)
        {
            var namespaceName = GetSafeNamespaceName(ns);
            var typeName = GetSafeTypeName(type);
            return $"{namespaceName.Replace('.', FileNamingOptions.NamespaceSeparator)}.{typeName}.{extension}";
        }

        /// <summary>
        /// Gets the access modifier string for the given accessibility.
        /// </summary>
        /// <param name="accessibility">The accessibility to convert.</param>
        /// <returns>The access modifier string.</returns>
        protected string GetAccessModifier(Accessibility accessibility)
        {
            return accessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Protected => "protected",
                Accessibility.Internal => "internal",
                Accessibility.ProtectedOrInternal => "protected internal",
                Accessibility.ProtectedAndInternal => "private protected",
                Accessibility.Private => "private",
                _ => ""
            };
        }

        /// <summary>
        /// Gets the member signature for a member symbol.
        /// </summary>
        /// <param name="member">The member to get the signature for.</param>
        /// <returns>The member signature string.</returns>
        protected string GetMemberSignature(DocMember member)
        {
            return member.Symbol switch
            {
                IMethodSymbol method => GetMethodSignature(method),
                IPropertySymbol property => GetPropertySignature(property),
                IFieldSymbol field => GetFieldSignature(field),
                IEventSymbol evt => GetEventSignature(evt),
                _ => member.Symbol.ToDisplayString()
            };
        }

        /// <summary>
        /// Gets the method signature string.
        /// </summary>
        /// <param name="method">The method symbol.</param>
        /// <returns>The method signature.</returns>
        protected string GetMethodSignature(IMethodSymbol method)
        {
            var sb = new StringBuilder();
            
            // Access modifiers
            sb.Append(GetAccessModifier(method.DeclaredAccessibility));
            
            // Modifiers
            if (method.IsStatic) sb.Append(" static");
            if (method.IsVirtual) sb.Append(" virtual");
            if (method.IsOverride) sb.Append(" override");
            if (method.IsAbstract) sb.Append(" abstract");
            if (method.IsAsync) sb.Append(" async");
            
            // Return type and name
            if (method.MethodKind != MethodKind.Constructor)
            {
                sb.Append($" {method.ReturnType.ToDisplayString()}");
            }
            sb.Append($" {method.Name}");
            
            // Type parameters
            if (method.IsGenericMethod)
            {
                sb.Append('<');
                sb.Append(string.Join(", ", method.TypeParameters.Select(t => t.Name)));
                sb.Append('>');
            }
            
            // Parameters
            sb.Append('(');
            sb.Append(string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}")));
            sb.Append(')');
            
            return sb.ToString();
        }

        /// <summary>
        /// Gets the property signature string.
        /// </summary>
        /// <param name="property">The property symbol.</param>
        /// <returns>The property signature.</returns>
        protected string GetPropertySignature(IPropertySymbol property)
        {
            var sb = new StringBuilder();
            
            sb.Append(GetAccessModifier(property.DeclaredAccessibility));
            
            if (property.IsStatic) sb.Append(" static");
            if (property.IsVirtual) sb.Append(" virtual");
            if (property.IsOverride) sb.Append(" override");
            if (property.IsAbstract) sb.Append(" abstract");
            
            sb.Append($" {property.Type.ToDisplayString()} {property.Name}");
            
            sb.Append(" { ");
            if (property.GetMethod is not null)
            {
                if (property.GetMethod.DeclaredAccessibility != property.DeclaredAccessibility)
                {
                    sb.Append($"{GetAccessModifier(property.GetMethod.DeclaredAccessibility).ToLowerInvariant()} ");
                }
                sb.Append("get; ");
            }
            if (property.SetMethod is not null)
            {
                if (property.SetMethod.DeclaredAccessibility != property.DeclaredAccessibility)
                {
                    sb.Append($"{GetAccessModifier(property.SetMethod.DeclaredAccessibility).ToLowerInvariant()} ");
                }
                sb.Append("set; ");
            }
            sb.Append('}');
            
            return sb.ToString();
        }

        /// <summary>
        /// Gets the field signature string.
        /// </summary>
        /// <param name="field">The field symbol.</param>
        /// <returns>The field signature.</returns>
        protected string GetFieldSignature(IFieldSymbol field)
        {
            var sb = new StringBuilder();
            
            sb.Append(GetAccessModifier(field.DeclaredAccessibility));
            
            // Const fields are implicitly static, so don't add "static" for const fields
            if (field.IsStatic && !field.IsConst) sb.Append(" static");
            if (field.IsReadOnly) sb.Append(" readonly");
            if (field.IsConst) sb.Append(" const");
            
            sb.Append($" {field.Type.ToDisplayString()} {field.Name}");
            
            return sb.ToString();
        }

        /// <summary>
        /// Gets the event signature string.
        /// </summary>
        /// <param name="evt">The event symbol.</param>
        /// <returns>The event signature.</returns>
        protected string GetEventSignature(IEventSymbol evt)
        {
            var sb = new StringBuilder();
            
            sb.Append(GetAccessModifier(evt.DeclaredAccessibility));
            
            if (evt.IsStatic) sb.Append(" static");
            if (evt.IsVirtual) sb.Append(" virtual");
            if (evt.IsOverride) sb.Append(" override");
            if (evt.IsAbstract) sb.Append(" abstract");
            
            sb.Append($" event {evt.Type.ToDisplayString()} {evt.Name}");
            
            return sb.ToString();
        }

        /// <summary>
        /// Gets the type signature string.
        /// </summary>
        /// <param name="type">The type to get the signature for.</param>
        /// <returns>The type signature.</returns>
        protected string GetTypeSignature(DocType type)
        {
            var sb = new StringBuilder();
            
            // Access modifiers
            sb.Append(GetAccessModifier(type.Symbol.DeclaredAccessibility));
            
            // Type modifiers
            if (type.Symbol.IsStatic) sb.Append(" static");
            if (type.Symbol.IsAbstract && type.Symbol.TypeKind != TypeKind.Interface) sb.Append(" abstract");
            if (type.Symbol.IsSealed && type.Symbol.TypeKind != TypeKind.Struct) sb.Append(" sealed");
            
            // Type kind
            sb.Append(type.Symbol.TypeKind switch
            {
                TypeKind.Class => " class",
                TypeKind.Interface => " interface",
                TypeKind.Struct => " struct",
                TypeKind.Enum => " enum",
                TypeKind.Delegate => " delegate",
                _ => ""
            });
            
            sb.Append($" {type.Symbol.Name}");
            
            // Type parameters (for named types)
            if (type.Symbol is INamedTypeSymbol namedType && namedType.TypeParameters.Any())
            {
                sb.Append('<');
                sb.Append(string.Join(", ", namedType.TypeParameters.Select(t => t.Name)));
                sb.Append('>');
            }
            
            // Base type and interfaces (for classes and structs)
            var hasBaseType = type.Symbol.BaseType != null && 
                             type.Symbol.BaseType.SpecialType != SpecialType.System_Object &&
                             type.Symbol.BaseType.SpecialType != SpecialType.System_ValueType &&
                             type.Symbol.TypeKind == TypeKind.Class;
            
            var interfaces = type.Symbol.AllInterfaces;
            
            if (hasBaseType || interfaces.Any())
            {
                sb.Append(" : ");
                var inheritance = new List<string>();
                
                if (hasBaseType)
                {
                    inheritance.Add(type.Symbol.BaseType!.ToDisplayString());
                }
                
                inheritance.AddRange(interfaces.Select(i => i.ToDisplayString()));
                sb.Append(string.Join(", ", inheritance));
            }
            
            return sb.ToString();
        }

        #endregion

    }

}
