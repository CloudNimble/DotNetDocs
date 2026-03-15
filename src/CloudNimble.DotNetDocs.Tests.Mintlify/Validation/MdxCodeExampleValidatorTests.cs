using System.Collections.Generic;
using CloudNimble.DotNetDocs.Mintlify.Validation;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Mintlify.Validation
{

    /// <summary>
    /// Tests for <see cref="MdxCodeExampleValidator"/>, verifying that code examples extracted
    /// from MDX files are correctly validated via Roslyn compilation.
    /// </summary>
    [TestClass]
    public class MdxCodeExampleValidatorTests
    {

        #region Validate Tests

        [TestMethod]
        public void Validate_EmptyList_ReturnsNoErrors()
        {
            var examples = new List<CodeExample>();

            var errors = MdxCodeExampleValidator.Validate(examples);

            errors.Should().BeEmpty();
        }

        [TestMethod]
        public void Validate_InvalidCode_ReturnsErrors()
        {
            var examples = new List<CodeExample>
            {
                new()
                {
                    Code = "Console.WriteLine(undefinedVariable);",
                    SourceFile = "broken.mdx"
                }
            };

            var errors = MdxCodeExampleValidator.Validate(examples);

            errors.Should().HaveCount(1);
            errors[0].SourceFile.Should().Be("broken.mdx");
            errors[0].Code.Should().Contain("undefinedVariable");
            errors[0].Diagnostics.Should().NotBeEmpty();
        }

        [TestMethod]
        public void Validate_MixedValidAndInvalidCode_OnlyReportsInvalid()
        {
            var examples = new List<CodeExample>
            {
                new()
                {
                    Code = "Console.WriteLine(\"Valid code\");",
                    SourceFile = "valid.mdx"
                },
                new()
                {
                    Code = "Console.WriteLine(missingVar);",
                    SourceFile = "invalid.mdx"
                }
            };

            var errors = MdxCodeExampleValidator.Validate(examples);

            errors.Should().HaveCount(1);
            errors[0].SourceFile.Should().Be("invalid.mdx");
        }

        [TestMethod]
        public void Validate_ValidCode_ReturnsNoErrors()
        {
            var examples = new List<CodeExample>
            {
                new()
                {
                    Code = "Console.WriteLine(\"Hello, World!\");",
                    SourceFile = "hello.mdx"
                }
            };

            var errors = MdxCodeExampleValidator.Validate(examples);

            errors.Should().BeEmpty();
        }

        #endregion

    }

}
