using System.Diagnostics;
using Granit.Webhooks.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.Webhooks.Tests.Diagnostics;

public sealed class WebhooksActivitySourceTests : IDisposable
{
    private readonly ActivityListener _listener;

    public WebhooksActivitySourceTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == WebhooksActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    [Fact]
    public void Name_is_Granit_Webhooks() =>
        WebhooksActivitySource.Name.ShouldBe("Granit.Webhooks");

    [Fact]
    public void StartActivity_returns_activity_when_listener_attached()
    {
        using Activity? activity = WebhooksActivitySource.Source.StartActivity(WebhooksActivitySource.Deliver);

        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("webhooks.deliver");
    }

    [Fact]
    public void StartActivity_returns_null_without_listener()
    {
        _listener.Dispose();

        // Use a dedicated ActivitySource to avoid interference from parallel test listeners.
        using var isolatedSource = new ActivitySource("Granit.Webhooks.Tests.Isolated");
        using Activity? activity = isolatedSource.StartActivity("webhooks.test");

        activity.ShouldBeNull();
    }
}
