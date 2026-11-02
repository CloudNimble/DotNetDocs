using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        /// <summary>
        /// Tests that metadata.timestamp is loaded and survives save.
        /// </summary>
        [TestMethod]
        public void Load_MetadataTimestamp_ParsesAndRoundTrips()
        {
            var manager = LoadDocument("""
                "metadata": {
                    "timestamp": true
                }
                """);

            manager.Configuration!.Metadata!.Timestamp.Should().BeTrue();

            var reloaded = SaveAndReload(manager);
            reloaded.Metadata!.Timestamp.Should().BeTrue();
        }

    }

}
