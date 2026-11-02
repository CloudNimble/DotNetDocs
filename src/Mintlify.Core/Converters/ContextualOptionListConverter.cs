using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Converters
{

    /// <summary>
    /// Converts a list of contextual options that mix built-in names and custom objects.
    /// </summary>
    public class ContextualOptionListConverter : JsonConverter<List<object>>
    {

        #region Private Fields

        private static readonly ContextualOptionConverter OptionConverter = new ContextualOptionConverter();

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override List<object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected StartArray token for contextual options.");
            }

            var list = new List<object>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }

                var item = OptionConverter.Read(ref reader, typeof(object), options);
                if (item is not null)
                {
                    list.Add(item);
                }
            }

            return list;
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, List<object> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
            {
                OptionConverter.Write(writer, item, options);
            }

            writer.WriteEndArray();
        }

        #endregion

    }

}
