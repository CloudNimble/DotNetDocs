namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents code block theming for Mintlify.
    /// </summary>
    /// <remarks>
    /// May be a string (<c>system</c>, <c>dark</c>, or a Shiki theme name) or an object with
    /// <see cref="Theme"/> / <see cref="ThemePair"/> and optional <see cref="Languages"/>.
    /// See https://www.mintlify.com/docs/organize/settings-appearance.
    /// </remarks>
    public class CodeblocksConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets custom language grammars.
        /// </summary>
        public CodeblockLanguagesConfig? Languages { get; set; }

        /// <summary>
        /// Gets or sets the theme name when a single string is used.
        /// </summary>
        /// <remarks>
        /// <c>system</c> and <c>dark</c> match docs appearance. Any other string is a Shiki theme name.
        /// </remarks>
        public string? Theme { get; set; }

        /// <summary>
        /// Gets or sets separate light and dark Shiki themes.
        /// </summary>
        public ThemePairConfig? ThemePair { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Implicitly converts a theme name to a <see cref="CodeblocksConfig"/>.
        /// </summary>
        /// <param name="theme">The theme name.</param>
        /// <returns>A codeblocks config with <see cref="Theme"/> set, or null.</returns>
        public static implicit operator CodeblocksConfig?(string? theme)
        {
            return theme is null ? null : new CodeblocksConfig { Theme = theme };
        }

        /// <summary>
        /// Implicitly converts a <see cref="CodeblocksConfig"/> to its theme name.
        /// </summary>
        /// <param name="config">The codeblocks configuration.</param>
        /// <returns>The theme name, or null.</returns>
        public static implicit operator string?(CodeblocksConfig? config)
        {
            return config?.Theme ?? config?.ThemePair?.Light ?? config?.ThemePair?.Dark;
        }

        /// <summary>
        /// Returns the theme name or an empty string.
        /// </summary>
        /// <returns>The theme name or an empty string.</returns>
        public override string ToString()
        {
            return Theme ?? ThemePair?.ToString() ?? string.Empty;
        }

        #endregion

    }

}
