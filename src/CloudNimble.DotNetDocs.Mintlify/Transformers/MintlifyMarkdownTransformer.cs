using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CloudNimble.DotNetDocs.Core;

namespace CloudNimble.DotNetDocs.Mintlify.Transformers
{

    /// <summary>
    /// Transforms HTML-style comments to JSX-style comments for MDX compatibility.
    /// </summary>
    /// <remarks>
    /// MDX (used by Mintlify) requires JSX-style comments {/* */} instead of HTML comments &lt;!-- --&gt;.
    /// This transformer processes all string properties in the DocEntity object graph and converts
    /// HTML comments to JSX comments to prevent MDX parsing errors.
    /// </remarks>
    public partial class MintlifyMarkdownTransformer : IDocTransformer
    {

        #region Fields

        /// <summary>
        /// Compiled regex for detecting HTML-style comments.
        /// </summary>
        [GeneratedRegex(@"<!--\s*(.*?)\s*-->", RegexOptions.Singleline)]
        private static partial Regex HtmlCommentPattern();

        #endregion

        #region Public Methods

        /// <summary>
        /// Transforms a documentation entity by converting HTML comments to JSX comments.
        /// </summary>
        /// <param name="entity">The documentation entity to transform.</param>
        /// <returns>A task representing the asynchronous transformation operation.</returns>
        public Task TransformAsync(DocEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            // Transform all string properties recursively
            TransformEntity(entity);

            return Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Recursively transforms all string properties in the entity graph.
        /// </summary>
        /// <param name="obj">The object to transform.</param>
        private void TransformEntity(object obj)
        {
            if (obj is null)
            {
                return;
            }

            var type = obj.GetType();

            // Process all properties
            foreach (var property in type.GetProperties())
            {
                // Only process readable and writable properties
                if (!property.CanRead || !property.CanWrite)
                {
                    continue;
                }

                var value = property.GetValue(obj);
                if (value is null)
                {
                    continue;
                }

                // If it's a string, transform HTML comments to JSX comments
                if (property.PropertyType == typeof(string))
                {
                    var stringValue = (string)value;
                    if (stringValue.Contains("<!--"))
                    {
                        var transformed = ConvertHtmlCommentsToJsx(stringValue);
                        property.SetValue(obj, transformed);
                    }
                }
                // If it's a DocEntity or derived type, recurse
                else if (value is DocEntity)
                {
                    TransformEntity(value);
                }
                // If it's a collection, process each item
                else if (value is System.Collections.IEnumerable enumerable && value is not string)
                {
                    foreach (var item in enumerable)
                    {
                        if (item is not null)
                        {
                            TransformEntity(item);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Converts HTML-style comments to JSX-style comments.
        /// </summary>
        /// <param name="text">The text containing HTML comments.</param>
        /// <returns>The text with JSX-style comments.</returns>
        private static string ConvertHtmlCommentsToJsx(string text)
        {
            // Replace <!-- comment --> with {/* comment */}
            return HtmlCommentPattern().Replace(text, match =>
            {
                var commentContent = match.Groups[1].Value;
                return $"{{/* {commentContent} */}}";
            });
        }

        #endregion

    }

}
