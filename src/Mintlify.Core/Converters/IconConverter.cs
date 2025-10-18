using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mintlify.Core.Models;

namespace Mintlify.Core.Converters
{

    /// <summary>
    /// Handles JSON conversion for icon properties that can be either strings or objects.
    /// </summary>
    /// <remarks>
    /// Icons in Mintlify can be specified as:
    /// - String: Simple icon name (e.g., "folder", "home")
    /// - Object: Icon configuration with name, style, and library properties
    /// </remarks>
    public class IconConverter : JsonConverter<IconConfig>
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
                if (options.Converters[i] is IconConverter)
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
        /// <returns>True if the type is IconConfig; otherwise, false.</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(IconConfig);
        }

        /// <summary>
        /// Reads and converts the JSON to an IconConfig object.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert to.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>An IconConfig for both simple icon names and complex icon configurations.</returns>
        /// <exception cref="JsonException">Thrown when the JSON token type is not supported.</exception>
        public override IconConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => new IconConfig { Name = reader.GetString() ?? string.Empty },
                JsonTokenType.StartObject => JsonSerializer.Deserialize<IconConfig>(ref reader, OptionsWithoutThis),
                JsonTokenType.Null => null,
                _ => throw new JsonException($"Unexpected token type for icon: {reader.TokenType}")
            };
        }

        /// <summary>
        /// Writes the IconConfig object to JSON.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The IconConfig value to write.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, IconConfig? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            // If only Name is set (simple icon), write as string
            if (!string.IsNullOrWhiteSpace(value.Name) &&
                string.IsNullOrWhiteSpace(value.Library) &&
                string.IsNullOrWhiteSpace(value.Style))
            {
                writer.WriteStringValue(value.Name);
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