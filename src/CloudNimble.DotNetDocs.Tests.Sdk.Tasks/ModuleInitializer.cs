using System.Runtime.CompilerServices;
using CloudNimble.EasyAF.MSBuild;

namespace CloudNimble.DotNetDocs.Tests.Sdk.Tasks
{

    /// <summary>
    /// Module initializer that registers MSBuild when the assembly is loaded.
    /// This runs before test discovery, ensuring MSBuildLocator can redirect
    /// assembly resolution to the SDK before any MSBuild types are loaded.
    /// </summary>
    internal static class ModuleInitializer
    {

        /// <summary>
        /// Registers MSBuild when the assembly is loaded, before test discovery.
        /// </summary>
        [ModuleInitializer]
        internal static void Initialize()
        {
            MSBuildProjectManager.EnsureMSBuildRegistered();
        }

    }

}
