# Try .NET: Client-Side C# Execution Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a complete client-side C# code execution system for Mintlify documentation, including a .NET WASM runner application, Mintlify React components, build-time code validation, and security sandboxing.

**Architecture:** A standalone .NET WASM application (`CloudNimble.DotNetDocs.WasmRunner`) exposes Roslyn compilation via `[JSExport]` interop. Mintlify React snippet components (`dotnet-runner.jsx`) lazy-load this runtime from a CDN on first "Run" click, execute user code client-side, and display output. Build-time validation in the Mintlify renderer scans MDX files for `<DotNetRunner>` usage and compiles embedded code during the build to catch errors early. A `SecuritySyntaxWalker` restricts dangerous namespaces.

**Tech Stack:** .NET 10 (browser-wasm), Roslyn (`Microsoft.CodeAnalysis.CSharp`), `[JSExport]`/`[JSImport]` interop, React (Mintlify snippets), MSTest v3, FluentAssertions

**Spec:** `specs/future/try-dotnet.md`

---

## File Structure

### New Projects

```
src/CloudNimble.DotNetDocs.WasmRunner/
├── CloudNimble.DotNetDocs.WasmRunner.csproj   — WASM app targeting browser-wasm
├── CSharpCompiler.cs                           — Roslyn compilation + execution wrapper
├── CompilationResult.cs                        — Structured result model
├── OutputCapture.cs                            — Console.WriteLine interception via TextWriter
├── SecuritySyntaxWalker.cs                     — Roslyn syntax walker blocking dangerous namespaces
└── Program.cs                                  — Entry point with [JSExport] methods

src/CloudNimble.DotNetDocs.Tests.WasmRunner/
├── CloudNimble.DotNetDocs.Tests.WasmRunner.csproj
├── CSharpCompilerTests.cs                      — Compilation + execution unit tests
├── OutputCaptureTests.cs                       — Console capture tests
├── SecuritySyntaxWalkerTests.cs                — Security validation tests
└── CompilationResultTests.cs                   — Result model tests
```

### New Files in Existing Projects

```
src/CloudNimble.DotNetDocs.Mintlify/
├── Validation/
│   ├── DotNetRunnerExtractor.cs                — Extracts <DotNetRunner> initialCode from MDX
│   └── MdxCodeExampleValidator.cs              — Compiles extracted code examples via Roslyn

src/CloudNimble.DotNetDocs.Tests.Mintlify/
├── Validation/
│   ├── DotNetRunnerExtractorTests.cs           — Extraction tests
│   └── MdxCodeExampleValidatorTests.cs         — Validation tests

src/CloudNimble.DotNetDocs.Docs/snippets/
├── dotnet-runner.jsx                           — Interactive C# runner component
```

### Modified Files

```
src/CloudNimble.DotNetDocs.slnx                — Add new projects
```

---

## Chunk 1: WASM Runner Core

### Task 1: Create the WasmRunner Project

**Files:**
- Create: `src/CloudNimble.DotNetDocs.WasmRunner/CloudNimble.DotNetDocs.WasmRunner.csproj`

- [ ] **Step 1: Create the project file**

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
        <OutputType>Exe</OutputType>
        <PublishTrimmed>true</PublishTrimmed>
        <TrimMode>link</TrimMode>
        <InvariantGlobalization>true</InvariantGlobalization>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
        <Description>A .NET WASM application that compiles and executes C# code in the browser using Roslyn, for use in interactive Mintlify documentation.</Description>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.*" />
    </ItemGroup>

</Project>
```

- [ ] **Step 2: Verify it builds**

Run: `dotnet build src/CloudNimble.DotNetDocs.WasmRunner/CloudNimble.DotNetDocs.WasmRunner.csproj -c Debug`
Expected: Build succeeds (may warn about missing Program.cs — that's fine, we'll add it next)

---

### Task 2: CompilationResult Model

**Files:**
- Create: `src/CloudNimble.DotNetDocs.WasmRunner/CompilationResult.cs`
- Create: `src/CloudNimble.DotNetDocs.Tests.WasmRunner/CloudNimble.DotNetDocs.Tests.WasmRunner.csproj`
- Create: `src/CloudNimble.DotNetDocs.Tests.WasmRunner/CompilationResultTests.cs`

- [ ] **Step 1: Create the test project**

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFrameworks>net10.0;net9.0;net8.0</TargetFrameworks>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="..\CloudNimble.DotNetDocs.WasmRunner\CloudNimble.DotNetDocs.WasmRunner.csproj" />
        <ProjectReference Include="..\CloudNimble.DotNetDocs.Tests.Shared\CloudNimble.DotNetDocs.Tests.Shared.csproj" />
    </ItemGroup>

</Project>
```

> **Important:** The WasmRunner project targets only `net10.0` with `browser-wasm` RID, but the test project uses standard TFMs and references it. If cross-TFM reference fails, extract `CompilationResult`, `OutputCapture`, `SecuritySyntaxWalker`, and `CSharpCompiler` into a separate shared library (`CloudNimble.DotNetDocs.WasmRunner.Core`) that multi-targets `net10.0;net9.0;net8.0` without the `browser-wasm` RID, and have the WASM project reference it. The test project references the shared library instead. This is the likely path — plan accordingly.

- [ ] **Step 2: Write the failing test**

```csharp
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
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test src/CloudNimble.DotNetDocs.Tests.WasmRunner -c Debug --framework net10.0`
Expected: FAIL — `CompilationResult` does not exist

- [ ] **Step 4: Write the CompilationResult model**

