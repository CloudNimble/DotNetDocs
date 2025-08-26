using System;
using System.Collections.Generic;
using System.Linq;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents frontmatter configuration for Mintlify documentation pages.
    /// </summary>
    /// <remarks>
    /// This class contains all the documented frontmatter properties supported by Mintlify.
    /// These properties control page appearance, navigation, and behavior in the Mintlify documentation site.
    /// For more information, see the Mintlify documentation at https://mintlify.com/docs/content/overview.
    /// </remarks>
    public class FrontMatterConfig
    {

        #region Core Properties

        /// <summary>
        /// Gets or sets the title of the page.
        /// </summary>
        /// <remarks>
        /// This is the main heading displayed at the top of the page and used in navigation.
        /// </remarks>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the page.
        /// </summary>
        /// <remarks>
        /// Used for SEO meta descriptions and page summaries.
        /// </remarks>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the icon for the page.
        /// </summary>
        /// <remarks>
        /// Can be a FontAwesome icon name, Lucide icon name, or a custom icon path.
        /// Examples: "cube", "book", "settings", "star".
        /// </remarks>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sidebar title.
        /// </summary>
        /// <remarks>
        /// Shorter title used in the sidebar navigation. If not specified, the main title is used.
        /// </remarks>
        public string SidebarTitle { get; set; } = string.Empty;

        #endregion

        #region Layout Properties

        /// <summary>
        /// Gets or sets the layout mode for the page.
        /// </summary>
        /// <remarks>
        /// Controls the page layout. Common values: "wide", "centered".
        /// </remarks>
        public string Mode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the icon type.
        /// </summary>
        /// <remarks>
        /// Specifies the icon style. Common values: "solid", "regular", "light", "duotone".
        /// </remarks>
        public string IconType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tag for the page.
        /// </summary>
        /// <remarks>
        /// Displays a tag badge next to the title. Common values: "NEW", "BETA", "DEPRECATED".
        /// </remarks>
        public string Tag { get; set; } = string.Empty;

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Gets or sets the external URL for the page.
        /// </summary>
        /// <remarks>
        /// If specified, clicking on this page in navigation will redirect to this URL instead of showing content.
        /// </remarks>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the page is deprecated.
        /// </summary>
        /// <remarks>
        /// When true, displays a deprecation notice on the page.
        /// </remarks>
        public bool? Deprecated { get; set; }

        /// <summary>
        /// Gets or sets the groups that can access this page.
        /// </summary>
        /// <remarks>
        /// Used for access control in authenticated documentation sites.
        /// </remarks>
        public List<string> Groups { get; set; } = [];

        /// <summary>
        /// Gets or sets a value indicating whether the page is public.
        /// </summary>
        /// <remarks>
        /// Controls visibility in public documentation sites.
        /// </remarks>
        public bool? Public { get; set; }

        /// <summary>
        /// Gets or sets the version for the page.
        /// </summary>
        /// <remarks>
        /// Used for version-specific documentation.
        /// </remarks>
        public string Version { get; set; } = string.Empty;

        #endregion

        #region SEO Properties

        /// <summary>
        /// Gets or sets the canonical URL for SEO.
        /// </summary>
        /// <remarks>
        /// Helps prevent duplicate content issues by specifying the canonical version of the page.
        /// </remarks>
        public string Canonical { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the robots meta tag content.
        /// </summary>
        /// <remarks>
        /// Controls search engine indexing. Common values: "index,follow", "noindex,nofollow".
        /// </remarks>
        public string Robots { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional meta tags for the page.
        /// </summary>
        /// <remarks>
        /// Dictionary of additional meta tag name/content pairs for custom SEO or social media tags.
        /// </remarks>
        public Dictionary<string, string> Meta { get; set; } = [];

        #endregion

        #region OpenGraph Properties

        /// <summary>
        /// Gets or sets the OpenGraph title.
        /// </summary>
        /// <remarks>
        /// Title used when the page is shared on social media platforms.
        /// </remarks>
        public string OgTitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the OpenGraph description.
        /// </summary>
        /// <remarks>
        /// Description used when the page is shared on social media platforms.
        /// </remarks>
        public string OgDescription { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the OpenGraph image URL.
        /// </summary>
        /// <remarks>
        /// Image used when the page is shared on social media platforms.
        /// </remarks>
        public string OgImage { get; set; } = string.Empty;

        #endregion

        #region Advanced Properties

        /// <summary>
        /// Gets or sets custom CSS classes for the page.
        /// </summary>
        /// <remarks>
        /// Additional CSS classes to apply to the page for custom styling.
        /// </remarks>
        public List<string> Classes { get; set; } = [];

        /// <summary>
        /// Gets or sets custom data attributes.
        /// </summary>
        /// <remarks>
        /// Custom data attributes to add to the page for analytics or custom JavaScript.
        /// </remarks>
        public Dictionary<string, string> Data { get; set; } = [];

        /// <summary>
        /// Gets or sets the navigation order.
        /// </summary>
        /// <remarks>
        /// Numeric value to control the order of pages in navigation. Lower numbers appear first.
        /// </remarks>
        public int? Order { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the page should be hidden from navigation.
        /// </summary>
        /// <remarks>
        /// When true, the page is accessible by direct URL but not shown in navigation menus.
        /// </remarks>
        public bool? Hidden { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Generates YAML frontmatter from this configuration.
        /// </summary>
        /// <returns>The YAML frontmatter as a string, including delimiters.</returns>
        public string ToYaml()
        {
            var lines = new List<string> { "---" };

            // Core properties - always include title and description if they exist
            if (!string.IsNullOrWhiteSpace(Title))
            {
                lines.Add($"title: {EscapeYamlValue(Title)}");
            }

            if (!string.IsNullOrWhiteSpace(Description))
            {
                lines.Add($"description: {EscapeYamlValue(Description)}");
            }

            // Layout properties
            if (!string.IsNullOrWhiteSpace(Icon))
            {
                lines.Add($"icon: {EscapeYamlValue(Icon)}");
            }

            if (!string.IsNullOrWhiteSpace(SidebarTitle))
            {
                lines.Add($"sidebarTitle: {EscapeYamlValue(SidebarTitle)}");
            }

            if (!string.IsNullOrWhiteSpace(Mode))
            {
                lines.Add($"mode: {EscapeYamlValue(Mode)}");
            }

            if (!string.IsNullOrWhiteSpace(IconType))
            {
                lines.Add($"iconType: {EscapeYamlValue(IconType)}");
            }

            if (!string.IsNullOrWhiteSpace(Tag))
            {
                lines.Add($"tag: {EscapeYamlValue(Tag)}");
            }

            // Navigation properties
            if (!string.IsNullOrWhiteSpace(Url))
            {
                lines.Add($"url: {EscapeYamlValue(Url)}");
            }

            if (Deprecated.HasValue)
            {
                lines.Add($"deprecated: {Deprecated.Value.ToString().ToLowerInvariant()}");
            }

            if (Groups.Any())
            {
                lines.Add($"groups: [{string.Join(", ", Groups.Select(EscapeYamlValue))}]");
            }

            if (Public.HasValue)
            {
                lines.Add($"public: {Public.Value.ToString().ToLowerInvariant()}");
            }

            if (!string.IsNullOrWhiteSpace(Version))
            {
                lines.Add($"version: {EscapeYamlValue(Version)}");
            }

            // SEO properties
            if (!string.IsNullOrWhiteSpace(Canonical))
            {
                lines.Add($"canonical: {EscapeYamlValue(Canonical)}");
            }

            if (!string.IsNullOrWhiteSpace(Robots))
            {
                lines.Add($"robots: {EscapeYamlValue(Robots)}");
            }

            // OpenGraph properties
            if (!string.IsNullOrWhiteSpace(OgTitle))
            {
                lines.Add($"ogTitle: {EscapeYamlValue(OgTitle)}");
            }

            if (!string.IsNullOrWhiteSpace(OgDescription))
            {
                lines.Add($"ogDescription: {EscapeYamlValue(OgDescription)}");
            }

            if (!string.IsNullOrWhiteSpace(OgImage))
            {
                lines.Add($"ogImage: {EscapeYamlValue(OgImage)}");
            }

            // Advanced properties
            if (Classes.Any())
            {
                lines.Add($"classes: [{string.Join(", ", Classes.Select(EscapeYamlValue))}]");
            }

            if (Order.HasValue)
            {
                lines.Add($"order: {Order.Value}");
            }

            if (Hidden.HasValue)
            {
                lines.Add($"hidden: {Hidden.Value.ToString().ToLowerInvariant()}");
            }

            // Meta and Data dictionaries
            foreach (var meta in Meta)
            {
                lines.Add($"meta.{meta.Key}: {EscapeYamlValue(meta.Value)}");
            }

            foreach (var data in Data)
            {
                lines.Add($"data.{data.Key}: {EscapeYamlValue(data.Value)}");
            }

            lines.Add("---");
            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Escapes a value for safe use in YAML frontmatter.
        /// </summary>
        /// <param name="value">The value to escape.</param>
        /// <returns>The escaped value, properly quoted if necessary.</returns>
        public static string EscapeYamlValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "\"\"";
            }

            // Clean up whitespace first
            var cleaned = value.Trim().Replace("\r\n", " ").Replace("\n", " ");

            // Normalize multiple spaces to single spaces
            while (cleaned.Contains("  "))
            {
                cleaned = cleaned.Replace("  ", " ");
            }

            // Escape MDX-sensitive characters for frontmatter
            cleaned = cleaned.Replace("<", "&lt;").Replace(">", "&gt;");

            // Check if the value needs quoting
            var needsQuoting = cleaned.Contains(":") || cleaned.Contains("\"") || cleaned.Contains("'") ||
                              cleaned.Contains("#") || cleaned.Contains("[") || cleaned.Contains("]") ||
                              cleaned.Contains("{") || cleaned.Contains("}") || cleaned.Contains("|") ||
                              cleaned.Contains("&") || cleaned.StartsWith("-") || cleaned.StartsWith("@") ||
                              char.IsDigit(cleaned[0]) || cleaned.Contains("\n") || cleaned.Contains("\r");

            if (needsQuoting)
            {
                // Escape quotes and backslashes for double-quoted strings
                cleaned = cleaned.Replace("\\", "\\\\").Replace("\"", "\\\"");
                return $"\"{cleaned}\"";
            }

            return cleaned;
        }

        /// <summary>
        /// Creates a FrontMatterConfig with basic title and description, properly escaped.
        /// </summary>
        /// <param name="title">The page title.</param>
        /// <param name="description">The page description.</param>
        /// <returns>A new FrontMatterConfig instance.</returns>
        public static FrontMatterConfig Create(string title, string? description = null)
        {
            return new FrontMatterConfig
            {
                Title = title ?? string.Empty,
                Description = description ?? string.Empty
            };
        }

        #endregion

    }

}
