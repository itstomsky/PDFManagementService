# PDFManagementService
This is a .NET CORE RESTFUL API to manage pdf files on Azure Blob Storage.
It has been tested with Azurite as this was the only Azure Blob Storage Emulator available on Mac.

Created: 18/10/2020
Author: Thomas Varghese



Upload (POST)
http://hostname:port/PDFManagementService/files/uploadfile

Add "uploadedFile" attribute to body as form-data (add the pdf file to this attribute for uploading)

Download (GET)
http://hostname:port/PDFManagementService/files/filename.pdf

List (GET)
http://hostname:port/PDFManagementService/files/list

Delete (DELETE)
http://hostname:port/PDFManagementService/files/delete/filename.pdf

ReOrder (GET)
http://hostname:port/PDFManagementService/files/reorder?filename=filename.pdf&filePosition=1
