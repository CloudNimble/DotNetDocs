using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mintlify.Core.Models;

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access
#pragma warning disable IL3050 // Members annotated with 'RequiresDynamicCodeAttribute' require dynamic access

namespace Mintlify.Core.Converters
{

    /// <summary>
    /// Handles JSON conversion for navigation page items that can be either strings or GroupConfig objects.
    /// </summary>
    /// <remarks>
    /// This converter is used for polymorphic navigation page properties in Mintlify configuration objects.
    /// It properly deserializes JSON into strongly-typed objects instead of JsonElement instances.
    /// </remarks>
    public class NavigationPageConverter : JsonConverter<object>
    {

        #region Public Methods

        /// <summary>
        /// Determines whether the specified type can be converted by this converter.
        /// </summary>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <returns>True if the type is object; otherwise, false.</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(object);
        }

        /// <summary>
        /// Reads and converts the JSON to a navigation page object.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert to.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>A string for page paths or a GroupConfig for nested navigation groups.</returns>
        /// <exception cref="JsonException">Thrown when the JSON token type is not supported.</exception>
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.StartObject => JsonSerializer.Deserialize<GroupConfig>(ref reader, options),
                _ => throw new JsonException($"Unexpected token type for navigation page: {reader.TokenType}")
            };
        }

        /// <summary>
        /// Writes the navigation page object to JSON.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="options">The serializer options.</param>
        /// <exception cref="JsonException">Thrown when the value type is not supported.</exception>
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case string stringValue:
                    writer.WriteStringValue(stringValue);
                    break;

                case GroupConfig groupValue:
                    JsonSerializer.Serialize(writer, groupValue, options);
                    break;

                default:
                    throw new JsonException(
                        $"Unsupported navigation page value type. Expected string or GroupConfig.");
            }
        }

        #endregion

    }

}
