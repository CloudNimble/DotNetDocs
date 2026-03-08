using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core.Models;

namespace Mintlify.Tests.Core.Models
{

    /// <summary>
    /// Tests for the <see cref="ThemePairConfig"/> base class that provides Light/Dark theme pair properties.
    /// </summary>
    [TestClass]
    public class ThemePairConfigTests
    {

        #region Constructor Tests

        /// <summary>
        /// Tests that a default-constructed ThemePairConfig has null Light and Dark properties.
        /// </summary>
        [TestMethod]
        public void Constructor_Default_LightAndDarkAreNull()
        {
            var config = new ThemePairConfig();

            config.Light.Should().BeNull();
            config.Dark.Should().BeNull();
        }

        #endregion

        #region ToString Tests

        /// <summary>
        /// Tests that ToString returns the Light value when it is set.
        /// </summary>
        [TestMethod]
        public void ToString_WithLight_ReturnsLight()
        {
            var config = new ThemePairConfig { Light = "light-value" };

            config.ToString().Should().Be("light-value");
        }

        /// <summary>
        /// Tests that ToString returns the Dark value when only Dark is set.
        /// </summary>
        [TestMethod]
        public void ToString_WithDarkOnly_ReturnsDark()
        {
            var config = new ThemePairConfig { Dark = "dark-value" };

            config.ToString().Should().Be("dark-value");
        }

        /// <summary>
        /// Tests that ToString returns empty string when both Light and Dark are null.
        /// </summary>
        [TestMethod]
        public void ToString_BothNull_ReturnsEmpty()
        {
            var config = new ThemePairConfig();

            config.ToString().Should().Be(string.Empty);
        }

        /// <summary>
        /// Tests that ToString prefers Light over Dark when both are set.
        /// </summary>
        [TestMethod]
        public void ToString_BothSet_PrefersLight()
        {
            var config = new ThemePairConfig { Light = "light", Dark = "dark" };

            config.ToString().Should().Be("light");
        }

        #endregion

        #region Implicit Operator Tests

        /// <summary>
        /// Tests that implicit conversion from string creates a ThemePairConfig with both Light and Dark set.
        /// </summary>
        [TestMethod]
        public void ImplicitStringOperator_FromString_SetsBothLightAndDark()
        {
            ThemePairConfig? config = "test-value";

            config.Should().NotBeNull();
            config!.Light.Should().Be("test-value");
            config!.Dark.Should().Be("test-value");
        }

        /// <summary>
        /// Tests that implicit conversion from null string returns null.
        /// </summary>
        [TestMethod]
        public void ImplicitStringOperator_FromNullString_ReturnsNull()
        {
            ThemePairConfig? config = (string?)null;

            config.Should().BeNull();
        }

        /// <summary>
        /// Tests that implicit conversion to string returns the Light value.
        /// </summary>
        [TestMethod]
        public void ImplicitStringOperator_ToStringFromConfig_ReturnsLight()
        {
            var config = new ThemePairConfig { Light = "light-val", Dark = "dark-val" };

            string? result = config;

            result.Should().Be("light-val");
        }

        /// <summary>
        /// Tests that implicit conversion to string from null config returns null.
        /// </summary>
        [TestMethod]
        public void ImplicitStringOperator_ToStringFromNull_ReturnsNull()
        {
            ThemePairConfig? config = null;

            string? result = config;

            result.Should().BeNull();
        }

        #endregion

    }

}
