using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Converters;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core.Converters
{

    /// <summary>
    /// Tests for PrimaryNavigationConverter that handles JSON conversion of primary navigation properties.
    /// </summary>
    [TestClass]
    public class PrimaryNavigationConverterTests
    {

        #region Private Fields

        private readonly JsonSerializerOptions _jsonOptions = MintlifyConstants.JsonSerializerOptions;

        #endregion

        #region Deserialization Tests

        /// <summary>
        /// Tests that PrimaryNavigationConverter correctly deserializes object values through NavbarConfig.
        /// </summary>
        [TestMethod]
        public void PrimaryNavigationConverter_DeserializeObject_ReturnsPrimaryNavigationConfig()
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
            result!.Primary.Should().BeOfType<PrimaryNavigationConfig>();
            result!.Primary!.Type.Should().Be("button");
            result!.Primary!.Label.Should().Be("Get Started");
            result!.Primary!.Href.Should().Be("/getting-started");
        }

        /// <summary>
        /// Tests that PrimaryNavigationConverter correctly handles null values.
        /// </summary>
        [TestMethod]
        public void PrimaryNavigationConverter_DeserializeNull_ReturnsNull()
        {
            var json = """
            {
                "primary": null
            }
            """;

            var result = JsonSerializer.Deserialize<NavbarConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Primary.Should().BeNull();
        }

        #endregion

        #region Serialization Tests

        /// <summary>
        /// Tests that PrimaryNavigationConverter correctly serializes object values.
        /// </summary>
        [TestMethod]
        public void PrimaryNavigationConverter_SerializeObject_ReturnsJsonObject()
        {
            var primaryNav = new PrimaryNavigationConfig
            {
                Type = "button",
                Label = "Get Started",
                Href = "/getting-started"
            };

            var config = new NavbarConfig
            {
                Primary = primaryNav
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);

            json.Should().Contain("\"type\": \"button\"");
            json.Should().Contain("\"label\": \"Get Started\"");
            json.Should().Contain("\"href\": \"/getting-started\"");
        }

        #endregion

        #region Recursion Prevention Tests

        /// <summary>
        /// Tests that the PrimaryNavigationConverter.OptionsWithoutThis excludes the PrimaryNavigationConverter to prevent infinite recursion.
        /// </summary>
        [TestMethod]
        public void OptionsWithoutThis_ExcludesPrimaryNavigationConverter()
        {
            var originalOptions = MintlifyConstants.JsonSerializerOptions;
            var optionsWithoutThis = PrimaryNavigationConverter.OptionsWithoutThis;

            // Should not be the same instance
            optionsWithoutThis.Should().NotBeSameAs(originalOptions);

            // Original should have PrimaryNavigationConverter
            originalOptions.Converters.Should().Contain(c => c is PrimaryNavigationConverter);

            // OptionsWithoutThis should NOT have PrimaryNavigationConverter
            optionsWithoutThis.Converters.Should().NotContain(c => c is PrimaryNavigationConverter);

            // Should preserve other important settings
            optionsWithoutThis.PropertyNamingPolicy.Should().Be(originalOptions.PropertyNamingPolicy);
            optionsWithoutThis.DefaultIgnoreCondition.Should().Be(originalOptions.DefaultIgnoreCondition);
        }

        /// <summary>
        /// Tests that serializing a NavbarConfig with PrimaryNavigationConfig does not cause stack overflow.
        /// </summary>
        [TestMethod]
        public void Serialize_NavbarConfigWithPrimaryNavigation_NoStackOverflow()
        {
            var config = new NavbarConfig
            {
                Primary = new PrimaryNavigationConfig
                {
                    Type = "button",
                    Label = "Get Started",
                    Href = "/getting-started"
                }
            };

            var act = () => JsonSerializer.Serialize(config, MintlifyConstants.JsonSerializerOptions);

            act.Should().NotThrow(); // This would throw StackOverflowException before the fix
            var json = act();
            json.Should().Contain("\"type\": \"button\"");
            json.Should().Contain("\"label\": \"Get Started\"");
            json.Should().Contain("\"href\": \"/getting-started\"");
        }

        #endregion

    }

}