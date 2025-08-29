using System;

namespace CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios
{

    /// <summary>
    /// A class demonstrating various property documentation scenarios.
    /// </summary>
    /// <remarks>
    /// This class contains properties with different access modifiers and documentation styles.
    /// </remarks>
    public class ClassWithProperties
    {

        #region Properties

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <remarks>
        /// This is a standard public property with get and set accessors.
        /// </remarks>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets the read-only identifier.
        /// </summary>
        /// <remarks>
        /// This property can only be read, not written to.
        /// </remarks>
        public int Id { get; } = 42;

        /// <summary>
        /// Gets or sets the value with a private setter.
        /// </summary>
        /// <remarks>
        /// This property can be read publicly but only set within the class.
        /// </remarks>
        public double Value { get; private set; }

        /// <summary>
        /// Gets or sets the internal data.
        /// </summary>
        internal string InternalData { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the protected information.
        /// </summary>
        protected string ProtectedInfo { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the private secret.
        /// </summary>
        private string PrivateSecret { get; set; } = string.Empty;

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the value property.
        /// </summary>
        /// <param name="newValue">The new value to set.</param>
        public void UpdateValue(double newValue)
        {
            Value = newValue;
        }

        #endregion

    }

}