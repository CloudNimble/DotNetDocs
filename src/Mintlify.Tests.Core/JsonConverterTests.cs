using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core
{

    /// <summary>
    /// Tests for Mintlify JSON converters that handle polymorphic serialization/deserialization.
    /// </summary>
    [TestClass]
    public class JsonConverterTests
    {

        #region Private Fields

        private readonly JsonSerializerOptions _jsonOptions = MintlifyConstants.JsonSerializerOptions;

        #endregion

        #region NavigationPageConverter Tests

        /// <summary>
        /// Tests that NavigationPageConverter correctly deserializes string values through GroupConfig.Pages.
        /// </summary>
        [TestMethod]
        public void NavigationPageConverter_DeserializeString_ReturnsString()
        {
            var json = """
            {
                "group": "Test Group",
                "pages": ["cli/index"]
            }
            """;

            var result = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Pages.Should().HaveCount(1);
            result!.Pages![0].Should().BeOfType<string>();
            result!.Pages![0].Should().Be("cli/index");
        }

        /// <summary>
        /// Tests that NavigationPageConverter correctly deserializes nested GroupConfig objects through GroupConfig.Pages.
        /// </summary>
        [TestMethod]
        public void NavigationPageConverter_DeserializeGroupConfig_ReturnsGroupConfig()
        {
            var json = """
            {
                "group": "Parent Group",
                "pages": [
                    "simple-page",
                    {
                        "group": "CLI Tools",
                        "pages": ["cli/index", "cli/commands"]
                    }
                ]
            }
            """;

            var result = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Group.Should().Be("Parent Group");
            result!.Pages.Should().HaveCount(2);
            result!.Pages![0].Should().BeOfType<string>();
            result!.Pages![1].Should().BeOfType<GroupConfig>();
            var nestedGroup = result!.Pages![1] as GroupConfig;
            nestedGroup!.Group.Should().Be("CLI Tools");
            nestedGroup!.Pages!.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that NavigationPageConverter correctly serializes string values.
        /// </summary>
        [TestMethod]
        public void NavigationPageConverter_SerializeString_ReturnsJsonString()
        {
            var value = "cli/index";

            var json = JsonSerializer.Serialize<object>(value, _jsonOptions);

            json.Should().Be("\"cli/index\"");
        }

        /// <summary>
        /// Tests that NavigationPageConverter correctly serializes GroupConfig objects.
        /// </summary>
        [TestMethod]
        public void NavigationPageConverter_SerializeGroupConfig_ReturnsJsonObject()
        {
            var value = new GroupConfig
            {
                Group = "CLI Tools",
                Pages = new List<object> { "cli/index", "cli/commands" }
            };

            var json = JsonSerializer.Serialize<object>(value, _jsonOptions);

            json.Should().Contain("\"group\": \"CLI Tools\"");
            json.Should().Contain("\"pages\"");
        }

        #endregion

        #region NavigationPageListConverter Tests

        /// <summary>
        /// Tests that NavigationPageListConverter correctly deserializes mixed arrays.
        /// </summary>
        [TestMethod]
        public void NavigationPageListConverter_DeserializeMixedArray_ReturnsCorrectTypes()
        {
            var json = """
            [
                "simple-page",
                {
                    "group": "Advanced",
                    "pages": ["advanced/topic1", "advanced/topic2"]
                }
            ]
            """;

            var result = JsonSerializer.Deserialize<List<object>>(json, _jsonOptions);

            result.Should().HaveCount(2);
            result![0].Should().BeOfType<string>().And.Be("simple-page");
            result![1].Should().BeOfType<GroupConfig>();
            var group = result![1] as GroupConfig;
            group!.Group.Should().Be("Advanced");
        }

        /// <summary>
        /// Tests that NavigationPageListConverter correctly serializes mixed arrays.
        /// </summary>
        [TestMethod]
        public void NavigationPageListConverter_SerializeMixedArray_ReturnsJsonArray()
        {
            var value = new List<object>
            {
                "simple-page",
                new GroupConfig
                {
                    Group = "Advanced",
                    Pages = new List<object> { "advanced/topic1" }
                }
            };

            var json = JsonSerializer.Serialize(value, _jsonOptions);

            json.Should().Contain("\"simple-page\"");
            json.Should().Contain("\"group\": \"Advanced\"");
        }

        #endregion

        #region IconConverter Tests

        /// <summary>
        /// Tests that IconConverter correctly deserializes string values through GroupConfig.Icon.
        /// </summary>
        [TestMethod]
        public void IconConverter_DeserializeString_ReturnsString()
        {
            var json = """
            {
                "group": "Test Group",
                "icon": "home"
            }
            """;

            var result = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Icon.Should().BeOfType<string>();
            result!.Icon.Should().Be("home");
        }

        /// <summary>
        /// Tests that IconConverter correctly deserializes object values through GroupConfig.Icon.
        /// </summary>
        [TestMethod]
        public void IconConverter_DeserializeObject_ReturnsDictionary()
        {
            var json = """
            {
                "group": "Test Group",
                "icon": {
                    "light": "icon-light.svg",
                    "dark": "icon-dark.svg"
                }
            }
            """;

            var result = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Icon.Should().BeOfType<Dictionary<string, object>>();
            var dict = result!.Icon as Dictionary<string, object>;
            dict!.Should().ContainKey("light");
            dict!.Should().ContainKey("dark");
        }

        #endregion

        #region ApiConfigConverter Tests

        /// <summary>
        /// Tests that ApiConfigConverter correctly deserializes string values through GroupConfig.AsyncApi.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_DeserializeString_ReturnsString()
        {
            var json = """
            {
                "group": "Test Group",
                "asyncapi": "https://api.example.com/openapi.json"
            }
            """;

            var result = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.AsyncApi.Should().BeOfType<string>();
            result!.AsyncApi.Should().Be("https://api.example.com/openapi.json");
        }

        /// <summary>
        /// Tests that ApiConfigConverter correctly deserializes array values through GroupConfig.OpenApi.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_DeserializeArray_ReturnsStringList()
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
            result!.OpenApi.Should().BeOfType<List<string>>();
            var list = result!.OpenApi as List<string>;
            list!.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that ApiConfigConverter correctly deserializes object values through GroupConfig.AsyncApi.
        /// </summary>
        [TestMethod]
        public void ApiConfigConverter_DeserializeObject_ReturnsDictionary()
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
            result!.AsyncApi.Should().BeOfType<Dictionary<string, object>>();
            var dict = result!.AsyncApi as Dictionary<string, object>;
            dict!.Should().ContainKey("source");
            dict!.Should().ContainKey("directory");
        }

        #endregion

        #region ColorConverter Tests

        /// <summary>
        /// Tests that ColorConverter correctly deserializes string values through BackgroundConfig.Color.
        /// </summary>
        [TestMethod]
        public void ColorConverter_DeserializeString_ReturnsString()
        {
            var json = """
            {
                "color": "#FF0000"
            }
            """;

            var result = JsonSerializer.Deserialize<BackgroundConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Color.Should().BeOfType<string>();
            result!.Color.Should().Be("#FF0000");
        }

        /// <summary>
        /// Tests that ColorConverter correctly deserializes object values through BackgroundConfig.Color.
        /// </summary>
        [TestMethod]
        public void ColorConverter_DeserializeObject_ReturnsDictionary()
        {
            var json = """
            {
                "color": {
                    "from": "#FF0000",
                    "to": "#00FF00",
                    "type": "gradient"
                }
            }
            """;

            var result = JsonSerializer.Deserialize<BackgroundConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Color.Should().BeOfType<Dictionary<string, object>>();
            var dict = result!.Color as Dictionary<string, object>;
            dict!.Should().ContainKey("from");
            dict!.Should().ContainKey("to");
            dict!.Should().ContainKey("type");
        }

        #endregion

        #region BackgroundImageConverter Tests

        /// <summary>
        /// Tests that BackgroundImageConverter correctly deserializes string values through BackgroundConfig.
        /// </summary>
        [TestMethod]
        public void BackgroundImageConverter_DeserializeString_ReturnsString()
        {
            var json = """
            {
                "image": "https://example.com/background.png"
            }
            """;

            var result = JsonSerializer.Deserialize<BackgroundConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Image.Should().BeOfType<string>();
            result!.Image.Should().Be("https://example.com/background.png");
        }

        /// <summary>
        /// Tests that BackgroundImageConverter correctly deserializes object values through BackgroundConfig.
        /// </summary>
        [TestMethod]
        public void BackgroundImageConverter_DeserializeObject_ReturnsDictionary()
        {
            var json = """
            {
                "image": {
                    "light": "background-light.png",
                    "dark": "background-dark.png"
                }
            }
            """;

            var result = JsonSerializer.Deserialize<BackgroundConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Image.Should().BeOfType<Dictionary<string, object>>();
            var dict = result!.Image as Dictionary<string, object>;
            dict!.Should().ContainKey("light");
            dict!.Should().ContainKey("dark");
        }

        #endregion

        #region PrimaryNavigationConverter Tests

        /// <summary>
        /// Tests that PrimaryNavigationConverter correctly deserializes object values through NavbarConfig.
        /// </summary>
        [TestMethod]
        public void PrimaryNavigationConverter_DeserializeObject_ReturnsDictionary()
        {
            var json = """
            {
                "primary": {
                    "type": "button",
                    "label": "Get Started",
                    "href": "/getting-started"
                }
            }
            """;

            var result = JsonSerializer.Deserialize<NavbarConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Primary.Should().BeOfType<Dictionary<string, object>>();
            var dict = result!.Primary as Dictionary<string, object>;
            dict!.Should().ContainKey("type");
            dict!.Should().ContainKey("label");
            dict!.Should().ContainKey("href");
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Tests full GroupConfig serialization/deserialization with polymorphic properties.
        /// </summary>
        [TestMethod]
        public void GroupConfig_FullSerializationRoundTrip_MaintainsData()
        {
            var original = new GroupConfig
            {
                Group = "API Reference",
                Icon = "api",
                AsyncApi = "https://api.example.com/asyncapi.json",
                OpenApi = new List<string> { "spec1.json", "spec2.json" },
                Pages = new List<object>
                {
                    "api/overview",
                    new GroupConfig
                    {
                        Group = "Endpoints",
                        Pages = new List<object> { "api/users", "api/orders" }
                    }
                }
            };

            var json = JsonSerializer.Serialize(original, _jsonOptions);
            var deserialized = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            deserialized!.Group.Should().Be(original.Group);
            deserialized!.Icon.Should().Be("api");
            deserialized!.AsyncApi.Should().Be("https://api.example.com/asyncapi.json");
            deserialized!.OpenApi.Should().BeOfType<List<string>>();
            deserialized!.Pages.Should().HaveCount(2);
            deserialized!.Pages![0].Should().Be("api/overview");
            deserialized!.Pages![1].Should().BeOfType<GroupConfig>();
        }

        /// <summary>
        /// Tests NavigationConfig serialization with CamelCase naming policy.
        /// </summary>
        [TestMethod]
        public void NavigationConfig_CamelCaseNaming_WorksCorrectly()
        {
            var config = new NavigationConfig
            {
                Pages = new List<object> { "index", "getting-started" }
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);

            json.Should().Contain("\"pages\"");
            json.Should().NotContain("\"Pages\"");
        }

        #endregion

    }

}
