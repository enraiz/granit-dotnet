using FluentAssertions;
using Xunit;

namespace Granit.BlobStorage.Tests;

public sealed class PresignedUploadTicketTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        Guid blobId = Guid.NewGuid();
        Uri uploadUrl = new("https://s3.example.com/bucket/key?signature=abc");
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        Dictionary<string, string> headers = new()
        {
            ["Content-Type"] = "application/pdf",
        };

        PresignedUploadTicket ticket = new(blobId, uploadUrl, "PUT", expiresAt, headers);

        ticket.BlobId.Should().Be(blobId);
        ticket.UploadUrl.Should().Be(uploadUrl);
        ticket.HttpMethod.Should().Be("PUT");
        ticket.ExpiresAt.Should().Be(expiresAt);
        ticket.RequiredHeaders.Should().ContainKey("Content-Type");
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        Guid blobId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Uri url = new("https://s3.example.com/key");
        DateTimeOffset expiry = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        Dictionary<string, string> headers = new() { ["Content-Type"] = "text/plain" };

        PresignedUploadTicket a = new(blobId, url, "PUT", expiry, headers);
        PresignedUploadTicket b = new(blobId, url, "PUT", expiry, headers);

        a.Should().Be(b);
    }
}
