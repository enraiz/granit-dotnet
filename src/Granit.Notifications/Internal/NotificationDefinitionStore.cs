using System.Collections.Frozen;
using Granit.Notifications.Abstractions;

namespace Granit.Notifications.Internal;

/// <summary>
/// Singleton registry of notification definitions, populated at startup from all
/// registered <see cref="INotificationDefinitionProvider"/> implementations.
/// </summary>
internal sealed class NotificationDefinitionStore : INotificationDefinitionStore
{
    private FrozenDictionary<string, NotificationDefinition> _definitions = FrozenDictionary<string, NotificationDefinition>.Empty;

    public IReadOnlyList<NotificationDefinition> GetAll() =>
        _definitions.Values.ToList();

    public NotificationDefinition? Get(string notificationTypeName) =>
        _definitions.GetValueOrDefault(notificationTypeName);

    internal void Initialize(IEnumerable<NotificationDefinition> definitions) =>
        _definitions = definitions.ToFrozenDictionary(d => d.Name);
}
