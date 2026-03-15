export const DotNetRunner = ({ initialCode = '', height = '200px', title = 'C# Example' }) => {
  const [ready, setReady] = React.useState(false);
  const [code, setCode] = React.useState(initialCode);
  const [output, setOutput] = React.useState(null);
  const [error, setError] = React.useState(null);
  const [running, setRunning] = React.useState(false);
  const [loading, setLoading] = React.useState(false);
  const runtimeRef = React.useRef(null);

  React.useEffect(() => {
    setReady(true);
  }, []);

  const ensureRuntime = async () => {
    if (runtimeRef.current) {
      return runtimeRef.current;
    }
    setLoading(true);
    try {
      const baseUrl = window.__dotnetRuntimeBase || '/dotnet-wasm-runner';
      const runtime = await window.dotnet
        .withDiagnosticTracing(false)
        .withResourceLoader((type, name, defaultUri) => {
          return `${baseUrl}/${name}`;
        })
        .create();
      runtimeRef.current = runtime;

      return runtime;
    } catch (err) {
      throw new Error(`Failed to load .NET runtime: ${err.message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleRun = async () => {
    setRunning(true);
    setOutput(null);
    setError(null);
    try {
      const runtime = await ensureRuntime();
      const exports = await runtime.getAssemblyExports('CloudNimble.DotNetDocs.WasmRunner');
      const resultJson = exports.Program.CompileAndRun(code);
      const result = JSON.parse(resultJson);
      if (result.success) {
        setOutput(result.output || '(no output)');
      } else {
        setError(result.error || 'Unknown compilation error');
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setRunning(false);
    }
  };

  const handleReset = () => {
    setCode(initialCode);
    setOutput(null);
    setError(null);
  };

  const styles = {
    container: {
      opacity: ready ? 1 : 0,
      transition: 'opacity 0.3s ease-out',
      borderRadius: '12px',
      border: '1px solid rgba(60, 208, 226, 0.2)',
      background: 'var(--tw-prose-pre-bg, #1e293b)',
      overflow: 'hidden',
      fontFamily: "'Cascadia Code', 'JetBrains Mono', monospace"
    },
    header: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      padding: '12px 16px',
      borderBottom: '1px solid rgba(60, 208, 226, 0.15)',
      background: 'rgba(60, 208, 226, 0.05)'
    },
    title: {
      fontSize: '14px',
      fontWeight: '600',
      color: '#3CD0E2',
      margin: 0,
      letterSpacing: '0.5px'
    },
    buttonGroup: {
      display: 'flex',
      gap: '8px'
    },
    runButton: {
      padding: '6px 16px',
      background: running || loading ? 'rgba(60, 208, 226, 0.3)' : 'linear-gradient(135deg, #3CD0E2, #419AC5)',
      color: 'white',
      border: 'none',
      borderRadius: '6px',
      fontSize: '13px',
      fontWeight: '600',
      cursor: running || loading ? 'wait' : 'pointer',
      fontFamily: "'Cascadia Code', 'JetBrains Mono', monospace",
      transition: 'all 0.2s'
    },
    resetButton: {
      padding: '6px 16px',
      background: 'transparent',
      color: '#8B9DC3',
      border: '1px solid rgba(139, 157, 195, 0.3)',
      borderRadius: '6px',
      fontSize: '13px',
      fontWeight: '600',
      cursor: 'pointer',
      fontFamily: "'Cascadia Code', 'JetBrains Mono', monospace",
      transition: 'all 0.2s'
    },
    textarea: {
      width: '100%',
      height: height,
      padding: '16px',
      background: 'transparent',
      color: '#e2e8f0',
      border: 'none',
      outline: 'none',
      resize: 'vertical',
      fontSize: '14px',
      lineHeight: '1.6',
      fontFamily: "'Cascadia Code', 'JetBrains Mono', monospace",
      boxSizing: 'border-box'
    },
    outputContainer: {
      borderTop: '1px solid rgba(60, 208, 226, 0.15)',
      padding: '12px 16px'
    },
    outputLabel: {
      fontSize: '11px',
      textTransform: 'uppercase',
      letterSpacing: '1px',
      color: '#8B9DC3',
      marginBottom: '8px'
    },
    outputPre: {
      margin: 0,
      padding: '12px',
      background: 'rgba(0, 0, 0, 0.3)',
      borderRadius: '8px',
      color: '#a5d6a7',
      fontSize: '13px',
      lineHeight: '1.5',
      fontFamily: "'Cascadia Code', 'JetBrains Mono', monospace",
      whiteSpace: 'pre-wrap',
      wordBreak: 'break-word'
    },
    errorPre: {
      margin: 0,
      padding: '12px',
      background: 'rgba(239, 68, 68, 0.1)',
      border: '1px solid rgba(239, 68, 68, 0.3)',
      borderRadius: '8px',
      color: '#ef4444',
      fontSize: '13px',
      lineHeight: '1.5',
      fontFamily: "'Cascadia Code', 'JetBrains Mono', monospace",
      whiteSpace: 'pre-wrap',
      wordBreak: 'break-word'
    },
    loadingIndicator: {
      fontSize: '12px',
      color: '#3CD0E2',
      padding: '8px 16px',
      borderTop: '1px solid rgba(60, 208, 226, 0.15)',
      textAlign: 'center'
    }
  };

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <span style={styles.title}>{title}</span>
        <div style={styles.buttonGroup}>
          <button style={styles.resetButton} onClick={handleReset} disabled={running}>
            Reset
          </button>
          <button style={styles.runButton} onClick={handleRun} disabled={running || loading}>
            {loading ? 'Loading Runtime…' : running ? 'Running…' : '▶ Run'}
          </button>
        </div>
      </div>
      <textarea
        style={styles.textarea}
        value={code}
        onChange={(e) => setCode(e.target.value)}
        spellCheck={false}
      />
      {loading && (
        <div style={styles.loadingIndicator}>
          Initializing .NET WebAssembly runtime…
        </div>
      )}
      {output !== null && (
        <div style={styles.outputContainer}>
          <div style={styles.outputLabel}>Output</div>
          <pre style={styles.outputPre}>{output}</pre>
        </div>
      )}
      {error !== null && (
        <div style={styles.outputContainer}>
          <div style={styles.outputLabel}>Error</div>
          <pre style={styles.errorPre}>{error}</pre>
        </div>
      )}
    </div>
  );
};