```csharp
namespace CloudNimble.DotNetDocs.WasmRunner
{

    /// <summary>
    /// Represents the result of compiling and executing a C# code snippet.
    /// </summary>
    public class CompilationResult
    {

        #region Properties

        /// <summary>
        /// Gets the compiler diagnostic messages, if any.
        /// </summary>
        public string Diagnostics { get; init; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether compilation and execution succeeded.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// Gets the captured console output from execution.
        /// </summary>
        public string Output { get; init; } = string.Empty;

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a failed compilation result with the specified diagnostics.
        /// </summary>
        /// <param name="diagnostics">The compiler diagnostic messages.</param>
        /// <returns>A new <see cref="CompilationResult"/> representing failure.</returns>
        public static CompilationResult Failure(string diagnostics) => new()
        {
            IsSuccess = false,
            Diagnostics = diagnostics
        };

        /// <summary>
        /// Creates a successful compilation result with the specified output.
        /// </summary>
        /// <param name="output">The captured console output.</param>
        /// <returns>A new <see cref="CompilationResult"/> representing success.</returns>
        public static CompilationResult Success(string output) => new()
        {
            IsSuccess = true,
            Output = output
        };

        #endregion

    }

}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test src/CloudNimble.DotNetDocs.Tests.WasmRunner -c Debug --framework net10.0`
Expected: PASS (3 tests)

- [ ] **Step 6: Commit**

```bash
git add src/CloudNimble.DotNetDocs.WasmRunner/ src/CloudNimble.DotNetDocs.Tests.WasmRunner/
git commit -m "feat: add WasmRunner project with CompilationResult model"
```

---

### Task 3: OutputCapture

**Files:**
- Create: `src/CloudNimble.DotNetDocs.WasmRunner/OutputCapture.cs`
- Create: `src/CloudNimble.DotNetDocs.Tests.WasmRunner/OutputCaptureTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using System;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.WasmRunner
{

    [TestClass]
    public class OutputCaptureTests
    {

        #region Capture Tests

        [TestMethod]
        public void StartCapture_CapturesConsoleWriteLine()
        {
            using var capture = new OutputCapture();

            capture.StartCapture();
            Console.WriteLine("Hello");
            var output = capture.StopCapture();

            output.Should().Contain("Hello");
        }

        [TestMethod]
        public void StartCapture_CapturesMultipleLines()
        {
            using var capture = new OutputCapture();

            capture.StartCapture();
            Console.WriteLine("Line 1");
            Console.WriteLine("Line 2");
            var output = capture.StopCapture();

            output.Should().Contain("Line 1");
            output.Should().Contain("Line 2");
        }

        [TestMethod]
        public void StopCapture_RestoresOriginalConsoleOut()
        {
            var originalOut = Console.Out;
            using var capture = new OutputCapture();

            capture.StartCapture();
            capture.StopCapture();

            Console.Out.Should().BeSameAs(originalOut);
        }

        [TestMethod]
        public void StartCapture_CalledTwice_ResetsBuffer()
        {
            using var capture = new OutputCapture();

            capture.StartCapture();
            Console.WriteLine("First");
            capture.StopCapture();

            capture.StartCapture();
            Console.WriteLine("Second");
            var output = capture.StopCapture();

            output.Should().NotContain("First");
            output.Should().Contain("Second");
        }

        #endregion

    }

}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/CloudNimble.DotNetDocs.Tests.WasmRunner -c Debug --framework net10.0`
Expected: FAIL — `OutputCapture` does not exist

- [ ] **Step 3: Write the implementation**

```csharp
using System;
using System.IO;

namespace CloudNimble.DotNetDocs.WasmRunner
{

    /// <summary>
    /// Captures console output by redirecting <see cref="Console.Out"/> to a <see cref="StringWriter"/>.
    /// </summary>
    /// <remarks>
    /// This class is used to intercept <c>Console.WriteLine</c> calls during code execution
    /// so their output can be returned to the caller. Call <see cref="StartCapture"/> before
    /// executing user code and <see cref="StopCapture"/> afterward to retrieve the output.
    /// </remarks>
    public class OutputCapture : IDisposable
    {

        #region Fields

        private bool _disposed;
        private TextWriter? _originalOut;
        private StringWriter? _writer;

        #endregion

        #region Public Methods

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_originalOut is not null)
            {
                Console.SetOut(_originalOut);
                _originalOut = null;
            }

            _writer?.Dispose();
            _writer = null;
            _disposed = true;
        }

        /// <summary>
        /// Begins capturing console output by redirecting <see cref="Console.Out"/> to an internal buffer.
        /// </summary>
        public void StartCapture()
        {
            _originalOut = Console.Out;
            _writer = new StringWriter();
            Console.SetOut(_writer);
        }

        /// <summary>
        /// Stops capturing console output and returns the captured text.
        /// </summary>
        /// <returns>The captured console output as a string.</returns>
        public string StopCapture()
        {
            var output = _writer?.ToString() ?? string.Empty;

            if (_originalOut is not null)
            {
                Console.SetOut(_originalOut);
                _originalOut = null;
            }

            _writer?.Dispose();
            _writer = null;

            return output;
        }

        #endregion

    }

}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test src/CloudNimble.DotNetDocs.Tests.WasmRunner -c Debug --framework net10.0`
Expected: PASS (all OutputCapture tests)

- [ ] **Step 5: Commit**

```bash
git add src/CloudNimble.DotNetDocs.WasmRunner/OutputCapture.cs src/CloudNimble.DotNetDocs.Tests.WasmRunner/OutputCaptureTests.cs
git commit -m "feat: add OutputCapture for console output interception"
```

---

### Task 4: SecuritySyntaxWalker

**Files:**
- Create: `src/CloudNimble.DotNetDocs.WasmRunner/SecuritySyntaxWalker.cs`
- Create: `src/CloudNimble.DotNetDocs.Tests.WasmRunner/SecuritySyntaxWalkerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Collections.Generic;
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
        public void Validate_SystemDiagnosticsProcess_IsBlocked()
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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/CloudNimble.DotNetDocs.Tests.WasmRunner -c Debug --framework net10.0`
Expected: FAIL — `SecuritySyntaxWalker` does not exist

