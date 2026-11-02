using System.Collections.Generic;

namespace Mintlify.Core.Models
{

    /// <summary>
    /// Represents custom language grammars for code blocks.
    /// </summary>
    public class CodeblockLanguagesConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets paths to custom TextMate or Shiki language JSON files.
        /// </summary>
        public List<string>? Custom { get; set; }

        #endregion

    }

}
