using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;
using Mintlify.Core.Converters;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core
{

    /// <summary>
    /// Tests for the IconConverter class to ensure proper serialization and deserialization behavior.
    /// </summary>
    [TestClass]
    public class IconConverterTests
    {

        #region Converter Instance Tests

        /// <summary>
        /// Tests that OptionsWithoutThis excludes the IconConverter to prevent infinite recursion.
        /// </summary>
        [TestMethod]
        public void OptionsWithoutThis_ExcludesIconConverter()
        {
            var originalOptions = MintlifyConstants.JsonSerializerOptions;
            var optionsWithoutThis = IconConverter.OptionsWithoutThis;

            // Should not be the same instance
            optionsWithoutThis.Should().NotBeSameAs(originalOptions);

            // Original should have IconConverter
            originalOptions.Converters.Should().Contain(c => c is IconConverter);

            // OptionsWithoutThis should NOT have IconConverter
            optionsWithoutThis.Converters.Should().NotContain(c => c is IconConverter);

            // Should preserve other important settings
            optionsWithoutThis.PropertyNamingPolicy.Should().Be(originalOptions.PropertyNamingPolicy);
            optionsWithoutThis.DefaultIgnoreCondition.Should().Be(originalOptions.DefaultIgnoreCondition);
        }

        /// <summary>
        /// Tests that OptionsWithoutThis preserves other converters while excluding IconConverter.
        /// </summary>
        [TestMethod]
        public void OptionsWithoutThis_PreservesOtherConverters()
        {
            var originalOptions = MintlifyConstants.JsonSerializerOptions;
            var optionsWithoutThis = IconConverter.OptionsWithoutThis;

            // Should have other converters but not IconConverter
            optionsWithoutThis.Converters.Should().Contain(c => c is ApiConfigConverter);
            optionsWithoutThis.Converters.Should().Contain(c => c is ServerConfigConverter);
            optionsWithoutThis.Converters.Should().NotContain(c => c is IconConverter);

            // Should have fewer converters than original
            optionsWithoutThis.Converters.Count.Should().BeLessThan(originalOptions.Converters.Count);
        }

        #endregion

        #region String Serialization Tests

        /// <summary>
        /// Tests that simple icon names are serialized as strings.
        /// </summary>
        [TestMethod]
        public void Serialize_SimpleIconName_ReturnsString()
        {
            var icon = new IconConfig { Name = "folder" };

            var json = JsonSerializer.Serialize(icon, MintlifyConstants.JsonSerializerOptions);

            json.Should().Be("\"folder\"");
        }

        /// <summary>
        /// Tests that simple icon names are deserialized from strings.
        /// </summary>
        [TestMethod]
        public void Deserialize_StringIcon_ReturnsIconConfig()
        {
            var json = "\"folder\"";

            var icon = JsonSerializer.Deserialize<IconConfig>(json, MintlifyConstants.JsonSerializerOptions);

            icon.Should().NotBeNull();
            icon!.Name.Should().Be("folder");
            icon.Library.Should().BeNull();
            icon.Style.Should().BeNull();
        }

        #endregion

        #region Object Serialization Tests

        /// <summary>
        /// Tests that complex icon configurations are serialized as objects.
        /// </summary>
        [TestMethod]
        public void Serialize_ComplexIcon_ReturnsObject()
        {
            var icon = new IconConfig
            {
                Name = "home",
                Library = "fontawesome",
                Style = "solid"
            };

            var json = JsonSerializer.Serialize(icon, MintlifyConstants.JsonSerializerOptions);

            json.Should().Contain("\"name\": \"home\"");
            json.Should().Contain("\"library\": \"fontawesome\"");
            json.Should().Contain("\"style\": \"solid\"");
        }

        /// <summary>
        /// Tests that complex icon configurations are deserialized from objects.
        /// </summary>
        [TestMethod]
        public void Deserialize_ObjectIcon_ReturnsIconConfig()
        {
            var json = "{\"name\":\"home\",\"library\":\"fontawesome\",\"style\":\"solid\"}";

            var icon = JsonSerializer.Deserialize<IconConfig>(json, MintlifyConstants.JsonSerializerOptions);

            icon.Should().NotBeNull();
            icon!.Name.Should().Be("home");
            icon.Library.Should().Be("fontawesome");
            icon.Style.Should().Be("solid");
        }

        #endregion

        #region Roundtrip Tests

        /// <summary>
        /// Tests that simple icons can roundtrip through serialization without stack overflow.
        /// </summary>
        [TestMethod]
        public void Roundtrip_SimpleIcon_NoStackOverflow()
        {
            var originalIcon = new IconConfig { Name = "folder" };

            var json = JsonSerializer.Serialize(originalIcon, MintlifyConstants.JsonSerializerOptions);
            var deserializedIcon = JsonSerializer.Deserialize<IconConfig>(json, MintlifyConstants.JsonSerializerOptions);

            deserializedIcon.Should().NotBeNull();
            deserializedIcon!.Name.Should().Be(originalIcon.Name);
            deserializedIcon.Library.Should().Be(originalIcon.Library);
            deserializedIcon.Style.Should().Be(originalIcon.Style);
        }

        /// <summary>
        /// Tests that complex icons can roundtrip through serialization without stack overflow.
        /// </summary>
        [TestMethod]
        public void Roundtrip_ComplexIcon_NoStackOverflow()
        {
            var originalIcon = new IconConfig
            {
                Name = "home",
                Library = "fontawesome",
                Style = "solid"
            };

            var json = JsonSerializer.Serialize(originalIcon, MintlifyConstants.JsonSerializerOptions);
            var deserializedIcon = JsonSerializer.Deserialize<IconConfig>(json, MintlifyConstants.JsonSerializerOptions);

            deserializedIcon.Should().NotBeNull();
            deserializedIcon!.Name.Should().Be(originalIcon.Name);
            deserializedIcon.Library.Should().Be(originalIcon.Library);
            deserializedIcon.Style.Should().Be(originalIcon.Style);
        }

        #endregion

        #region Nested Object Tests

        /// <summary>
        /// Tests that GroupConfig with Icon properties can be serialized without stack overflow.
        /// </summary>
        [TestMethod]
        public void Serialize_GroupConfigWithIcon_NoStackOverflow()
        {
            var group = new GroupConfig
            {
                Group = "API Reference",
                Icon = new IconConfig { Name = "folder" }
            };

            var act = () => JsonSerializer.Serialize(group, MintlifyConstants.JsonSerializerOptions);

            act.Should().NotThrow();
            var json = act();
            json.Should().Contain("\"group\": \"API Reference\"");
            json.Should().Contain("\"icon\": \"folder\"");
        }

        /// <summary>
        /// Tests that GroupConfig with complex Icon can be serialized without stack overflow.
        /// </summary>
        [TestMethod]
        public void Serialize_GroupConfigWithComplexIcon_NoStackOverflow()
        {
            var group = new GroupConfig
            {
                Group = "API Reference",
                Icon = new IconConfig
                {
                    Name = "home",
                    Library = "fontawesome",
                    Style = "solid"
                }
            };

            var act = () => JsonSerializer.Serialize(group, MintlifyConstants.JsonSerializerOptions);

            act.Should().NotThrow();
            var json = act();
            json.Should().Contain("\"group\": \"API Reference\"");
            json.Should().Contain("\"name\": \"home\"");
            json.Should().Contain("\"library\": \"fontawesome\"");
            json.Should().Contain("\"style\": \"solid\"");
        }

        #endregion

        #region Null and Edge Case Tests

        /// <summary>
        /// Tests that null icons are handled correctly.
        /// </summary>
        [TestMethod]
        public void Serialize_NullIcon_ReturnsNull()
        {
            IconConfig? icon = null;

            var json = JsonSerializer.Serialize(icon, MintlifyConstants.JsonSerializerOptions);

            json.Should().Be("null");
        }

        /// <summary>
        /// Tests that null JSON deserializes to null icon.
        /// </summary>
        [TestMethod]
        public void Deserialize_NullJson_ReturnsNull()
        {
            var json = "null";

            var icon = JsonSerializer.Deserialize<IconConfig>(json, MintlifyConstants.JsonSerializerOptions);

            icon.Should().BeNull();
        }

        #endregion

    }

}