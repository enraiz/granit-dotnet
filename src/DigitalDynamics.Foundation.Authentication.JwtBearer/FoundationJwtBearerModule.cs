using DigitalDynamics.Foundation.Authentication.JwtBearer.Extensions;
using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Security;

namespace DigitalDynamics.Foundation.Authentication.JwtBearer;

/// <summary>
/// Foundation module for generic OIDC JWT Bearer authentication.
/// Depends on <see cref="FoundationSecurityModule"/> for the abstractions.
/// </summary>
[DependsOn(typeof(FoundationSecurityModule))]
public sealed class FoundationJwtBearerModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationJwtBearer(context.Configuration);
}
