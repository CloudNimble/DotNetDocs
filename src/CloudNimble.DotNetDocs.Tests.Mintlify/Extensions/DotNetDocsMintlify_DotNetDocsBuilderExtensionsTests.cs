using System;
using System.Linq;
using CloudNimble.DotNetDocs.Core;
using CloudNimble.DotNetDocs.Core.Configuration;
using CloudNimble.DotNetDocs.Core.Transformers;
using CloudNimble.DotNetDocs.Mintlify;
using CloudNimble.DotNetDocs.Mintlify.Transformers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mintlify.Core;

namespace CloudNimble.DotNetDocs.Tests.Mintlify.Extensions
{

    /// <summary>
    /// Tests that the <see cref="DotNetDocsBuilder"/> Mintlify methods register the same services as the <see cref="IServiceCollection"/> methods.
    /// </summary>
    [TestClass]
    public class DotNetDocsMintlify_DotNetDocsBuilderExtensionsTests
    {

        #region Test Methods

        [TestMethod]
        public void UseMintlifyRenderer_RegistersSameServicesAs_AddMintlifyRenderer()
        {
            var builderServices = new ServiceCollection().AddDotNetDocsPipeline(pipeline => pipeline.UseMintlifyRenderer());
            var directServices = new ServiceCollection().AddMintlifyRenderer();

            GetMintlifyRegistrations(builderServices).Should().BeEquivalentTo(GetMintlifyRegistrations(directServices));
        }

        [TestMethod]
        public void UseMintlifyRenderer_RegistersMintlifyTransformersAndReferenceHandler()
        {
            var registrations = GetMintlifyRegistrations(new ServiceCollection().AddDotNetDocsPipeline(pipeline => pipeline.UseMintlifyRenderer()));

            registrations.Should().Contain((typeof(IDocRenderer), typeof(MintlifyRenderer)));
            registrations.Should().Contain((typeof(IDocTransformer), typeof(MarkdownXmlTransformer)));
            registrations.Should().Contain((typeof(IDocTransformer), typeof(MintlifyMarkdownTransformer)),
                "it converts HTML comments to the JSX comments that MDX requires");
            registrations.Should().Contain((typeof(IDocReferenceHandler), typeof(MintlifyDocReferenceHandler)));
            registrations.Should().Contain((typeof(DocsJsonManager), typeof(DocsJsonManager)));
            registrations.Should().Contain((typeof(DocsJsonValidator), typeof(DocsJsonValidator)));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the registrations a Mintlify renderer brings: renderers, transformers, reference handlers, and docs.json services.
        /// </summary>
        private static (Type ServiceType, Type? ImplementationType)[] GetMintlifyRegistrations(IServiceCollection services) =>
        [
            .. services
                .Where(descriptor => descriptor.ServiceType == typeof(IDocRenderer)
                    || descriptor.ServiceType == typeof(IDocTransformer)
                    || descriptor.ServiceType == typeof(IDocReferenceHandler)
                    || descriptor.ServiceType == typeof(DocsJsonManager)
                    || descriptor.ServiceType == typeof(DocsJsonValidator))
                .Select(descriptor => (descriptor.ServiceType, descriptor.ImplementationType))
        ];

        #endregion

    }

}
