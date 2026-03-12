using System.Diagnostics;
using Granit.Notifications.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Tests.Diagnostics;

public sealed class NotificationsActivitySourceTests : IDisposable
{
    private readonly ActivityListener _listener;

    public NotificationsActivitySourceTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == NotificationsActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    [Fact]
    public void Name_is_Granit_Notifications() =>
        NotificationsActivitySource.Name.ShouldBe("Granit.Notifications");

    [Fact]
    public void StartActivity_returns_activity_when_listener_attached()
    {
        using Activity? activity = NotificationsActivitySource.Source.StartActivity(NotificationsActivitySource.Deliver);

        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("notifications.deliver");
    }
}
