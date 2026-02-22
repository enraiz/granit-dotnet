// =============================================================================
// CurrentUserService - ICurrentUserService implementation via HttpContext
// =============================================================================
// Extracts current user information from JWT claims.
// UserName reads User.Identity.Name, which respects the NameClaimType configured
// in JWT Bearer (default: "sub" — RFC 7519, always present in a valid JWT).
// =============================================================================

using System.Security.Claims;
using DigitalDynamics.Foundation.Security;
using Microsoft.AspNetCore.Http;

namespace DigitalDynamics.Foundation.Authentication.JwtBearer.Authentication;

/// <summary>
/// Implementation of <see cref="ICurrentUserService"/> based on HttpContext.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? User?.FindFirstValue("sub");

    public string? UserName => User?.Identity?.Name;

    public string? Email => User?.FindFirstValue(ClaimTypes.Email)
                            ?? User?.FindFirstValue("email");

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyList<string> Roles => User?.FindAll(ClaimTypes.Role)
        .Select(c => c.Value)
        .ToList()
        .AsReadOnly() ?? new List<string>().AsReadOnly();

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
}
