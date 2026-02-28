// =============================================================================
// Tests - NotificationDefinitionStore
// =============================================================================
// Verifies the in-memory notification definition store: unknown name returns
// null, GetAll returns all definitions, Initialize populates the store.
// =============================================================================

using FluentAssertions;
using Granit.Notifications.Internal;
using Xunit;

namespace Granit.Notifications.Tests;

public sealed class NotificationDefinitionStoreTests
{
    [Fact]
    public void Get_UnknownName_ReturnsNull()
    {
        NotificationDefinitionStore store = new();

        NotificationDefinition? result = store.Get("unknown.notification");

        result.Should().BeNull();
    }

    [Fact]
    public void GetAll_AfterInitialize_ReturnsAllDefinitions()
    {
        NotificationDefinition def1 = new("notif.one") { DefaultChannels = [NotificationChannels.InApp] };
        NotificationDefinition def2 = new("notif.two") { DefaultChannels = [NotificationChannels.Email] };

        NotificationDefinitionStore store = new();
        store.Initialize([def1, def2]);

        IReadOnlyList<NotificationDefinition> all = store.GetAll();

        all.Should().HaveCount(2);
        all.Should().Contain(d => d.Name == "notif.one");
        all.Should().Contain(d => d.Name == "notif.two");
    }

    [Fact]
    public void Get_AfterInitialize_ReturnsDefinitionByName()
    {
        NotificationDefinition definition = new("provider.notif")
        {
            DefaultChannels = [NotificationChannels.InApp],
            DisplayName = "Test Notification",
        };

        NotificationDefinitionStore store = new();
        store.Initialize([definition]);

        NotificationDefinition? result = store.Get("provider.notif");
        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Test Notification");
    }
}
