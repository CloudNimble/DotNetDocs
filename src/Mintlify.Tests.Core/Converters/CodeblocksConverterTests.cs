using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core.Converters
{

    /// <summary>
    /// Tests for CodeblocksConverter string and object shapes.
    /// </summary>
    [TestClass]
    public class CodeblocksConverterTests
    {

        #region Private Fields

        private readonly JsonSerializerOptions _jsonOptions = MintlifyConstants.JsonSerializerOptions;

        #endregion

        #region Public Methods

        /// <summary>
        /// Tests that a Shiki theme name string round-trips as a string.
        /// </summary>
        [TestMethod]
        public void CodeblocksConverter_RoundTrip_ThemeString()
        {
            var json = """{ "codeblocks": "github-dark" }""";

            var result = JsonSerializer.Deserialize<StylingConfig>(json, _jsonOptions);

            result!.Codeblocks!.Theme.Should().Be("github-dark");

            var written = JsonSerializer.Serialize(result, _jsonOptions);
            written.Should().Contain("\"codeblocks\": \"github-dark\"");
        }

        /// <summary>
        /// Tests that a theme object with a string theme writes an object.
        /// </summary>
        [TestMethod]
        public void CodeblocksConverter_RoundTrip_ThemeObjectWithLanguages()
        {
            var json = """
            {
                "codeblocks": {
                    "theme": "nord",
                    "languages": {
                        "custom": ["lang.json"]
                    }
                }
            }
            """;

            var result = JsonSerializer.Deserialize<StylingConfig>(json, _jsonOptions);

            result!.Codeblocks!.Theme.Should().Be("nord");
            result.Codeblocks.Languages!.Custom.Should().Equal("lang.json");

            var written = JsonSerializer.Serialize(result, _jsonOptions);
            written.Should().Contain("\"theme\": \"nord\"");
            written.Should().Contain("\"lang.json\"");
        }

        #endregion

    }

}
