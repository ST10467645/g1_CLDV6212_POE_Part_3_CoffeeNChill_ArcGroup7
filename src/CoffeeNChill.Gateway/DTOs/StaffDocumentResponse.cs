/*
 * Code Attribution:
 * The response DTO follows the same idea as my MenuItemResponse and my Part 2 StaffDocumentResponse: send back only the facts the client needs not the whole blob object.
 *
 * Microsoft Learn (2026an). List blobs with .NET. (the BlobItem Name, ContentLength and LastModified properties)
 * Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blobs-list
 * [Accessed 21 October 2026].
 */

namespace CoffeeNChill.Gateway.DTOs
{
    //One staff document in the list: showing its name, size and when it was last changed.
    public class StaffDocumentResponse
    {
        public string FileName { get; set; } = string.Empty;
        public long? SizeInBytes { get; set; }
        public DateTimeOffset? LastModified { get; set; }

    }
}
