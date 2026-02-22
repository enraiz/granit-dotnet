using DigitalDynamics.Foundation.Authorization;
using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Persistence;

namespace DigitalDynamics.Foundation.Authorization.EntityFrameworkCore;

/// <summary>
/// Foundation module for EF Core authorization grant persistence.
/// Provides <see cref="Entities.PermissionGrant"/> entity, <see cref="DbContext.IPermissionGrantDbContext"/>,
/// and <see cref="Abstractions.IPermissionManager"/> with HDS audit logging.
/// </summary>
/// <remarks>
/// Registration of the generic store requires the application DbContext type. Call
/// <c>services.AddFoundationAuthorizationEntityFrameworkCore&lt;TContext&gt;()</c>
/// in the host application's module or startup code.
/// </remarks>
[DependsOn(
    typeof(FoundationAuthorizationModule),
    typeof(FoundationPersistenceModule))]
public sealed class FoundationAuthorizationEntityFrameworkCoreModule : FoundationModule;
