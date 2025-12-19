using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Tools.Commands;
using Microsoft.Extensions.Hosting;

namespace CloudNimble.DotNetDocs.Tools
{

    /// <summary>
    /// 
    /// </summary>
    public class Program
    {

        /// <summary>
        /// Initializes and runs the application using the specified command-line arguments.
        /// </summary>
        /// <remarks>This method configures the application's host, sets the content root, and runs the
        /// command-line application asynchronously. It is typically used as the application's entry point.</remarks>
        /// <param name="args">An array of command-line arguments to configure the application's behavior.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the application's exit code.</returns>
        public static Task<int> Main(string[] args) =>
            Host.CreateDefaultBuilder()
                // RWM: If this is not set, it won't find appsettings.json.
                //      https://github.com/dotnet/sdk/issues/9730#issuecomment-433724425
                .UseContentRoot(Directory.GetParent(Assembly.GetExecutingAssembly().Location)?.FullName ?? Directory.GetCurrentDirectory())
                .ConfigureServices((context, services) =>
                {
                    //services.AddEFCoreToEdmxServices();
                })
                .RunCommandLineApplicationAsync<DocsRootCommand>(args);

    }

}
