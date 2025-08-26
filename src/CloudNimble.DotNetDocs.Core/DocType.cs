using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

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
        /// Gets the base type, if any.
        /// </summary>
        /// <value>Documentation for the base type, or null if none exists.</value>
        public DocType? BaseType { get; set; }

        /// <summary>
        /// Gets the collection of implemented interfaces.
        /// </summary>
        /// <value>List of documented interfaces implemented by this type.</value>
        [NotNull]
        public List<DocType> ImplementedInterfaces { get; } = [];

        /// <summary>
        /// Gets the collection of members (methods, properties, fields, events, etc.).
        /// </summary>
        /// <value>List of documented members within this type.</value>
        [NotNull]
        public List<DocMember> Members { get; } = [];

        /// <summary>
        /// Gets the Roslyn symbol for the type.
        /// </summary>
        /// <value>The underlying Roslyn type symbol containing metadata.</value>
        [NotNull]
        public ITypeSymbol Symbol { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DocType"/> class.
        /// </summary>
        /// <param name="symbol">The Roslyn type symbol.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="symbol"/> is null.</exception>
        public DocType(ITypeSymbol symbol)
        {
            ArgumentNullException.ThrowIfNull(symbol);
            Symbol = symbol;
        }

        #endregion

    }

}