#if NET8_0_OR_GREATER
using System.Text.Json.Serialization;
using Mintlify.Core.Models;

namespace Mintlify.Core
{

    /// <summary>
    /// Source-generated JSON serializer context for AOT-compatible serialization of Mintlify configuration types.
    /// </summary>
    /// <remarks>
    /// This context enables trimming-safe and AOT-compatible JSON serialization for .NET 8+.
    /// Only root types that are independently serialized/deserialized need explicit registration;
    /// all property types are automatically discovered by the source generator.
    /// </remarks>
    [JsonSourceGenerationOptions(
        WriteIndented = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(DocsJsonConfig))]
    [JsonSerializable(typeof(GroupConfig))]
    [JsonSerializable(typeof(NavigationLinkConfig))]
    [JsonSerializable(typeof(SourceRefConfig))]
    [JsonSerializable(typeof(CodeblocksConfig))]
    [JsonSerializable(typeof(SdkRefConfig))]
    [JsonSerializable(typeof(NavThumbnailConfig))]
    [JsonSerializable(typeof(ContextualOptionConfig))]
    internal partial class MintlifyJsonContext : JsonSerializerContext
    {
    }

}
#endif
