using System;
using System.Collections.Generic;
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
    /// Tests for ApiConfigConverter that handles JSON conversion of API configuration properties.
    /// </summary>
    [TestClass]
    public class ApiConfigConverterTests
    {

        #region Private Fields

        private readonly JsonSerializerOptions _jsonOptions = MintlifyConstants.JsonSerializerOptions;

        #endregion

        #region Deserialization Tests

        /// <summary>
        /// Tests that ApiConfigConverter correctly deserializes string values to ApiSpecConfig.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_DeserializeString_ReturnsApiSpecConfig()
        {
            var json = """
            {
                "group": "Test Group",
                "asyncapi": "https://api.example.com/openapi.json"
            }
            """;

            var result = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.AsyncApi.Should().BeOfType<ApiSpecConfig>();
            result!.AsyncApi!.Source.Should().Be("https://api.example.com/openapi.json");
            result!.AsyncApi!.Directory.Should().BeNull();
            result!.AsyncApi!.Urls.Should().BeNull();
        }

        /// <summary>
        /// Tests that ApiConfigConverter correctly deserializes array values to ApiSpecConfig.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_DeserializeArray_ReturnsApiSpecConfig()
        {
            var json = """
            {
                "group": "Test Group",
                "openapi": [
                    "https://api.example.com/v1/openapi.json",
                    "https://api.example.com/v2/openapi.json"
                ]
            }
            """;

            var result = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.OpenApi.Should().BeOfType<ApiSpecConfig>();
            result!.OpenApi!.Urls.Should().HaveCount(2);
            result!.OpenApi!.Source.Should().BeNull();
            result!.OpenApi!.Directory.Should().BeNull();
        }

        /// <summary>
        /// Tests that ApiConfigConverter correctly deserializes object values to ApiSpecConfig.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_DeserializeObject_ReturnsApiSpecConfig()
        {
            var json = """
            {
                "group": "Test Group",
                "asyncapi": {
                    "source": "./openapi.yaml",
                    "directory": "./specs"
                }
            }
            """;

            var result = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.AsyncApi.Should().BeOfType<ApiSpecConfig>();
            result!.AsyncApi!.Source.Should().Be("./openapi.yaml");
            result!.AsyncApi!.Directory.Should().Be("./specs");
            result!.AsyncApi!.Urls.Should().BeNull();
        }

        /// <summary>
        /// Tests that an OpenAPI object with overlays deserializes onto ApiSpecConfig.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_DeserializeObjectWithOverlays_SetsOverlays()
        {
            var json = """
            {
                "group": "Test Group",
                "openapi": {
                    "source": "./openapi.yaml",
                    "directory": "./specs",
                    "overlays": ["overlay.json"]
                }
            }
            """;

            var result = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            result!.OpenApi!.Source.Should().Be("./openapi.yaml");
            result.OpenApi.Directory.Should().Be("./specs");
            result.OpenApi.Overlays.Should().Equal("overlay.json");
        }

        /// <summary>
        /// Tests that a mixed OpenAPI array of strings and objects is preserved in order.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_DeserializeMixedArray_PreservesEntries()
        {
            var json = """
            {
                "group": "Test Group",
                "openapi": [
                    "a.json",
                    {
                        "source": "b.json",
                        "overlays": ["b.overlay.json"]
                    }
                ]
            }
            """;

            var result = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            result!.OpenApi!.Specs.Should().HaveCount(2);
            result.OpenApi.Specs![0].Source.Should().Be("a.json");
            result.OpenApi.Specs[1].Source.Should().Be("b.json");
            result.OpenApi.Specs[1].Overlays.Should().Equal("b.overlay.json");
        }

        #endregion

        #region Serialization Tests

        /// <summary>
        /// Tests that ApiConfigConverter correctly serializes simple ApiSpecConfig as string.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_SerializeSimpleConfig_ReturnsString()
        {
            var config = new GroupConfig
            {
                Group = "Test Group",
                AsyncApi = new ApiSpecConfig { Source = "https://api.example.com/spec.json" }
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);

            json.Should().Contain("\"asyncapi\": \"https://api.example.com/spec.json\"");
        }

        /// <summary>
        /// Tests that ApiConfigConverter correctly serializes array ApiSpecConfig as array.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_SerializeArrayConfig_ReturnsArray()
        {
            var config = new GroupConfig
            {
                Group = "Test Group",
                OpenApi = new ApiSpecConfig { Urls = ["spec1.json", "spec2.json"] }
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);

            json.Should().Contain("\"openapi\": [")
                .And.Contain("\"spec1.json\"")
                .And.Contain("\"spec2.json\"");
        }

        /// <summary>
        /// Tests that ApiConfigConverter correctly serializes complex ApiSpecConfig as object.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_SerializeComplexConfig_ReturnsObject()
        {
            var config = new GroupConfig
            {
                Group = "Test Group",
                AsyncApi = new ApiSpecConfig
                {
                    Source = "./openapi.yaml",
                    Directory = "./specs"
                }
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);

            json.Should().Contain("\"source\": \"./openapi.yaml\"");
            json.Should().Contain("\"directory\": \"./specs\"");
        }

        /// <summary>
        /// Tests that overlays force an object write even when directory is omitted.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_SerializeOverlays_WritesObject()
        {
            var config = new GroupConfig
            {
                Group = "Test Group",
                OpenApi = new ApiSpecConfig
                {
                    Source = "openapi.yaml",
                    Overlays = []
                }
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);

            json.Should().Contain("\"source\": \"openapi.yaml\"");
            json.Should().Contain("\"overlays\"");
        }

        /// <summary>
        /// Tests that a mixed Specs list writes strings and objects in order.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_SerializeMixedSpecs_WritesArray()
        {
            var config = new GroupConfig
            {
                Group = "Test Group",
                OpenApi = new ApiSpecConfig
                {
                    Specs =
                    [
                        new ApiSpecConfig { Source = "a.json" },
                        new ApiSpecConfig { Source = "b.json", Overlays = ["b.overlay.json"] }
                    ]
                }
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);

            json.Should().Contain("\"a.json\"");
            json.Should().Contain("\"b.json\"");
            json.Should().Contain("\"b.overlay.json\"");
        }

        #endregion

        #region Implicit Conversion Tests

        /// <summary>
        /// Tests ApiSpecConfig implicit conversions work correctly.
        /// </summary>
        [TestMethod]
        public void ApiSpecConfig_ImplicitConversions_WorkCorrectly()
        {
            // Test string to ApiSpecConfig
            ApiSpecConfig? apiFromString = "https://api.example.com/spec.json";
            apiFromString.Should().NotBeNull();
            apiFromString!.Source.Should().Be("https://api.example.com/spec.json");

            // Test List<string> to ApiSpecConfig
            ApiSpecConfig? apiFromList = new List<string> { "spec1.json", "spec2.json" };
            apiFromList.Should().NotBeNull();
            apiFromList!.Urls.Should().HaveCount(2);

            // Test ApiSpecConfig to string
            string? urlFromApi = apiFromString;
            urlFromApi.Should().Be("https://api.example.com/spec.json");

            // Test ApiSpecConfig to List<string>
            List<string>? urlsFromApi = apiFromList;
            urlsFromApi.Should().HaveCount(2);

            List<string>? urlsFromSource = new ApiSpecConfig { Source = "only.json" };
            urlsFromSource.Should().Equal("only.json");

            List<string>? urlsFromNull = (ApiSpecConfig?)null;
            urlsFromNull.Should().BeNull();
        }

        /// <summary>
        /// Tests ApiSpecConfig ToString method.
        /// </summary>
        [TestMethod]
        public void ApiSpecConfig_ToString_ReturnsSource()
        {
            var api = new ApiSpecConfig { Source = "test-spec.json" };

            api.ToString().Should().Be("test-spec.json");
        }

        /// <summary>
        /// Tests ApiSpecConfig ToString method with URL list.
        /// </summary>
        [TestMethod]
        public void ApiSpecConfig_ToStringWithUrls_ReturnsFirstUrl()
        {
            var api = new ApiSpecConfig { Urls = ["spec1.json", "spec2.json"] };

            api.ToString().Should().Be("spec1.json");
        }

        /// <summary>
        /// Tests that ToString returns empty when no source or URLs are set.
        /// </summary>
        [TestMethod]
        public void ApiSpecConfig_ToString_EmptyWhenUnset()
        {
            new ApiSpecConfig().ToString().Should().BeEmpty();
        }

        #endregion

        #region Edge Cases

        /// <summary>
        /// Tests CanConvert for ApiSpecConfig and unrelated types.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_CanConvert_OnlyApiSpecConfig()
        {
            var converter = new ApiConfigConverter();

            converter.CanConvert(typeof(ApiSpecConfig)).Should().BeTrue();
            converter.CanConvert(typeof(string)).Should().BeFalse();
        }

        /// <summary>
        /// Tests that a null token deserializes to a null spec.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_DeserializeNull_ReturnsNull()
        {
            var json = """
            {
                "group": "Test Group",
                "openapi": null
            }
            """;

            var result = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            result!.OpenApi.Should().BeNull();

            var reader = new Utf8JsonReader("null"u8.ToArray());
            reader.Read();
            new ApiConfigConverter().Read(ref reader, typeof(ApiSpecConfig), _jsonOptions).Should().BeNull();
        }

        /// <summary>
        /// Tests that an unsupported JSON token throws.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_DeserializeNumber_Throws()
        {
            var json = """
            {
                "group": "Test Group",
                "openapi": 1
            }
            """;

            var act = () => JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            act.Should().Throw<JsonException>();
        }

        /// <summary>
        /// Tests that an unsupported array token throws.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_DeserializeArrayOfNumbers_Throws()
        {
            var json = """
            {
                "group": "Test Group",
                "openapi": [1]
            }
            """;

            var act = () => JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            act.Should().Throw<JsonException>();
        }

        /// <summary>
        /// Tests that blank strings in an OpenAPI array are skipped.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_DeserializeArrayWithBlankString_SkipsBlank()
        {
            var json = """
            {
                "group": "Test Group",
                "openapi": ["a.json", "  "]
            }
            """;

            var result = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            result!.OpenApi!.Urls.Should().Equal("a.json");
        }

        /// <summary>
        /// Tests that writing a null spec emits JSON null.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_WriteNull_WritesNullToken()
        {
            var converter = new ApiConfigConverter();
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            converter.Write(writer, null, _jsonOptions);
            writer.Flush();

            Encoding.UTF8.GetString(stream.ToArray()).Should().Be("null");
        }

        /// <summary>
        /// Tests NormalizeExpanded mappings including null and boolean-like strings.
        /// </summary>
        [TestMethod]
        public void ApiParamsConfig_NormalizeExpanded_MapsBooleanLikeValues()
        {
            ApiParamsConfig.NormalizeExpanded(null).Should().BeNull();
            ApiParamsConfig.NormalizeExpanded("   ").Should().BeNull();
            ApiParamsConfig.NormalizeExpanded("true").Should().Be("all");
            ApiParamsConfig.NormalizeExpanded("FALSE").Should().Be("closed");
            ApiParamsConfig.NormalizeExpanded("all").Should().Be("all");
            ApiParamsConfig.NormalizeExpanded("closed").Should().Be("closed");
        }

        #endregion

    }

}