using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        /// <summary>
        /// Tests that api.examples.autogenerate and api.url are loaded and survive save.
        /// </summary>
        [TestMethod]
        public void Load_Api_ParsesExamplesAndUrl()
        {
            var manager = LoadDocument("""
                "api": {
                    "url": "full",
                    "examples": {
                        "autogenerate": true,
                        "prefill": false,
                        "defaults": "required",
                        "languages": ["python", "curl"]
                    }
                }
                """);

            manager.Configuration!.Api!.Url.Should().Be("full");
            manager.Configuration.Api.Examples!.Autogenerate.Should().BeTrue();
            manager.Configuration.Api.Examples.Prefill.Should().BeFalse();
            manager.Configuration.Api.Examples.Defaults.Should().Be("required");
            manager.Configuration.Api.Examples.Languages.Should().Contain("python");

            var reloaded = SaveAndReload(manager);
            reloaded.Api!.Examples!.Autogenerate.Should().BeTrue();
            reloaded.Api.Url.Should().Be("full");
        }

        /// <summary>
        /// Tests that api.params.expanded, optional, overlays, and playground settings load and save.
        /// </summary>
        [TestMethod]
        public void Load_Api_ParsesExpandedOptionalOverlaysAndPlayground()
        {
            var manager = LoadDocument("""
                "api": {
                    "optional": "collapsed",
                    "params": {
                        "expanded": "closed",
                        "post": ["payload"]
                    },
                    "playground": {
                        "display": "auth",
                        "credentials": true
                    },
                    "openapi": {
                        "source": "openapi.yaml",
                        "directory": "api",
                        "overlays": ["overlay.json"]
                    }
                }
                """);

            manager.ConfigurationErrors.Should().NotContain(e => e.ErrorText.Contains("API"));
            manager.Configuration!.Api!.Optional.Should().Be("collapsed");
            manager.Configuration.Api.Params!.Expanded.Should().Be("closed");
            manager.Configuration.Api.Params.Post.Should().Equal("payload");
            manager.Configuration.Api.Playground!.Display.Should().Be("auth");
            manager.Configuration.Api.Playground.Credentials.Should().BeTrue();
            manager.Configuration.Api.OpenApi!.Source.Should().Be("openapi.yaml");
            manager.Configuration.Api.OpenApi.Directory.Should().Be("api");
            manager.Configuration.Api.OpenApi.Overlays.Should().Equal("overlay.json");

            var reloaded = SaveAndReload(manager);
            reloaded.Api!.Params!.Expanded.Should().Be("closed");
            reloaded.Api.OpenApi!.Overlays.Should().Equal("overlay.json");
            reloaded.Api.Playground!.Display.Should().Be("auth");
        }

        /// <summary>
        /// Tests that boolean-like expanded strings are mapped to the schema enum.
        /// </summary>
        [TestMethod]
        public void Load_Api_MapsBooleanLikeExpandedStrings()
        {
            var manager = LoadDocument("""
                "api": {
                    "params": {
                        "expanded": "true"
                    }
                }
                """);

            manager.Configuration!.Api!.Params!.Expanded.Should().Be("all");
        }

        /// <summary>
        /// Tests that an invalid params.expanded value is reported when loading.
        /// </summary>
        [TestMethod]
        public void Load_Api_InvalidExpanded_AddsValidationWarning()
        {
            var manager = LoadDocument("""
                "api": {
                    "params": {
                        "expanded": "maybe"
                    }
                }
                """);

            manager.ConfigurationErrors.Should().Contain(e => e.ErrorText.Contains("expanded"));
        }

    }

}
