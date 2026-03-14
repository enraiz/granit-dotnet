using System.Diagnostics;
using Granit.BlobStorage.Database.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.BlobStorage.Database.Tests.Diagnostics;

public sealed class BlobStorageDatabaseActivitySourceTests
{
    [Fact]
    public void Name_ShouldBe_GranitBlobStorageDatabase()
    {
        BlobStorageDatabaseActivitySource.Name.ShouldBe("Granit.BlobStorage.Database");
    }

    [Fact]
    public void Source_ShouldCreateActivity_WhenListenerIsRegistered()
    {
        using ActivityListener listener = new()
        {
            ShouldListenTo = source => source.Name == BlobStorageDatabaseActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(listener);

        using Activity? activity = BlobStorageDatabaseActivitySource.Source.StartActivity(BlobStorageDatabaseActivitySource.Save);

        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe(BlobStorageDatabaseActivitySource.Save);
    }
}
