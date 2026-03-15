using System.Text.Json.Serialization;

namespace CloudNimble.DotNetDocs.WasmRunner
{

    /// <summary>
    /// Source-generated JSON serialization context for AOT-compatible serialization of <see cref="CompilationResult"/>.
    /// </summary>
    [JsonSerializable(typeof(CompilationResult))]
    internal partial class CompilationResultContext : JsonSerializerContext
    {
    }

}
