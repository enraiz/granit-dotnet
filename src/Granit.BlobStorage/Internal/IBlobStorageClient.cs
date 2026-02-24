namespace Granit.BlobStorage.Internal;

/// <summary>
/// Combines Pre-signed URL generation and low-level S3 object operations.
/// Implemented by <c>Granit.BlobStorage.S3.Internal.S3BlobClient</c>.
/// </summary>
internal interface IBlobStorageClient : IBlobPresignedUrlGenerator, IBlobObjectClient
{
}
