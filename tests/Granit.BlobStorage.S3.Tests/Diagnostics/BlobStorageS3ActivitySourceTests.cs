using System.Diagnostics;
using Granit.BlobStorage.S3.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.BlobStorage.S3.Tests.Diagnostics;

public sealed class BlobStorageS3ActivitySourceTests : IDisposable
{
    private readonly ActivityListener _listener;

    public BlobStorageS3ActivitySourceTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == BlobStorageS3ActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    [Fact]
    public void Name_is_Granit_BlobStorage_S3() =>
        BlobStorageS3ActivitySource.Name.ShouldBe("Granit.BlobStorage.S3");

    [Fact]
    public void StartActivity_returns_activity_when_listener_attached()
    {
        using Activity? activity = BlobStorageS3ActivitySource.Source.StartActivity(BlobStorageS3ActivitySource.UploadTicket);

        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("blobstorage.upload-ticket");
    }
}
