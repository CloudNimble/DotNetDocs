# Dependency Injection Extension Methods Implementation

## Overview
Create intuitive extension methods in `DotNetDocsCore_IServiceCollectionExtensions` to register DotNetDocs components with dependency injection containers. The goal is to make it super simple for consumers to integrate the documentation system into their applications.

## Key Components to Register

### Core Services
1. **DocumentationManager** - Orchestrates the documentation pipeline (manages AssemblyManager internally)
2. **ProjectContext** - Configuration container

### Pipeline Components (Interfaces)
1. **IDocRenderer** - Output format generators (Markdown, JSON, YAML)
2. **IDocEnricher** - Conceptual content enhancers
3. **IDocTransformer** - Documentation model transformers

### Concrete Implementations
1. **MarkdownRenderer**
2. **JsonRenderer** 
3. **YamlRenderer**

## Implementation Steps

### Step 1: Add Missing Using Statements
Add to `DotNetDocsCore_IServiceCollectionExtensions.cs`:
```csharp
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Core.Renderers;
using Microsoft.Extensions.Options;
```

### Step 2: Implement Basic All-in-One Registration
```csharp
/// <summary>
/// Adds DotNetDocs services with all built-in renderers to the service collection.
/// </summary>
/// <param name="services">The service collection.</param>
/// <param name="configureContext">Optional action to configure the ProjectContext.</param>
/// <returns>The service collection for chaining.</returns>
/// <example>
/// <code>
/// services.AddDotNetDocs(context =>
/// {
///     context.OutputPath = "docs/api";
///     context.ShowPlaceholders = false;
/// });
/// </code>
/// </example>
public static IServiceCollection AddDotNetDocs(this IServiceCollection services, 
    Action<ProjectContext>? configureContext = null)
{
    // Register core services
    services.TryAddSingleton<ProjectContext>(sp => 
    {
        var context = new ProjectContext();
        configureContext?.Invoke(context);
        return context;
    });
    
    // Register all built-in renderers
    services.TryAddScoped<IDocRenderer, MarkdownRenderer>();
    services.TryAddScoped<IDocRenderer, JsonRenderer>();
    services.TryAddScoped<IDocRenderer, YamlRenderer>();
    
    // Register DocumentationManager
    services.TryAddScoped<DocumentationManager>();
    
    return services;
}
```

### Step 3: Implement Core Services Only Registration
```csharp
/// <summary>
/// Adds only the core DotNetDocs services without any renderers.
/// </summary>
/// <param name="services">The service collection.</param>
/// <param name="configureContext">Optional action to configure the ProjectContext.</param>
/// <returns>The service collection for chaining.</returns>
/// <remarks>
/// Use this method when you want to manually register specific renderers.
/// </remarks>
public static IServiceCollection AddDotNetDocsCore(this IServiceCollection services,
    Action<ProjectContext>? configureContext = null)
{
    // Just the core services without renderers
    services.TryAddSingleton<ProjectContext>(sp => 
    {
        var context = new ProjectContext();
        configureContext?.Invoke(context);
        return context;
    });
    
    services.TryAddScoped<DocumentationManager>();
    
    return services;
}
```

### Step 4: Implement Individual Renderer Registration Methods
```csharp
/// <summary>
/// Adds the Markdown renderer to the service collection.
/// </summary>
/// <param name="services">The service collection.</param>
/// <returns>The service collection for chaining.</returns>
public static IServiceCollection AddMarkdownRenderer(this IServiceCollection services)
{
    services.TryAddScoped<IDocRenderer, MarkdownRenderer>();
    return services;
}

/// <summary>
/// Adds the JSON renderer to the service collection.
/// </summary>
/// <param name="services">The service collection.</param>
/// <param name="configureOptions">Optional action to configure JsonRendererOptions.</param>
/// <returns>The service collection for chaining.</returns>
public static IServiceCollection AddJsonRenderer(this IServiceCollection services, 
    Action<JsonRendererOptions>? configureOptions = null)
{
    if (configureOptions != null)
    {
        services.Configure<JsonRendererOptions>(configureOptions);
    }
    services.TryAddScoped<IDocRenderer, JsonRenderer>();
    return services;
}

/// <summary>
/// Adds the YAML renderer to the service collection.
/// </summary>
/// <param name="services">The service collection.</param>
/// <returns>The service collection for chaining.</returns>
public static IServiceCollection AddYamlRenderer(this IServiceCollection services)
{
    services.TryAddScoped<IDocRenderer, YamlRenderer>();
    return services;
}
```

