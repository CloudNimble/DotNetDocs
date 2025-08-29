namespace CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios
{
    /// <summary>
    /// A base class for testing inheritance documentation.
    /// </summary>
    /// <remarks>
    /// This class serves as the base for DerivedClass.
    /// </remarks>
    public class BaseClass
    {

        #region Properties

        /// <summary>
        /// Gets or sets the base property.
        /// </summary>
        public virtual string BaseProperty { get; set; } = "Base";

        #endregion

        #region Public Methods

        /// <summary>
        /// A virtual method that can be overridden.
        /// </summary>
        /// <returns>A string value.</returns>
        public virtual string VirtualMethod()
        {
            return "Base implementation";
        }

        /// <summary>
        /// A method in the base class.
        /// </summary>
        public void BaseMethod()
        {
            // Base implementation
        }

        #endregion

    }

}