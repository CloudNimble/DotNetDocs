namespace CloudNimble.DotNetDocs.Tests.Shared.AccessModifiers
{

    /// <summary>
    /// A class with members of various access modifiers for testing filtering.
    /// </summary>
    /// <remarks>
    /// This class tests the IncludedMembers filtering functionality.
    /// </remarks>
    public class MixedAccessClass
    {

        #region Fields

        /// <summary>
        /// A public field.
        /// </summary>
        public string PublicField = "Public";

        /// <summary>
        /// An internal field.
        /// </summary>
        internal string InternalField = "Internal";

        /// <summary>
        /// A protected field.
        /// </summary>
        protected string ProtectedField = "Protected";

        /// <summary>
        /// A private field.
        /// </summary>
        #pragma warning disable CS0414 // The field is assigned but its value is never used
        private string PrivateField = "Private";
        #pragma warning restore CS0414

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the public property.
        /// </summary>
        public string PublicProperty { get; set; } = "Public";

        /// <summary>
        /// Gets or sets the internal property.
        /// </summary>
        internal string InternalProperty { get; set; } = "Internal";

        /// <summary>
        /// Gets or sets the protected property.
        /// </summary>
        protected string ProtectedProperty { get; set; } = "Protected";

        /// <summary>
        /// Gets or sets the private property.
        /// </summary>
        private string PrivateProperty { get; set; } = "Private";

        #endregion

        #region Public Methods

        /// <summary>
        /// A public method.
        /// </summary>
        /// <returns>A string indicating this is a public method.</returns>
        public string PublicMethod()
        {
            return "Public method";
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// An internal method.
        /// </summary>
        /// <returns>A string indicating this is an internal method.</returns>
        internal string InternalMethod()
        {
            return "Internal method";
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// A protected method.
        /// </summary>
        /// <returns>A string indicating this is a protected method.</returns>
        protected string ProtectedMethod()
        {
            return "Protected method";
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// A private method.
        /// </summary>
        /// <returns>A string indicating this is a private method.</returns>
        private string PrivateMethod()
        {
            return "Private method";
        }

        #endregion

    }

}