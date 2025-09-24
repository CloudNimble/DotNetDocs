using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
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

        #endregion

    }

}