using System.Collections.Generic;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents third-party npm package configuration for MDX pages and snippets.
    /// </summary>
    /// <remarks>
    /// Adding this object, even empty, turns on third-party libraries so MDX pages and
    /// snippets can import components from packages in <c>package.json</c>. Requires a
    /// <c>package-lock.json</c> or <c>pnpm-lock.yaml</c> file next to <c>docs.json</c>.
    /// See https://www.mintlify.com/docs/customize/third-party-libraries.
    /// </remarks>
    public class PackagesConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets compiled CSS files that packages ship, as paths inside an installed package.
        /// </summary>
        /// <remarks>
        /// Example: <c>@acme/ui/dist/styles.css</c>.
        /// </remarks>
        public List<string>? Css { get; set; }

        /// <summary>
        /// Gets or sets the path to a snippet that exports a Provider component.
        /// </summary>
        /// <remarks>
        /// The provider wraps each package component. Example: <c>/snippets/provider.jsx</c>.
        /// </remarks>
        public string? Provider { get; set; }

        /// <summary>
        /// Gets or sets the registry URL for each npm scope.
        /// </summary>
        /// <remarks>
        /// Example: <c>{ "@acme": "https://npm.acme.com/" }</c>.
        /// </remarks>
        public Dictionary<string, string>? Registries { get; set; }

        /// <summary>
        /// Gets or sets whether to render package components to HTML when deploying.
        /// </summary>
        /// <remarks>
        /// Defaults to true.
        /// </remarks>
        public bool? Ssr { get; set; }

        #endregion

    }

}
