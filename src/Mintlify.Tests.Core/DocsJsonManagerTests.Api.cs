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
                    "proxy": true,
                    "examples": {
                        "autogenerate": true,
                        "prefill": false,
                        "defaults": "required",
                        "languages": ["python", "curl"]
                    }
                }
                """);

            manager.Configuration!.Api!.Url.Should().Be("full");
            manager.Configuration.Api.Proxy.Should().BeTrue();
            manager.Configuration.Api.Examples!.Autogenerate.Should().BeTrue();
            manager.Configuration.Api.Examples.Prefill.Should().BeFalse();
            manager.Configuration.Api.Examples.Defaults.Should().Be("required");
            manager.Configuration.Api.Examples.Languages.Should().Contain("python");

            var reloaded = SaveAndReload(manager);
            reloaded.Api!.Examples!.Autogenerate.Should().BeTrue();
            reloaded.Api.Url.Should().Be("full");
        }

    }

}
