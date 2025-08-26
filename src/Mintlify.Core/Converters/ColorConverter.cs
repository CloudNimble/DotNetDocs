using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Converters
{

    /// <summary>
    /// Handles JSON conversion for color properties that can be either strings or objects.
    /// </summary>
    /// <remarks>
    /// Color configurations in Mintlify can be specified as:
    /// - String: Simple hex color value (e.g., "#FF0000")
    /// - Object: Complex color configuration with additional properties
    /// </remarks>
    public class ColorConverter : JsonConverter<object>
    {

        #region Public Methods

        /// <summary>
        /// Determines whether the specified type can be converted by this converter.
        /// </summary>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <returns>True if the type is object; otherwise, false.</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(object);
        }

        /// <summary>
        /// Reads and converts the JSON to a color object.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert to.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>A string for hex colors or a Dictionary for color configuration objects.</returns>
        /// <exception cref="JsonException">Thrown when the JSON token type is not supported.</exception>
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.StartObject => JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options),
                _ => throw new JsonException($"Unexpected token type for color: {reader.TokenType}")
            };
        }

        /// <summary>
        /// Writes the color object to JSON.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="options">The serializer options.</param>
        /// <exception cref="JsonException">Thrown when the value type is not supported.</exception>
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case string stringValue:
                    writer.WriteStringValue(stringValue);
                    break;

                case Dictionary<string, object> dictValue:
                    JsonSerializer.Serialize(writer, dictValue, options);
                    break;

                case null:
                    writer.WriteNullValue();
                    break;

                default:
                    throw new JsonException($"Unsupported color value type: {value?.GetType()}");
            }
        }

        #endregion

    }

}