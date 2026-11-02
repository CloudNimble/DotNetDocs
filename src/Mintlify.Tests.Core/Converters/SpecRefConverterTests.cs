using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core.Converters
{

    /// <summary>
    /// Tests for SpecRefConverter, which accepts a string path or a source/directory object.
    /// </summary>
    [TestClass]
    public class SpecRefConverterTests
    {

        #region Private Fields

        private readonly JsonSerializerOptions _jsonOptions = MintlifyConstants.JsonSerializerOptions;

        #endregion

        #region Public Methods

        /// <summary>
        /// Tests that a graphql string on a tab becomes SpecRefConfig.Source.
        /// </summary>
        [TestMethod]
        public void SpecRefConverter_DeserializeString_SetsSource()
        {
            var json = """
            {
                "tab": "GraphQL API",
                "graphql": "schema.graphql"
            }
            """;

            var result = JsonSerializer.Deserialize<TabConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Graphql.Should().NotBeNull();
            result.Graphql!.Source.Should().Be("schema.graphql");
            result.Graphql.Directory.Should().BeNull();
        }

        /// <summary>
        /// Tests that a graphql object on a group sets source and directory.
        /// </summary>
        [TestMethod]
        public void SpecRefConverter_DeserializeObject_SetsSourceAndDirectory()
        {
            var json = """
            {
                "group": "GraphQL API",
                "graphql": {
                    "source": "schema.graphql",
                    "directory": "api/graphql"
                }
            }
            """;

            var result = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Graphql!.Source.Should().Be("schema.graphql");
            result.Graphql.Directory.Should().Be("api/graphql");
        }

        /// <summary>
        /// Tests that mcp as a string on a group sets source.
        /// </summary>
        [TestMethod]
        public void SpecRefConverter_DeserializeMcpString_SetsSource()
        {
            var json = """
            {
                "group": "MCP Tools",
                "mcp": "mcp.json"
            }
            """;

            var result = JsonSerializer.Deserialize<GroupConfig>(json, _jsonOptions);

            result.Should().NotBeNull();
            result!.Mcp!.Source.Should().Be("mcp.json");
        }

        /// <summary>
        /// Tests that a spec ref with only source writes as a string.
        /// </summary>
        [TestMethod]
        public void SpecRefConverter_SerializeSourceOnly_WritesString()
        {
            var tab = new TabConfig
            {
                Tab = "GraphQL API",
                Graphql = new SpecRefConfig { Source = "schema.graphql" }
            };

            var json = JsonSerializer.Serialize(tab, _jsonOptions);

            json.Should().Contain("\"graphql\": \"schema.graphql\"");
            json.Should().NotContain("\"source\"");
        }

        #endregion

    }

}
