using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mintlify.Core.Converters
{

    /// <summary>
    /// Handles JSON conversion for lists of navigation page items that can be either strings or GroupConfig objects.
    /// </summary>
    /// <remarks>
    /// This converter uses a static NavigationPageConverter instance for efficient conversion of all items
    /// in the list without creating new converter instances for each item.
    /// </remarks>
    public class NavigationPageListConverter : JsonConverter<List<object>>
    {

        #region Private Fields

        private static readonly NavigationPageConverter PageConverter = new NavigationPageConverter();

        #endregion

        #region Public Methods

        /// <summary>
        /// Reads and converts the JSON array to a list of navigation page objects.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert to.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>A list containing string and GroupConfig objects.</returns>
        /// <exception cref="JsonException">Thrown when the JSON token is not a StartArray.</exception>
        public override List<object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected StartArray token for navigation pages");

            var list = new List<object>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                var item = PageConverter.Read(ref reader, typeof(object), options);
                if (item is not null)
                {
                    list.Add(item);
                }
            }

            return list;
        }

        /// <summary>
        /// Writes the list of navigation page objects to JSON array.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The list of navigation page objects to write.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, List<object> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach (var item in value)
            {
                PageConverter.Write(writer, item, options);
            }

            writer.WriteEndArray();
        }

        #endregion

    }

}