- [ ] **Step 3: Write the implementation**

```csharp
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CloudNimble.DotNetDocs.WasmRunner
{

    /// <summary>
    /// A Roslyn syntax walker that detects usage of blocked namespaces in user-submitted C# code.
    /// </summary>
    /// <remarks>
    /// This walker scans <c>using</c> directives and flags namespaces that provide dangerous
    /// capabilities such as file system access, network access, reflection, or process execution.
    /// Code that references blocked namespaces should not be compiled or executed in the sandbox.
    /// </remarks>
    public class SecuritySyntaxWalker : CSharpSyntaxWalker
    {

        #region Fields

        private static readonly HashSet<string> _blockedNamespaces =
        [
            "System.Diagnostics",
            "System.IO",
            "System.Net",
            "System.Net.Http",
            "System.Net.Sockets",
            "System.Reflection",
            "System.Runtime.InteropServices",
            "System.Runtime.Loader",
            "System.Security.Cryptography",
            "System.Threading"
        ];

        #endregion

        #region Properties

        /// <summary>
        /// Gets the list of blocked <c>using</c> directives found during the walk.
        /// </summary>
        public List<string> BlockedUsings { get; } = [];

        /// <summary>
        /// Gets a value indicating whether any security issues were found.
        /// </summary>
        public bool HasSecurityIssues => BlockedUsings.Count > 0;

        #endregion

        #region Public Methods

        /// <summary>
        /// Visits a <c>using</c> directive and checks whether it references a blocked namespace.
        /// </summary>
        /// <param name="node">The using directive syntax node.</param>
        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            var name = node.Name?.ToString();

            if (!string.IsNullOrWhiteSpace(name) &&
                _blockedNamespaces.Any(blocked =>
                    name == blocked || name.StartsWith($"{blocked}.")))
            {
                BlockedUsings.Add(name);
            }

            base.VisitUsingDirective(node);
        }

        #endregion

    }

}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test src/CloudNimble.DotNetDocs.Tests.WasmRunner -c Debug --framework net10.0`
Expected: PASS (all SecuritySyntaxWalker tests)

- [ ] **Step 5: Commit**

```bash
git add src/CloudNimble.DotNetDocs.WasmRunner/SecuritySyntaxWalker.cs src/CloudNimble.DotNetDocs.Tests.WasmRunner/SecuritySyntaxWalkerTests.cs
git commit -m "feat: add SecuritySyntaxWalker to block dangerous namespaces"
```

---

### Task 5: CSharpCompiler

**Files:**
- Create: `src/CloudNimble.DotNetDocs.WasmRunner/CSharpCompiler.cs`
- Create: `src/CloudNimble.DotNetDocs.Tests.WasmRunner/CSharpCompilerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.WasmRunner
{

    [TestClass]
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
            var code = "";

            var result = _compiler.CompileAndRun(code);

            // Empty code is valid top-level C#
            result.IsSuccess.Should().BeTrue();
            result.Output.Should().BeEmpty();
        }

        [TestMethod]
        public void CompileAndRun_WhitespaceOnly_ReturnsSuccess()
        {
            var code = "   \n  \n  ";

            var result = _compiler.CompileAndRun(code);

            result.IsSuccess.Should().BeTrue();
        }

        #endregion

    }

}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/CloudNimble.DotNetDocs.Tests.WasmRunner -c Debug --framework net10.0`
Expected: FAIL — `CSharpCompiler` does not exist

