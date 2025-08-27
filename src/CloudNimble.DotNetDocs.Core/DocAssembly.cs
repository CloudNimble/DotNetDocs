using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Represents documentation for a .NET assembly.
    /// </summary>
    /// <remarks>
    /// Contains metadata about an assembly and its namespaces, extracted from Roslyn symbols and enhanced
    /// with conceptual documentation.
    /// </remarks>
    public class DocAssembly : DocEntity
    {

        #region Properties

        /// <summary>
        /// Gets the name of the assembly.
        /// </summary>
        /// <value>The assembly name.</value>
        public string AssemblyName => Symbol.Name;

        /// <summary>
        /// Gets the collection of namespaces in the assembly.
        /// </summary>
        /// <value>List of documented namespaces within this assembly.</value>
        [NotNull]
        public List<DocNamespace> Namespaces { get; } = [];

        /// <summary>
        /// Gets the Roslyn symbol for the assembly.
        /// </summary>
        /// <value>The underlying Roslyn assembly symbol containing metadata.</value>
        [NotNull]
        [JsonIgnore]
        public IAssemblySymbol Symbol { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DocAssembly"/> class.
        /// </summary>
        /// <param name="symbol">The Roslyn assembly symbol.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="symbol"/> is null.</exception>
        public DocAssembly(IAssemblySymbol symbol)
        {
            ArgumentNullException.ThrowIfNull(symbol);
            Symbol = symbol;
        }

        #endregion

    }

}