namespace Granit.BackgroundJobs;

/// <summary>
/// Well-known header names used by the background jobs infrastructure.
/// </summary>
public static class BackgroundJobHeaders
{
    /// <summary>
    /// Header carrying the admin user identity for manual triggers (ISO 27001 audit).
    /// </summary>
    public const string TriggeredBy = "X-Triggered-By";
}
