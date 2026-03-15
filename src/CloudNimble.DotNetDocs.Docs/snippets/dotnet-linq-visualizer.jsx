export const DotNetLinqVisualizer = ({ initialQuery = '' }) => {
  const [ready, setReady] = React.useState(false);
  const [query, setQuery] = React.useState(initialQuery);
  const [steps, setSteps] = React.useState(null);
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

  const buildVisualizationCode = (linqQuery) => {
    return `
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;

var data = Enumerable.Range(1, 20);
var steps = new List<object>();

steps.Add(new { Step = "Source", Description = "Enumerable.Range(1, 20)", Values = string.Join(", ", data) });

var result = data.${linqQuery.trim()};
var resultList = result.ToList();

steps.Add(new { Step = "Result", Description = "${linqQuery.trim().replace(/"/g, '\\"')}", Values = string.Join(", ", resultList) });

Console.WriteLine(JsonSerializer.Serialize(steps));
`.trim();
  };

  const handleVisualize = async () => {
    setRunning(true);
    setSteps(null);
    setError(null);
    try {
      const runtime = await ensureRuntime();
      const exports = await runtime.getAssemblyExports('CloudNimble.DotNetDocs.WasmRunner');
      const wrappedCode = buildVisualizationCode(query);
      const resultJson = exports.Program.CompileAndRun(wrappedCode);
      const result = JSON.parse(resultJson);
      if (result.success) {
        try {
          const parsed = JSON.parse(result.output);
          setSteps(parsed);
        } catch {
          setSteps([{ Step: 'Output', Description: query, Values: result.output }]);
        }
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
    visualizeButton: {
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
    inputSection: {
      padding: '16px',
      borderBottom: '1px solid rgba(60, 208, 226, 0.1)'
    },
    inputLabel: {
      fontSize: '11px',
      textTransform: 'uppercase',
      letterSpacing: '1px',
      color: '#8B9DC3',
      marginBottom: '8px'
    },
    prefix: {
      fontSize: '13px',
      color: '#8B9DC3',
      marginBottom: '4px'
    },
    textarea: {
      width: '100%',
      minHeight: '60px',
      padding: '12px',
      background: 'rgba(0, 0, 0, 0.2)',
      color: '#e2e8f0',
      border: '1px solid rgba(60, 208, 226, 0.15)',
      borderRadius: '8px',
      outline: 'none',
      resize: 'vertical',
      fontSize: '14px',
      lineHeight: '1.6',
      fontFamily: "'Cascadia Code', 'JetBrains Mono', monospace",
      boxSizing: 'border-box'
    },
    pipelineContainer: {
      padding: '16px'
    },
    pipelineTitle: {
      fontSize: '11px',
      textTransform: 'uppercase',
      letterSpacing: '1px',
      color: '#8B9DC3',
      marginBottom: '16px'
    },
    stepCard: {
      marginBottom: '12px',
      borderRadius: '8px',
      border: '1px solid rgba(60, 208, 226, 0.12)',
      overflow: 'hidden',
      background: 'rgba(0, 0, 0, 0.15)'
    },
    stepHeader: {
      display: 'flex',
      alignItems: 'center',
      gap: '10px',
      padding: '10px 14px',
      borderBottom: '1px solid rgba(60, 208, 226, 0.08)',
      background: 'rgba(60, 208, 226, 0.04)'
    },
    stepBadge: {
      fontSize: '10px',
      fontWeight: '700',
      color: '#1e293b',
      background: '#3CD0E2',
      padding: '2px 8px',
      borderRadius: '4px',
      textTransform: 'uppercase',
      letterSpacing: '0.5px'
    },
    stepDescription: {
      fontSize: '13px',
      color: '#e2e8f0',
      fontWeight: '500'
    },
    stepValues: {
      padding: '10px 14px',
      fontSize: '13px',
      color: '#a5d6a7',
      lineHeight: '1.5',
      wordBreak: 'break-word'
    },
    arrow: {
      textAlign: 'center',
      color: '#3CD0E2',
      fontSize: '18px',
      padding: '4px 0',
      opacity: 0.6
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
        <span style={styles.title}>LINQ Query Visualizer</span>
        <button style={styles.visualizeButton} onClick={handleVisualize} disabled={running || loading}>
          {loading ? 'Loading Runtime…' : running ? 'Running…' : '▶ Visualize'}
        </button>
      </div>

      <div style={styles.inputSection}>
        <div style={styles.inputLabel}>LINQ Query</div>
        <div style={styles.prefix}>Enumerable.Range(1, 20).</div>
        <textarea
          style={styles.textarea}
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Where(x => x % 2 == 0).Select(x => x * x).Take(5)"
          spellCheck={false}
        />
      </div>

      {loading && (
        <div style={styles.loadingIndicator}>
          Initializing .NET WebAssembly runtime…
        </div>
      )}

      {error && (
        <pre style={styles.errorPre}>{error}</pre>
      )}

      {steps && (
        <div style={styles.pipelineContainer}>
          <div style={styles.pipelineTitle}>Pipeline Output</div>
          {steps.map((step, index) => (
            <React.Fragment key={index}>
              {index > 0 && (
                <div style={styles.arrow}>↓</div>
              )}
              <div style={styles.stepCard}>
                <div style={styles.stepHeader}>
                  <span style={styles.stepBadge}>{step.Step || `Step ${index + 1}`}</span>
                  <span style={styles.stepDescription}>{step.Description}</span>
                </div>
                <div style={styles.stepValues}>
                  {step.Values}
                </div>
              </div>
            </React.Fragment>
          ))}
        </div>
      )}
    </div>
  );
};
