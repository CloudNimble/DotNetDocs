using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mintlify.Core.Models;

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access
#pragma warning disable IL3050 // Members annotated with 'RequiresDynamicCodeAttribute' require dynamic access

namespace Mintlify.Core.Converters
{

    /// <summary>
    /// Converts <c>styling.codeblocks</c> values that can be a string or a theme object.
    /// </summary>
    public class CodeblocksConverter : JsonConverter<CodeblocksConfig>
    {

        #region Private Fields

        private static readonly Lazy<JsonSerializerOptions> _optionsWithoutThis = new Lazy<JsonSerializerOptions>(() =>
        {
            var options = new JsonSerializerOptions(MintlifyConstants.JsonSerializerOptions);
            for (int i = options.Converters.Count - 1; i >= 0; i--)
            {
                if (options.Converters[i] is CodeblocksConverter)
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
            return typeToConvert == typeof(CodeblocksConfig);
        }

        /// <inheritdoc />
        public override CodeblocksConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => new CodeblocksConfig { Theme = reader.GetString() },
                JsonTokenType.StartObject => ReadObject(ref reader),
                JsonTokenType.Null => null,
                _ => throw new JsonException($"Unexpected token type for styling.codeblocks: {reader.TokenType}")
            };
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, CodeblocksConfig? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            if (IsSimpleTheme(value))
            {
                writer.WriteStringValue(value.Theme);
                return;
            }

            writer.WriteStartObject();
            if (value.ThemePair is not null)
            {
                writer.WritePropertyName("theme");
                JsonSerializer.Serialize(writer, value.ThemePair, OptionsWithoutThis);
            }
            else if (!string.IsNullOrWhiteSpace(value.Theme))
            {
                writer.WriteString("theme", value.Theme);
            }

            if (value.Languages is not null)
            {
                writer.WritePropertyName("languages");
                JsonSerializer.Serialize(writer, value.Languages, OptionsWithoutThis);
            }

            writer.WriteEndObject();
        }

        #endregion

        #region Private Methods

        private static bool IsSimpleTheme(CodeblocksConfig value)
        {
            return !string.IsNullOrWhiteSpace(value.Theme) &&
                   value.ThemePair is null &&
                   value.Languages is null;
        }

        private static CodeblocksConfig ReadObject(ref Utf8JsonReader reader)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            var config = new CodeblocksConfig();

            if (root.TryGetProperty("theme", out var themeElement))
            {
                if (themeElement.ValueKind == JsonValueKind.String)
                {
                    config.Theme = themeElement.GetString();
                }
                else if (themeElement.ValueKind == JsonValueKind.Object)
                {
                    config.ThemePair = themeElement.Deserialize<ThemePairConfig>(OptionsWithoutThis);
                }
            }

            if (root.TryGetProperty("languages", out var languagesElement) &&
                languagesElement.ValueKind == JsonValueKind.Object)
            {
                config.Languages = languagesElement.Deserialize<CodeblockLanguagesConfig>(OptionsWithoutThis);
            }

            return config;
        }

        #endregion

    }

}
