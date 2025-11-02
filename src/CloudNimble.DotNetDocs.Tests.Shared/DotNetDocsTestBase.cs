using System.IO;
using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.DotNetDocs.Core;

namespace CloudNimble.DotNetDocs.Tests.Shared
{

    public class DotNetDocsTestBase : BreakdanceMSTestBase
    {

        public const string projectPath = "..//..//..//";

#if NET8_0
            public const string framework = "net8.0";
#elif NET9_0
            public const string framework = "net9.0";
#elif NET10_0
            public const string framework = "net10.0";
#endif

        #region Internal Methods

        public DocAssembly GetTestsDotSharedAssembly()
        {
            // Use a type from BasicScenarios namespace to ensure we get all types
            var assemblyPath = typeof(SampleClass).Assembly.Location;
            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            var manager = new AssemblyManager(assemblyPath, xmlPath);
            var context = new ProjectContext();
            var assembly = manager.DocumentAsync(context).GetAwaiter().GetResult();

            // Debug: Log available namespaces and types
            System.Diagnostics.Debug.WriteLine($"Loaded {assembly.Namespaces.Count} namespaces");
            foreach (var ns in assembly.Namespaces)
            {
                System.Diagnostics.Debug.WriteLine($"  Namespace: {ns.Symbol.ToDisplayString()} with {ns.Types.Count} types");
                foreach (var type in ns.Types)
                {
                    System.Diagnostics.Debug.WriteLine($"    Type: {type.Symbol.Name}");
                }
            }

            return assembly;
        }


        #endregion

    }

}