### Step 5: Implement Generic Component Registration Methods
```csharp
/// <summary>
/// Adds a custom document renderer to the service collection.
/// </summary>
/// <typeparam name="TRenderer">The type of renderer to add.</typeparam>
/// <param name="services">The service collection.</param>
/// <returns>The service collection for chaining.</returns>
public static IServiceCollection AddDocRenderer<TRenderer>(this IServiceCollection services)
    where TRenderer : class, IDocRenderer
{
    services.TryAddScoped<IDocRenderer, TRenderer>();
    return services;
}

/// <summary>
/// Adds a custom document enricher to the service collection.
/// </summary>
/// <typeparam name="TEnricher">The type of enricher to add.</typeparam>
/// <param name="services">The service collection.</param>
/// <returns>The service collection for chaining.</returns>
public static IServiceCollection AddDocEnricher<TEnricher>(this IServiceCollection services)
    where TEnricher : class, IDocEnricher
{
    services.TryAddScoped<IDocEnricher, TEnricher>();
    return services;
}

/// <summary>
/// Adds a custom document transformer to the service collection.
/// </summary>
/// <typeparam name="TTransformer">The type of transformer to add.</typeparam>
/// <param name="services">The service collection.</param>
/// <returns>The service collection for chaining.</returns>
public static IServiceCollection AddDocTransformer<TTransformer>(this IServiceCollection services)
    where TTransformer : class, IDocTransformer
{
    services.TryAddScoped<IDocTransformer, TTransformer>();
    return services;
}
```

### Step 6: Create Pipeline Builder Class
Create a new file `DotNetDocsPipelineBuilder.cs` in the same namespace:
```csharp
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CloudNimble.DotNetDocs.Core.Renderers;

namespace CloudNimble.DotNetDocs.Core.Extensions
{
    /// <summary>
    /// Builder for configuring the DotNetDocs documentation pipeline.
    /// </summary>
    public class DotNetDocsPipelineBuilder
    {
        private readonly IServiceCollection _services;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DotNetDocsPipelineBuilder"/> class.
        /// </summary>
        /// <param name="services">The service collection.</param>
        public DotNetDocsPipelineBuilder(IServiceCollection services)
        {
            _services = services;
        }
        
        /// <summary>
        /// Adds a custom renderer to the pipeline.
        /// </summary>
        /// <typeparam name="TRenderer">The type of renderer to add.</typeparam>
        /// <returns>The builder for chaining.</returns>
        public DotNetDocsPipelineBuilder AddRenderer<TRenderer>() 
            where TRenderer : class, IDocRenderer
        {
            _services.TryAddScoped<IDocRenderer, TRenderer>();
            return this;
        }
        
        /// <summary>
        /// Adds a custom enricher to the pipeline.
        /// </summary>
        /// <typeparam name="TEnricher">The type of enricher to add.</typeparam>
        /// <returns>The builder for chaining.</returns>
        public DotNetDocsPipelineBuilder AddEnricher<TEnricher>()
            where TEnricher : class, IDocEnricher
        {
            _services.TryAddScoped<IDocEnricher, TEnricher>();
            return this;
        }
        
        /// <summary>
        /// Adds a custom transformer to the pipeline.
        /// </summary>
        /// <typeparam name="TTransformer">The type of transformer to add.</typeparam>
        /// <returns>The builder for chaining.</returns>
        public DotNetDocsPipelineBuilder AddTransformer<TTransformer>()
            where TTransformer : class, IDocTransformer
        {
            _services.TryAddScoped<IDocTransformer, TTransformer>();
            return this;
        }
        
        /// <summary>
        /// Configures the ProjectContext for the pipeline.
        /// </summary>
        /// <param name="configure">Action to configure the ProjectContext.</param>
        /// <returns>The builder for chaining.</returns>
        public DotNetDocsPipelineBuilder ConfigureContext(Action<ProjectContext> configure)
        {
            var context = new ProjectContext();
            configure(context);
            _services.TryAddSingleton(context);
            return this;
        }
        
        /// <summary>
        /// Adds the Markdown renderer to the pipeline.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        public DotNetDocsPipelineBuilder UseMarkdownRenderer()
        {
            return AddRenderer<MarkdownRenderer>();
        }
        
        /// <summary>
        /// Adds the JSON renderer to the pipeline.
        /// </summary>
        /// <param name="configure">Optional action to configure JsonRendererOptions.</param>
        /// <returns>The builder for chaining.</returns>
        public DotNetDocsPipelineBuilder UseJsonRenderer(Action<JsonRendererOptions>? configure = null)
        {
            if (configure != null)
            {
                _services.Configure<JsonRendererOptions>(configure);
            }
            return AddRenderer<JsonRenderer>();
        }
        
        /// <summary>
        /// Adds the YAML renderer to the pipeline.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        public DotNetDocsPipelineBuilder UseYamlRenderer()
        {
            return AddRenderer<YamlRenderer>();
        }
        
        /// <summary>
        /// Builds the pipeline and ensures all required services are registered.
        /// </summary>
        internal void Build()
        {
            // Ensure DocumentationManager is registered
            _services.TryAddScoped<DocumentationManager>();
            
            // Ensure ProjectContext is registered if not already configured
            _services.TryAddSingleton<ProjectContext>();
        }
    }
}
```

