using Shouldly;
using Xunit;

namespace Granit.BlobStorage.Tests;

public sealed class DownloadUrlOptionsTests
{
    [Fact]
    public void DefaultValues_AreNull()
    {
        DownloadUrlOptions options = new();

        options.Expiry.ShouldBeNull();
        options.DownloadFileName.ShouldBeNull();
    }

    [Fact]
    public void WithExpiry_SetsExpiry()
    {
        DownloadUrlOptions options = new(Expiry: TimeSpan.FromMinutes(30));

        options.Expiry.ShouldBe(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void WithDownloadFileName_SetsFileName()
    {
        DownloadUrlOptions options = new(DownloadFileName: "report.pdf");

        options.DownloadFileName.ShouldBe("report.pdf");
    }

    [Fact]
    public void WithBothParameters_SetsBoth()
    {
        DownloadUrlOptions options = new(
            Expiry: TimeSpan.FromHours(1),
            DownloadFileName: "data.csv");

        options.Expiry.ShouldBe(TimeSpan.FromHours(1));
        options.DownloadFileName.ShouldBe("data.csv");
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        DownloadUrlOptions a = new(Expiry: TimeSpan.FromMinutes(5));
        DownloadUrlOptions b = new(Expiry: TimeSpan.FromMinutes(5));

        a.ShouldBe(b);
    }
}
