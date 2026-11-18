using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mintlify.Core.Models;

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access
#pragma warning disable IL3050 // Members annotated with 'RequiresDynamicCodeAttribute' require dynamic access

namespace Mintlify.Core.Converters
{

    /// <summary>
    /// Converts a contextual option that can be a built-in name or a custom object.
    /// </summary>
    public class ContextualOptionConverter : JsonConverter<object>
    {

        #region Public Methods

        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(object);
        }

        /// <inheritdoc />
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.StartObject => JsonSerializer.Deserialize<ContextualOptionConfig>(ref reader, options),
                JsonTokenType.Null => null,
                _ => throw new JsonException($"Unexpected token type for contextual option: {reader.TokenType}")
            };
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case string name:
                    writer.WriteStringValue(name);
                    break;
                case ContextualOptionConfig option:
                    JsonSerializer.Serialize(writer, option, options);
                    break;
                default:
                    throw new JsonException("Unsupported contextual option type. Expected string or ContextualOptionConfig.");
            }
        }

        #endregion

    }

}
