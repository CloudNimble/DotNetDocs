using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mintlify.Core.Models;

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access
#pragma warning disable IL3050 // Members annotated with 'RequiresDynamicCodeAttribute' require dynamic access

namespace Mintlify.Core.Converters
{

    /// <summary>
    /// Custom JSON converter for navigation that outputs simple array format for Mintlify compatibility.
    /// </summary>
    public class NavigationJsonConverter : JsonConverter<NavigationConfig>
    {

        #region Private Fields

        private static readonly NavigationPageListConverter PageListConverter = new NavigationPageListConverter();

        #endregion

        #region Public Methods

        /// <summary>
        /// Reads navigation from JSON, supporting both array and object formats.
        /// </summary>
        public override NavigationConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var pages = PageListConverter.Read(ref reader, typeof(List<object>), options);
                return pages is not null ? new NavigationConfig { Pages = pages } : null;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                return null;
            }

            var nav = new NavigationConfig();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    continue;
                }

                if (reader.ValueTextEquals("pages"))
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        nav.Pages = PageListConverter.Read(ref reader, typeof(List<object>), options);
                    }
                }
                else if (reader.ValueTextEquals("groups"))
                {
                    nav.Groups = ReadArray<GroupConfig>(ref reader, options) ?? nav.Groups;
                }
                else if (reader.ValueTextEquals("tabs"))
                {
                    nav.Tabs = ReadArray<TabConfig>(ref reader, options) ?? nav.Tabs;
                }
                else if (reader.ValueTextEquals("products"))
                {
                    nav.Products = ReadArray<ProductConfig>(ref reader, options) ?? nav.Products;
                }
                else if (reader.ValueTextEquals("anchors"))
                {
                    nav.Anchors = ReadArray<AnchorConfig>(ref reader, options) ?? nav.Anchors;
                }
                else if (reader.ValueTextEquals("dropdowns"))
                {
                    nav.Dropdowns = ReadArray<DropdownConfig>(ref reader, options) ?? nav.Dropdowns;
                }
                else if (reader.ValueTextEquals("languages"))
                {
                    nav.Languages = ReadArray<LanguageConfig>(ref reader, options) ?? nav.Languages;
                }
                else if (reader.ValueTextEquals("versions"))
                {
                    nav.Versions = ReadArray<VersionConfig>(ref reader, options) ?? nav.Versions;
                }
                else if (reader.ValueTextEquals("global"))
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        nav.Global = JsonSerializer.Deserialize<GlobalNavigationConfig>(ref reader, options);
                    }
                }
                else if (reader.ValueTextEquals("graphql"))
                {
                    reader.Read();
                    nav.Graphql = JsonSerializer.Deserialize<SpecRefConfig>(ref reader, options);
                }
                else if (reader.ValueTextEquals("mcp"))
                {
                    reader.Read();
                    nav.Mcp = JsonSerializer.Deserialize<SpecRefConfig>(ref reader, options);
                }
                else if (reader.ValueTextEquals("sdk"))
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        nav.Sdk = JsonSerializer.Deserialize<SdkRefConfig>(ref reader, options);
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
                else if (reader.ValueTextEquals("directory"))
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        nav.Directory = reader.GetString();
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
                else
                {
                    var propertyName = reader.GetString();
                    reader.Read();
                    if (!TryReadIgnoreCase(ref reader, nav, propertyName, options))
                    {
                        reader.Skip();
                    }
                }
            }

            return nav;
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
                PageListConverter.Write(writer, value.Pages, options);
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

            if (value.Graphql is not null)
            {
                writer.WritePropertyName("graphql");
                JsonSerializer.Serialize(writer, value.Graphql, options);
            }

            if (value.Mcp is not null)
            {
                writer.WritePropertyName("mcp");
                JsonSerializer.Serialize(writer, value.Mcp, options);
            }

            if (value.Sdk is not null)
            {
                writer.WritePropertyName("sdk");
                JsonSerializer.Serialize(writer, value.Sdk, options);
            }

            if (!string.IsNullOrWhiteSpace(value.Directory))
            {
                writer.WriteString("directory", value.Directory);
            }

            writer.WriteEndObject();
        }

        #endregion

        #region Private Methods

        private static List<T>? ReadArray<T>(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            reader.Read();
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                return JsonSerializer.Deserialize<List<T>>(ref reader, options);
            }

            return null;
        }

        private static bool TryReadIgnoreCase(ref Utf8JsonReader reader, NavigationConfig nav, string? propertyName, JsonSerializerOptions options)
        {
            if (propertyName is null)
            {
                return false;
            }

            if (propertyName.Equals("pages", StringComparison.OrdinalIgnoreCase))
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    nav.Pages = PageListConverter.Read(ref reader, typeof(List<object>), options);
                }

                return true;
            }

            if (propertyName.Equals("groups", StringComparison.OrdinalIgnoreCase))
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    nav.Groups = JsonSerializer.Deserialize<List<GroupConfig>>(ref reader, options);
                }

                return true;
            }

            if (propertyName.Equals("tabs", StringComparison.OrdinalIgnoreCase))
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    nav.Tabs = JsonSerializer.Deserialize<List<TabConfig>>(ref reader, options);
                }

                return true;
            }

            if (propertyName.Equals("products", StringComparison.OrdinalIgnoreCase))
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    nav.Products = JsonSerializer.Deserialize<List<ProductConfig>>(ref reader, options);
                }

                return true;
            }

            if (propertyName.Equals("anchors", StringComparison.OrdinalIgnoreCase))
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    nav.Anchors = JsonSerializer.Deserialize<List<AnchorConfig>>(ref reader, options);
                }

                return true;
            }

            if (propertyName.Equals("dropdowns", StringComparison.OrdinalIgnoreCase))
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    nav.Dropdowns = JsonSerializer.Deserialize<List<DropdownConfig>>(ref reader, options);
                }

                return true;
            }

            if (propertyName.Equals("languages", StringComparison.OrdinalIgnoreCase))
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    nav.Languages = JsonSerializer.Deserialize<List<LanguageConfig>>(ref reader, options);
                }

                return true;
            }

            if (propertyName.Equals("versions", StringComparison.OrdinalIgnoreCase))
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    nav.Versions = JsonSerializer.Deserialize<List<VersionConfig>>(ref reader, options);
                }

                return true;
            }

            if (propertyName.Equals("global", StringComparison.OrdinalIgnoreCase))
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    nav.Global = JsonSerializer.Deserialize<GlobalNavigationConfig>(ref reader, options);
                }

                return true;
            }

            if (propertyName.Equals("graphql", StringComparison.OrdinalIgnoreCase))
            {
                nav.Graphql = JsonSerializer.Deserialize<SpecRefConfig>(ref reader, options);
                return true;
            }

            if (propertyName.Equals("mcp", StringComparison.OrdinalIgnoreCase))
            {
                nav.Mcp = JsonSerializer.Deserialize<SpecRefConfig>(ref reader, options);
                return true;
            }

            if (propertyName.Equals("sdk", StringComparison.OrdinalIgnoreCase))
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    nav.Sdk = JsonSerializer.Deserialize<SdkRefConfig>(ref reader, options);
                }
                else
                {
                    reader.Skip();
                }

                return true;
            }

            if (propertyName.Equals("directory", StringComparison.OrdinalIgnoreCase))
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    nav.Directory = reader.GetString();
                }
                else
                {
                    reader.Skip();
                }

                return true;
            }

            return false;
        }

        #endregion

    }

}
