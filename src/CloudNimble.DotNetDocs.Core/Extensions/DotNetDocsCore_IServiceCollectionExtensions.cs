using System;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Core.Renderers;
using CloudNimble.DotNetDocs.Core.Transformers;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Extension methods for registering DotNetDocs services with dependency injection.
    /// </summary>
    public static class DotNetDocsCore_IServiceCollectionExtensions
    {

        #region Public Methods

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
        /// <remarks>
        /// This method registers:
        /// - ProjectContext as Singleton
        /// - DocumentationManager as Scoped
        /// - All built-in renderers (Markdown, JSON, YAML) as Scoped
        /// - MarkdownXmlTransformer for processing XML documentation tags
        /// </remarks>
        public static IServiceCollection AddDotNetDocs(this IServiceCollection services,
            Action<ProjectContext>? configureContext = null)
        {
            // Register core services
            services.TryAddSingleton(sp =>
            {
                var context = new ProjectContext();
                configureContext?.Invoke(context);
                return context;
            });

            // Register all built-in renderers (only if not already registered)
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IDocRenderer, MarkdownRenderer>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IDocRenderer, JsonRenderer>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IDocRenderer, YamlRenderer>());

            // Register the MarkdownXmlTransformer for processing XML documentation tags
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IDocTransformer, MarkdownXmlTransformer>());

            // Register DocumentationManager
            services.TryAddScoped<DocumentationManager>();

            return services;
        }

        /// <summary>
        /// Adds only the core DotNetDocs services without any renderers.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureContext">Optional action to configure the ProjectContext.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// Use this method when you want to manually register specific renderers.
        /// This registers:
        /// - ProjectContext as Singleton
        /// - DocumentationManager as Scoped
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddDotNetDocsCore(context =>
        /// {
        ///     context.OutputPath = "docs";
        /// });
        /// services.AddMarkdownRenderer();
        /// </code>
        /// </example>
        public static IServiceCollection AddDotNetDocsCore(this IServiceCollection services,
            Action<ProjectContext>? configureContext = null)
        {
            // Just the core services without renderers
            services.TryAddSingleton(sp => 
            {
                var context = new ProjectContext();
                configureContext?.Invoke(context);
                return context;
            });
            
            services.TryAddScoped<DocumentationManager>();
            
            return services;
        }

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
        ///         .UseJsonRenderer(options => options.WriteIndented = true)
        ///         .AddEnricher&lt;MyCustomEnricher&gt;()
        ///         .ConfigureContext(ctx => ctx.OutputPath = "docs");
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddDotNetDocsPipeline(this IServiceCollection services,
            Action<DotNetDocsBuilder> configurePipeline)
        {
            ArgumentNullException.ThrowIfNull(configurePipeline);
            
            var builder = new DotNetDocsBuilder(services);
            configurePipeline(builder);
            builder.Build();
            return services;
        }

        /// <summary>
        /// Adds the Markdown renderer to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// Registers MarkdownRenderer as Scoped implementation of IDocRenderer.
        /// Also registers MarkdownXmlTransformer to process XML documentation tags.
        /// </remarks>
        public static IServiceCollection AddMarkdownRenderer(this IServiceCollection services)
        {
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IDocRenderer, MarkdownRenderer>());
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IDocTransformer, MarkdownXmlTransformer>());
            return services;
        }

        /// <summary>
        /// Adds the JSON renderer to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure JsonRendererOptions.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// Registers JsonRenderer as Scoped implementation of IDocRenderer.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddJsonRenderer(options =>
        /// {
        ///     options.WriteIndented = true;
        ///     options.IncludeNullValues = false;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddJsonRenderer(this IServiceCollection services, 
            Action<JsonRendererOptions>? configureOptions = null)
        {
            if (configureOptions is not null)
            {
                services.Configure(configureOptions);
            }
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IDocRenderer, JsonRenderer>());
            return services;
        }

        /// <summary>
        /// Adds the YAML renderer to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// Registers YamlRenderer as Scoped implementation of IDocRenderer.
        /// </remarks>
        public static IServiceCollection AddYamlRenderer(this IServiceCollection services)
        {
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IDocRenderer, YamlRenderer>());
            return services;
        }

        /// <summary>
        /// Adds a custom document renderer to the service collection.
        /// </summary>
        /// <typeparam name="TRenderer">The type of renderer to add.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// Registers the renderer as Scoped implementation of IDocRenderer.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddDocRenderer&lt;MyCustomRenderer&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddDocRenderer<TRenderer>(this IServiceCollection services)
            where TRenderer : class, IDocRenderer
        {
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IDocRenderer, TRenderer>());
            return services;
        }

        /// <summary>
        /// Adds a custom document enricher to the service collection.
        /// </summary>
        /// <typeparam name="TEnricher">The type of enricher to add.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// Registers the enricher as Scoped implementation of IDocEnricher.
        /// Enrichers add conceptual content to documentation entities.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddDocEnricher&lt;ConceptualContentEnricher&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddDocEnricher<TEnricher>(this IServiceCollection services)
            where TEnricher : class, IDocEnricher
        {
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IDocEnricher, TEnricher>());
            return services;
        }

        /// <summary>
        /// Adds a custom document transformer to the service collection.
        /// </summary>
        /// <typeparam name="TTransformer">The type of transformer to add.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// Registers the transformer as Scoped implementation of IDocTransformer.
        /// Transformers modify the documentation model before rendering.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddDocTransformer&lt;InheritDocTransformer&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddDocTransformer<TTransformer>(this IServiceCollection services)
            where TTransformer : class, IDocTransformer
        {
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IDocTransformer, TTransformer>());
            return services;
        }

        #endregion

    }

}