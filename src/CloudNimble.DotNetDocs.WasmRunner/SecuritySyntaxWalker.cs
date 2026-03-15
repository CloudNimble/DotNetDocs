using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CloudNimble.DotNetDocs.WasmRunner
{

    /// <summary>
    /// A Roslyn syntax walker that detects usage of blocked namespaces in user-submitted C# code.
    /// </summary>
    /// <remarks>
    /// This walker scans <c>using</c> directives and flags namespaces that provide dangerous
    /// capabilities such as file system access, network access, reflection, or process execution.
    /// Code that references blocked namespaces should not be compiled or executed in the sandbox.
    /// </remarks>
    public class SecuritySyntaxWalker : CSharpSyntaxWalker
    {

        #region Fields

        private static readonly HashSet<string> _blockedNamespaces =
        [
            "System.Diagnostics",
            "System.IO",
            "System.Net",
            "System.Net.Http",
            "System.Net.Sockets",
            "System.Reflection",
            "System.Runtime.InteropServices",
            "System.Runtime.Loader",
            "System.Security.Cryptography"
        ];

        #endregion

        #region Properties

        /// <summary>
        /// Gets the list of blocked <c>using</c> directives found during the walk.
        /// </summary>
        public List<string> BlockedUsings { get; } = [];

        /// <summary>
        /// Gets a value indicating whether any security issues were found.
        /// </summary>
        public bool HasSecurityIssues => BlockedUsings.Count > 0;

        #endregion

        #region Public Methods

        /// <summary>
        /// Visits a <c>using</c> directive and checks whether it references a blocked namespace.
        /// </summary>
        /// <param name="node">The using directive syntax node.</param>
        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            var name = node.Name?.ToString();

            if (!string.IsNullOrWhiteSpace(name) &&
                _blockedNamespaces.Any(blocked =>
                    name == blocked || name.StartsWith($"{blocked}.")))
            {
                BlockedUsings.Add(name);
            }

            base.VisitUsingDirective(node);
        }

        #endregion

    }

}
