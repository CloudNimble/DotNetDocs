using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using McMaster.Extensions.CommandLineUtils;

namespace CloudNimble.DotNetDocs.Tools.Commands
{

    /// <summary>
    /// Command-line tool for creating and adding a documentation project to a solution file.
    /// </summary>
    /// <remarks>
    /// This command creates a .docsproj file configured for Mintlify documentation and adds it to the
    /// specified solution file (.sln or .slnx). The project is automatically added to a "Docs" solution folder.
    /// For .slnx files, the command post-processes the XML to add Type="C#" attributes to .docsproj nodes.
    /// </remarks>
    [Command("add", Description = "Add documentation project to solution")]
    public class AddCommand
    {

        #region Properties

        /// <summary>
        /// Gets or sets the output directory for the generated documentation project.
        /// </summary>
        [Option("--output|-o", Description = "Output directory for the docs project")]
        public string? OutputDirectory { get; set; }

        /// <summary>
        /// Gets or sets the name of the documentation project.
        /// </summary>
        /// <remarks>If not specified, the project name defaults to the solution name.</remarks>
        [Option("--name", Description = "Name for the docs project (defaults to solution name)")]
        public string? ProjectName { get; set; }

        /// <summary>
        /// Gets or sets the path to the solution file (.sln or .slnx) to use.
        /// </summary>
        [Option("--solution|-s", Description = "Path to solution file (.sln or .slnx)")]
        public string? SolutionPath { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes the command to create and add a documentation project to the specified solution.
        /// </summary>
        /// <param name="app">The command-line application context used to access command-line arguments and configuration.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is 0 if the documentation project is
        /// created and added successfully; otherwise, 1.</returns>
        /// <remarks>
        /// If the solution file is not specified, the method attempts to locate one in the
        /// current directory. The documentation project is created in the specified output directory or, if not
        /// provided, in a default location based on the solution file. Any errors encountered during execution are
        /// reported to the console, and a nonzero exit code is returned.
        /// </remarks>
        public async Task<int> OnExecute(CommandLineApplication app)
        {
            try
            {
                // Find solution file if not specified
                string? solutionFile = SolutionPath ?? FindSolutionFile();
                if (string.IsNullOrEmpty(solutionFile))
                {
                    Console.WriteLine("❌ No solution file (.sln or .slnx) found in current directory");
                    return 1;
                }

                Console.WriteLine($"📁 Found solution: {solutionFile}");

                // Determine project name
                string solutionName = ProjectName ?? Path.GetFileNameWithoutExtension(solutionFile);
                string docsProjectName = $"{solutionName}.Docs";

                // Determine output directory
                string outputDir = OutputDirectory ?? Path.Combine(Path.GetDirectoryName(solutionFile)!, docsProjectName);

                Console.WriteLine($"📝 Creating docs project: {docsProjectName}");
                Console.WriteLine($"📂 Output directory: {outputDir}");

                // Create the docs project directory
                Directory.CreateDirectory(outputDir);

                // Create the .docsproj file
                string docsProjPath = Path.Combine(outputDir, $"{docsProjectName}.docsproj");
                await CreateDocsProjectFile(docsProjPath, solutionName);

                Console.WriteLine($"✅ Created {docsProjPath}");

                // Add to solution
                await AddProjectToSolution(solutionFile, docsProjPath, docsProjectName);

                Console.WriteLine($"✅ Added {docsProjectName} to solution");
                Console.WriteLine($"🎉 Documentation project setup complete!");

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error setting up documentation project: {ex.Message}");
                return 1;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Adds the specified documentation project to the solution file using the dotnet CLI.
        /// </summary>
        /// <param name="solutionPath">The path to the solution file (.sln or .slnx).</param>
        /// <param name="projectPath">The path to the project file to add.</param>
        /// <param name="projectName">The name of the project being added.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown if the project cannot be added to the solution.</exception>
        /// <remarks>
        /// For .slnx files, this method performs post-processing to add the Type="C#" attribute to .docsproj project nodes.
        /// All projects are added to the "Docs" solution folder.
        /// </remarks>
        internal async Task AddProjectToSolution(string solutionPath, string projectPath, string projectName)
        {
            var isSlnx = Path.GetExtension(solutionPath).Equals(".slnx", StringComparison.OrdinalIgnoreCase);

            // Use dotnet CLI to add the project to the solution
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"sln \"{solutionPath}\" add \"{projectPath}\" --solution-folder \"Docs\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            await process!.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception("Failed to add project to solution");
            }

            // Post-process .slnx files to add Type="C#" attribute
            if (isSlnx)
            {
                await AddTypeAttributeToSlnx(solutionPath);
            }
        }

        /// <summary>
        /// Post-processes a .slnx file to add Type="C#" attributes to .docsproj project nodes.
        /// </summary>
        /// <param name="solutionPath">The path to the .slnx solution file.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method loads the .slnx XML file, finds all Project elements with .docsproj paths,
        /// and adds the Type="C#" attribute if it doesn't already exist.
        /// </remarks>
        internal async Task AddTypeAttributeToSlnx(string solutionPath)
        {
            var doc = XDocument.Load(solutionPath);

            // Find all Project elements with .docsproj extension
            var docsProjNodes = doc.Descendants("Project")
                .Where(p => p.Attribute("Path")?.Value.EndsWith(".docsproj", StringComparison.OrdinalIgnoreCase) == true);

            foreach (var node in docsProjNodes)
            {
                // Add Type="C#" attribute if it doesn't exist
                if (node.Attribute("Type") is null)
                {
                    node.Add(new XAttribute("Type", "C#"));
                }
            }

            await using var stream = File.Create(solutionPath);
            await doc.SaveAsync(stream, SaveOptions.None, default);
        }

        /// <summary>
        /// Creates a documentation project file (.docsproj) with default configuration for Mintlify documentation.
        /// </summary>
        /// <param name="filePath">The path where the .docsproj file should be created.</param>
        /// <param name="solutionName">The name of the solution, used in the Mintlify template configuration.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <remarks>
        /// The created project file includes:
        /// - DotNetDocs.Sdk reference
        /// - Mintlify documentation type configuration
        /// - Namespace mode set to Folder
        /// - Default Mintlify theme and color scheme
        /// </remarks>
        internal async Task CreateDocsProjectFile(string filePath, string solutionName)
        {
            string content = $@"<Project Sdk=""DotNetDocs.Sdk/1.0.0-preview.24"">

	<PropertyGroup>
		<KeepLocalOutput>true</KeepLocalOutput>
		<DocumentationType>Mintlify</DocumentationType>
		<GenerateDocumentation>true</GenerateDocumentation>
		<NamespaceMode>Folder</NamespaceMode>
		<ShowDocumentationStats>true</ShowDocumentationStats>

		<ConceptualDocsEnabled>false</ConceptualDocsEnabled>
		<ShowPlaceholders>false</ShowPlaceholders>

		<MintlifyNavigationMode>Unified</MintlifyNavigationMode>
		<MintlifyTemplate>
			<Name>{solutionName}</Name>
			<Theme>maple</Theme>
			<Colors>
				<Primary>#419AC5</Primary>
				<Light>#419AC5</Light>
				<Dark>#3CD0E2</Dark>
			</Colors>
		</MintlifyTemplate>
	</PropertyGroup>

</Project>";

            await File.WriteAllTextAsync(filePath, content);
        }

        /// <summary>
        /// Searches the current directory for a solution file, preferring .slnx files over .sln files.
        /// </summary>
        /// <returns>The path to the first solution file found, or null if no solution file exists.</returns>
        internal string? FindSolutionFile()
        {
            var currentDir = Directory.GetCurrentDirectory();

            // Look for .slnx first (preferred), then .sln
            var slnxFiles = Directory.GetFiles(currentDir, "*.slnx");
            if (slnxFiles.Length > 0)
            {
                return slnxFiles[0];
            }

            var slnFiles = Directory.GetFiles(currentDir, "*.sln");
            if (slnFiles.Length > 0)
            {
                return slnFiles[0];
            }

            return null;
        }

        #endregion

    }

}