using System.ComponentModel.DataAnnotations;

namespace CloudNimble.DotNetDocs.Core.Configuration
{

    /// <summary>
    /// Provides configuration options for how documentation files are named and organized.
    /// </summary>
    /// <remarks>
    /// This class controls the file naming strategy for rendered documentation, including
    /// how namespaces are represented in the file system and what characters are used
    /// as separators in file names.
    /// </remarks>
    public class FileNamingOptions
    {

        #region Properties

        /// <summary>
        /// Gets or sets the mode for organizing namespace documentation.
        /// </summary>
        /// <value>The namespace organization mode. Default is <see cref="NamespaceMode.File"/>.</value>
        /// <remarks>
        /// This property determines whether namespaces are rendered as individual files
        /// or organized into a folder hierarchy.
        /// </remarks>
        public NamespaceMode NamespaceMode { get; set; } = NamespaceMode.File;

        /// <summary>
        /// Gets or sets the character used to separate namespace parts in file names.
        /// </summary>
        /// <value>The separator character. Default is '-'.</value>
        /// <remarks>
        /// This setting is only used when <see cref="NamespaceMode"/> is set to <see cref="NamespaceMode.File"/>.
        /// When using <see cref="NamespaceMode.Folder"/>, this setting is ignored as namespaces
        /// are organized into actual folder hierarchies.
        /// Common values include '-' (hyphen), '_' (underscore), or '.' (period).
        /// </remarks>
        [MaxLength(1)]
        public char NamespaceSeparator { get; set; } = '-';

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FileNamingOptions"/> class with default settings.
        /// </summary>
        public FileNamingOptions()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileNamingOptions"/> class with the specified settings.
        /// </summary>
        /// <param name="namespaceMode">The namespace organization mode.</param>
        /// <param name="namespaceSeparator">The character to use as a namespace separator in file names.</param>
        public FileNamingOptions(NamespaceMode namespaceMode, char namespaceSeparator = '-')
        {
            NamespaceMode = namespaceMode;
            NamespaceSeparator = namespaceSeparator;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a copy of the current <see cref="FileNamingOptions"/> instance.
        /// </summary>
        /// <returns>A new instance with the same settings.</returns>
        public FileNamingOptions Clone()
        {
            return new FileNamingOptions(NamespaceMode, NamespaceSeparator);
        }

        #endregion

    }

}