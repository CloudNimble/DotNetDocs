export const SaaSHero = () => {
  return (
    <div style={{
      width: '100%',
      minHeight: '100vh',
      background: 'linear-gradient(135deg, #0A1628 0%, #1A2B3D 50%, #0A1628 100%)',
      position: 'relative',
      overflow: 'hidden',
      display: 'flex',
      alignItems: 'center',
      padding: '80px 0'
    }}>
      {/* Animated gradient orbs */}
      <div style={{
        position: 'absolute',
        width: '800px',
        height: '800px',
        background: 'radial-gradient(circle, #3CD0E2 0%, transparent 70%)',
        opacity: '0.15',
        borderRadius: '50%',
        top: '-200px',
        left: '-400px',
        filter: 'blur(100px)',
        animation: 'float 20s infinite ease-in-out'
      }} />

      <div style={{
        position: 'absolute',
        width: '600px',
        height: '600px',
        background: 'radial-gradient(circle, #419AC5 0%, transparent 70%)',
        opacity: '0.2',
        borderRadius: '50%',
        bottom: '-100px',
        right: '-300px',
        filter: 'blur(80px)',
        animation: 'float 15s infinite ease-in-out reverse'
      }} />

      {/* Grid pattern */}
      <div style={{
        position: 'absolute',
        inset: 0,
        backgroundImage: `
          linear-gradient(#3CD0E2 1px, transparent 1px),
          linear-gradient(90deg, #3CD0E2 1px, transparent 1px)
        `,
        backgroundSize: '100px 100px',
        opacity: 0.03
      }} />

      {/* Content */}
      <div style={{
        maxWidth: '1400px',
        margin: '0 auto',
        padding: '0 40px',
        position: 'relative',
        zIndex: 10,
        width: '100%'
      }}>
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(500px, 1fr))',
          gap: '80px',
          alignItems: 'center'
        }}>
          {/* Left content */}
          <div style={{ animation: 'slideInLeft 1s ease-out' }}>
            {/* Logo */}
            <div style={{
              marginBottom: '40px',
              display: 'flex',
              alignItems: 'center',
              gap: '20px'
            }}>
              <img
                src="/images/logos/dotnetdocs.dark.svg"
                alt="DotNetDocs"
                style={{
                  height: '56px',
                  width: 'auto'
                }}
              />
            </div>

            <div style={{
              display: 'none', // Hidden but preserved for future use
              // display: 'inline-flex',
              alignItems: 'center',
              padding: '8px 20px',
              background: 'linear-gradient(135deg, #3CD0E2, #419AC5)',
              borderRadius: '50px',
              marginBottom: '30px',
              boxShadow: '0 0 30px rgba(60, 208, 226, 0.3)'
            }}>
              <span style={{
                background: 'white',
                padding: '4px 8px',
                borderRadius: '20px',
                fontSize: '12px',
                fontWeight: 'bold',
                color: '#419AC5',
                marginRight: '10px'
              }}>NEW</span>
              <span style={{
                color: 'white',
                fontSize: '14px',
                fontWeight: '600'
              }}>Transform XML → Beautiful Docs in Seconds</span>
            </div>

            <h1 className="hero-heading">
              <span className="hero-heading-line1">Documentation</span>
              <span className="hero-heading-line2">That Ships</span>
              <span className="hero-heading-line3">With Your Code</span>
            </h1>

            <div style={{
              paddingBottom: '30px',
              maxWidth: '500px'
            }}>
              <p style={{
                fontSize: '20px',
                lineHeight: '1.6',
                color: '#B0C4DE',
                margin: 0
              }}>
                Turn your amazing .NET projects into stunning <nobr>AI-ready</nobr> documentation sites.
                Zero config. Full control. Built for developers.
              </p>
            </div>

            <div style={{
              display: 'flex',
              gap: '20px',
              flexWrap: 'wrap',
              marginBottom: '60px'
            }}>
              <a href="/quickstart" style={{
                padding: '18px 40px',
                background: 'linear-gradient(135deg, #3CD0E2, #419AC5)',
                color: 'white',
                borderRadius: '12px',
                textDecoration: 'none',
                fontWeight: 'bold',
                fontSize: '16px',
                boxShadow: '0 10px 40px rgba(60, 208, 226, 0.3)',
                transition: 'all 0.3s',
                display: 'inline-flex',
                alignItems: 'center',
                gap: '10px'
              }}>
                Start Building
                <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
                  <path d="M10.293 3.293a1 1 0 011.414 0l6 6a1 1 0 010 1.414l-6 6a1 1 0 01-1.414-1.414L14.586 11H3a1 1 0 110-2h11.586l-4.293-4.293a1 1 0 010-1.414z" />
                </svg>
              </a>

              <a href="https://github.com/CloudNimble/DotNetDocs" style={{
                padding: '18px 40px',
                background: 'rgba(255, 255, 255, 0.1)',
                backdropFilter: 'blur(10px)',
                color: 'white',
                border: '2px solid rgba(60, 208, 226, 0.3)',
                borderRadius: '12px',
                textDecoration: 'none',
                fontWeight: 'bold',
                fontSize: '16px',
                transition: 'all 0.3s',
                display: 'inline-flex',
                alignItems: 'center',
                gap: '10px'
              }}>
                <svg width="20" height="20" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/>
                </svg>
                View on GitHub
              </a>
            </div>

            {/* Created By & Sponsors */}
            <div style={{
              paddingTop: '40px',
              borderTop: '1px solid rgba(60, 208, 226, 0.2)',
              display: 'flex',
              gap: '60px',
              alignItems: 'flex-start',
              flexWrap: 'wrap'
            }}>
              {/* Created By */}
              <div>
                <div style={{
                  color: '#8B9DC3',
                  fontSize: '12px',
                  fontWeight: '600',
                  letterSpacing: '1px',
                  textTransform: 'uppercase',
                  marginBottom: '20px'
                }}>
                  Created By
                </div>
                <a href="https://github.com/cloudnimble" target="_blank" rel="noopener noreferrer">
                  <img
                    src="/images/logos/cloudnimble.dark.svg"
                    alt="CloudNimble"
                    style={{
                      height: '28px',
                      width: 'auto',
                      opacity: 0.7,
                      transition: 'opacity 0.3s',
                      cursor: 'pointer'
                    }}
                    onMouseOver={(e) => e.currentTarget.style.opacity = '1'}
                    onMouseOut={(e) => e.currentTarget.style.opacity = '0.7'}
                  />
                </a>
              </div>

              {/* Sponsored By */}
              <div>
                <div style={{
                  color: '#8B9DC3',
                  fontSize: '12px',
                  fontWeight: '600',
                  letterSpacing: '1px',
                  textTransform: 'uppercase',
                  marginBottom: '20px'
                }}>
                  Sponsored By
                </div>
                <div style={{
                  display: 'flex',
                  gap: '40px',
                  alignItems: 'center',
                  flexWrap: 'wrap'
                }}>
                  <a href="/providers/mintlify" target="_blank" rel="noopener noreferrer">
                    <img
                      src="/images/logos/mintlify.dark.svg"
                      alt="Mintlify"
                      style={{
                        height: '28px',
                        width: 'auto',
                        opacity: 0.7,
                        transition: 'opacity 0.3s',
                        cursor: 'pointer'
                      }}
                      onMouseOver={(e) => e.currentTarget.style.opacity = '1'}
                      onMouseOut={(e) => e.currentTarget.style.opacity = '0.7'}
                    />
                  </a>
                  <a href="https://sustainment.com" target="_blank" rel="noopener noreferrer">
                    <img
                      src="/images/logos/sustainment.dark.svg"
                      alt="Sustainment"
                      style={{
                        height: '28px',
                        width: 'auto',
                        opacity: 0.7,
                        transition: 'opacity 0.3s',
                        cursor: 'pointer'
                      }}
                      onMouseOver={(e) => e.currentTarget.style.opacity = '1'}
                      onMouseOut={(e) => e.currentTarget.style.opacity = '0.7'}
                    />
                  </a>
                </div>
              </div>
            </div>
          </div>

          {/* Right content - Interactive demo */}
          <div style={{
            position: 'relative',
            animation: 'slideInRight 1s ease-out'
          }}>
            {/* Code window */}
            <div style={{
              background: 'rgba(10, 22, 40, 0.8)',
              backdropFilter: 'blur(20px)',
              borderRadius: '16px',
              border: '1px solid rgba(60, 208, 226, 0.2)',
              overflow: 'hidden',
              boxShadow: '0 30px 60px rgba(0, 0, 0, 0.5)'
            }}>
              <div style={{
                padding: '12px 20px',
                background: 'rgba(60, 208, 226, 0.1)',
                borderBottom: '1px solid rgba(60, 208, 226, 0.2)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between'
              }}>
                <div style={{ display: 'flex', gap: '8px' }}>
                  <div style={{ width: '12px', height: '12px', borderRadius: '50%', background: '#ff5f57' }} />
                  <div style={{ width: '12px', height: '12px', borderRadius: '50%', background: '#ffbd2e' }} />
                  <div style={{ width: '12px', height: '12px', borderRadius: '50%', background: '#28ca42' }} />
                </div>
                <span style={{ color: '#8B9DC3', fontSize: '12px', fontFamily: 'monospace' }}>MyProject.cs</span>
              </div>

              <div style={{ padding: '30px', fontFamily: 'monospace', fontSize: '14px' }}>
                <div style={{ color: '#608B4E' }}>/// &lt;summary&gt;</div>
                <div style={{ color: '#608B4E' }}>/// Processes payment transactions</div>
                <div style={{ color: '#608B4E' }}>/// &lt;/summary&gt;</div>
                <div>
                  <span style={{ color: '#608B4E' }}>/// &lt;param </span>
                  <span style={{ color: '#9CDCFE' }}>name</span>
                  <span style={{ color: '#D4D4D4' }}>=</span>
                  <span style={{ color: '#CE9178' }}>"amount"</span>
                  <span style={{ color: '#608B4E' }}>&gt;The transaction amount to process&lt;/param&gt;</span>
                </div>
                <div>
                  <span style={{ color: '#608B4E' }}>/// &lt;returns&gt;A &lt;see </span>
                  <span style={{ color: '#9CDCFE' }}>cref</span>
                  <span style={{ color: '#D4D4D4' }}>=</span>
                  <span style={{ color: '#CE9178' }}>"PaymentResult"</span>
                  <span style={{ color: '#608B4E' }}>/&gt; indicating success or failure&lt;/returns&gt;</span>
                </div>
                <div>
                  <span style={{ color: '#569CD6' }}>public </span>
                  <span style={{ color: '#569CD6' }}>async </span>
                  <span style={{ color: '#4EC9B0' }}>Task</span>
                  <span style={{ color: '#D4D4D4' }}>&lt;</span>
                  <span style={{ color: '#4EC9B0' }}>PaymentResult</span>
                  <span style={{ color: '#D4D4D4' }}>&gt; </span>
                  <span style={{ color: '#DCDCAA' }}>ProcessPayment</span>
                  <span style={{ color: '#D4D4D4' }}>(</span>
                  <span style={{ color: '#569CD6' }}>decimal</span>
                  <span style={{ color: '#9CDCFE' }}> amount</span>
                  <span style={{ color: '#D4D4D4' }}>)</span>
                </div>
                <div style={{ color: '#D4D4D4' }}>{'{'}</div>
                <div style={{ paddingLeft: '20px' }}>
                  <span style={{ color: '#C586C0' }}>return </span>
                  <span style={{ color: '#C586C0' }}>await </span>
                  <span style={{ color: '#9CDCFE' }}>_processor</span>
                  <span style={{ color: '#D4D4D4' }}>.</span>
                  <span style={{ color: '#DCDCAA' }}>Execute</span>
                  <span style={{ color: '#D4D4D4' }}>(</span>
                  <span style={{ color: '#9CDCFE' }}>amount</span>
                  <span style={{ color: '#D4D4D4' }}>);</span>
                </div>
                <div style={{ color: '#D4D4D4' }}>{'}'}</div>
              </div>
            </div>

            {/* Floating badges */}
            <div style={{
              position: 'absolute',
              top: '-20px',
              right: '-20px',
              padding: '10px 20px',
              background: 'linear-gradient(135deg, #3CD0E2, #419AC5)',
              borderRadius: '30px',
              color: 'white',
              fontWeight: 'bold',
              fontSize: '14px',
              boxShadow: '0 10px 30px rgba(60, 208, 226, 0.4)',
              animation: 'float 3s infinite ease-in-out'
            }}>
              AI-Ready Docs
            </div>

            <div style={{
              position: 'absolute',
              bottom: '-20px',
              left: '-20px',
              padding: '10px 20px',
              background: 'rgba(255, 255, 255, 0.1)',
              backdropFilter: 'blur(10px)',
              border: '1px solid rgba(60, 208, 226, 0.3)',
              borderRadius: '30px',
              color: 'white',
              fontWeight: 'bold',
              fontSize: '14px',
              animation: 'float 3s infinite ease-in-out reverse'
            }}>
              Zero Config
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};