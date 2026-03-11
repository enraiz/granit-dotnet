using Granit.Authentication.ApiKeys.Domain;
using Granit.Authentication.ApiKeys.Endpoints.Dtos;
using Shouldly;
using Xunit;

namespace Granit.Authentication.ApiKeys.Endpoints.Tests.Dtos;

public sealed class ApiKeyResponseTests
{
    [Fact]
    public void FromEntry_Maps_All_Properties()
    {
        var entry = new ApiKeyEntry
        {
            Id = Guid.NewGuid(),
            Name = "Test Key",
            Type = ApiKeyType.Secret,
            Environment = "live",
            Prefix = "gk_live_sk_",
            LastFourChars = "Ab1x",
            Permissions = ["Read", "Write"],
            AllowedCidrs = ["10.0.0.0/8"],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            LastUsedAt = DateTimeOffset.UtcNow.AddHours(-1),
            RevokedAt = null,
            CacheBehavior = CacheBehavior.Normal,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-7),
        };

        var response = ApiKeyResponse.FromEntry(entry);

        response.Id.ShouldBe(entry.Id);
        response.Name.ShouldBe(entry.Name);
        response.Type.ShouldBe(entry.Type);
        response.Environment.ShouldBe(entry.Environment);
        response.Prefix.ShouldBe(entry.Prefix);
        response.LastFourChars.ShouldBe(entry.LastFourChars);
        response.Permissions.ShouldBe(entry.Permissions);
        response.AllowedCidrs.ShouldBe(entry.AllowedCidrs);
        response.ExpiresAt.ShouldBe(entry.ExpiresAt);
        response.LastUsedAt.ShouldBe(entry.LastUsedAt);
        response.RevokedAt.ShouldBeNull();
        response.CacheBehavior.ShouldBe(entry.CacheBehavior);
        response.CreatedAt.ShouldBe(entry.CreatedAt);
    }
}
