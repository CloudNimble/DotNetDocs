using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        /// <summary>
        /// Tests that contextual options and display are loaded and survive save.
        /// </summary>
        [TestMethod]
        public void Load_Contextual_ParsesOptionsAndDisplay()
        {
            var manager = LoadDocument("""
                "contextual": {
                    "options": ["copy", "claude"],
                    "display": "toc"
                }
                """);

            manager.Configuration!.Contextual!.Display.Should().Be("toc");
            manager.Configuration.Contextual.Options.Should().Contain("copy");
            manager.Configuration.Contextual.Options.Should().Contain("claude");

            var reloaded = SaveAndReload(manager);
            reloaded.Contextual!.Display.Should().Be("toc");
        }

    }

}
