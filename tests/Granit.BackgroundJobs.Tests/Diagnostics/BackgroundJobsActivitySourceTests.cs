using System.Diagnostics;
using Granit.BackgroundJobs.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.BackgroundJobs.Tests.Diagnostics;

public sealed class BackgroundJobsActivitySourceTests : IDisposable
{
    private readonly ActivityListener _listener;

    public BackgroundJobsActivitySourceTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == BackgroundJobsActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    [Fact]
    public void Name_is_Granit_BackgroundJobs() =>
        BackgroundJobsActivitySource.Name.ShouldBe("Granit.BackgroundJobs");

    [Fact]
    public void StartActivity_returns_activity_when_listener_attached()
    {
        using Activity? activity = BackgroundJobsActivitySource.Source.StartActivity(BackgroundJobsActivitySource.Trigger);

        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("backgroundjobs.trigger");
    }
}
