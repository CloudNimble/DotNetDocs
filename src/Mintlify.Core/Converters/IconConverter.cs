using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Converters
{

    /// <summary>
    /// Handles JSON conversion for icon properties that can be either strings or objects.
    /// </summary>
    /// <remarks>
    /// Icons in Mintlify can be specified as:
    /// - String: Simple icon name (e.g., "folder", "home")
    /// - Object: Icon configuration with additional properties
    /// </remarks>
    public class IconConverter : JsonConverter<object>
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
        /// Reads and converts the JSON to an icon object.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert to.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>A string for simple icon names or a Dictionary for icon configuration objects.</returns>
        /// <exception cref="JsonException">Thrown when the JSON token type is not supported.</exception>
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.StartObject => JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options),
                _ => throw new JsonException($"Unexpected token type for icon: {reader.TokenType}")
            };
        }

        /// <summary>
        /// Writes the icon object to JSON.
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
                    throw new JsonException($"Unsupported icon value type: {value?.GetType()}");
            }
        }

        #endregion

    }

}