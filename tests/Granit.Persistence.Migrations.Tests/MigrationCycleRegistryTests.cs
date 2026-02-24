using FluentAssertions;
using Granit.Persistence.Migrations.Internal;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Granit.Persistence.Migrations.Tests;

public sealed class MigrationCycleRegistryTests
{
    private readonly MigrationCycleRegistry _registry = new();

    [Fact]
    public void Register_NewCycleId_StoresRegistration()
    {
        BatchMigrationDelegate migration = (_, _, _) => Task.FromResult(new MigrationBatchResult(0, null));

        _registry.Register("cycle-1", typeof(DbContext), migration);

        MigrationCycleRegistration? found = _registry.Find("cycle-1");
        found.Should().NotBeNull();
        found!.CycleId.Should().Be("cycle-1");
        found.DbContextType.Should().Be<DbContext>();
        found.Migration.Should().BeSameAs(migration);
    }

    [Fact]
    public void Register_DuplicateCycleId_ThrowsInvalidOperationException()
    {
        BatchMigrationDelegate migration = (_, _, _) => Task.FromResult(new MigrationBatchResult(0, null));
        _registry.Register("cycle-dup", typeof(DbContext), migration);

        Action act = () => _registry.Register("cycle-dup", typeof(DbContext), migration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cycle-dup*");
    }

    [Fact]
    public void Register_IsCaseInsensitive()
    {
        BatchMigrationDelegate migration = (_, _, _) => Task.FromResult(new MigrationBatchResult(0, null));
        _registry.Register("CYCLE-CASE", typeof(DbContext), migration);

        Action act = () => _registry.Register("cycle-case", typeof(DbContext), migration);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Find_UnknownCycleId_ReturnsNull()
    {
        MigrationCycleRegistration? result = _registry.Find("unknown");

        result.Should().BeNull();
    }

    [Fact]
    public void Find_CaseInsensitive_ReturnsRegistration()
    {
        BatchMigrationDelegate migration = (_, _, _) => Task.FromResult(new MigrationBatchResult(0, null));
        _registry.Register("CycleA", typeof(DbContext), migration);

        MigrationCycleRegistration? result = _registry.Find("cyclea");

        result.Should().NotBeNull();
        result!.CycleId.Should().Be("CycleA");
    }
}
