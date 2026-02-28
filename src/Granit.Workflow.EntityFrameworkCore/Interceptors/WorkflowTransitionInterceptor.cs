using System.Reflection;
using Granit.Core.Domain;
using Granit.Core.MultiTenancy;
using Granit.Guids;
using Granit.Security;
using Granit.Timing;
using Granit.Workflow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Granit.Workflow.EntityFrameworkCore.Interceptors;

/// <summary>
/// EF Core interceptor that detects workflow state changes on entities implementing
/// <see cref="IWorkflowStateful"/> and automatically creates <see cref="WorkflowTransitionRecord"/>
/// entries in the same transaction.
/// </summary>
/// <remarks>
/// <para>
/// HDS compliance: transition records are INSERT-only and immutable. They capture who
/// changed the state, when, and provide an optional comment field for regulatory justification
/// read from <see cref="WorkflowTransitionContext.Current"/>.
/// </para>
/// <para>
/// Also synchronizes <see cref="IPublishable.IsPublished"/> for entities implementing
/// <see cref="IVersionedEntity"/> (keeps IsPublished in sync with LifecycleStatus).
/// </para>
/// <para>
/// Registered as Scoped. Must be ordered after <c>AuditedEntityInterceptor</c> and
/// before <c>SoftDeleteInterceptor</c> in the interceptor chain.
/// </para>
/// </remarks>
public sealed class WorkflowTransitionInterceptor(
    ICurrentUserService currentUserService,
    IClock clock,
    IGuidGenerator guidGenerator,
    ICurrentTenant currentTenant) : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IClock _clock = clock;
    private readonly IGuidGenerator _guidGenerator = guidGenerator;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        DetectAndRecordTransitions(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        DetectAndRecordTransitions(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void DetectAndRecordTransitions(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        DateTimeOffset now = _clock.Now;
        string userId = _currentUserService.UserId ?? "system";

        // Sync IsPublished for versioned entities (Added + Modified)
        SyncPublishableFlag(context);

        // Detect state changes on Modified entities implementing IWorkflowStateful
        foreach (EntityEntry entry in context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified))
        {
            if (entry.Entity is not IWorkflowStateful stateful)
            {
                continue;
            }

            Type entityType = entry.Entity.GetType();
            string statusPropertyName = GetStaticAbstract<string>(entityType, nameof(IWorkflowStateful.StatusPropertyName));
            string workflowEntityType = GetStaticAbstract<string>(entityType, nameof(IWorkflowStateful.WorkflowEntityType));

            PropertyEntry statusProperty = entry.Property(statusPropertyName);

            string? previousState = statusProperty.OriginalValue?.ToString();
            string? newState = statusProperty.CurrentValue?.ToString();

            if (string.Equals(previousState, newState, StringComparison.Ordinal))
            {
                continue;
            }

            WorkflowTransitionRecord record = new()
            {
                Id = _guidGenerator.Create(),
                EntityType = workflowEntityType,
                EntityId = stateful.GetWorkflowEntityId(),
                PreviousState = previousState ?? string.Empty,
                NewState = newState ?? string.Empty,
                TransitionedAt = now,
                TransitionedBy = userId,
                TenantId = _currentTenant.IsAvailable ? _currentTenant.Id : null,
                Comment = WorkflowTransitionContext.Current?.Comment,
            };

            context.Set<WorkflowTransitionRecord>().Add(record);
        }
    }

    private static void SyncPublishableFlag(DbContext context)
    {
        foreach (EntityEntry entry in context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            if (entry.Entity is IVersionedEntity versioned)
            {
                versioned.IsPublished = versioned.LifecycleStatus == WorkflowLifecycleStatus.Published;
            }
        }
    }

    private static T GetStaticAbstract<T>(Type type, string propertyName) =>
        (T)type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)! // NOSONAR S3011 - intentional: static abstract interface members resolved via reflection
            .GetValue(null)!;
}
