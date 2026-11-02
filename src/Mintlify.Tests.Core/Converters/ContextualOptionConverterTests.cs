using System.IO;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Converters;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core.Converters
{

    /// <summary>
    /// Tests for contextual option and href converters.
    /// </summary>
    [TestClass]
    public class ContextualOptionConverterTests
    {

        #region Private Fields

        private readonly JsonSerializerOptions _jsonOptions = MintlifyConstants.JsonSerializerOptions;

        #endregion

        #region Public Methods

        /// <summary>
        /// Tests that a builtin name round-trips as a string.
        /// </summary>
        [TestMethod]
        public void ContextualOptionConverter_RoundTrip_BuiltinString()
        {
            var json = """{ "options": ["copy", "grok"] }""";

            var result = JsonSerializer.Deserialize<ContextualConfig>(json, _jsonOptions);

            result!.Options.Should().Equal("copy", "grok");

            var written = JsonSerializer.Serialize(result, _jsonOptions);
            written.Should().Contain("\"copy\"");
            written.Should().Contain("\"grok\"");
        }

        /// <summary>
        /// Tests that a custom option with a string href round-trips.
        /// </summary>
        [TestMethod]
        public void ContextualOptionConverter_RoundTrip_StringHref()
        {
            var json = """
            {
                "options": [
                    {
                        "title": "Ask Grok",
                        "description": "Open in Grok",
                        "icon": "sparkles",
                        "href": "https://grok.com"
                    }
                ]
            }
            """;

            var result = JsonSerializer.Deserialize<ContextualConfig>(json, _jsonOptions);
            var option = result!.Options.Should().ContainSingle().Which.Should().BeOfType<ContextualOptionConfig>().Subject;
            option.Title.Should().Be("Ask Grok");
            option.Href!.Url.Should().Be("https://grok.com");
            option.Icon!.Name.Should().Be("sparkles");

            var written = JsonSerializer.Serialize(result, _jsonOptions);
            written.Should().Contain("\"https://grok.com\"");
            written.Should().NotContain("\"base\"");
        }

        /// <summary>
        /// Tests that a custom option with a query href round-trips as an object.
        /// </summary>
        [TestMethod]
        public void ContextualOptionConverter_RoundTrip_QueryHref()
        {
            var json = """
            {
                "options": [
                    {
                        "title": "Search",
                        "description": "Search this page",
                        "href": {
                            "base": "https://x.com/search",
                            "query": [
                                { "key": "q", "value": "$page" }
                            ]
                        }
                    }
                ]
            }
            """;

            var result = JsonSerializer.Deserialize<ContextualConfig>(json, _jsonOptions);
            var option = result!.Options.Should().ContainSingle().Which.Should().BeOfType<ContextualOptionConfig>().Subject;
            option.Href!.Base.Should().Be("https://x.com/search");
            option.Href.Query.Should().ContainSingle().Which.Value.Should().Be("$page");

            var written = JsonSerializer.Serialize(result, _jsonOptions);
            written.Should().Contain("\"base\": \"https://x.com/search\"");
            written.Should().Contain("\"$page\"");
        }

        /// <summary>
        /// Tests that a non-array options value throws.
        /// </summary>
        [TestMethod]
        public void ContextualOptionListConverter_NonArray_Throws()
        {
            var json = """{ "options": "copy" }""";

            var act = () => JsonSerializer.Deserialize<ContextualConfig>(json, _jsonOptions);

            act.Should().Throw<JsonException>();
        }

        /// <summary>
        /// Tests that an unexpected option token throws.
        /// </summary>
        [TestMethod]
        public void ContextualOptionConverter_NumberToken_Throws()
        {
            var json = """{ "options": [1] }""";

            var act = () => JsonSerializer.Deserialize<ContextualConfig>(json, _jsonOptions);

            act.Should().Throw<JsonException>();
        }

        /// <summary>
        /// Tests that an unexpected href token throws.
        /// </summary>
        [TestMethod]
        public void ContextualHrefConverter_NumberToken_Throws()
        {
            var json = """
            {
                "options": [
                    {
                        "title": "Ask",
                        "description": "Go",
                        "href": 12
                    }
                ]
            }
            """;

            var act = () => JsonSerializer.Deserialize<ContextualConfig>(json, _jsonOptions);

            act.Should().Throw<JsonException>();
        }

        /// <summary>
        /// Tests that writing an unsupported option type throws.
        /// </summary>
        [TestMethod]
        public void ContextualOptionConverter_WriteUnsupportedType_Throws()
        {
            var converter = new ContextualOptionConverter();
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            var act = () => converter.Write(writer, 42, _jsonOptions);

            act.Should().Throw<JsonException>();
        }

        /// <summary>
        /// Tests that CanConvert only matches object.
        /// </summary>
        [TestMethod]
        public void ContextualOptionConverter_CanConvert_ObjectOnly()
        {
            var converter = new ContextualOptionConverter();

            converter.CanConvert(typeof(object)).Should().BeTrue();
            converter.CanConvert(typeof(string)).Should().BeFalse();
            converter.CanConvert(typeof(ContextualOptionConfig)).Should().BeFalse();
        }

        /// <summary>
        /// Tests that a null href writes null.
        /// </summary>
        [TestMethod]
        public void ContextualHrefConverter_WriteNull_WritesNull()
        {
            var converter = new ContextualHrefConverter();
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);
            writer.WriteStartObject();
            writer.WritePropertyName("href");
            converter.Write(writer, null, _jsonOptions);
            writer.WriteEndObject();
            writer.Flush();

            Encoding.UTF8.GetString(stream.ToArray()).Should().Contain("\"href\":null");
        }

        #endregion

    }

}
