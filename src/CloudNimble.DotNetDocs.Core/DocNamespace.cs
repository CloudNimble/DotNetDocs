using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using YamlDotNet.Serialization;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Represents documentation for a .NET namespace.
    /// </summary>
    /// <remarks>
    /// Contains metadata about a namespace and its types, extracted from Roslyn symbols and enhanced
    /// with conceptual documentation.
    /// </remarks>
    public class DocNamespace : DocEntity
    {

        #region Properties

        /// <summary>
        /// Gets or sets the name of the namespace.
        /// </summary>
        /// <value>The namespace name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets the Roslyn symbol for the namespace.
        /// </summary>
        /// <value>The underlying Roslyn namespace symbol containing metadata.</value>
        [NotNull]
        [JsonIgnore]
        [YamlIgnore]
        public INamespaceSymbol Symbol { get; }

        /// <summary>
        /// Gets the collection of types in the namespace.
        /// </summary>
        /// <value>List of documented types within this namespace.</value>
        [NotNull]
        public List<DocType> Types { get; } = [];

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DocNamespace"/> class.
        /// </summary>
        /// <param name="symbol">The Roslyn namespace symbol.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="symbol"/> is null.</exception>
        public DocNamespace(INamespaceSymbol symbol) : base(symbol)
        {
            ArgumentNullException.ThrowIfNull(symbol);
            Symbol = symbol;
        }

        #endregion

    }

}