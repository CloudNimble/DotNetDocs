# Client-Side C# Execution in Mintlify Documentation

## Executive Summary

This document analyzes approaches for embedding interactive C# code execution in documentation, specifically for integration with Mintlify's React component system. It explores using the .NET WASM runtime directly (without Blazor's DOM manipulation layer) to enable true client-side C# compilation and execution, integrated with Mintlify's MDX-based documentation through React components in the `/snippets/` folder.

**Key Finding**: Using the .NET WASM runtime directly (without Blazor's DOM layer) enables client-side C# compilation and execution that can be integrated into Mintlify's React component system, creating a zero-latency, zero-cost, fully interactive documentation experience.

> **Note**: Microsoft's hosted Try .NET service (try.dot.net) has been shut down. The underlying technology — running the .NET WASM runtime in the browser — remains fully viable and is the foundation of the approach described here. The open-source components from the `dotnet/interactive` repository can still be leveraged.

---

## 1. Mintlify React Component Constraints

Before diving into the architecture, it's critical to understand what Mintlify supports for custom React components, as these constraints shape every design decision.

### What Mintlify Supports

| Capability | Details |
|-----------|---------|
| **Component location** | `.jsx` files in the `/snippets/` (or `/components/`) folder |
| **Syntax** | Arrow function exports only (`export const Foo = () => { ... }`). The `function` keyword is **not supported** in snippet files |
| **React hooks** | `useState`, `useEffect`, `useMemo`, `useCallback` all work |
| **Rendering** | Client-side only. Flash of loading content is expected |
| **Imports in MDX** | `import { Foo } from "/snippets/foo.jsx"` |
| **Nested imports** | **Not supported**. All component dependencies must be imported directly in the MDX file |
| **Built-in components** | `<CodeBlock>` renders code with syntax highlighting and copy support from within React components |
| **Custom scripts** | `docs.json` supports global JS/CSS injection (equivalent to `<script>` tags on every page) |
| **npm packages** | **Not available**. Cannot import from `node_modules`. External libraries must be loaded via CDN through custom scripts |

### What This Means for Our Architecture

1. **No npm-based CodeMirror** — Must load a code editor from a CDN via custom scripts, or use a `<textarea>` with Mintlify's `<CodeBlock>` for output display
2. **No npm-based WASM loader** — The .NET WASM runtime must be loaded from a self-hosted CDN via dynamic `<script>` injection or global custom scripts
3. **Flat component structure** — The `<DotNetRunner>` component must be self-contained in a single `.jsx` file, or its sub-components must each be individually imported in every MDX page that uses them
4. **Global state for the runtime** — Since we can't use npm modules, the WASM runtime instance should be attached to `window` and shared across all component instances on a page

---

## 2. Architecture

### High-Level Flow

```
MDX Page
  ↓
import { DotNetRunner } from "/snippets/dotnet-runner.jsx"
  ↓
<DotNetRunner> React Component (client-side)
  ↓
Editable Code Area (textarea or CDN-loaded editor)
  ↓
User clicks "Run"
  ↓
Lazy-load .NET WASM Runtime from CDN (first run only)
  ↓
Roslyn Compilation API (in-browser)
  ↓
C# Code Execution (in-browser)
  ↓
Output Capture → React State → <CodeBlock> display
```

### Component Architecture

```
docs.json
  └── custom scripts: load code editor CSS/JS from CDN (lazy)

/snippets/
  └── dotnet-runner.jsx      — Main interactive runner component
  └── dotnet-runtime.jsx     — Runtime loader/singleton (imported in MDX alongside runner)

MDX page:
  import { DotNetRunner } from "/snippets/dotnet-runner.jsx"
  import { DotNetRuntimeProvider } from "/snippets/dotnet-runtime.jsx"

  <DotNetRuntimeProvider>
    <DotNetRunner code={`Console.WriteLine("Hello");`} />
  </DotNetRuntimeProvider>
```

> Because Mintlify doesn't support nested imports, both `dotnet-runner.jsx` and `dotnet-runtime.jsx` must be imported directly in the MDX file. The runtime provider manages the singleton WASM instance so multiple runners on the same page share one runtime.

---

## 3. The .NET WASM Runtime (Without Blazor)

### The Key Insight

**Blazor WASM is simply a way to run C# code in the browser.** It compiles to .wasm files which run in any modern browser. You do NOT need to use Blazor's DOM manipulation or component model to execute C# code client-side.

This means we can:
1. Use the .NET WASM runtime directly (the engine that powers Blazor)
2. Compile and execute arbitrary C# code in the browser
3. Capture `Console.WriteLine()` and other output
4. Integrate with Mintlify React components for the UI
5. Skip all of Blazor's framework overhead

### Bundle Size

Since we're not using Blazor's DOM/component system, we can trim significantly:

**Minimal Configuration**:
- .NET Runtime (WASM): ~2-3MB
- Roslyn Compiler (minimal): ~5-8MB
- Core BCL only: ~3-5MB
- **Total**: ~10-16MB (first load)

**With Aggressive Trimming**:
- Tree-shake unused BCL assemblies
- Use IL Linker aggressively
- Load additional assemblies on-demand
- **Optimized**: ~5-10MB (first load)

**Caching Strategy**:
- Browser caching with long-lived cache headers
- Service Worker caching (if supported by Mintlify's hosting)
- Shared across all documentation pages
- Load once per browser session
- **Subsequent page navigations**: <100KB (just code changes)

### Performance Characteristics

| Metric | Direct WASM Runtime |
|--------|---------------------|
| **Initial Load** | 1-3 seconds (first page with a runner) |
| **Cached Load** | 50-100ms |
| **Compilation** | 100-500ms (depends on code complexity) |
| **Execution** | Near-native WASM performance |
| **Memory** | 30-100MB (runtime + compiled assemblies) |
| **Integration** | Direct JavaScript interop with React state |

---

## 4. Implementation

### Step 1: Host the .NET WASM Runtime

Build a minimal .NET WASM application that exposes Roslyn compilation and execution APIs via JavaScript interop. This is a standalone build artifact, not part of the Mintlify site itself.

```
dotnet-wasm-runner/
  ├── Program.cs              — Entry point, JS interop exports
  ├── CSharpCompiler.cs       — Roslyn compilation wrapper
  ├── OutputCapture.cs        — Console.WriteLine interception
  └── dotnet-wasm-runner.csproj
```

```xml
<!-- dotnet-wasm-runner.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
    <OutputType>Exe</OutputType>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>link</TrimMode>
    <InvariantGlobalization>true</InvariantGlobalization>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.*" />
  </ItemGroup>
</Project>
```

**Publish and host the output** on Azure Blob Storage, GitHub Pages, or any CDN with proper CORS headers. The `_framework/` folder contains the WASM files, BCL assemblies, and boot configuration.

### Step 2: Create the Runtime Loader Component

**File**: `/snippets/dotnet-runtime.jsx`

```jsx
// Arrow function syntax required by Mintlify
export const useDotNetRuntime = () => {
  const [runtimeReady, setRuntimeReady] = React.useState(false);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState(null);

  const loadRuntime = React.useCallback(async () => {
    // Return existing runtime if already loaded
    if (window.__dotnetRuntime) {
      setRuntimeReady(true);
      return window.__dotnetRuntime;
    }

    if (loading) return null;
    setLoading(true);

    try {
      // Dynamically load the .NET WASM boot script from CDN
      const RUNTIME_BASE = "https://your-cdn.example.com/dotnet-wasm-runner";

      await new Promise((resolve, reject) => {
        const script = document.createElement("script");
        script.src = `${RUNTIME_BASE}/_framework/dotnet.js`;
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
      });

      // Initialize the .NET runtime
      const { getAssemblyExports, Module } = await window.dotnet
        .withDiagnosticTracing(false)
        .withResourceLoader((type, name, defaultUri) => {
          return `${RUNTIME_BASE}/_framework/${name}`;
        })
        .create();

      // Capture console output
      let consoleBuffer = [];
      Module.print = (text) => consoleBuffer.push(text);
      Module.printErr = (text) => consoleBuffer.push(`ERROR: ${text}`);

      const exports = await getAssemblyExports("dotnet-wasm-runner");

      window.__dotnetRuntime = {
        compile: (code) => {
          consoleBuffer = [];
          return exports.CSharpCompiler.CompileAndRun(code);
        },
        getOutput: () => consoleBuffer.join("\n"),
        clearOutput: () => { consoleBuffer = []; }
      };

      setRuntimeReady(true);
      return window.__dotnetRuntime;
    } catch (err) {
      setError(err.message);
      return null;
    } finally {
      setLoading(false);
    }
  }, [loading]);

  return { runtimeReady, loading, error, loadRuntime };
};
```

### Step 3: Create the Runner Component

**File**: `/snippets/dotnet-runner.jsx`

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
  const [runtimeReady, setRuntimeReady] = React.useState(false);
  const [runtimeLoading, setRuntimeLoading] = React.useState(false);

  const loadAndRun = async () => {
    setIsRunning(true);
    setOutput("");
    setErrors("");

    try {
      // Lazy-load runtime on first run
      if (!window.__dotnetRuntime) {
        setRuntimeLoading(true);

        const RUNTIME_BASE = "https://your-cdn.example.com/dotnet-wasm-runner";

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

        let consoleBuffer = [];
        Module.print = (text) => consoleBuffer.push(text);
        Module.printErr = (text) => consoleBuffer.push(`ERROR: ${text}`);

        const exports = await getAssemblyExports("dotnet-wasm-runner");

        window.__dotnetRuntime = {
          compile: (code) => {
            consoleBuffer = [];
            return exports.CSharpCompiler.CompileAndRun(code);
          },
          getOutput: () => consoleBuffer.join("\n"),
          clearOutput: () => { consoleBuffer = []; }
        };

        setRuntimeLoading(false);
        setRuntimeReady(true);
      }

      const runtime = window.__dotnetRuntime;
      runtime.clearOutput();

      const result = runtime.compile(code);

      if (result.success) {
        setOutput(runtime.getOutput());
      } else {
        setErrors(result.diagnostics);
      }
    } catch (err) {
      setErrors(`Execution error: ${err.message}`);
    } finally {
      setIsRunning(false);
    }
  };

  return (
    <div style={{ border: "1px solid var(--border)", borderRadius: "8px", overflow: "hidden", marginBottom: "1rem" }}>
      {/* Header */}
      <div style={{
        display: "flex", justifyContent: "space-between", alignItems: "center",
        padding: "8px 12px", borderBottom: "1px solid var(--border)",
        backgroundColor: "var(--background-secondary)"
      }}>
        <span style={{ fontSize: "0.85rem", fontWeight: 600 }}>{title}</span>
        <div style={{ display: "flex", gap: "8px", alignItems: "center" }}>
          {runtimeLoading && (
            <span style={{ fontSize: "0.8rem", opacity: 0.7 }}>Loading .NET runtime...</span>
          )}
          <span style={{ fontSize: "0.75rem", opacity: 0.5 }}>Client-side execution</span>
        </div>
      </div>

      {/* Editor */}
      <textarea
        value={code}
        onChange={(e) => setCode(e.target.value)}
        style={{
          width: "100%", height, fontFamily: "monospace", fontSize: "0.9rem",
          padding: "12px", border: "none", resize: "vertical",
          backgroundColor: "var(--background)", color: "var(--text)",
          outline: "none", boxSizing: "border-box"
        }}
        spellCheck={false}
      />

      {/* Run button */}
      <div style={{ padding: "8px 12px", borderTop: "1px solid var(--border)" }}>
        <button
          onClick={loadAndRun}
          disabled={isRunning}
          style={{
            padding: "6px 16px", borderRadius: "4px", border: "none",
            backgroundColor: "var(--primary)", color: "white", cursor: "pointer",
            fontSize: "0.85rem", fontWeight: 600,
            opacity: isRunning ? 0.6 : 1
          }}
        >
          {isRunning ? "Running..." : "Run"}
        </button>
      </div>

      {/* Output */}
      {output && (
        <div style={{ borderTop: "1px solid var(--border)", padding: "12px", backgroundColor: "var(--background-secondary)" }}>
          <div style={{ fontSize: "0.8rem", fontWeight: 600, marginBottom: "4px" }}>Output:</div>
          <pre style={{ margin: 0, fontFamily: "monospace", fontSize: "0.85rem", whiteSpace: "pre-wrap" }}>{output}</pre>
        </div>
      )}

      {/* Errors */}
      {errors && (
        <div style={{ borderTop: "1px solid var(--border)", padding: "12px", backgroundColor: "#fef2f2" }}>
          <div style={{ fontSize: "0.8rem", fontWeight: 600, marginBottom: "4px", color: "#dc2626" }}>Errors:</div>
          <pre style={{ margin: 0, fontFamily: "monospace", fontSize: "0.85rem", whiteSpace: "pre-wrap", color: "#dc2626" }}>{errors}</pre>
        </div>
      )}
    </div>
  );
};
```

### Step 4: Use in MDX Pages

```mdx
---
title: "Getting Started"
---

import { DotNetRunner } from "/snippets/dotnet-runner.jsx"

# Getting Started

Try editing and running this C# code directly in your browser:

<DotNetRunner
  title="Hello World"
  initialCode={`using System;

Console.WriteLine("Hello from .NET in the browser!");
Console.WriteLine($"Running on {Environment.Version}");`}
/>
```

### Step 5: Upgrade to a Rich Code Editor (Optional)

For a better editing experience, load Monaco Editor or CodeMirror from a CDN via Mintlify's custom scripts in `docs.json`:

```json
{
  "js": [
    "https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs/loader.js"
  ],
  "css": [
    "https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs/editor/editor.main.css"
  ]
}
```

Then update the `DotNetRunner` component to use Monaco instead of a `<textarea>` when it's available on `window`, falling back to the textarea if not loaded.

### Step 6: Build-Time Validation

Extend `dotnet easyaf mintlify` to:

1. **Scan MDX files** for `<DotNetRunner>` component usage
2. **Extract the `initialCode` props** from each instance
3. **Compile each code snippet** using Roslyn during the build
4. **Fail the build** if any example doesn't compile
5. **Store expected output** in frontmatter for optional runtime comparison

```csharp
public class MintlifyDocGenerator
{
    public async Task ValidateCodeExamplesAsync()
    {
        // Find all <DotNetRunner> components in MDX
        var examples = await FindCodeExamplesAsync();

        foreach (var example in examples)
        {
            // Compile and validate
            var result = await CompileCSharpAsync(example.Code);

            if (!result.Success)
            {
                throw new InvalidOperationException(
                    $"Example in {example.File} failed to compile: {result.Errors}"
                );
            }
        }
    }
}
```

---

## 5. Advanced Components

### IL/WASM Visualizer

A component that shows the C# source alongside its compiled IL, helping users understand what the compiler generates.

```jsx
export const CompilationVisualizer = ({ code }) => {
  const [il, setIL] = React.useState("");
  const [output, setOutput] = React.useState("");
  const [isCompiling, setIsCompiling] = React.useState(false);

  const compile = async () => {
    setIsCompiling(true);
    try {
      const runtime = window.__dotnetRuntime;
      if (!runtime) return;
      const result = runtime.compile(code);
      setIL(result.il || "");
      setOutput(result.output || "");
    } finally {
      setIsCompiling(false);
    }
  };

  return (
    <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
      <div>
        <h4>C# Code</h4>
        <pre><code>{code}</code></pre>
      </div>
      <div>
        <h4>IL (Intermediate Language)</h4>
        <button onClick={compile} disabled={isCompiling}>
          {isCompiling ? "Compiling..." : "Show IL"}
        </button>
        {il && <pre><code>{il}</code></pre>}
      </div>
    </div>
  );
};
```

### Performance Benchmark Playground

```jsx
export const BenchmarkPlayground = ({ code1, code2, label1 = "Approach A", label2 = "Approach B" }) => {
  const [results, setResults] = React.useState(null);
  const [running, setRunning] = React.useState(false);

  const runBenchmark = async () => {
    setRunning(true);
    const runtime = window.__dotnetRuntime;
    if (!runtime) return;

    const iterations = 1000;
    const times1 = [];
    const times2 = [];

    for (let i = 0; i < iterations; i++) {
      const start1 = performance.now();
      runtime.compile(code1);
      times1.push(performance.now() - start1);

      const start2 = performance.now();
      runtime.compile(code2);
      times2.push(performance.now() - start2);
    }

    const avg = (arr) => arr.reduce((a, b) => a + b, 0) / arr.length;
    const med = (arr) => { const s = [...arr].sort((a,b) => a-b); return s[Math.floor(s.length/2)]; };

    setResults({
      a: { avg: avg(times1).toFixed(2), median: med(times1).toFixed(2) },
      b: { avg: avg(times2).toFixed(2), median: med(times2).toFixed(2) }
    });
    setRunning(false);
  };

  return (
    <div>
      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem" }}>
        <div><h4>{label1}</h4><pre><code>{code1}</code></pre></div>
        <div><h4>{label2}</h4><pre><code>{code2}</code></pre></div>
      </div>
      <button onClick={runBenchmark} disabled={running}>
        {running ? "Running..." : `Run Benchmark (${1000} iterations)`}
      </button>
      {results && (
        <table>
          <thead><tr><th>Metric</th><th>{label1}</th><th>{label2}</th></tr></thead>
          <tbody>
            <tr><td>Average</td><td>{results.a.avg}ms</td><td>{results.b.avg}ms</td></tr>
            <tr><td>Median</td><td>{results.a.median}ms</td><td>{results.b.median}ms</td></tr>
          </tbody>
        </table>
      )}
    </div>
  );
};
```

### Interactive Tutorial with State Persistence

```jsx
export const InteractiveTutorial = ({ steps }) => {
  const [currentStep, setCurrentStep] = React.useState(0);
  const [completedCode, setCompletedCode] = React.useState({});

  const step = steps[currentStep];
  const initialCode = completedCode[currentStep] || step.initialCode;

  const handleSuccess = (code) => {
    setCompletedCode({ ...completedCode, [currentStep]: code });
    if (currentStep < steps.length - 1) {
      setCurrentStep(currentStep + 1);
    }
  };

  return (
    <div>
      <div style={{ display: "flex", gap: "8px", marginBottom: "1rem" }}>
        {steps.map((s, i) => (
          <span key={i} style={{
            padding: "4px 12px", borderRadius: "4px", fontSize: "0.85rem",
            backgroundColor: i === currentStep ? "var(--primary)" : "var(--background-secondary)",
            color: i === currentStep ? "white" : "inherit",
            cursor: "pointer"
          }} onClick={() => setCurrentStep(i)}>
            Step {i + 1}
          </span>
        ))}
      </div>
      <h3>{step.title}</h3>
      <p>{step.instructions}</p>
      {/* DotNetRunner must be imported separately in the MDX file */}
    </div>
  );
};
```

### LINQ Query Visualizer

```jsx
export const LINQVisualizer = ({ initialQuery }) => {
  const [query, setQuery] = React.useState(initialQuery || "");
  const [steps, setSteps] = React.useState([]);

  const visualizeQuery = async () => {
    const runtime = window.__dotnetRuntime;
    if (!runtime) return;

    // Wrap the query with instrumentation
    const instrumented = `
using System;
using System.Linq;
using System.Collections.Generic;

var data = Enumerable.Range(1, 20).ToList();
Console.WriteLine("Input: " + string.Join(", ", data));

${query}
`;

    const result = runtime.compile(instrumented);
    if (result.success) {
      setSteps(runtime.getOutput().split("\n").filter(Boolean));
    }
  };

  return (
    <div>
      <textarea
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        style={{ width: "100%", height: "150px", fontFamily: "monospace" }}
        spellCheck={false}
      />
      <button onClick={visualizeQuery}>Visualize Query</button>
      {steps.length > 0 && (
        <div>
          <h4>Pipeline Output:</h4>
          {steps.map((step, i) => (
            <pre key={i} style={{ margin: "4px 0", padding: "8px", backgroundColor: "var(--background-secondary)", borderRadius: "4px" }}>{step}</pre>
          ))}
        </div>
      )}
    </div>
  );
};
```

---

## 6. Comparison: Backend API vs. Direct WASM Runtime

| Feature | Backend API | Direct WASM Runtime |
|---------|-------------|---------------------|
| **Bundle Size** | ~500KB | 5-15MB (first load) |
| **Subsequent Loads** | ~500KB | <100KB (cached) |
| **Latency** | 200-500ms per request | None (client-side) |
| **Infrastructure Cost** | $5-10/month | $0 (static hosting for WASM) |
| **Customization** | High | Very High |
| **Feature Set** | Full C# | Full C# |
| **Offline Support** | No | Yes (cached) |
| **Privacy** | Code sent to server | Code stays in browser |
| **Mobile Support** | Good | Excellent |
| **Scalability** | Limited (server) | Infinite (client) |
| **Maintenance** | Moderate (server upkeep) | Low (static files) |

---

## 7. Implementation Approach

### Task 1: Build the WASM Runner Application

Build a minimal .NET WASM application that:
1. Exposes a `CompileAndRun(string code)` method via `[JSExport]`
2. Uses Roslyn to compile C# code in-browser
3. Captures `Console.WriteLine` output
4. Returns structured results (success/failure, output, diagnostics)

Apply aggressive trimming and IL linking to minimize bundle size.

### Task 2: Host WASM Artifacts on CDN

1. Publish the WASM application (`dotnet publish -c Release`)
2. Upload the `_framework/` output to Azure Blob Storage or similar CDN
3. Configure CORS headers to allow loading from the Mintlify docs domain
4. Set long-lived cache headers on `.wasm` and `.dll` files

### Task 3: Create Mintlify React Components

1. Create `/snippets/dotnet-runner.jsx` with the interactive runner component
2. Use a `<textarea>` for the initial implementation (no external editor dependency)
3. Lazy-load the WASM runtime only when the user clicks "Run" (no impact on page load)
4. Use inline styles (Mintlify components can't import CSS modules)

### Task 4: Integrate with Build Pipeline

1. Extend `dotnet easyaf mintlify` to scan for `<DotNetRunner>` usage in MDX files
2. Extract `initialCode` props and compile them during the build
3. Fail the build on compilation errors in examples
4. Generate test cases from examples for CI validation

### Task 5: Add Advanced Components

1. Build the IL Visualizer, Benchmark Playground, LINQ Visualizer, and Tutorial components
2. Each component is a separate `.jsx` file in `/snippets/`
3. All share the same global `window.__dotnetRuntime` singleton

### Task 6: Upgrade to Rich Editor (Optional)

1. Add Monaco Editor or CodeMirror via CDN in `docs.json` custom scripts
2. Update `DotNetRunner` to detect and use the editor when available
3. Fall back to `<textarea>` if the CDN script hasn't loaded

---

## 8. Bundle Size Optimization

### Strategy 1: IL Linking (Aggressive Tree-Shaking)

```xml
<!-- Project.csproj -->
<PropertyGroup>
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>link</TrimMode>
  <InvariantGlobalization>true</InvariantGlobalization>
  <EnableAggressiveTrimming>true</EnableAggressiveTrimming>
</PropertyGroup>
```

**Result**: Can reduce BCL from ~15MB to ~3-5MB

### Strategy 2: Lazy Loading

```javascript
// Load runtime core first (~5MB)
const core = await loadDotNetCore();

// Load additional assemblies on-demand
if (needsLinq) {
  await loadAssembly("System.Linq.dll");
}

if (needsHttp) {
  await loadAssembly("System.Net.Http.dll");
}
```

### Strategy 3: Compression

```
// Brotli compression reduces WASM bundles by 70-80%
// Before: 15MB
// After Brotli: 3-5MB
```

Ensure the CDN serves `.br` compressed files with proper `Content-Encoding` headers.

### Strategy 4: Code Splitting

```javascript
const chunks = {
  core: "dotnet.core.wasm",        // 2MB - always loaded
  compiler: "roslyn.wasm",          // 5MB - load on first "Run" click
  bcl: "system.*.wasm",             // 5MB - load on demand
  advanced: "linq.reflection.wasm"  // 3MB - load if needed
};
```

---

## 9. Security Considerations

### Sandboxing

```javascript
const sandbox = {
  allowedNamespaces: [
    "System",
    "System.Collections.Generic",
    "System.Linq",
    "System.Text"
  ],

  blockedNamespaces: [
    "System.IO",                        // No file system access
    "System.Net",                       // No network access
    "System.Reflection",                // No reflection (prevents escape)
    "System.Runtime.InteropServices"    // No P/Invoke
  ],

  maxExecutionTime: 5000,   // 5 second timeout
  maxMemory: 100 * 1024 * 1024  // 100MB limit
};
```

### Code Validation

```csharp
public bool ValidateCode(string code)
{
    var tree = CSharpSyntaxTree.ParseText(code);
    var root = tree.GetRoot();

    // Block dangerous patterns
    var walker = new SecuritySyntaxWalker();
    walker.Visit(root);

    return !walker.HasSecurityIssues;
}

class SecuritySyntaxWalker : CSharpSyntaxWalker
{
    public bool HasSecurityIssues { get; private set; }

    public override void VisitUsingDirective(UsingDirectiveSyntax node)
    {
        var name = node.Name.ToString();
        if (BlockedNamespaces.Contains(name))
        {
            HasSecurityIssues = true;
        }
    }
}
```

---

## 10. Cost Analysis

### Direct WASM Runtime (Recommended)

| Item | Cost |
|------|------|
| Infrastructure | $0 (client-side execution) |
| CDN hosting for WASM | $5-20/month (static files, drops with caching) |
| Maintenance | Minimal (static files, update on new .NET releases) |
| **Total** | **$5-20/month** |

### Backend API (Alternative)

| Item | Cost |
|------|------|
| Azure Functions / Container | $5-10/month |
| Bandwidth | $1-5/month |
| Storage | $1/month |
| Maintenance | Moderate (server upkeep, scaling, security patches) |
| **Total** | **$7-16/month** |

---

## 11. Open Questions

1. **Mintlify CSP**: Will Mintlify's Content Security Policy allow loading WASM files from our CDN? May need to use their custom frontend/reverse proxy configuration to add CSP directives.
2. **Performance at scale**: How does runtime performance degrade with 50+ `<DotNetRunner>` instances on one page? (Likely fine — they all share one runtime instance, and each is just a textarea until "Run" is clicked.)
3. **Mobile data usage**: Is 5-15MB runtime download acceptable for mobile users? Consider showing a "Load interactive examples" opt-in button on mobile.
4. **Browser compatibility**: The .NET WASM runtime requires WebAssembly support (all modern browsers since 2017). SharedArrayBuffer may be needed for threading.
5. **NuGet package support**: Can we load arbitrary NuGet packages at runtime? This would require downloading and extracting `.nupkg` files into the WASM filesystem.
6. **Mintlify custom scripts loading order**: Do custom scripts in `docs.json` load before or after React components hydrate? This affects whether we can rely on globally-loaded editors.

---

## 12. Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **User Engagement** | 50% users run at least 1 example | Analytics |
| **Time on Page** | 2x increase vs static docs | Analytics |
| **Code Modifications** | 30% users edit examples | Event tracking |
| **Page Load Time** | <3 seconds (first load) | Real User Monitoring |
| **Bounce Rate** | 20% decrease | Analytics |
| **Runtime Load Time** | <2 seconds (cached) | Performance monitoring |

---

## 13. Resources

### Technical References
- [.NET Interactive GitHub](https://github.com/dotnet/interactive) — Open-source runtime components
- [Blazor WebAssembly](https://learn.microsoft.com/aspnet/core/blazor/) — Underlying WASM technology
- [Running .NET in the Browser without Blazor](https://andrewlock.net/running-dotnet-in-the-browser-without-blazor/)
- [Mintlify React Components](https://www.mintlify.com/docs/customize/react-components) — Component constraints and patterns
- [Mintlify Reusable Snippets](https://www.mintlify.com/docs/create/reusable-snippets) — Import patterns for JSX components
- [Mintlify Custom Scripts](https://www.mintlify.com/docs/customize/custom-scripts) — Global JS/CSS injection

### Related Projects
- [Codapi](https://codapi.org/) — Alternative interactive code playground
- [dotnetfiddle](https://dotnetfiddle.net/) — Online C# playground
- [SharpLab](https://sharplab.io/) — C# to IL/WASM visualization

---

## 14. Conclusion

The ability to run C# code directly in the browser using the .NET WASM runtime, **without the Blazor DOM layer**, integrated with Mintlify's React component system, creates documentation that is:

- **Interactive**: Users can edit and run code instantly
- **Fast**: Zero server latency, runs entirely client-side
- **Scalable**: No infrastructure costs beyond static CDN hosting
- **Advanced**: IL visualization, profiling, benchmarking, LINQ visualization
- **Engaging**: Significantly improved user experience

The approach respects Mintlify's component constraints (arrow functions, `/snippets/` folder, no npm imports, no nested component imports) by using CDN-loaded dependencies and a global `window.__dotnetRuntime` singleton that's lazy-loaded on first use, ensuring zero impact on initial page load performance.

---

**Last Updated**: 2026-03-11
**Status**: Ready for Implementation
