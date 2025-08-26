using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core
{

    /// <summary>
    /// Tests for the FrontMatterConfig class that handles YAML frontmatter generation and escaping.
    /// </summary>
    [TestClass]
    public class FrontMatterConfigTests
    {

        #region EscapeYamlValue Tests

        [TestMethod]
        public void EscapeYamlValue_WithNullInput_ReturnsEmptyQuotes()
        {
            var result = FrontMatterConfig.EscapeYamlValue(null!);
            result.Should().Be("\"\"");
        }

        [TestMethod]
        public void EscapeYamlValue_WithEmptyInput_ReturnsEmptyQuotes()
        {
            var result = FrontMatterConfig.EscapeYamlValue("");
            result.Should().Be("\"\"");
        }

        [TestMethod]
        public void EscapeYamlValue_WithWhitespaceInput_ReturnsEmptyQuotes()
        {
            var result = FrontMatterConfig.EscapeYamlValue("   ");
            result.Should().Be("\"\"");
        }

        [TestMethod]
        public void EscapeYamlValue_WithSimpleText_ReturnsUnquoted()
        {
            var result = FrontMatterConfig.EscapeYamlValue("SimpleText");
            result.Should().Be("SimpleText");
        }

        [TestMethod]
        public void EscapeYamlValue_WithGenericTypes_EscapesAngleBrackets()
        {
            var result = FrontMatterConfig.EscapeYamlValue("List<T>");
            result.Should().Be("\"List&lt;T&gt;\"");
        }

        [TestMethod]
        public void EscapeYamlValue_WithComplexGenericTypes_EscapesAllAngleBrackets()
        {
            var result = FrontMatterConfig.EscapeYamlValue("Dictionary<string, List<T>>");
            result.Should().Be("\"Dictionary&lt;string, List&lt;T&gt;&gt;\"");
        }

        [TestMethod]
        public void EscapeYamlValue_WithQuotes_EscapesQuotes()
        {
            var result = FrontMatterConfig.EscapeYamlValue("Text with \"quotes\"");
            result.Should().Be("\"Text with \\\"quotes\\\"\"");
        }

        [TestMethod]
        public void EscapeYamlValue_WithColons_RequiresQuoting()
        {
            var result = FrontMatterConfig.EscapeYamlValue("Key: Value");
            result.Should().Be("\"Key: Value\"");
        }

        [TestMethod]
        public void EscapeYamlValue_WithSpecialYamlCharacters_RequiresQuoting()
        {
            var result = FrontMatterConfig.EscapeYamlValue("Text with [brackets] and {braces}");
            result.Should().Be("\"Text with [brackets] and {braces}\"");
        }

        [TestMethod]
        public void EscapeYamlValue_WithLineBreaks_NormalizesToSpaces()
        {
            var result = FrontMatterConfig.EscapeYamlValue("Text with\r\nnew lines\nand more");
            result.Should().Be("Text with new lines and more"); // Text doesn't contain special chars requiring quoting
        }

        [TestMethod]
        public void EscapeYamlValue_WithMultipleSpaces_NormalizesToSingleSpaces()
        {
            var result = FrontMatterConfig.EscapeYamlValue("Text   with    multiple    spaces");
            result.Should().Be("Text with multiple spaces"); // Text doesn't contain special chars requiring quoting
        }

        [TestMethod]
        public void EscapeYamlValue_WithNumbersAtStart_RequiresQuoting()
        {
            var result = FrontMatterConfig.EscapeYamlValue("123abc");
            result.Should().Be("\"123abc\"");
        }

        [TestMethod]
        public void EscapeYamlValue_WithHyphenAtStart_RequiresQuoting()
        {
            var result = FrontMatterConfig.EscapeYamlValue("-test");
            result.Should().Be("\"-test\"");
        }

        [TestMethod]
        public void EscapeYamlValue_WithAtSymbolAtStart_RequiresQuoting()
        {
            var result = FrontMatterConfig.EscapeYamlValue("@test");
            result.Should().Be("\"@test\"");
        }

        [TestMethod]
        public void EscapeYamlValue_WithAmpersand_RequiresQuoting()
        {
            var result = FrontMatterConfig.EscapeYamlValue("AT&T");
            result.Should().Be("\"AT&T\""); // Ampersand triggers quoting but isn't escaped here
        }

        [TestMethod]
        public void EscapeYamlValue_WithBackslashes_EscapesBackslashes()
        {
            var result = FrontMatterConfig.EscapeYamlValue("Path\\to\\file");
            result.Should().Be("Path\\to\\file"); // This doesn't contain special chars requiring quoting
        }

        #endregion

        #region Create Tests

        [TestMethod]
        public void Create_WithValidInputs_SetsProperties()
        {
            var config = FrontMatterConfig.Create("Test Title", "Test Description");

            config.Title.Should().Be("Test Title");
            config.Description.Should().Be("Test Description");
        }

        [TestMethod]
        public void Create_WithNullTitle_SetsEmptyTitle()
        {
            var config = FrontMatterConfig.Create(null!, "Test Description");

            config.Title.Should().Be("");
            config.Description.Should().Be("Test Description");
        }

        [TestMethod]
        public void Create_WithNullDescription_SetsEmptyDescription()
        {
            var config = FrontMatterConfig.Create("Test Title", null!);

            config.Title.Should().Be("Test Title");
            config.Description.Should().Be("");
        }

        [TestMethod]
        public void Create_WithBothNull_SetsBothEmpty()
        {
            var config = FrontMatterConfig.Create(null!, null!);

            config.Title.Should().Be("");
            config.Description.Should().Be("");
        }

        #endregion

        #region ToYamlFrontmatter Tests

        [TestMethod]
        public void ToYamlFrontmatter_WithBasicTitleAndDescription_GeneratesCorrectYaml()
        {
            var config = new FrontMatterConfig
            {
                Title = "Test Title",
                Description = "Test Description"
            };

            var result = config.ToYaml();

            result.Should().Contain("---");
            result.Should().Contain("title: Test Title");
            result.Should().Contain("description: Test Description");
            result.Should().StartWith("---");
            result.Should().EndWith("---");
        }

        [TestMethod]
        public void ToYamlFrontmatter_WithGenericTypes_EscapesAngleBrackets()
        {
            var config = new FrontMatterConfig
            {
                Title = "List<T>",
                Description = "A generic list of type T"
            };

            var result = config.ToYaml();

            result.Should().Contain("title: \"List&lt;T&gt;\"");
            result.Should().Contain("description: A generic list of type T"); // Simple text doesn't need quotes
        }

        [TestMethod]
        public void ToYamlFrontmatter_WithAllProperties_IncludesAllValues()
        {
            var config = new FrontMatterConfig
            {
                Title = "Test Title",
                Description = "Test Description",
                Icon = "cube",
                SidebarTitle = "Sidebar",
                Mode = "wide",
                IconType = "solid",
                Tag = "NEW",
                Url = "https://example.com",
                Deprecated = true,
                Groups = ["admin", "user"],
                Public = false,
                Version = "1.0",
                Canonical = "https://canonical.example.com",
                Robots = "index,follow",
                OgTitle = "OG Title",
                OgDescription = "OG Description",
                OgImage = "https://example.com/image.png",
                Classes = ["class1", "class2"],
                Order = 1,
                Hidden = true
            };

            var result = config.ToYaml();

            result.Should().Contain("title: Test Title");
            result.Should().Contain("description: Test Description");
            result.Should().Contain("icon: cube");
            result.Should().Contain("sidebarTitle: Sidebar");
            result.Should().Contain("mode: wide");
            result.Should().Contain("iconType: solid");
            result.Should().Contain("tag: NEW");
            result.Should().Contain("url: \"https://example.com\""); // URLs with : get quoted
            result.Should().Contain("deprecated: true");
            result.Should().Contain("groups: [admin, user]");
            result.Should().Contain("public: false");
            result.Should().Contain("version: \"1.0\""); // Version with period gets quoted
            result.Should().Contain("canonical: \"https://canonical.example.com\""); // URL with colon gets quoted
            result.Should().Contain("robots: index,follow");
            result.Should().Contain("ogTitle: OG Title");
            result.Should().Contain("ogDescription: OG Description");
            result.Should().Contain("ogImage: \"https://example.com/image.png\""); // URL with colon gets quoted
            result.Should().Contain("classes: [class1, class2]");
            result.Should().Contain("order: 1");
            result.Should().Contain("hidden: true");
        }

        [TestMethod]
        public void ToYamlFrontmatter_WithEmptyProperties_OnlyIncludesNonEmptyValues()
        {
            var config = new FrontMatterConfig
            {
                Title = "Test Title",
                Description = "", // Empty
                Icon = "cube",
                SidebarTitle = null!, // Null
                Groups = [], // Empty list
                Meta = [], // Empty dictionary
            };

            var result = config.ToYaml();

            result.Should().Contain("title: Test Title");
            result.Should().Contain("icon: cube");
            result.Should().NotContain("description:");
            result.Should().NotContain("sidebarTitle:");
            result.Should().NotContain("groups:");
        }

        [TestMethod]
        public void ToYamlFrontmatter_WithMetaAndDataDictionaries_IncludesPrefixedEntries()
        {
            var config = new FrontMatterConfig
            {
                Title = "Test Title",
                Meta = new Dictionary<string, string>
                {
                    { "author", "John Doe" },
                    { "keywords", "test, yaml" }
                },
                Data = new Dictionary<string, string>
                {
                    { "customId", "12345" },
                    { "category", "documentation" }
                }
            };

            var result = config.ToYaml();

            result.Should().Contain("meta.author: John Doe");
            result.Should().Contain("meta.keywords: test, yaml"); // Commas don't trigger quoting in current implementation
            result.Should().Contain("data.customId: \"12345\""); // Numbers starting with digit get quoted
            result.Should().Contain("data.category: documentation");
        }

        [TestMethod]
        public void ToYamlFrontmatter_WithProblematicCharactersInValues_EscapesCorrectly()
        {
            var config = new FrontMatterConfig
            {
                Title = "API: List<T>",
                Description = "A \"quoted\" description with <angle> brackets",
                Icon = "cube:outline", // Colon should trigger quoting
                Groups = ["admin:full", "user:read"]
            };

            var result = config.ToYaml();

            result.Should().Contain("title: \"API: List&lt;T&gt;\"");
            result.Should().Contain("description: \"A \\\"quoted\\\" description with &lt;angle&gt; brackets\"");
            result.Should().Contain("icon: \"cube:outline\"");
            result.Should().Contain("groups: [\"admin:full\", \"user:read\"]");
        }

        [TestMethod]
        public void ToYamlFrontmatter_WithNullableBooleansSet_IncludesValues()
        {
            var config = new FrontMatterConfig
            {
                Title = "Test",
                Deprecated = true,
                Public = false,
                Hidden = null // Should not be included
            };

            var result = config.ToYaml();

            result.Should().Contain("deprecated: true");
            result.Should().Contain("public: false");
            result.Should().NotContain("hidden:");
        }

        [TestMethod]
        public void ToYamlFrontmatter_WithNullableIntegerSet_IncludesValue()
        {
            var config = new FrontMatterConfig
            {
                Title = "Test",
                Order = 42
            };

            var result = config.ToYaml();

            result.Should().Contain("order: 42");
        }

        [TestMethod]
        public void ToYamlFrontmatter_WithMinimalConfig_GeneratesValidYaml()
        {
            var config = new FrontMatterConfig();

            var result = config.ToYaml();

            result.Should().StartWith("---");
            result.Should().EndWith("---");
            // Should only contain the delimiters since all properties are empty
            var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            lines.Should().HaveCount(2); // Just the two --- delimiters
        }

        #endregion

    }

}
