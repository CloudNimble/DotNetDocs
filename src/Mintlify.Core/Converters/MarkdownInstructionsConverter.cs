using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access
#pragma warning disable IL3050 // Members annotated with 'RequiresDynamicCodeAttribute' require dynamic access

namespace Mintlify.Core.Converters
{

    /// <summary>
    /// Converts <c>markdown.instructions</c> values that can be a string or an array of strings.
    /// </summary>
    public class MarkdownInstructionsConverter : JsonConverter<List<string>>
    {

        #region Private Fields

        private static readonly Lazy<JsonSerializerOptions> _optionsWithoutThis = new Lazy<JsonSerializerOptions>(() =>
        {
            var options = new JsonSerializerOptions(MintlifyConstants.JsonSerializerOptions);
            for (int i = options.Converters.Count - 1; i >= 0; i--)
            {
                if (options.Converters[i] is MarkdownInstructionsConverter)
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
            return typeToConvert == typeof(List<string>);
        }

        /// <inheritdoc />
        public override List<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => new List<string> { reader.GetString() ?? string.Empty },
                JsonTokenType.StartArray => JsonSerializer.Deserialize<List<string>>(ref reader, OptionsWithoutThis),
                JsonTokenType.Null => null,
                _ => throw new JsonException($"Unexpected token type for markdown.instructions: {reader.TokenType}")
            };
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, List<string>? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            if (value.Count == 1)
            {
                writer.WriteStringValue(value[0]);
                return;
            }

            JsonSerializer.Serialize(writer, value, OptionsWithoutThis);
        }

        #endregion

    }

}
