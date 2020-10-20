using Azure.Storage.Blobs;
using PDFManagementService.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using PDFManagementService.Helpers;
using System.Linq;

namespace PDFManagementService.Services
{
    /// <summary>
    /// The BlobStorage Service class
    /// </summary>
    public class BlobStorageService : IBlobStorageService
    {
        /// <summary>
        ///  The file configuration
        /// </summary>
        private readonly IOptions<Configuration> _configuration;

        /// <summary>
        /// The BlobCContainer client
        /// </summary>
        private BlobContainerClient _blobContainerClient;

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<long, string> filenameLookup { get; set; } = new Dictionary<long, string>();

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, long> fileOrderLookup { get; set; } = new Dictionary<string, long>();

        /// <summary>
        /// Creates a new instance of <see cref="BlobStorageService"/>
        /// </summary>        
        /// <param name="Configuration"> </param>
        public BlobStorageService(IOptions<Configuration> _configuration)
        {
            try
            {
                this._configuration = _configuration;

                // Creates a new Blob Container
                this.CreateBlobContainer();
            }
            catch (Exception ex)
            {
                // Preserve and throw original stack trace for error tracking and debugging purposes. 
                throw ex;
            }
        }

        /// <summary>
        /// Checks if file already exists
        /// </summary>
        /// <param name="fileName">the fileName</param>
        /// <returns>true or false</returns>
        public bool CheckFileExists(string fileName)
        {
            bool fileExists = false;
            try
            {                
                var blobClient = this._blobContainerClient.GetBlobClient(fileName);
                if (blobClient != null)
                {
                    fileExists = true;
                }
            }
            catch (Exception ex)
            {
                // Preserve and throw original stack trace for error tracking and debugging purposes. 
                throw ex;
            }
            return fileExists;
        }

        /// <summary>
        /// Delete File from Blob Storage
        /// </summary>
        /// <param name="fileName">the fileName</param>
        /// <returns>true or false</returns>
        public bool DeleteFile(string fileName)
        {
            try
            {
                var blobClient = this._blobContainerClient.GetBlobClient(fileName);
                return blobClient.DeleteIfExists().Value;
            }
            catch (Exception ex)
            {
                // Preserve and throw original stack trace for error tracking and debugging purposes. 
                throw ex;
            }            
        }

        /// <summary>
        /// Get File from Blob Storage
        /// </summary>
        /// <param name="fileName">the fileName</param>
        /// <returns>the file</returns>
        public File GetFile(string fileName)
        {
            try
            {
                var blobClient = this._blobContainerClient.GetBlobClient(fileName);
                var blobdownloadInfo = blobClient.Download();

                File file = new File()
                {
                    FileName = fileName,
                    Content = FileReader.ReadFileContents(blobdownloadInfo.Value.Content),
                    ContentType = blobdownloadInfo.Value.ContentType
                };

                return file;
            }
            catch (Exception ex)
            {
                // Preserve and throw original stack trace for error tracking and debugging purposes. 
                throw ex;
            }
        }

        /// <summary>
        /// Get the list of files
        /// Note* Blobs are ordered lexicographically by name.
        /// </summary>
        /// <returns>the list of files</returns>
        public IEnumerable<File> ListFiles()
        {            
            try 
            {
                var files = new List<File>();

                foreach (var blobItem in this._blobContainerClient.GetBlobs())
                {
                    long.TryParse(blobItem.Name.Split('-').First(), out long Position);

                    File newFile = new File();
                    newFile.FileName = blobItem.Name.Split('-').Last();
                    newFile.ContentType = blobItem.Properties.ContentType;
                    newFile.FileLength = blobItem.Properties.ContentLength.Value;
                    newFile.Position = Position;

                    files.Add(newFile);
                }

                return files.OrderBy(x => x.Position).ToList();
            }
            catch (Exception ex)
            {
                // Preserve and throw original stack trace for error tracking and debugging purposes. 
                throw ex;
            }
        }

