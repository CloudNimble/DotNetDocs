using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace CloudNimble.DotNetDocs.Core.Renderers
{

    /// <summary>
    /// Base class for documentation renderers providing common functionality.
    /// </summary>
    public abstract class RendererBase
    {

        #region Protected Methods

        /// <summary>
        /// Gets a safe namespace name for use in file names and display.
        /// </summary>
        /// <param name="ns">The namespace to get the name for.</param>
        /// <returns>A safe namespace name, using "global" for the global namespace.</returns>
        protected string GetSafeNamespaceName(DocNamespace ns)
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
        /// Gets a safe file name for a namespace, suitable for use in file systems.
        /// </summary>
        /// <param name="ns">The namespace to get the file name for.</param>
        /// <param name="extension">The file extension (without the dot).</param>
        /// <returns>A safe file name for the namespace.</returns>
        protected string GetNamespaceFileName(DocNamespace ns, string extension)
        {
            var namespaceName = GetSafeNamespaceName(ns);
            return $"{namespaceName.Replace('.', '-')}.{extension}";
        }

        /// <summary>
        /// Gets a safe file name for a type, suitable for use in file systems.
        /// </summary>
        /// <param name="type">The type to get the file name for.</param>
        /// <param name="ns">The namespace containing the type.</param>
        /// <param name="extension">The file extension (without the dot).</param>
        /// <returns>A safe file name for the type.</returns>
        protected string GetTypeFileName(DocType type, DocNamespace ns, string extension)
        {
            var namespaceName = GetSafeNamespaceName(ns);
            var typeName = GetSafeTypeName(type);
            return $"{namespaceName.Replace('.', '-')}.{typeName}.{extension}";
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
            sb.Append("}");
            
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
            
            if (field.IsStatic) sb.Append(" static");
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

        #endregion

    }

}
