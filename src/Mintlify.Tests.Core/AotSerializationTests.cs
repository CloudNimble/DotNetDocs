#if NET8_0_OR_GREATER
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core
{

    /// <summary>
    /// Tests for AOT-compatible source-generated JSON serialization.
    /// Verifies that the MintlifyJsonContext produces identical output to reflection-based serialization.
    /// </summary>
    [TestClass]
    public class AotSerializationTests
    {

        #region Private Fields

        private readonly JsonSerializerOptions _jsonOptions = MintlifyConstants.JsonSerializerOptions;

        #endregion

        #region Roundtrip Tests

        /// <summary>
        /// Tests that serializing a DocsJsonConfig with AOT-enabled options matches reflection output.
        /// </summary>
        [TestMethod]
        public void AotContext_SerializeDocsJsonConfig_MatchesReflectionOutput()
        {
            var config = new DocsJsonConfig
            {
                Name = "Test Docs",
                Theme = "mint",
                Colors = new ColorsConfig { Primary = "#0000FF" },
                Description = "Test documentation site"
            };

            // Serialize with AOT-enabled options (MintlifyConstants includes the context)
            var aotJson = JsonSerializer.Serialize(config, _jsonOptions);

            // Serialize with pure reflection options (no source-generated context)
            var reflectionOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                Converters =
                {
                    new Mintlify.Core.Converters.NavigationJsonConverter(),
                    new Mintlify.Core.Converters.NavigationPageListConverter(),
                    new Mintlify.Core.Converters.NavigationPageConverter(),
                    new Mintlify.Core.Converters.IconConverter(),
                    new Mintlify.Core.Converters.ApiConfigConverter(),
                    new Mintlify.Core.Converters.ServerConfigConverter(),
                    new Mintlify.Core.Converters.ColorConverter(),
                    new Mintlify.Core.Converters.BackgroundImageConverter(),
                    new Mintlify.Core.Converters.PrimaryNavigationConverter()
                }
            };
            var reflectionJson = JsonSerializer.Serialize(config, reflectionOptions);

            aotJson.Should().Be(reflectionJson);
        }

        /// <summary>
        /// Tests that a DocsJsonConfig round-trips through deserialization and serialization with AOT options.
        /// </summary>
        [TestMethod]
        public void AotContext_DeserializeDocsJsonConfig_RoundTrips()
        {
            var json = """
            {
                "$schema": "https://mintlify.com/docs.json",
                "name": "Round Trip Test",
                "theme": "palm",
                "colors": {
                    "primary": "#FF5500"
                },
                "navigation": {
                    "pages": [
                        "introduction",
                        "getting-started"
                    ]
                }
            }
            """;

            var deserialized = JsonSerializer.Deserialize<DocsJsonConfig>(json, _jsonOptions);
            deserialized.Should().NotBeNull();
            deserialized!.Name.Should().Be("Round Trip Test");
            deserialized!.Theme.Should().Be("palm");

            var reserialized = JsonSerializer.Serialize(deserialized, _jsonOptions);
            reserialized.Should().Contain("\"name\": \"Round Trip Test\"");
            reserialized.Should().Contain("\"theme\": \"palm\"");
            reserialized.Should().Contain("\"primary\": \"#FF5500\"");
        }

        #endregion

        #region Polymorphic Page Tests

        /// <summary>
        /// Tests that polymorphic navigation pages (strings and GroupConfigs) work via AOT path.
        /// </summary>
        [TestMethod]
        public void AotContext_SerializeWithConverters_HandlesPolymorphicPages()
        {
            var config = new DocsJsonConfig
            {
                Name = "Poly Test",
                Theme = "mint",
                Colors = new ColorsConfig { Primary = "#123456" },
                Navigation = new NavigationConfig
                {
                    Pages = new List<object>
                    {
                        "introduction",
                        new GroupConfig { Group = "Getting Started", Pages = new List<object> { "quickstart", "setup" } },
                        "faq"
                    }
                }
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);

            json.Should().Contain("\"introduction\"");
            json.Should().Contain("\"Getting Started\"");
            json.Should().Contain("\"quickstart\"");
            json.Should().Contain("\"faq\"");

            // Deserialize back and verify structure
            var result = JsonSerializer.Deserialize<DocsJsonConfig>(json, _jsonOptions);
            result.Should().NotBeNull();
            result!.Navigation.Pages.Should().HaveCount(3);
            result!.Navigation.Pages![0].Should().Be("introduction");
            result!.Navigation.Pages![1].Should().BeOfType<GroupConfig>();
            ((GroupConfig)result!.Navigation.Pages![1]).Group.Should().Be("Getting Started");
            result!.Navigation.Pages![2].Should().Be("faq");
        }

        #endregion

        #region Navigation Structure Tests

        /// <summary>
        /// Tests that full navigation with tabs, groups, and anchors serializes correctly via AOT.
        /// </summary>
        [TestMethod]
        public void AotContext_SerializeNavigationConfig_PreservesStructure()
        {
            var config = new DocsJsonConfig
            {
                Name = "Nav Test",
                Theme = "mint",
                Colors = new ColorsConfig { Primary = "#ABCDEF" },
                Navigation = new NavigationConfig
                {
                    Tabs = new List<TabConfig>
                    {
                        new TabConfig { Tab = "API Reference", Href = "api" }
                    },
                    Groups = new List<GroupConfig>
                    {
                        new GroupConfig { Group = "Guides", Pages = new List<object> { "guide1" } }
                    },
                    Anchors = new List<AnchorConfig>
                    {
                        new AnchorConfig { Anchor = "Community", Href = "https://community.example.com" }
                    }
                }
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);

            json.Should().Contain("\"API Reference\"");
            json.Should().Contain("\"Guides\"");
            json.Should().Contain("\"Community\"");

            var result = JsonSerializer.Deserialize<DocsJsonConfig>(json, _jsonOptions);
            result.Should().NotBeNull();
            result!.Navigation.Tabs.Should().HaveCount(1);
            result!.Navigation.Groups.Should().HaveCount(1);
            result!.Navigation.Anchors.Should().HaveCount(1);
        }

        #endregion

        #region Type Info Resolver Tests

        /// <summary>
        /// Tests that MintlifyConstants.JsonSerializerOptions has the source-generated context in its resolver chain.
        /// </summary>
        [TestMethod]
        public void AotContext_TypeInfoResolver_ContainsRequiredTypes()
        {
            var options = MintlifyConstants.JsonSerializerOptions;

            // The options should be able to resolve DocsJsonConfig type info
            var typeInfo = options.GetTypeInfo(typeof(DocsJsonConfig));
            typeInfo.Should().NotBeNull();

            // And GroupConfig (independently deserialized in NavigationPageConverter)
            var groupTypeInfo = options.GetTypeInfo(typeof(GroupConfig));
            groupTypeInfo.Should().NotBeNull();
        }

        #endregion

    }

}
#endif
