using System.Diagnostics.CodeAnalysis;
using Granit.BlobStorage.Internal;
using Granit.BlobStorage.Options;
using Granit.BlobStorage.S3.Internal;
using Granit.BlobStorage.S3.Options;
using Granit.Core.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Granit.BlobStorage.S3.Extensions;

/// <summary>
/// Extension methods for registering the S3-compatible blob storage provider.
/// </summary>
// DI wiring only — no logic to unit test.
[ExcludeFromCodeCoverage]
public static class BlobStorageS3HostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds <c>Granit.BlobStorage.S3</c> services: S3 client, key strategy, and the
    /// <see cref="Granit.BlobStorage.IBlobStorage"/> orchestrator.
    /// </summary>
    /// <remarks>
    /// Reads <see cref="S3BlobOptions"/> from the <c>"BlobStorage"</c> configuration section.
    /// Credentials (<c>AccessKey</c>, <c>SecretKey</c>) must be injected from Granit.Vault;
    /// never store them in <c>appsettings.json</c>.
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddGranitBlobStorageS3(
        this IHostApplicationBuilder builder)
    {
        GranitActivitySourceRegistry.Register(S3.Diagnostics.BlobStorageS3ActivitySource.Name);

        builder.Services
            .AddOptions<S3BlobOptions>()
            .BindConfiguration(BlobStorageOptions.SectionName)
            .ValidateOnStart();

        builder.Services.AddSingleton<IValidateOptions<S3BlobOptions>, S3BlobOptionsValidator>();

        // S3BlobClient implements IBlobStorageClient (pre-signed URLs + object operations).
        // Registered as Singleton: AmazonS3Client is thread-safe and intended for reuse.
        builder.Services.AddSingleton<S3BlobClient>();
        builder.Services.AddSingleton<IBlobStorageClient>(sp => sp.GetRequiredService<S3BlobClient>());

        builder.Services.AddScoped<IBlobKeyStrategy, PrefixBlobKeyStrategy>();
        builder.Services.AddScoped<IBlobStorage, DefaultBlobStorage>();

        return builder;
    }
}
