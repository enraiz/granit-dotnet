using DigitalDynamics.Foundation.Vault.Options;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Kubernetes;
using VaultSharp.V1.AuthMethods.Token;

namespace DigitalDynamics.Foundation.Vault.Services;

/// <summary>
/// Factory for creating a VaultSharp client with the configured authentication method.
/// </summary>
public sealed partial class VaultClientFactory(
    IOptions<VaultOptions> options,
    ILogger<VaultClientFactory> logger,
    IStringLocalizer<VaultLocalizationResource> localizer)
{
    private readonly VaultOptions _options = options.Value;
    private readonly ILogger<VaultClientFactory> _logger = logger;
    private readonly IStringLocalizer<VaultLocalizationResource> _localizer = localizer;

    /// <summary>Creates an authenticated VaultSharp client.</summary>
    public IVaultClient Create()
    {
        IAuthMethodInfo authMethod = _options.AuthMethod.ToLowerInvariant() switch
        {
            "kubernetes" => CreateKubernetesAuth(),
            "token" => CreateTokenAuth(),
            _ => throw new InvalidOperationException(
                _localizer["Vault:UnknownAuthMethod", _options.AuthMethod])
        };

        VaultClientSettings settings = new(_options.Address, authMethod);
        LogClientCreated(_logger, _options.AuthMethod, _options.Address);

        return new VaultClient(settings);
    }

    private KubernetesAuthMethodInfo CreateKubernetesAuth()
    {
        string jwt = File.ReadAllText(_options.KubernetesTokenPath);
        LogKubernetesAuth(_logger, _options.KubernetesRole);
        return new KubernetesAuthMethodInfo(_options.KubernetesRole, jwt);
    }

    private TokenAuthMethodInfo CreateTokenAuth()
    {
        if (string.IsNullOrEmpty(_options.Token))
        {
            throw new InvalidOperationException(_localizer["Vault:TokenRequired"]);
        }

        LogTokenAuth(_logger);
        return new TokenAuthMethodInfo(_options.Token);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Vault client created with auth method {AuthMethod} at {Address}")]
    private static partial void LogClientCreated(ILogger logger, string authMethod, string address);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Kubernetes authentication with role {Role}")]
    private static partial void LogKubernetesAuth(ILogger logger, string role);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Static token Vault authentication — use only for local development")]
    private static partial void LogTokenAuth(ILogger logger);
}
