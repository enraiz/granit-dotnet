using DigitalDynamics.Foundation.Authorization.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace DigitalDynamics.Foundation.Authorization.Authorization;

internal sealed class PermissionAuthorizationHandler(IPermissionChecker permissionChecker)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (await permissionChecker.IsGrantedAsync(requirement.PermissionName))
        {
            context.Succeed(requirement);
        }
    }
}
