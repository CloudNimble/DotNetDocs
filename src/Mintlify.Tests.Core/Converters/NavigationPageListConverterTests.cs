using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core.Converters
{

    /// <summary>
    /// Tests for NavigationPageListConverter that handles JSON conversion of page lists.
    /// </summary>
    [TestClass]
    public class NavigationPageListConverterTests
    {

        #region Private Fields

        private readonly JsonSerializerOptions _jsonOptions = MintlifyConstants.JsonSerializerOptions;

        #endregion

        #region Deserialization Tests

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

        #endregion

        #region Serialization Tests

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

    }

}