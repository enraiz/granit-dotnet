using Granit.Core.Events;

namespace Granit.BackgroundJobs.Events;

/// <summary>
/// Raised when a background job is paused by an administrator.
/// </summary>
public sealed record BackgroundJobPaused(
    Guid JobId,
    string JobName) : IDomainEvent;
