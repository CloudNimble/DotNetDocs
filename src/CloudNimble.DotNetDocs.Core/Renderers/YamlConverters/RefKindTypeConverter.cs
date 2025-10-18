using System;
using Microsoft.CodeAnalysis;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace CloudNimble.DotNetDocs.Core.Renderers.YamlConverters
{

    /// <summary>
    /// Custom type converter for RefKind enum.
    /// </summary>
    public class RefKindTypeConverter : IYamlTypeConverter
    {
        
        /// <summary>
        /// Determines whether this converter can handle the specified type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>true if this converter can handle the type; otherwise, false.</returns>
        public bool Accepts(Type type) => type == typeof(RefKind);

        /// <summary>
        /// Reads the YAML representation of the object.
        /// </summary>
        /// <param name="parser">The parser to read from.</param>
        /// <param name="type">The type of the object.</param>
        /// <param name="rootDeserializer">The root deserializer.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="NotImplementedException">Reading is not supported.</exception>
        public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer) 
            => throw new NotImplementedException("Reading RefKind from YAML is not supported");

        /// <summary>
        /// Writes the YAML representation of the object.
        /// </summary>
        /// <param name="emitter">The emitter to write to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="type">The type of the value.</param>
        /// <param name="serializer">The serializer.</param>
        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            if (value is RefKind kind)
            {
                emitter.Emit(new Scalar(kind.ToString()));
            }
            else
            {
                emitter.Emit(new Scalar("null"));
            }
        }
        
    }

}