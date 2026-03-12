namespace Granit.BackgroundJobs.Internal;

/// <summary>
/// Shared helper for creating job message instances from type names.
/// </summary>
internal static class CronSchedulerHelper
{
    /// <summary>
    /// Creates an instance of the message type resolved from the assembly-qualified type name.
    /// </summary>
    internal static object CreateMessage(string messageType, string jobName)
    {
        var type = Type.GetType(messageType);
        if (type is null)
        {
            throw new InvalidOperationException(
                $"Cannot resolve message type '{messageType}' for job '{jobName}'. " +
                "Ensure the assembly containing the message is loaded.");
        }

        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException(
                $"Cannot instantiate message type '{type.Name}' for job '{jobName}'.");
    }
}
