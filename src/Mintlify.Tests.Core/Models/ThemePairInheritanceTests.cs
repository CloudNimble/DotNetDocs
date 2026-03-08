using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core.Models
{

    /// <summary>
    /// Tests that all ThemePairConfig subclasses correctly serialize and deserialize
    /// Light/Dark properties after inheriting from the base class.
    /// </summary>
    [TestClass]
    public class ThemePairInheritanceTests
    {

        #region Private Fields

        private readonly JsonSerializerOptions _jsonOptions = MintlifyConstants.JsonSerializerOptions;

        #endregion

        #region ColorConfig Tests

        /// <summary>
        /// Tests that ColorConfig round-trips through JSON serialization preserving Light and Dark values.
        /// </summary>
        [TestMethod]
        public void ColorConfig_SerializeRoundTrip_PreservesLightDark()
        {
            var json = """
            {
                "color": {
                    "light": "#FF0000",
                    "dark": "#00FF00"
                }
            }
            """;

            var result = JsonSerializer.Deserialize<BackgroundConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Color.Should().NotBeNull();
            result!.Color!.Light.Should().Be("#FF0000");
            result!.Color!.Dark.Should().Be("#00FF00");
            result!.Color.Should().BeAssignableTo<ThemePairConfig>();
        }

        #endregion

        #region ColorPairConfig Tests

        /// <summary>
        /// Tests that ColorPairConfig round-trips preserving Light and Dark values.
        /// </summary>
        [TestMethod]
        public void ColorPairConfig_SerializeRoundTrip_PreservesLightDark()
        {
            var config = new ColorPairConfig { Light = "#AABBCC", Dark = "#112233" };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var result = JsonSerializer.Deserialize<ColorPairConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Light.Should().Be("#AABBCC");
            result!.Dark.Should().Be("#112233");
            result.Should().BeAssignableTo<ThemePairConfig>();
        }

        #endregion

        #region ColorsConfig Tests

        /// <summary>
        /// Tests that ColorsConfig round-trips preserving Light, Dark, and Primary values.
        /// </summary>
        [TestMethod]
        public void ColorsConfig_SerializeRoundTrip_PreservesLightDarkAndPrimary()
        {
            var json = """
            {
                "primary": "#0000FF",
                "light": "#FFFFFF",
                "dark": "#000000"
            }
            """;

            var result = JsonSerializer.Deserialize<ColorsConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Primary.Should().Be("#0000FF");
            result!.Light.Should().Be("#FFFFFF");
            result!.Dark.Should().Be("#000000");
            result.Should().BeAssignableTo<ThemePairConfig>();

            var serialized = JsonSerializer.Serialize(result, _jsonOptions);
            serialized.Should().Contain("\"primary\": \"#0000FF\"");
            serialized.Should().Contain("\"light\": \"#FFFFFF\"");
            serialized.Should().Contain("\"dark\": \"#000000\"");
        }

        #endregion

        #region BackgroundImageConfig Tests

        /// <summary>
        /// Tests that BackgroundImageConfig round-trips preserving Light and Dark values.
        /// </summary>
        [TestMethod]
        public void BackgroundImageConfig_SerializeRoundTrip_PreservesLightDark()
        {
            var json = """
            {
                "image": {
                    "light": "/images/bg-light.png",
                    "dark": "/images/bg-dark.png"
                }
            }
            """;

            var result = JsonSerializer.Deserialize<BackgroundConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Image.Should().NotBeNull();
            result!.Image!.Light.Should().Be("/images/bg-light.png");
            result!.Image!.Dark.Should().Be("/images/bg-dark.png");
            result!.Image.Should().BeAssignableTo<ThemePairConfig>();
        }

        #endregion

        #region LogoConfig Tests

        /// <summary>
        /// Tests that LogoConfig round-trips preserving Light, Dark, and Href values.
        /// </summary>
        [TestMethod]
        public void LogoConfig_SerializeRoundTrip_PreservesLightDarkAndHref()
        {
            var config = new LogoConfig
            {
                Light = "/logo-light.svg",
                Dark = "/logo-dark.svg",
                Href = "https://example.com"
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var result = JsonSerializer.Deserialize<LogoConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Light.Should().Be("/logo-light.svg");
            result!.Dark.Should().Be("/logo-dark.svg");
            result!.Href.Should().Be("https://example.com");
            result.Should().BeAssignableTo<ThemePairConfig>();
        }

        #endregion

        #region FaviconConfig Tests

        /// <summary>
        /// Tests that FaviconConfig round-trips preserving Light and Dark values.
        /// </summary>
        [TestMethod]
        public void FaviconConfig_SerializeRoundTrip_PreservesLightDark()
        {
            var json = """
            {
                "dark": "/favicon-dark.svg",
                "light": "/favicon-light.svg"
            }
            """;

            var result = JsonSerializer.Deserialize<FaviconConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Dark.Should().Be("/favicon-dark.svg");
            result!.Light.Should().Be("/favicon-light.svg");
            result.Should().BeAssignableTo<ThemePairConfig>();

            // Round-trip: different dark/light should serialize as object
            var serialized = JsonSerializer.Serialize(result, _jsonOptions);
            serialized.Should().Contain("dark");
            serialized.Should().Contain("light");
        }

        #endregion

    }

}
