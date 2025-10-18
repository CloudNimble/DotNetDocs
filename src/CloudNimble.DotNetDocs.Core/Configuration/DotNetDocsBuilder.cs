using System;
using CloudNimble.DotNetDocs.Core.Renderers;
using CloudNimble.DotNetDocs.Core.Transformers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CloudNimble.DotNetDocs.Core.Configuration
{

    /// <summary>
    /// Builder for configuring the DotNetDocs documentation pipeline.
    /// </summary>
    /// <remarks>
    /// Provides a fluent API for registering renderers, enrichers, transformers,
    /// and configuring the documentation pipeline context.
    /// </remarks>
    public class DotNetDocsBuilder
    {

        #region Fields

        internal readonly IServiceCollection _services;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DotNetDocsBuilder"/> class.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        public DotNetDocsBuilder(IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            _services = services;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a custom enricher to the pipeline.
        /// </summary>
        /// <typeparam name="TEnricher">The type of enricher to add.</typeparam>
        /// <returns>The builder for chaining.</returns>
        /// <remarks>
        /// Enrichers add conceptual content to documentation entities.
        /// </remarks>
        /// <example>
        /// <code>
        /// pipeline.AddEnricher&lt;ConceptualContentEnricher&gt;();
        /// </code>
        /// </example>
        public DotNetDocsBuilder AddEnricher<TEnricher>()
            where TEnricher : class, IDocEnricher
        {
            _services.TryAddEnumerable(ServiceDescriptor.Scoped<IDocEnricher, TEnricher>());
            return this;
        }

        /// <summary>
        /// Adds a custom renderer to the pipeline.
        /// </summary>
        /// <typeparam name="TRenderer">The type of renderer to add.</typeparam>
        /// <returns>The builder for chaining.</returns>
        /// <remarks>
        /// Renderers generate output in specific formats (e.g., Markdown, JSON, YAML).
        /// </remarks>
        /// <example>
        /// <code>
        /// pipeline.AddRenderer&lt;MyCustomRenderer&gt;();
        /// </code>
        /// </example>
        public DotNetDocsBuilder AddRenderer<TRenderer>() 
            where TRenderer : class, IDocRenderer
        {
            _services.TryAddEnumerable(ServiceDescriptor.Scoped<IDocRenderer, TRenderer>());
            return this;
        }

        /// <summary>
        /// Adds a custom transformer to the pipeline.
        /// </summary>
        /// <typeparam name="TTransformer">The type of transformer to add.</typeparam>
        /// <returns>The builder for chaining.</returns>
        /// <remarks>
        /// Transformers modify the documentation model before rendering.
        /// </remarks>
        /// <example>
        /// <code>
        /// pipeline.AddTransformer&lt;InheritDocTransformer&gt;();
        /// </code>
        /// </example>
        public DotNetDocsBuilder AddTransformer<TTransformer>()
            where TTransformer : class, IDocTransformer
        {
            _services.TryAddEnumerable(ServiceDescriptor.Scoped<IDocTransformer, TTransformer>());
            return this;
        }

        /// <summary>
        /// Builds the pipeline and ensures all required services are registered.
        /// </summary>
        /// <remarks>
        /// This method is called internally by the extension method and ensures
        /// that DocumentationManager and ProjectContext are registered if they
        /// haven't been already.
        /// </remarks>
        internal void Build()
        {
            // Ensure DocumentationManager is registered
            _services.TryAddScoped<DocumentationManager>();
            
            // Ensure ProjectContext is registered if not already configured
            _services.TryAddSingleton<ProjectContext>();
        }

        /// <summary>
        /// Configures the ProjectContext for the pipeline.
        /// </summary>
        /// <param name="configure">Action to configure the ProjectContext.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// pipeline.ConfigureContext(ctx =>
        /// {
        ///     ctx.OutputPath = "docs/api";
        ///     ctx.ShowPlaceholders = false;
        /// });
        /// </code>
        /// </example>
        public DotNetDocsBuilder ConfigureContext(Action<ProjectContext> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            
            // Register or replace the ProjectContext configuration
            _services.Replace(ServiceDescriptor.Singleton(sp =>
            {
                var context = new ProjectContext();
                configure(context);
                return context;
            }));
            
            return this;
        }

        /// <summary>
        /// Adds the JSON renderer to the pipeline.
        /// </summary>
        /// <param name="configure">Optional action to configure JsonRendererOptions.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// pipeline.UseJsonRenderer(options =>
        /// {
        ///     options.WriteIndented = true;
        ///     options.IncludeNullValues = false;
        /// });
        /// </code>
        /// </example>
        public DotNetDocsBuilder UseJsonRenderer(Action<JsonRendererOptions>? configure = null)
        {
            if (configure is not null)
            {
                _services.Configure(configure);
            }
            return AddRenderer<JsonRenderer>();
        }

        /// <summary>
        /// Adds the Markdown renderer to the pipeline.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        /// <remarks>
        /// Also registers MarkdownXmlTransformer to process XML documentation tags.
        /// </remarks>
        /// <example>
        /// <code>
        /// pipeline.UseMarkdownRenderer();
        /// </code>
        /// </example>
        public DotNetDocsBuilder UseMarkdownRenderer()
        {
            AddTransformer<MarkdownXmlTransformer>();
            return AddRenderer<MarkdownRenderer>();
        }

        /// <summary>
        /// Adds the YAML renderer to the pipeline.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// pipeline.UseYamlRenderer();
        /// </code>
        /// </example>
        public DotNetDocsBuilder UseYamlRenderer()
        {
            return AddRenderer<YamlRenderer>();
        }

        #endregion

    }

}