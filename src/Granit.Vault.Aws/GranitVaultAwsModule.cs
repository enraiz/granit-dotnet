using Granit.Core.Modularity;
using Granit.Encryption;
using Granit.Vault.Aws.Extensions;
using Microsoft.Extensions.Hosting;

namespace Granit.Vault.Aws;

/// <summary>Module for AWS KMS + Secrets Manager vault provider.</summary>
[DependsOn(typeof(GranitEncryptionModule))]
public sealed class GranitVaultAwsModule : GranitModule
{
    /// <inheritdoc />
    public override bool IsEnabled(ServiceConfigurationContext context) =>
        !context.Builder.Environment.IsDevelopment();

    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitVaultAws();
}