- [ ] **Step 3: Write the implementation**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CloudNimble.DotNetDocs.WasmRunner
{

    /// <summary>
    /// Compiles and executes C# code using the Roslyn compiler.
    /// </summary>
    /// <remarks>
    /// This class accepts top-level C# statements (or full programs), compiles them in-memory
    /// using Roslyn, performs security validation via <see cref="SecuritySyntaxWalker"/>,
    /// captures console output via <see cref="OutputCapture"/>, and returns structured
    /// <see cref="CompilationResult"/> objects.
    /// </remarks>
    public class CSharpCompiler
    {

        #region Fields

        private static readonly string[] _defaultUsings =
        [
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text"
        ];

        private readonly List<MetadataReference> _references;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpCompiler"/> class.
        /// </summary>
        public CSharpCompiler()
        {
            _references = BuildReferences();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Compiles and executes the specified C# code, returning the result.
        /// </summary>
        /// <param name="code">The C# code to compile and execute. Supports top-level statements.</param>
        /// <returns>
        /// A <see cref="CompilationResult"/> containing either the captured console output
        /// on success, or compiler diagnostics on failure.
        /// </returns>
        public CompilationResult CompileAndRun(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return CompilationResult.Success(string.Empty);
            }

            // Security check: scan for blocked namespaces
            var tree = CSharpSyntaxTree.ParseText(code);
            var walker = new SecuritySyntaxWalker();
            walker.Visit(tree.GetRoot());

            if (walker.HasSecurityIssues)
            {
                var blocked = string.Join(", ", walker.BlockedUsings);
                return CompilationResult.Failure(
                    $"Blocked namespace(s): {blocked}. " +
                    "These namespaces are not allowed in the sandbox for security reasons.");
            }

            // Compile
            var compilation = CSharpCompilation.Create(
                $"UserCode_{Guid.NewGuid():N}",
                [tree],
                _references,
                new CSharpCompilationOptions(OutputKind.ConsoleApplication)
                    .WithUsings(_defaultUsings));

            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);

            if (!emitResult.Success)
            {
                var errors = emitResult.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.ToString());
                return CompilationResult.Failure(string.Join(Environment.NewLine, errors));
            }

            // Execute
            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());
            var entryPoint = assembly.EntryPoint;

            if (entryPoint is null)
            {
                return CompilationResult.Success(string.Empty);
            }

            using var capture = new OutputCapture();
            capture.StartCapture();

            try
            {
                var parameters = entryPoint.GetParameters();
                var args = parameters.Length > 0 ? [new string[0]] : null;
                entryPoint.Invoke(null, args);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                var output = capture.StopCapture();
                return CompilationResult.Failure(
                    $"Runtime error: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}" +
                    (output.Length > 0 ? $"\n\nPartial output:\n{output}" : ""));
            }
            catch (Exception ex)
            {
                var output = capture.StopCapture();
                return CompilationResult.Failure(
                    $"Execution error: {ex.Message}" +
                    (output.Length > 0 ? $"\n\nPartial output:\n{output}" : ""));
            }

            return CompilationResult.Success(capture.StopCapture());
        }

        #endregion

        #region Private Methods

        private static List<MetadataReference> BuildReferences()
        {
            var references = new List<MetadataReference>();
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

            // Core runtime assemblies
            string[] assemblyNames =
            [
                "System.Runtime.dll",
                "System.Console.dll",
                "System.Collections.dll",
                "System.Linq.dll",
                "System.Text.RegularExpressions.dll",
                "System.Threading.Tasks.dll",
                "System.Private.CoreLib.dll",
                "netstandard.dll"
            ];

            foreach (var name in assemblyNames)
            {
                var path = Path.Combine(assemblyPath, name);
                if (File.Exists(path))
                {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }

            return references;
        }

        #endregion

    }

}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test src/CloudNimble.DotNetDocs.Tests.WasmRunner -c Debug --framework net10.0`
Expected: PASS (all CSharpCompiler tests)

> **Note:** If `Assembly.Load(byte[])` doesn't work in the test environment (it should on desktop .NET), you may need to use `AssemblyLoadContext.Default.LoadFromStream(ms)` instead. Adjust accordingly.

- [ ] **Step 5: Commit**

```bash
git add src/CloudNimble.DotNetDocs.WasmRunner/CSharpCompiler.cs src/CloudNimble.DotNetDocs.Tests.WasmRunner/CSharpCompilerTests.cs
git commit -m "feat: add CSharpCompiler with Roslyn compilation, security validation, and output capture"
```

---

### Task 6: Program.cs with JSExport Entry Points

**Files:**
- Create: `src/CloudNimble.DotNetDocs.WasmRunner/Program.cs`

- [ ] **Step 1: Write the entry point**

This file is the WASM entry point. It cannot be easily unit-tested since `[JSExport]` is a WASM-only interop mechanism. The logic it delegates to (`CSharpCompiler`) is fully tested.

```csharp
using System;
using System.Runtime.InteropServices.JavaScript;

namespace CloudNimble.DotNetDocs.WasmRunner
{

    /// <summary>
    /// Entry point for the .NET WASM runner application.
    /// </summary>
    /// <remarks>
    /// Exposes C# compilation and execution capabilities to JavaScript via <c>[JSExport]</c> interop.
    /// The exported methods are called from the <c>dotnet-runner.jsx</c> Mintlify React component.
    /// </remarks>
    public partial class Program
    {

        #region Fields

        private static readonly CSharpCompiler _compiler = new();

        #endregion

        #region Public Methods

        /// <summary>
        /// Compiles and executes C# code, returning JSON with the result.
        /// </summary>
        /// <param name="code">The C# code to compile and execute.</param>
        /// <returns>A JSON string containing the <see cref="CompilationResult"/>.</returns>
        [JSExport]
        public static string CompileAndRun(string code)
        {
            var result = _compiler.CompileAndRun(code);
            return System.Text.Json.JsonSerializer.Serialize(result, CompilationResultContext.Default.CompilationResult);
        }

        /// <summary>
        /// Application entry point. Required for WASM runtime initialization.
        /// </summary>
        public static void Main()
        {
            Console.WriteLine("DotNetDocs WASM Runner initialized.");
        }

        #endregion

    }

}
```

> **Note:** The `CompilationResultContext` is a source-generated JSON serializer context for AOT compatibility. Create it in the same file or a separate file:

```csharp
using System.Text.Json.Serialization;

namespace CloudNimble.DotNetDocs.WasmRunner
{

    /// <summary>
    /// Source-generated JSON serialization context for AOT-compatible serialization.
    /// </summary>
    [JsonSerializable(typeof(CompilationResult))]
    internal partial class CompilationResultContext : JsonSerializerContext
    {
    }

}
```

Save as `src/CloudNimble.DotNetDocs.WasmRunner/CompilationResultContext.cs`.

- [ ] **Step 2: Build to verify**

Run: `dotnet build src/CloudNimble.DotNetDocs.WasmRunner -c Debug`
Expected: Build succeeds

> **Note:** `[JSExport]` requires `browser-wasm` RID and the `wasm-tools` workload. If the build fails because the workload isn't installed, run: `dotnet workload install wasm-tools`. If `System.Runtime.InteropServices.JavaScript` isn't available, add `#if NET10_0_OR_GREATER` guards around the `[JSExport]` attribute and the `JSHost` import.

- [ ] **Step 3: Commit**

```bash
git add src/CloudNimble.DotNetDocs.WasmRunner/Program.cs src/CloudNimble.DotNetDocs.WasmRunner/CompilationResultContext.cs
git commit -m "feat: add Program.cs with JSExport entry points for WASM interop"
```

---

## Chunk 2: Mintlify React Components & MDX Integration

### Task 7: DotNetRunner JSX Component

**Files:**
- Create: `src/CloudNimble.DotNetDocs.Docs/snippets/dotnet-runner.jsx`

- [ ] **Step 1: Create the component**

This is a Mintlify React snippet component. It must follow Mintlify constraints:
- Arrow function export only (no `function` keyword)
- In `/snippets/` folder
- No npm imports
- `React.useState` / `React.useEffect` (not destructured `import { useState }`)
- Inline styles only (no CSS modules)

