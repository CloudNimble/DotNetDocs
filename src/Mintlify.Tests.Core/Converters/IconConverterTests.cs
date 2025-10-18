using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core.Converters
{

    /// <summary>
    /// Tests for IconConverter that handles JSON conversion of icon properties.
    /// </summary>
    [TestClass]
    public class IconConverterTests
    {

        #region Private Fields

        private readonly JsonSerializerOptions _jsonOptions = MintlifyConstants.JsonSerializerOptions;

        #endregion

        #region Deserialization Tests

        /// <summary>
        /// Tests that IconConverter correctly deserializes string values to IconConfig.
        /// </summary>
        [TestMethod]
        public void IconConverter_DeserializeString_ReturnsIconConfig()
        {
            var json = """
            {
                "group": "Test Group",
                "icon": "home"
            }
            """;

            var result = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Icon.Should().BeOfType<IconConfig>();
            result!.Icon!.Name.Should().Be("home");
            result!.Icon!.Library.Should().BeNull();
            result!.Icon!.Style.Should().BeNull();
        }

        /// <summary>
        /// Tests that IconConverter correctly deserializes object values to IconConfig.
        /// </summary>
        [TestMethod]
        public void IconConverter_DeserializeObject_ReturnsIconConfig()
        {
            var json = """
            {
                "group": "Test Group",
                "icon": {
                    "name": "user",
                    "style": "solid",
                    "library": "fontawesome"
                }
            }
            """;

            var result = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Icon.Should().BeOfType<IconConfig>();
            result!.Icon!.Name.Should().Be("user");
            result!.Icon!.Style.Should().Be("solid");
            result!.Icon!.Library.Should().Be("fontawesome");
        }

        /// <summary>
        /// Tests that IconConverter correctly handles null values.
        /// </summary>
        [TestMethod]
        public void IconConverter_DeserializeNull_ReturnsNull()
        {
            var json = """
            {
                "group": "Test Group",
                "icon": null
            }
            """;

            var result = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Icon.Should().BeNull();
        }

        #endregion

        #region Serialization Tests

        /// <summary>
        /// Tests that IconConverter correctly serializes simple IconConfig as string.
        /// </summary>
        [TestMethod]
        public void IconConverter_SerializeSimpleIcon_ReturnsString()
        {
            var config = new GroupConfig
            {
                Group = "Test Group",
                Icon = new IconConfig { Name = "home" }
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);

            json.Should().Contain("\"icon\": \"home\"");
        }

        /// <summary>
        /// Tests that IconConverter correctly serializes complex IconConfig as object.
        /// </summary>
        [TestMethod]
        public void IconConverter_SerializeComplexIcon_ReturnsObject()
        {
            var config = new GroupConfig
            {
                Group = "Test Group",
                Icon = new IconConfig
                {
                    Name = "user",
                    Style = "solid",
                    Library = "fontawesome"
                }
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);

            json.Should().Contain("\"name\": \"user\"");
            json.Should().Contain("\"style\": \"solid\"");
            json.Should().Contain("\"library\": \"fontawesome\"");
        }

        #endregion

        #region Implicit Conversion Tests

        /// <summary>
        /// Tests that IconConfig implicit string conversion works correctly.
        /// </summary>
        [TestMethod]
        public void IconConfig_ImplicitStringConversion_WorksCorrectly()
        {
            IconConfig? icon = "home";
            string? iconString = icon;

            icon.Should().NotBeNull();
            icon!.Name.Should().Be("home");
            iconString.Should().Be("home");
        }

        /// <summary>
        /// Tests IconConfig ToString method.
        /// </summary>
        [TestMethod]
        public void IconConfig_ToString_ReturnsName()
        {
            var icon = new IconConfig { Name = "test-icon" };

            icon.ToString().Should().Be("test-icon");
        }

        /// <summary>
        /// Tests IconConfig ToString method with null name.
        /// </summary>
        [TestMethod]
        public void IconConfig_ToStringWithNullName_ReturnsEmptyString()
        {
            var icon = new IconConfig { Name = null! };

            icon.ToString().Should().Be(string.Empty);
        }

        #endregion

    }

}