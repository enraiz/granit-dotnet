using FluentAssertions;
using Granit.Persistence.Migrations.Internal;
using Xunit;

namespace Granit.Persistence.Migrations.Tests;

public sealed class NullTenantEnumeratorTests
{
    [Fact]
    public async Task GetActiveTenantIdsAsync_ReturnsEmptyStream()
    {
        NullTenantEnumerator sut = new();
        List<Guid> result = [];

        await foreach (Guid id in sut.GetActiveTenantIdsAsync(TestContext.Current.CancellationToken))
        {
            result.Add(id);
        }

        result.Should().BeEmpty();
    }
}
