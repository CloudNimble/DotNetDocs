using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core.Converters
{

    /// <summary>
    /// Tests for NavigationPageConverter that handles JSON conversion of individual navigation pages.
    /// </summary>
    [TestClass]
    public class NavigationPageConverterTests
    {

        #region Private Fields

        private readonly JsonSerializerOptions _jsonOptions = MintlifyConstants.JsonSerializerOptions;

        #endregion

        #region Deserialization Tests

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
            result!.Pages.Should().ContainSingle();
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

        #endregion

        #region Serialization Tests

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
                Pages = ["cli/index", "cli/commands"]
            };

            var json = JsonSerializer.Serialize<object>(value, _jsonOptions);

            json.Should().Contain("\"group\": \"CLI Tools\"");
            json.Should().Contain("\"pages\"");
        }

        #endregion

    }

}