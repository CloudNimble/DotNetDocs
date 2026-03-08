using System.Text.Json;
using System.Text.Json.Serialization;
using Mintlify.Core.Converters;

namespace Mintlify.Core
{

    /// <summary>
    /// Contains constants and shared configuration objects for Mintlify documentation generation.
    /// </summary>
    public static class MintlifyConstants
    {

        #region Properties

        /// <summary>
        /// Gets the shared JsonSerializerOptions instance for consistent Mintlify JSON serialization.
        /// </summary>
        /// <remarks>
        /// This instance is configured with:
        /// - Indented formatting for readable output
        /// - CamelCase property naming to match Mintlify schema
        /// - Null value ignoring to omit optional properties
        /// - Polymorphic JSON converters for complex object types
        /// - Source-generated type info resolver on .NET 8+ for AOT compatibility
        /// </remarks>
        public static JsonSerializerOptions JsonSerializerOptions { get; } = CreateOptions();

        #endregion

        #region Private Methods

        private static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters =
                {
                    new NavigationJsonConverter(),
                    new NavigationPageListConverter(),
                    new NavigationPageConverter(),
                    new IconConverter(),
                    new ApiConfigConverter(),
                    new ServerConfigConverter(),
                    new ColorConverter(),
                    new BackgroundImageConverter(),
                    new PrimaryNavigationConverter()
                }
            };
#if NET8_0_OR_GREATER
            options.TypeInfoResolverChain.Insert(0, MintlifyJsonContext.Default);
#endif
            return options;
        }

        #endregion

    }

}
