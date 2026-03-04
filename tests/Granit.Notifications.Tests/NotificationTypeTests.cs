// =============================================================================
// Tests - NotificationType<TData>
// =============================================================================
// Verifies the abstract notification type base class: DataType resolution,
// default severity, Name and DefaultChannels overrides.
// =============================================================================

using Shouldly;
using Xunit;

namespace Granit.Notifications.Tests;

public sealed class NotificationTypeTests
{
    [Fact]
    public void DataType_ReturnsGenericTypeArgument()
    {
        TestOrderNotificationType notificationType = TestOrderNotificationType.Instance;

        notificationType.DataType.ShouldBe(typeof(OrderCreatedPayload));
    }

    [Fact]
    public void Name_ReturnsOverriddenValue()
    {
        TestOrderNotificationType notificationType = TestOrderNotificationType.Instance;

        notificationType.Name.ShouldBe("order.created");
    }

    [Fact]
    public void DefaultChannels_ReturnsOverriddenValue()
    {
        TestOrderNotificationType notificationType = TestOrderNotificationType.Instance;

        notificationType.DefaultChannels.ShouldContain(NotificationChannels.InApp);
        notificationType.DefaultChannels.ShouldContain(NotificationChannels.Email);
        notificationType.DefaultChannels.Count.ShouldBe(2);
    }

    [Fact]
    public void DefaultSeverity_IsInfoByDefault()
    {
        TestOrderNotificationType notificationType = TestOrderNotificationType.Instance;

        notificationType.DefaultSeverity.ShouldBe(NotificationSeverity.Info);
    }

    [Fact]
    public void DefaultSeverity_CanBeOverridden()
    {
        TestSecurityAlertNotificationType notificationType = TestSecurityAlertNotificationType.Instance;

        notificationType.DefaultSeverity.ShouldBe(NotificationSeverity.Fatal);
    }

    [Fact]
    public void DataType_DifferentPayloadTypes_ReturnCorrectType()
    {
        TestSecurityAlertNotificationType notificationType = TestSecurityAlertNotificationType.Instance;

        notificationType.DataType.ShouldBe(typeof(SecurityAlertPayload));
    }

    // -------------------------------------------------------------------------
    // Test types
    // -------------------------------------------------------------------------

    private sealed record OrderCreatedPayload(string OrderId, decimal Total);

    private sealed class TestOrderNotificationType : NotificationType<OrderCreatedPayload>
    {
        public static readonly TestOrderNotificationType Instance = new();
        public override string Name => "order.created";
        public override IReadOnlyList<string> DefaultChannels =>
            [NotificationChannels.InApp, NotificationChannels.Email];
    }

    private sealed record SecurityAlertPayload(string Reason);

    private sealed class TestSecurityAlertNotificationType : NotificationType<SecurityAlertPayload>
    {
        public static readonly TestSecurityAlertNotificationType Instance = new();
        public override string Name => "security.alert";
        public override IReadOnlyList<string> DefaultChannels => [NotificationChannels.InApp];
        public override NotificationSeverity DefaultSeverity => NotificationSeverity.Fatal;
    }
}
