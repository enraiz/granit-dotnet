using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Granit.BlobStorage.GoogleCloud.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Granit.BlobStorage.GoogleCloud.HealthChecks;

/// <summary>
/// Health check that verifies Google Cloud Storage connectivity by issuing a
/// <c>ListObjects</c> request (max 1 result) on the configured default bucket.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>Bucket accessible → <see cref="HealthCheckResult.Healthy"/></item>
///   <item>Bucket not found or access denied → <see cref="HealthCheckResult.Unhealthy"/></item>
///   <item>Unreachable → <see cref="HealthCheckResult.Unhealthy"/></item>
/// </list>
/// The response never exposes endpoint URLs, bucket names, or credentials.
/// </remarks>
internal sealed class GoogleCloudStorageHealthCheck(
    IOptions<GoogleCloudStorageOptions> options) : IHealthCheck
{
    private static readonly TimeSpan s_healthCheckTimeout = TimeSpan.FromSeconds(10);
    private readonly StorageClient _storage = CreateClient(options.Value);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // ListObjects with PageSize=1 is a lightweight connectivity check
            await Task.Run(
                () => _storage.ListObjects(options.Value.DefaultBucket, options: new ListObjectsOptions { PageSize = 1 }),
                cancellationToken)
                .WaitAsync(s_healthCheckTimeout, cancellationToken).ConfigureAwait(false);

            return HealthCheckResult.Healthy();
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return HealthCheckResult.Unhealthy("GCS bucket not found");
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return HealthCheckResult.Unhealthy("GCS access denied");
        }
        catch (Exception ex)
        {
            // Sanitize: never expose endpoint URLs or credentials in the message
            return HealthCheckResult.Unhealthy($"GCS unreachable: {ex.GetType().Name}");
        }
    }

    private static StorageClient CreateClient(GoogleCloudStorageOptions opts)
    {
        if (!string.IsNullOrEmpty(opts.CredentialFilePath))
        {
            ServiceAccountCredential credential = CredentialFactory.FromFile<ServiceAccountCredential>(opts.CredentialFilePath);
            return StorageClient.Create(credential.ToGoogleCredential());
        }

        return StorageClient.Create();
    }
}
