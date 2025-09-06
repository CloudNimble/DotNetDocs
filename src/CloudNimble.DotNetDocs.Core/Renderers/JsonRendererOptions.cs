using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloudNimble.DotNetDocs.Core.Renderers
{

    /// <summary>
    /// Configuration options for the JsonRenderer.
    /// </summary>
    public class JsonRendererOptions
    {

        #region Fields

        private static readonly JsonSerializerOptions _defaultSerializerOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.Preserve,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the JsonSerializerOptions to use for serialization.
        /// </summary>
        /// <remarks>
        /// If not specified, uses default options with camelCase naming, indented output,
        /// null value ignoring, and enum string conversion.
        /// </remarks>
        public JsonSerializerOptions SerializerOptions { get; set; } = _defaultSerializerOptions;

        #endregion

    }

}