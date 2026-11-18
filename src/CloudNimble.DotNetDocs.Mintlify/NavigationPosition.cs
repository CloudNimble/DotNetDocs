namespace CloudNimble.DotNetDocs.Mintlify
{

    /// <summary>
    /// Specifies where a template-defined tab should appear relative to generated navigation.
    /// </summary>
    /// <remarks>
    /// This is a DotNetDocs generation hint parsed from the <c>Position</c> attribute on a
    /// <c>&lt;Tab&gt;</c> element in a MintlifyTemplate. It is not part of the Mintlify
    /// <c>docs.json</c> schema and is never serialized.
    /// </remarks>
    public enum NavigationPosition
    {

        /// <summary>
        /// The tab is emitted in template declaration order, before DocumentationReference tabs.
        /// This is the default.
        /// </summary>
        Start = 0,

        /// <summary>
        /// The tab is appended after the root tab and every DocumentationReference tab.
        /// Use this for outbound href tabs such as a sibling product site.
        /// </summary>
        End = 1

    }

}
