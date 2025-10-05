export const FeatureShowcase = () => {
    const features = [
        {
            icon: 'sidebar-flip',
            iconType: 'duotone',
            title: 'First-Class Editing',
            description: 'New Docs Project brings documentation editing to VS & VSCode',
            color: '#3CD0E2',
            details: 'The new .docsproj files + the DotNetDocs.Sdk bring your docs for Mintlify, DocFX, MkDocs, Jekyll, Hugo, and more right into your VS and VSCode solutions.'
        },
        {
            icon: 'file-code',
            iconType: 'duotone',
            title: 'Automatic API Docs',
            description: 'Renders your .NET XML Documentation Comments in multiple formats',
            color: '#3CD0E2',
            details: 'Built-in support for both Mintlify and Standard Markdown, YAML, and JSON. Build your own renderers to support custom formats.'
        },
        {
            icon: 'layer-group',
            iconType: 'duotone',
            title: 'Integrated Conceptual Docs',
            description: 'Weave in generated & written docs without losing your work',
            color: '#3CD0E2',
            details: 'Content on how to use an API, best practices, and more do not belong in your code files. Expertly weave your API & Conceptual docs and regenerate without losing your hard work.'
        },
        {
            icon: 'palette',
            iconType: 'duotone',
            title: 'Beautiful by Default',
            description: 'Exceptional Mintlify.com support for best-in-class experiences',
            color: '#419AC5',
            details: 'Give your users the experience trusted by Vercel, Anthropic, X, and more, with built in LLMS.txt and MCP support to help them build solutions faster.'
        },
        {
            icon: 'gears',
            iconType: 'duotone',
            title: 'MSBuild Integration',
            description: 'Automatically updates your Docs every time you compile',
            color: '#3CD0E2',
            details: 'Your docs are always up-to-date, whether you compile in VS, with `dotnet build` or in your CI/CD pipeline.'
        },
        {
            icon: 'pipeline-valve',
            iconType: 'duotone',
            title: 'Pluggable Pipeline',
            description: 'Easily Generate, merge, enrich, transform, and render your docs',
            color: '#3CD0E2',
            details: 'DotNetDocs is the last documentation system you\'ll ever need. Our modern pipeline is designed for the future.'
        }
    ];

    return (
        <div style={{
            width: '100%',
            padding: '120px 0',
            background: 'linear-gradient(180deg, #0F1922 0%, #1A2B3D 100%)',
            position: 'relative',
            overflow: 'hidden'
        }}>
            {/* Background decoration */}
            <div style={{
                position: 'absolute',
                inset: 0,
                opacity: 0.05,
                backgroundImage: `radial-gradient(circle at 20% 50%, #3CD0E2 0%, transparent 50%),
                          radial-gradient(circle at 80% 50%, #419AC5 0%, transparent 50%)`
            }} />

            <div style={{
                maxWidth: '1400px',
                margin: '0 auto',
                padding: '0 40px',
                position: 'relative',
                zIndex: 10
            }}>
                {/* Header */}
                <div style={{ textAlign: 'center', marginBottom: '80px' }}>
                    <h2 style={{
                        fontSize: 'clamp(36px, 5vw, 56px)',
                        fontWeight: '900',
                        marginBottom: '20px',
                        background: 'linear-gradient(135deg, #3CD0E2, #419AC5)',
                        WebkitBackgroundClip: 'text',
                        WebkitTextFillColor: 'transparent',
                        backgroundClip: 'text'
                    }}>
                        Everything You Need
                    </h2>
                    <p style={{
                        fontSize: '20px',
                        color: '#B0D4E8',
                        maxWidth: '600px',
                        margin: '0 auto',
                        lineHeight: '1.6'
                    }}>
                        Powerful features that make documentation a joy, not a chore
                    </p>
                </div>

                {/* Features Grid */}
                <div style={{
                    display: 'grid',
                    gridTemplateColumns: 'repeat(auto-fit, minmax(350px, 1fr))',
                    gap: '30px',
                    marginBottom: '60px'
                }}>
                    {features.map((feature, index) => (
                        <div
                            key={index}
                            className="feature-card"
                            style={{
                                background: 'rgba(255, 255, 255, 0.02)',
                                border: '2px solid rgba(60, 208, 226, 0.1)',
                                borderRadius: '20px',
                                padding: '40px',
                                cursor: 'pointer',
                                transition: 'all 0.4s cubic-bezier(0.4, 0, 0.2, 1)'
                            }}
                        >
                            <div style={{
                                marginBottom: '20px',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'flex-start'
                            }}>
                                <Icon icon={feature.icon} iconType={feature.iconType} size={48} color={feature.color} />
                            </div>

                            <h3 style={{
                                fontSize: '24px',
                                fontWeight: 'bold',
                                color: 'white',
                                marginBottom: '10px'
                            }}>
                                {feature.title}
                            </h3>

                            <div style={{
                                color: '#A0C8DD',
                                fontSize: '16px',
                                lineHeight: '1.6',
                                marginBottom: '0'
                            }}>
                                {feature.description}
                            </div>

                            <div className="feature-details" style={{
                                color: '#B0C4DE',
                                fontSize: '14px',
                                lineHeight: '1.6',
                                marginTop: '20px',
                                paddingTop: '20px',
                                borderTop: '1px solid rgba(60, 208, 226, 0.1)',
                                maxHeight: '0',
                                overflow: 'hidden',
                                opacity: '0',
                                transition: 'all 0.3s ease-in-out'
                            }}>
                                {feature.details}
                            </div>
                        </div>
                    ))}
                </div>

                {/* CTA */}
                <div style={{
                    textAlign: 'center',
                    paddingTop: '40px'
                }}>
                    <a href="https://dotnetdocs.com/docs/features" className="explore-features-btn" style={{
                        display: 'inline-flex',
                        alignItems: 'center',
                        gap: '10px',
                        padding: '16px 32px',
                        background: 'transparent',
                        border: '2px solid #3CD0E2',
                        color: '#3CD0E2',
                        borderRadius: '10px',
                        textDecoration: 'none',
                        fontWeight: 'bold',
                        fontSize: '16px',
                        transition: 'all 0.3s'
                    }}>
                        Explore All Features
                        <svg width="16" height="16" viewBox="0 0 20 20" fill="currentColor">
                            <path d="M10.293 3.293a1 1 0 011.414 0l6 6a1 1 0 010 1.414l-6 6a1 1 0 01-1.414-1.414L14.586 11H3a1 1 0 110-2h11.586l-4.293-4.293a1 1 0 010-1.414z" />
                        </svg>
                    </a>
                </div>
            </div>

            <style>{`
        .feature-card:hover {
          background: linear-gradient(135deg, rgba(60, 208, 226, 0.1), rgba(65, 154, 197, 0.1)) !important;
          border-color: #3CD0E2 !important;
          transform: translateY(-5px) scale(1.02);
          box-shadow: 0 20px 40px rgba(60, 208, 226, 0.2);
        }

        .feature-card:hover h3 {
          color: #3CD0E2 !important;
        }

        .feature-card:hover .feature-details {
          max-height: 200px !important;
          opacity: 1 !important;
        }

        .explore-features-btn:hover {
          background: rgba(60, 208, 226, 0.1) !important;
          transform: translateX(5px);
        }
      `}</style>
        </div>
    );
};