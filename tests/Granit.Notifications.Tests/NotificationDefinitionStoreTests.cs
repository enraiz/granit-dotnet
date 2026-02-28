// =============================================================================
// Tests - NotificationDefinitionStore
// =============================================================================
// Verifies the in-memory notification definition store: unknown name returns
// null, GetAll returns all definitions, Initialize populates the store.
// =============================================================================

using Granit.Notifications.Internal;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Tests;

public sealed class NotificationDefinitionStoreTests
{
    [Fact]
    public void Get_UnknownName_ReturnsNull()
    {
        NotificationDefinitionStore store = new();

        NotificationDefinition? result = store.Get("unknown.notification");

        result.ShouldBeNull();
    }

    [Fact]
    public void GetAll_AfterInitialize_ReturnsAllDefinitions()
    {
        NotificationDefinition def1 = new("notif.one") { DefaultChannels = [NotificationChannels.InApp] };
        NotificationDefinition def2 = new("notif.two") { DefaultChannels = [NotificationChannels.Email] };

        NotificationDefinitionStore store = new();
        store.Initialize([def1, def2]);

        IReadOnlyList<NotificationDefinition> all = store.GetAll();

        all.Count.ShouldBe(2);
        all.ShouldContain(d => d.Name == "notif.one");
        all.ShouldContain(d => d.Name == "notif.two");
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
        result.ShouldNotBeNull();
        result!.DisplayName.ShouldBe("Test Notification");
    }
}
