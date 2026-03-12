namespace Granit.BackgroundJobs.Internal;

/// <summary>
/// Internal envelope wrapping a job message and optional headers for in-process dispatch.
/// </summary>
internal sealed record BackgroundJobEnvelope(
    object Message,
    IDictionary<string, string>? Headers = null);
