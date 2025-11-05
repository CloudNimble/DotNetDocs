using System.Collections.Generic;
using System.Xml.Linq;
using CloudNimble.DotNetDocs.Sdk.Tasks;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
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

        #region Styling Configuration Tests

        /// <summary>
        /// Tests that ParseStylingConfig correctly parses CodeBlocks property.
        /// </summary>
        [TestMethod]
        public void ParseStylingConfig_WithCodeBlocks_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Styling>
                    <CodeBlocks>dark</CodeBlocks>
                </Styling>
                """;
            var stylingElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseStylingConfig(stylingElement);

            // Assert
            result.Should().NotBeNull();
            result.Codeblocks.Should().Be("dark");
        }

        /// <summary>
        /// Tests that ParseStylingConfig correctly parses Eyebrows property.
        /// </summary>
        [TestMethod]
        public void ParseStylingConfig_WithEyebrows_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Styling>
                    <Eyebrows>subtle</Eyebrows>
                </Styling>
                """;
            var stylingElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseStylingConfig(stylingElement);

            // Assert
            result.Should().NotBeNull();
            result.Eyebrows.Should().Be("subtle");
        }

        /// <summary>
        /// Tests that ParseStylingConfig correctly parses both properties.
        /// </summary>
        [TestMethod]
        public void ParseStylingConfig_WithAllProperties_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Styling>
                    <CodeBlocks>dark</CodeBlocks>
                    <Eyebrows>subtle</Eyebrows>
                </Styling>
                """;
            var stylingElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseStylingConfig(stylingElement);

            // Assert
            result.Should().NotBeNull();
            result.Codeblocks.Should().Be("dark");
            result.Eyebrows.Should().Be("subtle");
        }

        /// <summary>
        /// Tests that ParseStylingConfig handles empty styling correctly.
        /// </summary>
        [TestMethod]
        public void ParseStylingConfig_WithEmptyElement_ReturnsEmptyConfig()
        {
            // Arrange
            var xml = "<Styling></Styling>";
            var stylingElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseStylingConfig(stylingElement);

            // Assert
            result.Should().NotBeNull();
            result.Codeblocks.Should().BeNull();
            result.Eyebrows.Should().BeNull();
        }

        #endregion

        #region Appearance Configuration Tests

        /// <summary>
        /// Tests that ParseAppearanceConfig correctly parses Default property.
        /// </summary>
        [TestMethod]
        public void ParseAppearanceConfig_WithDefault_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Appearance>
                    <Default>dark</Default>
                </Appearance>
                """;
            var appearanceElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseAppearanceConfig(appearanceElement);

            // Assert
            result.Should().NotBeNull();
            result.Default.Should().Be("dark");
        }

        /// <summary>
        /// Tests that ParseAppearanceConfig correctly parses Strict property.
        /// </summary>
        [TestMethod]
        public void ParseAppearanceConfig_WithStrict_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Appearance>
                    <Strict>true</Strict>
                </Appearance>
                """;
            var appearanceElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseAppearanceConfig(appearanceElement);

            // Assert
            result.Should().NotBeNull();
            result.Strict.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ParseAppearanceConfig correctly parses both properties.
        /// </summary>
        [TestMethod]
        public void ParseAppearanceConfig_WithAllProperties_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Appearance>
                    <Default>light</Default>
                    <Strict>false</Strict>
                </Appearance>
                """;
            var appearanceElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseAppearanceConfig(appearanceElement);

            // Assert
            result.Should().NotBeNull();
            result.Default.Should().Be("light");
            result.Strict.Should().BeFalse();
        }

        /// <summary>
        /// Tests that ParseAppearanceConfig handles system default correctly.
        /// </summary>
        [TestMethod]
        public void ParseAppearanceConfig_WithSystemDefault_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Appearance>
                    <Default>system</Default>
                </Appearance>
                """;
            var appearanceElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseAppearanceConfig(appearanceElement);

            // Assert
            result.Should().NotBeNull();
            result.Default.Should().Be("system");
        }

        /// <summary>
        /// Tests that ParseAppearanceConfig handles empty appearance correctly.
        /// </summary>
        [TestMethod]
        public void ParseAppearanceConfig_WithEmptyElement_ReturnsEmptyConfig()
        {
            // Arrange
            var xml = "<Appearance></Appearance>";
            var appearanceElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseAppearanceConfig(appearanceElement);

            // Assert
            result.Should().NotBeNull();
            result.Default.Should().BeNull();
            result.Strict.Should().BeNull();
        }

        /// <summary>
        /// Tests that ParseAppearanceConfig ignores invalid boolean values for Strict.
        /// </summary>
        [TestMethod]
        public void ParseAppearanceConfig_WithInvalidStrictValue_IgnoresValue()
        {
            // Arrange
            var xml = """
                <Appearance>
                    <Strict>invalid</Strict>
                </Appearance>
                """;
            var appearanceElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseAppearanceConfig(appearanceElement);

            // Assert
            result.Should().NotBeNull();
            result.Strict.Should().BeNull(); // Invalid value should be ignored
        }

        #endregion

        #region Interaction Configuration Tests

        /// <summary>
        /// Tests that ParseInteractionConfig correctly parses Drilldown property set to true.
        /// </summary>
        [TestMethod]
        public void ParseInteractionConfig_WithDrilldownTrue_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Interaction>
                    <Drilldown>true</Drilldown>
                </Interaction>
                """;
            var interactionElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseInteractionConfig(interactionElement);

            // Assert
            result.Should().NotBeNull();
            result.Drilldown.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ParseInteractionConfig correctly parses Drilldown property set to false.
        /// </summary>
        [TestMethod]
        public void ParseInteractionConfig_WithDrilldownFalse_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Interaction>
                    <Drilldown>false</Drilldown>
                </Interaction>
                """;
            var interactionElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseInteractionConfig(interactionElement);

            // Assert
            result.Should().NotBeNull();
            result.Drilldown.Should().BeFalse();
        }

        /// <summary>
        /// Tests that ParseInteractionConfig handles empty interaction correctly.
        /// </summary>
        [TestMethod]
        public void ParseInteractionConfig_WithEmptyElement_ReturnsEmptyConfig()
        {
            // Arrange
            var xml = "<Interaction></Interaction>";
            var interactionElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseInteractionConfig(interactionElement);

            // Assert
            result.Should().NotBeNull();
            result.Drilldown.Should().BeNull();
        }

        /// <summary>
        /// Tests that ParseInteractionConfig ignores invalid boolean values for Drilldown.
        /// </summary>
        [TestMethod]
        public void ParseInteractionConfig_WithInvalidDrilldownValue_IgnoresValue()
        {
            // Arrange
            var xml = """
                <Interaction>
                    <Drilldown>invalid</Drilldown>
                </Interaction>
                """;
            var interactionElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseInteractionConfig(interactionElement);

            // Assert
            result.Should().NotBeNull();
            result.Drilldown.Should().BeNull(); // Invalid value should be ignored
        }

        /// <summary>
        /// Tests that ParseInteractionConfig handles case-insensitive boolean values.
        /// </summary>
        [TestMethod]
        public void ParseInteractionConfig_WithCaseInsensitiveBooleans_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Interaction>
                    <Drilldown>True</Drilldown>
                </Interaction>
                """;
            var interactionElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseInteractionConfig(interactionElement);

            // Assert
            result.Should().NotBeNull();
            result.Drilldown.Should().BeTrue();
        }

        #endregion

        #region DocumentationReference Integration Tests

        [TestMethod]
        public void Task_WithNoResolvedDocumentationReferences_ExecutesWithoutReferences()
        {
            _task.ResolvedDocumentationReferences = null;

            _task.ResolvedDocumentationReferences.Should().BeNull();
        }

        [TestMethod]
        public void Task_WithEmptyResolvedDocumentationReferences_ExecutesWithoutReferences()
        {
            _task.ResolvedDocumentationReferences = [];

            _task.ResolvedDocumentationReferences.Should().BeEmpty();
        }

        [TestMethod]
        public void Task_CanAcceptResolvedDocumentationReferences()
        {
            var references = new ITaskItem[]
            {
                new TaskItem("ServiceA.docsproj")
            };
            references[0].SetMetadata("ProjectPath", @"D:\projects\ServiceA\ServiceA.docsproj");
            references[0].SetMetadata("DocumentationRoot", @"D:\projects\ServiceA\docs");
            references[0].SetMetadata("DestinationPath", "services/service-a");
            references[0].SetMetadata("IntegrationType", "Tabs");
            references[0].SetMetadata("DocumentationType", "Mintlify");
            references[0].SetMetadata("NavigationFilePath", @"D:\projects\ServiceA\docs\docs.json");

            _task.ResolvedDocumentationReferences = references;

            _task.ResolvedDocumentationReferences.Should().HaveCount(1);
            _task.ResolvedDocumentationReferences[0].GetMetadata("ProjectPath").Should().Be(@"D:\projects\ServiceA\ServiceA.docsproj");
            _task.ResolvedDocumentationReferences[0].GetMetadata("DocumentationRoot").Should().Be(@"D:\projects\ServiceA\docs");
            _task.ResolvedDocumentationReferences[0].GetMetadata("DestinationPath").Should().Be("services/service-a");
            _task.ResolvedDocumentationReferences[0].GetMetadata("IntegrationType").Should().Be("Tabs");
            _task.ResolvedDocumentationReferences[0].GetMetadata("DocumentationType").Should().Be("Mintlify");
            _task.ResolvedDocumentationReferences[0].GetMetadata("NavigationFilePath").Should().Be(@"D:\projects\ServiceA\docs\docs.json");
        }

        [TestMethod]
        public void Task_WithMultipleResolvedDocumentationReferences_AcceptsAll()
        {
            var references = new ITaskItem[]
            {
                new TaskItem("ServiceA.docsproj"),
                new TaskItem("ServiceB.docsproj"),
                new TaskItem("ServiceC.docsproj")
            };

            for (int i = 0; i < references.Length; i++)
            {
                references[i].SetMetadata("ProjectPath", $@"D:\projects\Service{(char)('A' + i)}\Service{(char)('A' + i)}.docsproj");
                references[i].SetMetadata("DocumentationRoot", $@"D:\projects\Service{(char)('A' + i)}\docs");
                references[i].SetMetadata("DestinationPath", $"services/service-{(char)('a' + i)}");
                references[i].SetMetadata("IntegrationType", "Tabs");
                references[i].SetMetadata("DocumentationType", "Mintlify");
                references[i].SetMetadata("NavigationFilePath", $@"D:\projects\Service{(char)('A' + i)}\docs\docs.json");
            }

            _task.ResolvedDocumentationReferences = references;

            _task.ResolvedDocumentationReferences.Should().HaveCount(3);

            for (int i = 0; i < 3; i++)
            {
                _task.ResolvedDocumentationReferences[i].GetMetadata("DestinationPath").Should().Be($"services/service-{(char)('a' + i)}");
            }
        }

        [TestMethod]
        public void Task_WithResolvedDocumentationReferences_AcceptsVariousIntegrationTypes()
        {
            var references = new ITaskItem[]
            {
                new TaskItem("Tabs.docsproj"),
                new TaskItem("Products.docsproj")
            };

            references[0].SetMetadata("IntegrationType", "Tabs");
            references[0].SetMetadata("DocumentationType", "Mintlify");
            references[1].SetMetadata("IntegrationType", "Products");
            references[1].SetMetadata("DocumentationType", "Mintlify");

            _task.ResolvedDocumentationReferences = references;

            _task.ResolvedDocumentationReferences.Should().HaveCount(2);
            _task.ResolvedDocumentationReferences[0].GetMetadata("IntegrationType").Should().Be("Tabs");
            _task.ResolvedDocumentationReferences[1].GetMetadata("IntegrationType").Should().Be("Products");
        }

        [TestMethod]
        public void Task_WithResolvedDocumentationReferences_AcceptsVariousDocumentationTypes()
        {
            var references = new ITaskItem[]
            {
                new TaskItem("Mintlify.docsproj"),
                new TaskItem("DocFX.docsproj"),
                new TaskItem("MkDocs.docsproj")
            };

            references[0].SetMetadata("DocumentationType", "Mintlify");
            references[1].SetMetadata("DocumentationType", "DocFX");
            references[2].SetMetadata("DocumentationType", "MkDocs");

            _task.ResolvedDocumentationReferences = references;

            _task.ResolvedDocumentationReferences.Should().HaveCount(3);
            _task.ResolvedDocumentationReferences[0].GetMetadata("DocumentationType").Should().Be("Mintlify");
            _task.ResolvedDocumentationReferences[1].GetMetadata("DocumentationType").Should().Be("DocFX");
            _task.ResolvedDocumentationReferences[2].GetMetadata("DocumentationType").Should().Be("MkDocs");
        }

        [TestMethod]
        public void Task_WithResolvedDocumentationReferences_AcceptsComplexPaths()
        {
            var references = new ITaskItem[]
            {
                new TaskItem("Service.docsproj")
            };

            references[0].SetMetadata("ProjectPath", @"..\..\services\AuthService\AuthService.docsproj");
            references[0].SetMetadata("DocumentationRoot", @"..\..\services\AuthService\docs");
            references[0].SetMetadata("DestinationPath", "services/auth");
            references[0].SetMetadata("NavigationFilePath", @"..\..\services\AuthService\docs\docs.json");
            references[0].SetMetadata("IntegrationType", "Tabs");
            references[0].SetMetadata("DocumentationType", "Mintlify");

            _task.ResolvedDocumentationReferences = references;

            _task.ResolvedDocumentationReferences[0].GetMetadata("ProjectPath").Should().Be(@"..\..\services\AuthService\AuthService.docsproj");
            _task.ResolvedDocumentationReferences[0].GetMetadata("DocumentationRoot").Should().Be(@"..\..\services\AuthService\docs");
            _task.ResolvedDocumentationReferences[0].GetMetadata("DestinationPath").Should().Be("services/auth");
        }

        [TestMethod]
        public void Task_WithResolvedDocumentationReferences_AcceptsUnixStylePaths()
        {
            var references = new ITaskItem[]
            {
                new TaskItem("Service.docsproj")
            };

            references[0].SetMetadata("ProjectPath", "/home/user/projects/ServiceA/ServiceA.docsproj");
            references[0].SetMetadata("DocumentationRoot", "/home/user/projects/ServiceA/docs");
            references[0].SetMetadata("DestinationPath", "services/service-a");
            references[0].SetMetadata("NavigationFilePath", "/home/user/projects/ServiceA/docs/docs.json");
            references[0].SetMetadata("IntegrationType", "Tabs");
            references[0].SetMetadata("DocumentationType", "Mintlify");

            _task.ResolvedDocumentationReferences = references;

            _task.ResolvedDocumentationReferences[0].GetMetadata("ProjectPath").Should().Be("/home/user/projects/ServiceA/ServiceA.docsproj");
            _task.ResolvedDocumentationReferences[0].GetMetadata("DocumentationRoot").Should().Be("/home/user/projects/ServiceA/docs");
        }

        [TestMethod]
        public void Task_WithResolvedDocumentationReferences_AcceptsEmptyNavigationFilePath()
        {
            var references = new ITaskItem[]
            {
                new TaskItem("Service.docsproj")
            };

            references[0].SetMetadata("ProjectPath", @"D:\projects\ServiceA\ServiceA.docsproj");
            references[0].SetMetadata("DocumentationRoot", @"D:\projects\ServiceA\docs");
            references[0].SetMetadata("DestinationPath", "services/service-a");
            references[0].SetMetadata("NavigationFilePath", "");
            references[0].SetMetadata("IntegrationType", "Tabs");
            references[0].SetMetadata("DocumentationType", "Mintlify");

            _task.ResolvedDocumentationReferences = references;

            _task.ResolvedDocumentationReferences[0].GetMetadata("NavigationFilePath").Should().BeEmpty();
        }

        [TestMethod]
        public void Task_WithResolvedDocumentationReferences_SupportsNestedDestinationPaths()
        {
            var references = new ITaskItem[]
            {
                new TaskItem("Service.docsproj")
            };

            references[0].SetMetadata("DestinationPath", "services/microservices/auth/v2");
            references[0].SetMetadata("DocumentationType", "Mintlify");
            references[0].SetMetadata("IntegrationType", "Tabs");

            _task.ResolvedDocumentationReferences = references;

            _task.ResolvedDocumentationReferences[0].GetMetadata("DestinationPath").Should().Be("services/microservices/auth/v2");
        }

        [TestMethod]
        public void Task_ResolvedDocumentationReferences_CanBeSetMultipleTimes()
        {
            var references1 = new ITaskItem[]
            {
                new TaskItem("ServiceA.docsproj")
            };

            _task.ResolvedDocumentationReferences = references1;
            _task.ResolvedDocumentationReferences.Should().HaveCount(1);

            var references2 = new ITaskItem[]
            {
                new TaskItem("ServiceB.docsproj"),
                new TaskItem("ServiceC.docsproj")
            };

            _task.ResolvedDocumentationReferences = references2;
            _task.ResolvedDocumentationReferences.Should().HaveCount(2);
        }

        #endregion

    }

}