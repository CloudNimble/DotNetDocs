using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using YamlDotNet.Serialization;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Represents documentation for a .NET type.
    /// </summary>
    /// <remarks>
    /// Contains metadata about a type (class, interface, struct, enum, delegate) and its members,
    /// extracted from Roslyn symbols and enhanced with conceptual documentation.
    /// </remarks>
    public class DocType : DocEntity
    {

        #region Properties

        /// <summary>
        /// Gets the base type name, if any.
        /// </summary>
        /// <value>The name of the base type, or null if none exists.</value>
        public string? BaseType { get; set; }

        /// <summary>
        /// Gets the collection of members (methods, properties, fields, events, etc.).
        /// </summary>
        /// <value>List of documented members within this type.</value>
        [NotNull]
        public List<DocMember> Members { get; } = [];

        /// <summary>
        /// Gets or sets the containing assembly name.
        /// </summary>
        /// <value>The name of the assembly containing this type.</value>
        public string? AssemblyName { get; set; }

        /// <summary>
        /// Gets or sets the fully qualified name of the type.
        /// </summary>
        /// <value>The fully qualified type name including namespace.</value>
        public string? FullName { get; set; }

        /// <summary>
        /// Gets or sets the name of the type.
        /// </summary>
        /// <value>The type name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the signature of the type.
        /// </summary>
        /// <value>The type signature including modifiers, inheritance, etc.</value>
        public string? Signature { get; set; }

        /// <summary>
        /// Gets the Roslyn symbol for the type.
        /// </summary>
        /// <value>The underlying Roslyn type symbol containing metadata.</value>
        [NotNull]
        [JsonIgnore]
        [YamlIgnore]
        public ITypeSymbol Symbol { get; }

        /// <summary>
        /// Gets or sets the type kind.
        /// </summary>
        /// <value>The kind of type (Class, Interface, Struct, Enum, Delegate, etc.).</value>
        public TypeKind TypeKind { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DocType"/> class.
        /// </summary>
        /// <param name="symbol">The Roslyn type symbol.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="symbol"/> is null.</exception>
        public DocType(ITypeSymbol symbol) : base(symbol)
        {
            ArgumentNullException.ThrowIfNull(symbol);
            Symbol = symbol;
        }

        #endregion

    }

}