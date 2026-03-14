using System.Diagnostics;
using Granit.BlobStorage.AzureBlob.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.BlobStorage.AzureBlob.Tests.Diagnostics;

public sealed class BlobStorageAzureActivitySourceTests
{
    [Fact]
    public void Name_ShouldBe_GranitBlobStorageAzureBlob() =>
        BlobStorageAzureActivitySource.Name.ShouldBe("Granit.BlobStorage.AzureBlob");

    [Fact]
    public void Source_ShouldCreateActivity_WhenListenerIsRegistered()
    {
        using ActivityListener listener = new()
        {
            ShouldListenTo = source => source.Name == BlobStorageAzureActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(listener);

        using Activity? activity = BlobStorageAzureActivitySource.Source.StartActivity(BlobStorageAzureActivitySource.Save);

        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe(BlobStorageAzureActivitySource.Save);
    }
}
