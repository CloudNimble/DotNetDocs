using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Converters;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core.Converters
{

    /// <summary>
    /// Tests for ColorConverter that handles JSON conversion of color properties.
    /// </summary>
    [TestClass]
    public class ColorConverterTests
    {

        #region Private Fields

        private readonly JsonSerializerOptions _jsonOptions = MintlifyConstants.JsonSerializerOptions;

        #endregion

        #region Deserialization Tests

        /// <summary>
        /// Tests that ColorConverter correctly deserializes string values through BackgroundConfig.Color.
        /// </summary>
        [TestMethod]
        public void ColorConverter_DeserializeString_ReturnsColorConfig()
        {
            var json = """
            {
                "color": "#FF0000"
            }
            """;

            var result = JsonSerializer.Deserialize<BackgroundConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Color.Should().BeOfType<ColorConfig>();
            result!.Color!.Light.Should().Be("#FF0000");
            result!.Color!.Dark.Should().Be("#FF0000");
            // Test implicit string conversion
            string? colorString = result!.Color;
            colorString.Should().Be("#FF0000");
        }

        /// <summary>
        /// Tests that ColorConverter correctly deserializes light/dark object values through BackgroundConfig.Color.
        /// </summary>
        [TestMethod]
        public void ColorConverter_DeserializeLightDarkObject_ReturnsColorConfig()
        {
            var json = """
            {
                "color": {
                    "light": "#FF0000",
                    "dark": "#000000"
                }
            }
            """;

            var result = JsonSerializer.Deserialize<BackgroundConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Color.Should().BeOfType<ColorConfig>();
            result!.Color!.Light.Should().Be("#FF0000");
            result!.Color!.Dark.Should().Be("#000000");
            // Test implicit string conversion (should return light color)
            string? colorString = result!.Color;
            colorString.Should().Be("#FF0000");
        }

        /// <summary>
        /// Tests that ColorConverter correctly handles null values.
        /// </summary>
        [TestMethod]
        public void ColorConverter_DeserializeNull_ReturnsNull()
        {
            var json = """
            {
                "color": null
            }
            """;

            var result = JsonSerializer.Deserialize<BackgroundConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Color.Should().BeNull();
        }

        #endregion

        #region Serialization Tests

        /// <summary>
        /// Tests that ColorConverter correctly serializes string values using implicit conversion.
        /// </summary>
        [TestMethod]
        public void ColorConverter_SerializeString_ReturnsJsonString()
        {
            var config = new BackgroundConfig
            {
                Color = "#FF0000" // Implicit conversion from string to ColorConfig
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);

            json.Should().Contain("\"color\": \"#FF0000\"");
        }

        /// <summary>
        /// Tests that ColorConverter correctly serializes light/dark color object values.
        /// </summary>
        [TestMethod]
        public void ColorConverter_SerializeLightDarkObject_ReturnsJsonObject()
        {
            var config = new BackgroundConfig
            {
                Color = new ColorConfig("#FF0000", "#000000")
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);

            json.Should().Contain("\"light\": \"#FF0000\"");
            json.Should().Contain("\"dark\": \"#000000\"");
        }

        #endregion

        #region Recursion Prevention Tests

        /// <summary>
        /// Tests that the ColorConverter.OptionsWithoutThis excludes the ColorConverter to prevent infinite recursion.
        /// </summary>
        [TestMethod]
        public void OptionsWithoutThis_ExcludesColorConverter()
        {
            var originalOptions = MintlifyConstants.JsonSerializerOptions;
            var optionsWithoutThis = ColorConverter.OptionsWithoutThis;

            // Should not be the same instance
            optionsWithoutThis.Should().NotBeSameAs(originalOptions);

            // Original should have ColorConverter
            originalOptions.Converters.Should().Contain(c => c is ColorConverter);

            // OptionsWithoutThis should NOT have ColorConverter
            optionsWithoutThis.Converters.Should().NotContain(c => c is ColorConverter);

            // Should preserve other important settings
            optionsWithoutThis.PropertyNamingPolicy.Should().Be(originalOptions.PropertyNamingPolicy);
            optionsWithoutThis.DefaultIgnoreCondition.Should().Be(originalOptions.DefaultIgnoreCondition);
        }

        /// <summary>
        /// Tests that serializing a BackgroundConfig with ColorConfig does not cause stack overflow.
        /// </summary>
        [TestMethod]
        public void Serialize_BackgroundConfigWithColor_NoStackOverflow()
        {
            var config = new BackgroundConfig
            {
                Color = new ColorConfig("#FF0000", "#000000")
            };

            var act = () => JsonSerializer.Serialize(config, MintlifyConstants.JsonSerializerOptions);

            act.Should().NotThrow(); // This would throw StackOverflowException before the fix
            var json = act();
            json.Should().Contain("\"light\": \"#FF0000\"");
            json.Should().Contain("\"dark\": \"#000000\"");
        }

        #endregion

    }

}