```jsx
export const DotNetRunner = ({
  initialCode = "",
  height = "200px",
  title = "C# Example"
}) => {
  const [code, setCode] = React.useState(initialCode);
  const [output, setOutput] = React.useState("");
  const [errors, setErrors] = React.useState("");
  const [isRunning, setIsRunning] = React.useState(false);
  const [runtimeLoading, setRuntimeLoading] = React.useState(false);
  const [ready, setReady] = React.useState(false);

  // Prevent double-flash on mount
  React.useEffect(() => {
    setReady(true);
  }, []);

  const loadAndRun = async () => {
    setIsRunning(true);
    setOutput("");
    setErrors("");

    try {
      // Lazy-load runtime on first run
      if (!window.__dotnetRuntime) {
        setRuntimeLoading(true);

        const RUNTIME_BASE = window.__dotnetRuntimeBase || "/dotnet-wasm-runner";

        await new Promise((resolve, reject) => {
          if (window.dotnet) { resolve(); return; }
          const script = document.createElement("script");
          script.src = `${RUNTIME_BASE}/_framework/dotnet.js`;
          script.onload = resolve;
          script.onerror = reject;
          document.head.appendChild(script);
        });

        const { getAssemblyExports, Module } = await window.dotnet
          .withDiagnosticTracing(false)
          .withResourceLoader((type, name) =>
            `${RUNTIME_BASE}/_framework/${name}`
          )
          .create();

        const exports = await getAssemblyExports("CloudNimble.DotNetDocs.WasmRunner");

        window.__dotnetRuntime = {
          compileAndRun: (code) => {
            const json = exports.CloudNimble.DotNetDocs.WasmRunner.Program.CompileAndRun(code);
            return JSON.parse(json);
          }
        };

        setRuntimeLoading(false);
      }

      const runtime = window.__dotnetRuntime;
      const result = runtime.compileAndRun(code);

      if (result.isSuccess) {
        setOutput(result.output || "(no output)");
      } else {
        setErrors(result.diagnostics);
      }
    } catch (err) {
      setErrors(`Execution error: ${err.message}`);
    } finally {
      setIsRunning(false);
      setRuntimeLoading(false);
    }
  };

  return (
    <div style={{
      border: "1px solid var(--border-color, #e2e8f0)",
      borderRadius: "8px",
      overflow: "hidden",
      marginBottom: "1rem",
      opacity: ready ? 1 : 0,
      transition: "opacity 0.3s ease-out"
    }}>
      {/* Header */}
      <div style={{
        display: "flex", justifyContent: "space-between", alignItems: "center",
        padding: "8px 12px", borderBottom: "1px solid var(--border-color, #e2e8f0)",
        backgroundColor: "var(--tw-prose-pre-bg, #1e293b)"
      }}>
        <span style={{ fontSize: "0.85rem", fontWeight: 600, color: "var(--tw-prose-pre-code, #e2e8f0)" }}>{title}</span>
        <div style={{ display: "flex", gap: "8px", alignItems: "center" }}>
          {runtimeLoading && (
            <span style={{ fontSize: "0.8rem", opacity: 0.7, color: "var(--tw-prose-pre-code, #e2e8f0)" }}>
              Loading .NET runtime...
            </span>
          )}
          <span style={{ fontSize: "0.7rem", opacity: 0.4, color: "var(--tw-prose-pre-code, #e2e8f0)" }}>
            Client-side C#
          </span>
        </div>
      </div>

      {/* Editor */}
      <textarea
        value={code}
        onChange={(e) => setCode(e.target.value)}
        style={{
          width: "100%", height, fontFamily: "'Cascadia Code', 'JetBrains Mono', monospace",
          fontSize: "0.875rem", lineHeight: "1.5",
          padding: "12px", border: "none", resize: "vertical",
          backgroundColor: "var(--tw-prose-pre-bg, #1e293b)",
          color: "var(--tw-prose-pre-code, #e2e8f0)",
          outline: "none", boxSizing: "border-box"
        }}
        spellCheck={false}
      />

      {/* Controls */}
      <div style={{
        padding: "8px 12px",
        borderTop: "1px solid var(--border-color, #e2e8f0)",
        display: "flex", gap: "8px", alignItems: "center"
      }}>
        <button
          onClick={loadAndRun}
          disabled={isRunning}
          style={{
            padding: "6px 16px", borderRadius: "6px", border: "none",
            backgroundColor: "#3CD0E2", color: "#0A1628", cursor: "pointer",
            fontSize: "0.85rem", fontWeight: 600,
            opacity: isRunning ? 0.6 : 1,
            transition: "opacity 0.15s"
          }}
        >
          {isRunning ? "Running..." : "\u25B6 Run"}
        </button>
        <button
          onClick={() => { setCode(initialCode); setOutput(""); setErrors(""); }}
          style={{
            padding: "6px 12px", borderRadius: "6px",
            border: "1px solid var(--border-color, #e2e8f0)",
            backgroundColor: "transparent",
            color: "var(--tw-prose-body, inherit)",
            cursor: "pointer", fontSize: "0.8rem"
          }}
        >
          Reset
        </button>
      </div>

      {/* Output */}
      {output && (
        <div style={{
          borderTop: "1px solid var(--border-color, #e2e8f0)",
          padding: "12px",
          backgroundColor: "var(--tw-prose-pre-bg, #1e293b)"
        }}>
          <div style={{ fontSize: "0.75rem", fontWeight: 600, marginBottom: "6px", color: "#3CD0E2" }}>
            Output
          </div>
          <pre style={{
            margin: 0, fontFamily: "'Cascadia Code', 'JetBrains Mono', monospace",
            fontSize: "0.85rem", whiteSpace: "pre-wrap",
            color: "var(--tw-prose-pre-code, #e2e8f0)"
          }}>{output}</pre>
        </div>
      )}

      {/* Errors */}
      {errors && (
        <div style={{
          borderTop: "1px solid var(--border-color, #e2e8f0)",
          padding: "12px",
          backgroundColor: "rgba(220, 38, 38, 0.1)"
        }}>
          <div style={{ fontSize: "0.75rem", fontWeight: 600, marginBottom: "6px", color: "#ef4444" }}>
            Errors
          </div>
          <pre style={{
            margin: 0, fontFamily: "'Cascadia Code', 'JetBrains Mono', monospace",
            fontSize: "0.85rem", whiteSpace: "pre-wrap",
            color: "#fca5a5"
          }}>{errors}</pre>
        </div>
      )}
    </div>
  );
};
```

