export const DotNetILVisualizer = ({ code = '' }) => {
  const [ready, setReady] = React.useState(false);
  const [ilOutput, setIlOutput] = React.useState(null);
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

  const handleShowIL = async () => {
    setRunning(true);
    setIlOutput(null);
    setError(null);
    try {
      const runtime = await ensureRuntime();
      const exports = await runtime.getAssemblyExports('CloudNimble.DotNetDocs.WasmRunner');
      const resultJson = exports.Program.CompileAndRun(code);
      const result = JSON.parse(resultJson);
      if (result.success) {
        setIlOutput(result.il || result.output || '(no IL output available)');
      } else {
        setError(result.error || 'Compilation failed');
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setRunning(false);
    }
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
    showButton: {
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
    grid: {
      display: 'grid',
      gridTemplateColumns: ilOutput || error ? '1fr 1fr' : '1fr',
      minHeight: '200px'
    },
    panel: {
      padding: '0'
    },
    panelLabel: {
      fontSize: '11px',
      textTransform: 'uppercase',
      letterSpacing: '1px',
      color: '#8B9DC3',
      padding: '8px 16px',
      borderBottom: '1px solid rgba(60, 208, 226, 0.1)',
      margin: 0
    },
    codePre: {
      margin: 0,
      padding: '16px',
      color: '#e2e8f0',
      fontSize: '13px',
      lineHeight: '1.6',
      fontFamily: "'Cascadia Code', 'JetBrains Mono', monospace",
      whiteSpace: 'pre-wrap',
      wordBreak: 'break-word',
      background: 'transparent',
      height: '100%',
      boxSizing: 'border-box'
    },
    ilPre: {
      margin: 0,
      padding: '16px',
      color: '#a5d6a7',
      fontSize: '13px',
      lineHeight: '1.6',
      fontFamily: "'Cascadia Code', 'JetBrains Mono', monospace",
      whiteSpace: 'pre-wrap',
      wordBreak: 'break-word',
      background: 'rgba(0, 0, 0, 0.2)',
      height: '100%',
      boxSizing: 'border-box'
    },
    divider: {
      width: '1px',
      background: 'rgba(60, 208, 226, 0.15)'
    },
    errorPre: {
      margin: 0,
      padding: '16px',
      color: '#ef4444',
      fontSize: '13px',
      lineHeight: '1.6',
      fontFamily: "'Cascadia Code', 'JetBrains Mono', monospace",
      whiteSpace: 'pre-wrap',
      wordBreak: 'break-word',
      background: 'rgba(239, 68, 68, 0.05)'
    },
    loadingIndicator: {
      fontSize: '12px',
      color: '#3CD0E2',
      padding: '8px 16px',
      textAlign: 'center',
      borderTop: '1px solid rgba(60, 208, 226, 0.15)'
    }
  };

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <span style={styles.title}>IL Visualizer</span>
        <button style={styles.showButton} onClick={handleShowIL} disabled={running || loading}>
          {loading ? 'Loading Runtime…' : running ? 'Compiling…' : '⚙ Show IL'}
        </button>
      </div>
      <div style={styles.grid}>
        <div style={styles.panel}>
          <div style={styles.panelLabel}>C# Source</div>
          <pre style={styles.codePre}>{code}</pre>
        </div>
        {(ilOutput || error) && (
          <div style={{ ...styles.panel, borderLeft: '1px solid rgba(60, 208, 226, 0.15)' }}>
            <div style={styles.panelLabel}>{error ? 'Error' : 'IL Output'}</div>
            {error ? (
              <pre style={styles.errorPre}>{error}</pre>
            ) : (
              <pre style={styles.ilPre}>{ilOutput}</pre>
            )}
          </div>
        )}
      </div>
      {loading && (
        <div style={styles.loadingIndicator}>
          Initializing .NET WebAssembly runtime…
        </div>
      )}
    </div>
  );
};
