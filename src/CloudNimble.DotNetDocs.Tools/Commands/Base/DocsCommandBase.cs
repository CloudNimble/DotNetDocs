using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Tools.Models;
using Pastel;

namespace CloudNimble.DotNetDocs.Tools.Commands.Base
{

    /// <summary>
    /// Base class for all DotNetDocs CLI commands, providing shared functionality like header display.
    /// </summary>
    public partial class DocsCommandBase
    {

        #region Public Methods

        /// <summary>
        /// Writes the DotNetDocs CLI header to the console with colorful ASCII art and version information.
        /// </summary>
        /// <remarks>
        /// This method displays a multi-line ASCII art logo combining the DotNetDocs branding with
        /// version and attribution information. The output uses console colors to create an eye-catching
        /// header for CLI operations.
        /// </remarks>
        public static void WriteHeader()
        {
            // Set console encoding to UTF-8 to support Unicode characters like ❤️ and ≠
            Console.OutputEncoding = Encoding.UTF8;

            // Define colors from the SVG
            Color lightBlue = Color.FromArgb(60, 208, 226); // Lighter cyan for top and "docs"
            Color darkBlue = Color.FromArgb(65, 154, 197);  // Darker blue for bottom
            Color white = Color.FromArgb(240, 240, 240);    // For "dotnet" - light gray
            Color gray = Color.FromArgb(128, 128, 128);     // For attribution

            // Text ASCII art lines for "dotnetdocs"
            string[] textLines =
            [
                "      ≠≠≠≠≠≠≠   ≠≠≠≠≠≠≠≠≠                                                                                                                      ",
                "     ≠≠≠≠≠≠≠≠   ≠≠≠≠≠≠≠≠≠≠≠                  ≈≈≈                                                            ≠≠≠                                ",
                "    ≠≠≠≠≠≠≠≠   ≠≠≠≠≠≠≠≠≠≠≠≠                  ≈≈≈                ≈≈                            ≈≈≈           ≠≠≠                                ",
                "    ≠≠≠≠≠     ≠≠≠≠≠=  ≠≠≠≠≠                  ≈≈≈               ≈≈≈                           ≈≈≈≈           ≠≠≠                                ",
                "≠≠≠≠≠≠≠≠≠     ≠≠≠≠≠    ≠≠≠≠≠≠≠≠        ≈≈≈≈≈≈≈≈≈    ≈≈≈≈≈≈≈≈  ≈≈≈≈≈≈≈ ≈≈≈≈≈≈≈≈≈    ≈≈≈≈≈≈≈  ≈≈≈≈≈≈≈  =≠≠≠≠≠≠≠≠≠   ≠≠≠≠≠≠≠≠    ≠≠≠≠≠≠≠≠ ≠≠≠≠≠≠≠ ",
                "≠≠≠≠≠≠≠      ≠≠≠≠≠      ≠≠≠≠≠≠≠≠      ≈≈≈   ∞≈≈≈  ≈≈≈    ≈≈≈≈  ≈≈≈≈   ≈≈≈≈   ≈≈≈  ≈≈≈   ≈≈≈  ≈≈≈≈   ≠≠≠≠   ≠≠≠≠  ≠≠≠    ≠≠≠  ≠≠≠≠     ≠≠≠      ",
                "=======      =====      ========     ≈≈≈     ≈≈≈  ≈≈≈     ≈≈≈  ≈≈≈    ≈≈≈    ≈≈≈ ≈≈≈    ≈≈≈  ≈≈≈≈   ≠≠≠     ≠≠≠ ≠≠≠      ≠≠≠ ≠≠≠       ≠≠≠≠≠   ",
                "=========   =====      ========      ≈≈≈     ≈≈≈ ≈≈≈≈     ≈≈≈  ≈≈≈    ≈≈≈    ≈≈≈ ≈≈≈≈≈≈≈≈≈≈  ≈≈≈≈   ≠≠≠     ≠≠≠ ≠≠≠      ≠≠≠ ≠≠≠         ≠≠≠≠≠ ",
                "    =====  ======     =====           ≈≈≈   ∞≈≈≈  ≈≈≈    ≈≈≈≈  ≈≈≈≈   ≈≈≈    ≈≈≈  ≈≈≈        ≈≈≈≈   ≠≠≠    ≠≠≠≠  ≠≠≠    ≠≠≠  ≠≠≠≠          ≠≠≠≠",
                "    ============    =======            ≈≈≈≈≈≈≈≈≈   ≈≈≈≈≈≈≈≈≈   ≈≈≈≈≈≈ ≈≈≈    ≈≈≈   ≈≈≈≈≈≈≈≈   ≈≈≈≈≈  ≠≠≠≠≠≠≠≠≠≠   ≠≠≠≠≠≠≠≠    ≠≠≠≠≠≠≠≠ ≠≠≠≠≠≠≠ ",
                "     ==========   =========                                                                                                                    ",
                "      =========   =======                                                                                                                      "
            ];

            // Approximate columns
            int braceWidth = 32; // For left "brace" area
            int docsStartColumn = 100; // For "docs" start

            // RWM: Start with a blank line to separate the logo from the command.
            Console.WriteLine();

            // Print each line with segmented colors
            for (int i = 0; i < 12; i++)
            {
                string line = textLines[i];

                // Left part (0 to 31): blue based on row (top light, bottom dark)
                Color leftColor = i < 6 ? lightBlue : darkBlue;
                string leftPart = line.Substring(0, Math.Min(braceWidth, line.Length));

                // Mid part (32 to 99): white for "dotnet"
                int midLength = Math.Min(docsStartColumn - braceWidth, line.Length - braceWidth);
                string midPart = line.Substring(braceWidth, midLength);

                // Right part (100+): lightBlue for "docs"
                string rightPart = line.Length > docsStartColumn ? line[docsStartColumn..] : "";

                Console.Write(leftPart.Pastel(leftColor));
                Console.Write(midPart.Pastel(white));
                Console.Write(rightPart.Pastel(lightBlue));

                Console.WriteLine();
            }

            // Add the footer lines
            Console.WriteLine();
            Console.WriteLine($"DotNetDocs CLI v{GetVersion()}".Pastel(lightBlue));
            Console.WriteLine("https://dotnetdocs.com".Pastel(darkBlue));
            Console.WriteLine();
            Console.WriteLine($"Made with {"❤️".Pastel(Color.Red)} by {"☁️".Pastel(darkBlue)} CloudNimble".Pastel(gray));
            Console.WriteLine("https://github.com/CloudNimble".Pastel(gray));
            Console.WriteLine();
        }

