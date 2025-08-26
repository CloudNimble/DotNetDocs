#if NETSTANDARD2_0
using System;

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Specifies that an output will not be null even if the corresponding type allows it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class NotNullAttribute : Attribute
    {
    }
}
#endif