using Granit.Core.MultiTenancy;
using Granit.Guids;
using Granit.Security;
using Granit.Timeline.Domain;
using Granit.Timing;

namespace Granit.Timeline.Internal;

/// <summary>
/// Groups the infrastructure services required for audit-field initialization.
/// </summary>
internal sealed record AuditContext(
    IGuidGenerator GuidGenerator,
    IClock Clock,
    ICurrentUserService CurrentUser,
    ICurrentTenant CurrentTenant);

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
        AuditContext ctx) =>
        new()
        {
            Id = ctx.GuidGenerator.Create(),
            EntityType = entityType,
            EntityId = entityId,
            EntryType = entryType,
            Body = body,
            AuthorId = ctx.CurrentUser.UserId ?? string.Empty,
            AuthorName = ctx.CurrentUser.UserName ?? string.Empty,
            ParentEntryId = parentEntryId,
            CreatedAt = ctx.Clock.Now,
            CreatedBy = ctx.CurrentUser.UserId ?? string.Empty,
            TenantId = ctx.CurrentTenant.IsAvailable ? ctx.CurrentTenant.Id : null,
        };

    internal static TimelineAttachment CreateAttachment(
        Guid entryId,
        Guid blobId,
        string fileName,
        string contentType,
        long sizeBytes,
        AuditContext ctx) =>
        new()
        {
            Id = ctx.GuidGenerator.Create(),
            EntryId = entryId,
            BlobId = blobId,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            CreatedAt = ctx.Clock.Now,
            CreatedBy = ctx.CurrentUser.UserId ?? string.Empty,
            TenantId = ctx.CurrentTenant.IsAvailable ? ctx.CurrentTenant.Id : null,
        };
}
