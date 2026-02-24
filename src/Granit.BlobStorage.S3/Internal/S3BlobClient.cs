using Amazon.S3;
using Amazon.S3.Model;
using Granit.BlobStorage.Internal;
using Microsoft.Extensions.Options;

namespace Granit.BlobStorage.S3.Internal;

/// <summary>
/// S3 implementation of <see cref="IBlobPresignedUrlGenerator"/> and <see cref="IBlobObjectClient"/>.
/// Uses AWSSDK.S3 with a configurable <see cref="S3BlobOptions.ServiceUrl"/> for S3-compatible providers.
/// </summary>
internal sealed class S3BlobClient : IBlobPresignedUrlGenerator, IBlobObjectClient, IDisposable
{
    private readonly AmazonS3Client _s3;
    private readonly S3BlobOptions _options;

    public S3BlobClient(IOptions<S3BlobOptions> options)
    {
        _options = options.Value;

        AmazonS3Config config = new()
        {
            ServiceURL = _options.ServiceUrl,
            ForcePathStyle = _options.ForcePathStyle,
            AuthenticationRegion = _options.Region,
            // Disable AWS SDK telemetry — we are consuming the S3 protocol, not AWS infrastructure.
            LogResponse = false,
            LogMetrics = false,
        };

        Amazon.Runtime.BasicAWSCredentials credentials = new(_options.AccessKey, _options.SecretKey);
        _s3 = new AmazonS3Client(credentials, config);
    }

    // ── IBlobPresignedUrlGenerator ────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<PresignedUploadTicket> GenerateUploadTicketAsync(
        string bucket,
        string objectKey,
        Guid blobId,
        BlobUploadRequest request,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        GetPreSignedUrlRequest presignRequest = new()
        {
            BucketName = bucket,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiry),
            ContentType = request.ContentType,
        };

        // Embed declared content-type and original filename as S3 metadata.
        presignRequest.Metadata.Add("x-amz-meta-original-filename", request.FileName);
        presignRequest.Metadata.Add("x-amz-meta-declared-content-type", request.ContentType);

        if (request.Metadata is not null)
        {
            foreach (KeyValuePair<string, string> entry in request.Metadata)
            {
                presignRequest.Metadata.Add($"x-amz-meta-{entry.Key}", entry.Value);
            }
        }

        string uploadUrl = _s3.GetPreSignedURL(presignRequest);

        Dictionary<string, string> requiredHeaders = new()
        {
            ["Content-Type"] = request.ContentType,
        };

        PresignedUploadTicket ticket = new(
            BlobId: blobId,
            UploadUrl: new Uri(uploadUrl),
            HttpMethod: "PUT",
            ExpiresAt: DateTimeOffset.UtcNow.Add(expiry),
            RequiredHeaders: requiredHeaders);

        return Task.FromResult(ticket);
    }

    /// <inheritdoc/>
    public Task<PresignedDownloadUrl> GenerateDownloadUrlAsync(
        string bucket,
        string objectKey,
        DownloadUrlOptions? options,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        GetPreSignedUrlRequest presignRequest = new()
        {
            BucketName = bucket,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry),
        };

        if (!string.IsNullOrEmpty(options?.DownloadFileName))
        {
            presignRequest.ResponseHeaderOverrides.ContentDisposition =
                $"attachment; filename=\"{options.DownloadFileName}\"";
        }

        string downloadUrl = _s3.GetPreSignedURL(presignRequest);

        PresignedDownloadUrl result = new(
            Url: new Uri(downloadUrl),
            ExpiresAt: DateTimeOffset.UtcNow.Add(expiry));

        return Task.FromResult(result);
    }

    // ── IBlobObjectClient ─────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task DeleteObjectAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        DeleteObjectRequest deleteRequest = new()
        {
            BucketName = bucket,
            Key = objectKey,
        };

        await _s3.DeleteObjectAsync(deleteRequest, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<long> GetObjectSizeBytesAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        GetObjectMetadataRequest metadataRequest = new()
        {
            BucketName = bucket,
            Key = objectKey,
        };

        GetObjectMetadataResponse response = await _s3.GetObjectMetadataAsync(metadataRequest, cancellationToken);
        return response.ContentLength;
    }

    /// <inheritdoc/>
    public async Task<Stream> OpenPartialStreamAsync(
        string bucket,
        string objectKey,
        int byteCount,
        CancellationToken cancellationToken = default)
    {
        GetObjectRequest rangeRequest = new()
        {
            BucketName = bucket,
            Key = objectKey,
            ByteRange = new ByteRange(0, byteCount - 1),
        };

        GetObjectResponse response = await _s3.GetObjectAsync(rangeRequest, cancellationToken);
        return response.ResponseStream;
    }

    /// <inheritdoc/>
    public void Dispose() => _s3.Dispose();
}
