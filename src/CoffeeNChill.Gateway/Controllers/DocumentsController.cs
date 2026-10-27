/*
 * Code Attribution:
 * The upload with a content type, the listing, the streaming download and the delete were adapted from my Part 1 document Functions and from the Azure Blob Storage documentation below. 
 * the controller layout (injected client, a GetContainerAsync helper, [Authorize], ActionResult return types and ProducesResponseType) is the same as my MenuController.
 * IFormFile is used instead of the MultipartReader of my Part 1 Functions, because an ASP.NET Core controller can bind an uploaded file directly.
 * The file type list, the 10 MB limit and the Path.GetFileName check are my own addition.
 * 
 * stackoverflow (2015) Response model for specific status codes using Swagger
 * Available at: https://stackoverflow.com/questions/29489304/response-model-for-specific-status-codes-using-swagger showed me an example of how i could do /// <summary> /// </summary> and /// <response code="...">....</response> (these will be comment visible to the user)
 * [Accessed 21 October 2026].
 * 
 * Microsoft Learn (2026ai). Controller action return types in ASP.NET Core web API. (helped me with ProducesResponseType and CreatedAtAction)
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types
 * [Accessed 20 October 2026].
 * 
 * Microsoft Learn (2026am). Upload files in ASP.NET Core. (For an MVC POST controller action method section helped me)(also helped me with RequestSizeLimit and Path.GetFileName())
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads
 * [Accessed 21 October 2026].
 *
 * Microsoft Learn (2026u). BlobHttpHeaders Class. (helped me with ContentType)
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.models.blobhttpheaders?view=azure-dotnet
 * [Accessed 21 October 2026].
 *
 * Microsoft Learn (2026v). BlobUploadOptions.HttpHeaders Property. (helped me with saving the content type on upload)
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.models.blobuploadoptions.httpheaders?view=azure-dotnet
 * [Accessed 21 October 2026].
 * 
 * GitHub (2020) [QUERY] How to create a new blob using OpenWriteAsync and setting metadata with a single operation #16875 (also helped me with saving the content type on upload)
 * Available at: https://github.com/Azure/azure-sdk-for-net/issues/16875
 * [Accessed 21 October 2026].
 * 
 * Microsoft Learn (2026an). List blobs with .NET. (helped me with GetBlobsAsync)
 * Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blobs-list
 * [Accessed 21 October 2026].
 *
 * Microsoft Learn (2026at). BlobBaseClient.DownloadStreamingAsync Method (helped me with DownloadStreamingAsync)
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.specialized.blobbaseclient.downloadstreamingasync?view=azure-dotnet#azure-storage-blobs-specialized-blobbaseclient-downloadstreamingasync(azure-storage-blobs-models-blobdownloadoptions-system-threading-cancellationtoken)
 * [Accessed 21 October 2026].
 *
 * Microsoft Learn (2026ao). Delete and restore a blob with .NET. (helped me with DeleteIfExistsAsync)
 * Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-delete
 * [Accessed 21 October 2026].
 *
 * Microsoft Learn (2026af). Role-based authorization in ASP.NET Core. (the [Authorize(Roles = "...")] controller example)
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
 * [Accessed 20 October 2026].
 */
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CoffeeNChill.Gateway.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers
{
    //the following /// comments are so the user can see them when using swagger
    /// <summary>
    /// Staff documents (recipe sheets, cleaning manuals, safety policies). Admin and Staff only.
    /// </summary>
    [ApiController]
    [Route("api/documents")]
    //[Authorize] on the class means all 4 endpoints need an Admin or Staff token
    [Authorize(Roles = "Admin,Staff")] 
    [Produces("application/json")]
    public class DocumentsController : ControllerBase
    {
        private const string ContainerName = "staff-docs";
        //10 MB max
        private const long MaxFileBytes = 10 * 1024 * 1024; 

        //The only file types we accept and the content type each one is saved with.
        private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".pdf", "application/pdf" },
            { ".png", "image/png" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" }
        };

        //One shared client for the whole blob service provided by a dependency injection (found in Program.cs).
        private readonly BlobServiceClient _blobs;

        public DocumentsController(BlobServiceClient blobs)
        {
            _blobs = blobs;
        }

        //Gets the staff-docs container. It creates it first if it is missing so the list is empty instead of an error.
        private async Task<BlobContainerClient> GetContainerAsync()
        {
            BlobContainerClient container = _blobs.GetBlobContainerClient(ContainerName);
            await container.CreateIfNotExistsAsync();
            return container;
        }

        /// <summary>
        /// Uploads a staff document (PDF, PNG or JPEG, 10 MB max). A file with the same name is replaced.
        /// </summary>
        /// <param name="file">The file to upload.</param>
        /// <response code="201">The file was saved.</response>
        /// <response code="400">No file, a file type that is not allowed or the file is too big.</response>
        /// <response code="401">No token or the token is not valid.</response>
        /// <response code="403">The token is valid but the role is not Admin or Staff.</response>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        //a bit more than 10 MB so our own 400 message shows for big files
        [RequestSizeLimit(11 * 1024 * 1024)] 
        [ProducesResponseType<StaffDocumentResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationErrorResponse>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<StaffDocumentResponse>> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ValidationErrorResponse("No file was uploaded. Choose a PDF, PNG or JPEG file."));
            }

            //Keep only the file name, never any folder text (so nobody can write ../something).
            string fileName = Path.GetFileName(file.FileName);
            string extension = Path.GetExtension(fileName);

            if (!AllowedTypes.TryGetValue(extension, out string? contentType))
            {
                return BadRequest(new ValidationErrorResponse("File type not allowed. Only PDF, PNG and JPEG files can be uploaded."));
            }

            if (file.Length > MaxFileBytes)
            {
                return BadRequest(new ValidationErrorResponse("The file is too big. The limit is 10 MB."));
            }

            BlobContainerClient container = await GetContainerAsync();
            BlobClient blob = container.GetBlobClient(fileName);

            //Saves the content type with the file or Azure stores it as application/octet-stream.
            await using Stream stream = file.OpenReadStream();
            await blob.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            });

            BlobProperties properties = (await blob.GetPropertiesAsync()).Value;
            var response = new StaffDocumentResponse
            {
                FileName = fileName,
                SizeInBytes = properties.ContentLength,
                LastModified = properties.LastModified
            };

            // 201 Created and a Location header that points to the download of this file.
            return CreatedAtAction(nameof(Download), new { fileName }, response);
        }

        /// <summary>
        /// Lists all staff documents with their size and last modified date.
        /// </summary>
        /// <response code="200">The list of documents (an empty list when there are none).</response>
        /// <response code="401">No token, or the token is not valid.</response>
        /// <response code="403">The token is valid but the role is not Admin or Staff.</response>
        [HttpGet]
        [ProducesResponseType<List<StaffDocumentResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<StaffDocumentResponse>>> List()
        {
            BlobContainerClient container = await GetContainerAsync();
            var files = new List<StaffDocumentResponse>();

            await foreach (BlobItem item in container.GetBlobsAsync())
            {
                files.Add(new StaffDocumentResponse
                {
                    FileName = item.Name,
                    SizeInBytes = item.Properties.ContentLength,
                    LastModified = item.Properties.LastModified
                });
            }
            return Ok(files);
        }

        /// <summary>
        /// Downloads one staff document.
        /// </summary>
        /// <param name="fileName">The exact file name for example recipe-sheet.pdf.</param>
        /// <response code="200">The file.</response>
        /// <response code="400">The file name is not valid.</response>
        /// <response code="401">No token or the token is not valid.</response>
        /// <response code="403">The token is valid but the role is not Admin or Staff.</response>
        /// <response code="404">There is no file with that name.</response>
        [HttpGet("download/{fileName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationErrorResponse>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Download(string fileName)
        {
            //Refuse names that contain folder text.
            if (fileName != Path.GetFileName(fileName))
            {
                return BadRequest(new ValidationErrorResponse("The file name is not valid."));
            }

            BlobContainerClient container = await GetContainerAsync();
            BlobClient blob = container.GetBlobClient(fileName);

            //Check it exists first so we can return a 404.
            if (!await blob.ExistsAsync())
            {
                return NotFound(new ErrorResponse($"File '{fileName}' was not found.", HttpContext.TraceIdentifier));
            }

            //Streaming sends the file piece by piece instead of loading all of it into memory first.
            BlobDownloadStreamingResult result = (await blob.DownloadStreamingAsync()).Value;
            return File(result.Content, result.Details.ContentType ?? "application/octet-stream", fileName);
        }

        /// <summary>
        /// Deletes one staff document.
        /// </summary>
        /// <param name="fileName">The exact file name.</param>
        /// <response code="204">The file was deleted.</response>
        /// <response code="400">The file name is not valid.</response>
        /// <response code="401">No token or the token is not valid.</response>
        /// <response code="403">The token is valid but the role is not Admin or Staff.</response>
        /// <response code="404">There is no file with that name.</response>
        [HttpDelete("{fileName}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationErrorResponse>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string fileName)
        {
            if (fileName != Path.GetFileName(fileName))
            {
                return BadRequest(new ValidationErrorResponse("The file name is not valid."));
            }

            BlobContainerClient container = await GetContainerAsync();
            BlobClient blob = container.GetBlobClient(fileName);

            //DeleteIfExistsAsync tells us if something was really deleted so we can answer 404 when it was not.
            bool deleted = await blob.DeleteIfExistsAsync();
            if (!deleted)
            {
                return NotFound(new ErrorResponse($"File '{fileName}' was not found.", HttpContext.TraceIdentifier));
            }

            return NoContent();
        }
    }
}

