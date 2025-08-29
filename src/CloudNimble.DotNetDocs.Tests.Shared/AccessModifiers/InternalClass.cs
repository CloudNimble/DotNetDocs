
namespace CloudNimble.DotNetDocs.Tests.Shared.AccessModifiers
{
    /// <summary>
    /// An internal class for testing internal type visibility.
    /// </summary>
    /// <remarks>
    /// This class should only be visible when internal members are included.
    /// </remarks>
    internal class InternalClass
    {

        #region Properties

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = "InternalClass";

        /// <summary>
        /// Gets or sets the internal value.
        /// </summary>
        internal int InternalValue { get; set; } = 42;

        #endregion

        #region Public Methods

        /// <summary>
        /// A public method in an internal class.
        /// </summary>
        /// <returns>A description string.</returns>
        public string GetDescription()
        {
            return $"{Name}: {InternalValue}";
        }

        #endregion

    }

}