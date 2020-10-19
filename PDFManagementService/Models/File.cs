using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PDFManagementService.Models
{
    /// <summary>
    /// The File class 
    /// </summary>
    public class File
    {
        /// <summary>
        /// Gets or sets the FileName
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the contentType 
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        ///  Gets or sets FileLength
        /// </summary>
        public long FileLength { get; set; }

        /// <summary>
        /// Gets or sets the file stream
        /// </summary>
        public byte[] Content { get; set; }

        /// <summary>
        /// Gets or sets the file order
        /// </summary>
        public long Position { get; set; }
    }
}
