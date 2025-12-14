export const ValueProposition = ({ minimal = false, minCardWidth = 360, gap = 40 }) => {
    const [animationsStarted, setAnimationsStarted] = React.useState(false);

    React.useEffect(() => {
        setAnimationsStarted(true);
    }, []);

    const values = [
        {
            icon: 'sidebar-flip',
            iconType: 'duotone',
            number: '01',
            title: 'Bring any docs into Visual Studio',
            description: 'The new .docsproj Projects + the DotNetDocs.Sdk bring your docs for Mintlify, DocFX, MkDocs, Jekyll, Hugo, and more right into your VS and VSCode solutions.',
            gradient: 'linear-gradient(135deg, #3CD0E2, #2BA8C7)'
        },
        {
            icon: 'file-code',
            iconType: 'duotone',
            number: '02',
            title: 'Add real-time API reference docs',
            description: 'Automatically transform your .NET XML Documentation Comments into beautiful, searchable API reference docs that stay in sync with every build.',
            gradient: 'linear-gradient(135deg, #419AC5, #2D7BA8)'
        },
        {
            icon: 'rocket-launch',
            iconType: 'duotone',
            number: '03',
            title: 'Build & deploy anywhere',
            description: 'Your docs, your way. Deploy to Mintlify, GitHub Pages, Netlify, Vercel, or any static hosting. Full MSBuild integration means your CI/CD pipeline is already ready.',
            gradient: 'linear-gradient(135deg, #3CD0E2, #419AC5)'
        }
    ];

    // Value prop cards (reused in both modes)
    const valueCards = (
        <div style={{
            display: 'grid',
            gridTemplateColumns: `repeat(auto-fit, minmax(${minCardWidth}px, 1fr))`,
            gap: `${gap}px`,
            marginBottom: minimal ? 0 : '80px'
        }}>
            {values.map((value, index) => (
                <div
                    key={index}
                    className="value-card"
                    style={{
                        position: 'relative',
                        background: 'linear-gradient(135deg, rgba(255, 255, 255, 0.03), rgba(255, 255, 255, 0.01))',
                        border: '2px solid rgba(60, 208, 226, 0.25)',
                        borderRadius: '28px',
                        padding: '50px 40px',
                        transition: 'all 0.5s cubic-bezier(0.4, 0, 0.2, 1)',
                        overflow: 'hidden',
                        boxShadow: '0 10px 40px rgba(0, 0, 0, 0.3), inset 0 1px 0 rgba(255, 255, 255, 0.05)'
                    }}
                >
                    {/* Card background (gradient + mesh + grid) - only in minimal mode */}
                    {minimal && (
                        <div className="card-background" style={{
                            position: 'absolute',
                            inset: 0,
                            borderRadius: '28px',
                            overflow: 'hidden',
                            zIndex: 0
                        }}>
                            {/* Linear gradient */}
                            <div style={{
                                position: 'absolute',
                                inset: 0,
                                background: 'linear-gradient(180deg, #050B12 0%, #0D1821 50%, #0A1520 100%)'
                            }} />

                            {/* Mesh gradient */}
                            <div style={{
                                position: 'absolute',
                                inset: 0,
                                opacity: 0.4,
                                background: `
                                    radial-gradient(ellipse 80% 50% at 50% -20%, rgba(60, 208, 226, 0.25), transparent),
                                    radial-gradient(ellipse 60% 50% at 10% 40%, rgba(65, 154, 197, 0.2), transparent),
                                    radial-gradient(ellipse 60% 50% at 90% 60%, rgba(60, 208, 226, 0.2), transparent),
                                    radial-gradient(ellipse 100% 100% at 50% 100%, rgba(65, 154, 197, 0.15), transparent)
                                `,
                                filter: 'blur(60px)',
                                animation: 'morphGradient 12s ease-in-out infinite'
                            }} />

                            {/* Animated grid */}
                            <div style={{
                                position: 'absolute',
                                inset: 0,
                                opacity: 0.03,
                                backgroundImage: `
                                    linear-gradient(#3CD0E2 1px, transparent 1px),
                                    linear-gradient(90deg, #3CD0E2 1px, transparent 1px)
                                `,
                                backgroundSize: '100px 100px',
                                animation: 'gridSlide 20s linear infinite'
                            }} />
                        </div>
                    )}

                    {/* Animated border gradient */}
                    <div className="card-border-gradient" style={{
                        position: 'absolute',
                        inset: 0,
                        borderRadius: '28px',
                        padding: '2px',
                        background: `linear-gradient(135deg, ${value.gradient.replace('linear-gradient(135deg, ', '').replace(')', '')}, transparent, transparent)`,
                        WebkitMask: 'linear-gradient(#fff 0 0) content-box, linear-gradient(#fff 0 0)',
                        WebkitMaskComposite: 'xor',
                        maskComposite: 'exclude',
                        opacity: 0,
                        transition: 'opacity 0.5s ease'
                    }} />

                    {/* Card background glow */}
                    <div className="card-glow" style={{
                        position: 'absolute',
                        top: '-50%',
                        left: '-50%',
                        width: '200%',
                        height: '200%',
                        background: value.gradient,
                        opacity: 0,
                        transition: 'opacity 0.5s ease',
                        borderRadius: '50%',
                        filter: 'blur(60px)'
                    }} />

                    {/* Number badge */}
                    <div style={{
                        position: 'absolute',
                        top: '30px',
                        right: '30px',
                        fontSize: '72px',
                        fontWeight: '900',
                        background: value.gradient,
                        WebkitBackgroundClip: 'text',
                        WebkitTextFillColor: 'transparent',
                        backgroundClip: 'text',
                        opacity: 0.1,
                        lineHeight: '1',
                        pointerEvents: 'none'
                    }}>
                        {value.number}
                    </div>

                    <div style={{ position: 'relative', zIndex: 2 }}>
                        {/* Icon */}
                        <div className="icon-container" style={{
                            marginBottom: minimal ? '20px' : '30px',
                            display: 'inline-flex',
                            padding: '24px',
                            background: `linear-gradient(135deg, rgba(60, 208, 226, 0.15), rgba(65, 154, 197, 0.1))`,
                            borderRadius: '20px',
                            border: '2px solid rgba(60, 208, 226, 0.3)',
                            position: 'relative',
                            boxShadow: '0 8px 32px rgba(60, 208, 226, 0.2), inset 0 1px 0 rgba(255, 255, 255, 0.1)',
                            transition: 'all 0.4s ease'
                        }}>
                            <Icon icon={value.icon} iconType={value.iconType} size={44} color="#3CD0E2" />
                        </div>

                        {/* Title */}
                        <h3 style={{
                            fontSize: '28px',
                            fontWeight: 'bold',
                            color: 'white',
                            margin: '0 0 16px 0',
                            lineHeight: '1.3'
                        }}>
                            {value.title}
                        </h3>

                        {/* Description */}
                        <p style={{
                            color: '#A0C8DD',
                            fontSize: '17px',
                            lineHeight: '1.7',
                            marginBottom: '0'
                        }}>
                            {value.description}
                        </p>
                    </div>
                </div>
            ))}
        </div>
    );

    // Minimal mode: just the cards
    if (minimal) {
        return (
            <>
                {valueCards}
                <style>{`
                    @keyframes morphGradient {
                        0%, 100% {
                            opacity: 0.4;
                            transform: scale(1) rotate(0deg);
                        }
                        50% {
                            opacity: 0.6;
                            transform: scale(1.1) rotate(5deg);
                        }
                    }

                    @keyframes gridSlide {
                        0% { background-position: 0 0; }
                        100% { background-position: 100px 100px; }
                    }

                    .value-card:hover {
                        transform: translateY(-12px) scale(1.02);
                        border-color: transparent !important;
                        box-shadow: 0 40px 80px rgba(60, 208, 226, 0.35), 0 0 60px rgba(60, 208, 226, 0.15);
                        background: linear-gradient(135deg, rgba(60, 208, 226, 0.08), rgba(65, 154, 197, 0.05)) !important;
                    }

                    .value-card:hover .card-glow {
                        opacity: 0.12 !important;
                    }

                    .value-card:hover .card-border-gradient {
                        opacity: 1 !important;
                    }

                    .value-card:hover .icon-container {
                        transform: scale(1.1) rotate(-5deg);
                        box-shadow: 0 12px 48px rgba(60, 208, 226, 0.4), inset 0 2px 0 rgba(255, 255, 255, 0.2) !important;
                        background: linear-gradient(135deg, rgba(60, 208, 226, 0.25), rgba(65, 154, 197, 0.2)) !important;
                        border-color: rgba(60, 208, 226, 0.6) !important;
                    }

                    .value-card:hover h3 {
                        background: linear-gradient(135deg, #3CD0E2, #419AC5);
                        -webkit-background-clip: text;
                        -webkit-text-fill-color: transparent;
                        background-clip: text;
                        transform: translateX(5px);
                    }

                    .value-card h3 {
                        transition: all 0.4s ease;
                    }
                `}</style>
            </>
        );
    }

    // Full marketing mode: background, header, cards, footer
    return (
        <div style={{
            width: '100%',
            padding: '140px 0',
            background: 'linear-gradient(180deg, #050B12 0%, #0D1821 50%, #0A1520 100%)',
            position: 'relative',
            overflow: 'hidden',
            opacity: animationsStarted ? 1 : 0,
            transition: 'opacity 0.5s ease-out'
        }}>
            {/* Dramatic mesh gradient background */}
            <div style={{
                position: 'absolute',
                inset: 0,
                opacity: 0.4,
                background: `
                    radial-gradient(ellipse 80% 50% at 50% -20%, rgba(60, 208, 226, 0.25), transparent),
                    radial-gradient(ellipse 60% 50% at 10% 40%, rgba(65, 154, 197, 0.2), transparent),
                    radial-gradient(ellipse 60% 50% at 90% 60%, rgba(60, 208, 226, 0.2), transparent),
                    radial-gradient(ellipse 100% 100% at 50% 100%, rgba(65, 154, 197, 0.15), transparent)
                `,
                filter: 'blur(60px)',
                animation: 'morphGradient 12s ease-in-out infinite'
            }} />

            {/* Animated grid pattern */}
            <div style={{
                position: 'absolute',
                inset: 0,
                opacity: 0.03,
                backgroundImage: `
                    linear-gradient(#3CD0E2 1px, transparent 1px),
                    linear-gradient(90deg, #3CD0E2 1px, transparent 1px)
                `,
                backgroundSize: '100px 100px',
                animation: 'gridSlide 20s linear infinite'
            }} />

            {/* Floating orbs */}
            <div className="orb orb-1" style={{
                position: 'absolute',
                top: '10%',
                left: '5%',
                width: '400px',
                height: '400px',
                background: 'radial-gradient(circle, rgba(60, 208, 226, 0.15), transparent 70%)',
                borderRadius: '50%',
                filter: 'blur(80px)',
                animation: 'floatOrb1 25s ease-in-out infinite'
            }} />
            <div className="orb orb-2" style={{
                position: 'absolute',
                bottom: '10%',
                right: '10%',
                width: '500px',
                height: '500px',
                background: 'radial-gradient(circle, rgba(65, 154, 197, 0.2), transparent 70%)',
                borderRadius: '50%',
                filter: 'blur(90px)',
                animation: 'floatOrb2 30s ease-in-out infinite'
            }} />

            <div style={{
                maxWidth: '1400px',
                margin: '0 auto',
                padding: '0 40px',
                position: 'relative',
                zIndex: 10
            }}>
                {/* Header with dramatic typography */}
                <div style={{
                    textAlign: 'center',
                    marginBottom: '100px',
                    position: 'relative'
                }}>
                    <div style={{
                        display: 'inline-block',
                        padding: '8px 20px',
                        background: 'rgba(60, 208, 226, 0.1)',
                        border: '1px solid rgba(60, 208, 226, 0.3)',
                        borderRadius: '50px',
                        marginBottom: '30px',
                        fontSize: '14px',
                        fontWeight: 'bold',
                        color: '#3CD0E2',
                        letterSpacing: '1px',
                        textTransform: 'uppercase'
                    }}>
                        Why DotNetDocs
                    </div>

                    <h2 style={{
                        fontSize: 'clamp(42px, 6vw, 72px)',
                        fontWeight: '900',
                        marginBottom: '20px',
                        lineHeight: '1.1',
                        letterSpacing: '-0.02em'
                    }}>
                        <span style={{
                            display: 'block',
                            background: 'linear-gradient(135deg, #3CD0E2, #419AC5, #3CD0E2)',
                            backgroundSize: '200% auto',
                            WebkitBackgroundClip: 'text',
                            WebkitTextFillColor: 'transparent',
                            backgroundClip: 'text',
                            animation: 'shine 3s linear infinite'
                        }}>
                            Make Documentation a Joy
                        </span>
                        <span style={{
                            color: 'white',
                            fontSize: '0.7em',
                            fontWeight: '600',
                            display: 'block',
                            marginTop: '10px'
                        }}>
                            Not a Chore
                        </span>
                    </h2>

                </div>

                {/* Value propositions with stunning cards */}
                {valueCards}

                {/* Bottom tagline */}
                <div style={{
                    textAlign: 'center',
                    padding: '60px 40px',
                    background: 'rgba(60, 208, 226, 0.03)',
                    border: '2px solid rgba(60, 208, 226, 0.15)',
                    borderRadius: '20px',
                    position: 'relative',
                    overflow: 'hidden'
                }}>
                    <div style={{
                        position: 'absolute',
                        inset: 0,
                        background: 'linear-gradient(90deg, transparent, rgba(60, 208, 226, 0.05), transparent)',
                        animation: 'slideShine 3s ease-in-out infinite'
                    }} />

                    <h3 style={{
                        fontSize: 'clamp(24px, 4vw, 36px)',
                        fontWeight: '800',
                        background: 'linear-gradient(135deg, #3CD0E2, #419AC5)',
                        WebkitBackgroundClip: 'text',
                        WebkitTextFillColor: 'transparent',
                        backgroundClip: 'text',
                        marginBottom: '16px',
                        position: 'relative',
                        zIndex: 2
                    }}>
                        Give devs the experience they deserve.
                    </h3>
                    <p style={{
                        fontSize: '18px',
                        color: '#B0D4E8',
                        maxWidth: '700px',
                        margin: '0 auto',
                        lineHeight: '1.6',
                        position: 'relative',
                        zIndex: 2
                    }}>
                        No more context switching. No more outdated docs. Just seamless, integrated documentation that grows with your codebase.
                    </p>
                </div>
            </div>

            <style>{`
                @keyframes morphGradient {
                    0%, 100% {
                        opacity: 0.4;
                        transform: scale(1) rotate(0deg);
                    }
                    50% {
                        opacity: 0.6;
                        transform: scale(1.1) rotate(5deg);
                    }
                }

                @keyframes gridSlide {
                    0% { background-position: 0 0; }
                    100% { background-position: 100px 100px; }
                }

                @keyframes floatOrb1 {
                    0%, 100% { transform: translate(0, 0) scale(1); }
                    25% { transform: translate(30px, -20px) scale(1.05); }
                    50% { transform: translate(-20px, 30px) scale(0.95); }
                    75% { transform: translate(20px, 20px) scale(1.02); }
                }

                @keyframes floatOrb2 {
                    0%, 100% { transform: translate(0, 0) scale(1); }
                    33% { transform: translate(-40px, 20px) scale(1.08); }
                    66% { transform: translate(30px, -30px) scale(0.92); }
                }

                @keyframes shine {
                    to { background-position: 200% center; }
                }

                @keyframes slideShine {
                    0%, 100% { transform: translateX(-100%); }
                    50% { transform: translateX(100%); }
                }

                .value-card:hover {
                    transform: translateY(-12px) scale(1.02);
                    border-color: transparent !important;
                    box-shadow: 0 40px 80px rgba(60, 208, 226, 0.35), 0 0 60px rgba(60, 208, 226, 0.15);
                    background: linear-gradient(135deg, rgba(60, 208, 226, 0.08), rgba(65, 154, 197, 0.05)) !important;
                }

                .value-card:hover .card-glow {
                    opacity: 0.12 !important;
                }

                .value-card:hover .card-border-gradient {
                    opacity: 1 !important;
                }

                .value-card:hover .icon-container {
                    transform: scale(1.1) rotate(-5deg);
                    box-shadow: 0 12px 48px rgba(60, 208, 226, 0.4), inset 0 2px 0 rgba(255, 255, 255, 0.2) !important;
                    background: linear-gradient(135deg, rgba(60, 208, 226, 0.25), rgba(65, 154, 197, 0.2)) !important;
                    border-color: rgba(60, 208, 226, 0.6) !important;
                }

                .value-card:hover h3 {
                    background: linear-gradient(135deg, #3CD0E2, #419AC5);
                    -webkit-background-clip: text;
                    -webkit-text-fill-color: transparent;
                    background-clip: text;
                    transform: translateX(5px);
                }

                .value-card h3 {
                    transition: all 0.4s ease;
                }
            `}</style>
        </div>
    );
};
