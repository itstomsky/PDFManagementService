using PDFManagementService.Controllers;
using PDFManagementService.Models;
using PDFManagementService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PDFManagementService.Tests
{
    /// <summary>
    /// The FilesController Test class
    /// </summary>
    public class FilesController_Tests
    {
        /// <summary>
        /// The file config options
        /// </summary>
        private Mock<IOptions<Configuration>> _mockConfiguration;

        /// <summary>
        /// The mock logger 
        /// </summary>
        private Mock<ILogger<FilesController>> _mockLogger;

        /// <summary>
        /// The File controller
        /// </summary>
        private FilesController _fileController;

        /// <summary>
        /// Mock Blob Service
        /// </summary>
        private Mock<IBlobStorageService> _mockBlobService;

        /// <summary>
        /// initialises a new instance of <see cref="FileController_Tests"/> and set up tests 
        /// </summary>
        public FilesController_Tests()
        {
            this._mockLogger = new Mock<ILogger<FilesController>>();
            this.SetUpConfiguration();
            this._mockBlobService = new Mock<IBlobStorageService>();
            this._mockBlobService.Object.GenerateLookup();
        }


        /// <summary>
        /// Sets up Configuration
        /// </summary>
        private void SetUpConfiguration()
        {
            this._mockConfiguration = new Mock<IOptions<Configuration>>();

            Configuration config = new Configuration()
            {
                MaxFileSizeAllowed = 5242880,
                SupportedTypes = new string[] { "application/pdf" }
            };
            this._mockConfiguration.Setup(x => x.Value).Returns(config);
        }

        /// <summary>
        /// Creates IFormFile
        /// </summary>
        /// <param name="fileName">the fileName</param>
        /// <param name="content">the content</param>
        /// <param name="contentType">the contentType</param>
        /// <param name="fileLength">the fileLenght</param>
        /// <returns></returns>
        private IFormFile CreateTestFormFile(string fileName, string content, string contentType, long fileLength)
        {
            byte[] fileBytes = Encoding.UTF8.GetBytes(content);

            var file = new FormFile(new System.IO.MemoryStream(fileBytes), 0, fileLength, null, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };

            return file;
        }

        /// <summary>
        /// Test to check if uploading pdf files work
        /// </summary>
        [Fact]
        public void UploadPDFFileTest()
        {
            IFormFile file = CreateTestFormFile("Test.pdf", "Test Content", "application/pdf", 2 * 1024);
            this._fileController = new FilesController(this._mockBlobService.Object, this._mockLogger.Object, this._mockConfiguration.Object);

            var actual = this._fileController.UploadFile(file);
            Assert.IsType<OkObjectResult>(actual);
        }

        /// <summary>
        /// Test to check if bad request thrown for invalid file type
        /// </summary>
        [Fact]
        public void UploadFile_Invalid()
        {
            IFormFile file = CreateTestFormFile("Test.json", "Test Content", "application/json", 10 * 1024);
            this._fileController = new FilesController(this._mockBlobService.Object, this._mockLogger.Object, this._mockConfiguration.Object);
            var actual = this._fileController.UploadFile(file);            
            var actualErrorString = ((SerializableError)(((BadRequestObjectResult)actual).Value)).GetValueOrDefault("InvalidFileType");
            var expectedErrorString = "Input file type is not supported";

            Assert.IsType<BadRequestObjectResult>(actual);            
            Assert.Equal(expectedErrorString, ((string[])actualErrorString)[0]);
        }

        /// <summary>
        /// Tests Upload File returns Bad Request for larger than allowed file length
        /// </summary>
        [Fact]
        public void UploadFile_LargerThanAllowed()
        {
            IFormFile file = CreateTestFormFile("Test.pdf", "Test Content", "application/pdf", 1024 * 1024 * 1024);
            this._fileController = new FilesController(this._mockBlobService.Object, this._mockLogger.Object, this._mockConfiguration.Object);
            var actual = this._fileController.UploadFile(file);
            var actualErrorString = ((SerializableError)(((BadRequestObjectResult)actual).Value)).GetValueOrDefault("FileSizeTooBig");
            var expectedErrorString = "File size is bigger than maximum allowed file size 5242880";

            Assert.IsType<BadRequestObjectResult>(actual);
            Assert.Equal(expectedErrorString, ((string[])actualErrorString)[0]);
        }

        /// <summary>
        /// Test to check if file can be downloaded
        /// </summary>
        [Fact]
        public void DownloadFile()
        {
            var fileName = "Test.pdf";
            var contentType = "application/pdf";
            byte[] bytes = Encoding.ASCII.GetBytes("Test Content String");
            FileContentResult fileContent = new FileContentResult(bytes, contentType);
            fileContent.FileDownloadName = fileName;

            File _file = new File()
            {
                FileName = fileName,
                ContentType = contentType,
                Content = fileContent.FileContents
            };
            this._mockBlobService.Setup(x => x.CheckFileExists(It.IsAny<string>())).Returns(true);
            this._mockBlobService.Setup(x => x.GetFile(It.IsAny<string>())).Returns(() => _file);            
            this._fileController = new FilesController(this._mockBlobService.Object, this._mockLogger.Object, this._mockConfiguration.Object);
            var actual = this._fileController.GetFile(fileName) as FileContentResult;
            var expected = fileContent;

            Assert.Equal(expected.FileContents, actual.FileContents);            
        }

        /// <summary>
        /// Tests download service returns bad request when no file name provided
        /// </summary>
        [Fact]
        public void DownloadFile_NoName()
        {
            this._fileController = new FilesController(this._mockBlobService.Object, this._mockLogger.Object, this._mockConfiguration.Object);
            var actual = this._fileController.GetFile("");
            var expected = "FileName not provided";

            Assert.IsType<BadRequestObjectResult>(actual);
            Assert.Equal(expected, ((BadRequestObjectResult)actual).Value);
        }

        /// <summary>
        /// Tests download service returns bad request when file doesn't exist
        /// </summary>
        [Fact]
        public void DownloadFile_InvalidRequest()
        {
            this._fileController = new FilesController(this._mockBlobService.Object, this._mockLogger.Object, this._mockConfiguration.Object);
            var actual = this._fileController.GetFile("Test.pdf");
            var expected = "File Doesn't exist";

            Assert.IsType<BadRequestObjectResult>(actual);
            Assert.Equal(expected, ((BadRequestObjectResult)actual).Value);
        }

        /// <summary>
        /// Tests list service 
        /// </summary>
        [Fact]
        public void GetFileList()
        {
            IEnumerable<File> files = new List<File>();
            this._mockBlobService.Setup(x => x.ListFiles()).Returns(() => files);
            this._fileController = new FilesController(this._mockBlobService.Object, this._mockLogger.Object, this._mockConfiguration.Object);
            var actual = this._fileController.GetFileList();
            
            Assert.IsType<OkObjectResult>(actual);            
        }

        /// <summary>
        /// Tests Delete service
        /// </summary>
        [Fact]
        public void DeleteFile()
        {          
            this._mockBlobService.Setup(x => x.CheckFileExists(It.IsAny<string>())).Returns(true);
            this._mockBlobService.Setup(x => x.DeleteFile(It.IsAny<string>())).Returns(() => true);
            this._fileController = new FilesController(this._mockBlobService.Object, this._mockLogger.Object, this._mockConfiguration.Object);
            var actual = this._fileController.DeleteFile("Test.pdf");

            Assert.IsType<OkObjectResult>(actual);
        }

        /// <summary>
        /// Tests Delete service returns bad request when no file name provided
        /// </summary>
        [Fact]
        public void DeleteFile_NoName()
        {
            this._fileController = new FilesController(this._mockBlobService.Object, this._mockLogger.Object, this._mockConfiguration.Object);
            var actual = this._fileController.DeleteFile("");
            var expected = "FileName not provided";

            Assert.IsType<BadRequestObjectResult>(actual);
            Assert.Equal(expected, ((BadRequestObjectResult)actual).Value);
        }

        /// <summary>
        /// Tests Delete service returns bad request when file doesn't exist
        /// </summary>
        [Fact]
        public void DeleteFile_InvalidRequest()
        {
            this._fileController = new FilesController(this._mockBlobService.Object, this._mockLogger.Object, this._mockConfiguration.Object);
            var actual = this._fileController.DeleteFile("Test.pdf");
            var expected = "File Doesn't exist";

            Assert.IsType<BadRequestObjectResult>(actual);
            Assert.Equal(expected, ((BadRequestObjectResult)actual).Value);
        }
    }
}
