using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Converters
{

    /// <summary>
    /// Handles JSON conversion for API configuration properties that can be strings, arrays, or objects.
    /// </summary>
    /// <remarks>
    /// API configurations in Mintlify (OpenAPI and AsyncAPI) can be specified as:
    /// - String: Single URL to specification file
    /// - Array: Multiple URLs to specification files
    /// - Object: Complex configuration with source and directory properties
    /// </remarks>
    public class ApiConfigConverter : JsonConverter<object>
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
        /// Reads and converts the JSON to an API configuration object.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert to.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>A string, List&lt;string&gt;, or Dictionary for API configurations.</returns>
        /// <exception cref="JsonException">Thrown when the JSON token type is not supported.</exception>
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.StartArray => JsonSerializer.Deserialize<List<string>>(ref reader, options),
                JsonTokenType.StartObject => JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options),
                _ => throw new JsonException($"Unexpected token type for API configuration: {reader.TokenType}")
            };
        }

        /// <summary>
        /// Writes the API configuration object to JSON.
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

                case List<string> listValue:
                    JsonSerializer.Serialize(writer, listValue, options);
                    break;

                case Dictionary<string, object> dictValue:
                    JsonSerializer.Serialize(writer, dictValue, options);
                    break;

                case null:
                    writer.WriteNullValue();
                    break;

                default:
                    throw new JsonException($"Unsupported API configuration value type: {value?.GetType()}");
            }
        }

        #endregion

    }

}