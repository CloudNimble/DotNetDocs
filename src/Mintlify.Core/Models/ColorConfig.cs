using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents a color configuration for Mintlify.
    /// Can be a simple hex color string or a complex color pair configuration with light and dark modes.
    /// </summary>
    [JsonConverter(typeof(ColorConfigJsonConverter))]
    public class ColorConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the color in hex format to use in dark mode.
        /// </summary>
        public string? Dark { get; set; }

        /// <summary>
        /// Gets or sets the color in hex format to use in light mode.
        /// </summary>
        public string? Light { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorConfig"/> class.
        /// </summary>
        public ColorConfig()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorConfig"/> class with a single color.
        /// </summary>
        /// <param name="color">The hex color string for both light and dark modes.</param>
        public ColorConfig(string? color)
        {
            Light = color;
            Dark = color;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorConfig"/> class with separate light and dark colors.
        /// </summary>
        /// <param name="light">The hex color string for light mode.</param>
        /// <param name="dark">The hex color string for dark mode.</param>
        public ColorConfig(string? light, string? dark)
        {
            Light = light;
            Dark = dark;
        }

        #endregion

        #region Implicit Operators

        /// <summary>
        /// Implicitly converts a string color to a ColorConfig.
        /// </summary>
        /// <param name="color">The hex color string.</param>
        /// <returns>A ColorConfig with the specified color for both light and dark modes.</returns>
        public static implicit operator ColorConfig?(string? color)
        {
            return color is null ? null : new ColorConfig(color);
        }

        /// <summary>
        /// Implicitly converts a ColorConfig to a string.
        /// </summary>
        /// <param name="colorConfig">The color configuration.</param>
        /// <returns>The light color, dark color, or null.</returns>
        public static implicit operator string?(ColorConfig? colorConfig)
        {
            return colorConfig?.Light ?? colorConfig?.Dark;
        }

        /// <summary>
        /// Implicitly converts a ColorPairConfig to a ColorConfig.
        /// </summary>
        /// <param name="colorPairConfig">The color pair configuration.</param>
        /// <returns>A ColorConfig with the same light and dark values.</returns>
        public static implicit operator ColorConfig?(ColorPairConfig? colorPairConfig)
        {
            return colorPairConfig is null ? null : new ColorConfig(colorPairConfig.Light, colorPairConfig.Dark);
        }

        /// <summary>
        /// Implicitly converts a ColorConfig to a ColorPairConfig.
        /// </summary>
        /// <param name="colorConfig">The color configuration.</param>
        /// <returns>A ColorPairConfig with the same light and dark values.</returns>
        public static implicit operator ColorPairConfig?(ColorConfig? colorConfig)
        {
            return colorConfig is null ? null : new ColorPairConfig { Light = colorConfig.Light, Dark = colorConfig.Dark };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the string representation of the color configuration.
        /// </summary>
        /// <returns>The light color, dark color, or empty string.</returns>
        public override string ToString()
        {
            return Light ?? Dark ?? string.Empty;
        }

        #endregion

    }

    /// <summary>
    /// Custom JSON converter for ColorConfig that supports both string and object formats.
    /// </summary>
    public class ColorConfigJsonConverter : JsonConverter<ColorConfig>
    {

        #region Public Methods

        /// <summary>
        /// Reads and converts the JSON to a ColorConfig object.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert to.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>A ColorConfig object.</returns>
        public override ColorConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                // Handle simple string format: "color": "#FF0000"
                var colorValue = reader.GetString();
                return new ColorConfig(colorValue);
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                // Handle object format: "color": { "dark": "#000000", "light": "#FFFFFF" }
                var colorConfig = new ColorConfig();

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
                                colorConfig.Dark = reader.GetString();
                                break;
                            case "light":
                                colorConfig.Light = reader.GetString();
                                break;
                        }
                    }
                }

                return colorConfig;
            }

            return null;
        }

        /// <summary>
        /// Writes a ColorConfig object as JSON.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The ColorConfig object to write.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, ColorConfig value, JsonSerializerOptions options)
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
                var colorValue = !string.IsNullOrEmpty(value.Light) ? value.Light : value.Dark;
                if (!string.IsNullOrEmpty(colorValue))
                {
                    writer.WriteStringValue(colorValue);
                }
                else
                {
                    writer.WriteNullValue();
                }
            }
        }

        #endregion

    }

}