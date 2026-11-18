using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core.Converters
{

    /// <summary>
    /// Tests for MarkdownInstructionsConverter, which accepts a string or an array of strings.
    /// </summary>
    [TestClass]
    public class MarkdownInstructionsConverterTests
    {

        #region Private Fields

        private readonly JsonSerializerOptions _jsonOptions = MintlifyConstants.JsonSerializerOptions;

        #endregion

        #region Public Methods

        /// <summary>
        /// Tests that a string instruction becomes a one-item list.
        /// </summary>
        [TestMethod]
        public void MarkdownInstructionsConverter_DeserializeString_WrapsInList()
        {
            var json = """
            {
                "schema": false,
                "instructions": "Always cite the API version."
            }
            """;

            var result = JsonSerializer.Deserialize<MarkdownConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Instructions.Should().Equal("Always cite the API version.");
        }

        /// <summary>
        /// Tests that an instruction array is preserved.
        /// </summary>
        [TestMethod]
        public void MarkdownInstructionsConverter_DeserializeArray_PreservesItems()
        {
            var json = """
            {
                "instructions": [
                    "Always cite the API version.",
                    "Prefer the TypeScript SDK in examples."
                ]
            }
            """;

            var result = JsonSerializer.Deserialize<MarkdownConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Instructions.Should().Equal(
                "Always cite the API version.",
                "Prefer the TypeScript SDK in examples.");
        }

        /// <summary>
        /// Tests that a single instruction writes as a string.
        /// </summary>
        [TestMethod]
        public void MarkdownInstructionsConverter_SerializeSingleItem_WritesString()
        {
            var config = new MarkdownConfig
            {
                Instructions = new List<string> { "Always cite the API version." }
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);

            json.Should().Contain("\"instructions\": \"Always cite the API version.\"");
        }

        #endregion

    }

}
