export const CTASection = () => {
  return (
    <div style={{
      width: '100%',
      padding: '120px 0',
      background: 'linear-gradient(135deg, #1A2B3D 0%, #0A1628 100%)',
      textAlign: 'center'
    }}>
      <div style={{
        maxWidth: '800px',
        margin: '0 auto',
        padding: '0 40px'
      }}>
        <h2 style={{
          fontSize: 'clamp(36px, 5vw, 48px)',
          fontWeight: '900',
          marginBottom: '30px',
          color: 'white'
        }}>
          Ready to Ship Better Docs?
        </h2>
        <div style={{
          fontSize: '20px',
          color: '#B0C4DE',
          marginBottom: '40px',
          lineHeight: '1.6'
        }}>
          Start building beautiful docs in less than 5 minutes.
        </div>
        <div style={{
          display: 'flex',
          gap: '20px',
          justifyContent: 'center',
          flexWrap: 'wrap'
        }}>
          <a href="/quickstart" style={{
            padding: '18px 40px',
            background: 'linear-gradient(135deg, #3CD0E2, #419AC5)',
            color: 'white',
            borderRadius: '12px',
            textDecoration: 'none',
            fontWeight: 'bold',
            fontSize: '18px',
            boxShadow: '0 10px 40px rgba(60, 208, 226, 0.3)',
            transition: 'all 0.3s',
            display: 'inline-block'
          }}>
            Get Started Free
          </a>
        </div>

        {/* Quick install */}
        <div style={{
          marginTop: '60px',
          padding: '30px',
          background: 'rgba(60, 208, 226, 0.05)',
          border: '1px solid rgba(60, 208, 226, 0.2)',
          borderRadius: '12px'
        }}>
          <p style={{
            color: '#8B9DC3',
            marginBottom: '15px',
            fontSize: '14px',
            textTransform: 'uppercase',
            letterSpacing: '1px'
          }}>
            Quick Install
          </p>
          <code style={{
            display: 'block',
            padding: '15px',
            background: 'rgba(0, 0, 0, 0.3)',
            borderRadius: '8px',
            color: '#3CD0E2',
            fontSize: '16px',
            fontFamily: 'monospace'
          }}>
            dotnet tool install DotNetDocs.Tools --global
          </code>
        </div>
      </div>
    </div>
  );
};