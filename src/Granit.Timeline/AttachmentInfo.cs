namespace Granit.Timeline;

/// <summary>
/// Attachment metadata for display in the activity stream.
/// </summary>
public sealed record AttachmentInfo(
    Guid Id,
    Guid BlobId,
    string FileName,
    string ContentType,
    long SizeBytes);
