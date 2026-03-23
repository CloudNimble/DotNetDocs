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
            mintlifyGroup.Pages.Should().ContainSingle();
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
        /// Tests that ParseStylingConfig correctly parses Codeblocks property.
        /// </summary>
        [TestMethod]
        public void ParseStylingConfig_WithCodeBlocks_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Styling>
                    <Codeblocks>dark</Codeblocks>
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
                    <Codeblocks>dark</Codeblocks>
                    <Eyebrows>subtle</Eyebrows>
                    <Latex>true</Latex>
                </Styling>
                """;
            var stylingElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseStylingConfig(stylingElement);

            // Assert
            result.Should().NotBeNull();
            result.Codeblocks.Should().Be("dark");
            result.Eyebrows.Should().Be("subtle");
            result.Latex.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ParseStylingConfig correctly parses Latex property.
        /// </summary>
        [TestMethod]
        public void ParseStylingConfig_WithLatex_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Styling>
                    <Latex>false</Latex>
                </Styling>
                """;
            var stylingElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseStylingConfig(stylingElement);

            // Assert
            result.Should().NotBeNull();
            result.Latex.Should().BeFalse();
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
            result.Latex.Should().BeNull();
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

        #region Integrations Configuration Tests - New Integrations

        /// <summary>
        /// Tests that ParseIntegrationsConfig correctly parses PostHog SessionRecording.
        /// </summary>
        [TestMethod]
        public void ParseIntegrationsConfig_WithPostHogSessionRecording_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Integrations>
                    <PostHog ApiKey="phc_123" ApiHost="https://app.posthog.com" SessionRecording="true" />
                </Integrations>
                """;
            var integrationsElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseIntegrationsConfig(integrationsElement);

            // Assert
            result.PostHog.Should().NotBeNull();
            result.PostHog!.ApiKey.Should().Be("phc_123");
            result.PostHog.ApiHost.Should().Be("https://app.posthog.com");
            result.PostHog.SessionRecording.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ParseIntegrationsConfig correctly parses Adobe integration.
        /// </summary>
        [TestMethod]
        public void ParseIntegrationsConfig_WithAdobe_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Integrations>
                    <Adobe LaunchUrl="https://assets.adobedtm.com/launch-abc123.min.js" />
                </Integrations>
                """;
            var integrationsElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseIntegrationsConfig(integrationsElement);

            // Assert
            result.Adobe.Should().NotBeNull();
            result.Adobe!.LaunchUrl.Should().Be("https://assets.adobedtm.com/launch-abc123.min.js");
        }

        /// <summary>
        /// Tests that ParseIntegrationsConfig correctly parses Clarity integration.
        /// </summary>
        [TestMethod]
        public void ParseIntegrationsConfig_WithClarity_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Integrations>
                    <Clarity ProjectId="abc123" />
                </Integrations>
                """;
            var integrationsElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseIntegrationsConfig(integrationsElement);

            // Assert
            result.Clarity.Should().NotBeNull();
            result.Clarity!.ProjectId.Should().Be("abc123");
        }

        /// <summary>
        /// Tests that ParseIntegrationsConfig correctly parses Cookies integration.
        /// </summary>
        [TestMethod]
        public void ParseIntegrationsConfig_WithCookies_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Integrations>
                    <Cookies Key="consent" Value="accepted" />
                </Integrations>
                """;
            var integrationsElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseIntegrationsConfig(integrationsElement);

            // Assert
            result.Cookies.Should().NotBeNull();
            result.Cookies!.Key.Should().Be("consent");
            result.Cookies!.Value.Should().Be("accepted");
        }

        /// <summary>
        /// Tests that ParseIntegrationsConfig correctly parses FrontChat integration.
        /// </summary>
        [TestMethod]
        public void ParseIntegrationsConfig_WithFrontChat_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Integrations>
                    <FrontChat SnippetId="snippet_abc123" />
                </Integrations>
                """;
            var integrationsElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseIntegrationsConfig(integrationsElement);

            // Assert
            result.FrontChat.Should().NotBeNull();
            result.FrontChat!.SnippetId.Should().Be("snippet_abc123");
        }

        /// <summary>
        /// Tests that ParseIntegrationsConfig correctly parses Intercom integration.
        /// </summary>
        [TestMethod]
        public void ParseIntegrationsConfig_WithIntercom_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Integrations>
                    <Intercom AppId="app_abc123" />
                </Integrations>
                """;
            var integrationsElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseIntegrationsConfig(integrationsElement);

            // Assert
            result.Intercom.Should().NotBeNull();
            result.Intercom!.AppId.Should().Be("app_abc123");
        }

        /// <summary>
        /// Tests that ParseIntegrationsConfig correctly parses Koala integration.
        /// </summary>
        [TestMethod]
        public void ParseIntegrationsConfig_WithKoala_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Integrations>
                    <Koala PublicApiKey="pk_abc123" />
                </Integrations>
                """;
            var integrationsElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseIntegrationsConfig(integrationsElement);

            // Assert
            result.Koala.Should().NotBeNull();
            result.Koala!.PublicApiKey.Should().Be("pk_abc123");
        }

        /// <summary>
        /// Tests that ParseIntegrationsConfig correctly parses Telemetry integration.
        /// </summary>
        [TestMethod]
        public void ParseIntegrationsConfig_WithTelemetry_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Integrations>
                    <Telemetry Enabled="false" />
                </Integrations>
                """;
            var integrationsElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseIntegrationsConfig(integrationsElement);

            // Assert
            result.Telemetry.Should().NotBeNull();
            result.Telemetry!.Enabled.Should().BeFalse();
        }

        #endregion

        #region Api Configuration Tests

        /// <summary>
        /// Tests that ParseApiConfig correctly parses Url property.
        /// </summary>
        [TestMethod]
        public void ParseApiConfig_WithUrl_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Api>
                    <Url>https://api.example.com</Url>
                </Api>
                """;
            var apiElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseApiConfig(apiElement);

            // Assert
            result.Should().NotBeNull();
            result.Url.Should().Be("https://api.example.com");
        }

        /// <summary>
        /// Tests that ParseApiConfig correctly parses Proxy property.
        /// </summary>
        [TestMethod]
        public void ParseApiConfig_WithProxy_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Api>
                    <Proxy>false</Proxy>
                </Api>
                """;
            var apiElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseApiConfig(apiElement);

            // Assert
            result.Should().NotBeNull();
            result.Proxy.Should().BeFalse();
        }

        /// <summary>
        /// Tests that ParseApiConfig correctly parses OpenApi spec.
        /// </summary>
        [TestMethod]
        public void ParseApiConfig_WithOpenApi_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Api>
                    <OpenApi>https://api.example.com/openapi.json</OpenApi>
                </Api>
                """;
            var apiElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseApiConfig(apiElement);

            // Assert
            result.Should().NotBeNull();
            result.OpenApi.Should().NotBeNull();
            result.OpenApi!.Source.Should().Be("https://api.example.com/openapi.json");
        }

        /// <summary>
        /// Tests that ParseApiConfig correctly parses Examples with Autogenerate.
        /// </summary>
        [TestMethod]
        public void ParseApiConfig_WithExamplesAutogenerate_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Api>
                    <Examples>
                        <Autogenerate>false</Autogenerate>
                        <Defaults>required</Defaults>
                        <Prefill>true</Prefill>
                        <Languages>javascript;python;curl</Languages>
                    </Examples>
                </Api>
                """;
            var apiElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseApiConfig(apiElement);

            // Assert
            result.Should().NotBeNull();
            result.Examples.Should().NotBeNull();
            result.Examples!.Autogenerate.Should().BeFalse();
            result.Examples.Defaults.Should().Be("required");
            result.Examples.Prefill.Should().BeTrue();
            result.Examples.Languages.Should().ContainInOrder("javascript", "python", "curl");
        }

        /// <summary>
        /// Tests that ParseApiConfig correctly parses Playground config.
        /// </summary>
        [TestMethod]
        public void ParseApiConfig_WithPlayground_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Api>
                    <Playground>
                        <Display>simple</Display>
                        <Proxy>false</Proxy>
                    </Playground>
                </Api>
                """;
            var apiElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseApiConfig(apiElement);

            // Assert
            result.Should().NotBeNull();
            result.Playground.Should().NotBeNull();
            result.Playground!.Display.Should().Be("simple");
            result.Playground.Proxy.Should().BeFalse();
        }

        /// <summary>
        /// Tests that ParseApiConfig correctly parses Params config.
        /// </summary>
        [TestMethod]
        public void ParseApiConfig_WithParams_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Api>
                    <Params>
                        <Expanded>true</Expanded>
                    </Params>
                </Api>
                """;
            var apiElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseApiConfig(apiElement);

            // Assert
            result.Should().NotBeNull();
            result.Params.Should().NotBeNull();
            result.Params!.Expanded.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ParseApiConfig handles empty element.
        /// </summary>
        [TestMethod]
        public void ParseApiConfig_WithEmptyElement_ReturnsEmptyConfig()
        {
            // Arrange
            var xml = "<Api></Api>";
            var apiElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseApiConfig(apiElement);

            // Assert
            result.Should().NotBeNull();
            result.Url.Should().BeNull();
            result.Proxy.Should().BeNull();
            result.OpenApi.Should().BeNull();
            result.AsyncApi.Should().BeNull();
            result.Playground.Should().BeNull();
            result.Params.Should().BeNull();
            result.Examples.Should().BeNull();
            result.Mdx.Should().BeNull();
        }

        #endregion

        #region Contextual Configuration Tests

        /// <summary>
        /// Tests that ParseContextualConfig correctly parses Options and Display.
        /// </summary>
        [TestMethod]
        public void ParseContextualConfig_WithAllProperties_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Contextual>
                    <Options>copy;chatgpt;claude</Options>
                    <Display>toc</Display>
                </Contextual>
                """;
            var contextualElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseContextualConfig(contextualElement);

            // Assert
            result.Should().NotBeNull();
            result.Options.Should().ContainInOrder("copy", "chatgpt", "claude");
            result.Display.Should().Be("toc");
        }

        /// <summary>
        /// Tests that ParseContextualConfig handles empty element.
        /// </summary>
        [TestMethod]
        public void ParseContextualConfig_WithEmptyElement_ReturnsEmptyConfig()
        {
            // Arrange
            var xml = "<Contextual></Contextual>";
            var contextualElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseContextualConfig(contextualElement);

            // Assert
            result.Should().NotBeNull();
            result.Options.Should().BeNull();
            result.Display.Should().BeNull();
        }

        #endregion

        #region Fonts Configuration Tests

        /// <summary>
        /// Tests that ParseFontsConfig correctly parses all top-level properties.
        /// </summary>
        [TestMethod]
        public void ParseFontsConfig_WithTopLevelProperties_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Fonts>
                    <Family>Open Sans</Family>
                    <Format>woff2</Format>
                    <Source>https://example.com/font.woff2</Source>
                    <Weight>400</Weight>
                </Fonts>
                """;
            var fontsElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseFontsConfig(fontsElement);

            // Assert
            result.Should().NotBeNull();
            result.Family.Should().Be("Open Sans");
            result.Format.Should().Be("woff2");
            result.Source.Should().Be("https://example.com/font.woff2");
            result.Weight.Should().Be(400);
        }

        /// <summary>
        /// Tests that ParseFontsConfig correctly parses Heading and Body sub-configs.
        /// </summary>
        [TestMethod]
        public void ParseFontsConfig_WithHeadingAndBody_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Fonts>
                    <Heading>
                        <Family>Playfair Display</Family>
                        <Weight>700</Weight>
                        <Source>https://example.com/heading.woff2</Source>
                        <Format>woff2</Format>
                    </Heading>
                    <Body>
                        <Family>Inter</Family>
                        <Weight>400</Weight>
                    </Body>
                </Fonts>
                """;
            var fontsElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseFontsConfig(fontsElement);

            // Assert
            result.Should().NotBeNull();
            result.Heading.Should().NotBeNull();
            result.Heading!.Family.Should().Be("Playfair Display");
            result.Heading.Weight.Should().Be(700);
            result.Heading.Source.Should().Be("https://example.com/heading.woff2");
            result.Heading.Format.Should().Be("woff2");
            result.Body.Should().NotBeNull();
            result.Body!.Family.Should().Be("Inter");
            result.Body.Weight.Should().Be(400);
        }

        /// <summary>
        /// Tests that ParseFontsConfig handles empty element.
        /// </summary>
        [TestMethod]
        public void ParseFontsConfig_WithEmptyElement_ReturnsEmptyConfig()
        {
            // Arrange
            var xml = "<Fonts></Fonts>";
            var fontsElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseFontsConfig(fontsElement);

            // Assert
            result.Should().NotBeNull();
            result.Family.Should().BeNull();
            result.Format.Should().BeNull();
            result.Source.Should().BeNull();
            result.Weight.Should().BeNull();
            result.Heading.Should().BeNull();
            result.Body.Should().BeNull();
        }

        #endregion

        #region Thumbnails Configuration Tests

        /// <summary>
        /// Tests that ParseThumbnailsConfig correctly parses all properties.
        /// </summary>
        [TestMethod]
        public void ParseThumbnailsConfig_WithAllProperties_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Thumbnails>
                    <Appearance>dark</Appearance>
                    <Background>/images/bg.png</Background>
                    <Fonts>
                        <Family>Roboto</Family>
                    </Fonts>
                </Thumbnails>
                """;
            var thumbnailsElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseThumbnailsConfig(thumbnailsElement);

            // Assert
            result.Should().NotBeNull();
            result.Appearance.Should().Be("dark");
            result.Background.Should().Be("/images/bg.png");
            result.Fonts.Should().NotBeNull();
            result.Fonts!.Family.Should().Be("Roboto");
        }

        /// <summary>
        /// Tests that ParseThumbnailsConfig handles empty element.
        /// </summary>
        [TestMethod]
        public void ParseThumbnailsConfig_WithEmptyElement_ReturnsEmptyConfig()
        {
            // Arrange
            var xml = "<Thumbnails></Thumbnails>";
            var thumbnailsElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseThumbnailsConfig(thumbnailsElement);

            // Assert
            result.Should().NotBeNull();
            result.Appearance.Should().BeNull();
            result.Background.Should().BeNull();
            result.Fonts.Should().BeNull();
        }

        #endregion

        #region Metadata Configuration Tests

        /// <summary>
        /// Tests that ParseMetadataConfig correctly parses Timestamp property.
        /// </summary>
        [TestMethod]
        public void ParseMetadataConfig_WithTimestamp_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Metadata>
                    <Timestamp>true</Timestamp>
                </Metadata>
                """;
            var metadataElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseMetadataConfig(metadataElement);

            // Assert
            result.Should().NotBeNull();
            result.Timestamp.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ParseMetadataConfig handles empty element.
        /// </summary>
        [TestMethod]
        public void ParseMetadataConfig_WithEmptyElement_ReturnsEmptyConfig()
        {
            // Arrange
            var xml = "<Metadata></Metadata>";
            var metadataElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseMetadataConfig(metadataElement);

            // Assert
            result.Should().NotBeNull();
            result.Timestamp.Should().BeNull();
        }

        #endregion

        #region Errors Configuration Tests

        /// <summary>
        /// Tests that ParseErrorsConfig correctly parses NotFound with all properties.
        /// </summary>
        [TestMethod]
        public void ParseErrorsConfig_WithAllProperties_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Errors>
                    <NotFound>
                        <Redirect>false</Redirect>
                        <Title>Oops!</Title>
                        <Description>The page you were looking for doesn't exist.</Description>
                    </NotFound>
                </Errors>
                """;
            var errorsElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseErrorsConfig(errorsElement);

            // Assert
            result.Should().NotBeNull();
            result.NotFound.Should().NotBeNull();
            result.NotFound!.Redirect.Should().BeFalse();
            result.NotFound.Title.Should().Be("Oops!");
            result.NotFound.Description.Should().Be("The page you were looking for doesn't exist.");
        }

        /// <summary>
        /// Tests that ParseErrorsConfig handles empty element.
        /// </summary>
        [TestMethod]
        public void ParseErrorsConfig_WithEmptyElement_ReturnsEmptyConfig()
        {
            // Arrange
            var xml = "<Errors></Errors>";
            var errorsElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseErrorsConfig(errorsElement);

            // Assert
            result.Should().NotBeNull();
            result.NotFound.Should().BeNull();
        }

        #endregion

        #region Tab, Anchor, Dropdown, and Product Parsing Tests

        /// <summary>
        /// Tests that ParseTabConfig correctly extracts tab name, href, and pages from XML.
        /// </summary>
        [TestMethod]
        public void ParseTabConfig_WithNameAndHref_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Tab Name="Guides" Href="/guides">
                    <Pages>
                        <Page>guides/index</Page>
                        <Page>guides/quickstart</Page>
                    </Pages>
                </Tab>
                """;
            var tabElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseTabConfig(tabElement);

            // Assert
            result.Should().NotBeNull();
            result!.Tab.Should().Be("Guides");
            result.Href.Should().Be("/guides");
            result.Pages.Should().HaveCount(2);
            result.Pages![0].Should().Be("guides/index");
            result.Pages![1].Should().Be("guides/quickstart");
        }

        /// <summary>
        /// Tests that ParseTabConfig correctly decodes HTML-encoded characters in the Name attribute.
        /// </summary>
        [TestMethod]
        public void ParseTabConfig_WithHtmlEncodedName_DecodesCorrectly()
        {
            // Arrange
            var xml = "<Tab Name=\"S&amp;S Landscape Design\" Href=\"/s-and-s\" />";
            var tabElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseTabConfig(tabElement);

            // Assert
            result.Should().NotBeNull();
            result!.Tab.Should().Be("S&S Landscape Design");
        }

        /// <summary>
        /// Tests that ParseTabConfig returns null when the Name attribute is missing.
        /// </summary>
        [TestMethod]
        public void ParseTabConfig_WithoutName_ReturnsNull()
        {
            // Arrange
            var xml = "<Tab Href=\"/guides\" />";
            var tabElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseTabConfig(tabElement);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that ParseTabConfig correctly parses nested groups within a tab.
        /// </summary>
        [TestMethod]
        public void ParseTabConfig_WithNestedGroups_ParsesHierarchy()
        {
            // Arrange
            var xml = """
                <Tab Name="API Reference" Href="/api">
                    <Pages>
                        <Groups>
                            <Group Name="Endpoints" Icon="bolt">
                                <Pages>api/index;api/auth</Pages>
                            </Group>
                        </Groups>
                    </Pages>
                </Tab>
                """;
            var tabElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseTabConfig(tabElement);

            // Assert
            result.Should().NotBeNull();
            result!.Tab.Should().Be("API Reference");
            result.Pages.Should().HaveCount(1);
            result.Pages![0].Should().BeOfType<GroupConfig>();
            var group = result.Pages![0] as GroupConfig;
            group!.Group.Should().Be("Endpoints");
            group.Icon!.Name.Should().Be("bolt");
            group.Pages.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that ParseTabConfig sets Href to null when the attribute is absent.
        /// </summary>
        [TestMethod]
        public void ParseTabConfig_WithoutHref_HasNullHref()
        {
            // Arrange
            var xml = "<Tab Name=\"Guides\" />";
            var tabElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseTabConfig(tabElement);

            // Assert
            result.Should().NotBeNull();
            result!.Href.Should().BeNull();
        }

        /// <summary>
        /// Tests that ParseTabConfig correctly sets the Icon property from the attribute.
        /// </summary>
        [TestMethod]
        public void ParseTabConfig_WithIcon_SetsIcon()
        {
            // Arrange
            var xml = "<Tab Name=\"Guides\" Icon=\"book\" />";
            var tabElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseTabConfig(tabElement);

            // Assert
            result.Should().NotBeNull();
            result!.Icon.Should().NotBeNull();
            result.Icon!.Name.Should().Be("book");
        }

        /// <summary>
        /// Tests that ParseAnchorConfig correctly extracts anchor name, href, and pages from XML.
        /// </summary>
        [TestMethod]
        public void ParseAnchorConfig_WithNameAndHref_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Anchor Name="API Reference" Href="/api" Icon="code">
                    <Pages>
                        <Page>api/index</Page>
                    </Pages>
                </Anchor>
                """;
            var anchorElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseAnchorConfig(anchorElement);

            // Assert
            result.Should().NotBeNull();
            result!.Anchor.Should().Be("API Reference");
            result.Href.Should().Be("/api");
            result.Icon!.Name.Should().Be("code");
            result.Pages.Should().HaveCount(1);
            result.Pages![0].Should().Be("api/index");
        }

        /// <summary>
        /// Tests that ParseAnchorConfig returns null when the Name attribute is missing.
        /// </summary>
        [TestMethod]
        public void ParseAnchorConfig_WithoutName_ReturnsNull()
        {
            // Arrange
            var xml = "<Anchor Href=\"/api\" />";
            var anchorElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseAnchorConfig(anchorElement);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that ParseAnchorConfig correctly parses nested tabs within an anchor.
        /// </summary>
        [TestMethod]
        public void ParseAnchorConfig_WithNestedTabs_ParsesTabs()
        {
            // Arrange
            var xml = """
                <Anchor Name="Platform" Icon="layers">
                    <Tabs>
                        <Tab Name="Core" Href="/core">
                            <Pages>
                                <Page>core/index</Page>
                            </Pages>
                        </Tab>
                        <Tab Name="Extensions" Href="/extensions" />
                    </Tabs>
                </Anchor>
                """;
            var anchorElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseAnchorConfig(anchorElement);

            // Assert
            result.Should().NotBeNull();
            result!.Anchor.Should().Be("Platform");
            result.Tabs.Should().HaveCount(2);
            result.Tabs![0].Tab.Should().Be("Core");
            result.Tabs![0].Href.Should().Be("/core");
            result.Tabs![1].Tab.Should().Be("Extensions");
        }

        /// <summary>
        /// Tests that ParseDropdownConfig correctly extracts dropdown name, href, and pages from XML.
        /// </summary>
        [TestMethod]
        public void ParseDropdownConfig_WithNameAndHref_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Dropdown Name="Products" Href="/products" Icon="grid">
                    <Pages>
                        <Page>products/index</Page>
                    </Pages>
                </Dropdown>
                """;
            var dropdownElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseDropdownConfig(dropdownElement);

            // Assert
            result.Should().NotBeNull();
            result!.Dropdown.Should().Be("Products");
            result.Href.Should().Be("/products");
            result.Icon!.Name.Should().Be("grid");
            result.Pages.Should().HaveCount(1);
            result.Pages![0].Should().Be("products/index");
        }

        /// <summary>
        /// Tests that ParseDropdownConfig returns null when the Name attribute is missing.
        /// </summary>
        [TestMethod]
        public void ParseDropdownConfig_WithoutName_ReturnsNull()
        {
            // Arrange
            var xml = "<Dropdown Href=\"/products\" />";
            var dropdownElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseDropdownConfig(dropdownElement);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that ParseDropdownConfig correctly parses nested tabs and anchors within a dropdown.
        /// </summary>
        [TestMethod]
        public void ParseDropdownConfig_WithNestedTabsAndAnchors_ParsesBoth()
        {
            // Arrange
            var xml = """
                <Dropdown Name="Platform" Icon="layers">
                    <Tabs>
                        <Tab Name="Core" Href="/core" />
                    </Tabs>
                    <Anchors>
                        <Anchor Name="API Reference" Href="/api" Icon="code" />
                    </Anchors>
                </Dropdown>
                """;
            var dropdownElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseDropdownConfig(dropdownElement);

            // Assert
            result.Should().NotBeNull();
            result!.Dropdown.Should().Be("Platform");
            result.Tabs.Should().HaveCount(1);
            result.Tabs![0].Tab.Should().Be("Core");
            result.Anchors.Should().HaveCount(1);
            result.Anchors![0].Anchor.Should().Be("API Reference");
        }

        /// <summary>
        /// Tests that ParseProductConfig correctly extracts product name, href, and pages from XML.
        /// </summary>
        [TestMethod]
        public void ParseProductConfig_WithNameAndHref_ParsesCorrectly()
        {
            // Arrange
            var xml = """
                <Product Name="CloudNimble Core" Href="/core" Icon="box">
                    <Pages>
                        <Page>core/index</Page>
                        <Page>core/quickstart</Page>
                    </Pages>
                </Product>
                """;
            var productElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseProductConfig(productElement);

            // Assert
            result.Should().NotBeNull();
            result!.Product.Should().Be("CloudNimble Core");
            result.Href.Should().Be("/core");
            result.Icon!.Name.Should().Be("box");
            result.Pages.Should().HaveCount(2);
            result.Pages![0].Should().Be("core/index");
            result.Pages![1].Should().Be("core/quickstart");
        }

        /// <summary>
        /// Tests that ParseProductConfig correctly parses the Description attribute.
        /// </summary>
        [TestMethod]
        public void ParseProductConfig_WithDescription_ParsesDescription()
        {
            // Arrange
            var xml = "<Product Name=\"Core\" Href=\"/core\" Description=\"Core platform features\" />";
            var productElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseProductConfig(productElement);

            // Assert
            result.Should().NotBeNull();
            result!.Description.Should().Be("Core platform features");
        }

        /// <summary>
        /// Tests that ParseProductConfig returns null when the Name attribute is missing.
        /// </summary>
        [TestMethod]
        public void ParseProductConfig_WithoutName_ReturnsNull()
        {
            // Arrange
            var xml = "<Product Href=\"/core\" />";
            var productElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseProductConfig(productElement);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that ParseNavigationConfig populates Tabs and leaves Pages null when only Tabs are defined.
        /// </summary>
        [TestMethod]
        public void ParseNavigationConfig_WithTabsElement_PopulatesTabsNotPages()
        {
            // Arrange
            var xml = """
                <Navigation>
                    <Tabs>
                        <Tab Name="Guides" Href="/guides" />
                        <Tab Name="API" Href="/api" />
                    </Tabs>
                </Navigation>
                """;
            var navigationElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseNavigationConfig(navigationElement);

            // Assert
            result.Should().NotBeNull();
            result.Tabs.Should().HaveCount(2);
            result.Pages.Should().BeNull();
        }

        /// <summary>
        /// Tests that ParseNavigationConfig preserves the order of tabs as defined in the template.
        /// </summary>
        [TestMethod]
        public void ParseNavigationConfig_WithTabsElement_PreservesTabOrder()
        {
            // Arrange
            var xml = """
                <Navigation>
                    <Tabs>
                        <Tab Name="S&amp;S Landscape Design" Href="/s-and-s" />
                        <Tab Name="Scott Leese Consulting" Href="/scott-leese" />
                        <Tab Name="Surf &amp; Sales" Href="/surf-sales" />
                        <Tab Name="Surf &amp; Sales Podcast" Href="/podcast" />
                        <Tab Name="What&apos;s Your Story" Href="/story" />
                    </Tabs>
                </Navigation>
                """;
            var navigationElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseNavigationConfig(navigationElement);

            // Assert
            result.Tabs.Should().HaveCount(5);
            result.Tabs![0].Tab.Should().Be("S&S Landscape Design");
            result.Tabs![1].Tab.Should().Be("Scott Leese Consulting");
            result.Tabs![2].Tab.Should().Be("Surf & Sales");
            result.Tabs![3].Tab.Should().Be("Surf & Sales Podcast");
            result.Tabs![4].Tab.Should().Be("What's Your Story");
        }

        /// <summary>
        /// Tests that ParseNavigationConfig populates Anchors when an Anchors element is present.
        /// </summary>
        [TestMethod]
        public void ParseNavigationConfig_WithAnchorsElement_PopulatesAnchors()
        {
            // Arrange
            var xml = """
                <Navigation>
                    <Anchors>
                        <Anchor Name="Docs" Href="/docs" Icon="book" />
                        <Anchor Name="API" Href="/api" Icon="code" />
                    </Anchors>
                </Navigation>
                """;
            var navigationElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseNavigationConfig(navigationElement);

            // Assert
            result.Anchors.Should().HaveCount(2);
            result.Anchors![0].Anchor.Should().Be("Docs");
            result.Anchors![1].Anchor.Should().Be("API");
            result.Pages.Should().BeNull();
        }

        /// <summary>
        /// Tests that ParseNavigationConfig populates Dropdowns when a Dropdowns element is present.
        /// </summary>
        [TestMethod]
        public void ParseNavigationConfig_WithDropdownsElement_PopulatesDropdowns()
        {
            // Arrange
            var xml = """
                <Navigation>
                    <Dropdowns>
                        <Dropdown Name="Platform" Href="/platform" Icon="layers" />
                        <Dropdown Name="Tools" Href="/tools" Icon="wrench" />
                    </Dropdowns>
                </Navigation>
                """;
            var navigationElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseNavigationConfig(navigationElement);

            // Assert
            result.Dropdowns.Should().HaveCount(2);
            result.Dropdowns![0].Dropdown.Should().Be("Platform");
            result.Dropdowns![1].Dropdown.Should().Be("Tools");
            result.Pages.Should().BeNull();
        }

        /// <summary>
        /// Tests that ParseNavigationConfig populates Products when a Products element is present.
        /// </summary>
        [TestMethod]
        public void ParseNavigationConfig_WithProductsElement_PopulatesProducts()
        {
            // Arrange
            var xml = """
                <Navigation>
                    <Products>
                        <Product Name="Core SDK" Href="/core" Icon="box" Description="The core library" />
                        <Product Name="Extensions" Href="/extensions" Icon="puzzle" />
                    </Products>
                </Navigation>
                """;
            var navigationElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseNavigationConfig(navigationElement);

            // Assert
            result.Products.Should().HaveCount(2);
            result.Products![0].Product.Should().Be("Core SDK");
            result.Products![0].Description.Should().Be("The core library");
            result.Products![1].Product.Should().Be("Extensions");
            result.Pages.Should().BeNull();
        }

        /// <summary>
        /// Tests that ParseNavigationConfig can populate both Tabs and Anchors simultaneously.
        /// </summary>
        [TestMethod]
        public void ParseNavigationConfig_WithTabsAndAnchors_PopulatesBoth()
        {
            // Arrange
            var xml = """
                <Navigation>
                    <Tabs>
                        <Tab Name="Guides" Href="/guides" />
                    </Tabs>
                    <Anchors>
                        <Anchor Name="API" Href="/api" Icon="code" />
                    </Anchors>
                </Navigation>
                """;
            var navigationElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseNavigationConfig(navigationElement);

            // Assert
            result.Tabs.Should().HaveCount(1);
            result.Tabs![0].Tab.Should().Be("Guides");
            result.Anchors.Should().HaveCount(1);
            result.Anchors![0].Anchor.Should().Be("API");
            result.Pages.Should().BeNull();
        }

        /// <summary>
        /// Tests that ParseNavigationConfig still correctly processes Pages-based navigation for backward compatibility.
        /// </summary>
        [TestMethod]
        public void ParseNavigationConfig_WithPagesElement_StillWorks()
        {
            // Arrange
            var xml = """
                <Navigation>
                    <Pages>
                        <Groups>
                            <Group Name="Getting Started" Icon="stars">
                                <Pages>index;quickstart</Pages>
                            </Group>
                        </Groups>
                    </Pages>
                </Navigation>
                """;
            var navigationElement = XElement.Parse(xml);

            // Act
            var result = _task.ParseNavigationConfig(navigationElement);

            // Assert
            result.Pages.Should().HaveCount(1);
            result.Pages![0].Should().BeOfType<GroupConfig>();
            var group = result.Pages![0] as GroupConfig;
            group!.Group.Should().Be("Getting Started");
            result.Tabs.Should().BeNull();
            result.Anchors.Should().BeNull();
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

            _task.ResolvedDocumentationReferences.Should().ContainSingle();
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
            _task.ResolvedDocumentationReferences.Should().ContainSingle();

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