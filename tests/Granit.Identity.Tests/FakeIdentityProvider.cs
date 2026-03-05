using Granit.Identity.Models;

namespace Granit.Identity.Tests;

/// <summary>
/// Minimal <see cref="IIdentityProvider"/> implementation used in DI registration tests.
/// </summary>
internal sealed class FakeIdentityProvider : IIdentityProvider
{
    public Task<IReadOnlyList<IdentityUser>> GetUsersAsync(
        string? search = null, int? first = null, int? max = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IdentityUser>>([]);

    public Task<IdentityUser?> GetUserAsync(
        string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IdentityUser?>(null);

    public Task SetUserEnabledAsync(
        string userId, bool enabled, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<IdentitySession>> GetUserSessionsAsync(
        string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IdentitySession>>([]);

    public Task<IReadOnlyList<IdentityDeviceActivity>> GetUserDeviceActivityAsync(
        string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IdentityDeviceActivity>>([]);

    public Task<DateTimeOffset?> GetPasswordChangedAtAsync(
        string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<DateTimeOffset?>(null);

    public Task<IReadOnlyList<IdentityRole>> GetRolesAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IdentityRole>>([]);

    public Task<IReadOnlyList<IdentityUser>> GetRoleMembersAsync(
        string roleName, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IdentityUser>>([]);
}
