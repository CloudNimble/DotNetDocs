using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;

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
        /// Gets the member kind (method, property, field, event, etc.).
        /// </summary>
        /// <value>The kind of member as defined by Roslyn.</value>
        public SymbolKind Kind => Symbol.Kind;

        /// <summary>
        /// Gets the name of the member.
        /// </summary>
        /// <value>The member name.</value>
        public string Name => Symbol.Name;

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
        /// Gets the Roslyn symbol for the member.
        /// </summary>
        /// <value>The underlying Roslyn symbol containing metadata.</value>
        [NotNull]
        [JsonIgnore]
        public ISymbol Symbol { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DocMember"/> class.
        /// </summary>
        /// <param name="symbol">The Roslyn member symbol.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="symbol"/> is null.</exception>
        public DocMember(ISymbol symbol)
        {
            ArgumentNullException.ThrowIfNull(symbol);
            Symbol = symbol;
        }

        #endregion

    }

}