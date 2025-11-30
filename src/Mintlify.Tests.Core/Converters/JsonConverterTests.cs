using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core.Converters
{

    /// <summary>
    /// Integration tests for Mintlify JSON converters that verify overall serialization behavior.
    /// </summary>
    /// <remarks>
    /// This test class focuses on integration scenarios that test multiple converters working together.
    /// Individual converter tests are in their respective test classes (e.g., IconConverterTests.cs).
    /// </remarks>
    [TestClass]
    public class JsonConverterTests
    {

        #region Private Fields

        private readonly JsonSerializerOptions _jsonOptions = MintlifyConstants.JsonSerializerOptions;

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
                Pages =
                [
                    "api/overview",
                    new GroupConfig
                    {
                        Group = "Endpoints",
                        Pages = ["api/users", "api/orders"]
                    }
                ]
            };

            var json = JsonSerializer.Serialize(original, _jsonOptions);
            var deserialized = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            deserialized!.Group.Should().Be(original.Group);
            deserialized!.Icon!.Name.Should().Be("api");
            deserialized!.AsyncApi!.Source.Should().Be("https://api.example.com/asyncapi.json");
            deserialized!.OpenApi!.Urls.Should().BeOfType<List<string>>();
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
                Pages = ["index", "getting-started"]
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);

            json.Should().Contain("\"pages\"");
            json.Should().NotContain("\"Pages\"");
        }

        /// <summary>
        /// Tests complex BackgroundConfig with multiple converter interactions.
        /// </summary>
        [TestMethod]
        public void BackgroundConfig_MultipleConverters_WorkTogether()
        {
            var config = new BackgroundConfig
            {
                Color = "#FF0000",
                Image = new BackgroundImageConfig
                {
                    Light = "bg-light.jpg",
                    Dark = "bg-dark.jpg"
                },
                Decoration = "gradient"
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var deserialized = JsonSerializer.Deserialize<BackgroundConfig>(json, _jsonOptions);

            deserialized.Should().NotBeNull();
            ((string?)deserialized!.Color).Should().Be("#FF0000");
            deserialized!.Image.Should().BeOfType<BackgroundImageConfig>();
            deserialized!.Image!.Light.Should().Be("bg-light.jpg");
            deserialized!.Image!.Dark.Should().Be("bg-dark.jpg");
            deserialized!.Decoration.Should().Be("gradient");
        }

        #endregion

    }

}