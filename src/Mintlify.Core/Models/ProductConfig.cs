using System.Diagnostics.CodeAnalysis;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a product configuration in Mintlify navigation.
    /// </summary>
    /// <remarks>
    /// Products separate documentation into distinct product-specific sections.
    /// The product name is required.
    /// </remarks>
    public class ProductConfig : NavigationContainerBase
    {

        #region Properties

        /// <summary>
        /// Gets or sets the description of the product.
        /// </summary>
        /// <remarks>
        /// Provides additional context about the product section.
        /// </remarks>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the name of the product.
        /// </summary>
        /// <remarks>
        /// This is a required field that appears as the product label in navigation.
        /// </remarks>
        [NotNull]
        public string Product { get; set; } = string.Empty;

        #endregion

    }

}
