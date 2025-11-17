using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mintlify.Core.Models;

namespace Mintlify.Core.Converters
{

    /// <summary>
    /// Custom JSON converter for navigation that outputs simple array format for Mintlify compatibility.
    /// </summary>
    public class NavigationJsonConverter : JsonConverter<NavigationConfig>
    {

        /// <summary>
        /// Reads navigation from JSON, supporting both array and object formats.
        /// </summary>
        public override NavigationConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                // Handle array format: "navigation": [...]
                // Use the NavigationPageListConverter to properly deserialize the pages
                var converter = new NavigationPageListConverter();
                var pages = converter.Read(ref reader, typeof(List<object>), options);
                return pages is not null ? new NavigationConfig { Pages = pages } : null;
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                // Handle object format: "navigation": { "pages": [...], ... }
                // We need to manually deserialize to avoid infinite recursion
                var nav = new NavigationConfig();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        break;

                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = reader.GetString();
                        reader.Read();

                        switch (propertyName?.ToLowerInvariant())
                        {
                            case "pages":
                                if (reader.TokenType == JsonTokenType.StartArray)
                                {
                                    var converter = new NavigationPageListConverter();
                                    nav.Pages = converter.Read(ref reader, typeof(List<object>), options);
                                }
                                break;
                            case "groups":
                                if (reader.TokenType == JsonTokenType.StartArray)
                                {
                                    nav.Groups = JsonSerializer.Deserialize<List<GroupConfig>>(ref reader, options);
                                }
                                break;
                            case "tabs":
                                if (reader.TokenType == JsonTokenType.StartArray)
                                {
                                    nav.Tabs = JsonSerializer.Deserialize<List<TabConfig>>(ref reader, options);
                                }
                                break;
                            case "products":
                                if (reader.TokenType == JsonTokenType.StartArray)
                                {
                                    nav.Products = JsonSerializer.Deserialize<List<ProductConfig>>(ref reader, options);
                                }
                                break;
                            case "anchors":
                                if (reader.TokenType == JsonTokenType.StartArray)
                                {
                                    nav.Anchors = JsonSerializer.Deserialize<List<AnchorConfig>>(ref reader, options);
                                }
                                break;
                            default:
                                reader.Skip();
                                break;
                        }
                    }
                }

                return nav;
            }

            return null;
        }

        /// <summary>
        /// Writes navigation to JSON in the appropriate format for Mintlify compatibility.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, NavigationConfig value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            // Manually write the object to avoid infinite recursion
            writer.WriteStartObject();

            if (value.Pages is not null && value.Pages.Count > 0)
            {
                writer.WritePropertyName("pages");
                var converter = new NavigationPageListConverter();
                converter.Write(writer, value.Pages, options);
            }

            if (value.Groups is not null)
            {
                writer.WritePropertyName("groups");
                JsonSerializer.Serialize(writer, value.Groups, options);
            }

            if (value.Tabs is not null)
            {
                writer.WritePropertyName("tabs");
                JsonSerializer.Serialize(writer, value.Tabs, options);
            }

            if (value.Products is not null)
            {
                writer.WritePropertyName("products");
                JsonSerializer.Serialize(writer, value.Products, options);
            }

            if (value.Anchors is not null)
            {
                writer.WritePropertyName("anchors");
                JsonSerializer.Serialize(writer, value.Anchors, options);
            }

            if (value.Dropdowns is not null)
            {
                writer.WritePropertyName("dropdowns");
                JsonSerializer.Serialize(writer, value.Dropdowns, options);
            }

            if (value.Languages is not null)
            {
                writer.WritePropertyName("languages");
                JsonSerializer.Serialize(writer, value.Languages, options);
            }

            if (value.Versions is not null)
            {
                writer.WritePropertyName("versions");
                JsonSerializer.Serialize(writer, value.Versions, options);
            }

            if (value.Global is not null)
            {
                writer.WritePropertyName("global");
                JsonSerializer.Serialize(writer, value.Global, options);
            }

            writer.WriteEndObject();
        }

    }

}
