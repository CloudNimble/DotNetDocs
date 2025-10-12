using System;
using System.Drawing;
using System.Text;
using Console = Colorful.Console;

namespace CloudNimble.DotNetDocs.Tools.Commands.Base
{

    /// <summary>
    /// Base class for all DotNetDocs CLI commands, providing shared functionality like header display.
    /// </summary>
    public class DocsCommandBase
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

            // Set background to black for contrast
            //Console.BackgroundColor = Color.Black;

            // Define colors from the SVG
            Color lightBlue = Color.FromArgb(60, 208, 226); // Lighter cyan for top and "docs"
            Color darkBlue = Color.FromArgb(65, 154, 197);  // Darker blue for bottom
            Color white = Color.FromArgb(240, 240, 240);     // For "dotnet" - explicit white

            // Text ASCII art lines for "dotnetdocs"
            string[] textLines = new string[]
            {
                "      ≠≠≠≠≠≠≠   ≠≠≠≠≠≠≠≠≠                                                                                                                      ",
                "     ≠≠≠≠≠≠≠≠   ≠≠≠≠≠≠≠≠≠≠≠                  ≈≈≈                                                            ≠≠≠                                ",
                "    ≠≠≠≠≠≠≠≠   ≠≠≠≠≠≠≠≠≠≠≠≠                  ≈≈≈                ≈≈                            ≈≈≈           ≠≠≠                                ",
                "    ≠≠≠≠≠     ≠≠≠≠≠=  ≠≠≠≠≠                  ≈≈≈               ≈≈≈                           ≈≈≈≈           ≠≠≠                                ",
                "≠≠≠≠≠≠≠≠≠     ≠≠≠≠≠    ≠≠≠≠≠≠≠≠        ≈≈≈≈≈≈≈≈≈    ≈≈≈≈≈≈≈≈  ≈≈≈≈≈≈≈ ≈≈≈≈≈≈≈≈≈    ≈≈≈≈≈≈≈  ≈≈≈≈≈≈≈  =≠≠≠≠≠≠≠≠≠   ≠≠≠≠≠≠≠≠    ≠≠≠≠≠≠≠≠ ≠≠≠≠≠≠≠ ",
                "≠≠≠≠≠≠≠      ≠≠≠≠≠      ≠≠≠≠≠≠≠≠      ≈≈≈   ∞≈≈≈  ≈≈≈    ≈≈≈≈  ≈≈≈≈   ≈≈≈≈   ≈≈≈  ≈≈≈   ≈≈≈  ≈≈≈≈   ≠≠≠≠   ≠≠≠≠  ≠≠≠    ≠≠≠  ≠≠≠≠     ≠≠≠      ",
                "=======      =====      ========     ≈≈≈     ≈≈≈  ≈≈≈     ≈≈≈  ≈≈≈    ≈≈≈    ≈≈≈ ≈≈≈≈≈≈≈≈≈≈  ≈≈≈≈   ≠≠≠     ≠≠≠ ≠≠≠      ≠≠≠≠≠≠≠       ≠≠≠≠≠   ",
                "=========   =====      ========      ≈≈≈     ≈≈≈ ≈≈≈≈     ≈≈≈  ≈≈≈    ≈≈≈    ≈≈≈ ≈≈≈≈≈≈≈≈≈≈  ≈≈≈≈   ≠≠≠     ≠≠≠ ≠≠≠      ≠≠≠≠≠≠≠         ≠≠≠≠≠ ",
                "    =====  ======     =====           ≈≈≈   ∞≈≈≈  ≈≈≈    ≈≈≈≈  ≈≈≈≈   ≈≈≈    ≈≈≈  ≈≈≈        ≈≈≈≈   ≠≠≠    ≠≠≠≠ =≠≠≠    ≠≠≠  ≠≠≠           ≠≠≠≠",
                "    ============    =======            ≈≈≈≈≈≈≈≈≈   ≈≈≈≈≈≈≈≈≈   ≈≈≈≈≈≈ ≈≈≈    ≈≈≈   ≈≈≈≈≈≈≈≈   ≈≈≈≈≈  ≠≠≠≠≠≠≠≠≠≠   ≠≠≠≠≠≠≠≠    ≠≠≠≠≠≠≠≠≠≠≠≠≠≠≠≠ ",
                "     ==========   =========                                                                                                                    ",
                "      =========   =======                                                                                                                      "
            };

            // Approximate columns
            int braceWidth = 32; // For left "brace" area
            int docsStartColumn = 100; // For "docs" start

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

                Console.Write(leftPart, leftColor);
                Console.Write(midPart, white);
                Console.Write(rightPart, lightBlue);

                Console.WriteLine();
            }

            // Add the footer lines to match the image
            Console.Write("DotNetDocs 1.0 CLI", lightBlue);
            Console.WriteLine();

            Console.Write("Home: https://dotnetdocs.com", darkBlue);
            Console.WriteLine();

            Console.Write("Made with ❤️ by CloudNimble.", ConsoleColor.Gray);
            Console.WriteLine();

            Console.Write("https://github.com/CloudNimble", ConsoleColor.Gray);
            Console.WriteLine();

            // Reset console colors to default
            Console.ResetColor();
        }

        #endregion

    }

}