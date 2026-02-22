using DigitalDynamics.Foundation.Authorization.EntityFrameworkCore.DbContext;
using DigitalDynamics.Foundation.Authorization.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalDynamics.Foundation.Authorization.EntityFrameworkCore.Tests;

/// <summary>
/// Shared in-memory test DbContext for Foundation.Authorization.EntityFrameworkCore tests.
/// Must be internal (not private) so that Castle.DynamicProxy can proxy
/// ILogger&lt;PermissionManager&lt;TestDbContext&gt;&gt; via NSubstitute.
/// </summary>
internal sealed class TestDbContext(DbContextOptions<TestDbContext> options)
    : Microsoft.EntityFrameworkCore.DbContext(options), IPermissionGrantDbContext
{
    public DbSet<PermissionGrant> PermissionGrants => Set<PermissionGrant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ConfigurePermissionGrants();
}
