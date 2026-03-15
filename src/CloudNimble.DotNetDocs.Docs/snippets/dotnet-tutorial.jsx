export const DotNetTutorial = ({ steps = [] }) => {
  const [ready, setReady] = React.useState(false);
  const [currentStep, setCurrentStep] = React.useState(0);
  const [codeByStep, setCodeByStep] = React.useState(() =>
    steps.reduce((acc, step, i) => ({ ...acc, [i]: step.initialCode || '' }), {})
  );
  const [output, setOutput] = React.useState(null);
  const [error, setError] = React.useState(null);
  const [running, setRunning] = React.useState(false);
  const [loading, setLoading] = React.useState(false);
  const [completedSteps, setCompletedSteps] = React.useState({});
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
      const resultJson = exports.Program.CompileAndRun(codeByStep[currentStep]);
      const result = JSON.parse(resultJson);
      if (result.success) {
        setOutput(result.output || '(no output)');
        setCompletedSteps((prev) => ({ ...prev, [currentStep]: true }));
      } else {
        setError(result.error || 'Compilation failed');
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setRunning(false);
    }
  };

  const handleStepChange = (index) => {
    setCurrentStep(index);
    setOutput(null);
    setError(null);
  };

  const handleCodeChange = (e) => {
    setCodeByStep((prev) => ({ ...prev, [currentStep]: e.target.value }));
  };

  const step = steps[currentStep];

  if (!step) {
    return null;
  }

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
    stepNav: {
      display: 'flex',
      gap: '0',
      padding: '0',
      borderBottom: '1px solid rgba(60, 208, 226, 0.1)',
      overflowX: 'auto'
    },
    stepButton: (index) => ({
      padding: '10px 20px',
      background: index === currentStep ? 'rgba(60, 208, 226, 0.1)' : 'transparent',
      color: index === currentStep ? '#3CD0E2' : '#8B9DC3',
      border: 'none',
      borderBottom: index === currentStep ? '2px solid #3CD0E2' : '2px solid transparent',
      fontSize: '13px',
      fontWeight: index === currentStep ? '600' : '400',
      cursor: 'pointer',
      fontFamily: "'Cascadia Code', 'JetBrains Mono', monospace",
      transition: 'all 0.2s',
      whiteSpace: 'nowrap',
      display: 'flex',
      alignItems: 'center',
      gap: '6px'
    }),
    completedDot: {
      width: '8px',
      height: '8px',
      borderRadius: '50%',
      background: '#a5d6a7',
      display: 'inline-block',
      flexShrink: 0
    },
    pendingDot: {
      width: '8px',
      height: '8px',
      borderRadius: '50%',
      border: '1px solid #8B9DC3',
      display: 'inline-block',
      flexShrink: 0
    },
    instructionsSection: {
      padding: '16px',
      borderBottom: '1px solid rgba(60, 208, 226, 0.1)'
    },
    stepTitle: {
      fontSize: '16px',
      fontWeight: '700',
      color: '#e2e8f0',
      marginBottom: '8px'
    },
    instructions: {
      fontSize: '14px',
      color: '#B0C4DE',
      lineHeight: '1.7',
      fontFamily: "-apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif"
    },
    editorSection: {
      borderBottom: '1px solid rgba(60, 208, 226, 0.1)'
    },
    editorHeader: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      padding: '8px 16px',
      borderBottom: '1px solid rgba(60, 208, 226, 0.08)'
    },
    editorLabel: {
      fontSize: '11px',
      textTransform: 'uppercase',
      letterSpacing: '1px',
      color: '#8B9DC3'
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
      minHeight: '180px',
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
      textAlign: 'center',
      borderTop: '1px solid rgba(60, 208, 226, 0.15)'
    },
    progressBar: {
      display: 'flex',
      alignItems: 'center',
      gap: '8px',
      padding: '8px 16px',
      fontSize: '12px',
      color: '#8B9DC3',
      borderTop: '1px solid rgba(60, 208, 226, 0.1)'
    },
    progressFill: {
      flex: 1,
      height: '4px',
      background: 'rgba(60, 208, 226, 0.15)',
      borderRadius: '2px',
      overflow: 'hidden'
    },
    progressInner: {
      height: '100%',
      background: 'linear-gradient(90deg, #3CD0E2, #419AC5)',
      borderRadius: '2px',
      transition: 'width 0.3s ease'
    }
  };

  const completedCount = Object.keys(completedSteps).length;

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <span style={styles.title}>Interactive Tutorial</span>
        <span style={{ fontSize: '12px', color: '#8B9DC3' }}>
          {completedCount}/{steps.length} completed
        </span>
      </div>

      {/* Step navigation */}
      <div style={styles.stepNav}>
        {steps.map((s, index) => (
          <button
            key={index}
            style={styles.stepButton(index)}
            onClick={() => handleStepChange(index)}
          >
            <span style={completedSteps[index] ? styles.completedDot : styles.pendingDot} />
            {index + 1}. {s.title}
          </button>
        ))}
      </div>

      {/* Instructions */}
      <div style={styles.instructionsSection}>
        <div style={styles.stepTitle}>{step.title}</div>
        <div style={styles.instructions}>{step.instructions}</div>
      </div>

      {/* Editor */}
      <div style={styles.editorSection}>
        <div style={styles.editorHeader}>
          <span style={styles.editorLabel}>Code Editor</span>
          <div style={styles.buttonGroup}>
            <button
              style={styles.resetButton}
              onClick={() => setCodeByStep((prev) => ({ ...prev, [currentStep]: step.initialCode || '' }))}
              disabled={running}
            >
              Reset
            </button>
            <button style={styles.runButton} onClick={handleRun} disabled={running || loading}>
              {loading ? 'Loading…' : running ? 'Running…' : '▶ Run'}
            </button>
          </div>
        </div>
        <textarea
          style={styles.textarea}
          value={codeByStep[currentStep]}
          onChange={handleCodeChange}
          spellCheck={false}
        />
      </div>

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

      {/* Progress bar */}
      <div style={styles.progressBar}>
        <span>Progress</span>
        <div style={styles.progressFill}>
          <div style={{
            ...styles.progressInner,
            width: `${steps.length > 0 ? (completedCount / steps.length) * 100 : 0}%`
          }} />
        </div>
        <span>{Math.round(steps.length > 0 ? (completedCount / steps.length) * 100 : 0)}%</span>
      </div>
    </div>
  );
};
