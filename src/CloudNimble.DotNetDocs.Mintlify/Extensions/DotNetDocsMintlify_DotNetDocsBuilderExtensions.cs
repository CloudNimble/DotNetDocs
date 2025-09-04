using System;
using CloudNimble.DotNetDocs.Mintlify;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mintlify.Core;

namespace CloudNimble.DotNetDocs.Core.Configuration
{

    /// <summary>
    /// Extension methods for adding Mintlify support to the DotNetDocs pipeline builder.
    /// </summary>
    public static class DotNetDocsMintlify_DotNetDocsBuilderExtensions
    {

        #region Public Methods

        /// <summary>
        /// Adds the Mintlify renderer to the documentation pipeline.
        /// </summary>
        /// <param name="builder">The DotNetDocs pipeline builder.</param>
        /// <returns>The builder for chaining.</returns>
        /// <remarks>
        /// <para>This method registers:</para>
        /// <list type="bullet">
        /// <item><description>MintlifyRenderer for generating MDX documentation</description></item>
        /// <item><description>DocsJsonManager for navigation generation</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddDotNetDocsPipeline(pipeline =>
        /// {
        ///     pipeline
        ///         .UseMintlifyRenderer()
        ///         .ConfigureContext(ctx => ctx.OutputPath = "docs/api");
        /// });
        /// </code>
        /// </example>
        public static DotNetDocsBuilder UseMintlifyRenderer(this DotNetDocsBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            // Register DocsJsonManager for navigation generation
            builder._services.TryAddScoped<DocsJsonManager>();
            
            // Add the renderer using the builder's AddRenderer method
            return builder.AddRenderer<MintlifyRenderer>();
        }

        /// <summary>
        /// Adds the Mintlify renderer to the documentation pipeline with configuration options.
        /// </summary>
        /// <param name="builder">The DotNetDocs pipeline builder.</param>
        /// <param name="configureMintlify">Action to configure Mintlify options.</param>
        /// <returns>The builder for chaining.</returns>
        /// <remarks>
        /// <para>This method registers:</para>
        /// <list type="bullet">
        /// <item><description>MintlifyRenderer for generating MDX documentation</description></item>
        /// <item><description>DocsJsonManager for navigation generation</description></item>
        /// <item><description>MintlifyOptions configuration</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddDotNetDocsPipeline(pipeline =>
        /// {
        ///     pipeline
        ///         .UseMintlifyRenderer(options =>
        ///         {
        ///             options.GenerateDocsJson = true;
        ///             options.IncludeIcons = true;
        ///         })
        ///         .ConfigureContext(ctx => ctx.OutputPath = "docs/api");
        /// });
        /// </code>
        /// </example>
        public static DotNetDocsBuilder UseMintlifyRenderer(this DotNetDocsBuilder builder,
            Action<MintlifyRendererOptions> configureMintlify)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configureMintlify);

            // Configure options
            builder._services.Configure(configureMintlify);

            // Register DocsJsonManager for navigation generation
            builder._services.TryAddScoped<DocsJsonManager>();
            
            // Add the renderer using the builder's AddRenderer method
            return builder.AddRenderer<MintlifyRenderer>();
        }

        /// <summary>
        /// Adds a custom Mintlify renderer implementation to the documentation pipeline.
        /// </summary>
        /// <typeparam name="TRenderer">The type of Mintlify renderer to add.</typeparam>
        /// <param name="builder">The DotNetDocs pipeline builder.</param>
        /// <returns>The builder for chaining.</returns>
        /// <remarks>
        /// <para>The renderer must inherit from MintlifyRenderer.</para>
        /// <para>This method also registers DocsJsonManager for navigation generation.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddDotNetDocsPipeline(pipeline =>
        /// {
        ///     pipeline
        ///         .UseMintlifyRenderer&lt;CustomMintlifyRenderer&gt;()
        ///         .ConfigureContext(ctx => ctx.OutputPath = "docs");
        /// });
        /// </code>
        /// </example>
        public static DotNetDocsBuilder UseMintlifyRenderer<TRenderer>(this DotNetDocsBuilder builder)
            where TRenderer : MintlifyRenderer
        {
            ArgumentNullException.ThrowIfNull(builder);

            // Register DocsJsonManager for navigation generation
            builder._services.TryAddScoped<DocsJsonManager>();
            
            // Add the custom renderer using the builder's AddRenderer method
            return builder.AddRenderer<TRenderer>();
        }

        #endregion

    }

}