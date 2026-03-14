using System.Diagnostics;
using Granit.BlobStorage.GoogleCloud.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.BlobStorage.GoogleCloud.Tests.Diagnostics;

public sealed class BlobStorageGoogleCloudActivitySourceTests : IDisposable
{
    private readonly ActivityListener _listener;

    public BlobStorageGoogleCloudActivitySourceTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == BlobStorageGoogleCloudActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    [Fact]
    public void Name_is_Granit_BlobStorage_GoogleCloud() =>
        BlobStorageGoogleCloudActivitySource.Name.ShouldBe("Granit.BlobStorage.GoogleCloud");

    [Fact]
    public void StartActivity_returns_activity_when_listener_attached()
    {
        using Activity? activity = BlobStorageGoogleCloudActivitySource.Source.StartActivity(BlobStorageGoogleCloudActivitySource.UploadTicket);

        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("blobstorage.upload-ticket");
    }
}
