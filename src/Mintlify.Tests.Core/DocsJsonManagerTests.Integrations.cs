using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        /// <summary>
        /// Tests that Mixpanel region, Segment cdnUrl, OneTrust, and Transcend are loaded and survive save.
        /// </summary>
        [TestMethod]
        public void Load_Integrations_ParsesNewProvidersAndFields()
        {
            var manager = LoadDocument("""
                "integrations": {
                    "mixpanel": {
                        "projectToken": "token",
                        "region": "eu"
                    },
                    "segment": {
                        "key": "write-key",
                        "cdnUrl": "https://cdn.example.com"
                    },
                    "onetrust": {
                        "domainScript": "abc-123",
                        "categoryId": "C0002"
                    },
                    "transcend": {
                        "bundleId": "bundle_1",
                        "analyticsPurpose": "Product Analytics"
                    },
                    "posthog": {
                        "apiKey": "phc_test123",
                        "sessionRecording": false
                    }
                }
                """);

            var integrations = manager.Configuration!.Integrations;
            integrations.Should().NotBeNull();
            integrations!.Mixpanel!.Region.Should().Be("eu");
            integrations.Segment!.CdnUrl.Should().Be("https://cdn.example.com");
            integrations.OneTrust!.DomainScript.Should().Be("abc-123");
            integrations.Transcend!.BundleId.Should().Be("bundle_1");
            integrations.Transcend.AnalyticsPurpose.Should().Be("Product Analytics");
            integrations.PostHog!.SessionRecording.Should().BeFalse();

            var reloaded = SaveAndReload(manager);
            reloaded.Integrations!.Mixpanel!.Region.Should().Be("eu");
            reloaded.Integrations.Transcend!.BundleId.Should().Be("bundle_1");
            reloaded.Integrations.PostHog!.SessionRecording.Should().BeFalse();
        }

    }

}