### Step 7: Add Pipeline Builder Extension Method
Add to `DotNetDocsCore_IServiceCollectionExtensions.cs`:
```csharp
/// <summary>
/// Adds DotNetDocs services using a fluent pipeline builder.
/// </summary>
/// <param name="services">The service collection.</param>
/// <param name="configurePipeline">Action to configure the documentation pipeline.</param>
/// <returns>The service collection for chaining.</returns>
/// <example>
/// <code>
/// services.AddDotNetDocsPipeline(pipeline =>
/// {
///     pipeline
///         .UseMarkdownRenderer()
///         .UseJsonRenderer()
///         .AddEnricher&lt;MyCustomEnricher&gt;()
///         .ConfigureContext(ctx => ctx.OutputPath = "docs");
/// });
/// </code>
/// </example>
public static IServiceCollection AddDotNetDocsPipeline(this IServiceCollection services,
    Action<DotNetDocsPipelineBuilder> configurePipeline)
{
    var builder = new DotNetDocsPipelineBuilder(services);
    configurePipeline(builder);
    builder.Build();
    return services;
}
```

### Step 8: Update DocumentationManager Constructor for DI
Ensure DocumentationManager can be properly injected by updating its constructor to accept IEnumerable from DI:
```csharp
public DocumentationManager(
    IEnumerable<IDocEnricher>? enrichers = null,
    IEnumerable<IDocTransformer>? transformers = null,
    IEnumerable<IDocRenderer>? renderers = null)
{
    this.enrichers = enrichers ?? [];
    this.transformers = transformers ?? [];
    this.renderers = renderers ?? [];
}
```

### Step 9: Update Renderer Constructors for DI
Ensure renderers can accept optional dependencies from DI:
- JsonRenderer should accept IOptions<JsonRendererOptions> optionally
- All renderers should accept ProjectContext from DI

## Testing Plan

### Unit Tests
1. Test that AddDotNetDocs registers all expected services
2. Test that AddDotNetDocsCore only registers core services
3. Test that individual renderer methods register correctly
4. Test that pipeline builder correctly chains operations
5. Test that TryAdd prevents duplicate registrations

### Integration Tests
1. Create a test host with DI container
2. Register services using extension methods
3. Resolve DocumentationManager and verify it works
4. Verify renderers are properly injected

## Service Lifetimes
- **ProjectContext**: Singleton (configuration that doesn't change)
- **DocumentationManager**: Scoped (may maintain state during processing)
- **IDocRenderer**: Scoped (may maintain state during rendering)
- **IDocEnricher**: Scoped (may maintain state during enrichment)
- **IDocTransformer**: Scoped (may maintain state during transformation)

## Notes
- AssemblyManager is NOT registered in DI as it's created and managed internally by DocumentationManager
- All methods use TryAdd to prevent duplicate registrations and allow overrides
- The builder pattern provides a fluent API for complex scenarios