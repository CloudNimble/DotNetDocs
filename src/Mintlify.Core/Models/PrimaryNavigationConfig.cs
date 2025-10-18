namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the primary navigation configuration for Mintlify navbar.
    /// Can be a button configuration or a GitHub configuration.
    /// </summary>
    public class PrimaryNavigationConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the href URL for the navigation item.
        /// </summary>
        public string? Href { get; set; }

        /// <summary>
        /// Gets or sets the label text for button-type navigation items.
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// Gets or sets the type of navigation item (e.g., "button", "github").
        /// </summary>
        public string? Type { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimaryNavigationConfig"/> class.
        /// </summary>
        public PrimaryNavigationConfig()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimaryNavigationConfig"/> class for a button.
        /// </summary>
        /// <param name="label">The button label.</param>
        /// <param name="href">The button href URL.</param>
        public PrimaryNavigationConfig(string label, string href)
        {
            Type = "button";
            Label = label;
            Href = href;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimaryNavigationConfig"/> class for GitHub.
        /// </summary>
        /// <param name="href">The GitHub URL.</param>
        public PrimaryNavigationConfig(string href)
        {
            Type = "github";
            Href = href;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a button-type navigation configuration.
        /// </summary>
        /// <param name="label">The button label.</param>
        /// <param name="href">The button href URL.</param>
        /// <returns>A PrimaryNavigationConfig configured as a button.</returns>
        public static PrimaryNavigationConfig Button(string label, string href)
        {
            return new PrimaryNavigationConfig(label, href);
        }

        /// <summary>
        /// Creates a GitHub-type navigation configuration.
        /// </summary>
        /// <param name="href">The GitHub URL.</param>
        /// <returns>A PrimaryNavigationConfig configured for GitHub.</returns>
        public static PrimaryNavigationConfig GitHub(string href)
        {
            return new PrimaryNavigationConfig(href);
        }

        /// <summary>
        /// Returns the string representation of the navigation configuration.
        /// </summary>
        /// <returns>The href URL or empty string.</returns>
        public override string ToString()
        {
            return Href ?? string.Empty;
        }

        #endregion

    }

}