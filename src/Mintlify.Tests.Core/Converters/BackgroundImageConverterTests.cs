using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core.Converters
{

    /// <summary>
    /// Tests for BackgroundImageConverter that handles JSON conversion of background image properties.
    /// </summary>
    [TestClass]
    public class BackgroundImageConverterTests
    {

        #region Private Fields

        private readonly JsonSerializerOptions _jsonOptions = MintlifyConstants.JsonSerializerOptions;

        #endregion

        #region Deserialization Tests

        /// <summary>
        /// Tests that BackgroundImageConverter correctly deserializes string values to BackgroundImageConfig.
        /// </summary>
        [TestMethod]
        public void BackgroundImageConverter_DeserializeString_ReturnsBackgroundImageConfig()
        {
            var json = """
            {
                "image": "https://example.com/background.png"
            }
            """;

            var result = JsonSerializer.Deserialize<BackgroundConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Image.Should().BeOfType<BackgroundImageConfig>();
            result!.Image!.Url.Should().Be("https://example.com/background.png");
            result!.Image!.Light.Should().BeNull();
            result!.Image!.Dark.Should().BeNull();
        }

        /// <summary>
        /// Tests that BackgroundImageConverter correctly deserializes object values to BackgroundImageConfig.
        /// </summary>
        [TestMethod]
        public void BackgroundImageConverter_DeserializeObject_ReturnsBackgroundImageConfig()
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
            result!.Image.Should().BeOfType<BackgroundImageConfig>();
            result!.Image!.Light.Should().Be("background-light.png");
            result!.Image!.Dark.Should().Be("background-dark.png");
            result!.Image!.Url.Should().BeNull();
        }

        /// <summary>
        /// Tests that BackgroundImageConverter correctly handles null values.
        /// </summary>
        [TestMethod]
        public void BackgroundImageConverter_DeserializeNull_ReturnsNull()
        {
            var json = """
            {
                "image": null
            }
            """;

            var result = JsonSerializer.Deserialize<BackgroundConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Image.Should().BeNull();
        }

        #endregion

        #region Serialization Tests

        /// <summary>
        /// Tests that BackgroundImageConverter correctly serializes simple BackgroundImageConfig as string.
        /// </summary>
        [TestMethod]
        public void BackgroundImageConverter_SerializeSimpleConfig_ReturnsString()
        {
            var config = new BackgroundConfig
            {
                Image = new BackgroundImageConfig { Url = "https://example.com/bg.jpg" }
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);

            json.Should().Contain("\"image\": \"https://example.com/bg.jpg\"");
        }

        /// <summary>
        /// Tests that BackgroundImageConverter correctly serializes theme-specific BackgroundImageConfig as object.
        /// </summary>
        [TestMethod]
        public void BackgroundImageConverter_SerializeThemeConfig_ReturnsObject()
        {
            var config = new BackgroundConfig
            {
                Image = new BackgroundImageConfig
                {
                    Light = "bg-light.jpg",
                    Dark = "bg-dark.jpg"
                }
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);

            json.Should().Contain("\"light\": \"bg-light.jpg\"");
            json.Should().Contain("\"dark\": \"bg-dark.jpg\"");
        }

        #endregion

        #region Implicit Conversion Tests

        /// <summary>
        /// Tests BackgroundImageConfig implicit conversions work correctly.
        /// </summary>
        [TestMethod]
        public void BackgroundImageConfig_ImplicitConversions_WorkCorrectly()
        {
            // Test string to BackgroundImageConfig
            BackgroundImageConfig? bgFromString = "https://example.com/bg.jpg";
            bgFromString.Should().NotBeNull();
            bgFromString!.Url.Should().Be("https://example.com/bg.jpg");

            // Test BackgroundImageConfig to string
            string? urlFromBg = bgFromString;
            urlFromBg.Should().Be("https://example.com/bg.jpg");
        }

        /// <summary>
        /// Tests BackgroundImageConfig ToString method.
        /// </summary>
        [TestMethod]
        public void BackgroundImageConfig_ToString_ReturnsUrl()
        {
            var bg = new BackgroundImageConfig { Url = "test-background.jpg" };

            bg.ToString().Should().Be("test-background.jpg");
        }

        /// <summary>
        /// Tests BackgroundImageConfig ToString method with light/dark images.
        /// </summary>
        [TestMethod]
        public void BackgroundImageConfig_ToStringWithThemeImages_ReturnsLightImage()
        {
            var bg = new BackgroundImageConfig
            {
                Light = "bg-light.jpg",
                Dark = "bg-dark.jpg"
            };

            bg.ToString().Should().Be("bg-light.jpg");
        }

        #endregion

    }

}