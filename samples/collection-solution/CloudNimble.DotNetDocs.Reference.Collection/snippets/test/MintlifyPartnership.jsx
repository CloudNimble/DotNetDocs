export const MintlifyPartnership = () => {
    const [animationsStarted, setAnimationsStarted] = React.useState(false);

    React.useEffect(() => {
        setAnimationsStarted(true);
    }, []);

    return (
        <div style={{
            width: '100%',
            padding: '120px 0',
            background: '#0a1628',
            position: 'relative',
            overflow: 'hidden',
            opacity: animationsStarted ? 1 : 0,
            transition: 'opacity 0.5s ease-out'
        }}>
            {/* Mintlify aurora background */}
            <div style={{
                position: 'absolute',
                top: '0',
                left: '0',
                right: '0',
                bottom: '0',
                backgroundImage: 'url(/images/mintlify-bg.svg)',
                backgroundSize: 'cover',
                backgroundPosition: 'center center',
                backgroundRepeat: 'no-repeat',
                opacity: 1,
                pointerEvents: 'none'
            }} />

            <div style={{
                maxWidth: '1400px',
                margin: '0 auto',
                padding: '0 40px',
                position: 'relative',
                zIndex: 10
            }}>
                {/* Partnership Badge */}
                <div style={{
                    textAlign: 'center',
                    marginBottom: '60px'
                }}>
                    <div style={{
                        display: 'inline-flex',
                        alignItems: 'center',
                        gap: '12px',
                        padding: '10px 20px',
                        background: 'rgba(14, 164, 114, 0.1)',
                        border: '1px solid rgba(14, 164, 114, 0.3)',
                        borderRadius: '100px',
                        color: '#0ea472',
                        fontWeight: '600',
                        fontSize: '22px',
                        letterSpacing: '0.3px',
                        boxShadow: '0 0 20px rgba(14, 164, 114, 0.15)',
                        fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif'
                    }}>
                        <Icon icon="handshake" iconType="solid" size={28} color="#0ea472" />
                        <span>Official Partner</span>
                    </div>
                </div>

                {/* Hero Title */}
                <div style={{
                    textAlign: 'center',
                    marginBottom: '80px'
                }}>
                    <h2 style={{
                        fontSize: 'clamp(40px, 6vw, 72px)',
                        fontWeight: '600',
                        color: 'white',
                        margin: '0 0 20px 0',
                        lineHeight: '1.1',
                        fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif'
                    }}>
                        Better with Mintlify
                    </h2>
                    <p style={{
                        fontSize: '20px',
                        lineHeight: '1.6',
                        color: 'rgba(255, 255, 255, 0.7)',
                        maxWidth: '800px',
                        margin: '0 auto',
                        fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif'
                    }}>
                        Why settle for static markdown when you can have interactive API docs, AI-powered search, custom components, and enterprise analytics?
                        Give your users the same experience as Coinbase, Vercel, Anthropic, and more.
                    </p>
                </div>

                {/* Main Content Grid */}
                <div style={{
                    display: 'grid',
                    gridTemplateColumns: 'repeat(auto-fit, minmax(min(100%, 400px), 1fr))',
                    gap: '60px',
                    alignItems: 'start',
                    marginBottom: '80px'
                }}>
                    {/* Left: Why Mintlify */}
                    <div style={{
                        background: 'rgba(10, 22, 40, 0.6)',
                        borderRadius: '24px',
                        padding: '48px',
                        boxShadow: '0 4px 24px rgba(0, 0, 0, 0.3)',
                        border: '1px solid rgba(255, 255, 255, 0.2)',
                        backdropFilter: 'blur(20px)',
                        display: 'flex',
                        flexDirection: 'column',
                        gap: '32px'
                    }}>
                        <div>
                            <div style={{
                                display: 'flex',
                                alignItems: 'center',
                                gap: '16px',
                                marginBottom: '16px'
                            }}>
                                <img
                                    src="/images/icons/mintlify.svg"
                                    alt="Mintlify"
                                    style={{
                                        width: '48px',
                                        height: '48px',
                                        filter: 'invert(31%) sepia(67%) saturate(3604%) hue-rotate(146deg) brightness(90%) contrast(91%)'
                                    }}
                                />
                                <h3 style={{
                                    fontSize: '28px',
                                    fontWeight: '600',
                                    color: 'white',
                                    margin: 0,
                                    fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif'
                                }}>
                                    Why Mintlify?
                                </h3>
                            </div>

                            <p style={{
                                fontSize: '16px',
                                lineHeight: '1.7',
                                color: 'rgba(255, 255, 255, 0.7)',
                                margin: 0,
                                fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif'
                            }}>
                                GitHub Pages, Jekyll, and Hugo can't do this.
                            </p>
                        </div>

                        <div style={{
                            display: 'flex',
                            flexDirection: 'column',
                            gap: '24px'
                        }}>
                            {[
                                { icon: 'robot', title: 'Built for People and AI', text: 'AI search, LLMS.txt, MCP support, and AI agents for writing' },
                                { icon: 'code', title: 'OpenAPI Integration', text: 'Interactive REST API docs with live SDK examples and testing' },
                                { icon: 'chart-line', title: 'Analytics & Insights', text: 'Track usage, search behavior, and user feedback' },
                                { icon: 'shield-check', title: 'Enterprise Compliance', text: 'SOC 2, GDPR, and ISO 27001 certified for security teams' },
                                { icon: 'lock', title: 'Granular Access Control', text: 'Secure access and provisioning with enterprise authentication' }
                            ].map((item, index) => (
                                <div key={index} style={{
                                    display: 'flex',
                                    alignItems: 'flex-start',
                                    gap: '16px'
                                }}>
                                    <div style={{
                                        width: '40px',
                                        height: '40px',
                                        borderRadius: '12px',
                                        background: 'rgba(14, 164, 114, 0.2)',
                                        border: '1px solid rgba(14, 164, 114, 0.3)',
                                        display: 'flex',
                                        alignItems: 'center',
                                        justifyContent: 'center',
                                        flexShrink: 0,
                                        marginTop: '2px'
                                    }}>
                                        <Icon icon={item.icon} iconType="solid" size={20} color="#0ea472" />
                                    </div>
                                    <div>
                                        <div style={{
                                            fontSize: '16px',
                                            fontWeight: '600',
                                            color: 'white',
                                            marginBottom: '4px',
                                            fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif'
                                        }}>
                                            {item.title}
                                        </div>
                                        <div style={{
                                            fontSize: '14px',
                                            color: 'rgba(255, 255, 255, 0.6)',
                                            lineHeight: '1.5',
                                            fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif'
                                        }}>
                                            {item.text}
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>

                    {/* Right: DotNetDocs + Mintlify */}
                    <div style={{
                        background: 'rgba(10, 22, 40, 0.6)',
                        borderRadius: '24px',
                        padding: '48px',
                        boxShadow: '0 4px 24px rgba(0, 0, 0, 0.3)',
                        border: '1px solid rgba(255, 255, 255, 0.2)',
                        backdropFilter: 'blur(20px)',
                        display: 'flex',
                        flexDirection: 'column',
                        gap: '32px'
                    }}>
                        <div>
                            <h3 style={{
                                fontSize: '28px',
                                fontWeight: '600',
                                color: 'white',
                                margin: '0 0 16px 0',
                                lineHeight: '1.4',
                                display: 'flex',
                                alignItems: 'flex-start',
                                gap: '16px',
                                flexWrap: 'wrap',
                                fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif'
                            }}>
                                <img
                                    src="/images/icons/favicon.svg"
                                    alt="DotNetDocs"
                                    style={{
                                        width: '48px',
                                        height: '48px',
                                        flexShrink: 0,
                                        display: 'inline-block',
                                        verticalAlign: 'middle'
                                    }}
                                />
                                <span style={{ flex: '1 1 180px', minWidth: '180px' }}>
                                    DotNetDocs + Mintlify = Awesome
                                </span>
                            </h3>

                            <p style={{
                                fontSize: '16px',
                                lineHeight: '1.7',
                                color: 'rgba(255, 255, 255, 0.7)',
                                margin: 0,
                                fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif'
                            }}>
                                The magical duo for modern .NET documentation.
                            </p>
                        </div>

                        <div style={{
                            display: 'flex',
                            flexDirection: 'column',
                            gap: '24px'
                        }}>
                            {[
                                { icon: 'file-code', title: 'MDX with Frontmatter', text: 'Auto-generated frontmatter w/ icons, tags, SEO metadata, & keywords' },
                                { icon: 'diagram-project', title: 'Rich Components', text: 'Custom React components, Mermaid diagrams, interactive elements' },
                                { icon: 'sitemap', title: 'Smart Navigation', text: 'Auto-generated docs.json w/ hierarchical namespace navigation' },
                                { icon: 'icons', title: 'Context-Aware Icons', text: 'FontAwesome icons for all object types' },
                                { icon: 'magnifying-glass', title: 'Enhanced Discoverability', text: 'SEO-optimized descriptions, keywords, and wide mode' },
                            ].map((item, index) => (
                                <div key={index} style={{
                                    display: 'flex',
                                    alignItems: 'flex-start',
                                    gap: '16px'
                                }}>
                                    <div style={{
                                        width: '40px',
                                        height: '40px',
                                        borderRadius: '12px',
                                        background: 'rgba(60, 208, 226, 0.2)',
                                        border: '1px solid rgba(60, 208, 226, 0.3)',
                                        display: 'flex',
                                        alignItems: 'center',
                                        justifyContent: 'center',
                                        flexShrink: 0,
                                        marginTop: '2px'
                                    }}>
                                        <Icon icon={item.icon} iconType="solid" size={20} color="#3CD0E2" />
                                    </div>
                                    <div>
                                        <div style={{
                                            fontSize: '16px',
                                            fontWeight: '600',
                                            color: 'white',
                                            marginBottom: '4px',
                                            fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif'
                                        }}>
                                            {item.title}
                                        </div>
                                        <div style={{
                                            fontSize: '14px',
                                            color: 'rgba(255, 255, 255, 0.6)',
                                            lineHeight: '1.5',
                                            fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif'
                                        }}>
                                            {item.text}
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                </div>

                {/* Logos */}
                <div style={{
                    textAlign: 'center',
                    marginBottom: '60px'
                }}>
                    <p style={{
                        fontSize: '14px',
                        color: 'rgba(255, 255, 255, 0.5)',
                        marginBottom: '24px',
                        fontWeight: '500',
                        letterSpacing: '0.5px',
                        textTransform: 'uppercase',
                        fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif'
                    }}>
                        Trusted by innovative teams
                    </p>
                    <div style={{
                        display: 'flex',
                        gap: '48px',
                        justifyContent: 'center',
                        alignItems: 'center',
                        flexWrap: 'wrap',
                        opacity: 0.5
                    }}>
                        {['Anthropic', 'Vercel', 'PayPal', 'Coinbase', 'LinkedIn', 'X'].map((company, index) => (
                            <div key={index} style={{
                                fontSize: '18px',
                                fontWeight: '600',
                                color: 'white',
                                fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif'
                            }}>
                                {company}
                            </div>
                        ))}
                    </div>
                </div>

                {/* CTA */}
                <div style={{
                    textAlign: 'center'
                }}>
                    <div style={{
                        display: 'flex',
                        gap: '16px',
                        justifyContent: 'center',
                        alignItems: 'center',
                        flexWrap: 'wrap'
                    }}>
                        <a href="/providers/mintlify" target="_blank" rel="noopener noreferrer" className="mintlify-cta-secondary" style={{
                            display: 'inline-flex',
                            alignItems: 'center',
                            gap: '8px',
                            padding: '14px 28px',
                            background: 'rgba(255, 255, 255, 0.05)',
                            border: '1px solid rgba(255, 255, 255, 0.2)',
                            color: 'white',
                            borderRadius: '100px',
                            textDecoration: 'none',
                            fontWeight: '600',
                            fontSize: '15px',
                            transition: 'all 0.2s',
                            fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
                            boxShadow: '0 2px 8px rgba(0, 0, 0, 0.2)'
                        }}>
                            Learn About Mintlify
                            <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                                <path d="M11 5L11 11M11 5L5 5M11 5L5 11" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                            </svg>
                        </a>
                        <a href="https://dashboard.mintlify.com/signup?ajs_aid=281bf9d4-cd11-4748-9f5e-03a8cdff7f26&_gl=1*lqwmpa*_gcl_au*MTUxMDgzOTg3NS4xNzU5NTYzNDg1&utm_source=dotnetdocs" target="_blank" rel="noopener noreferrer" className="mintlify-cta-primary" style={{
                            display: 'inline-flex',
                            alignItems: 'center',
                            gap: '8px',
                            padding: '14px 28px',
                            background: 'white',
                            color: '#0a1628',
                            borderRadius: '100px',
                            textDecoration: 'none',
                            fontWeight: '600',
                            fontSize: '15px',
                            transition: 'all 0.2s',
                            border: 'none',
                            fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
                            boxShadow: '0 4px 16px rgba(255, 255, 255, 0.1)'
                        }}>
                            Sign Up for Free
                            <svg width="16" height="16" viewBox="0 0 16 16" fill="none" style={{ marginLeft: '4px' }}>
                                <path d="M6 3L11 8L6 13" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                            </svg>
                        </a>
                    </div>
                </div>
            </div>

            <style>{`
                .mintlify-cta-primary:hover {
                    background: #0ea472 !important;
                    color: white !important;
                    transform: translateY(-2px);
                    box-shadow: 0 8px 24px rgba(14, 164, 114, 0.3) !important;
                }

                .mintlify-cta-secondary:hover {
                    background: rgba(255, 255, 255, 0.1) !important;
                    border-color: rgba(255, 255, 255, 0.3) !important;
                    transform: translateY(-2px);
                    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3) !important;
                }
            `}</style>
        </div>
    );
};
