using Granit.BlobStorage.Options;
using Microsoft.Extensions.Options;

namespace Granit.BlobStorage.FileSystem.Options;

/// <summary>
/// Configuration for the local file system blob storage provider.
/// Extends <see cref="BlobStorageOptions"/> with file system-specific settings.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="BasePath"/> is the root directory where blobs are stored.
/// The directory structure is managed automatically:
/// <c>{BasePath}/{tenantId}/{containerName}/{yyyy}/{MM}/{blobId}</c>.
/// </para>
/// <para>
/// For development, use a relative path like <c>./blobs</c>.
/// For production on-premise, use an absolute path like <c>/data/blobs</c>.
/// </para>
/// </remarks>
public sealed class FileSystemBlobOptions : BlobStorageOptions
{
    /// <summary>
    /// Root directory for blob storage. Must be non-empty and writable.
    /// Examples: <c>./blobs</c> (dev), <c>/data/blobs</c> (production).
    /// </summary>
    public string BasePath { get; set; } = string.Empty;
}

/// <summary>
/// Validates <see cref="FileSystemBlobOptions"/> at startup (fail-fast on misconfiguration).
/// </summary>
internal sealed class FileSystemBlobOptionsValidator : IValidateOptions<FileSystemBlobOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, FileSystemBlobOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BasePath))
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(options.BasePath)} must be non-empty. " +
                "Set it to the root directory for blob storage (e.g. ./blobs or /data/blobs).");
        }

        return ValidateOptionsResult.Success;
    }
}
