using System.Collections.Generic;
using System.Xml.Linq;
using CloudNimble.DotNetDocs.Sdk.Tasks;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core.Models;

namespace CloudNimble.DotNetDocs.Tests.Sdk.Tasks
{

    /// <summary>
    /// Tests for the GenerateDocumentationTask class, specifically focusing on MintlifyTemplate XML parsing.
    /// </summary>
    [TestClass]
    public class GenerateDocumentationTaskTests : DotNetDocsTestBase
    {

        #region Fields

        private GenerateDocumentationTask _task = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            TestSetup();
            _task = new GenerateDocumentationTask();

            // Set up a minimal build engine to avoid logging errors
            var buildEngine = new TestBuildEngine();
            _task.BuildEngine = buildEngine;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestTearDown();
        }

        #endregion

        #region MintlifyTemplate XML Parsing Tests

        /// <summary>
        /// Tests that ParseGroupConfig correctly extracts group name, icon, and tag from XML.
        /// </summary>
        [TestMethod]
        public void ParseGroupConfig_WithCompleteAttributes_ParsesAllProperties()
        {
            // Arrange
            var xml = """
                <Group Name="Getting Started" Icon="stars" Tag="CORE">
                    <Pages>index;quickstart</Pages>
                </Group>
                """;
            var groupElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseGroupConfig(groupElement);

            // Assert
            result.Should().NotBeNull();
            result!.Group.Should().Be("Getting Started");
            result.Icon!.Name.Should().Be("stars");
            result.Tag.Should().Be("CORE");
            result.Pages.Should().HaveCount(2);
            result.Pages![0].Should().Be("index");
            result.Pages![1].Should().Be("quickstart");
        }

        /// <summary>
        /// Tests that ParseGroupConfig preserves the exact order of groups as defined in the template.
        /// </summary>
        [TestMethod]
        public void ParseGroupConfig_PreservesTemplateOrder()
        {
            // Arrange - Template order from .docsproj file
            var xml = """
                <Groups>
                    <Group Name="Getting Started" Icon="stars">
                        <Pages>index;quickstart</Pages>
                    </Group>
                    <Group Name="Guides" Icon="dog-leashed">
                        <Pages>guides/index;guides/conceptual-docs</Pages>
                    </Group>
                    <Group Name="Providers" Icon="books">
                        <Pages>providers/index</Pages>
                    </Group>
                    <Group Name="Plugins" Icon="outlet">
                        <Pages>plugins/index</Pages>
                    </Group>
                    <Group Name="Learnings" Icon="">
                        <Pages>learnings/bridge-assemblies</Pages>
                    </Group>
                </Groups>
                """;
            var groupsElement = XElement.Parse(xml);

            // Act
            var results = new List<GroupConfig>();
            foreach (var groupElement in groupsElement.Elements("Group"))
            {
                var group = _task.ParseGroupConfig(groupElement);
                if (group is not null)
                {
                    results.Add(group);
                }
            }

            // Assert
            results.Should().HaveCount(5);
            results[0].Group.Should().Be("Getting Started");
            results[0].Icon!.Name.Should().Be("stars");
            results[1].Group.Should().Be("Guides");
            results[1].Icon!.Name.Should().Be("dog-leashed");
            results[2].Group.Should().Be("Providers");
            results[2].Icon!.Name.Should().Be("books");
            results[3].Group.Should().Be("Plugins");
            results[3].Icon!.Name.Should().Be("outlet");
            results[4].Group.Should().Be("Learnings");
            results[4].Icon!.Name.Should().Be("");
        }

        /// <summary>
        /// Tests that ParseGroupConfig correctly handles nested groups.
        /// </summary>
        [TestMethod]
        public void ParseGroupConfig_WithNestedGroups_ParsesHierarchy()
        {
            // Arrange
            var xml = """
                <Group Name="Providers" Icon="books">
                    <Pages>providers/index</Pages>
                    <Groups>
                        <Group Name="Mintlify" Icon="/mintlify.svg" Tag="PARTNER">
                            <Pages>providers/mintlify/index</Pages>
                        </Group>
                        <Group Name="GitHub" Icon="github">
                            <Pages>providers/github/index</Pages>
                        </Group>
                    </Groups>
                </Group>
                """;
            var groupElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseGroupConfig(groupElement);

            // Assert
            result.Should().NotBeNull();
            result!.Group.Should().Be("Providers");
            result.Icon!.Name.Should().Be("books");
            result.Pages.Should().HaveCount(3); // 1 direct page + 2 nested groups

            // First item should be the direct page
            result.Pages![0].Should().Be("providers/index");

            // Second item should be the nested Mintlify group
            result.Pages![1].Should().BeOfType<GroupConfig>();
            var mintlifyGroup = result.Pages![1] as GroupConfig;
            mintlifyGroup!.Group.Should().Be("Mintlify");
            mintlifyGroup.Icon!.Name.Should().Be("/mintlify.svg");
            mintlifyGroup.Tag.Should().Be("PARTNER");
            mintlifyGroup.Pages.Should().HaveCount(1);
            mintlifyGroup.Pages![0].Should().Be("providers/mintlify/index");

            // Third item should be the nested GitHub group
            result.Pages![2].Should().BeOfType<GroupConfig>();
            var githubGroup = result.Pages![2] as GroupConfig;
            githubGroup!.Group.Should().Be("GitHub");
            githubGroup.Icon!.Name.Should().Be("github");
        }