        /// <summary>
        /// ReOrder a file to the given position
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="filePosition"></param>
        /// <returns></returns>
        public IEnumerable<File> ReOrderFile(string filename, long filePosition)
        {
            try
            {
                long currentFileOrder = this.fileOrderLookup[filename];

                if (currentFileOrder > filePosition)
                {
                    for (long i = filePosition; i < currentFileOrder; i++)
                    {
                        if (this.filenameLookup.ContainsKey(i))
                        {
                            RenameFile(this.filenameLookup[i], (i + 1).ToString() + "-" + this.filenameLookup[i].Split('-').Last());
                        }
                    }
                    RenameFile(this.filenameLookup[currentFileOrder], filePosition.ToString() + "-" + this.filenameLookup[currentFileOrder].Split('-').Last());
                }
                else if(filePosition > currentFileOrder)
                {
                    for (long i = currentFileOrder+1; i <= filePosition; i++)
                    {
                        if (this.filenameLookup.ContainsKey(i))
                        {
                            RenameFile(this.filenameLookup[i], (i - 1).ToString() + "-" + this.filenameLookup[i].Split('-').Last());
                        }
                    }
                    RenameFile(this.filenameLookup[currentFileOrder], filePosition.ToString() + "-" + this.filenameLookup[currentFileOrder].Split('-').Last());
                }
                else
                {
                    // Same order so do nothing
                }

                return this.ListFiles();

            }
            catch (Exception ex)
            {
                // Preserve and throw original stack trace for error tracking and debugging purposes. 
                throw ex;
            }
        }

        /// <summary>
        /// Uploads file to Azure Blob Storage 
        /// </summary>        
        /// <param name="file">the file</param>        
        /// <returns>file path</returns>
        public string UploadFile(File file, long fileOrder)
        {
            try 
            {
                var blobClient = this._blobContainerClient.GetBlobClient(file.FileName);

                blobClient.Upload(new System.IO.MemoryStream(file.Content), new BlobHttpHeaders { ContentType = file.ContentType });

                return blobClient.Uri.AbsoluteUri;
            }
            catch (Exception ex)
            {
                // Preserve and throw original stack trace for error tracking and debugging purposes. 
                throw ex;
            }
        }

        /// <summary>
        /// Create Container in Azure Blob storage
        /// </summary>
        private void CreateBlobContainer()
        {
            // Create blob Container if not already exists 
            this._blobContainerClient = new BlobContainerClient(_configuration.Value.ConnectionString, _configuration.Value.ContainerName);
            this._blobContainerClient.CreateIfNotExists();
        }

        public void GenerateLookup()
        {
            this.filenameLookup = new Dictionary<long, string>();
            this.fileOrderLookup = new Dictionary<string, long>();


            foreach (var blobItem in this._blobContainerClient.GetBlobs())
            {
                long fileOrder;

                if (blobItem.Name.Contains('-'))
                {
                    string fileOrderString = blobItem.Name.Split('-').First();
                    long.TryParse(fileOrderString, out fileOrder);

                    if (!filenameLookup.ContainsKey(fileOrder))
                    {
                        filenameLookup.Add(fileOrder, blobItem.Name);
                        fileOrderLookup.Add(blobItem.Name.Split('-').Last(), fileOrder);
                    }
                    else
                    {
                        fileOrder = (this.filenameLookup.Keys.Count > 0) ? this.filenameLookup.Keys.Max() + 1 : 1;
                        string newFilename = fileOrder.ToString() + "-" + blobItem.Name.Split('-').Last();
                        RenameFile(blobItem.Name, newFilename);

                        filenameLookup.Add(fileOrder, newFilename);
                        fileOrderLookup.Add(blobItem.Name.Split('-').Last(), fileOrder);
                    }
                }
                else
                {
                    fileOrder = (this.filenameLookup.Keys.Count > 0) ? this.filenameLookup.Keys.Max() + 1 : 1;
                    string newFilename = fileOrder.ToString() + "-" + blobItem.Name;
                    RenameFile(blobItem.Name, newFilename);

                    filenameLookup.Add(fileOrder, newFilename);
                    fileOrderLookup.Add(blobItem.Name, fileOrder);
                }
            }
        }

        private void RenameFile(string oldFilename, string newFilename)
        {
            try
            {
                var blobClientNew = this._blobContainerClient.GetBlobClient(newFilename);
                if (!blobClientNew.Exists())
                {
                    var blobClientOld = this._blobContainerClient.GetBlobClient(oldFilename);

                    if (blobClientOld.Exists())
                    {
                        blobClientNew.StartCopyFromUri(blobClientOld.Uri);
                        blobClientOld.DeleteIfExists();
                    }
                }

            }
            catch (Exception ex)
            {
                // Preserve and throw original stack trace for error tracking and debugging purposes. 
                throw ex;
            }
        }
    }
}
