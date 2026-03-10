namespace Granit.BackgroundJobs.Internal;

/// <summary>
/// Thread-safe, additive collection of <see cref="RecurringJobRegistration"/> descriptors.
/// Registered as a singleton so that both <see cref="BackgroundJobsSeedService"/> (which reads)
/// and <c>AddGranitBackgroundJobAssemblies</c> (which writes) share the same instance.
/// </summary>
internal sealed class RecurringJobRegistrationCollection
{
    private readonly List<RecurringJobRegistration> _registrations = [];
    private readonly object _lock = new();

    internal void AddRange(IEnumerable<RecurringJobRegistration> registrations)
    {
        lock (_lock)
        {
            foreach (RecurringJobRegistration registration in registrations)
            {
                if (!_registrations.Exists(r => r.JobName == registration.JobName))
                {
                    _registrations.Add(registration);
                }
            }
        }
    }

    internal IReadOnlyList<RecurringJobRegistration> ToList()
    {
        lock (_lock)
        {
            return [.. _registrations];
        }
    }
}