        /// <summary>
        /// Tests that ParseGroupConfig handles empty icon attributes correctly.
        /// </summary>
        [TestMethod]
        public void ParseGroupConfig_WithEmptyIcon_CreatesEmptyIconConfig()
        {
            // Arrange
            var xml = """
                <Group Name="Learnings" Icon="">
                    <Pages>learnings/bridge-assemblies;learnings/sdk-packaging</Pages>
                </Group>
                """;
            var groupElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseGroupConfig(groupElement);

            // Assert
            result.Should().NotBeNull();
            result!.Group.Should().Be("Learnings");
            result.Icon!.Name.Should().Be("");
            result.Pages.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that ParseGroupConfig handles missing icon attributes correctly.
        /// </summary>
        [TestMethod]
        public void ParseGroupConfig_WithoutIcon_HasNullIcon()
        {
            // Arrange
            var xml = """
                <Group Name="Basic Group">
                    <Pages>page1;page2</Pages>
                </Group>
                """;
            var groupElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseGroupConfig(groupElement);

            // Assert
            result.Should().NotBeNull();
            result!.Group.Should().Be("Basic Group");
            result.Icon.Should().BeNull();
            result.Pages.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that ParseGroupConfig handles semicolon-separated page lists correctly.
        /// </summary>
        [TestMethod]
        public void ParseGroupConfig_WithSemicolonSeparatedPages_ParsesAllPages()
        {
            // Arrange
            var xml = """
                <Group Name="Guides" Icon="dog-leashed">
                    <Pages>guides/index;guides/conceptual-docs;guides/pipeline;guides/advanced</Pages>
                </Group>
                """;
            var groupElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseGroupConfig(groupElement);

            // Assert
            result.Should().NotBeNull();
            result!.Group.Should().Be("Guides");
            result.Icon!.Name.Should().Be("dog-leashed");
            result.Pages.Should().HaveCount(4);
            result.Pages![0].Should().Be("guides/index");
            result.Pages![1].Should().Be("guides/conceptual-docs");
            result.Pages![2].Should().Be("guides/pipeline");
            result.Pages![3].Should().Be("guides/advanced");
        }

        /// <summary>
        /// Tests that ParseGroupConfig handles whitespace around page names correctly.
        /// </summary>
        [TestMethod]
        public void ParseGroupConfig_WithWhitespaceInPages_TrimsCorrectly()
        {
            // Arrange
            var xml = """
                <Group Name="Test Group">
                    <Pages>  page1  ; page2;   page3   ;page4;</Pages>
                </Group>
                """;
            var groupElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseGroupConfig(groupElement);

            // Assert
            result.Should().NotBeNull();
            result!.Pages.Should().HaveCount(4);
            result.Pages![0].Should().Be("page1");
            result.Pages![1].Should().Be("page2");
            result.Pages![2].Should().Be("page3");
            result.Pages![3].Should().Be("page4");
        }

        /// <summary>
        /// Tests that ParseGroupConfig handles empty pages elements correctly.
        /// </summary>
        [TestMethod]
        public void ParseGroupConfig_WithEmptyPages_CreatesEmptyPagesList()
        {
            // Arrange
            var xml = """
                <Group Name="Empty Group" Icon="folder">
                    <Pages></Pages>
                </Group>
                """;
            var groupElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseGroupConfig(groupElement);

            // Assert
            result.Should().NotBeNull();
            result!.Group.Should().Be("Empty Group");
            result.Icon!.Name.Should().Be("folder");
            result.Pages.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that ParseGroupConfig handles missing pages elements correctly.
        /// </summary>
        [TestMethod]
        public void ParseGroupConfig_WithoutPages_CreatesEmptyPagesList()
        {
            // Arrange
            var xml = """
                <Group Name="Group Without Pages" Icon="folder">
                </Group>
                """;
            var groupElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseGroupConfig(groupElement);

            // Assert
            result.Should().NotBeNull();
            result!.Group.Should().Be("Group Without Pages");
            result.Icon!.Name.Should().Be("folder");
            result.Pages.Should().BeEmpty();
        }

        #endregion

        #region Edge Cases and Validation Tests

        /// <summary>
        /// Tests that ParseGroupConfig returns null for groups with missing name attributes.
        /// </summary>
        [TestMethod]
        public void ParseGroupConfig_WithoutName_ReturnsNull()
        {
            // Arrange
            var xml = """
                <Group Icon="stars">
                    <Pages>page1;page2</Pages>
                </Group>
                """;
            var groupElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseGroupConfig(groupElement);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that ParseGroupConfig returns null for groups with empty name attributes.
        /// </summary>
        [TestMethod]
        public void ParseGroupConfig_WithEmptyName_ReturnsNull()
        {
            // Arrange
            var xml = """
                <Group Name="" Icon="stars">
                    <Pages>page1;page2</Pages>
                </Group>
                """;
            var groupElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseGroupConfig(groupElement);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that ParseGroupConfig returns null for groups with whitespace-only names.
        /// </summary>
        [TestMethod]
        public void ParseGroupConfig_WithWhitespaceOnlyName_ReturnsNull()
        {
            // Arrange
            var xml = """
                <Group Name="   " Icon="stars">
                    <Pages>page1;page2</Pages>
                </Group>
                """;
            var groupElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseGroupConfig(groupElement);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that ParseGroupConfig handles special characters in icon names.
        /// </summary>
        [TestMethod]
        public void ParseGroupConfig_WithSpecialCharactersInIcon_PreservesIcon()
        {
            // Arrange
            var xml = """
                <Group Name="Custom Group" Icon="/images/custom-icon.svg">
                    <Pages>page1</Pages>
                </Group>
                """;
            var groupElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseGroupConfig(groupElement);

            // Assert
            result.Should().NotBeNull();
            result!.Icon!.Name.Should().Be("/images/custom-icon.svg");
        }

        /// <summary>
        /// Tests that ParseGroupConfig correctly processes complex real-world template scenarios.
        /// </summary>
        [TestMethod]
        public void ParseGroupConfig_ComplexRealWorldScenario_ParsesCorrectly()
        {
            // Arrange - Based on actual .docsproj template from the project
            var xml = """
                <Navigation>
                    <Pages>
                        <Groups>
                            <Group Name="Getting Started" Icon="stars">
                                <Pages>index;quickstart</Pages>
                            </Group>
                            <Group Name="Guides" Icon="dog-leashed">
                                <Pages>guides/index;guides/conceptual-docs;guides/pipeline</Pages>
                            </Group>
                            <Group Name="Providers" Icon="books">
                                <Pages>providers/index</Pages>
                                <Groups>
                                    <Group Name="Mintlify" Icon="/mintlify.svg" Tag="PARTNER">
                                        <Pages>providers/mintlify/index</Pages>
                                    </Group>
                                </Groups>
                            </Group>
                            <Group Name="Plugins" Icon="outlet">
                                <Pages>plugins/index</Pages>
                            </Group>
                            <Group Name="Learnings" Icon="">
                                <Pages>learnings/bridge-assemblies;learnings/sdk-packaging</Pages>
                            </Group>
                        </Groups>
                    </Pages>
                </Navigation>
                """;
            var navigationElement = XElement.Parse(xml);
            var groupsElement = navigationElement.Element("Pages")?.Element("Groups");

            // Act
            var results = new List<GroupConfig>();
            if (groupsElement is not null)
            {
                foreach (var groupElement in groupsElement.Elements("Group"))
                {
                    var group = _task.ParseGroupConfig(groupElement);
                    if (group is not null)
                    {
                        results.Add(group);
                    }
                }
            }

            // Assert
            results.Should().HaveCount(5);

            // Verify order and properties
            results[0].Group.Should().Be("Getting Started");
            results[0].Icon!.Name.Should().Be("stars");
            results[0].Pages.Should().HaveCount(2);

            results[1].Group.Should().Be("Guides");
            results[1].Icon!.Name.Should().Be("dog-leashed");
            results[1].Pages.Should().HaveCount(3);

            results[2].Group.Should().Be("Providers");
            results[2].Icon!.Name.Should().Be("books");
            results[2].Pages.Should().HaveCount(2); // 1 direct page + 1 nested group

            // Verify nested group
            var nestedGroup = results[2].Pages![1] as GroupConfig;
            nestedGroup.Should().NotBeNull();
            nestedGroup!.Group.Should().Be("Mintlify");
            nestedGroup.Icon!.Name.Should().Be("/mintlify.svg");
            nestedGroup.Tag.Should().Be("PARTNER");

            results[3].Group.Should().Be("Plugins");
            results[3].Icon!.Name.Should().Be("outlet");

            results[4].Group.Should().Be("Learnings");
            results[4].Icon!.Name.Should().Be(""); // Empty icon is valid
        }

        #endregion

    }

}