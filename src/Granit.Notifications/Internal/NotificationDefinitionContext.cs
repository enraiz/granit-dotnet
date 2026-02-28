using Granit.Notifications.Abstractions;

namespace Granit.Notifications.Internal;

internal sealed class NotificationDefinitionContext : INotificationDefinitionContext
{
    private readonly List<NotificationDefinition> _definitions = [];

    public void Add(NotificationDefinition definition) =>
        _definitions.Add(definition);

    internal IReadOnlyList<NotificationDefinition> GetDefinitions() =>
        _definitions;
}
