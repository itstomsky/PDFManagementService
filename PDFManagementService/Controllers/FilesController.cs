using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PDFManagementService.Models;
using PDFManagementService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PDFManagementService.Controllers
{
    /// <summary>
    /// The FilesController
    /// </summary>
    [Route("PDFManagementService/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        /// <summary>
        /// The Logger 
        /// </summary>
        private readonly ILogger<FilesController> _logger;

        /// <summary>
        /// The BlobStorage Service
        /// </summary>
        private readonly IBlobStorageService _blobService;

        /// <summary>
        /// The BlobStorage config 
        /// </summary>
        private readonly IOptions<Configuration> _configuration;

        /// <summary>
        /// Creates a new instance of FilesController
        /// </summary>
        /// <param name="blobService"></param>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        public FilesController(IBlobStorageService blobService, ILogger<FilesController> logger, IOptions<Configuration> configuration)
        {
            this._blobService = blobService;
            this._blobService.GenerateLookup(); // Generate new lookups for reorder service

            this._logger = logger;
            this._configuration = configuration;
        }

        /// <summary>
        /// Http Get Method for file request
        /// </summary>
        /// <returns>the request file if exists</returns>
        [ProducesResponseType(typeof(FileContentResult),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("{fileName}")]
        public IActionResult GetFile(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                    return BadRequest("FileName not provided");

                fileName = this._blobService.filenameLookup[this._blobService.fileOrderLookup[fileName]];
                if (this._blobService.CheckFileExists(fileName))
                {
                    var file = this._blobService.GetFile(fileName);

                    return new FileContentResult(file.Content, file.ContentType);
                }
                else
                {
                    return BadRequest("File Doesn't exist");
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error downloading file:  {fileName}";
                this._logger.LogError(ex, errorMessage);
                return BadRequest(errorMessage);
            }
        }

        /// <summary>
        /// Http Post method to upload pdf file to Azure Blob Storage
        /// </summary>
        /// <param name="file">the file</param>
        /// <returns>uploaded file Absolute URI</returns>
        [Route("uploadfile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost]
        public IActionResult UploadFile([FromForm] IFormFile uploadedFile)
        {
            try
            {
                if (uploadedFile == null)
                    ModelState.AddModelError("NoFile", "file not uploaded");

                if (uploadedFile.Length > this._configuration.Value.MaxFileSizeAllowed)
                    ModelState.AddModelError("FileSizeTooBig", $"File size is bigger than maximum allowed file size {this._configuration.Value.MaxFileSizeAllowed}");


                if (!this._configuration.Value.SupportedTypes.Contains(uploadedFile.ContentType))
                    ModelState.AddModelError("InvalidFileType", $"Input file type is not supported");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);


                File file = new File()
                {
                    FileName = System.IO.Path.GetFileName(uploadedFile.FileName),
                    FileLength = uploadedFile.Length,
                    ContentType = uploadedFile.ContentType
                };

                using (var target = new System.IO.MemoryStream())
                {
                    uploadedFile.CopyTo(target);
                    file.Content = target.ToArray();
                }

                var fileLocation = "";

                if (this._blobService.filenameLookup != null)
                {
                    fileLocation = this._blobService.UploadFile(file, (this._blobService.filenameLookup.Keys.Count > 0) ? this._blobService.filenameLookup.Keys.Max() + 1 : 1);
                }
                else
                {
                    fileLocation = this._blobService.UploadFile(file, 1);
                }


                return Ok(fileLocation);
            }
            catch (Exception ex)
            {
                var errorMessage = $"failed to upload file : {uploadedFile.FileName} ";
                this._logger.LogError(ex, errorMessage);
                return BadRequest(errorMessage);
            }
        }

        /// <summary>
        /// Returns the list of files stored in storage.
        /// </summary>
        [HttpGet]
        [Route("list")]
        [ProducesResponseType(typeof(IEnumerable<File>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetFileList()
        {
            try
            {
                return Ok(_blobService.ListFiles());
            }
            catch (Exception ex)
            {
                var errorMessage = "Error getting file list";
                _logger.LogError(ex,$"Message: {errorMessage}");
                return BadRequest(errorMessage);
            }
        }

        /// <summary>
        /// Delete file from Storage 
        /// </summary>
        /// <param name="fileName">the fileName</param>
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpDelete("delete/{fileName}")]
        public IActionResult DeleteFile(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                    return BadRequest("FileName not provided");

                fileName = this._blobService.filenameLookup[this._blobService.fileOrderLookup[fileName]];
                if (this._blobService.CheckFileExists(fileName))
                {
                    if (this._blobService.DeleteFile(fileName))
                        return Ok($"File : {fileName} Deteled successfully");
                    else
                        return BadRequest($"Unable to delete file : {fileName}");                    
                }
                else
                {
                    return BadRequest("File Doesn't exist");
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"failed to delete file : {fileName} ";
                this._logger.LogError(ex, errorMessage);
                return BadRequest(errorMessage);
            }
        }


        /// <summary>
        /// ReOrder a file from the list
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="filePosition"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("reorder")]
        [ProducesResponseType(typeof(IEnumerable<File>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult ReorderFiles(string filename, long filePosition)
        {
            try
            {
                if (filename == null || filePosition == 0)
                    return BadRequest("File name or position is invalid");

                if (this._blobService.CheckFileExists(this._blobService.filenameLookup[this._blobService.fileOrderLookup[filename]]))
                {
                    if (this._blobService.filenameLookup.Keys.Count > 1)
                    {
                        if (filePosition > this._blobService.filenameLookup.Keys.Max())
                        {
                            filePosition = this._blobService.filenameLookup.Keys.Max();
                        }

                        return Ok(this._blobService.ReOrderFile(filename, filePosition));
                    }
                    else
                    {
                        return BadRequest("Not enough files to re-order");
                    }
                }
                else
                {
                    return BadRequest("File Doesn't exist");
                }
                
            }
            catch (Exception ex)
            {
                var errorMessage = "unable to re-order files";
                this._logger.LogError(ex, errorMessage);
                return BadRequest(errorMessage);
            }
        }
    }
}
