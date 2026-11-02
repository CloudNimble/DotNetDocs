using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        /// <summary>
        /// Tests that navigation graphql and mcp spec refs are loaded and survive save.
        /// </summary>
        [TestMethod]
        public void Load_Navigation_ParsesGraphqlAndMcp()
        {
            var json = """
                {
                    "name": "Test Documentation",
                    "theme": "mint",
                    "colors": { "primary": "#419AC5" },
                    "navigation": {
                        "graphql": "schema.graphql",
                        "mcp": {
                            "source": "mcp.json",
                            "directory": "mcp-reference"
                        },
                        "pages": [
                            {
                                "group": "GraphQL API",
                                "graphql": {
                                    "source": "schema.graphql",
                                    "directory": "api/graphql"
                                },
                                "pages": ["graphql/overview"]
                            }
                        ]
                    }
                }
                """;

            var manager = new DocsJsonManager();
            manager.Load(json);

            var navigation = manager.Configuration!.Navigation;
            navigation.Graphql.Should().NotBeNull();
            navigation.Graphql!.Source.Should().Be("schema.graphql");
            navigation.Mcp.Should().NotBeNull();
            navigation.Mcp!.Source.Should().Be("mcp.json");
            navigation.Mcp.Directory.Should().Be("mcp-reference");

            var group = navigation.Pages.Should().ContainSingle().Which.Should().BeOfType<GroupConfig>().Subject;
            group.Graphql!.Source.Should().Be("schema.graphql");
            group.Graphql.Directory.Should().Be("api/graphql");

            var reloaded = SaveAndReload(manager);
            reloaded.Navigation.Graphql!.Source.Should().Be("schema.graphql");
            reloaded.Navigation.Mcp!.Directory.Should().Be("mcp-reference");
        }

    }

}
