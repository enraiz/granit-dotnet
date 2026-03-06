using Granit.Identity.EntityFrameworkCore.Entities;
using Granit.Identity.EntityFrameworkCore.Events;
using Granit.Identity.EntityFrameworkCore.Internal;
using Granit.Identity.Models;
using Microsoft.Extensions.Logging;

namespace Granit.Identity.EntityFrameworkCore.Handlers;

/// <summary>
/// Wolverine handler that processes identity provider user events and updates the local cache.
/// </summary>
internal sealed partial class IdentityUserEventHandler(
    IIdentityProvider identityProvider,
    IUserCacheStore store,
    TimeProvider timeProvider,
    ILogger<IdentityUserEventHandler> logger)
{
    /// <summary>
    /// Handles a user created/updated event by fetching the user from the identity provider
    /// and upserting the cache entry.
    /// </summary>
    public async Task HandleAsync(IdentityUserUpdatedEvent @event, CancellationToken cancellationToken)
    {
        IdentityUser? user = await identityProvider.GetUserAsync(@event.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            LogUserNotFoundInProvider(@event.UserId);
            return;
        }

        var entry = new UserCacheEntry
        {
            ExternalUserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Enabled = user.Enabled,
            LastSyncedAt = timeProvider.GetUtcNow()
        };

        await store.UpsertAsync(entry, cancellationToken).ConfigureAwait(false);
        LogUserCacheUpdated(@event.UserId);
    }

    /// <summary>
    /// Handles a user deleted event by hard-deleting the cache entry (RGPD Art. 17).
    /// </summary>
    public async Task HandleAsync(IdentityUserDeletedEvent @event, CancellationToken cancellationToken)
    {
        await store.DeleteByExternalIdAsync(@event.UserId, @event.TenantId, cancellationToken)
            .ConfigureAwait(false);
        LogUserCacheDeleted(@event.UserId);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "User {UserId} not found in identity provider during cache sync")]
    private partial void LogUserNotFoundInProvider(string userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "[AUDIT] User cache entry updated via webhook for user {UserId}")]
    private partial void LogUserCacheUpdated(string userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "[AUDIT] RGPD: user cache entry deleted via webhook for user {UserId}")]
    private partial void LogUserCacheDeleted(string userId);
}
