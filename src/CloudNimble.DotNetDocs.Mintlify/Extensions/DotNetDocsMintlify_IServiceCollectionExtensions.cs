using System;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Transformers;
using CloudNimble.DotNetDocs.Mintlify;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mintlify.Core;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Extension methods for registering Mintlify documentation services with dependency injection.
    /// </summary>
    public static class DotNetDocsMintlify_IServiceCollectionExtensions
    {

        #region Public Methods

        /// <summary>
        /// Adds the Mintlify renderer to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>This method registers:</para>
        /// <list type="bullet">
        /// <item><description>MintlifyRenderer as Scoped implementation of IDocRenderer</description></item>
        /// <item><description>DocsJsonManager as Scoped service for navigation generation</description></item>
        /// <item><description>DocsJsonValidator to ensure correct structures</description></item>
        /// <item><description>MarkdownXmlTransformer for processing XML documentation tags</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMintlifyRenderer();
        /// </code>
        /// </example>
        public static IServiceCollection AddMintlifyRenderer(this IServiceCollection services)
        {
            // Call the generic version with MintlifyRenderer as the type parameter
            return services.AddMintlifyRenderer<MintlifyRenderer>();
        }

        /// <summary>
        /// Adds Mintlify documentation services including the renderer and DocsJsonManager.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>This method registers:</para>
        /// <list type="bullet">
        /// <item><description>MintlifyRenderer as Scoped implementation of IDocRenderer</description></item>
        /// <item><description>DocsJsonManager as Scoped service for navigation generation</description></item>
        /// <item><description>DocsJsonValidator to ensure correct structures</description></item>
        /// <item><description>MarkdownXmlTransformer for processing XML documentation tags</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMintlifyServices();
        /// </code>
        /// </example>
        public static IServiceCollection AddMintlifyServices(this IServiceCollection services)
        {
            // Call the configuration overload with an empty configuration
            return services.AddMintlifyServices(_ => { });
        }

        /// <summary>
        /// Adds Mintlify documentation services with configuration options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureMintlify">Action to configure Mintlify options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>This method registers:</para>
        /// <list type="bullet">
        /// <item><description>MintlifyRenderer as Scoped implementation of IDocRenderer</description></item>
        /// <item><description>DocsJsonManager as Scoped service for navigation generation</description></item>
        /// <item><description>DocsJsonValidator to ensure correct structures</description></item>
        /// <item><description>MintlifyOptions configuration</description></item>
        /// <item><description>MarkdownXmlTransformer for processing XML documentation tags</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMintlifyServices(options =>
        /// {
        ///     options.GenerateDocsJson = true;
        ///     options.IncludeIcons = true;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddMintlifyServices(this IServiceCollection services,
            Action<MintlifyRendererOptions> configureMintlify)
        {
            ArgumentNullException.ThrowIfNull(configureMintlify);

            // Configure options
            services.Configure(configureMintlify);

            // Register services using the generic renderer method
            return services.AddMintlifyRenderer<MintlifyRenderer>();
        }

        /// <summary>
        /// Adds a custom Mintlify renderer implementation to the service collection.
        /// </summary>
        /// <typeparam name="TRenderer">The type of Mintlify renderer to add.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>Registers the custom renderer as Scoped implementation of IDocRenderer.</para>
        /// <para>The renderer must inherit from MintlifyRenderer.</para>
        /// <para>This method also registers:</para>
        /// <list type="bullet">
        /// <item><description>DocsJsonManager for manipulating docs.json files</description></item>
        /// <item><description>DocsJsonValidator to ensure correct structures</description></item>
        /// <item><description>MarkdownXmlTransformer for processing XML documentation tags</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMintlifyRenderer&lt;CustomMintlifyRenderer&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddMintlifyRenderer<TRenderer>(this IServiceCollection services)
            where TRenderer : MintlifyRenderer
        {
            // Register the renderer
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IDocRenderer, TRenderer>());

            // Register the MarkdownXmlTransformer for processing XML documentation tags
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IDocTransformer, MarkdownXmlTransformer>());

            // Register DocsJsonManager for manipulating docs.json files
            services.TryAddScoped<DocsJsonManager>();

            // Register DocsJsonValidator to ensure correct structures
            services.TryAddScoped<DocsJsonValidator>();

            return services;
        }

        #endregion

    }

}