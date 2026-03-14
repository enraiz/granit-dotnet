using System.Diagnostics;
using Granit.BlobStorage.FileSystem.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.BlobStorage.FileSystem.Tests.Diagnostics;

public sealed class BlobStorageFileSystemActivitySourceTests
{
    [Fact]
    public void Name_ShouldBe_GranitBlobStorageFileSystem()
    {
        BlobStorageFileSystemActivitySource.Name.ShouldBe("Granit.BlobStorage.FileSystem");
    }

    [Fact]
    public void Source_ShouldCreateActivity_WhenListenerIsRegistered()
    {
        using ActivityListener listener = new()
        {
            ShouldListenTo = source => source.Name == BlobStorageFileSystemActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(listener);

        using Activity? activity = BlobStorageFileSystemActivitySource.Source.StartActivity(BlobStorageFileSystemActivitySource.Save);

        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe(BlobStorageFileSystemActivitySource.Save);
    }
}
