using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using YamlDotNet.Serialization;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Represents documentation for a .NET member (method, property, field, event, etc.).
    /// </summary>
    /// <remarks>
    /// Contains metadata about a type member, extracted from Roslyn symbols and enhanced
    /// with conceptual documentation.
    /// </remarks>
    public class DocMember : DocEntity
    {

        #region Properties

        /// <summary>
        /// Gets or sets the accessibility level of the member.
        /// </summary>
        /// <value>The accessibility level (public, private, protected, etc.).</value>
        public Accessibility Accessibility { get; set; }

        /// <summary>
        /// Gets or sets the member kind (method, property, field, event, etc.).
        /// </summary>
        /// <value>The kind of member as defined by Roslyn.</value>
        public SymbolKind MemberKind { get; set; }

        /// <summary>
        /// Gets or sets the method kind for method members.
        /// </summary>
        /// <value>The kind of method (Constructor, Ordinary, etc.), or null for non-method members.</value>
        public MethodKind? MethodKind { get; set; }

        /// <summary>
        /// Gets or sets the name of the member.
        /// </summary>
        /// <value>The member name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets the collection of parameters for this member.
        /// </summary>
        /// <value>List of documented parameters. Empty for members without parameters.</value>
        /// <remarks>
        /// This collection is populated for methods, constructors, delegates, and indexers.
        /// </remarks>
        [NotNull]
        public List<DocParameter> Parameters { get; init; } = [];

        /// <summary>
        /// Gets the return type documentation, if applicable.
        /// </summary>
        /// <value>Documentation for the return type, or null for void methods and non-method members.</value>
        public DocType? ReturnType { get; set; }

        /// <summary>
        /// Gets or sets the name of the return type.
        /// </summary>
        /// <value>The return type name for methods and properties, or null for other members.</value>
        public string? ReturnTypeName { get; set; }

        /// <summary>
        /// Gets or sets the signature of the member.
        /// </summary>
        /// <value>The member signature including modifiers, return type, parameters, etc.</value>
        public string? Signature { get; set; }

        /// <summary>
        /// Gets the Roslyn symbol for the member.
        /// </summary>
        /// <value>The underlying Roslyn symbol containing metadata.</value>
        [NotNull]
        [JsonIgnore]
        [YamlIgnore]
        public ISymbol Symbol { get; }

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

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DocMember"/> class.
        /// </summary>
        /// <remarks>
        /// This parameterless constructor is provided for deserialization purposes only.
        /// Use <see cref="DocMember(ISymbol)"/> for normal instantiation.
        /// </remarks>
        [Obsolete("This constructor is for deserialization only. Use DocMember(ISymbol) instead.", error: true)]
        [JsonConstructor]
        protected DocMember() : base()
        {
            Symbol = null!;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocMember"/> class.
        /// </summary>
        /// <param name="symbol">The Roslyn member symbol.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="symbol"/> is null.</exception>
        public DocMember(ISymbol symbol) : base(symbol)
        {
            ArgumentNullException.ThrowIfNull(symbol);
            Symbol = symbol;
        }

        #endregion

    }

}