using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mintlify.Core.Models;

namespace Mintlify.Core.Converters
{

    /// <summary>
    /// Handles JSON conversion for server configuration properties that can be strings or arrays.
    /// </summary>
    /// <remarks>
    /// Server configurations in Mintlify can be specified as:
    /// - String: Single server URL
    /// - Array: Multiple server URLs
    /// </remarks>
    public class ServerConfigConverter : JsonConverter<ServerConfig>
    {

        #region Public Methods

        /// <summary>
        /// Determines whether the specified type can be converted by this converter.
        /// </summary>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <returns>True if the type is ServerConfig; otherwise, false.</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(ServerConfig);
        }

        /// <summary>
        /// Reads and converts the JSON to a ServerConfig object.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert to.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>A ServerConfig for all supported server configuration formats.</returns>
        /// <exception cref="JsonException">Thrown when the JSON token type is not supported.</exception>
        public override ServerConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => new ServerConfig { Url = reader.GetString() },
                JsonTokenType.StartArray => new ServerConfig { Urls = JsonSerializer.Deserialize<List<string>>(ref reader, options) },
                JsonTokenType.Null => null,
                _ => throw new JsonException($"Unexpected token type for server configuration: {reader.TokenType}")
            };
        }

        /// <summary>
        /// Writes the ServerConfig object to JSON.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The ServerConfig value to write.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, ServerConfig? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            // If URLs list is set, write as array
            if (value.Urls is not null)
            {
                JsonSerializer.Serialize(writer, value.Urls, options);
            }
            // Otherwise, write as single string
            else if (!string.IsNullOrWhiteSpace(value.Url))
            {
                writer.WriteStringValue(value.Url);
            }
            else
            {
                writer.WriteNullValue();
            }
        }

        #endregion

    }

}