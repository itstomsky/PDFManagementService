using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PDFManagementService.Models
{
    /// <summary>
    /// Configuration for PDFManagementService
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Gets or sets Connectionstring of Azure BlobStorage
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// gets or sets Containername
        /// </summary>
        public string ContainerName { get; set; }

        /// <summary>
        /// Gets or sets max file size
        /// </summary>
        public int MaxFileSizeAllowed { get; set; }

        /// <summary>
        /// Gets or sets supported file types
        /// </summary>
        public string[] SupportedTypes { get; set; }        
    }
}
