using CloudNimble.DotNetDocs.WasmRunner;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.WasmRunner
{

    [TestClass]
    public class SecuritySyntaxWalkerTests
    {

        #region Safe Code Tests

        [TestMethod]
        public void Validate_SafeCode_ReturnsNoIssues()
        {
            var code = @"
using System;
using System.Linq;

Console.WriteLine(""Hello"");
var nums = new[] { 1, 2, 3 }.Where(n => n > 1);
";
            var walker = new SecuritySyntaxWalker();
            var tree = CSharpSyntaxTree.ParseText(code);
            walker.Visit(tree.GetRoot());

            walker.HasSecurityIssues.Should().BeFalse();
            walker.BlockedUsings.Should().BeEmpty();
        }

        #endregion

        #region Blocked Namespace Tests

        [TestMethod]
        public void Validate_SystemIO_IsBlocked()
        {
            var code = @"using System.IO;";
            var walker = new SecuritySyntaxWalker();
            var tree = CSharpSyntaxTree.ParseText(code);
            walker.Visit(tree.GetRoot());

            walker.HasSecurityIssues.Should().BeTrue();
            walker.BlockedUsings.Should().Contain("System.IO");
        }

        [TestMethod]
        public void Validate_SystemNet_IsBlocked()
        {
            var code = @"using System.Net;";
            var walker = new SecuritySyntaxWalker();
            var tree = CSharpSyntaxTree.ParseText(code);
            walker.Visit(tree.GetRoot());

            walker.HasSecurityIssues.Should().BeTrue();
            walker.BlockedUsings.Should().Contain("System.Net");
        }

        [TestMethod]
        public void Validate_SystemReflection_IsBlocked()
        {
            var code = @"using System.Reflection;";
            var walker = new SecuritySyntaxWalker();
            var tree = CSharpSyntaxTree.ParseText(code);
            walker.Visit(tree.GetRoot());

            walker.HasSecurityIssues.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_SystemRuntimeInteropServices_IsBlocked()
        {
            var code = @"using System.Runtime.InteropServices;";
            var walker = new SecuritySyntaxWalker();
            var tree = CSharpSyntaxTree.ParseText(code);
            walker.Visit(tree.GetRoot());

            walker.HasSecurityIssues.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_SystemDiagnostics_IsBlocked()
        {
            var code = @"using System.Diagnostics;";
            var walker = new SecuritySyntaxWalker();
            var tree = CSharpSyntaxTree.ParseText(code);
            walker.Visit(tree.GetRoot());

            walker.HasSecurityIssues.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_MultipleBlockedUsings_ReportsAll()
        {
            var code = @"
using System.IO;
using System.Net;
using System.Diagnostics;
";
            var walker = new SecuritySyntaxWalker();
            var tree = CSharpSyntaxTree.ParseText(code);
            walker.Visit(tree.GetRoot());

            walker.HasSecurityIssues.Should().BeTrue();
            walker.BlockedUsings.Should().HaveCount(3);
        }

        #endregion

        #region Allowed Namespace Tests

        [TestMethod]
        [DataRow("System")]
        [DataRow("System.Collections.Generic")]
        [DataRow("System.Linq")]
        [DataRow("System.Text")]
        [DataRow("System.Text.RegularExpressions")]
        [DataRow("System.Threading.Tasks")]
        public void Validate_AllowedNamespace_ReturnsNoIssues(string ns)
        {
            var code = $"using {ns};";
            var walker = new SecuritySyntaxWalker();
            var tree = CSharpSyntaxTree.ParseText(code);
            walker.Visit(tree.GetRoot());

            walker.HasSecurityIssues.Should().BeFalse();
        }

        #endregion

    }

}
