using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents the favicon configuration for Mintlify.
    /// Can be a single file or separate files for light and dark mode.
    /// </summary>
    [JsonConverter(typeof(FaviconConfigJsonConverter))]
    public class FaviconConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the path to the dark favicon file, including the file extension.
        /// </summary>
        public string? Dark { get; set; }

        /// <summary>
        /// Gets or sets the path to the light favicon file, including the file extension.
        /// </summary>
        public string? Light { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FaviconConfig"/> class.
        /// </summary>
        public FaviconConfig()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FaviconConfig"/> class with a single favicon path.
        /// </summary>
        /// <param name="faviconPath">The path to the favicon file.</param>
        public FaviconConfig(string? faviconPath)
        {
            Dark = faviconPath;
            Light = faviconPath;
        }

        #endregion

    }

    /// <summary>
    /// Custom JSON converter for FaviconConfig that supports both string and object formats.
    /// </summary>
    public class FaviconConfigJsonConverter : JsonConverter<FaviconConfig>
    {

        /// <summary>
        /// Reads and converts the JSON to a FaviconConfig object.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert to.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>A FaviconConfig object.</returns>
        public override FaviconConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                // Handle simple string format: "favicon": "/favicon.svg"
                var faviconPath = reader.GetString();
                return new FaviconConfig(faviconPath);
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                // Handle object format: "favicon": { "dark": "/dark.svg", "light": "/light.svg" }
                var faviconConfig = new FaviconConfig();

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
                            case "dark":
                                faviconConfig.Dark = reader.GetString();
                                break;
                            case "light":
                                faviconConfig.Light = reader.GetString();
                                break;
                        }
                    }
                }

                return faviconConfig;
            }

            return null;
        }

        /// <summary>
        /// Writes a FaviconConfig object as JSON.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The FaviconConfig object to write.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, FaviconConfig value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            // If both dark and light are the same (or one is null), write as simple string
            if (!string.IsNullOrEmpty(value.Dark) && !string.IsNullOrEmpty(value.Light) && value.Dark != value.Light)
            {
                // Write as object when dark and light are different
                writer.WriteStartObject();

                if (!string.IsNullOrEmpty(value.Dark))
                {
                    writer.WriteString("dark", value.Dark);
                }

                if (!string.IsNullOrEmpty(value.Light))
                {
                    writer.WriteString("light", value.Light);
                }

                writer.WriteEndObject();
            }
            else
            {
                // Write as simple string when both are the same or only one is set
                var faviconPath = !string.IsNullOrEmpty(value.Dark) ? value.Dark : value.Light;
                if (!string.IsNullOrEmpty(faviconPath))
                {
                    writer.WriteStringValue(faviconPath);
                }
                else
                {
                    writer.WriteNullValue();
                }
            }
        }

    }

}