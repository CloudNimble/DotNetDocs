namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Specifies the type of documentation reference.
    /// </summary>
    public enum ReferenceType
    {

        /// <summary>
        /// Unknown or unrecognized reference type.
        /// </summary>
        Unknown,

        /// <summary>
        /// Reference to a type (class, interface, struct, enum, delegate).
        /// </summary>
        Type,

        /// <summary>
        /// Reference to a field or enum member.
        /// </summary>
        Field,

        /// <summary>
        /// Reference to a property.
        /// </summary>
        Property,

        /// <summary>
        /// Reference to a method.
        /// </summary>
        Method,

        /// <summary>
        /// Reference to an event.
        /// </summary>
        Event,

        /// <summary>
        /// Reference to a namespace.
        /// </summary>
        Namespace,

        /// <summary>
        /// External reference (URL).
        /// </summary>
        External,

        /// <summary>
        /// Reference to a .NET Framework type.
        /// </summary>
        Framework

    }

}