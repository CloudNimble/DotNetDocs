namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the configuration for 404 (Not Found) error handling in the Mintlify documentation site.
    /// </summary>
    /// <remarks>
    /// This configuration controls what happens when users try to access pages that don't exist,
    /// including whether to automatically redirect them to the home page.
    /// </remarks>
    public class Error404Config
    {

        #region Properties

        /// <summary>
        /// Gets or sets whether to automatically redirect users to the home page when a 404 error occurs.
        /// </summary>
        /// <remarks>
        /// When true (default), users who navigate to non-existent pages will be automatically
        /// redirected to the home page of the documentation site. When false, users will see
        /// a standard 404 error page instead. Automatic redirection can improve user experience
        /// by keeping users within the documentation rather than showing error pages.
        /// </remarks>
        public bool? Redirect { get; set; }

        #endregion

    }

}