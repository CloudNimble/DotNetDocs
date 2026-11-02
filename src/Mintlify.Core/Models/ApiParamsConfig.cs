using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the display settings for API parameters in the documentation.
    /// </summary>
    /// <remarks>
    /// Controls how API parameters are displayed in the interactive playground
    /// and documentation pages, including whether they are expanded by default.
    /// </remarks>
    public class ApiParamsConfig
    {

        #region Fields

        private string? _expanded;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets how parameter fields are expanded in the playground.
        /// </summary>
        /// <example>
        /// <code>
        /// "params": {
        ///   "expanded": "closed"
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// Valid values are <c>all</c> and <c>closed</c>. Assigning <c>true</c> or <c>false</c>
        /// (as strings) maps to <c>all</c> and <c>closed</c>. Boolean JSON tokens are not accepted.
        /// </remarks>
        [JsonPropertyName("expanded")]
        public string? Expanded
        {
            get => _expanded;
            set => _expanded = NormalizeExpanded(value);
        }

        /// <summary>
        /// Gets or sets OpenAPI keys rendered as ParamField pills for POST bodies.
        /// </summary>
        [JsonPropertyName("post")]
        public List<string>? Post { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Maps legacy boolean-like values to the schema enum.
        /// </summary>
        /// <param name="value">The raw expanded value.</param>
        /// <returns><c>all</c> or <c>closed</c> when the value is boolean-like; otherwise the trimmed input.</returns>
        public static string? NormalizeExpanded(string? value)
        {
            if (value is null || string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            if (bool.TryParse(trimmed, out var expanded))
            {
                return expanded ? "all" : "closed";
            }

            return trimmed;
        }

        #endregion

    }

}