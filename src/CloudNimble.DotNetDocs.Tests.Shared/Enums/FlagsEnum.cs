using System;

namespace CloudNimble.DotNetDocs.Tests.Shared.Enums
{

    /// <summary>
    /// A flags enum for bitwise operations.
    /// </summary>
    [Flags]
    public enum FlagsEnum
    {

        /// <summary>
        /// No permissions.
        /// </summary>
        None = 0,

        /// <summary>
        /// Read permission.
        /// </summary>
        Read = 1,

        /// <summary>
        /// Write permission.
        /// </summary>
        Write = 2,

        /// <summary>
        /// Execute permission.
        /// </summary>
        Execute = 4,

        /// <summary>
        /// Delete permission.
        /// </summary>
        Delete = 8,

        /// <summary>
        /// All permissions combined.
        /// </summary>
        All = Read | Write | Execute | Delete

    }

}