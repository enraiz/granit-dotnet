using FluentAssertions;
using Granit.Persistence.Migrations.Extensions;
using Granit.Persistence.Migrations.Internal;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Granit.Persistence.Migrations.Tests;

public sealed class MigrationCycleRegistryExtensionsTests
{
    [Fact]
    public void Register_Generic_StoresCorrectDbContextType()
    {
        MigrationCycleRegistry registry = new();
        BatchMigrationDelegate migration = (_, _, _) => Task.FromResult(new MigrationBatchResult(0, null));

        IMigrationCycleRegistry returned = registry.Register<StubDbContext>("cycle-ext", migration);

        MigrationCycleRegistration? found = registry.Find("cycle-ext");
        found.Should().NotBeNull();
        found!.DbContextType.Should().Be<StubDbContext>();
        returned.Should().BeSameAs(registry);
    }

    [Fact]
    public void Register_Generic_ReturnsSameRegistry_ForFluentChaining()
    {
        MigrationCycleRegistry registry = new();
        BatchMigrationDelegate migration = (_, _, _) => Task.FromResult(new MigrationBatchResult(0, null));

        IMigrationCycleRegistry result = registry.Register<StubDbContext>("chain-cycle", migration);

        result.Should().BeSameAs(registry);
    }

    private sealed class StubDbContext(DbContextOptions<StubDbContext> options) : DbContext(options);
}
