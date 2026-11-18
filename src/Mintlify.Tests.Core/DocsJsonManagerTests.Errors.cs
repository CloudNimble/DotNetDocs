using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mintlify.Tests.Core
{

    public partial class DocsJsonManagerTests
    {

        /// <summary>
        /// Tests that errors.404 title and description are loaded and survive save.
        /// </summary>
        [TestMethod]
        public void Load_Errors404_ParsesTitleAndDescription()
        {
            var manager = LoadDocument("""
                "errors": {
                    "404": {
                        "title": "Oops!",
                        "description": "Page not found",
                        "redirect": false
                    }
                }
                """);

            var notFound = manager.Configuration!.Errors!.NotFound;
            notFound.Should().NotBeNull();
            notFound!.Title.Should().Be("Oops!");
            notFound.Description.Should().Be("Page not found");
            notFound.Redirect.Should().BeFalse();

            var reloaded = SaveAndReload(manager);
            reloaded.Errors!.NotFound!.Title.Should().Be("Oops!");
        }

    }

}