        /// <summary>
        /// Queries NuGet.org for the latest version of the DotNetDocs.Sdk package.
        /// </summary>
        /// <param name="includePrerelease">Whether to include prerelease versions in the search.</param>
        /// <returns>The latest version string, or null if the query fails.</returns>
        protected static async Task<string?> GetLatestSdkVersionAsync(bool includePrerelease)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                // Query NuGet v3 API for package versions
                var url = "https://api.nuget.org/v3-flatcontainer/dotnetdocs.sdk/index.json";
                var response = await httpClient.GetStringAsync(url);

                // Parse JSON response
                using var document = JsonDocument.Parse(response);
                var versionsElement = document.RootElement.GetProperty("versions");

                // Extract and parse versions
                var versions = new List<NuGetVersion>();
                foreach (var versionElement in versionsElement.EnumerateArray())
                {
                    var versionString = versionElement.GetString();
                    if (!string.IsNullOrEmpty(versionString))
                    {
                        versions.Add(new NuGetVersion(versionString));
                    }
                }

                // Filter based on prerelease preference
                var filteredVersions = includePrerelease
                    ? versions
                    : versions.Where(v => !v.IsPrerelease).ToList();

                if (filteredVersions.Count == 0)
                {
                    return null;
                }

                // Sort and get the latest
                var latestVersion = filteredVersions.OrderByDescending(v => v).First();
                return latestVersion.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to query NuGet: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the version string for the tool.
        /// </summary>
        /// <returns>The version string.</returns>
        internal static string GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version
                ?? "Unknown";
            return version;
        }

        #endregion
        #region Helper Classes

        #endregion

    }

}