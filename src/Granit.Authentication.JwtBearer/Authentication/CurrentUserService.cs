using System.Security.Claims;
using Granit.Security;
using Microsoft.AspNetCore.Http;

namespace Granit.Authentication.JwtBearer.Authentication;

/// <summary>
/// Implementation of <see cref="ICurrentUserService"/> based on HttpContext.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? User?.FindFirstValue("sub");

    public string? UserName => User?.Identity?.Name;

    public string? Email => User?.FindFirstValue(ClaimTypes.Email)
                            ?? User?.FindFirstValue("email");

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyList<string> GetRoles()
    {
        if (User is not { } user)
        {
            return Array.Empty<string>();
        }

        List<string> roles = [.. user.FindAll(ClaimTypes.Role).Select(c => c.Value)];
        return roles.Count == 0 ? Array.Empty<string>() : roles;
    }

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
}
