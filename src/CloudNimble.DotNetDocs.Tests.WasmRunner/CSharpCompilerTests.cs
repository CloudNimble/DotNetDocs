using CloudNimble.DotNetDocs.WasmRunner;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.WasmRunner
{

    [TestClass]
    [DoNotParallelize]
    public class CSharpCompilerTests
    {

        #region Fields

        private CSharpCompiler _compiler = null!;

        #endregion

        #region Test Lifecycle

        [TestInitialize]
        public void TestInitialize()
        {
            _compiler = new CSharpCompiler();
        }

        #endregion

        #region Successful Compilation Tests

        [TestMethod]
        public void CompileAndRun_HelloWorld_ReturnsOutput()
        {
            var code = @"
using System;
Console.WriteLine(""Hello, World!"");
";
            var result = _compiler.CompileAndRun(code);

            result.IsSuccess.Should().BeTrue();
            result.Output.Should().Contain("Hello, World!");
        }

        [TestMethod]
        public void CompileAndRun_MultipleWriteLines_CapturesAll()
        {
            var code = @"
using System;
Console.WriteLine(""Line 1"");
Console.WriteLine(""Line 2"");
Console.WriteLine(""Line 3"");
";
            var result = _compiler.CompileAndRun(code);

            result.IsSuccess.Should().BeTrue();
            result.Output.Should().Contain("Line 1");
            result.Output.Should().Contain("Line 2");
            result.Output.Should().Contain("Line 3");
        }

        [TestMethod]
        public void CompileAndRun_WithLinq_WorksCorrectly()
        {
            var code = @"
using System;
using System.Linq;

var numbers = new[] { 1, 2, 3, 4, 5 };
var even = numbers.Where(n => n % 2 == 0);
Console.WriteLine(string.Join("", "", even));
";
            var result = _compiler.CompileAndRun(code);

            result.IsSuccess.Should().BeTrue();
            result.Output.Should().Contain("2, 4");
        }

        [TestMethod]
        public void CompileAndRun_StringInterpolation_Works()
        {
            var code = @"
using System;
var name = ""World"";
Console.WriteLine($""Hello, {name}!"");
";
            var result = _compiler.CompileAndRun(code);

            result.IsSuccess.Should().BeTrue();
            result.Output.Should().Contain("Hello, World!");
        }

        #endregion

        #region Compilation Error Tests

        [TestMethod]
        public void CompileAndRun_SyntaxError_ReturnsDiagnostics()
        {
            var code = @"Console.WriteLine(""missing semicolon"")";

            var result = _compiler.CompileAndRun(code);

            result.IsSuccess.Should().BeFalse();
            result.Diagnostics.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public void CompileAndRun_UndefinedVariable_ReturnsDiagnostics()
        {
            var code = @"Console.WriteLine(undefinedVariable);";

            var result = _compiler.CompileAndRun(code);

            result.IsSuccess.Should().BeFalse();
            result.Diagnostics.Should().NotBeNullOrWhiteSpace();
        }

        #endregion

        #region Security Tests

        [TestMethod]
        public void CompileAndRun_BlockedNamespace_ReturnsDiagnostics()
        {
            var code = @"
using System.IO;
File.ReadAllText(""secret.txt"");
";
            var result = _compiler.CompileAndRun(code);

            result.IsSuccess.Should().BeFalse();
            result.Diagnostics.Should().Contain("System.IO");
        }

        [TestMethod]
        public void CompileAndRun_SystemNet_ReturnsDiagnostics()
        {
            var code = @"
using System.Net.Http;
var client = new HttpClient();
";
            var result = _compiler.CompileAndRun(code);

            result.IsSuccess.Should().BeFalse();
            result.Diagnostics.Should().Contain("System.Net");
        }

        #endregion

        #region Edge Case Tests

        [TestMethod]
        public void CompileAndRun_EmptyCode_ReturnsSuccess()
        {
            var result = _compiler.CompileAndRun("");

            result.IsSuccess.Should().BeTrue();
            result.Output.Should().BeEmpty();
        }

        [TestMethod]
        public void CompileAndRun_WhitespaceOnly_ReturnsSuccess()
        {
            var result = _compiler.CompileAndRun("   \n  \n  ");

            result.IsSuccess.Should().BeTrue();
        }

        #endregion

    }

}
