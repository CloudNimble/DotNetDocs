using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mintlify.Core.Models;

namespace Mintlify.Core.Converters
{

    /// <summary>
    /// Handles JSON conversion for color properties that can be either strings or objects.
    /// </summary>
    /// <remarks>
    /// Color configurations in Mintlify can be specified as:
    /// - String: Simple hex color value (e.g., "#FF0000")
    /// - Object: Complex color configuration with light and dark mode properties
    /// </remarks>
    public class ColorConverter : JsonConverter<ColorConfig>
    {

        #region Private Fields

        /// <summary>
        /// Lazy-initialized JsonSerializerOptions that excludes this converter to prevent infinite recursion.
        /// </summary>
        private static readonly Lazy<JsonSerializerOptions> _optionsWithoutThis = new Lazy<JsonSerializerOptions>(() =>
        {
            var options = new JsonSerializerOptions(MintlifyConstants.JsonSerializerOptions);
            // Remove this converter to prevent recursion
            for (int i = options.Converters.Count - 1; i >= 0; i--)
            {
                if (options.Converters[i] is ColorConverter)
                {
                    options.Converters.RemoveAt(i);
                }
            }

            return options;
        });

        /// <summary>
        /// Gets the JsonSerializerOptions instance without this converter to prevent infinite recursion.
        /// </summary>
        internal static JsonSerializerOptions OptionsWithoutThis => _optionsWithoutThis.Value;

        #endregion

        #region Public Methods

        /// <summary>
        /// Determines whether the specified type can be converted by this converter.
        /// </summary>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <returns>True if the type is ColorConfig; otherwise, false.</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(ColorConfig);
        }

        /// <summary>
        /// Reads and converts the JSON to a ColorConfig object.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert to.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>A ColorConfig object.</returns>
        /// <exception cref="JsonException">Thrown when the JSON token type is not supported.</exception>
        public override ColorConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => new ColorConfig(reader.GetString()),
                JsonTokenType.StartObject => JsonSerializer.Deserialize<ColorConfig>(ref reader, OptionsWithoutThis),
                _ => throw new JsonException($"Unexpected token type for color: {reader.TokenType}")
            };
        }

        /// <summary>
        /// Writes the ColorConfig object to JSON.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The ColorConfig value to write.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, ColorConfig value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, value, OptionsWithoutThis);
        }

        #endregion

    }

}