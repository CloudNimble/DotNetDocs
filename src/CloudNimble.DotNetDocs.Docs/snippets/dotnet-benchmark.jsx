export const DotNetBenchmark = ({
  code1 = '',
  code2 = '',
  label1 = 'Approach A',
  label2 = 'Approach B',
  iterations = 100
}) => {
  const [ready, setReady] = React.useState(false);
  const [results, setResults] = React.useState(null);
  const [error, setError] = React.useState(null);
  const [running, setRunning] = React.useState(false);
  const [loading, setLoading] = React.useState(false);
  const [progress, setProgress] = React.useState('');
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

  const runSingle = async (exports, code, iterCount) => {
    const timings = [];
    for (let i = 0; i < iterCount; i++) {
      const start = performance.now();
      const resultJson = exports.Program.CompileAndRun(code);
      const elapsed = performance.now() - start;
      const result = JSON.parse(resultJson);
      if (!result.success) {
        throw new Error(result.error || 'Compilation failed');
      }
      timings.push(elapsed);
    }

    return timings;
  };

  const computeStats = (timings) => {
    const sorted = [...timings].sort((a, b) => a - b);
    const sum = sorted.reduce((acc, v) => acc + v, 0);
    const avg = sum / sorted.length;
    const median = sorted.length % 2 === 0
      ? (sorted[sorted.length / 2 - 1] + sorted[sorted.length / 2]) / 2
      : sorted[Math.floor(sorted.length / 2)];
    const min = sorted[0];
    const max = sorted[sorted.length - 1];

    return { avg, median, min, max };
  };

  const handleRunBenchmark = async () => {
    setRunning(true);
    setResults(null);
    setError(null);
    try {
      const runtime = await ensureRuntime();
      const exports = await runtime.getAssemblyExports('CloudNimble.DotNetDocs.WasmRunner');

      setProgress(`Running ${label1}…`);
      const timings1 = await runSingle(exports, code1, iterations);

      setProgress(`Running ${label2}…`);
      const timings2 = await runSingle(exports, code2, iterations);

      setProgress('');
      setResults({
        a: computeStats(timings1),
        b: computeStats(timings2)
      });
    } catch (err) {
      setError(err.message);
      setProgress('');
    } finally {
      setRunning(false);
    }
  };

  const formatMs = (ms) => {
    if (ms < 1) {
      return `${(ms * 1000).toFixed(0)}μs`;
    }

    return `${ms.toFixed(2)}ms`;
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
    codeGrid: {
      display: 'grid',
      gridTemplateColumns: '1fr 1fr'
    },
    codePanel: {
      padding: '0',
      overflow: 'hidden'
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
      background: 'transparent'
    },
    resultsContainer: {
      borderTop: '1px solid rgba(60, 208, 226, 0.15)',
      padding: '16px'
    },
    resultsTitle: {
      fontSize: '11px',
      textTransform: 'uppercase',
      letterSpacing: '1px',
      color: '#8B9DC3',
      marginBottom: '12px'
    },
    table: {
      width: '100%',
      borderCollapse: 'collapse',
      fontSize: '13px'
    },
    th: {
      textAlign: 'left',
      padding: '8px 12px',
      color: '#8B9DC3',
      borderBottom: '1px solid rgba(60, 208, 226, 0.15)',
      fontWeight: '600',
      fontSize: '11px',
      textTransform: 'uppercase',
      letterSpacing: '0.5px'
    },
    td: {
      padding: '8px 12px',
      color: '#e2e8f0',
      borderBottom: '1px solid rgba(60, 208, 226, 0.08)'
    },
    tdLabel: {
      padding: '8px 12px',
      color: '#3CD0E2',
      fontWeight: '600',
      borderBottom: '1px solid rgba(60, 208, 226, 0.08)'
    },
    winnerCell: {
      padding: '8px 12px',
      color: '#a5d6a7',
      fontWeight: '600',
      borderBottom: '1px solid rgba(60, 208, 226, 0.08)'
    },
    progressText: {
      fontSize: '12px',
      color: '#3CD0E2',
      padding: '8px 16px',
      textAlign: 'center',
      borderTop: '1px solid rgba(60, 208, 226, 0.15)'
    },
    errorPre: {
      margin: '16px',
      padding: '12px',
      background: 'rgba(239, 68, 68, 0.1)',
      border: '1px solid rgba(239, 68, 68, 0.3)',
      borderRadius: '8px',
      color: '#ef4444',
      fontSize: '13px',
      fontFamily: "'Cascadia Code', 'JetBrains Mono', monospace",
      whiteSpace: 'pre-wrap'
    },
    iterBadge: {
      fontSize: '11px',
      color: '#8B9DC3',
      background: 'rgba(139, 157, 195, 0.1)',
      padding: '2px 8px',
      borderRadius: '4px',
      marginLeft: '8px'
    }
  };

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <div style={{ display: 'flex', alignItems: 'center' }}>
          <span style={styles.title}>Benchmark Playground</span>
          <span style={styles.iterBadge}>{iterations} iterations</span>
        </div>
        <button style={styles.runButton} onClick={handleRunBenchmark} disabled={running || loading}>
          {loading ? 'Loading Runtime…' : running ? 'Running…' : '⏱ Run Benchmark'}
        </button>
      </div>

      <div style={styles.codeGrid}>
        <div style={styles.codePanel}>
          <div style={styles.panelLabel}>{label1}</div>
          <pre style={styles.codePre}>{code1}</pre>
        </div>
        <div style={{ ...styles.codePanel, borderLeft: '1px solid rgba(60, 208, 226, 0.15)' }}>
          <div style={styles.panelLabel}>{label2}</div>
          <pre style={styles.codePre}>{code2}</pre>
        </div>
      </div>

      {progress && (
        <div style={styles.progressText}>{progress}</div>
      )}

      {error && (
        <pre style={styles.errorPre}>{error}</pre>
      )}

      {results && (
        <div style={styles.resultsContainer}>
          <div style={styles.resultsTitle}>Results</div>
          <table style={styles.table}>
            <thead>
              <tr>
                <th style={styles.th}>Metric</th>
                <th style={styles.th}>{label1}</th>
                <th style={styles.th}>{label2}</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td style={styles.tdLabel}>Average</td>
                <td style={results.a.avg <= results.b.avg ? styles.winnerCell : styles.td}>
                  {formatMs(results.a.avg)}
                </td>
                <td style={results.b.avg <= results.a.avg ? styles.winnerCell : styles.td}>
                  {formatMs(results.b.avg)}
                </td>
              </tr>
              <tr>
                <td style={styles.tdLabel}>Median</td>
                <td style={results.a.median <= results.b.median ? styles.winnerCell : styles.td}>
                  {formatMs(results.a.median)}
                </td>
                <td style={results.b.median <= results.a.median ? styles.winnerCell : styles.td}>
                  {formatMs(results.b.median)}
                </td>
              </tr>
              <tr>
                <td style={styles.tdLabel}>Min</td>
                <td style={styles.td}>{formatMs(results.a.min)}</td>
                <td style={styles.td}>{formatMs(results.b.min)}</td>
              </tr>
              <tr>
                <td style={styles.tdLabel}>Max</td>
                <td style={styles.td}>{formatMs(results.a.max)}</td>
                <td style={styles.td}>{formatMs(results.b.max)}</td>
              </tr>
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};
