using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mintlify.Core.Models;

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access
#pragma warning disable IL3050 // Members annotated with 'RequiresDynamicCodeAttribute' require dynamic access

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
                JsonTokenType.StartArray => ReadArray(ref reader),
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

            if (value.Specs is not null)
            {
                WriteMixedArray(writer, value.Specs);
            }
            else if (value.Urls is not null)
            {
                JsonSerializer.Serialize(writer, value.Urls, OptionsWithoutThis);
            }
            else if (IsSimpleSource(value))
            {
                writer.WriteStringValue(value.Source);
            }
            else
            {
                JsonSerializer.Serialize(writer, value, OptionsWithoutThis);
            }
        }

        #endregion

        #region Private Methods

        private static bool IsSimpleSource(ApiSpecConfig value)
        {
            return !string.IsNullOrWhiteSpace(value.Source) &&
                   string.IsNullOrWhiteSpace(value.Directory) &&
                   value.Overlays is null;
        }

        private static ApiSpecConfig ReadArray(ref Utf8JsonReader reader)
        {
            List<string>? urls = null;
            List<ApiSpecConfig>? specs = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        var url = reader.GetString();
                        if (url is not null && !string.IsNullOrWhiteSpace(url))
                        {
                            if (specs is not null)
                            {
                                specs.Add(new ApiSpecConfig { Source = url });
                            }
                            else
                            {
                                urls ??= [];
                                urls.Add(url);
                            }
                        }
                        break;
                    case JsonTokenType.StartObject:
                        if (specs is null)
                        {
                            specs = [];
                            if (urls is not null)
                            {
                                foreach (var existing in urls)
                                {
                                    specs.Add(new ApiSpecConfig { Source = existing });
                                }

                                urls = null;
                            }
                        }

                        var spec = JsonSerializer.Deserialize<ApiSpecConfig>(ref reader, OptionsWithoutThis);
                        if (spec is not null)
                        {
                            specs.Add(spec);
                        }
                        break;
                    default:
                        throw new JsonException($"Unexpected token type in API specification array: {reader.TokenType}");
                }
            }

            if (specs is not null)
            {
                return new ApiSpecConfig { Specs = specs };
            }

            return new ApiSpecConfig { Urls = urls ?? [] };
        }

        private static void WriteMixedArray(Utf8JsonWriter writer, List<ApiSpecConfig> specs)
        {
            writer.WriteStartArray();
            foreach (var spec in specs)
            {
                if (IsSimpleSource(spec))
                {
                    writer.WriteStringValue(spec.Source);
                }
                else
                {
                    JsonSerializer.Serialize(writer, spec, OptionsWithoutThis);
                }
            }
            writer.WriteEndArray();
        }

        #endregion

    }

}