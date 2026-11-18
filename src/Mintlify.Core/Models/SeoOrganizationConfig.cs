using System.Collections.Generic;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the organization used as the publisher entity in structured data.
    /// </summary>
    /// <remarks>
    /// All fields are optional. If this object is omitted, Mintlify derives the organization
    /// from the site name, docs logo, and site URL. See https://www.mintlify.com/docs/optimize/seo.
    /// </remarks>
    public class SeoOrganizationConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the stable <c>@id</c> URL identifying the organization across pages.
        /// </summary>
        /// <remarks>
        /// Example: <c>https://example.com/#organization</c>. Defaults to
        /// <c>&lt;site origin&gt;/#organization</c>.
        /// </remarks>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the registered legal name.
        /// </summary>
        public string? LegalName { get; set; }

        /// <summary>
        /// Gets or sets the canonical logo URL used in structured data.
        /// </summary>
        /// <remarks>
        /// Defaults to the docs logo.
        /// </remarks>
        public string? Logo { get; set; }

        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        /// <remarks>
        /// Defaults to the site name.
        /// </remarks>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets official profile URLs such as X, LinkedIn, or GitHub.
        /// </summary>
        public List<string>? SameAs { get; set; }

        /// <summary>
        /// Gets or sets the canonical organization homepage URL.
        /// </summary>
        public string? Url { get; set; }

        #endregion

    }

}
