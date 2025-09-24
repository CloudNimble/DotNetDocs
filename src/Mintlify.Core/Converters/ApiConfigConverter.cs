using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mintlify.Core.Models;

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
    public class ApiConfigConverter : JsonConverter<ApiSpecConfig>
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
                if (options.Converters[i] is ApiConfigConverter)
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
        /// <returns>True if the type is ApiSpecConfig; otherwise, false.</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(ApiSpecConfig);
        }

        /// <summary>
        /// Reads and converts the JSON to an ApiSpecConfig object.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert to.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>An ApiSpecConfig for all supported API configuration formats.</returns>
        /// <exception cref="JsonException">Thrown when the JSON token type is not supported.</exception>
        public override ApiSpecConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => new ApiSpecConfig { Source = reader.GetString() },
                JsonTokenType.StartArray => new ApiSpecConfig { Urls = JsonSerializer.Deserialize<List<string>>(ref reader, OptionsWithoutThis) },
                JsonTokenType.StartObject => JsonSerializer.Deserialize<ApiSpecConfig>(ref reader, OptionsWithoutThis),
                JsonTokenType.Null => null,
                _ => throw new JsonException($"Unexpected token type for API configuration: {reader.TokenType}")
            };
        }

        /// <summary>
        /// Writes the ApiSpecConfig object to JSON.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The ApiSpecConfig value to write.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, ApiSpecConfig? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            // If URLs list is set, write as array
            if (value.Urls is not null)
            {
                JsonSerializer.Serialize(writer, value.Urls, OptionsWithoutThis);
            }
            // If only Source is set (simple API config), write as string
            else if (!string.IsNullOrWhiteSpace(value.Source) &&
                     string.IsNullOrWhiteSpace(value.Directory))
            {
                writer.WriteStringValue(value.Source);
            }
            else
            {
                // Write as object for complex configurations
                // Use options without this converter to avoid infinite recursion
                JsonSerializer.Serialize(writer, value, OptionsWithoutThis);
            }
        }

        #endregion

    }

}