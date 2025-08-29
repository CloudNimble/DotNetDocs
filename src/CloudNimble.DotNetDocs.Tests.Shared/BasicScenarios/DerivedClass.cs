using System;

namespace CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios
{

    /// <summary>
    /// A derived class for testing inheritance documentation.
    /// </summary>
    /// <remarks>
    /// This class inherits from BaseClass and overrides some members.
    /// </remarks>
    /// <example>
    /// <code>
    /// var derived = new DerivedClass();
    /// var result = derived.VirtualMethod();
    /// </code>
    /// </example>
    public class DerivedClass : BaseClass
    {

        #region Properties

        /// <summary>
        /// Gets or sets the derived property.
        /// </summary>
        public string DerivedProperty { get; set; } = "Derived";

        /// <summary>
        /// Gets or sets the base property with overridden behavior.
        /// </summary>
        /// <remarks>
        /// This property overrides the base implementation.
        /// </remarks>
        public override string BaseProperty { get; set; } = "Overridden";

        #endregion

        #region Public Methods

        /// <summary>
        /// Overrides the virtual method from the base class.
        /// </summary>
        /// <returns>A string indicating the derived implementation.</returns>
        /// <remarks>
        /// This method provides custom behavior for the derived class.
        /// </remarks>
        public override string VirtualMethod()
        {
            return "Derived implementation";
        }

        /// <summary>
        /// An additional method in the derived class.
        /// </summary>
        public void DerivedMethod()
        {
            // Derived-specific implementation
        }

        #endregion

    }

}