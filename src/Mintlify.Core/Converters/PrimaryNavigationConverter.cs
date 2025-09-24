using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mintlify.Core.Models;

namespace Mintlify.Core.Converters
{

    /// <summary>
    /// Handles JSON conversion for primary navigation properties that can be different types of objects.
    /// </summary>
    /// <remarks>
    /// Primary navigation in Mintlify NavbarConfig can be specified as:
    /// - Object: Button configuration with type, label, and href properties
    /// - Object: GitHub configuration with type and href properties
    /// </remarks>
    public class PrimaryNavigationConverter : JsonConverter<PrimaryNavigationConfig>
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
                if (options.Converters[i] is PrimaryNavigationConverter)
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
        /// <returns>True if the type is PrimaryNavigationConfig; otherwise, false.</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(PrimaryNavigationConfig);
        }

        /// <summary>
        /// Reads and converts the JSON to a PrimaryNavigationConfig object.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert to.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>A PrimaryNavigationConfig object.</returns>
        /// <exception cref="JsonException">Thrown when the JSON token type is not supported.</exception>
        public override PrimaryNavigationConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.StartObject => JsonSerializer.Deserialize<PrimaryNavigationConfig>(ref reader, OptionsWithoutThis),
                _ => throw new JsonException($"Unexpected token type for primary navigation: {reader.TokenType}")
            };
        }

        /// <summary>
        /// Writes the PrimaryNavigationConfig object to JSON.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The PrimaryNavigationConfig value to write.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, PrimaryNavigationConfig value, JsonSerializerOptions options)
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