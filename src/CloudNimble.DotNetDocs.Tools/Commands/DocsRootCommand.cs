using McMaster.Extensions.CommandLineUtils;

namespace CloudNimble.DotNetDocs.Tools.Commands
{

    /// <summary>
    /// Represents the root command for the DotNetDocs command-line interface (CLI) application.
    /// </summary>
    /// <remarks>This command serves as the entry point for the DotNetDocs CLI and provides access to
    /// subcommands such as build and add. When invoked without a subcommand or with insufficient arguments, it displays
    /// help information describing available commands and usage.</remarks>
    [Command(Name="dotnet docs", Description = "DotNetDocs 1.0 CLI\nhttps://dotnetdocs.com\n\nMade with ❤️ by CloudNimble.\nhttps://github.com/CloudNimble")]
    [Subcommand(typeof(BuildCommand), typeof(AddCommand))]
    public class DocsRootCommand
    {

        /// <summary>
        /// Displays the help information for the specified command-line application.
        /// </summary>
        /// <param name="app">The command-line application for which to display help information. Cannot be null.</param>
        /// <returns>Always returns 0 after displaying the help information.</returns>
        public int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 1;
        }

    }

}