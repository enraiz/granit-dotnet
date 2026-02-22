// =============================================================================
// CurrentUserService - Implémentation de ICurrentUserService via HttpContext
// =============================================================================
// Extrait les informations de l'utilisateur courant depuis les claims JWT.
// UserName lit User.Identity.Name, qui respecte le NameClaimType configuré
// dans JWT Bearer (défaut : "sub" — RFC 7519, toujours présent dans un JWT valide).
// =============================================================================

using System.Security.Claims;
using DigitalDynamics.Foundation.Security;
using Microsoft.AspNetCore.Http;

namespace DigitalDynamics.Foundation.Security.Authentication;

/// <summary>
/// Implémentation de <see cref="ICurrentUserService"/> basée sur HttpContext.
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
