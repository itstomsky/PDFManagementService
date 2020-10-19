using PDFManagementService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PDFManagementService.Services
{
    /// <summary>
    /// the BlobStorageService interface
    /// </summary>
    public interface IBlobStorageService
    {
        /// <summary>
        /// File name lookup for reorder service
        /// </summary>
        public Dictionary<long, string> filenameLookup { get; set; }

        /// <summary>
        /// File Position lookup for reorder service
        /// </summary>
        public Dictionary<string, long> fileOrderLookup { get; set; }

        /// <summary>
        /// Generate lookups for reorder service
        /// </summary>
        public void GenerateLookup();

        /// <summary>
        /// Uploads file to Blob Storage
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fileOrder"></param>
        /// <returns></returns>
        public string UploadFile(File file, long fileOrder);

        /// <summary>
        /// Checks if file already exists
        /// </summary>
        /// <param name="fileName">the fileName</param>
        /// <returns>true or false</returns>
        bool CheckFileExists(string fileName);

        /// <summary>
        /// Get File from Blob Storage
        /// </summary>
        /// <param name="fileName">the fileName</param>
        /// <returns>the file</returns>
        File GetFile(string fileName);

        /// <summary>
        /// Get the list of files 
        /// </summary>
        /// <returns>the list of files</returns>
        IEnumerable<File> ListFiles();

        /// <summary>
        /// Delete File from Blob Storage
        /// </summary>
        /// <param name="fileName">the fileName</param>
        /// <returns>true or false</returns>
        bool DeleteFile(string fileName);

        /// <summary>
        /// ReOrder a file to the given position
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="fileOrder"></param>
        /// <returns></returns>
        public IEnumerable<File> ReOrderFile(string filename, long fileOrder);
    }
}