- [ ] **Step 2: Verify the file is valid JSX**

Manually review: all exports use arrow functions, no `import` statements, uses `React.useState`/`React.useEffect` globals.

- [ ] **Step 3: Commit**

```bash
git add src/CloudNimble.DotNetDocs.Docs/snippets/dotnet-runner.jsx
git commit -m "feat: add DotNetRunner Mintlify React component"
```

---

### Task 8: Advanced Components

**Files:**
- Create: `src/CloudNimble.DotNetDocs.Docs/snippets/dotnet-il-visualizer.jsx`
- Create: `src/CloudNimble.DotNetDocs.Docs/snippets/dotnet-benchmark.jsx`
- Create: `src/CloudNimble.DotNetDocs.Docs/snippets/dotnet-linq-visualizer.jsx`
- Create: `src/CloudNimble.DotNetDocs.Docs/snippets/dotnet-tutorial.jsx`

- [ ] **Step 1: Create IL Visualizer component**

See spec Section 5 "IL/WASM Visualizer" for the component code. The component must:
- Use arrow function export
- Share `window.__dotnetRuntime`
- Display C# source alongside compiled IL output
- Use inline styles, `React.useState`

- [ ] **Step 2: Create Benchmark Playground component**

See spec Section 5 "Performance Benchmark Playground". The component must:
- Accept `code1`, `code2`, `label1`, `label2` props
- Run configurable iterations
- Display average and median timing results in a table

- [ ] **Step 3: Create LINQ Visualizer component**

See spec Section 5 "LINQ Query Visualizer". The component must:
- Accept `initialQuery` prop
- Wrap user's LINQ query with instrumented data pipeline
- Display step-by-step output

- [ ] **Step 4: Create Interactive Tutorial component**

See spec Section 5 "Interactive Tutorial with State Persistence". The component must:
- Accept `steps` array prop
- Track current step and completed code per step
- Display step navigation and progress

- [ ] **Step 5: Commit**

```bash
git add src/CloudNimble.DotNetDocs.Docs/snippets/dotnet-il-visualizer.jsx \
        src/CloudNimble.DotNetDocs.Docs/snippets/dotnet-benchmark.jsx \
        src/CloudNimble.DotNetDocs.Docs/snippets/dotnet-linq-visualizer.jsx \
        src/CloudNimble.DotNetDocs.Docs/snippets/dotnet-tutorial.jsx
git commit -m "feat: add advanced interactive components (IL visualizer, benchmark, LINQ, tutorial)"
```

---

## Chunk 3: Build-Time Validation

### Task 9: DotNetRunnerExtractor

**Files:**
- Create: `src/CloudNimble.DotNetDocs.Mintlify/Validation/DotNetRunnerExtractor.cs`
- Create: `src/CloudNimble.DotNetDocs.Tests.Mintlify/Validation/DotNetRunnerExtractorTests.cs`

