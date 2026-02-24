namespace Granit.BackgroundJobs.Internal;

/// <summary>
/// Immutable descriptor of a recurring job discovered at startup via
/// <see cref="RecurringJobDiscovery"/>.
/// </summary>
internal sealed record RecurringJobRegistration(
    string JobName,
    string CronExpression,
    string MessageType);
