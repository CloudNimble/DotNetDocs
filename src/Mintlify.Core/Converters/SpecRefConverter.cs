using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mintlify.Core.Models;

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access
#pragma warning disable IL3050 // Members annotated with 'RequiresDynamicCodeAttribute' require dynamic access

namespace Mintlify.Core.Converters
{

    /// <summary>
    /// Converts GraphQL and MCP specification references that can be a string or an object.
    /// </summary>
    public class SpecRefConverter : JsonConverter<SpecRefConfig>
    {

        #region Private Fields

        private static readonly Lazy<JsonSerializerOptions> _optionsWithoutThis = new Lazy<JsonSerializerOptions>(() =>
        {
            var options = new JsonSerializerOptions(MintlifyConstants.JsonSerializerOptions);
            for (int i = options.Converters.Count - 1; i >= 0; i--)
            {
                if (options.Converters[i] is SpecRefConverter)
                {
                    options.Converters.RemoveAt(i);
                }
            }

            return options;
        });

        internal static JsonSerializerOptions OptionsWithoutThis => _optionsWithoutThis.Value;

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(SpecRefConfig);
        }

        /// <inheritdoc />
        public override SpecRefConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => new SpecRefConfig { Source = reader.GetString() },
                JsonTokenType.StartObject => JsonSerializer.Deserialize<SpecRefConfig>(ref reader, OptionsWithoutThis),
                JsonTokenType.Null => null,
                _ => throw new JsonException($"Unexpected token type for specification reference: {reader.TokenType}")
            };
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, SpecRefConfig? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            if (!string.IsNullOrWhiteSpace(value.Source) && string.IsNullOrWhiteSpace(value.Directory))
            {
                writer.WriteStringValue(value.Source);
                return;
            }

            JsonSerializer.Serialize(writer, value, OptionsWithoutThis);
        }

        #endregion

    }

}
