// =============================================================================
// Tests - NotificationDefinitionStore (edge cases)
// =============================================================================
// Verifies store edge cases: Initialize with duplicate names throws,
// Initialize replaces previous state, Get is case-sensitive.
// =============================================================================

using Granit.Notifications.Internal;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Tests;

public sealed class NotificationDefinitionStoreEdgeCaseTests
{
    [Fact]
    public void Initialize_DuplicateNames_ThrowsArgumentException()
    {
        NotificationDefinition def1 = new("duplicate.name") { DefaultChannels = [NotificationChannels.InApp] };
        NotificationDefinition def2 = new("duplicate.name") { DefaultChannels = [NotificationChannels.Email] };

        NotificationDefinitionStore store = new();

        // FrozenDictionary.ToFrozenDictionary throws on duplicate keys
        Should.Throw<ArgumentException>(() => store.Initialize([def1, def2]));
    }

    [Fact]
    public void Initialize_Twice_ReplacesFirstSet()
    {
        NotificationDefinition firstDef = new("first.notification") { DefaultChannels = [NotificationChannels.InApp] };
        NotificationDefinition secondDef = new("second.notification") { DefaultChannels = [NotificationChannels.Email] };

        NotificationDefinitionStore store = new();
        store.Initialize([firstDef]);
        store.Initialize([secondDef]);

        store.Get("first.notification").ShouldBeNull("first set should be replaced");
        store.Get("second.notification").ShouldNotBeNull("second set should be active");
    }

    [Fact]
    public void Get_IsCaseSensitive()
    {
        NotificationDefinition definition = new("Order.Created") { DefaultChannels = [NotificationChannels.InApp] };

        NotificationDefinitionStore store = new();
        store.Initialize([definition]);

        store.Get("Order.Created").ShouldNotBeNull();
        store.Get("order.created").ShouldBeNull("lookup should be case-sensitive");
        store.Get("ORDER.CREATED").ShouldBeNull("lookup should be case-sensitive");
    }

    [Fact]
    public void GetAll_EmptyStore_ReturnsEmptyList()
    {
        NotificationDefinitionStore store = new();

        IReadOnlyList<NotificationDefinition> all = store.GetAll();

        all.ShouldBeEmpty();
    }

    [Fact]
    public void Initialize_EmptyCollection_ClearsStore()
    {
        NotificationDefinition definition = new("test.notification") { DefaultChannels = [NotificationChannels.InApp] };

        NotificationDefinitionStore store = new();
        store.Initialize([definition]);
        store.Initialize([]);

        store.GetAll().ShouldBeEmpty();
        store.Get("test.notification").ShouldBeNull();
    }
}
