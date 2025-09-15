namespace CloudNimble.DotNetDocs.Core
{

    /// <summary>
    /// Represents a cross-reference in documentation, such as a see or seealso tag.
    /// </summary>
    /// <remarks>
    /// This class encapsulates all information needed to resolve and render a documentation reference,
    /// including the original reference string, resolved display name, relative path, and anchor for
    /// member-level references.
    /// </remarks>
    public class DocReference
    {

        #region Properties

        /// <summary>
        /// Gets or sets the anchor for member-level references.
        /// </summary>
        /// <value>The anchor name for linking to specific members within a type, or null for type-level references.</value>
        /// <example>For a reference to an enum value, this would be "file" for NamespaceMode.File.</example>
        public string? Anchor { get; set; }

        /// <summary>
        /// Gets or sets the display name for the reference.
        /// </summary>
        /// <value>The human-readable name to display for this reference.</value>
        /// <example>For a member reference, this might be "NamespaceMode.File" or just "File" depending on context.</example>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this reference has been successfully resolved.
        /// </summary>
        /// <value>True if the reference was found and resolved; otherwise, false.</value>
        public bool IsResolved { get; set; }

        /// <summary>
        /// Gets or sets the original reference string from the XML documentation.
        /// </summary>
        /// <value>The raw cref value including any prefix (T:, F:, P:, M:, E:).</value>
        /// <example>F:CloudNimble.DotNetDocs.Core.Configuration.NamespaceMode.File</example>
        public string RawReference { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of reference.
        /// </summary>
        /// <value>The classification of this reference.</value>
        public ReferenceType ReferenceType { get; set; }

        /// <summary>
        /// Gets or sets the resolved relative path to the target documentation.
        /// </summary>
        /// <value>The relative path from the current document to the target, or null if unresolved.</value>
        /// <example>../Configuration/NamespaceMode.md</example>
        public string? RelativePath { get; set; }

        /// <summary>
        /// Gets or sets the target entity that this reference points to.
        /// </summary>
        /// <value>The resolved DocEntity, or null if the reference couldn't be resolved.</value>
        public DocEntity? TargetEntity { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DocReference"/> class.
        /// </summary>
        public DocReference()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocReference"/> class with a raw reference.
        /// </summary>
        /// <param name="rawReference">The raw reference string from XML documentation.</param>
        public DocReference(string rawReference)
        {
            RawReference = rawReference ?? string.Empty;
            ReferenceType = DetermineReferenceType(rawReference);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates a Markdown link for this reference.
        /// </summary>
        /// <returns>A Markdown-formatted link, or inline code if the reference is unresolved.</returns>
        public string ToMarkdownLink()
        {
            if (!IsResolved || string.IsNullOrWhiteSpace(RelativePath))
            {
                // Fallback to inline code for unresolved references
                return $"`{DisplayName ?? GetSimpleNameFromReference(RawReference)}`";
            }

            var link = RelativePath;
            if (!string.IsNullOrWhiteSpace(Anchor))
            {
                link += $"#{Anchor}";
            }

            return $"[{DisplayName}]({link})";
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Determines the reference type based on the prefix in the raw reference.
        /// </summary>
        internal static ReferenceType DetermineReferenceType(string? rawReference)
        {
            if (string.IsNullOrWhiteSpace(rawReference))
                return ReferenceType.Unknown;

            if (rawReference.StartsWith("T:"))
                return ReferenceType.Type;
            if (rawReference.StartsWith("F:"))
                return ReferenceType.Field;
            if (rawReference.StartsWith("P:"))
                return ReferenceType.Property;
            if (rawReference.StartsWith("M:"))
                return ReferenceType.Method;
            if (rawReference.StartsWith("E:"))
                return ReferenceType.Event;
            if (rawReference.StartsWith("N:"))
                return ReferenceType.Namespace;

            // Check if it's a URL
            if (rawReference.StartsWith("http://") || rawReference.StartsWith("https://"))
                return ReferenceType.External;

            return ReferenceType.Unknown;
        }

        /// <summary>
        /// Extracts a simple name from a reference string.
        /// </summary>
        internal static string GetSimpleNameFromReference(string reference)
        {
            // Remove prefix if present
            if (reference.Contains(':'))
            {
                reference = reference.Substring(reference.IndexOf(':') + 1);
            }

            // Get the last part after the final dot
            var lastDot = reference.LastIndexOf('.');
            if (lastDot >= 0)
            {
                return reference.Substring(lastDot + 1);
            }

            return reference;
        }

        #endregion

    }

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