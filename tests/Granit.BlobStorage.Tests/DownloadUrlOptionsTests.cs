using FluentAssertions;
using Xunit;

namespace Granit.BlobStorage.Tests;

public sealed class DownloadUrlOptionsTests
{
    [Fact]
    public void DefaultValues_AreNull()
    {
        DownloadUrlOptions options = new();

        options.Expiry.Should().BeNull();
        options.DownloadFileName.Should().BeNull();
    }

    [Fact]
    public void WithExpiry_SetsExpiry()
    {
        DownloadUrlOptions options = new(Expiry: TimeSpan.FromMinutes(30));

        options.Expiry.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void WithDownloadFileName_SetsFileName()
    {
        DownloadUrlOptions options = new(DownloadFileName: "report.pdf");

        options.DownloadFileName.Should().Be("report.pdf");
    }

    [Fact]
    public void WithBothParameters_SetsBoth()
    {
        DownloadUrlOptions options = new(
            Expiry: TimeSpan.FromHours(1),
            DownloadFileName: "data.csv");

        options.Expiry.Should().Be(TimeSpan.FromHours(1));
        options.DownloadFileName.Should().Be("data.csv");
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        DownloadUrlOptions a = new(Expiry: TimeSpan.FromMinutes(5));
        DownloadUrlOptions b = new(Expiry: TimeSpan.FromMinutes(5));

        a.Should().Be(b);
    }
}
