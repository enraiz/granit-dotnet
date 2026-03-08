using Granit.Core.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Granit.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that collects domain events from tracked
/// <see cref="IDomainEventSource"/> entities before <c>SaveChanges</c>
/// and dispatches them after the transaction commits.
/// </summary>
/// <remarks>
/// Events are collected <em>before</em> save (while change tracker entries are available)
/// and dispatched <em>after</em> save (so that the database state is consistent).
/// If <c>SaveChanges</c> throws, no events are dispatched.
/// </remarks>
public sealed class DomainEventDispatcherInterceptor(IDomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
    // AsyncLocal because multiple SaveChanges calls may be in-flight concurrently
    // across different DbContext instances in the same async flow.
    private static readonly AsyncLocal<List<IDomainEvent>?> PendingEvents = new();

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CollectDomainEvents(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CollectDomainEvents(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DispatchCollectedEventsSync();
        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchCollectedEventsAsync(cancellationToken).ConfigureAwait(false);
        return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        // Discard collected events — the transaction failed.
        PendingEvents.Value = null;
        base.SaveChangesFailed(eventData);
    }

    /// <inheritdoc />
    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        // Discard collected events — the transaction failed.
        PendingEvents.Value = null;
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private static void CollectDomainEvents(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        List<IDomainEvent>? events = null;

        foreach (EntityEntry entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IDomainEventSource source || source.DomainEvents.Count == 0)
            {
                continue;
            }

            events ??= [];
            events.AddRange(source.DomainEvents);
            source.ClearDomainEvents();
        }

        PendingEvents.Value = events;
    }

    private void DispatchCollectedEventsSync()
    {
        List<IDomainEvent>? events = PendingEvents.Value;
        PendingEvents.Value = null;

        if (events is null or { Count: 0 })
        {
            return;
        }

        // Synchronous dispatch: fire-and-forget is acceptable here because
        // domain events are in-process and dispatched on the local queue.
        dispatcher.DispatchAsync(events, CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    private async Task DispatchCollectedEventsAsync(CancellationToken cancellationToken)
    {
        List<IDomainEvent>? events = PendingEvents.Value;
        PendingEvents.Value = null;

        if (events is null or { Count: 0 })
        {
            return;
        }

        await dispatcher.DispatchAsync(events, cancellationToken).ConfigureAwait(false);
    }
}
