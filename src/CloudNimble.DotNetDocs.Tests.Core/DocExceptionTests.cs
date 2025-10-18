using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Tests.Shared;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Core
{

    /// <summary>
    /// Tests for the <see cref="DocException"/> class.
    /// </summary>
    [TestClass]
    public class DocExceptionTests : DotNetDocsTestBase
    {

        #region Public Methods

        [TestMethod]
        public void Constructor_CreatesInstanceWithNullProperties()
        {
            var exception = new DocException();

            exception.Type.Should().BeNull();
            exception.Description.Should().BeNull();
        }

        [TestMethod]
        public void Properties_CanBeSetAndRetrieved()
        {
            var exception = new DocException
            {
                Type = "ArgumentNullException",
                Description = "Thrown when the parameter is null."
            };

            exception.Type.Should().Be("ArgumentNullException");
            exception.Description.Should().Be("Thrown when the parameter is null.");
        }

        [TestMethod]
        public void Properties_CanBeSetToNull()
        {
            var exception = new DocException
            {
                Type = "InvalidOperationException",
                Description = "Some description"
            };

            exception.Type = null;
            exception.Description = null;

            exception.Type.Should().BeNull();
            exception.Description.Should().BeNull();
        }


        [TestMethod]
        public void Type_HandlesFullyQualifiedNames()
        {
            var exception = new DocException
            {
                Type = "System.ArgumentNullException",
                Description = "Parameter cannot be null."
            };

            exception.Type.Should().Be("System.ArgumentNullException");
            exception.Type.Should().Contain(".");
        }

        [TestMethod]
        public void Description_HandlesMultilineText()
        {
            var multilineDescription = @"Thrown when the operation is invalid.
This can happen in several scenarios:
- Object is in an invalid state
- Method called at inappropriate time";

            var exception = new DocException
            {
                Type = "InvalidOperationException",
                Description = multilineDescription
            };

            exception.Description.Should().Be(multilineDescription);
            exception.Description.Should().Contain("\n");
        }

        [TestMethod]
        public void Description_HandlesSpecialCharacters()
        {
            var descriptionWithSpecialChars = "Thrown when value < 0 or value > 100 (value must be in range [0, 100])";

            var exception = new DocException
            {
                Type = "ArgumentOutOfRangeException",
                Description = descriptionWithSpecialChars
            };

            exception.Description.Should().Be(descriptionWithSpecialChars);
            exception.Description.Should().Contain("<");
            exception.Description.Should().Contain(">");
            exception.Description.Should().Contain("[");
            exception.Description.Should().Contain("]");
        }

        #endregion

    }

}