// =============================================================================
// Tests - VaultCredentialLeaseManager
// =============================================================================
// Vérifie le cycle de vie des credentials dynamiques PostgreSQL :
//   - Obtention initiale
//   - Renouvellement du lease
//   - Fallback en cas d'échec de renouvellement
//   - Propriétés IsReady, Username, Password
// =============================================================================

using FluentAssertions;
using Granit.Vault.Options;
using Granit.Vault.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using VaultSharp;
using VaultSharp.V1;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines;
using VaultSharp.V1.SecretsEngines.Database;
using VaultSharp.V1.SystemBackend;
using Xunit;

namespace Granit.Vault.Tests;

public sealed class VaultCredentialLeaseManagerTests : IDisposable
{
    private readonly IVaultClient _vaultClient;
    private readonly IDatabaseSecretsEngine _databaseEngine;
    private readonly ISystemBackend _systemBackend;
    private readonly VaultOptions _options;
    private readonly VaultCredentialLeaseManager _sut;

    public VaultCredentialLeaseManagerTests()
    {
        _vaultClient = Substitute.For<IVaultClient>();

        IVaultClientV1 v1 = Substitute.For<IVaultClientV1>();
        _vaultClient.V1.Returns(v1);

        ISecretsEngine secretsEngine = Substitute.For<ISecretsEngine>();
        v1.Secrets.Returns(secretsEngine);

        _databaseEngine = Substitute.For<IDatabaseSecretsEngine>();
        secretsEngine.Database.Returns(_databaseEngine);

        _systemBackend = Substitute.For<ISystemBackend>();
        v1.System.Returns(_systemBackend);

        _options = new VaultOptions
        {
            DatabaseMountPoint = "database",
            DatabaseRoleName = "readwrite",
            LeaseRenewalThreshold = 0.75
        };

        _sut = new VaultCredentialLeaseManager(
            _vaultClient,
            Microsoft.Extensions.Options.Options.Create(_options),
            NullLogger<VaultCredentialLeaseManager>.Instance);
    }

    public void Dispose() => _sut.Dispose();

    [Fact]
    public void IsReady_Initially_ReturnsFalse() => _sut.IsReady.Should().BeFalse();

    [Fact]
    public void Username_Initially_ReturnsEmpty() => _sut.Username.Should().BeEmpty();

    [Fact]
    public void Password_Initially_ReturnsEmpty() => _sut.Password.Should().BeEmpty();

    [Fact]
    public async Task ExecuteAsync_ObtainsCredentialsOnStart()
    {
        // Arrange
        SetupDatabaseCredentials("v-user-123", "p@ssw0rd!", "lease-abc", 3600);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);

        // Act - Start and immediately cancel after credentials are obtained
        await _sut.StartAsync(cts.Token);

        // Give time for ExecuteAsync to call ObtainCredentialsAsync
        await Task.Delay(200, TestContext.Current.CancellationToken);
        await cts.CancelAsync();

        try
        {
            await _sut.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }

        // Assert
        _sut.IsReady.Should().BeTrue();
        _sut.Username.Should().Be("v-user-123");
        _sut.Password.Should().Be("p@ssw0rd!");

        await _databaseEngine.Received(1).GetCredentialsAsync(
            "readwrite",
            mountPoint: "database");
    }

    [Fact]
    public async Task ExecuteAsync_RenewsLeaseBeforeExpiration()
    {
        // Arrange - Short TTL to trigger renewal quickly
        SetupDatabaseCredentials("v-user-456", "secret", "lease-xyz", 1);

        _systemBackend.RenewLeaseAsync("lease-xyz", 1)
            .Returns(new Secret<RenewedLease>
            {
                LeaseDurationSeconds = 3600
            });

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);

        // Act
        await _sut.StartAsync(cts.Token);

        // Wait enough for renewal cycle (TTL=1s * threshold=0.75 = 0.75s delay)
        await Task.Delay(1500, TestContext.Current.CancellationToken);
        await cts.CancelAsync();

        try
        {
            await _sut.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        await _systemBackend.Received(1).RenewLeaseAsync("lease-xyz", 1);
    }

    [Fact]
    public async Task ExecuteAsync_FallbackToNewCredentials_WhenRenewalFails()
    {
        // Arrange - Short TTL
        SetupDatabaseCredentials("v-user-first", "pass1", "lease-1", 1);

        // First renewal fails
        _systemBackend.RenewLeaseAsync("lease-1", 1)
            .ThrowsAsync(new InvalidOperationException("Lease expired"));

        // Setup second credential obtainment (fallback)
        int callCount = 0;
        _databaseEngine.GetCredentialsAsync("readwrite", mountPoint: "database")
            .Returns(_ =>
            {
                callCount++;
                string username = callCount == 1 ? "v-user-first" : "v-user-second";
                string password = callCount == 1 ? "pass1" : "pass2";
                string leaseId = callCount == 1 ? "lease-1" : "lease-2";
                return new Secret<UsernamePasswordCredentials>
                {
                    Data = new UsernamePasswordCredentials { Username = username, Password = password },
                    LeaseId = leaseId,
                    LeaseDurationSeconds = callCount == 1 ? 1 : 3600
                };
            });

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);

        // Act
        await _sut.StartAsync(cts.Token);
        await Task.Delay(2000, TestContext.Current.CancellationToken);
        await cts.CancelAsync();

        try
        {
            await _sut.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - Should have obtained new credentials after renewal failure
        _sut.IsReady.Should().BeTrue();
        _sut.Username.Should().Be("v-user-second");
        _sut.Password.Should().Be("pass2");
    }

    [Fact]
    public void IDatabaseCredentialProvider_IsImplemented() =>
        _sut.Should().BeAssignableTo<IDatabaseCredentialProvider>();

    private void SetupDatabaseCredentials(string username, string password, string leaseId, int ttl) =>
        _databaseEngine.GetCredentialsAsync("readwrite", mountPoint: "database")
            .Returns(new Secret<UsernamePasswordCredentials>
            {
                Data = new UsernamePasswordCredentials { Username = username, Password = password },
                LeaseId = leaseId,
                LeaseDurationSeconds = ttl
            });
}
