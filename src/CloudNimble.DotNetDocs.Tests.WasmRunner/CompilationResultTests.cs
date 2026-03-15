using CloudNimble.DotNetDocs.WasmRunner;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.WasmRunner
{

    [TestClass]
    public class CompilationResultTests
    {

        #region Success Tests

        [TestMethod]
        public void Success_WithOutput_SetsPropertiesCorrectly()
        {
            var result = CompilationResult.Success("Hello, World!");

            result.IsSuccess.Should().BeTrue();
            result.Output.Should().Be("Hello, World!");
            result.Diagnostics.Should().BeEmpty();
        }

        [TestMethod]
        public void Success_WithEmptyOutput_HasEmptyOutputString()
        {
            var result = CompilationResult.Success("");

            result.IsSuccess.Should().BeTrue();
            result.Output.Should().BeEmpty();
            result.Diagnostics.Should().BeEmpty();
        }

        #endregion

        #region Failure Tests

        [TestMethod]
        public void Failure_WithDiagnostics_SetsPropertiesCorrectly()
        {
            var result = CompilationResult.Failure("CS1002: ; expected");

            result.IsSuccess.Should().BeFalse();
            result.Output.Should().BeEmpty();
            result.Diagnostics.Should().Be("CS1002: ; expected");
        }

        #endregion

    }

}