This class scans MDX files for `<DotNetRunner>` component usage and extracts the `initialCode` prop values for build-time compilation.

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Collections.Generic;
using System.Linq;
using CloudNimble.DotNetDocs.Mintlify.Validation;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Mintlify.Validation
{

    [TestClass]
    public class DotNetRunnerExtractorTests
    {

        #region Extraction Tests

        [TestMethod]
        public void Extract_SingleRunner_ReturnsOneExample()
        {
            var mdx = @"
# Hello

<DotNetRunner
  initialCode={`Console.WriteLine(""Hello"");`}
/>
";
            var examples = DotNetRunnerExtractor.Extract(mdx, "test.mdx");

            examples.Should().HaveCount(1);
            examples.First().Code.Should().Contain("Console.WriteLine");
            examples.First().SourceFile.Should().Be("test.mdx");
        }

        [TestMethod]
        public void Extract_MultipleRunners_ReturnsAll()
        {
            var mdx = @"
<DotNetRunner initialCode={`var x = 1;`} />

Some text between.

<DotNetRunner initialCode={`var y = 2;`} />
";
            var examples = DotNetRunnerExtractor.Extract(mdx, "test.mdx");

            examples.Should().HaveCount(2);
        }

        [TestMethod]
        public void Extract_NoRunners_ReturnsEmpty()
        {
            var mdx = @"# Just a normal page\n\nNo interactive code here.";

            var examples = DotNetRunnerExtractor.Extract(mdx, "test.mdx");

            examples.Should().BeEmpty();
        }

        [TestMethod]
        public void Extract_MultilineCode_ExtractsCorrectly()
        {
            var mdx = @"
<DotNetRunner
  title=""Multi-line""
  initialCode={`using System;

Console.WriteLine(""Line 1"");
Console.WriteLine(""Line 2"");`}
/>
";
            var examples = DotNetRunnerExtractor.Extract(mdx, "test.mdx");

            examples.Should().HaveCount(1);
            examples.First().Code.Should().Contain("Line 1");
            examples.First().Code.Should().Contain("Line 2");
        }

        #endregion

    }

}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/CloudNimble.DotNetDocs.Tests.Mintlify -c Debug --framework net10.0 --filter "FullyQualifiedName~DotNetRunnerExtractor"`
Expected: FAIL — class does not exist

- [ ] **Step 3: Write the implementation**

```csharp
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CloudNimble.DotNetDocs.Mintlify.Validation
{

    /// <summary>
    /// Extracts <c>&lt;DotNetRunner&gt;</c> component code examples from MDX content.
    /// </summary>
    /// <remarks>
    /// Scans MDX file content for <c>&lt;DotNetRunner&gt;</c> JSX component usage and
    /// extracts the <c>initialCode</c> prop values. These extracted code snippets can
    /// then be compiled at build time to ensure all documentation examples are valid.
    /// </remarks>
    public static partial class DotNetRunnerExtractor
    {

        #region Public Methods

        /// <summary>
        /// Extracts all <c>&lt;DotNetRunner&gt;</c> code examples from the specified MDX content.
        /// </summary>
        /// <param name="mdxContent">The raw MDX file content to scan.</param>
        /// <param name="sourceFile">The file path of the MDX file, used for error reporting.</param>
        /// <returns>A list of extracted code examples with their source file locations.</returns>
        public static List<CodeExample> Extract(string mdxContent, string sourceFile)
        {
            var examples = new List<CodeExample>();

            if (string.IsNullOrWhiteSpace(mdxContent))
            {
                return examples;
            }

            var matches = InitialCodeRegex().Matches(mdxContent);

            foreach (Match match in matches)
            {
                if (match.Groups["code"].Success)
                {
                    examples.Add(new CodeExample
                    {
                        Code = match.Groups["code"].Value,
                        SourceFile = sourceFile
                    });
                }
            }

            return examples;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Matches <c>initialCode={`...`}</c> prop values in JSX, including multiline content.
        /// </summary>
        [GeneratedRegex(@"initialCode=\{`(?<code>[\s\S]*?)`\}", RegexOptions.Compiled)]
        private static partial Regex InitialCodeRegex();

        #endregion

    }

    /// <summary>
    /// Represents a C# code example extracted from an MDX file.
    /// </summary>
    public class CodeExample
    {

        #region Properties

        /// <summary>
        /// Gets or sets the extracted C# code.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source MDX file path where this example was found.
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

        #endregion

    }

}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test src/CloudNimble.DotNetDocs.Tests.Mintlify -c Debug --framework net10.0 --filter "FullyQualifiedName~DotNetRunnerExtractor"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/CloudNimble.DotNetDocs.Mintlify/Validation/ src/CloudNimble.DotNetDocs.Tests.Mintlify/Validation/
git commit -m "feat: add DotNetRunnerExtractor to scan MDX for interactive code examples"
```

---

### Task 10: MdxCodeExampleValidator

**Files:**
- Create: `src/CloudNimble.DotNetDocs.Mintlify/Validation/MdxCodeExampleValidator.cs`
- Create: `src/CloudNimble.DotNetDocs.Tests.Mintlify/Validation/MdxCodeExampleValidatorTests.cs`

This class takes extracted code examples and compiles them using Roslyn to verify they're valid at build time.

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Collections.Generic;
using System.Linq;
using CloudNimble.DotNetDocs.Mintlify.Validation;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.Mintlify.Validation
{

    [TestClass]
    public class MdxCodeExampleValidatorTests
    {

        #region Validation Tests

        [TestMethod]
        public void Validate_ValidCode_ReturnsNoErrors()
        {
            var examples = new List<CodeExample>
            {
                new() { Code = @"using System; Console.WriteLine(""Hello"");", SourceFile = "test.mdx" }
            };

            var errors = MdxCodeExampleValidator.Validate(examples);

            errors.Should().BeEmpty();
        }

        [TestMethod]
        public void Validate_InvalidCode_ReturnsErrors()
        {
            var examples = new List<CodeExample>
            {
                new() { Code = @"Console.WriteLine(undefinedVar)", SourceFile = "test.mdx" }
            };

            var errors = MdxCodeExampleValidator.Validate(examples);

            errors.Should().NotBeEmpty();
            errors.First().SourceFile.Should().Be("test.mdx");
        }

        [TestMethod]
        public void Validate_MixedValid_InvalidCode_OnlyReportsInvalid()
        {
            var examples = new List<CodeExample>
            {
                new() { Code = @"using System; Console.WriteLine(""ok"");", SourceFile = "good.mdx" },
                new() { Code = @"Console.WriteLine(nope)", SourceFile = "bad.mdx" }
            };

            var errors = MdxCodeExampleValidator.Validate(examples);

            errors.Should().HaveCount(1);
            errors.First().SourceFile.Should().Be("bad.mdx");
        }

        [TestMethod]
        public void Validate_EmptyList_ReturnsNoErrors()
        {
            var errors = MdxCodeExampleValidator.Validate([]);

            errors.Should().BeEmpty();
        }

        #endregion

    }

}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/CloudNimble.DotNetDocs.Tests.Mintlify -c Debug --framework net10.0 --filter "FullyQualifiedName~MdxCodeExampleValidator"`
Expected: FAIL — class does not exist

- [ ] **Step 3: Write the implementation**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CloudNimble.DotNetDocs.Mintlify.Validation
{

    /// <summary>
    /// Validates C# code examples extracted from MDX files by compiling them with Roslyn.
    /// </summary>
    /// <remarks>
    /// This validator is used during the documentation build process to ensure that all
    /// interactive <c>&lt;DotNetRunner&gt;</c> code examples compile successfully.
    /// Compilation errors are reported with their source MDX file for easy identification.
    /// </remarks>
    public static class MdxCodeExampleValidator
    {

        #region Fields

        private static readonly string[] _defaultUsings = ["System", "System.Collections.Generic", "System.Linq", "System.Text"];

        #endregion

        #region Public Methods

        /// <summary>
        /// Validates the specified code examples by compiling each one.
        /// </summary>
        /// <param name="examples">The code examples to validate.</param>
        /// <returns>A list of validation errors. Empty if all examples compile successfully.</returns>
        public static List<ValidationError> Validate(List<CodeExample> examples)
        {
            var errors = new List<ValidationError>();
            var references = BuildReferences();

            foreach (var example in examples)
            {
                var tree = CSharpSyntaxTree.ParseText(example.Code);
                var compilation = CSharpCompilation.Create(
                    $"Validation_{Guid.NewGuid():N}",
                    [tree],
                    references,
                    new CSharpCompilationOptions(OutputKind.ConsoleApplication)
                        .WithUsings(_defaultUsings));

                var diagnostics = compilation.GetDiagnostics()
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .ToList();

                if (diagnostics.Count > 0)
                {
                    errors.Add(new ValidationError
                    {
                        SourceFile = example.SourceFile,
                        Code = example.Code,
                        Diagnostics = diagnostics.Select(d => d.ToString()).ToList()
                    });
                }
            }

            return errors;
        }

        #endregion

        #region Private Methods

        private static List<MetadataReference> BuildReferences()
        {
            var references = new List<MetadataReference>();
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

            string[] assemblyNames =
            [
                "System.Runtime.dll",
                "System.Console.dll",
                "System.Collections.dll",
                "System.Linq.dll",
                "System.Text.RegularExpressions.dll",
                "System.Private.CoreLib.dll",
                "netstandard.dll"
            ];

            foreach (var name in assemblyNames)
            {
                var path = Path.Combine(assemblyPath, name);
                if (File.Exists(path))
                {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }

            return references;
        }

        #endregion

    }

    /// <summary>
    /// Represents a validation error for a code example found in an MDX file.
    /// </summary>
    public class ValidationError
    {

        #region Properties

        /// <summary>
        /// Gets or sets the original C# code that failed to compile.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the compiler diagnostic messages.
        /// </summary>
        public List<string> Diagnostics { get; set; } = [];

        /// <summary>
        /// Gets or sets the source MDX file where the error was found.
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

        #endregion

    }

}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test src/CloudNimble.DotNetDocs.Tests.Mintlify -c Debug --framework net10.0 --filter "FullyQualifiedName~MdxCodeExampleValidator"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/CloudNimble.DotNetDocs.Mintlify/Validation/MdxCodeExampleValidator.cs src/CloudNimble.DotNetDocs.Tests.Mintlify/Validation/MdxCodeExampleValidatorTests.cs
git commit -m "feat: add MdxCodeExampleValidator for build-time code example compilation"
```

---

## Chunk 4: Solution Integration & Final Assembly

### Task 11: Update Solution File

**Files:**
- Modify: `src/CloudNimble.DotNetDocs.slnx`

- [ ] **Step 1: Add new projects to the solution**

Add the WasmRunner project under a new `/WASM/` folder, and the test project under `/Tests/`:

```xml
<Folder Name="/WASM/">
    <Project Path="CloudNimble.DotNetDocs.WasmRunner/CloudNimble.DotNetDocs.WasmRunner.csproj" />
</Folder>
```

And under the existing `/Tests/` folder:

```xml
<Project Path="CloudNimble.DotNetDocs.Tests.WasmRunner/CloudNimble.DotNetDocs.Tests.WasmRunner.csproj" />
```

- [ ] **Step 2: Build the full solution**

Run: `dotnet build src/CloudNimble.DotNetDocs.slnx -c Debug`
Expected: Build succeeds

- [ ] **Step 3: Run all tests**

Run: `dotnet test src/CloudNimble.DotNetDocs.slnx -c Debug --framework net10.0`
Expected: All tests pass, including new WasmRunner tests and validation tests

- [ ] **Step 4: Commit**

```bash
git add src/CloudNimble.DotNetDocs.slnx
git commit -m "feat: add WasmRunner and test projects to solution"
```

---

### Task 12: Verify Complete Build & Push

- [ ] **Step 1: Full solution build**

Run: `dotnet build src/CloudNimble.DotNetDocs.slnx -c Release`
Expected: Clean build with no errors

- [ ] **Step 2: Full test run**

Run: `dotnet test src/CloudNimble.DotNetDocs.slnx -c Release`
Expected: All tests pass across all target frameworks

- [ ] **Step 3: Push branch**

Run: `git push -u origin feature/try-dotnet`

---

## Notes & Known Risks

### Project Structure Decision

The `browser-wasm` RID on the WasmRunner project may prevent the test project from referencing it directly on `net9.0`/`net8.0` TFMs. If this happens:

1. Extract `CompilationResult`, `OutputCapture`, `SecuritySyntaxWalker`, and `CSharpCompiler` into `CloudNimble.DotNetDocs.WasmRunner.Core` (multi-targets `net10.0;net9.0;net8.0`, no `browser-wasm` RID)
2. Keep `Program.cs` and `CompilationResultContext.cs` in the WASM project (single-target `net10.0` with `browser-wasm`)
3. Test project references the `.Core` project

### WASM Workload

Building the WASM project requires: `dotnet workload install wasm-tools`

### CDN Hosting

The `RUNTIME_BASE` URL in `dotnet-runner.jsx` is configured via `window.__dotnetRuntimeBase`. During development, this can point to a local dev server. For production, host the published WASM artifacts on Azure Blob Storage or similar CDN with:
- CORS headers allowing the Mintlify docs domain
- Long-lived cache headers on `.wasm` and `.dll` files
- Brotli compression for 70-80% size reduction

### Roslyn References

Both `CSharpCompiler` (in WasmRunner) and `MdxCodeExampleValidator` (in Mintlify) use Roslyn for compilation. The Mintlify project already references `Microsoft.CodeAnalysis.CSharp` transitively through `CloudNimble.DotNetDocs.Core`. Ensure version compatibility.
