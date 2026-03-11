using Granit.Core.Events;

namespace Granit.BlobStorage.Events;

/// <summary>
/// Raised when a blob is crypto-shredded (S3 bytes erased, DB record retained for ISO 27001 audit).
/// </summary>
public sealed record BlobDeleted(
    Guid BlobId,
    string ContainerName,
    string? DeletionReason) : IDomainEvent;
