using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mintlify.Core.Models;

namespace Mintlify.Core.Converters
{

    /// <summary>
    /// Handles JSON conversion for background image properties that can be either strings or objects.
    /// </summary>
    /// <remarks>
    /// Background image configurations in Mintlify can be specified as:
    /// - String: Single image URL (e.g., "https://example.com/image.png")
    /// - Object: Light/dark mode specific images with "light" and "dark" properties
    /// </remarks>
    public class BackgroundImageConverter : JsonConverter<BackgroundImageConfig>
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
                if (options.Converters[i] is BackgroundImageConverter)
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
        /// <returns>True if the type is BackgroundImageConfig; otherwise, false.</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(BackgroundImageConfig);
        }

        /// <summary>
        /// Reads and converts the JSON to a BackgroundImageConfig object.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert to.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>A BackgroundImageConfig for both simple URLs and theme-specific configurations.</returns>
        /// <exception cref="JsonException">Thrown when the JSON token type is not supported.</exception>
        public override BackgroundImageConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => new BackgroundImageConfig { Url = reader.GetString() },
                JsonTokenType.StartObject => JsonSerializer.Deserialize<BackgroundImageConfig>(ref reader, OptionsWithoutThis),
                JsonTokenType.Null => null,
                _ => throw new JsonException($"Unexpected token type for background image: {reader.TokenType}")
            };
        }

        /// <summary>
        /// Writes the BackgroundImageConfig object to JSON.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The BackgroundImageConfig value to write.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, BackgroundImageConfig? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            // If only Url is set (simple background image), write as string
            if (!string.IsNullOrWhiteSpace(value.Url) &&
                string.IsNullOrWhiteSpace(value.Light) &&
                string.IsNullOrWhiteSpace(value.Dark))
            {
                writer.WriteStringValue(value.Url);
            }
            else
            {
                // Write as object for theme-specific configurations
                // Use options without this converter to avoid infinite recursion
                JsonSerializer.Serialize(writer, value, OptionsWithoutThis);
            }
        }

        #endregion

    }

}