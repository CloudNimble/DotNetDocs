using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Represents documentation for a method or constructor parameter.
    /// </summary>
    /// <remarks>
    /// Contains metadata about a parameter, extracted from Roslyn symbols and enhanced
    /// with conceptual documentation.
    /// </remarks>
    public class DocParameter : DocEntity
    {

        #region Properties

        /// <summary>
        /// Gets the default value of the parameter, if any.
        /// </summary>
        /// <value>The default value as a string, or null if no default value exists.</value>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Gets a value indicating whether this parameter has a default value.
        /// </summary>
        /// <value>True if the parameter has a default value; otherwise, false.</value>
        public bool HasDefaultValue => Symbol.HasExplicitDefaultValue;

        /// <summary>
        /// Gets a value indicating whether this parameter is optional.
        /// </summary>
        /// <value>True if the parameter is optional; otherwise, false.</value>
        public bool IsOptional => Symbol.IsOptional;

        /// <summary>
        /// Gets a value indicating whether this parameter uses the params keyword.
        /// </summary>
        /// <value>True if the parameter is a params array; otherwise, false.</value>
        public bool IsParams => Symbol.IsParams;

        /// <summary>
        /// Gets the parameter type documentation.
        /// </summary>
        /// <value>Documentation for the parameter's type.</value>
        public DocType? ParameterType { get; set; }

        /// <summary>
        /// Gets the Roslyn symbol for the parameter.
        /// </summary>
        /// <value>The underlying Roslyn parameter symbol containing metadata.</value>
        [NotNull]
        public IParameterSymbol Symbol { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DocParameter"/> class.
        /// </summary>
        /// <param name="symbol">The Roslyn parameter symbol.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="symbol"/> is null.</exception>
        public DocParameter(IParameterSymbol symbol)
        {
            ArgumentNullException.ThrowIfNull(symbol);
            Symbol = symbol;

            if (symbol.HasExplicitDefaultValue)
            {
                DefaultValue = symbol.ExplicitDefaultValue?.ToString();
            }
        }

        #endregion

    }

}