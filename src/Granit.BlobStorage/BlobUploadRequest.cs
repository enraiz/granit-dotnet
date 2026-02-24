namespace Granit.BlobStorage;

/// <summary>
/// Parameters for requesting a Pre-signed upload ticket.
/// </summary>
/// <param name="FileName">Original filename provided by the client (used for <see cref="BlobDescriptor.OriginalFileName"/>).</param>
/// <param name="ContentType">MIME type declared by the client. Enforced in the Pre-signed URL headers; verified post-upload by <see cref="IBlobValidator"/>.</param>
/// <param name="MaxAllowedBytes">Maximum file size in bytes. Enforced post-upload by <see cref="IBlobValidator"/>.</param>
/// <param name="Metadata">Optional application-level key/value pairs stored as S3 object metadata.</param>
public sealed record BlobUploadRequest(
    string FileName,
    string ContentType,
    long MaxAllowedBytes,
    IReadOnlyDictionary<string, string>? Metadata = null);
