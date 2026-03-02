using Granit.Core.MultiTenancy;
using Granit.Guids;
using Granit.Security;
using Granit.Timeline.Domain;
using Granit.Timing;

namespace Granit.Timeline.Internal;

/// <summary>
/// Centralizes <see cref="TimelineEntry"/> and <see cref="TimelineAttachment"/> construction
/// to avoid duplicating audit-field initialization across store implementations.
/// </summary>
internal static class TimelineEntityFactory
{
    internal static TimelineEntry CreateEntry(
        string entityType,
        string entityId,
        TimelineEntryType entryType,
        string body,
        Guid? parentEntryId,
        IGuidGenerator guidGenerator,
        IClock clock,
        ICurrentUserService currentUser,
        ICurrentTenant currentTenant) =>
        new()
        {
            Id = guidGenerator.Create(),
            EntityType = entityType,
            EntityId = entityId,
            EntryType = entryType,
            Body = body,
            AuthorId = currentUser.UserId ?? string.Empty,
            AuthorName = currentUser.UserName ?? string.Empty,
            ParentEntryId = parentEntryId,
            CreatedAt = clock.Now,
            CreatedBy = currentUser.UserId ?? string.Empty,
            TenantId = currentTenant.IsAvailable ? currentTenant.Id : null,
        };

    internal static TimelineAttachment CreateAttachment(
        Guid entryId,
        Guid blobId,
        string fileName,
        string contentType,
        long sizeBytes,
        IGuidGenerator guidGenerator,
        IClock clock,
        ICurrentUserService currentUser,
        ICurrentTenant currentTenant) =>
        new()
        {
            Id = guidGenerator.Create(),
            EntryId = entryId,
            BlobId = blobId,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            CreatedAt = clock.Now,
            CreatedBy = currentUser.UserId ?? string.Empty,
            TenantId = currentTenant.IsAvailable ? currentTenant.Id : null,
        };
}
