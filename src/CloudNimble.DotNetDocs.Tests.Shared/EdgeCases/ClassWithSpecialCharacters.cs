namespace CloudNimble.DotNetDocs.Tests.Shared.EdgeCases
{
    /// <summary>
    /// A class with special characters in documentation: &lt;, &gt;, &amp;, &quot;, &apos;.
    /// </summary>
    /// <remarks>
    /// This tests handling of XML special characters like &lt;tag&gt; and &amp;entity;.
    /// Also tests "quotes" and 'apostrophes'.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Using generics: List&lt;string&gt;
    /// var list = new List&lt;string&gt;();
    /// if (x &gt; 0 &amp;&amp; y &lt; 10) { }
    /// </code>
    /// </example>
    public class ClassWithSpecialCharacters
    {

        #region Public Methods

        /// <summary>
        /// A method with special characters in docs: &lt;T&gt; generics.
        /// </summary>
        /// <param name="input">An input with &quot;quotes&quot; and &apos;apostrophes&apos;.</param>
        /// <returns>A string with &amp; ampersands.</returns>
        /// <remarks>
        /// This method handles &lt;, &gt;, &amp; characters properly.
        /// </remarks>
        public string MethodWithSpecialChars(string input)
        {
            return $"<{input}> & more";
        }

        #endregion

    }

}