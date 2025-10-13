using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using YamlDotNet.Serialization;

namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Represents documentation for a .NET enum type.
    /// </summary>
    /// <remarks>
    /// Contains metadata about an enum type and its values, extracted from Roslyn symbols
    /// and enhanced with conceptual documentation. Inherits from <see cref="DocType"/> but
    /// uses a specialized structure for enum values instead of the Members collection.
    /// </remarks>
    public class DocEnum : DocType
    {

        #region Properties

        /// <summary>
        /// Gets whether this enum has the Flags attribute.
        /// </summary>
        /// <value>True if the enum is decorated with [Flags]; otherwise, false.</value>
        public bool IsFlags { get; set; }

        /// <summary>
        /// Gets or sets the underlying type of the enum as a <see cref="DocReference"/>.
        /// </summary>
        /// <value>
        /// A reference to the underlying type (e.g., System.Int32, System.Byte) that can be
        /// resolved and linked in documentation.
        /// </value>
        [NotNull]
        public DocReference UnderlyingType { get; set; } = new DocReference
        {
            RawReference = "T:System.Int32",
            DisplayName = "int",
            IsResolved = true,
            ReferenceType = ReferenceType.Framework
        };

        /// <summary>
        /// Gets the collection of enum values with their documentation.
        /// </summary>
        /// <value>
        /// A list of documented enum values, each containing the name, numeric value,
        /// and associated documentation.
        /// </value>
        [NotNull]
        public List<DocEnumValue> Values { get; } = [];

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DocEnum"/> class.
        /// </summary>
        /// <param name="symbol">The Roslyn type symbol representing the enum.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="symbol"/> is null.</exception>
        public DocEnum(ITypeSymbol symbol) : base(symbol)
        {
            ArgumentNullException.ThrowIfNull(symbol);

            // Clear the Members collection as we use Values instead for enums
            Members.Clear();
        }

        #endregion

    }

    /// <summary>
    /// Represents a single value within an enum type.
    /// </summary>
    /// <remarks>
    /// Inherits from <see cref="DocEntity"/> to provide standard documentation properties
    /// while adding enum-specific properties like the numeric value.
    /// </remarks>
    public class DocEnumValue : DocEntity
    {

        #region Properties

        /// <summary>
        /// Gets or sets the name of the enum value.
        /// </summary>
        /// <value>The identifier name of the enum member.</value>
        [NotNull]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the numeric value of the enum member.
        /// </summary>
        /// <value>
        /// The numeric value as a string to preserve formatting (e.g., "0x10" for hex values).
        /// May be null if the value is implicitly assigned.
        /// </value>
        public string? NumericValue { get; set; }

        /// <summary>
        /// Gets the Roslyn symbol for the enum field.
        /// </summary>
        /// <value>The underlying Roslyn field symbol containing metadata.</value>
        [JsonIgnore]
        [YamlIgnore]
        public IFieldSymbol? Symbol { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DocEnumValue"/> class.
        /// </summary>
        /// <param name="symbol">The Roslyn field symbol representing the enum value.</param>
        public DocEnumValue(IFieldSymbol symbol) : base(symbol)
        {
            Symbol = symbol;
            Name = symbol.Name;
        }

        #endregion

    }

}