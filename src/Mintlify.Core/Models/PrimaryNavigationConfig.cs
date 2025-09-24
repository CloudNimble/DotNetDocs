using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the primary navigation configuration for Mintlify navbar.
    /// Can be a button configuration or a GitHub configuration.
    /// </summary>
    [JsonConverter(typeof(PrimaryNavigationConfigJsonConverter))]
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

    /// <summary>
    /// Custom JSON converter for PrimaryNavigationConfig that handles object format.
    /// </summary>
    public class PrimaryNavigationConfigJsonConverter : JsonConverter<PrimaryNavigationConfig>
    {

        #region Public Methods

        /// <summary>
        /// Reads and converts the JSON to a PrimaryNavigationConfig object.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert to.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>A PrimaryNavigationConfig object.</returns>
        public override PrimaryNavigationConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                var config = new PrimaryNavigationConfig();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }

                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = reader.GetString();
                        reader.Read();

                        switch (propertyName?.ToLowerInvariant())
                        {
                            case "type":
                                config.Type = reader.GetString();
                                break;
                            case "label":
                                config.Label = reader.GetString();
                                break;
                            case "href":
                                config.Href = reader.GetString();
                                break;
                        }
                    }
                }

                return config;
            }

            return null;
        }

        /// <summary>
        /// Writes a PrimaryNavigationConfig object as JSON.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The PrimaryNavigationConfig object to write.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, PrimaryNavigationConfig value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            if (!string.IsNullOrWhiteSpace(value.Type))
            {
                writer.WriteString("type", value.Type);
            }

            if (!string.IsNullOrWhiteSpace(value.Label))
            {
                writer.WriteString("label", value.Label);
            }

            if (!string.IsNullOrWhiteSpace(value.Href))
            {
                writer.WriteString("href", value.Href);
            }

            writer.WriteEndObject();
        }

        #endregion

    }

}