using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mintlify.Core.Models;

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access
#pragma warning disable IL3050 // Members annotated with 'RequiresDynamicCodeAttribute' require dynamic access

namespace Mintlify.Core.Converters
{

    /// <summary>
    /// Converts contextual option href values that can be a string or a base/query object.
    /// </summary>
    public class ContextualHrefConverter : JsonConverter<ContextualHrefConfig>
    {

        #region Private Fields

        private static readonly Lazy<JsonSerializerOptions> _optionsWithoutThis = new Lazy<JsonSerializerOptions>(() =>
        {
            var options = new JsonSerializerOptions(MintlifyConstants.JsonSerializerOptions);
            for (int i = options.Converters.Count - 1; i >= 0; i--)
            {
                if (options.Converters[i] is ContextualHrefConverter)
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
        public override ContextualHrefConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => new ContextualHrefConfig { Url = reader.GetString() },
                JsonTokenType.StartObject => JsonSerializer.Deserialize<ContextualHrefConfig>(ref reader, OptionsWithoutThis),
                JsonTokenType.Null => null,
                _ => throw new JsonException($"Unexpected token type for contextual href: {reader.TokenType}")
            };
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, ContextualHrefConfig? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            if (!string.IsNullOrWhiteSpace(value.Url) && value.Base is null && value.Query is null)
            {
                writer.WriteStringValue(value.Url);
                return;
            }

            JsonSerializer.Serialize(writer, value, OptionsWithoutThis);
        }

        #endregion

    }

}
